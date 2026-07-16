using System.Globalization;

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

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm(httpClient, settings));
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
