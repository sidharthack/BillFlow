using BillFlow.TenantService.Data;
using BillFlow.TenantService.DTOs;
using BillFlow.TenantService.Models;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.TenantService.Services;

public class TenantService : ITenantService
{
    private readonly TenantDbContext _db;
    private readonly ILogger<TenantService> _logger;

    public TenantService(TenantDbContext db, ILogger<TenantService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<TenantResponse?> GetBySlugAsync(string slug)
    {
        var tenant = await _db.Tenants
            .Include(t => t.Settings)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == slug && t.Status == TenantStatus.Active);

        return tenant is null ? null : MapToResponse(tenant);
    }

    public async Task<TenantResponse?> GetByIdAsync(int id)
    {
        var tenant = await _db.Tenants
            .Include(t => t.Settings)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

        return tenant is null ? null : MapToResponse(tenant);
    }

    public async Task<IEnumerable<TenantResponse>> GetAllAsync()
    {
        var tenants = await _db.Tenants
            .Include(t => t.Settings)
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync();

        return tenants.Select(MapToResponse);
    }

    public async Task<TenantResponse> RegisterAsync(RegisterTenantRequest request)
    {
        var slug = GenerateSlug(request.Name);

        var finalSlug = await EnsureUniqueSlugAsync(slug);

        var tenant = new Tenant
        {
            Slug = finalSlug,
            Name = request.Name,
            OwnerEmail = request.OwnerEmail,
            Plan = TenantPlan.Starter,
            Status = TenantStatus.Active,
            CreatedAt = DateTime.UtcNow,
            Settings = new TenantSettings
            {
                CompanyName = request.CompanyName ?? request.Name,
                Currency = request.Currency,
                CountryCode = request.CountryCode,
                DefaultTaxRate = 0.18m,
                InvoicePrefix = "INV",
                InvoiceSequence = 1
            }
        };

        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Registered new tenant {Slug} for {Email}",
            tenant.Slug, tenant.OwnerEmail);

        return MapToResponse(tenant);
    }

    public async Task<bool> ExistsAsync(string slug)
    {
        return await _db.Tenants.AnyAsync(t => t.Slug == slug);
    }

    private static string GenerateSlug(string name)
    {
        return name
            .ToLowerInvariant()
            .Replace(" ", "-")
            .Replace(".", "")
            .Replace(",", "")
            .Replace("'", "")
            .Trim('-');
    }

    private async Task<string> EnsureUniqueSlugAsync(string baseSlug)
    {
        var slug = baseSlug;
        var counter = 1;

        while (await _db.Tenants.AnyAsync(t => t.Slug == slug))
        {
            slug = $"{baseSlug}-{counter++}";
        }

        return slug;
    }

    private static TenantResponse MapToResponse(Tenant t) => new(
        t.Id,
        t.Slug,
        t.Name,
        t.OwnerEmail,
        t.Plan.ToString(),
        t.Status.ToString(),
        t.CreatedAt,
        new TenantSettingsResponse(
            t.Settings.CompanyName,
            t.Settings.LogoUrl,
            t.Settings.PrimaryColor,
            t.Settings.Currency,
            t.Settings.CountryCode,
            t.Settings.DefaultTaxRate,
            t.Settings.InvoicePrefix
        )
    );
}