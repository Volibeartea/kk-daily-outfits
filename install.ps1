param([string]$GamePath = 'D:\Koikatsu3.33Perfection2')
$ErrorActionPreference = 'Stop'
$gameRoot = (Resolve-Path -LiteralPath $GamePath).Path
if (-not (Test-Path -LiteralPath (Join-Path $gameRoot 'Koikatu.exe'))) { throw 'Not a Koikatu game directory.' }
$gameExe = Join-Path $gameRoot 'Koikatu.exe'
foreach ($gameProcess in Get-Process Koikatu -ErrorAction SilentlyContinue) {
    if (-not $gameProcess.Path -or [string]::Equals($gameProcess.Path, $gameExe, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Close this copy of Koikatu before installing the plugin.'
    }
}
$source = Join-Path $PSScriptRoot 'dist\KK_DailyOutfits.dll'
if (-not (Test-Path -LiteralPath $source)) { throw 'Run build.ps1 first.' }
$pluginDir = Join-Path $gameRoot 'BepInEx\plugins\KK_DailyOutfits'
New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null
$destination = Join-Path $pluginDir 'KK_DailyOutfits.dll'
if (Test-Path -LiteralPath $destination) {
    $backupDir = Join-Path $PSScriptRoot 'dist\previous'
    New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
    Copy-Item -LiteralPath $destination -Destination (Join-Path $backupDir ('KK_DailyOutfits.' + [DateTime]::UtcNow.ToString('yyyyMMddHHmmssfff') + '.dll'))
}
Copy-Item -LiteralPath $source -Destination $destination -Force
New-Item -ItemType Directory -Force -Path (Join-Path $gameRoot 'UserData\DailyOutfits\DefaultCloset') | Out-Null
if ((Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $destination).Hash) { throw 'Installed DLL checksum mismatch.' }
Write-Host "Installed: $destination"
Write-Host "Wardrobe root: $gameRoot\UserData\DailyOutfits"
