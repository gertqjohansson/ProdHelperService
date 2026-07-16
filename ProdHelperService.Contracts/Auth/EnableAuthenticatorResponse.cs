namespace ProdHelperService.Contracts.Auth;

// RecoveryCodes are shown here exactly once — Identity only stores them hashed,
// there is no endpoint to fetch them again later, only to regenerate a new set.
public class EnableAuthenticatorResponse
{
    public string[] RecoveryCodes { get; set; } = [];
}
