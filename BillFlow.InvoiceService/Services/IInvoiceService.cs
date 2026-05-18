using BillFlow.InvoiceService.Models;

namespace BillFlow.InvoiceService.Services;

public interface IInvoiceService
{
    Task<IEnumerable<Invoice>> GetAllAsync();
    Task<Invoice?> GetByIdAsync(int id);
    Task<Invoice> CreateAsync(Invoice invoice);
}