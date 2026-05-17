namespace BillFlow.Contracts.Tenancy;

/// <summary>
/// Mutable implementation — set once by middleware, then read-only for
/// the rest of the request. Registered as Scoped so each request gets
/// its own instance.
/// </summary>
public class TenantContext : ITenantContext
{
    public int TenantId { get; private set; }
    public string TenantSlug { get; private set; } = string.Empty;
    public string TenantName { get; private set; } = string.Empty;
    public string Currency { get; private set; } = "INR";
    public decimal DefaultTaxRate { get; private set; } = 0.18m;
    public bool IsResolved { get; private set; }

    /// <summary>
    /// Called once by TenantMiddleware after the tenant is validated.
    /// Nothing else should call this.
    /// </summary>
    public void Resolve(
        int tenantId,
        string slug,
        string name,
        string currency,
        decimal defaultTaxRate)
    {
        TenantId = tenantId;
        TenantSlug = slug;
        TenantName = name;
        Currency = currency;
        DefaultTaxRate = defaultTaxRate;
        IsResolved = true;
    }
}