namespace BillFlow.Contracts.Events;

/// <summary>
/// Published when an invoice is first created (Draft status).
/// </summary>
public record InvoiceCreatedEvent(
    int InvoiceId,
    int TenantId,
    string TenantSlug,
    string InvoiceNumber,
    string CustomerName,
    string CustomerEmail,
    decimal TotalAmount,
    string Currency,
    DateTime CreatedAt
);

/// <summary>
/// Published when an invoice is marked as Sent.
/// </summary>
public record InvoiceSentEvent(
    int InvoiceId,
    int TenantId,
    string TenantSlug,
    string InvoiceNumber,
    string CustomerName,
    string CustomerEmail,
    decimal TotalAmount,
    string Currency,
    DateTime? DueDate,
    DateTime SentAt
);

/// <summary>
/// Published when an invoice transitions to Overdue.
/// Triggered by the daily Hangfire job.
/// </summary>
public record InvoiceOverdueEvent(
    int InvoiceId,
    int TenantId,
    string TenantSlug,
    string InvoiceNumber,
    string CustomerName,
    string CustomerEmail,
    decimal TotalAmount,
    string Currency,
    DateTime DueDate,
    int DaysOverdue
);

/// <summary>
/// Published when payment is confirmed (Week 5 — Stripe webhook).
/// </summary>
public record InvoicePaidEvent(
    int InvoiceId,
    int TenantId,
    string TenantSlug,
    string InvoiceNumber,
    string CustomerName,
    string CustomerEmail,
    decimal TotalAmount,
    string Currency,
    DateTime PaidAt
);