namespace ProdHelperService.Auth;

public interface ITokenService
{
    TimeSpan RefreshTokenLifetime { get; }

    (string AccessToken, DateTime ExpiresUtc) CreateAccessToken(
        ApplicationUser user, IEnumerable<string> roles, IEnumerable<string> amr);

    string GenerateRefreshToken();

    string HashToken(string rawToken);
}
