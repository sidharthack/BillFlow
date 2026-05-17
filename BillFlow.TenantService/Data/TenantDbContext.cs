using BillFlow.TenantService.Models;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.TenantService.Data;

public class TenantDbContext : DbContext
{
    public TenantDbContext(DbContextOptions<TenantDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantSettings> TenantSettings => Set<TenantSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("Tenants");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Slug)
                  .IsUnique();

            entity.HasIndex(e => e.OwnerEmail)
                  .IsUnique();

            entity.Property(e => e.Slug)
                  .HasMaxLength(100);

            entity.Property(e => e.Status)
                  .HasConversion<string>();

            entity.Property(e => e.Plan)
                  .HasConversion<string>();

            entity.HasOne(e => e.Settings)
                  .WithOne()
                  .HasForeignKey<TenantSettings>(s => s.TenantId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TenantSettings>(entity =>
        {
            entity.ToTable("TenantSettings");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.DefaultTaxRate)
                  .HasPrecision(5, 4);

            entity.Property(e => e.Currency)
                  .HasMaxLength(3);
        });
    }
}