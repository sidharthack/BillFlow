namespace BillFlow.Gateway.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Use client-provided ID or generate a new one
        var correlationId = context.Request.Headers[CorrelationIdHeader]
            .FirstOrDefault()
            ?? Guid.NewGuid().ToString("N")[..12]; // short 12-char ID

        // Stamp it on the request so YARP forwards it to services
        context.Request.Headers[CorrelationIdHeader] = correlationId;

        // Stamp it on the response so clients can match request/response
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        await _next(context);
    }
}