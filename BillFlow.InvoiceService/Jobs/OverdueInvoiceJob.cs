using BillFlow.Contracts.Events;
using BillFlow.InvoiceService.Data;
using BillFlow.InvoiceService.Messaging;
using BillFlow.InvoiceService.Models;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.InvoiceService.Jobs;

public class OverdueInvoiceJob
{
    private readonly AppDbContext _db;
    private readonly IEventPublisher _publisher;
    private readonly ILogger<OverdueInvoiceJob> _logger;

    public OverdueInvoiceJob(
        AppDbContext db,
        IEventPublisher publisher,
        ILogger<OverdueInvoiceJob> logger)
    {
        _db = db;
        _publisher = publisher;
        _logger = logger;
    }

    /// <summary>
    /// Runs daily. Finds all Sent invoices past their due date,
    /// transitions them to Overdue, and publishes an event per invoice.
    /// </summary>
    public async Task ExecuteAsync()
    {
        var now = DateTime.UtcNow;

        _logger.LogInformation(
            "OverdueInvoiceJob started at {Time}", now);

        // Find all Sent invoices whose due date has passed
        var overdueInvoices = await _db.Invoices
            .Include(i => i.Events)
            .Where(i =>
                i.Status == InvoiceStatus.Sent &&
                i.DueDate.HasValue &&
                i.DueDate.Value < now)
            .ToListAsync();

        _logger.LogInformation(
            "Found {Count} overdue invoices", overdueInvoices.Count);

        var transitioned = 0;

        foreach (var invoice in overdueInvoices)
        {
            try
            {
                var daysOverdue = (int)(now - invoice.DueDate!.Value).TotalDays;

                // Apply status machine transition
                InvoiceStatusMachine.Transition(
                    invoice,
                    InvoiceStatus.Overdue,
                    $"Automatically marked overdue — {daysOverdue} day(s) past due date");

                await _db.SaveChangesAsync();

                // Publish event to RabbitMQ
                // NotificationService will pick this up and send a reminder email
                await _publisher.PublishAsync(
                    new InvoiceOverdueEvent(
                        invoice.Id,
                        invoice.TenantId,
                        string.Empty,       // TenantSlug added in Week 5
                        invoice.InvoiceNumber,
                        invoice.CustomerName,
                        invoice.CustomerEmail,
                        invoice.TotalAmount,
                        invoice.Currency,
                        invoice.DueDate!.Value,
                        daysOverdue
                    ),
                    routingKey: "invoice.overdue"
                );

                transitioned++;

                _logger.LogInformation(
                    "Invoice {Number} marked overdue ({Days} days past due)",
                    invoice.InvoiceNumber, daysOverdue);
            }
            catch (Exception ex)
            {
                // Log but continue — one failed invoice shouldn't stop the rest
                _logger.LogError(ex,
                    "Failed to process overdue invoice {Id}", invoice.Id);
            }
        }

        _logger.LogInformation(
            "OverdueInvoiceJob complete — {Count}/{Total} invoices transitioned",
            transitioned, overdueInvoices.Count);
    }
}