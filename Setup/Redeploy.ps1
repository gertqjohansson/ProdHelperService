<#
.SYNOPSIS
    Republishes ProdHelperService and redeploys it to the installed Windows Service location,
    without touching the installed appsettings.json (which holds real, working configuration -
    only the sanitized template ships with the original installer).

.DESCRIPTION
    Run this elevated (Run as Administrator) any time the installed service needs to pick up
    backend code changes without re-running the full Inno Setup installer. Steps:
      1. Stop the ProdHelperService Windows Service (its .exe/.dll are locked while running).
      2. dotnet publish the current source to Setup\publish (same command the installer itself uses).
      3. Copy the refreshed files into the install directory, excluding appsettings.json.
      4. Start the service again.
#>

$ErrorActionPreference = "Stop"

$installDir = "C:\Program Files\ProdHelper\ProdHelperService"
$publishDir = Join-Path $PSScriptRoot "publish"
$projectPath = Join-Path $PSScriptRoot "..\ProdHelperService.csproj"

$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Redeploy.ps1 must be run elevated (Run as Administrator) - stopping the service and writing to '$installDir' both require it."
}

Write-Output "Stopping ProdHelperService..."
Stop-Service -Name ProdHelperService -Force
Start-Sleep -Seconds 1

Write-Output "Publishing current source to $publishDir..."
dotnet publish $projectPath -c Release -r win-x64 --self-contained true -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

Write-Output "Copying refreshed files to $installDir (preserving the installed appsettings.json)..."
Get-ChildItem -Path $publishDir -Recurse -File | Where-Object { $_.Name -ne "appsettings.json" } | ForEach-Object {
    $relativePath = $_.FullName.Substring($publishDir.Length + 1)
    $destPath = Join-Path $installDir $relativePath
    $destDir = Split-Path $destPath -Parent
    if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Path $destDir -Force | Out-Null }
    Copy-Item -Path $_.FullName -Destination $destPath -Force
}

Write-Output "Starting ProdHelperService..."
Start-Service -Name ProdHelperService
Start-Sleep -Seconds 2

$status = (Get-Service -Name ProdHelperService).Status
Write-Output "Service status: $status"

Write-Output "Verifying Service/GetVersion responds..."
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5080/Service/GetVersion" -Method POST -UseBasicParsing -TimeoutSec 10
    Write-Output "Service/GetVersion: HTTP $($response.StatusCode) - $($response.Content)"
} catch {
    Write-Warning "Service/GetVersion did not respond successfully: $($_.Exception.Message)"
}
