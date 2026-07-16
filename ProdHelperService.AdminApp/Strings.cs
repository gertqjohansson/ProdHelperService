using System.Globalization;
using System.Resources;

namespace ProdHelperService.AdminApp;

// Hand-written ResourceManager wrapper instead of a VS-generated
// Strings.Designer.cs — that generator only runs at design time inside
// Visual Studio, not as part of `dotnet build`, which this app relies on
// exclusively. Satellite assemblies (Strings.{culture}.resx) still build
// automatically under plain `dotnet build`/`dotnet run`.
internal static class Strings
{
    private static readonly ResourceManager ResourceManager =
        new("ProdHelperService.AdminApp.Strings", typeof(Strings).Assembly);

    public static string WindowTitle => Get("WindowTitle");
    public static string Try1ButtonText => Get("Try1ButtonText");
    public static string Try2ButtonText => Get("Try2ButtonText");
    public static string LanguageMenuText => Get("LanguageMenuText");
    public static string MenuLabel => Get("MenuLabel");
    public static string GuestLabel => Get("GuestLabel");

    public static string CallSuccessHeading(string relativeUrl, Uri? baseAddress) =>
        string.Format(CultureInfo.CurrentUICulture, Get("CallSuccessHeading"), relativeUrl, baseAddress);

    public static string CallErrorHeading(string relativeUrl) =>
        string.Format(CultureInfo.CurrentUICulture, Get("CallErrorHeading"), relativeUrl);

    public static string ItemSelectedFormat(string item) =>
        string.Format(CultureInfo.CurrentUICulture, Get("ItemSelectedFormat"), item);

    public static string AuthEmailLabel => Get("AuthEmailLabel");
    public static string AuthPasswordLabel => Get("AuthPasswordLabel");
    public static string AuthConfirmPasswordLabel => Get("AuthConfirmPasswordLabel");
    public static string AuthDisplayNameLabel => Get("AuthDisplayNameLabel");
    public static string AuthLoginButtonText => Get("AuthLoginButtonText");
    public static string AuthRegisterButtonText => Get("AuthRegisterButtonText");
    public static string AuthLogoutButtonText => Get("AuthLogoutButtonText");
    public static string AuthNeedAccountText => Get("AuthNeedAccountText");
    public static string AuthHaveAccountText => Get("AuthHaveAccountText");
    public static string AuthLoginTitle => Get("AuthLoginTitle");
    public static string AuthRegisterTitle => Get("AuthRegisterTitle");
    public static string AuthEmailInvalidMessage => Get("AuthEmailInvalidMessage");
    public static string AuthPasswordMismatchMessage => Get("AuthPasswordMismatchMessage");
    public static string AuthRegisterSuccessMessage => Get("AuthRegisterSuccessMessage");
    public static string AuthCloseButtonText => Get("AuthCloseButtonText");
    public static string AuthMfaTitle => Get("AuthMfaTitle");
    public static string AuthMfaDescription => Get("AuthMfaDescription");
    public static string AuthMfaCodeLabel => Get("AuthMfaCodeLabel");
    public static string AuthVerifyButtonText => Get("AuthVerifyButtonText");
    public static string AuthMfaSetupTitle => Get("AuthMfaSetupTitle");
    public static string AuthMfaSetupDescription => Get("AuthMfaSetupDescription");
    public static string AuthMfaAlreadyEnabledMessage => Get("AuthMfaAlreadyEnabledMessage");
    public static string AuthMfaEnableButtonText => Get("AuthMfaEnableButtonText");
    public static string AuthMfaDisableButtonText => Get("AuthMfaDisableButtonText");
    public static string AuthMfaRecoveryCodesDescription => Get("AuthMfaRecoveryCodesDescription");

    private static string Get(string name) =>
        ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? name;
}
