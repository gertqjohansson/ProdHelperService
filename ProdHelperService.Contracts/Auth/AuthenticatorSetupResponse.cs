namespace ProdHelperService.Contracts.Auth;

public class AuthenticatorSetupResponse
{
    public string SharedKey { get; set; } = string.Empty;
    public string AuthenticatorUri { get; set; } = string.Empty;

    // Base64-encoded PNG of a QR code for AuthenticatorUri, ready for an <img src="data:image/png;base64,...">.
    public string QrCodePngBase64 { get; set; } = string.Empty;
}
