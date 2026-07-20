using System.Globalization;
using System.Threading;
using ProdHelperService.Contracts.Auth;
using ProdHelperService.Contracts.Service;
using ProdHelperService.ServiceManagement;

namespace ProdHelperService.AdminApp;

internal static class Program
{
    // Must match ProdHelperService's LocalApi:Port (appsettings.json), which
    // hosts the same controllers this app calls, over plain HTTP.
    private const string LocalApiBaseUrl = "http://localhost:5080/";

    [STAThread]
    private static void Main()
    {
        AppSettings settings = AppSettings.Load();
        ApplyCulture(settings.Culture);

        using var httpClient = new HttpClient { BaseAddress = new Uri(LocalApiBaseUrl) };
        var authApiClient = new AuthApiClient(httpClient);
        var serviceApiClient = new ServiceApiClient(httpClient);
        var windowsServiceInstaller = new WindowsServiceInstaller();
        var session = new AuthSession();

        ApplicationConfiguration.Initialize();

        if (!EnsureServiceRegistered(windowsServiceInstaller))
        {
            return;
        }

        if (!EnsureServiceReachable(serviceApiClient, windowsServiceInstaller, settings))
        {
            return;
        }

        TryRestoreSession(authApiClient, session, settings);

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

            using var mainForm = new MainForm(httpClient, settings, authApiClient, serviceApiClient, windowsServiceInstaller, session);
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

    // Asks (before any login) whether to register ProdHelperService as a Windows Service, if it
    // isn't already. Checks and registers directly against the local Windows Service Control
    // Manager (via IWindowsServiceInstaller) rather than over HTTP through ProdHelperService's own
    // API - registration is exactly what's needed when that service isn't running yet, so gating
    // the check on the backend being reachable would defeat the point (and previously crashed the
    // app outright: an unreachable backend throws HttpRequestException, which nothing caught).
    // Only Windows admin rights are required, via this app's own elevation (see app.manifest) - no
    // login needed. Returns false to signal Main() should exit immediately (user declined); true
    // to continue the normal startup flow either way otherwise.
    //
    // Deliberately synchronous (not async), and every Task this calls is blocked on immediately via
    // .GetAwaiter().GetResult() rather than awaited - Main() runs this before Application.Run() has
    // ever started a message loop, and a real await suspension here would resume its continuation
    // on a thread-pool thread that was never [STAThread]-initialized, breaking anything downstream
    // that needs genuine COM/OLE (FolderBrowserDialog, Clipboard). Blocking is safe here since
    // nothing else needs this thread responsive yet - there's no message loop to keep pumping.
    private static bool EnsureServiceRegistered(IWindowsServiceInstaller windowsServiceInstaller)
    {
        ServiceRegistrationState state = windowsServiceInstaller.GetStatus();
        if (state != ServiceRegistrationState.NotRegistered) return true;

        DialogResult choice = MessageBox.Show(
            Strings.ServiceNotRegisteredStartupMessage,
            Strings.ServiceDialogTitle,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (choice != DialogResult.Yes) return false;

        // ProdHelperService is installed as a sibling folder next to this app (see the Setup
        // project's Inno Setup script) - this app resolves that path itself rather than asking the
        // backend for it, since the backend isn't necessarily running to ask. Falls back to the
        // dev source-tree layout (ProdHelperService only ever run via `dotnet run`/F5, in either
        // Debug or Release) so this also works when testing locally, not just once installed.
        string? binPath = AdminAppPaths.ResolveProdHelperServiceFile(
            Path.Combine("..", "ProdHelperService", "ProdHelperService.exe"),
            Path.Combine("..", "..", "..", "..", "bin", "Debug", "net8.0", "ProdHelperService.exe"),
            Path.Combine("..", "..", "..", "..", "bin", "Release", "net8.0", "ProdHelperService.exe"));
        if (binPath is null)
        {
            MessageBox.Show(
                "Could not find ProdHelperService.exe.",
                Strings.ServiceDialogTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return true;
        }

        ServiceOperationResult result = windowsServiceInstaller.RegisterAsync(binPath, CancellationToken.None).GetAwaiter().GetResult();
        if (!result.Success)
        {
            MessageBox.Show(result.ErrorMessage, Strings.ServiceDialogTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        return true;
    }

    // Confirms ProdHelperService's HTTP API is actually answering before proceeding to login -
    // EnsureServiceRegistered above only confirms the Windows Service entry exists, not that
    // it's running/listening/configured correctly (wrong port, bad Relay config, placeholder
    // secrets straight after a fresh install, etc. would all leave the Windows Service "Running"
    // per the SCM while the API itself is unreachable or erroring). If unreachable, gives the user
    // a chance to fix configuration and/or start the service via ServiceConfigForm, then re-checks
    // exactly once - no retry loop, matching this file's existing "never became available -> exit"
    // pattern used above and in the login loop below.
    private static bool EnsureServiceReachable(
        ServiceApiClient serviceApiClient, IWindowsServiceInstaller windowsServiceInstaller, AppSettings settings)
    {
        if (IsReachable(serviceApiClient)) return true;

        using var configForm = new ServiceConfigForm(serviceApiClient, windowsServiceInstaller, settings);
        configForm.ShowDialog();

        return IsReachable(serviceApiClient);
    }

    private static bool IsReachable(ServiceApiClient serviceApiClient)
    {
        try
        {
            serviceApiClient.IamAliveAsync(null).GetAwaiter().GetResult();
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or AuthApiException)
        {
            return false;
        }
    }

    // Silently exchanges a DPAPI-stored refresh token for a fresh session on
    // startup, so the user doesn't have to log in every launch. Any failure
    // (expired/revoked token, corrupt data, API unreachable) just leaves the
    // app logged out ("Guest") rather than blocking startup.
    private static void TryRestoreSession(AuthApiClient authApiClient, AuthSession session, AppSettings settings)
    {
        string? storedRefreshToken = TokenStore.Unprotect(settings.EncryptedRefreshToken);
        if (storedRefreshToken is null) return;

        try
        {
            TokenResponse refreshed = authApiClient.RefreshAsync(new RefreshRequest { RefreshToken = storedRefreshToken }).GetAwaiter().GetResult();
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
