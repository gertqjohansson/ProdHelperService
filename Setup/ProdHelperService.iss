; Installer for ProdHelperService (backend Windows Service only - not AdminApp
; or the web client). Installs to a "ProdHelper\ProdHelperService" folder so
; that ProdHelperService.AdminApp (installed separately, as a sibling
; "ProdHelper\ProdHelperService.AdminApp" folder) can find this service's exe
; via a predictable relative path when registering it directly.
;
; Build steps (from ProdHelperService/):
;   dotnet publish ProdHelperService.csproj -c Release -r win-x64 --self-contained true -o Setup\publish
;   copy Setup\appsettings.production-template.json Setup\publish\appsettings.json
;   ISCC Setup\ProdHelperService.iss

#define MyAppName "ProdHelper Service"
#define MyAppVersion "1.0.0"
#define MyServiceName "ProdHelperService"

[Setup]
AppName={#MyAppName}
AppVersion={#MyAppVersion}
DefaultDirName={commonpf64}\ProdHelper\ProdHelperService
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=Output
OutputBaseFilename=ProdHelperServiceSetup
Compression=lzma2
SolidCompression=yes
InfoAfterFile=ReadMe.txt
UninstallDisplayIcon={app}\ProdHelperService.exe

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Run]
; Register the Windows Service, set to auto-start on boot, but deliberately
; do NOT start it here - appsettings.json still has placeholder secrets
; immediately after install (see ReadMe.txt / InfoAfterFile above).
Filename: "sc.exe"; Parameters: "create ""{#MyServiceName}"" binPath= ""{app}\ProdHelperService.exe"" start= auto DisplayName= ""{#MyAppName}"""; Flags: runhidden; StatusMsg: "Registering ProdHelperService as a Windows Service..."

[UninstallRun]
Filename: "sc.exe"; Parameters: "stop ""{#MyServiceName}"""; Flags: runhidden; RunOnceId: "StopService"
Filename: "sc.exe"; Parameters: "delete ""{#MyServiceName}"""; Flags: runhidden; RunOnceId: "DeleteService"
