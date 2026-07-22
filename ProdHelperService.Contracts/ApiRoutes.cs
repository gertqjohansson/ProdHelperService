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

    public const string EquipmentList = "Equipment/List";
    public const string EquipmentCreate = "Equipment/Create";
    public const string EquipmentUpdate = "Equipment/Update";
    public const string EquipmentDelete = "Equipment/Delete";
    public const string EquipmentSaveComment = "Equipment/SaveComment";
    public const string EquipmentMoveSchiftParent = "Equipment/MoveSchiftParent";

    public const string EquipmentUploadList = "EquipmentUpload/List";
    public const string EquipmentUploadUpload = "EquipmentUpload/Upload";
    public const string EquipmentUploadDownload = "EquipmentUpload/Download";
    public const string EquipmentUploadDelete = "EquipmentUpload/Delete";

    public const string EquipmentLinkList = "EquipmentLink/List";
    public const string EquipmentLinkCreate = "EquipmentLink/Create";
    public const string EquipmentLinkDelete = "EquipmentLink/Delete";

    public const string EquipmentLogList = "EquipmentLog/List";
    public const string EquipmentLogCreate = "EquipmentLog/Create";
    public const string EquipmentLogUpdate = "EquipmentLog/Update";
    public const string EquipmentLogDelete = "EquipmentLog/Delete";

    public const string ShiftScheduleVersionCreate = "ShiftScheduleVersion/Create";
    public const string ShiftScheduleVersionListEquipmentIdsWithSchedule = "ShiftScheduleVersion/ListEquipmentIdsWithSchedule";
    public const string ShiftScheduleVersionGetLatestForEquipment = "ShiftScheduleVersion/GetLatestForEquipment";

    public const string EquipmentCategoryList = "EquipmentCategory/List";
    public const string EquipmentCategoryCreate = "EquipmentCategory/Create";
    public const string EquipmentCategoryUpdate = "EquipmentCategory/Update";
    public const string EquipmentCategoryDelete = "EquipmentCategory/Delete";

    public const string TranslationTranslate = "Translation/Translate";

    public const string ServiceGetInfo = "Service/GetInfo";
    public const string ServiceUpdatePort = "Service/UpdatePort";
    public const string ServiceGetRegistrationStatus = "Service/GetRegistrationStatus";
    public const string ServiceRegister = "Service/Register";
    public const string ServiceUnregister = "Service/Unregister";
    public const string ServiceStart = "Service/Start";
    public const string ServiceStop = "Service/Stop";
    public const string ServiceGetVersion = "Service/GetVersion";
}
