namespace BillFlow.InvoiceService.Services;

public interface IPdfService
{
    Task<byte[]> GenerateInvoicePdfAsync(int invoiceId);
}