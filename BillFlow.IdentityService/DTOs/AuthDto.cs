namespace BillFlow.IdentityService.DTOs;

public record RegisterRequest(
    string TenantSlug,
    string Email,
    string FullName,
    string Password,
    string Role = "Member"
);

public record LoginRequest(
    string TenantSlug,
    string Email,
    string Password
);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserInfo User
);

public record UserInfo(
    int Id,
    string Email,
    string FullName,
    string Role,
    string TenantSlug,
    int TenantId
);

public record RefreshTokenRequest(
    string RefreshToken
);