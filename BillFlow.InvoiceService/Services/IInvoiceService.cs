using BillFlow.InvoiceService.DTOs;
using BillFlow.InvoiceService.Models;

namespace BillFlow.InvoiceService.Services;

public interface IInvoiceService
{
    Task<IEnumerable<InvoiceResponse>> GetAllAsync();
    Task<InvoiceResponse?> GetByIdAsync(int id);
    Task<InvoiceResponse> CreateAsync(CreateInvoiceRequest request, string bearerToken);
    Task<InvoiceResponse?> TransitionAsync(int id, TransitionRequest request);
    Task CancelAsync(int id);
}