namespace ProdHelperService.ServiceManagement;

public interface IWindowsServiceInstaller
{
    ServiceRegistrationState GetStatus();

    // The file path the service is actually registered to run, as recorded by the Windows Service
    // Control Manager - null if the service isn't registered. Callers that need to locate files
    // belonging to the running/registered service (e.g. its appsettings.json) should derive that
    // from this rather than guessing an install layout.
    string? GetBinaryPath();

    Task<ServiceOperationResult> RegisterAsync(string binPath, CancellationToken cancellationToken);

    Task<ServiceOperationResult> UnregisterAsync(CancellationToken cancellationToken);

    Task<ServiceOperationResult> StartAsync(CancellationToken cancellationToken);

    Task<ServiceOperationResult> StopAsync(CancellationToken cancellationToken);
}

public enum ServiceRegistrationState
{
    NotRegistered,
    Running,
    Stopped,
    StartPending,
    StopPending,
    Unknown,
}

// Success is reported via State alone; a failed attempt sets ErrorCode/ErrorMessage instead of
// throwing, since every failure path here (sc.exe not elevated, service already in that state,
// SCM timeout) is an expected outcome the caller (ServiceController) needs to turn into a normal
// error response - same shape as PortUpdateResult.
public sealed record ServiceOperationResult(bool Success, string? ErrorCode, string? ErrorMessage)
{
    public static ServiceOperationResult Ok() => new(true, null, null);

    public static ServiceOperationResult Failed(string errorCode, string errorMessage) =>
        new(false, errorCode, errorMessage);
}
