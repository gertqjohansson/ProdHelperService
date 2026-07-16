namespace ProdHelperService.AdminApp;

internal static class Program
{
    // Must match ProdHelperService's LocalApi:Port (appsettings.json), which
    // hosts the same controllers this app calls, over plain HTTP.
    private const string LocalApiBaseUrl = "http://localhost:5080/";

    [STAThread]
    private static void Main()
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri(LocalApiBaseUrl) };

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm(httpClient));
    }
}
