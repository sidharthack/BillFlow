using Serilog.Core;
using Serilog.Events;

namespace BillFlow.Contracts.Logging;

/// <summary>
/// Reads the CorrelationId from the ambient context
/// and attaches it to every log event.
/// Services set this via CorrelationIdMiddleware.
/// </summary>
public class CorrelationIdEnricher : ILogEventEnricher
{
    private const string CorrelationIdProperty = "CorrelationId";

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory factory)
    {
        // Read from AsyncLocal — set by middleware on each request
        var correlationId = CorrelationContext.Current ?? "no-correlation";

        logEvent.AddPropertyIfAbsent(
            factory.CreateProperty(CorrelationIdProperty, correlationId));
    }
}

/// <summary>
/// Stores correlation ID per async context (per request).
/// AsyncLocal flows through async/await chains automatically.
/// </summary>
public static class CorrelationContext
{
    private static readonly AsyncLocal<string?> _current = new();

    public static string? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}