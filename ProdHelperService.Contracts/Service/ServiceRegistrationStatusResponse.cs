namespace ProdHelperService.Contracts.Service;

public class ServiceRegistrationStatusResponse
{
    public bool IsRegistered { get; set; }
    public string State { get; set; } = string.Empty;
}
