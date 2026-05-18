namespace BillFlow.InvoiceService.Services;

public interface IInvoiceNumberService
{
    /// <summary>
    /// Generates the next sequential invoice number for the tenant.
    /// e.g. INV-2026-0001, INV-2026-0002 ...
    /// Thread-safe — uses a DB-level lock to prevent duplicates.
    /// </summary>
    Task<string> GenerateNextAsync(int tenantId, string prefix = "INV");
}
