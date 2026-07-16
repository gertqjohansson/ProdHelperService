using ProdHelperService.Controllers.Interface;

namespace ProdHelperService;

/// <summary>
/// Runs the Azure Relay listener as a background service alongside Kestrel,
/// so the same process serves the Swagger-documented local API and the
/// relay-based production path at once. RelayListener itself is unchanged —
/// it still calls IControllerDispatcher directly, in-process.
/// </summary>
public class RelayListenerHostedService : BackgroundService
{
    private readonly RelayListener _relayListener;

    public RelayListenerHostedService(IConfiguration config, IControllerDispatcher dispatcher)
    {
        var relaySection = config.GetSection("Relay");

        string relayNamespace = relaySection["Namespace"]
            ?? throw new InvalidOperationException("Relay:Namespace is not configured.");
        string connectionName = relaySection["ConnectionName"]
            ?? throw new InvalidOperationException("Relay:ConnectionName is not configured.");
        string keyName = relaySection["KeyName"]
            ?? throw new InvalidOperationException("Relay:KeyName is not configured.");
        string key = relaySection["Key"]
            ?? throw new InvalidOperationException("Relay:Key is not configured.");

        _relayListener = new RelayListener(relayNamespace, connectionName, keyName, key, dispatcher);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => _relayListener.RunAsync(stoppingToken);
}
