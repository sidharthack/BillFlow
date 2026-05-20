namespace BillFlow.Gateway.Middleware;

/// <summary>
/// Rejects requests whose Content-Length exceeds the configured limit.
/// Prevents large payload attacks without reading the body.
/// </summary>
public class RequestSizeLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestSizeLimitMiddleware> _logger;

    // Per-route limits in bytes
    private const long DefaultMaxBytes = 5 * 1024 * 1024;    // 5 MB
    private const long AuthMaxBytes = 10 * 1024;              // 10 KB (login payloads are tiny)
    private const long InvoiceMaxBytes = 1 * 1024 * 1024;    // 1 MB

    public RequestSizeLimitMiddleware(
        RequestDelegate next,
        ILogger<RequestSizeLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var contentLength = context.Request.ContentLength;

        if (contentLength.HasValue)
        {
            var limit = GetLimitForPath(context.Request.Path);

            if (contentLength.Value > limit)
            {
                _logger.LogWarning(
                    "Request to {Path} rejected — body {Size} bytes exceeds limit {Limit} bytes",
                    context.Request.Path, contentLength.Value, limit);

                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Payload too large",
                    maxAllowedBytes = limit,
                    receivedBytes = contentLength.Value
                });
                return;
            }
        }

        await _next(context);
    }

    private static long GetLimitForPath(PathString path)
    {
        var p = path.Value?.ToLowerInvariant() ?? string.Empty;

        if (p.StartsWith("/api/auth")) return AuthMaxBytes;
        if (p.StartsWith("/api/invoice")) return InvoiceMaxBytes;

        return DefaultMaxBytes;
    }
}