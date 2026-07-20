namespace ProdHelperService.ServiceManagement;

public interface IServiceLifecycleManager
{
    int CurrentPort { get; }

    Task<PortUpdateResult> UpdatePortAsync(int newPort, CancellationToken cancellationToken);
}

// Success is reported via NewPort/SettingsFilePersisted; a failed attempt sets ErrorCode/ErrorMessage
// instead of throwing, since every failure path here (bad port, port in use, new instance never came
// up) is an expected outcome the caller (ServiceController) needs to turn into a normal error response.
public sealed record PortUpdateResult(bool Success, int NewPort, bool SettingsFilePersisted, string? ErrorCode, string? ErrorMessage)
{
    public static PortUpdateResult Ok(int newPort, bool settingsFilePersisted) =>
        new(true, newPort, settingsFilePersisted, null, null);

    public static PortUpdateResult Failed(string errorCode, string errorMessage) =>
        new(false, 0, false, errorCode, errorMessage);
}
