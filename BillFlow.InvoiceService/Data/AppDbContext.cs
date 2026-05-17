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

            entity.HasIndex(e => e.InvoiceNumber)
                  .IsUnique();

            entity.Property(e => e.Amount)
                  .HasPrecision(18, 2);

            entity.Property(e => e.TaxRate)
                  .HasPrecision(5, 4);

            entity.Ignore(e => e.TotalAmount);

            entity.Property(e => e.Status)
                  .HasConversion<string>();

            entity.HasData(
                new Invoice
                {
                    Id = 1,
                    InvoiceNumber = "INV-001",
                    CustomerName = "Acme Corp",
                    Amount = 50000,
                    Status = InvoiceStatus.Paid,
                    CreatedAt = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
                    PaidAt = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc)
                },
                new Invoice
                {
                    Id = 2,
                    InvoiceNumber = "INV-002",
                    CustomerName = "GlobalTech Ltd",
                    Amount = 120000,
                    Status = InvoiceStatus.Sent,
                    CreatedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Invoice
                {
                    Id = 3,
                    InvoiceNumber = "INV-003",
                    CustomerName = "StartupXYZ",
                    Amount = 25000,
                    Status = InvoiceStatus.Draft,
                    CreatedAt = new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        });
    }
}