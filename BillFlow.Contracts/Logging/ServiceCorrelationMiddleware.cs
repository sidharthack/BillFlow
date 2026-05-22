using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BillFlow.Contracts.Logging;

/// <summary>
/// Reads X-Correlation-ID forwarded by the Gateway and sets
/// CorrelationContext.Current so all logs in this request
/// carry the same correlation ID.
/// </summary>
public class ServiceCorrelationMiddleware
{
    private readonly RequestDelegate _next;
    private const string Header = "X-Correlation-ID";

    public ServiceCorrelationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[Header].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N")[..12];

        // Set on AsyncLocal — flows through all async calls in this request
        CorrelationContext.Current = correlationId;

        // Ensure it's on the response too
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[Header] = correlationId;
            return Task.CompletedTask;
        });

        // Push into Serilog LogContext so it appears on every log line
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}