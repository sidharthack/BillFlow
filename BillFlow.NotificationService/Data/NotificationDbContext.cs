using BillFlow.NotificationService.Models;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.NotificationService.Data;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(
        DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<NotificationLog>(entity =>
        {
            entity.ToTable("NotificationLogs");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.Status);

            entity.Property(e => e.Status)
                  .HasConversion<string>();

            entity.Property(e => e.RecipientEmail)
                  .HasMaxLength(256);

            entity.Property(e => e.Subject)
                  .HasMaxLength(512);
        });
    }
}