namespace BillFlow.Contracts.Tenancy;

/// <summary>
/// Provides the current tenant identity for the duration of an HTTP request.
/// Injected via DI — available in any service, controller, or repository.
/// </summary>
public interface ITenantContext
{
    /// <summary>The numeric tenant ID used for DB filtering.</summary>
    int TenantId { get; }

    /// <summary>The human-readable slug e.g. "acme-corp".</summary>
    string TenantSlug { get; }

    /// <summary>Display name e.g. "Acme Corporation".</summary>
    string TenantName { get; }

    /// <summary>ISO currency code e.g. "INR".</summary>
    string Currency { get; }

    /// <summary>Default tax rate for this tenant e.g. 0.18</summary>
    decimal DefaultTaxRate { get; }

    /// <summary>True when a tenant has been successfully resolved.</summary>
    bool IsResolved { get; }
}