using System.Security.Cryptography;
using System.Text;

namespace ProdHelperService.AdminApp;

// Encrypts the refresh token with Windows DPAPI (current-user scope) before
// it's persisted in AppSettings.json — only the same Windows account on the
// same machine can decrypt it, unlike storing it as plain text.
public static class TokenStore
{
    private static readonly byte[] Entropy = "ProdHelperService.AdminApp.RefreshToken"u8.ToArray();

    public static string? Protect(string? refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken)) return null;
        byte[] plainBytes = Encoding.UTF8.GetBytes(refreshToken);
        byte[] encryptedBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encryptedBytes);
    }

    public static string? Unprotect(string? encrypted)
    {
        if (string.IsNullOrEmpty(encrypted)) return null;
        try
        {
            byte[] encryptedBytes = Convert.FromBase64String(encrypted);
            byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
            // Corrupt, foreign-machine, or foreign-user data — treat as absent
            // rather than crash startup.
            return null;
        }
    }
}
