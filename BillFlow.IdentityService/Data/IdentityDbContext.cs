using BillFlow.IdentityService.Models;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.IdentityService.Data;

public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);

            // Email must be unique per tenant
            entity.HasIndex(e => new { e.TenantId, e.Email })
                  .IsUnique();

            entity.Property(e => e.Email)
                  .HasMaxLength(256);

            entity.Property(e => e.Role)
                  .HasConversion<string>();

            // One user has many refresh tokens
            entity.HasMany(e => e.RefreshTokens)
                  .WithOne(r => r.User)
                  .HasForeignKey(r => r.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
        });
    }
}