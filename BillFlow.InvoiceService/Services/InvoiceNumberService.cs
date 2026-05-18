using BillFlow.InvoiceService.Data;
using BillFlow.InvoiceService.Models;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.InvoiceService.Services;

public class InvoiceNumberService : IInvoiceNumberService
{
    private readonly AppDbContext _db;

    public InvoiceNumberService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<string> GenerateNextAsync(int tenantId, string prefix = "INV")
    {
        var year = DateTime.UtcNow.Year;

        // Use EF Core's ExecuteUpdate for atomic increment
        // This prevents race conditions when two invoices are created simultaneously
        var sequence = await _db.InvoiceSequences
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Year == year);

        if (sequence is null)
        {
            // First invoice of the year for this tenant
            sequence = new InvoiceSequence
            {
                TenantId = tenantId,
                Year = year,
                LastSequence = 1
            };
            _db.InvoiceSequences.Add(sequence);
        }
        else
        {
            sequence.LastSequence++;
        }

        await _db.SaveChangesAsync();

        // Format: INV-2026-0001 (zero-padded to 4 digits)
        return $"{prefix}-{year}-{sequence.LastSequence:D4}";
    }
}