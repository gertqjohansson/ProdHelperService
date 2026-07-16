using ProdHelperService.Contracts.Auth;

namespace ProdHelperService.AdminApp;

// In-memory holder for the current login session — not persisted directly;
// MainForm rebuilds its user menu from this after any auth state change.
// The refresh token is what actually gets persisted (DPAPI-encrypted, see
// TokenStore) so the app can restore a session silently on next launch.
public class AuthSession
{
    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public string? Email { get; private set; }
    public string? DisplayName { get; private set; }
    public bool MfaEnabled { get; private set; }

    public bool IsAuthenticated => AccessToken is not null;

    public void SetFromTokens(TokenResponse tokens)
    {
        AccessToken = tokens.AccessToken;
        RefreshToken = tokens.RefreshToken;

        JwtClaims claims = JwtClaimsReader.Read(tokens.AccessToken);
        Email = claims.Email;
        DisplayName = claims.DisplayName;
        MfaEnabled = claims.Amr.Contains("mfa");
    }

    public void Clear()
    {
        AccessToken = null;
        RefreshToken = null;
        Email = null;
        DisplayName = null;
        MfaEnabled = false;
    }
}
