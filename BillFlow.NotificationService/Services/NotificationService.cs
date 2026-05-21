using System.Text.Json;
using BillFlow.Contracts.Events;
using BillFlow.NotificationService.Data;
using BillFlow.NotificationService.Models;
using BillFlow.NotificationService.Templates;

namespace BillFlow.NotificationService.Services;

public class NotificationService : INotificationService
{
    private readonly IEmailService _emailService;
    private readonly NotificationDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IEmailService emailService,
        NotificationDbContext db,
        IConfiguration config,
        ILogger<NotificationService> logger)
    {
        _emailService = emailService;
        _db = db;
        _config = config;
        _logger = logger;
    }

    public async Task HandleInvoiceCreatedAsync(InvoiceCreatedEvent evt)
    {
        var companyName = await GetCompanyNameAsync(evt.TenantSlug);

        var (subject, html) = EmailTemplates.InvoiceCreated(
            companyName,
            evt.CustomerName,
            evt.InvoiceNumber,
            evt.TotalAmount,
            evt.Currency,
            evt.CreatedAt);

        await SendAndLogAsync(
            tenantId: evt.TenantId,
            eventType: "InvoiceCreated",
            toEmail: evt.CustomerEmail,
            toName: evt.CustomerName,
            subject: subject,
            html: html,
            payload: JsonSerializer.Serialize(evt));
    }

    public async Task HandleInvoiceSentAsync(InvoiceSentEvent evt)
    {
        var companyName = await GetCompanyNameAsync(evt.TenantSlug);

        var (subject, html) = EmailTemplates.InvoiceSent(
            companyName,
            evt.CustomerName,
            evt.InvoiceNumber,
            evt.TotalAmount,
            evt.Currency,
            evt.DueDate);

        await SendAndLogAsync(
            tenantId: evt.TenantId,
            eventType: "InvoiceSent",
            toEmail: evt.CustomerEmail,
            toName: evt.CustomerName,
            subject: subject,
            html: html,
            payload: JsonSerializer.Serialize(evt));
    }

    public async Task HandleInvoiceOverdueAsync(InvoiceOverdueEvent evt)
    {
        var companyName = await GetCompanyNameAsync(evt.TenantSlug);

        var (subject, html) = EmailTemplates.InvoiceOverdue(
            companyName,
            evt.CustomerName,
            evt.InvoiceNumber,
            evt.TotalAmount,
            evt.Currency,
            evt.DueDate,
            evt.DaysOverdue);

        await SendAndLogAsync(
            tenantId: evt.TenantId,
            eventType: "InvoiceOverdue",
            toEmail: evt.CustomerEmail,
            toName: evt.CustomerName,
            subject: subject,
            html: html,
            payload: JsonSerializer.Serialize(evt));
    }

    public async Task HandleInvoicePaidAsync(InvoicePaidEvent evt)
    {
        var companyName = await GetCompanyNameAsync(evt.TenantSlug);

        var (subject, html) = EmailTemplates.InvoicePaid(
            companyName,
            evt.CustomerName,
            evt.InvoiceNumber,
            evt.TotalAmount,
            evt.Currency,
            evt.PaidAt);

        await SendAndLogAsync(
            tenantId: evt.TenantId,
            eventType: "InvoicePaid",
            toEmail: evt.CustomerEmail,
            toName: evt.CustomerName,
            subject: subject,
            html: html,
            payload: JsonSerializer.Serialize(evt));
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private async Task SendAndLogAsync(
        int tenantId,
        string eventType,
        string toEmail,
        string toName,
        string subject,
        string html,
        string payload)
    {
        var log = new NotificationLog
        {
            TenantId = tenantId,
            EventType = eventType,
            RecipientEmail = toEmail,
            RecipientName = toName,
            Subject = subject,
            EventPayload = payload,
            Status = NotificationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _db.NotificationLogs.Add(log);
        await _db.SaveChangesAsync();

        var success = await _emailService.SendAsync(toEmail, toName, subject, html);

        log.Status = success
            ? NotificationStatus.Sent
            : NotificationStatus.Failed;

        if (success)
            log.SentAt = DateTime.UtcNow;
        else
            log.ErrorMessage = "Email send failed — check SendGrid logs";

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Notification [{EventType}] for tenant [{TenantId}] → {Email}: {Status}",
            eventType, tenantId, toEmail, log.Status);
    }

    private async Task<string> GetCompanyNameAsync(string tenantSlug)
    {
        // Try to fetch branding from TenantService
        // Fall back to slug if unreachable
        try
        {
            using var http = new HttpClient();
            var tenantUrl = _config["ServiceUrls:TenantService"] ?? "http://localhost:5001";
            var response = await http.GetAsync($"{tenantUrl}/tenant/{tenantSlug}");

            if (!response.IsSuccessStatusCode)
                return tenantSlug;

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            return doc.RootElement
                .GetProperty("settings")
                .GetProperty("companyName")
                .GetString() ?? tenantSlug;
        }
        catch
        {
            return tenantSlug;
        }
    }
}