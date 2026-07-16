namespace ProdHelperService.Contracts.Auth;

public class VerifyMfaRequest
{
    public string MfaToken { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
