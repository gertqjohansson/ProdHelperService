using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Relay;
using ProdHelperService.Controllers.Interface;

namespace ProdHelperService;

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
    private readonly string _connectionName;
    private readonly IControllerDispatcher _dispatcher;

    public RelayListener(string relayNamespace, string connectionName, string keyName, string key, IControllerDispatcher dispatcher)
    {
        _connectionName = connectionName;
        _dispatcher = dispatcher;

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
        HttpStatusCode statusCode = HttpStatusCode.OK;
        object payload;

        try
        {
            Console.WriteLine($"[Request] {context.Request.HttpMethod} {context.Request.Url}");

            // Allow the browser (running on a different origin, e.g. your
            // Azure Web App URL) to read the response.
            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            context.Response.Headers.Add("Content-Type", "application/json");

            // The request URL looks like https://{namespace}/{hcName}/{Controller}/{Function}
            // (Azure Relay forwards the full path, connection name included).
            string[] segments = (context.Request.Url?.AbsolutePath ?? string.Empty)
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            // Drop the hybrid connection name if it's the first segment, so
            // this works whether or not Relay includes it in what we see.
            if (segments.Length > 0 && string.Equals(segments[0], _connectionName, StringComparison.OrdinalIgnoreCase))
            {
                segments = segments[1..];
            }

            if (segments.Length >= 2)
            {
                string controller = segments[0];
                string function = segments[1];
                string[] parameters = ReadParameters(context);

                Console.WriteLine($"[Dispatch] {controller}/{function} Parameters=[{string.Join(", ", parameters)}]");

                if (_dispatcher.TryInvoke(controller, function, parameters, out var result))
                {
                    payload = result!;
                }
                else
                {
                    statusCode = HttpStatusCode.NotFound;
                    payload = new { ok = false, error = $"No controller/function registered for '{controller}/{function}'." };
                }
            }
            else
            {
                payload = new
                {
                    message = "Hello from the on-premises .NET service! Call it as /{Controller}/{Function}.",
                    machineName = Environment.MachineName,
                    receivedAtUtc = DateTime.UtcNow.ToString("o"),
                    requestPath = context.Request.Url?.AbsolutePath
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] {ex.Message}");
            statusCode = HttpStatusCode.InternalServerError;
            payload = new { ok = false, error = ex.Message };
        }

        try
        {
            string json = JsonSerializer.Serialize(payload);
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            context.Response.StatusCode = statusCode;
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
        }
        finally
        {
            // The context MUST be closed for every request.
            context.Response.Close();
        }
    }

    private static string[] ReadParameters(RelayedHttpListenerContext context)
    {
        using var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
        string body = reader.ReadToEnd();

        if (string.IsNullOrWhiteSpace(body))
        {
            return Array.Empty<string>();
        }

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.TryGetProperty("Parameters", out var p)
            ? p.EnumerateArray().Select(e => e.ToString()).ToArray()
            : Array.Empty<string>();
    }
}
