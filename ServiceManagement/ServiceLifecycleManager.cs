using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ProdHelperService.ServiceManagement;

// Changing the listen port can't be done live - Kestrel only binds once, at Build() time (see
// Program.cs). So a port change means: spawn a fresh instance of this same process on the new
// port, confirm it's actually serving before touching anything else, persist the new port for
// future manual restarts, and only then stop the current instance. There is no Windows Service or
// watchdog in this codebase, so self-relaunch is the only mechanism available.
public sealed class ServiceLifecycleManager(
    IConfiguration configuration,
    IHostApplicationLifetime lifetime,
    IHttpClientFactory httpClientFactory,
    ILogger<ServiceLifecycleManager> logger) : IServiceLifecycleManager
{
    private static readonly TimeSpan HealthCheckBudget = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan HealthCheckInterval = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan RestartGraceDelay = TimeSpan.FromMilliseconds(750);

    // Captured once at startup, same read Program.cs does - always reflects the port Kestrel
    // actually bound, independent of any later appsettings.json edits.
    public int CurrentPort { get; } = configuration.GetValue("LocalApi:Port", 5080);

    public async Task<PortUpdateResult> UpdatePortAsync(int newPort, CancellationToken cancellationToken)
    {
        if (newPort is < 1 or > 65535)
        {
            return PortUpdateResult.Failed("InvalidPort", "Port must be between 1 and 65535.");
        }

        if (newPort == CurrentPort)
        {
            return PortUpdateResult.Failed("SamePort", "The service is already running on that port.");
        }

        if (!TryReserveLoopbackPort(newPort))
        {
            return PortUpdateResult.Failed("PortInUse", $"Port {newPort} is already in use.");
        }

        Process process;
        try
        {
            process = Process.Start(BuildRelaunchStartInfo(newPort))!;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to relaunch ProdHelperService on port {Port}.", newPort);
            return PortUpdateResult.Failed("RelaunchFailed", "Could not start a new instance of the service.");
        }

        bool healthy = await WaitForNewInstanceAsync(newPort, cancellationToken);
        if (!healthy)
        {
            TryKill(process);
            return PortUpdateResult.Failed(
                "NewInstanceDidNotStart",
                $"The service did not come up on port {newPort} in time; the current instance keeps running.");
        }

        bool persisted = await TryPersistPortAsync(newPort);

        _ = Task.Run(async () =>
        {
            // Let the 200 OK response for this very call finish transmitting over
            // loopback before this process exits.
            await Task.Delay(RestartGraceDelay);
            lifetime.StopApplication();
        });

        return PortUpdateResult.Ok(newPort, persisted);
    }

    private static bool TryReserveLoopbackPort(int port)
    {
        try
        {
            var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    private static ProcessStartInfo BuildRelaunchStartInfo(int newPort)
    {
        string dllPath = Assembly.GetEntryAssembly()!.Location;
        string? processPath = Environment.ProcessPath;
        bool viaDotnetMuxer = processPath is not null
            && Path.GetFileNameWithoutExtension(processPath).Equals("dotnet", StringComparison.OrdinalIgnoreCase);

        string[] originalArgs = Environment.GetCommandLineArgs()
            .Skip(1)
            .Where(a => !a.StartsWith("--LocalApi:Port=", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var startInfo = new ProcessStartInfo
        {
            WorkingDirectory = AppContext.BaseDirectory,
            UseShellExecute = false,
        };

        if (viaDotnetMuxer)
        {
            startInfo.FileName = "dotnet";
            startInfo.ArgumentList.Add(dllPath);
        }
        else
        {
            startInfo.FileName = processPath ?? dllPath;
        }

        foreach (string arg in originalArgs)
        {
            startInfo.ArgumentList.Add(arg);
        }
        startInfo.ArgumentList.Add($"--LocalApi:Port={newPort}");

        return startInfo;
    }

    private async Task<bool> WaitForNewInstanceAsync(int port, CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(HealthCheckBudget);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        HttpClient client = httpClientFactory.CreateClient();

        while (!linkedCts.IsCancellationRequested)
        {
            try
            {
                using HttpResponseMessage response =
                    await client.GetAsync($"http://localhost:{port}/swagger/v1/swagger.json", linkedCts.Token);
                if (response.IsSuccessStatusCode) return true;
            }
            catch
            {
                // Not up yet - keep polling until the timeout budget elapses.
            }

            try
            {
                await Task.Delay(HealthCheckInterval, linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        return false;
    }

    private void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to terminate the relaunched process that did not come up in time.");
        }
    }

    private async Task<bool> TryPersistPortAsync(int newPort)
    {
        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            string json = await File.ReadAllTextAsync(path);
            JsonNode? root = JsonNode.Parse(json);
            if (root is null) return false;

            root["LocalApi"] ??= new JsonObject();
            root["LocalApi"]!["Port"] = newPort;

            await File.WriteAllTextAsync(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to persist new LocalApi:Port {Port} to appsettings.json.", newPort);
            return false;
        }
    }
}
