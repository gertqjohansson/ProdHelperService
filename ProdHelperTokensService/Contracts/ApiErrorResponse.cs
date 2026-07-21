namespace ProdHelperTokensService.Contracts;

// Same {Code,Message} shape ProdHelperService.Contracts.Auth.AuthErrorResponse uses, duplicated
// here rather than referencing that project - this service is meant to stay independently
// deployable, not pull in ProdHelperService's whole Contracts library for one small DTO.
public class ApiErrorResponse
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
