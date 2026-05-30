using BillFlow.Contracts.Logging;
using BillFlow.Contracts.Metrics;
using BillFlow.Gateway.Endpoints;
using BillFlow.Gateway.Middleware;
using BillFlow.Gateway.Transforms;
using Serilog;
using System.Threading.RateLimiting;
using Yarp.ReverseProxy.Model;

const string ServiceName = "Gateway";

SerilogBootstrap.Configure(ServiceName);

try
{
    var builder = WebApplication.CreateBuilder(args);

    SerilogBootstrap.ConfigureBuilder(builder, ServiceName);
    builder.Services.AddBillFlowMetrics(ServiceName);

    // ── YARP ─────────────────────────────────────────────────────────────────
    builder.Services
        .AddReverseProxy()
        .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
        .AddTransforms<GatewayTransformProvider>();

    // ── Rate limiting ─────────────────────────────────────────────────────────
    var rateLimitConfig = builder.Configuration.GetSection("RateLimiting");
    var permitLimit = rateLimitConfig.GetValue<int>("PermitLimit", 100);
    var windowSeconds = rateLimitConfig.GetValue<int>("WindowSeconds", 60);
    var queueLimit = rateLimitConfig.GetValue<int>("QueueLimit", 10);

    builder.Services.AddRateLimiter(options =>
    {
        options.AddPolicy("standard", context =>
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: $"standard_{clientIp}",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromSeconds(windowSeconds),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = queueLimit
                });
        });

        options.AddPolicy("auth", context =>
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: $"auth_{clientIp}",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromSeconds(60),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 2
                });
        });

        options.AddPolicy("pdf", context =>
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: $"pdf_{clientIp}",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromSeconds(60),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 5
                });
        });

        options.OnRejected = async (context, cancellationToken) =>
        {
            BillFlowMetrics.RateLimitRejections
                .WithLabels(context.HttpContext.Request.Path)
                .Inc();

            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.Headers["Retry-After"] = windowSeconds.ToString();
            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                error = "Rate limit exceeded",
                retryAfterSeconds = windowSeconds,
                message = $"Too many requests. Retry after {windowSeconds} seconds."
            }, cancellationToken);
        };
    });

    // ── HttpClient for health checks ──────────────────────────────────────────
    // ← ConfigurePrimaryHttpMessageHandler accepts self-signed dev certs
    builder.Services.AddHttpClient("HealthCheck", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(5);
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    });

    // ── CORS ──────────────────────────────────────────────────────────────────
    var allowedOrigins = builder.Configuration
        .GetSection("AllowedOrigins")
        .Get<string[]>()
        ?? ["http://localhost:3000"];

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("BillFlowPolicy", policy =>
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
    });

    var app = builder.Build();

    // ── Middleware pipeline ───────────────────────────────────────────────────
    app.UseHttpsRedirection();
    app.UseBillFlowMetrics();

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseMiddleware<GatewayLoggingMiddleware>();
    app.UseMiddleware<RequestSizeLimitMiddleware>();
    app.UseCors("BillFlowPolicy");
    app.UseRateLimiter();

    app.MapHealthEndpoints();

    // ← Metrics tracked INSIDE YARP pipeline where IReverseProxyFeature exists
    app.MapReverseProxy(pipeline =>
    {
        pipeline.Use(async (context, next) =>
        {
            await next();

            // ClusterId is on the route model, not the destination
            var proxyFeature = context.GetReverseProxyFeature();
            var cluster = proxyFeature?.Route?.Config?.ClusterId
                ?? "unknown";

            BillFlowMetrics.GatewayRequestsProxied
                .WithLabels(cluster, context.Response.StatusCode.ToString())
                .Inc();
        });

        pipeline.UseMiddleware<RateLimitPolicyFilter>();
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "{ServiceName} terminated unexpectedly", ServiceName);
}
finally
{
    Log.CloseAndFlush();
}