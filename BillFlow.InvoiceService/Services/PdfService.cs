using BillFlow.Contracts.Tenancy;
using BillFlow.InvoiceService.Data;
using BillFlow.InvoiceService.Documents;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace BillFlow.InvoiceService.Services;

public class PdfService : IPdfService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PdfService> _logger;

    public PdfService(
        AppDbContext db,
        ITenantContext tenant,
        IHttpClientFactory httpClientFactory,
        ILogger<PdfService> logger)
    {
        _db = db;
        _tenant = tenant;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<byte[]> GenerateInvoicePdfAsync(int invoiceId)
    {
        // Fetch invoice with all related data
        var invoice = await _db.Invoices
            .Include(i => i.LineItems)
            .Include(i => i.Events)
            .AsNoTracking()
            .FirstOrDefaultAsync(i =>
                i.Id == invoiceId &&
                i.TenantId == _tenant.TenantId)
            ?? throw new InvalidOperationException(
                $"Invoice {invoiceId} not found");

        // Fetch tenant branding from TenantService
        var branding = await GetTenantBrandingAsync();

        // Map to DTO (reuse existing mapper logic)
        var invoiceDto = new DTOs.InvoiceResponse(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.CustomerId,
            invoice.CustomerName,
            invoice.CustomerEmail,
            invoice.CustomerGstNumber,
            invoice.SubTotal,
            invoice.TaxRate,
            invoice.TaxAmount,
            invoice.TotalAmount,
            invoice.Currency,
            invoice.Status.ToString(),
            invoice.Notes,
            invoice.CreatedAt,
            invoice.SentAt,
            invoice.PaidAt,
            invoice.DueDate,
            invoice.CancelledAt,
            invoice.LineItems.Select(l => new DTOs.LineItemResponse(
                l.Id, l.Description, l.Quantity, l.UnitPrice, l.Amount
            )).ToList(),
            invoice.Events
                .OrderBy(e => e.OccurredAt)
                .Select(e => new DTOs.InvoiceEventResponse(
                    e.FromStatus, e.ToStatus, e.Note, e.OccurredAt
                )).ToList()
        );

        // Generate PDF bytes
        var document = new InvoicePdfDocument(invoiceDto, branding);
        var pdfBytes = document.GeneratePdf();

        _logger.LogInformation(
            "Generated PDF for invoice {Number} ({Bytes} bytes)",
            invoice.InvoiceNumber, pdfBytes.Length);

        return pdfBytes;
    }

    private async Task<TenantBranding> GetTenantBrandingAsync()
    {
        var client = _httpClientFactory.CreateClient("TenantService");

        try
        {
            var response = await client.GetAsync($"/tenant/{_tenant.TenantSlug}");

            if (!response.IsSuccessStatusCode)
                return DefaultBranding();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await System.Text.Json.JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;

            var settings = root.GetProperty("settings");

            return new TenantBranding(
                settings.GetProperty("companyName").GetString()
                    ?? _tenant.TenantName,
                null,   // email and phone added in Week 5 when settings are extended
                null,
                settings.TryGetProperty("primaryColor", out var color)
                    ? color.GetString() ?? "#6366F1"
                    : "#6366F1"
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch tenant branding, using defaults");
            return DefaultBranding();
        }
    }

    private TenantBranding DefaultBranding() => new(
        _tenant.TenantName,
        null,
        null,
        "#6366F1"
    );
}