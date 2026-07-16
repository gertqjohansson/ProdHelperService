using System.Text.Json;

namespace ProdHelperService.AdminApp;

// Decodes a JWT's payload claims for display purposes only — no signature
// verification (the token was already validated server-side; this is just
// reading `sub`/`email`/`displayName`/`amr` out of it client-side).
public record JwtClaims(string? Sub, string? Email, string? DisplayName, string[] Amr);

public static class JwtClaimsReader
{
    public static JwtClaims Read(string jwt)
    {
        string[] parts = jwt.Split('.');
        if (parts.Length < 2) return new JwtClaims(null, null, null, []);

        string payload = parts[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + ((4 - payload.Length % 4) % 4), '=');

        using JsonDocument doc = JsonDocument.Parse(Convert.FromBase64String(payload));
        JsonElement root = doc.RootElement;

        string? sub = root.TryGetProperty("sub", out var subEl) ? subEl.GetString() : null;
        string? email = root.TryGetProperty("email", out var emailEl) ? emailEl.GetString() : null;
        string? displayName = root.TryGetProperty("displayName", out var nameEl) ? nameEl.GetString() : null;

        string[] amr = [];
        if (root.TryGetProperty("amr", out var amrEl))
        {
            amr = amrEl.ValueKind switch
            {
                JsonValueKind.String => [amrEl.GetString()!],
                JsonValueKind.Array => [.. amrEl.EnumerateArray().Select(e => e.GetString()!)],
                _ => [],
            };
        }

        return new JwtClaims(sub, email, displayName, amr);
    }
}
