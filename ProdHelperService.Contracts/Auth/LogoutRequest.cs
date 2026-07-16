namespace ProdHelperService.Contracts.Auth;

public class LogoutRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
