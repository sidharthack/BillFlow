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
        var correlationId = context.Request.Headers[CorrelationIdHeader]
            .FirstOrDefault() ?? "unknown";

        var start = DateTime.UtcNow;

        _logger.LogInformation(
            "[{CorrelationId}] → {Method} {Path}{Query}",
            correlationId,
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString);

        await _next(context);

        var elapsed = (DateTime.UtcNow - start).TotalMilliseconds;

        _logger.LogInformation(
            "[{CorrelationId}] ← {StatusCode} {Method} {Path} in {Elapsed}ms",
            correlationId,
            context.Response.StatusCode,
            context.Request.Method,
            context.Request.Path,
            Math.Round(elapsed));
    }
}