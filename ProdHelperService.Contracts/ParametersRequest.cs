namespace ProdHelperService.Contracts;

/// <summary>Request body shape for the local API endpoints, matching the
/// same {"Parameters": [...]} envelope used over the relay.</summary>
public class ParametersRequest
{
    public string[] Parameters { get; set; } = Array.Empty<string>();
}
