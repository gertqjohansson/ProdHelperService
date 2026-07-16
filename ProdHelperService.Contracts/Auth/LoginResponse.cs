namespace ProdHelperService.Contracts.Auth;

// Either MfaRequired is false and Tokens is populated (login complete), or
// MfaRequired is true and MfaToken is populated (call Auth/VerifyMfa next).
public class LoginResponse
{
    public bool MfaRequired { get; set; }
    public string? MfaToken { get; set; }
    public TokenResponse? Tokens { get; set; }
}
