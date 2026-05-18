using BillFlow.Contracts.Tenancy;
using BillFlow.InvoiceService.Data;
using BillFlow.InvoiceService.DTOs;
using BillFlow.InvoiceService.Models;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.InvoiceService.Services;

public class InvoiceService : IInvoiceService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IInvoiceNumberService _invoiceNumberService;
    private readonly ICustomerClient _customerClient;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        AppDbContext db,
        ITenantContext tenant,
        IInvoiceNumberService invoiceNumberService,
        ICustomerClient customerClient,
        ILogger<InvoiceService> logger)
    {
        _db = db;
        _tenant = tenant;
        _invoiceNumberService = invoiceNumberService;
        _customerClient = customerClient;
        _logger = logger;
    }

    public async Task<IEnumerable<InvoiceResponse>> GetAllAsync()
    {
        var invoices = await _db.Invoices
            .Include(i => i.LineItems)
            .Include(i => i.Events)
            .Where(i => i.TenantId == _tenant.TenantId)
            .AsNoTracking()
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        return invoices.Select(MapToResponse);
    }

    public async Task<InvoiceResponse?> GetByIdAsync(int id)
    {
        var invoice = await _db.Invoices
            .Include(i => i.LineItems)
            .Include(i => i.Events)
            .AsNoTracking()
            .FirstOrDefaultAsync(i =>
                i.Id == id && i.TenantId == _tenant.TenantId);

        return invoice is null ? null : MapToResponse(invoice);
    }

    public async Task<InvoiceResponse> CreateAsync(
        CreateInvoiceRequest request, string bearerToken)
    {
        if (request.LineItems is null || request.LineItems.Count == 0)
            throw new InvalidOperationException(
                "Invoice must have at least one line item");

        // Fetch customer snapshot from CustomerService
        var customer = await _customerClient
            .GetCustomerAsync(request.CustomerId, bearerToken)
            ?? throw new InvalidOperationException(
                $"Customer {request.CustomerId} not found");

        // Generate sequential invoice number for this tenant
        var invoiceNumber = await _invoiceNumberService
            .GenerateNextAsync(_tenant.TenantId);

        var lineItems = request.LineItems.Select(l => new InvoiceLineItem
        {
            Description = l.Description,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice
        }).ToList();

        var subTotal = lineItems.Sum(l => l.Amount);
        var taxAmount = Math.Round(subTotal * _tenant.DefaultTaxRate, 2);

        var invoice = new Invoice
        {
            TenantId = _tenant.TenantId,
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            CustomerEmail = customer.Email,
            CustomerGstNumber = customer.GstNumber,
            InvoiceNumber = invoiceNumber,
            Status = InvoiceStatus.Draft,
            SubTotal = subTotal,
            TaxRate = _tenant.DefaultTaxRate,
            TaxAmount = taxAmount,
            TotalAmount = subTotal + taxAmount,
            Currency = _tenant.Currency,
            Notes = request.Notes,
            DueDate = request.DueDate,
            LineItems = lineItems,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Record creation event
        invoice.Events.Add(new InvoiceEvent
        {
            FromStatus = "None",
            ToStatus = InvoiceStatus.Draft.ToString(),
            Note = "Invoice created",
            OccurredAt = DateTime.UtcNow
        });

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Tenant [{TenantId}] created invoice {Number} for customer {Customer}",
            _tenant.TenantId, invoice.InvoiceNumber, customer.Name);

        return MapToResponse(invoice);
    }

    public async Task<InvoiceResponse?> TransitionAsync(
        int id, TransitionRequest request)
    {
        var invoice = await _db.Invoices
            .Include(i => i.LineItems)
            .Include(i => i.Events)
            .FirstOrDefaultAsync(i =>
                i.Id == id && i.TenantId == _tenant.TenantId);

        if (invoice is null) return null;

        if (!Enum.TryParse<InvoiceStatus>(request.ToStatus, true, out var toStatus))
            throw new InvalidOperationException(
                $"Unknown status '{request.ToStatus}'. " +
                $"Valid values: {string.Join(", ", Enum.GetNames<InvoiceStatus>())}");

        // Status machine enforces valid transitions
        InvoiceStatusMachine.Transition(invoice, toStatus, request.Note);

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Invoice {Number} transitioned to {Status}",
            invoice.InvoiceNumber, toStatus);

        return MapToResponse(invoice);
    }

    public async Task CancelAsync(int id)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Events)
            .FirstOrDefaultAsync(i =>
                i.Id == id && i.TenantId == _tenant.TenantId);

        if (invoice is null) return;

        InvoiceStatusMachine.Transition(invoice, InvoiceStatus.Cancelled, "Cancelled by admin");
        await _db.SaveChangesAsync();
    }

    // ── Mapper ────────────────────────────────────────────────────────────

    private static InvoiceResponse MapToResponse(Invoice i) => new(
        i.Id,
        i.InvoiceNumber,
        i.CustomerId,
        i.CustomerName,
        i.CustomerEmail,
        i.CustomerGstNumber,
        i.SubTotal,
        i.TaxRate,
        i.TaxAmount,
        i.TotalAmount,
        i.Currency,
        i.Status.ToString(),
        i.Notes,
        i.CreatedAt,
        i.SentAt,
        i.PaidAt,
        i.DueDate,
        i.CancelledAt,
        i.LineItems.Select(l => new LineItemResponse(
            l.Id, l.Description, l.Quantity, l.UnitPrice, l.Amount
        )).ToList(),
        i.Events
            .OrderBy(e => e.OccurredAt)
            .Select(e => new InvoiceEventResponse(
                e.FromStatus, e.ToStatus, e.Note, e.OccurredAt
            )).ToList()
    );
}