using QRCoder;

namespace ProdHelperService.Auth;

// Builds the otpauth:// URI and QR code Microsoft/Google Authenticator scan to
// set up TOTP. `unformattedKey` must be the raw key from
// UserManager.GetAuthenticatorKeyAsync (already Base32, ready for `secret=`).
public static class TotpUriHelper
{
    public static string BuildAuthenticatorUri(string issuer, string email, string unformattedKey)
    {
        var otp = new PayloadGenerator.OneTimePassword
        {
            Secret = unformattedKey,
            Issuer = issuer,
            Label = email,
            Type = PayloadGenerator.OneTimePassword.OneTimePasswordAuthType.TOTP,
            Digits = 6,
        };
        return otp.ToString();
    }

    public static string GenerateQrCodePngBase64(string authenticatorUri)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(authenticatorUri, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        return Convert.ToBase64String(png.GetGraphic(20));
    }
}
