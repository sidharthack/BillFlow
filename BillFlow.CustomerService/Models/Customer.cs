namespace BillFlow.CustomerService.Models;

public class Customer
{
    public int Id { get; set; }

    // Row-level tenant isolation — same pattern as InvoiceService
    public int TenantId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }
    public string? GstNumber { get; set; }  // Indian GST registration number
    public string? PanNumber { get; set; }  // PAN for tax compliance

    public CustomerStatus Status { get; set; } = CustomerStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation to address
    public CustomerAddress? Address { get; set; }
}

public class CustomerAddress
{
    public int Id { get; set; }
    public int CustomerId { get; set; }

    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PinCode { get; set; } = string.Empty;
    public string Country { get; set; } = "India";

    public Customer Customer { get; set; } = null!;
}

public enum CustomerStatus
{
    Active = 0,
    Inactive = 1,
    Blocked = 2
}