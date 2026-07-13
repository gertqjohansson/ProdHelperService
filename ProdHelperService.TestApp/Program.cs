using Microsoft.Extensions.DependencyInjection;
using ProdHelperService.Controllers;

namespace ProdHelperService.TestApp;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var services = new ServiceCollection();
        services.AddProdHelperControllers();
        services.AddSingleton<MainForm>();
        using var provider = services.BuildServiceProvider();

        ApplicationConfiguration.Initialize();
        Application.Run(provider.GetRequiredService<MainForm>());
    }
}
