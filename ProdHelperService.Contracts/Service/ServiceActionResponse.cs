namespace ProdHelperService.Contracts.Service;

// Shared success shape for Register/Unregister/Start/Stop - failures use the existing
// AuthErrorResponse instead (see ServiceController), same split UpdatePort already uses.
public class ServiceActionResponse
{
    public string State { get; set; } = string.Empty;
}
