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

    public static string AuthEmailLabel => Get("AuthEmailLabel");
    public static string AuthPasswordLabel => Get("AuthPasswordLabel");
    public static string AuthConfirmPasswordLabel => Get("AuthConfirmPasswordLabel");
    public static string AuthAdminPasswordLabel => Get("AuthAdminPasswordLabel");
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
    public static string AuthForgotPasswordLinkText => Get("AuthForgotPasswordLinkText");
    public static string AuthForgotPasswordTitle => Get("AuthForgotPasswordTitle");
    public static string AuthForgotPasswordDescription => Get("AuthForgotPasswordDescription");
    public static string AuthForgotPasswordButtonText => Get("AuthForgotPasswordButtonText");
    public static string AuthForgotPasswordSuccessMessage => Get("AuthForgotPasswordSuccessMessage");
    public static string AuthResetPasswordTitle => Get("AuthResetPasswordTitle");
    public static string AuthResetPasswordDescription => Get("AuthResetPasswordDescription");
    public static string AuthResetCodeLabel => Get("AuthResetCodeLabel");
    public static string AuthNewPasswordLabel => Get("AuthNewPasswordLabel");
    public static string AuthConfirmNewPasswordLabel => Get("AuthConfirmNewPasswordLabel");
    public static string AuthResetPasswordButtonText => Get("AuthResetPasswordButtonText");
    public static string AuthResetPasswordSuccessMessage => Get("AuthResetPasswordSuccessMessage");

    public static string ApiDocumentationMenuText => Get("ApiDocumentationMenuText");
    public static string ServiceUrlDialogTitle => Get("ServiceUrlDialogTitle");
    public static string ServiceUrlDialogDescription => Get("ServiceUrlDialogDescription");
    public static string ServiceUrlPortLabel => Get("ServiceUrlPortLabel");
    public static string ServiceUrlSaveButtonText => Get("ServiceUrlSaveButtonText");
    public static string ServiceUrlCancelButtonText => Get("ServiceUrlCancelButtonText");
    public static string ServiceUrlInvalidPortMessage => Get("ServiceUrlInvalidPortMessage");
    public static string ServiceUrlSamePortMessage => Get("ServiceUrlSamePortMessage");
    public static string ServiceUrlVerifyingMessage => Get("ServiceUrlVerifyingMessage");
    public static string ServiceUrlSettingsPersistFailedMessage => Get("ServiceUrlSettingsPersistFailedMessage");

    public static string ServiceUrlRestartingMessage(int port) =>
        string.Format(CultureInfo.CurrentUICulture, Get("ServiceUrlRestartingMessage"), port);

    public static string ApiDocumentationOpenErrorMessage(string errorMessage) =>
        string.Format(CultureInfo.CurrentUICulture, Get("ApiDocumentationOpenErrorMessage"), errorMessage);

    public static string ServiceDialogTitle => Get("ServiceDialogTitle");
    public static string ServiceDialogDescription => Get("ServiceDialogDescription");
    public static string ServiceStatusNotRegistered => Get("ServiceStatusNotRegistered");
    public static string ServiceStatusRunning => Get("ServiceStatusRunning");
    public static string ServiceStatusStopped => Get("ServiceStatusStopped");
    public static string ServiceStatusPending => Get("ServiceStatusPending");
    public static string ServiceRegisterButtonText => Get("ServiceRegisterButtonText");
    public static string ServiceUnregisterButtonText => Get("ServiceUnregisterButtonText");
    public static string ServiceStartButtonText => Get("ServiceStartButtonText");
    public static string ServiceStopButtonText => Get("ServiceStopButtonText");
    public static string ServiceCloseButtonText => Get("ServiceCloseButtonText");
    public static string ServiceRegisteringMessage => Get("ServiceRegisteringMessage");
    public static string ServiceUnregisteringMessage => Get("ServiceUnregisteringMessage");
    public static string ServiceStartingMessage => Get("ServiceStartingMessage");
    public static string ServiceStoppingMessage => Get("ServiceStoppingMessage");
    public static string ServiceUnregisterConfirmTitle => Get("ServiceUnregisterConfirmTitle");
    public static string ServiceUnregisterConfirmMessage => Get("ServiceUnregisterConfirmMessage");
    public static string ServiceNotRegisteredStartupMessage => Get("ServiceNotRegisteredStartupMessage");

    public static string ServiceConfigMenuText => Get("ServiceConfigMenuText");
    public static string ServiceConfigDialogTitle => Get("ServiceConfigDialogTitle");
    public static string ServiceConfigDialogDescription => Get("ServiceConfigDialogDescription");
    public static string ServiceConfigGetVersionButtonText => Get("ServiceConfigGetVersionButtonText");
    public static string ServiceConfigCheckingMessage => Get("ServiceConfigCheckingMessage");
    public static string ServiceConfigReachableMessage => Get("ServiceConfigReachableMessage");
    public static string ServiceConfigUnreachableMessage => Get("ServiceConfigUnreachableMessage");
    public static string ServiceConfigRelaySectionHeader => Get("ServiceConfigRelaySectionHeader");
    public static string ServiceConfigLocalApiSectionHeader => Get("ServiceConfigLocalApiSectionHeader");
    public static string ServiceConfigDatabaseSectionHeader => Get("ServiceConfigDatabaseSectionHeader");
    public static string ServiceConfigJwtSectionHeader => Get("ServiceConfigJwtSectionHeader");
    public static string ServiceConfigEmailSectionHeader => Get("ServiceConfigEmailSectionHeader");
    public static string ServiceConfigTokenTrackingSectionHeader => Get("ServiceConfigTokenTrackingSectionHeader");
    public static string ServiceConfigNamespaceLabel => Get("ServiceConfigNamespaceLabel");
    public static string ServiceConfigConnectionNameLabel => Get("ServiceConfigConnectionNameLabel");
    public static string ServiceConfigKeyNameLabel => Get("ServiceConfigKeyNameLabel");
    public static string ServiceConfigKeyLabel => Get("ServiceConfigKeyLabel");
    public static string ServiceConfigPortLabel => Get("ServiceConfigPortLabel");
    public static string ServiceConfigDatabaseConnectionStringLabel => Get("ServiceConfigDatabaseConnectionStringLabel");
    public static string ServiceConfigJwtKeyLabel => Get("ServiceConfigJwtKeyLabel");
    public static string ServiceConfigAccessTokenMinutesLabel => Get("ServiceConfigAccessTokenMinutesLabel");
    public static string ServiceConfigRefreshTokenDaysLabel => Get("ServiceConfigRefreshTokenDaysLabel");
    public static string ServiceConfigEmailConnectionStringLabel => Get("ServiceConfigEmailConnectionStringLabel");
    public static string ServiceConfigSenderAddressLabel => Get("ServiceConfigSenderAddressLabel");
    public static string ServiceConfigTokenTrackingBaseUrlLabel => Get("ServiceConfigTokenTrackingBaseUrlLabel");
    public static string ServiceConfigTokenTrackingApiKeyLabel => Get("ServiceConfigTokenTrackingApiKeyLabel");
    public static string ServiceConfigTokenTrackingIntervalMinutesLabel => Get("ServiceConfigTokenTrackingIntervalMinutesLabel");
    public static string ServiceConfigTryConnectionButtonText => Get("ServiceConfigTryConnectionButtonText");
    public static string ServiceConfigTestingConnectionMessage => Get("ServiceConfigTestingConnectionMessage");
    public static string ServiceConfigConnectionSuccessMessage => Get("ServiceConfigConnectionSuccessMessage");
    public static string ServiceConfigInvalidPortMessage => Get("ServiceConfigInvalidPortMessage");
    public static string ServiceConfigInvalidAccessTokenMinutesMessage => Get("ServiceConfigInvalidAccessTokenMinutesMessage");
    public static string ServiceConfigInvalidRefreshTokenDaysMessage => Get("ServiceConfigInvalidRefreshTokenDaysMessage");
    public static string ServiceConfigInvalidIntervalMinutesMessage => Get("ServiceConfigInvalidIntervalMinutesMessage");
    public static string ServiceConfigJwtKeyChangeWarningMessage => Get("ServiceConfigJwtKeyChangeWarningMessage");
    public static string ServiceConfigSaveButtonText => Get("ServiceConfigSaveButtonText");
    public static string ServiceConfigCancelButtonText => Get("ServiceConfigCancelButtonText");
    public static string ServiceConfigSavingMessage => Get("ServiceConfigSavingMessage");
    public static string ServiceConfigSaveFailedMessage => Get("ServiceConfigSaveFailedMessage");
    public static string ServiceConfigLoadFailedMessage => Get("ServiceConfigLoadFailedMessage");
    public static string ServiceConfigGenerateJwtKeyButtonText => Get("ServiceConfigGenerateJwtKeyButtonText");
    public static string ServiceConfigVersionMessage(string version) =>
        string.Format(CultureInfo.CurrentUICulture, Get("ServiceConfigVersionMessage"), version);

    public static string FoldersMenuText => Get("FoldersMenuText");
    public static string UploadFolderDialogTitle => Get("UploadFolderDialogTitle");
    public static string UploadFolderDialogDescription => Get("UploadFolderDialogDescription");
    public static string UploadFolderPathLabel => Get("UploadFolderPathLabel");
    public static string UploadFolderBrowseButtonText => Get("UploadFolderBrowseButtonText");
    public static string UploadFolderSaveButtonText => Get("UploadFolderSaveButtonText");
    public static string UploadFolderCancelButtonText => Get("UploadFolderCancelButtonText");
    public static string UploadFolderInvalidPathMessage => Get("UploadFolderInvalidPathMessage");
    public static string UploadFolderSavingMessage => Get("UploadFolderSavingMessage");
    public static string UploadFolderSaveFailedMessage => Get("UploadFolderSaveFailedMessage");

    public static string ServiceConfigConnectionFailedMessage(string errorMessage) =>
        string.Format(CultureInfo.CurrentUICulture, Get("ServiceConfigConnectionFailedMessage"), errorMessage);

    private static string Get(string name) =>
        ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? name;
}
