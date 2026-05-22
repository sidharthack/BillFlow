using BillFlow.Contracts.Logging;
using BillFlow.Gateway.Endpoints;
using BillFlow.Gateway.Middleware;
using BillFlow.Gateway.Transforms;
using Serilog;
using System.Threading.RateLimiting;
const string ServiceName = "Gateway";
SerilogBootstrap.Configure(ServiceName);

try
{
    var builder = WebApplication.CreateBuilder(args);

    SerilogBootstrap.ConfigureBuilder(builder, ServiceName);

    // ── YARP with programmatic transforms ────────────────────────────────────
    builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms<GatewayTransformProvider>();  // ← register transform provider

    // ── Rate limiting ─────────────────────────────────────────────────────────
    var rateLimitConfig = builder.Configuration.GetSection("RateLimiting");
    var permitLimit = rateLimitConfig.GetValue<int>("PermitLimit", 100);
    var windowSeconds = rateLimitConfig.GetValue<int>("WindowSeconds", 60);
    var queueLimit = rateLimitConfig.GetValue<int>("QueueLimit", 10);

    builder.Services.AddRateLimiter(options =>
    {
        // Standard API policy
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

        // Auth policy — strict
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

        // PDF policy
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
    builder.Services.AddHttpClient("HealthCheck", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(3);
    });

    // ── CORS ──────────────────────────────────────────────────────────────────
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("BillFlowPolicy", policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:3000",
                    "http://localhost:4200",
                    "https://app.billflow.io")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
    });

    var app = builder.Build();

    // ── Middleware pipeline (order matters) ───────────────────────────────────

    app.UseMiddleware<CorrelationIdMiddleware>();       // 1. Correlation ID first
    app.UseMiddleware<SecurityHeadersMiddleware>();     // 2. Security headers
    app.UseMiddleware<GatewayLoggingMiddleware>();      // 3. Log with correlation ID
    app.UseMiddleware<RequestSizeLimitMiddleware>();    // 4. Reject oversized payloads
    app.UseCors("BillFlowPolicy");                     // 5. CORS
    app.UseRateLimiter();                              // 6. Rate limiting

    // 7. Health endpoints (handled by gateway — not proxied)
    app.MapHealthEndpoints();

    // 8. YARP proxy — handles all /api/* routes
    app.MapReverseProxy(pipeline =>
    {
        // Add rate limit policy filter inside YARP pipeline
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