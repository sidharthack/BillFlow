using BillFlow.Contracts.Events;

namespace BillFlow.NotificationService.Services;

public interface INotificationService
{
    Task HandleInvoiceCreatedAsync(InvoiceCreatedEvent evt);
    Task HandleInvoiceSentAsync(InvoiceSentEvent evt);
    Task HandleInvoiceOverdueAsync(InvoiceOverdueEvent evt);
    Task HandleInvoicePaidAsync(InvoicePaidEvent evt);
}