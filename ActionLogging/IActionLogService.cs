namespace ProdHelperService.ActionLogging;

public interface IActionLogService
{
    // oldValuesJson/newValuesJson are already-serialized JSON, not raw objects - callers control
    // exactly when serialization happens, since an Update snapshots the same entity object twice
    // (before and after mutation).
    void Record(string actionType, string section, string madeByUser, DateTime actionTimeUtc, string? oldValuesJson, string newValuesJson);
}
