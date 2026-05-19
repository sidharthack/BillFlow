namespace BillFlow.InvoiceService.Messaging;

public interface IEventPublisher
{
    Task PublishAsync<T>(T eventMessage, string routingKey) where T : class;
}