using ProdHelperService.ServiceManagement;

namespace ProdHelperService.AdminApp;

// Resolves files that belong to ProdHelperService (its appsettings.json, its .exe) from
// AdminApp's own process location. Two layouts are supported: installed (AdminApp and
// ProdHelperService as sibling folders under a common parent - the Inno Setup installer's
// layout) and dev (AdminApp running from its own bin\Debug|Release\net8.0-windows output while
// ProdHelperService is only ever run via `dotnet run`/F5 from source). Callers pass candidate
// relative paths in preference order; the first one that actually exists on disk wins.
internal static class AdminAppPaths
{
    public static string? ResolveProdHelperServiceFile(params string[] candidateRelativePaths)
    {
        foreach (string relativePath in candidateRelativePaths)
        {
            string fullPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativePath));
            if (File.Exists(fullPath)) return fullPath;
        }
        return null;
    }

    // Asks the Windows Service Control Manager where ProdHelperService is actually registered to
    // run (the real source of truth) and uses the appsettings.json next to that binary. Falls
    // back to the install/dev path-guessing helper only if that somehow doesn't resolve to a real
    // file (e.g. the service isn't registered yet, or the registry read failed). Shared by every
    // form that needs to read/write ProdHelperService's config (ServiceConfigForm, UploadFolderForm).
    public static string? ResolveAppSettingsPath(IWindowsServiceInstaller windowsServiceInstaller)
    {
        string? binPath = windowsServiceInstaller.GetBinaryPath();
        string? binDirectory = binPath is not null ? Path.GetDirectoryName(binPath) : null;
        if (binDirectory is not null)
        {
            string candidate = Path.Combine(binDirectory, "appsettings.json");
            if (File.Exists(candidate)) return candidate;
        }

        return ResolveProdHelperServiceFile(
            Path.Combine("..", "ProdHelperService", "appsettings.json"),
            Path.Combine("..", "..", "..", "..", "appsettings.json"));
    }
}
