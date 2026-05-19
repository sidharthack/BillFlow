namespace BillFlow.InvoiceService.Services;

public interface ICustomerClient
{
    Task<CustomerSnapshot?> GetCustomerAsync(int customerId, string bearerToken);
}

// Only the fields InvoiceService needs — not the full customer record
public record CustomerSnapshot(
    int Id,
    string Name,
    string Email,
    string? GstNumber
);