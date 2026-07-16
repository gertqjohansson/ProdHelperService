namespace ProdHelperService.Contracts;

// Route paths shared between ProdHelperService's API controllers and any
// HTTP caller (e.g. ProdHelperService.AdminApp), so both sides route off the
// same constants instead of duplicating literal strings.
public static class ApiRoutes
{
    public const string OeeCalculate = "Oee/Calculate";
    public const string PlannerGetInteruption = "Planner/GetInteruption";

    public const string AuthRegister = "Auth/Register";
    public const string AuthHasUsers = "Auth/HasUsers";
    public const string AuthLogin = "Auth/Login";
    public const string AuthForgotPassword = "Auth/ForgotPassword";
    public const string AuthResetPassword = "Auth/ResetPassword";
    public const string AuthVerifyMfa = "Auth/VerifyMfa";
    public const string AuthRefresh = "Auth/Refresh";
    public const string AuthLogout = "Auth/Logout";
    public const string AuthMfaAuthenticatorSetup = "Auth/Mfa/Authenticator/Setup";
    public const string AuthMfaAuthenticatorEnable = "Auth/Mfa/Authenticator/Enable";
    public const string AuthMfaAuthenticatorDisable = "Auth/Mfa/Authenticator/Disable";
}
