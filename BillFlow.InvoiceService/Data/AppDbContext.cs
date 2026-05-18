using BillFlow.InvoiceService.Models;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.InvoiceService.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    public DbSet<Invoice> Invoices => Set<Invoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.ToTable("Invoices");
            entity.HasKey(e => e.Id);

            // ← ADD: index on TenantId — every query filters by this
            entity.HasIndex(e => e.TenantId);

            // ← ADD: composite unique index — same invoice number can exist
            // across tenants, but must be unique within a tenant
            entity.HasIndex(e => new { e.TenantId, e.InvoiceNumber })
                  .IsUnique();

            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.TaxRate).HasPrecision(5, 4);
            entity.Ignore(e => e.TotalAmount);
            entity.Property(e => e.Status).HasConversion<string>();
        });
    }
}