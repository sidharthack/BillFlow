using BillFlow.Contracts.Tenancy;

namespace BillFlow.InvoiceService.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    // Routes that don't require a tenant (public endpoints)
    private static readonly string[] _publicRoutes =
    [
        "/health",
        "/swagger",
        "/favicon.ico"
    ];

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        // Note: TenantContext is injected here (not in constructor) because
        // it's Scoped — constructor injection in middleware only works for
        // Singleton services. This pattern is called "method injection".

        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // Skip tenant resolution for public routes
        if (_publicRoutes.Any(r => path.StartsWith(r)))
        {
            await _next(context);
            return;
        }

        // Read tenant identifier from header
        // In Day 4 this becomes: read from JWT claim instead
        var tenantSlug = context.Request.Headers["X-Tenant-ID"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(tenantSlug))
        {
            _logger.LogWarning("Request to {Path} missing X-Tenant-ID header", path);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Missing X-Tenant-ID header",
                hint = "All API requests must include X-Tenant-ID: <tenant-slug>"
            });
            return;
        }

        // Look up tenant from TenantService
        // For now we call it directly via HTTP — in Week 4 this goes through the Gateway
        var tenant = await ResolveTenantAsync(context, tenantSlug);

        if (tenant is null)
        {
            _logger.LogWarning("Tenant '{Slug}' not found or inactive", tenantSlug);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                error = $"Tenant '{tenantSlug}' not found or inactive"
            });
            return;
        }

        // Populate the scoped TenantContext — now available everywhere in this request
        tenantContext.Resolve(
            tenant.Id,
            tenant.Slug,
            tenant.Name,
            tenant.Settings.Currency,
            tenant.Settings.DefaultTaxRate
        );

        _logger.LogInformation(
            "Request {Method} {Path} resolved to tenant [{Id}] {Slug}",
            context.Request.Method, path, tenant.Id, tenant.Slug);

        await _next(context);
    }

    private static async Task<TenantLookupResult?> ResolveTenantAsync(
        HttpContext context, string slug)
    {
        // Get the HttpClientFactory from the request's service provider
        var httpClientFactory = context.RequestServices
            .GetRequiredService<IHttpClientFactory>();

        var client = httpClientFactory.CreateClient("TenantService");

        try
        {
            // Call TenantService to validate and fetch tenant details
            var response = await client.GetAsync($"/tenant/{slug}");

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content
                .ReadFromJsonAsync<TenantLookupResult>();
        }
        catch (HttpRequestException ex)
        {
            var logger = context.RequestServices
                .GetRequiredService<ILogger<TenantMiddleware>>();

            logger.LogError(ex,
                "Failed to reach TenantService while resolving '{Slug}'", slug);

            return null;
        }
    }
}

// Local DTO matching TenantService's response shape
// We only map fields we actually need — not the full response
internal record TenantLookupResult(
    int Id,
    string Slug,
    string Name,
    string Status,
    TenantSettingsLookup Settings
);

internal record TenantSettingsLookup(
    string Currency,
    decimal DefaultTaxRate,
    string InvoicePrefix
);