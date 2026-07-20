using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Win32;
using SysServiceController = System.ServiceProcess.ServiceController;

namespace ProdHelperService.ServiceManagement;

// Installs/uninstalls/starts/stops this same process (see Program.cs's UseWindowsService()) as a
// Windows Service. .NET has no managed service-install API, so Register/Unregister shell out to
// sc.exe - the standard approach - while Start/Stop/GetStatus use the real
// System.ServiceProcess.ServiceController (aliased here since the ASP.NET controller in this same
// namespace is also called ServiceController).
//
// Lives in its own project (rather than compiled into the main ProdHelperService assembly) so that
// ProdHelperService.AdminApp can call GetStatus()/RegisterAsync() directly, in-process, without
// depending on ProdHelperService's own HTTP API being reachable - the exact situation AdminApp
// needs to handle at first-run, before the service is registered (and possibly not running at
// all).
public sealed class WindowsServiceInstaller : IWindowsServiceInstaller
{
    public const string ServiceName = "ProdHelperService";
    private const string DisplayName = "ProdHelper Service";

    private static readonly TimeSpan StatusChangeTimeout = TimeSpan.FromSeconds(15);

    public ServiceRegistrationState GetStatus()
    {
        try
        {
            using var controller = new SysServiceController(ServiceName);
            return ToState(controller.Status);
        }
        catch (InvalidOperationException)
        {
            // Thrown when the service isn't registered at all.
            return ServiceRegistrationState.NotRegistered;
        }
    }

    // Reads the SCM's own record of what this service is registered to run - the actual source of
    // truth, rather than guessing an install layout. ImagePath is whatever was passed to
    // `sc create binPath=`, optionally double-quoted by the SCM.
    public string? GetBinaryPath()
    {
        using RegistryKey? key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{ServiceName}");
        string? imagePath = key?.GetValue("ImagePath") as string;
        return imagePath?.Trim().Trim('"');
    }

    public async Task<ServiceOperationResult> RegisterAsync(string binPath, CancellationToken cancellationToken)
    {
        if (GetStatus() != ServiceRegistrationState.NotRegistered)
        {
            return ServiceOperationResult.Failed("AlreadyRegistered", "The service is already registered.");
        }

        (int exitCode, string output) = await RunScAsync(cancellationToken,
            "create", ServiceName, "binPath=", binPath, "start=", "auto", "DisplayName=", DisplayName);

        if (exitCode != 0)
        {
            return ServiceOperationResult.Failed(
                exitCode == 5 ? "AccessDenied" : "RegisterFailed",
                exitCode == 5
                    ? "Access denied - the app must run elevated (as Administrator) to register the service."
                    : $"Failed to register the service: {output.Trim()}");
        }

        // Best-effort - the admin sees "Registered / Running" immediately instead of only after
        // the next reboot; a failure to start right away doesn't undo the registration itself.
        await StartAsync(cancellationToken);

        return ServiceOperationResult.Ok();
    }

    public async Task<ServiceOperationResult> UnregisterAsync(CancellationToken cancellationToken)
    {
        if (GetStatus() == ServiceRegistrationState.NotRegistered)
        {
            return ServiceOperationResult.Failed("NotRegistered", "The service is not registered.");
        }

        await StopAsync(cancellationToken); // best-effort; sc delete works on a stopped or running service either way

        (int exitCode, string output) = await RunScAsync(cancellationToken, "delete", ServiceName);

        if (exitCode != 0)
        {
            return ServiceOperationResult.Failed(
                exitCode == 5 ? "AccessDenied" : "UnregisterFailed",
                exitCode == 5
                    ? "Access denied - the app must run elevated (as Administrator) to unregister the service."
                    : $"Failed to unregister the service: {output.Trim()}");
        }

        return ServiceOperationResult.Ok();
    }

    public Task<ServiceOperationResult> StartAsync(CancellationToken cancellationToken) =>
        ChangeStatusAsync(
            expectedFailureCode: "StartFailed",
            action: controller => controller.Start(),
            waitFor: System.ServiceProcess.ServiceControllerStatus.Running,
            cancellationToken);

    public Task<ServiceOperationResult> StopAsync(CancellationToken cancellationToken) =>
        ChangeStatusAsync(
            expectedFailureCode: "StopFailed",
            action: controller => controller.Stop(),
            waitFor: System.ServiceProcess.ServiceControllerStatus.Stopped,
            cancellationToken);

    private async Task<ServiceOperationResult> ChangeStatusAsync(
        string expectedFailureCode,
        Action<SysServiceController> action,
        System.ServiceProcess.ServiceControllerStatus waitFor,
        CancellationToken cancellationToken)
    {
        try
        {
            using var controller = new SysServiceController(ServiceName);
            await Task.Run(() =>
            {
                action(controller);
                controller.WaitForStatus(waitFor, StatusChangeTimeout);
            }, cancellationToken);
            return ServiceOperationResult.Ok();
        }
        catch (InvalidOperationException ex)
        {
            bool accessDenied = ex.InnerException is Win32Exception { NativeErrorCode: 5 };
            return ServiceOperationResult.Failed(
                accessDenied ? "AccessDenied" : expectedFailureCode,
                accessDenied
                    ? "Access denied - the app must run elevated (as Administrator) to control the service."
                    : ex.Message);
        }
        catch (System.ServiceProcess.TimeoutException)
        {
            return ServiceOperationResult.Failed(expectedFailureCode, "Timed out waiting for the service to change status.");
        }
    }

    private static ServiceRegistrationState ToState(System.ServiceProcess.ServiceControllerStatus status) => status switch
    {
        System.ServiceProcess.ServiceControllerStatus.Running => ServiceRegistrationState.Running,
        System.ServiceProcess.ServiceControllerStatus.Stopped => ServiceRegistrationState.Stopped,
        System.ServiceProcess.ServiceControllerStatus.StartPending => ServiceRegistrationState.StartPending,
        System.ServiceProcess.ServiceControllerStatus.StopPending => ServiceRegistrationState.StopPending,
        _ => ServiceRegistrationState.Unknown,
    };

    // Same muxer-vs-apphost detection ServiceLifecycleManager.BuildRelaunchStartInfo uses, but
    // here the result must be a single string (sc.exe's binPath is the literal ImagePath the SCM
    // later splits with CommandLineToArgvW), not a ProcessStartInfo.ArgumentList. Resolves *this*
    // process's own path, so it's only valid for a caller that is itself the ProdHelperService
    // process (i.e. the HTTP-triggered self-registration path) - a different process (AdminApp)
    // must resolve and pass its own binPath instead.
    public static string ResolveBinPath()
    {
        string dllPath = Assembly.GetEntryAssembly()!.Location;
        string? processPath = Environment.ProcessPath;
        bool viaDotnetMuxer = processPath is not null
            && Path.GetFileNameWithoutExtension(processPath).Equals("dotnet", StringComparison.OrdinalIgnoreCase);

        return viaDotnetMuxer
            ? $"\"{processPath}\" \"{dllPath}\""
            : $"\"{processPath ?? dllPath}\"";
    }

    private static async Task<(int ExitCode, string Output)> RunScAsync(CancellationToken cancellationToken, params string[] args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "sc.exe",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        foreach (string arg in args) startInfo.ArgumentList.Add(arg);

        using var process = Process.Start(startInfo)!;
        string stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        string stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        return (process.ExitCode, string.IsNullOrWhiteSpace(stderr) ? stdout : $"{stdout}{stderr}");
    }
}
