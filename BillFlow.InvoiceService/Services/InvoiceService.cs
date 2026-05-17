using BillFlow.InvoiceService.Data;
using BillFlow.InvoiceService.Models;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.InvoiceService.Services;

public class InvoiceService : IInvoiceService
{
    private readonly AppDbContext _db;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(AppDbContext db, ILogger<InvoiceService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<Invoice>> GetAllAsync()
    {
        _logger.LogInformation("Fetching all invoices from database");

        // AsNoTracking = faster reads when you won't update the data
        // EF Core doesn't need to track changes for a simple list
        return await _db.Invoices
            .AsNoTracking()
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<Invoice?> GetByIdAsync(int id)
    {
        return await _db.Invoices
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);
        // Returns null if not found — controller handles the 404
    }

    public async Task<Invoice> CreateAsync(Invoice invoice)
    {
        invoice.CreatedAt = DateTime.UtcNow;

        _db.Invoices.Add(invoice);

        // SaveChangesAsync writes to SQL Server and populates invoice.Id
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Created invoice {InvoiceNumber} for {Customer}",
            invoice.InvoiceNumber,
            invoice.CustomerName);

        return invoice;
    }
}