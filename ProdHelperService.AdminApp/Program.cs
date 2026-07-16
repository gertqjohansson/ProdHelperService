using System.Globalization;
using ProdHelperService.Contracts.Auth;

namespace ProdHelperService.AdminApp;

internal static class Program
{
    // Must match ProdHelperService's LocalApi:Port (appsettings.json), which
    // hosts the same controllers this app calls, over plain HTTP.
    private const string LocalApiBaseUrl = "http://localhost:5080/";

    [STAThread]
    private static async Task Main()
    {
        AppSettings settings = AppSettings.Load();
        ApplyCulture(settings.Culture);

        using var httpClient = new HttpClient { BaseAddress = new Uri(LocalApiBaseUrl) };
        var authApiClient = new AuthApiClient(httpClient);
        var session = new AuthSession();

        await TryRestoreSessionAsync(authApiClient, session, settings);

        ApplicationConfiguration.Initialize();

        // Loops back to LoginForm after a Log out from within MainForm - each
        // iteration opens a fresh MainForm only once LoginForm has fully
        // closed, so there's never a stale MainForm left open in the
        // background behind it.
        while (true)
        {
            if (!session.IsAuthenticated)
            {
                using var loginForm = new LoginForm(authApiClient, session, settings);
                if (loginForm.ShowDialog() != DialogResult.OK)
                {
                    // Closed without logging in - login is mandatory, so exit
                    // rather than opening MainForm.
                    return;
                }

                settings.EncryptedRefreshToken = TokenStore.Protect(session.RefreshToken);
                settings.Save();
            }

            using var mainForm = new MainForm(httpClient, settings, authApiClient, session);
            Application.Run(mainForm);

            if (session.IsAuthenticated)
            {
                // MainForm was closed directly (not via Log out) - exit.
                return;
            }
            // Else: Log out cleared the session and closed MainForm - loop
            // back around to LoginForm.
        }
    }

    // Silently exchanges a DPAPI-stored refresh token for a fresh session on
    // startup, so the user doesn't have to log in every launch. Any failure
    // (expired/revoked token, corrupt data, API unreachable) just leaves the
    // app logged out ("Guest") rather than blocking startup.
    private static async Task TryRestoreSessionAsync(AuthApiClient authApiClient, AuthSession session, AppSettings settings)
    {
        string? storedRefreshToken = TokenStore.Unprotect(settings.EncryptedRefreshToken);
        if (storedRefreshToken is null) return;

        try
        {
            TokenResponse refreshed = await authApiClient.RefreshAsync(new RefreshRequest { RefreshToken = storedRefreshToken });
            session.SetFromTokens(refreshed);
            settings.EncryptedRefreshToken = TokenStore.Protect(refreshed.RefreshToken);
            settings.Save();
        }
        catch
        {
            settings.EncryptedRefreshToken = null;
            settings.Save();
        }
    }

    private static void ApplyCulture(string cultureCode)
    {
        var culture = CultureInfo.GetCultureInfo(cultureCode);
        Thread.CurrentThread.CurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        // Safety net for any code path that doesn't flow ExecutionContext.
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }
}
