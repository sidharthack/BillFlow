namespace BillFlow.InvoiceService.Models;

public class Invoice
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    // Link to CustomerService — we store a local snapshot of customer
    // info so the invoice is self-contained even if the customer changes
    public int CustomerId { get; set; }           // FK reference (logical, not DB constraint)
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerGstNumber { get; set; }

    // Invoice number: INV-2026-0001 format, sequential per tenant per year
    public string InvoiceNumber { get; set; } = string.Empty;

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    public decimal SubTotal { get; set; }         // sum of line items
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }        // SubTotal * TaxRate
    public decimal TotalAmount { get; set; }      // SubTotal + TaxAmount

    public string Currency { get; set; } = "INR";

    public string? Notes { get; set; }            // optional memo on the invoice

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? DueDate { get; set; }        // when payment is expected
    public DateTime? CancelledAt { get; set; }

    // Line items — each invoice has one or more
    public List<InvoiceLineItem> LineItems { get; set; } = [];

    // Audit trail — every status change is recorded
    public List<InvoiceEvent> Events { get; set; } = [];
}

public class InvoiceLineItem
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }

    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal Amount => Quantity * UnitPrice;  // computed, not stored

    public Invoice Invoice { get; set; } = null!;
}

public class InvoiceEvent
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }

    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public Invoice Invoice { get; set; } = null!;
}

// Invoice number sequence tracker — one row per tenant per year
public class InvoiceSequence
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int Year { get; set; }
    public int LastSequence { get; set; } = 0;
}

public enum InvoiceStatus
{
    Draft = 0,
    Sent = 1,
    Paid = 2,
    Overdue = 3,
    Cancelled = 4
}