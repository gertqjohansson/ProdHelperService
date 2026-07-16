namespace ProdHelperService.Auth;

// Raw refresh token values are never persisted — only a SHA-256 hash, the same
// principle as password hashing. TokenHash is the lookup key on Auth/Refresh.
public class RefreshToken
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public DateTime ExpiresUtc { get; set; }
    public DateTime? RevokedUtc { get; set; }

    // Links a rotated-out token to the token that replaced it, for reuse-detection audit.
    public string? ReplacedByTokenHash { get; set; }
    public string? CreatedByIp { get; set; }

    public bool IsActive => RevokedUtc is null && DateTime.UtcNow < ExpiresUtc;
}
