using System.IdentityModel.Tokens.Jwt;
using System.Text;
using BillFlow.Contracts.Tenancy;
using Microsoft.IdentityModel.Tokens;

namespace BillFlow.InvoiceService.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

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
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // Skip auth for public routes
        if (_publicRoutes.Any(r => path.StartsWith(r)))
        {
            await _next(context);
            return;
        }

        // Extract Bearer token from Authorization header
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            await RespondUnauthorized(context, "Missing or invalid Authorization header. Expected: Bearer <token>");
            return;
        }

        var token = authHeader["Bearer ".Length..].Trim();

        // Validate token and extract claims
        var claims = ValidateToken(context, token);

        if (claims is null)
        {
            await RespondUnauthorized(context, "Invalid or expired token");
            return;
        }

        // Read tenant claims embedded in the JWT
        var tenantIdClaim = claims.FirstOrDefault(c => c.Type == "tenantId")?.Value;
        var tenantSlug = claims.FirstOrDefault(c => c.Type == "tenantSlug")?.Value;
        var tenantName = claims.FirstOrDefault(c => c.Type == "fullName")?.Value ?? string.Empty;

        if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out var tenantId))
        {
            await RespondUnauthorized(context, "Token missing tenant claims");
            return;
        }

        // Resolve tenant settings (currency, tax rate) from TenantService
        // We still need settings — they aren't in the JWT because they can change
        var settings = await GetTenantSettingsAsync(context, tenantSlug!);

        // Populate scoped TenantContext — available everywhere in this request
        tenantContext.Resolve(
            tenantId,
            tenantSlug!,
            tenantName,
            settings?.Currency ?? "INR",
            settings?.DefaultTaxRate ?? 0.18m
        );

        _logger.LogInformation(
            "Authenticated user for tenant [{TenantId}] {Slug}",
            tenantId, tenantSlug);

        await _next(context);
    }

    private IEnumerable<System.Security.Claims.Claim>? ValidateToken(
        HttpContext context, string token)
    {
        var jwtSettings = context.RequestServices
            .GetRequiredService<JwtSettings>();

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings.SecretKey));

        var validationParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(
                token, validationParams, out _);
            return principal.Claims;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning("Token validation failed: {Message}", ex.Message);
            return null;
        }
    }

    private static async Task<TenantSettingsLookup?> GetTenantSettingsAsync(
        HttpContext context, string slug)
    {
        var factory = context.RequestServices
            .GetRequiredService<IHttpClientFactory>();

        var client = factory.CreateClient("TenantService");

        try
        {
            var response = await client.GetAsync($"/tenant/{slug}");
            if (!response.IsSuccessStatusCode) return null;

            var tenant = await response.Content
                .ReadFromJsonAsync<TenantLookupResult>();

            return tenant?.Settings;
        }
        catch (HttpRequestException ex)
        {
            var logger = context.RequestServices
                .GetRequiredService<ILogger<TenantMiddleware>>();
            logger.LogError(ex, "Failed to reach TenantService for '{Slug}'", slug);
            return null;
        }
    }

    private static async Task RespondUnauthorized(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new { error = message });
    }
}

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