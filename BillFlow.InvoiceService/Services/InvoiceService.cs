using BillFlow.Contracts.Tenancy;
using BillFlow.InvoiceService.Data;
using BillFlow.InvoiceService.Models;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.InvoiceService.Services;

public class InvoiceService : IInvoiceService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;   // ← injected
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        AppDbContext db,
        ITenantContext tenant,
        ILogger<InvoiceService> logger)
    {
        _db = db;
        _tenant = tenant;
        _logger = logger;
    }

    public async Task<IEnumerable<Invoice>> GetAllAsync()
    {
        return await _db.Invoices
            .Where(i => i.TenantId == _tenant.TenantId)  // ← tenant scoped
            .AsNoTracking()
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<Invoice?> GetByIdAsync(int id)
    {
        return await _db.Invoices
            .AsNoTracking()
            // BOTH conditions required — prevents tenant A fetching tenant B's invoice
            // by guessing an ID even if they somehow bypass the header
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == _tenant.TenantId);
    }

    public async Task<Invoice> CreateAsync(Invoice invoice)
    {
        // Always stamp with the current tenant — never trust the caller to set this
        invoice.TenantId = _tenant.TenantId;
        invoice.TaxRate = _tenant.DefaultTaxRate;  // use tenant's configured rate
        invoice.CreatedAt = DateTime.UtcNow;

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Tenant [{TenantId}] created invoice {Number}",
            _tenant.TenantId, invoice.InvoiceNumber);

        return invoice;
    }
}