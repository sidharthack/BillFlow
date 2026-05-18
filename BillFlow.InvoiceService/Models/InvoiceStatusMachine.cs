namespace BillFlow.InvoiceService.Models;

public static class InvoiceStatusMachine
{
    // Valid transitions — the ONLY paths allowed
    private static readonly Dictionary<InvoiceStatus, InvoiceStatus[]> _allowed = new()
    {
        [InvoiceStatus.Draft] = [InvoiceStatus.Sent, InvoiceStatus.Cancelled],
        [InvoiceStatus.Sent] = [InvoiceStatus.Paid, InvoiceStatus.Overdue, InvoiceStatus.Cancelled],
        [InvoiceStatus.Overdue] = [InvoiceStatus.Paid, InvoiceStatus.Cancelled],
        [InvoiceStatus.Paid] = [],   // terminal — no transitions out of Paid
        [InvoiceStatus.Cancelled] = []    // terminal — no transitions out of Cancelled
    };

    /// <summary>
    /// Returns true if the transition from → to is valid.
    /// </summary>
    public static bool CanTransition(InvoiceStatus from, InvoiceStatus to)
        => _allowed.TryGetValue(from, out var allowed) && allowed.Contains(to);

    /// <summary>
    /// Applies the transition or throws if invalid.
    /// Updates the invoice fields appropriate to the new status.
    /// </summary>
    public static void Transition(Invoice invoice, InvoiceStatus to, string? note = null)
    {
        if (!CanTransition(invoice.Status, to))
            throw new InvalidOperationException(
                $"Cannot transition invoice from {invoice.Status} to {to}. " +
                $"Allowed: {string.Join(", ", _allowed[invoice.Status])}");

        var from = invoice.Status;

        // Record the event before changing status
        invoice.Events.Add(new InvoiceEvent
        {
            FromStatus = from.ToString(),
            ToStatus = to.ToString(),
            Note = note,
            OccurredAt = DateTime.UtcNow
        });

        // Apply status-specific side effects
        invoice.Status = to;
        invoice.UpdatedAt = DateTime.UtcNow;

        switch (to)
        {
            case InvoiceStatus.Sent:
                invoice.SentAt = DateTime.UtcNow;
                // Default due date: 30 days from send date
                invoice.DueDate ??= DateTime.UtcNow.AddDays(30);
                break;

            case InvoiceStatus.Paid:
                invoice.PaidAt = DateTime.UtcNow;
                break;

            case InvoiceStatus.Cancelled:
                invoice.CancelledAt = DateTime.UtcNow;
                break;
        }
    }
}