namespace BillFlow.TenantService.Models;

public class Tenant
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public TenantPlan Plan { get; set; } = TenantPlan.Starter;
    public TenantStatus Status { get; set; } = TenantStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SuspendedAt { get; set; }
    public TenantSettings Settings { get; set; } = new();
}

public class TenantSettings
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string PrimaryColor { get; set; } = "#6366F1";
    public string Currency { get; set; } = "INR";
    public string CountryCode { get; set; } = "IN";
    public decimal DefaultTaxRate { get; set; } = 0.18m;
    public string InvoicePrefix { get; set; } = "INV";
    public int InvoiceSequence { get; set; } = 1;
}
public enum TenantPlan
{
    Starter = 0,
    Growth = 1,
    Enterprise = 2
}

public enum TenantStatus
{
    Active = 0,
    Suspended = 1,
    Cancelled = 2
}