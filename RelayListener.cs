using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Relay;

namespace RelayService;

/// <summary>
/// Opens an outbound connection to an Azure Relay Hybrid Connection and
/// handles incoming HTTP requests that are forwarded through it.
///
/// No inbound firewall/port-forwarding rule is needed on this machine:
/// the connection to Azure Relay is initiated FROM here, and Azure Relay
/// uses that existing connection to forward requests back down.
/// </summary>
public class RelayListener
{
    private readonly HybridConnectionListener _listener;

    public RelayListener(string relayNamespace, string connectionName, string keyName, string key)
    {
        var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(keyName, key);
        var uri = new Uri($"sb://{relayNamespace}/{connectionName}");
        _listener = new HybridConnectionListener(uri, tokenProvider);

        _listener.Connecting += (_, _) => Console.WriteLine("[Relay] Connecting...");
        _listener.Offline += (_, _) => Console.WriteLine("[Relay] Offline.");
        _listener.Online += (_, _) => Console.WriteLine("[Relay] Online and listening.");
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _listener.RequestHandler = HandleRequest;

        await _listener.OpenAsync(cancellationToken);

        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            // Expected on shutdown (Ctrl+C).
        }
        finally
        {
            await _listener.CloseAsync(CancellationToken.None);
            Console.WriteLine("Listener closed.");
        }
    }

    private void HandleRequest(RelayedHttpListenerContext context)
    {
        try
        {
            Console.WriteLine($"[Request] {context.Request.HttpMethod} {context.Request.Url}");

            // Allow the browser (running on a different origin, e.g. your
            // Azure Static Web App / App Service URL) to read the response.
            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            context.Response.Headers.Add("Content-Type", "application/json");

            var payload = new
            {
                message = "Hello from the on-premises .NET service!",
                machineName = Environment.MachineName,
                receivedAtUtc = DateTime.UtcNow.ToString("o"),
                requestPath = context.Request.Url?.AbsolutePath
            };

            string json = JsonSerializer.Serialize(payload);
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            context.Response.StatusCode = HttpStatusCode.OK;
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] {ex.Message}");
            context.Response.StatusCode = HttpStatusCode.InternalServerError;
        }
        finally
        {
            // The context MUST be closed for every request.
            context.Response.Close();
        }
    }
}
