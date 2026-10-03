param(
    [string]$DllPath = (Join-Path $PSScriptRoot 'dist\KK_DailyOutfits.dll'),
    [string]$GamePath = 'D:\Koikatsu3.33Perfection2'
)
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $GamePath 'BepInEx\core\Mono.Cecil.dll')
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Resolve-Path -LiteralPath $DllPath).Path)
try {
    $plugin = $assembly.MainModule.Types | Where-Object FullName -eq 'KKDailyOutfits.DailyOutfitsPlugin'
    $attribute = $plugin.CustomAttributes | Where-Object { $_.AttributeType.FullName -eq 'BepInEx.BepInPlugin' }
    $version = [string]$attribute.ConstructorArguments[2].Value
    if ($version -notmatch '^\d+\.\d+\.\d+(\.\d+)?$') { throw 'Plugin version missing or invalid.' }
} finally { $assembly.Dispose() }
$releaseRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'release'))
$releaseDir = Join-Path $releaseRoot "v$version"
New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null
$hash = (Get-FileHash -LiteralPath $DllPath -Algorithm SHA256).Hash
$destination = Join-Path $releaseDir 'KK_DailyOutfits.dll'
Copy-Item -LiteralPath $DllPath -Destination $destination -Force
if ((Get-FileHash -LiteralPath $destination).Hash -ne $hash) { throw 'Release checksum mismatch.' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md') -Destination (Join-Path $releaseDir 'README.md') -Force
"$hash  KK_DailyOutfits.dll" | Set-Content -LiteralPath (Join-Path $releaseDir 'SHA256.txt') -Encoding ascii
# Remove only recognized generated release folders, after the new DLL is verified.
$generatedNames = @('KK_DailyOutfits.dll', 'README.md', 'SHA256.txt')
foreach ($directory in Get-ChildItem -LiteralPath $releaseRoot -Directory) {
    if ($directory.Name -notmatch '^v\d+\.\d+\.\d+(\.\d+)?$' -or $directory.FullName -eq $releaseDir) { continue }
    if ($directory.Parent.FullName -ne $releaseRoot -or ($directory.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Unexpected release cleanup path.' }
    $contents = @(Get-ChildItem -LiteralPath $directory.FullName -Force)
    if ($contents | Where-Object { $_.PSIsContainer -or $_.Name -notin $generatedNames -or ($_.Attributes -band [IO.FileAttributes]::ReparsePoint) }) {
        throw "Unrecognized files in old release; preserved: $($directory.FullName)"
    }
    Remove-Item -LiteralPath $directory.FullName -Recurse -Force
}
# Migrate the previous flat layout; do not touch unrelated files.
foreach ($name in $generatedNames) {
    $oldFile = Join-Path $releaseRoot $name
    if (Test-Path -LiteralPath $oldFile -PathType Leaf) { Remove-Item -LiteralPath $oldFile }
}
Write-Host "Latest release updated: $releaseDir"
