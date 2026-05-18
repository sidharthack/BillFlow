using BillFlow.IdentityService.Data;
using BillFlow.IdentityService.DTOs;
using BillFlow.IdentityService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BillFlow.IdentityService.Services;

public class AuthService : IAuthService
{
    private readonly IdentityDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwtSettings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IdentityDbContext db,
        ITokenService tokenService,
        IOptions<JwtSettings> jwtSettings,
        IHttpClientFactory httpClientFactory,
        ILogger<AuthService> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _jwtSettings = jwtSettings.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Validate tenant exists via TenantService
        var tenant = await GetTenantAsync(request.TenantSlug)
            ?? throw new InvalidOperationException(
                $"Tenant '{request.TenantSlug}' not found or inactive");

        // Check email not already registered for this tenant
        var exists = await _db.Users.AnyAsync(u =>
            u.TenantId == tenant.Id &&
            u.Email == request.Email.ToLowerInvariant());

        if (exists)
            throw new InvalidOperationException(
                $"Email '{request.Email}' is already registered");

        var role = Enum.TryParse<UserRole>(request.Role, true, out var parsed)
            ? parsed
            : UserRole.Member;

        var user = new User
        {
            TenantId = tenant.Id,
            TenantSlug = tenant.Slug,
            Email = request.Email.ToLowerInvariant(),
            FullName = request.FullName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = role,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Registered user {Email} for tenant {Slug}",
            user.Email, user.TenantSlug);

        return await IssueTokensAsync(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u =>
                u.TenantSlug == request.TenantSlug &&
                u.Email == request.Email.ToLowerInvariant() &&
                u.IsActive);

        // Verify password — BCrypt handles timing-safe comparison
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password");

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "User {Email} logged in to tenant {Slug}",
            user.Email, user.TenantSlug);

        return await IssueTokensAsync(user);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken)
    {
        var stored = await _db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r =>
                r.Token == refreshToken &&
                !r.IsRevoked &&
                r.ExpiresAt > DateTime.UtcNow);

        if (stored is null)
            throw new UnauthorizedAccessException("Invalid or expired refresh token");

        // Rotate refresh token — revoke old, issue new
        stored.IsRevoked = true;
        await _db.SaveChangesAsync();

        return await IssueTokensAsync(stored.User);
    }

    public async Task RevokeAsync(string refreshToken)
    {
        var stored = await _db.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == refreshToken);

        if (stored is null) return;

        stored.IsRevoked = true;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Refresh token revoked");
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private async Task<AuthResponse> IssueTokensAsync(User user)
    {
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes);

        // Persist refresh token
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays)
        });
        await _db.SaveChangesAsync();

        return new AuthResponse(
            accessToken,
            refreshTokenValue,
            expiresAt,
            new UserInfo(
                user.Id,
                user.Email,
                user.FullName,
                user.Role.ToString(),
                user.TenantSlug,
                user.TenantId
            )
        );
    }

    private async Task<TenantLookup?> GetTenantAsync(string slug)
    {
        var client = _httpClientFactory.CreateClient("TenantService");
        try
        {
            var response = await client.GetAsync($"/tenant/{slug}");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<TenantLookup>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to reach TenantService for slug '{Slug}'", slug);
            return null;
        }
    }
}

internal record TenantLookup(int Id, string Slug, string Name, string Status);