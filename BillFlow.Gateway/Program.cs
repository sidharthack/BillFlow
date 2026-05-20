using System.Threading.RateLimiting;
using BillFlow.Gateway.Endpoints;
using BillFlow.Gateway.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ── YARP reverse proxy ────────────────────────────────────────────────────
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// ── Rate limiting ─────────────────────────────────────────────────────────
var rateLimitConfig = builder.Configuration.GetSection("RateLimiting");
var permitLimit = rateLimitConfig.GetValue<int>("PermitLimit", 100);
var windowSeconds = rateLimitConfig.GetValue<int>("WindowSeconds", 60);
var queueLimit = rateLimitConfig.GetValue<int>("QueueLimit", 10);

builder.Services.AddRateLimiter(options =>
{
    // Global fixed-window limiter — applies to all routes
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        // Partition by IP address — each client gets its own bucket
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: clientIp,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = queueLimit
            });
    });

    // Return 429 with helpful headers when limit is exceeded
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.Headers["Retry-After"] = windowSeconds.ToString();

        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Rate limit exceeded",
            retryAfterSeconds = windowSeconds,
            message = $"Maximum {permitLimit} requests per {windowSeconds}s window"
        }, cancellationToken);
    };
});

// ── HttpClient for health checks ──────────────────────────────────────────
builder.Services.AddHttpClient("HealthCheck", client =>
{
    client.Timeout = TimeSpan.FromSeconds(3);
});

// ── CORS — allow frontend apps to call the gateway ────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("BillFlowPolicy", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",   // React dev server
                "http://localhost:4200",   // Angular dev server
                "https://app.billflow.io"  // Production frontend
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────
// Order is critical — each layer wraps the next

app.UseMiddleware<CorrelationIdMiddleware>();    // 1. Stamp correlation ID
app.UseMiddleware<GatewayLoggingMiddleware>();   // 2. Log request/response
app.UseCors("BillFlowPolicy");                  // 3. CORS headers
app.UseRateLimiter();                           // 4. Rate limit check

// 5. Health endpoints (before YARP so they're handled by gateway)
app.MapHealthEndpoints();

// 6. YARP handles everything else — routes to downstream services
app.MapReverseProxy();

app.Run();