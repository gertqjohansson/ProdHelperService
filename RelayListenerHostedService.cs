using ProdHelperService.Controllers.Interface;

namespace ProdHelperService;

/// <summary>
/// Runs the Azure Relay listener as a background service alongside Kestrel,
/// so the same process serves the Swagger-documented local API and the
/// relay-based production path at once. RelayListener dispatches Oee/Planner
/// through IControllerDispatcher in-process, and proxies Auth/* calls over
/// loopback HTTP to the same process's Kestrel-hosted AuthController.
/// </summary>
public class RelayListenerHostedService : BackgroundService
{
    private readonly RelayListener _relayListener;

    public RelayListenerHostedService(IConfiguration config, IControllerDispatcher dispatcher, IHttpClientFactory httpClientFactory)
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
        int localApiPort = config.GetValue("LocalApi:Port", 5080);
        string localApiBaseUrl = $"http://localhost:{localApiPort}";

        _relayListener = new RelayListener(relayNamespace, connectionName, keyName, key, dispatcher, httpClientFactory.CreateClient(), localApiBaseUrl);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => _relayListener.RunAsync(stoppingToken);
}
