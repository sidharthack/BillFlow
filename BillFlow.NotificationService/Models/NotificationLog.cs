namespace BillFlow.NotificationService.Models;

// Every email sent is logged here — audit trail + retry support
public class NotificationLog
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    public string EventType { get; set; } = string.Empty;  // "InvoiceCreated" etc.
    public string RecipientEmail { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;

    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; } = 0;

    // Store the event payload so we can retry without re-consuming
    public string EventPayload { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
}

public enum NotificationStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2,
    Skipped = 3   // e.g. SendGrid not configured in dev
}