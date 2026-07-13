using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProdHelperService.Controllers;
using ProdHelperService.Controllers.Interface;

namespace ProdHelperService;

public class Program
{
    public static async Task Main(string[] args)
    {
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddEnvironmentVariables(prefix: "RELAY_")
            .Build();

        var relaySection = config.GetSection("Relay");

        string relayNamespace = relaySection["Namespace"]
            ?? throw new InvalidOperationException("Relay:Namespace is not configured.");
        string connectionName = relaySection["ConnectionName"]
            ?? throw new InvalidOperationException("Relay:ConnectionName is not configured.");
        string keyName = relaySection["KeyName"]
            ?? throw new InvalidOperationException("Relay:KeyName is not configured.");
        string key = relaySection["Key"]
            ?? throw new InvalidOperationException("Relay:Key is not configured.");

        var services = new ServiceCollection();
        services.AddProdHelperControllers();
        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IControllerDispatcher>();

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        var listener = new RelayListener(relayNamespace, connectionName, keyName, key, dispatcher);

        Console.WriteLine("=== Azure Relay Hybrid Connection Service ===");
        Console.WriteLine($"Namespace : {relayNamespace}");
        Console.WriteLine($"Connection: {connectionName}");
        Console.WriteLine("Press Ctrl+C to stop.");
        Console.WriteLine();

        await listener.RunAsync(cts.Token);
    }
}
