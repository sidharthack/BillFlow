namespace BillFlow.IdentityService.Models;

public class User
{
    public int Id { get; set; }

    // Which tenant this user belongs to
    public int TenantId { get; set; }
    public string TenantSlug { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    // Never store plain text — always BCrypt hashed
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Member;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    // Navigation to refresh tokens
    public List<RefreshToken> RefreshTokens { get; set; } = [];
}

public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }

    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRevoked { get; set; }

    // Navigation back to user
    public User User { get; set; } = null!;
}

public enum UserRole
{
    Admin = 0,   // full access to tenant's data
    Member = 1,  // read + create invoices
    Viewer = 2   // read only
}