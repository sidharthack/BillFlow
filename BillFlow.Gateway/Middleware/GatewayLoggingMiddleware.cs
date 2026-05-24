using Serilog.Context;

namespace BillFlow.Gateway.Middleware;

public class GatewayLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GatewayLoggingMiddleware> _logger;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public GatewayLoggingMiddleware(
        RequestDelegate next,
        ILogger<GatewayLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"]
        .FirstOrDefault() ?? "unknown";

        var start = DateTime.UtcNow;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            _logger.LogInformation(
                "→ {Method} {Path}{Query}",
                context.Request.Method,
                context.Request.Path,
                context.Request.QueryString);

            await _next(context);

            var elapsed = (DateTime.UtcNow - start).TotalMilliseconds;

            _logger.LogInformation(
                "← {StatusCode} {Method} {Path} in {Elapsed}ms",
                context.Response.StatusCode,
                context.Request.Method,
                context.Request.Path,
                Math.Round(elapsed));
        }
    }
}