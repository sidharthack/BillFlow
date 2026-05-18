using BillFlow.CustomerService.Models;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.CustomerService.Data;

public class CustomerDbContext : DbContext
{
    public CustomerDbContext(DbContextOptions<CustomerDbContext> options)
        : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers");
            entity.HasKey(e => e.Id);

            // Email unique per tenant — two tenants can have same customer email
            entity.HasIndex(e => new { e.TenantId, e.Email })
                  .IsUnique();

            entity.HasIndex(e => e.TenantId);

            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.GstNumber).HasMaxLength(15);
            entity.Property(e => e.PanNumber).HasMaxLength(10);

            entity.Property(e => e.Status)
                  .HasConversion<string>();

            entity.HasOne(e => e.Address)
                  .WithOne(a => a.Customer)
                  .HasForeignKey<CustomerAddress>(a => a.CustomerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CustomerAddress>(entity =>
        {
            entity.ToTable("CustomerAddresses");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PinCode).HasMaxLength(10);
        });
    }
}