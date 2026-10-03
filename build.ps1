param([string]$GamePath = 'D:\Koikatsu3.33Perfection2')
$ErrorActionPreference = 'Stop'
$projectDir = $PSScriptRoot
$managedDir = Join-Path $GamePath 'Koikatu_Data\Managed'
$references = @(
    "$managedDir\mscorlib.dll", "$managedDir\System.dll", "$managedDir\System.Core.dll",
    "$managedDir\UnityEngine.dll", "$managedDir\Assembly-CSharp.dll", "$managedDir\Assembly-CSharp-firstpass.dll",
    "$GamePath\BepInEx\core\BepInEx.dll", "$GamePath\BepInEx\plugins\KKAPI.dll",
    "$GamePath\BepInEx\plugins\KK_BepisPlugins\ExtensibleSaveFormat.dll"
)
foreach ($reference in $references) {
    if (-not (Test-Path -LiteralPath $reference)) { throw "Missing reference: $reference" }
}
$sdkLine = (& dotnet --list-sdks | Select-Object -Last 1)
if ($sdkLine -notmatch '^(\S+)\s+\[(.+)\]$') { throw 'A .NET SDK is required.' }
$compilerPath = Join-Path (Join-Path $Matches[2] $Matches[1]) 'Roslyn\bincore\csc.dll'
$outputDir = Join-Path $projectDir 'dist'
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
$compilerArgs = @('/nologo', '/noconfig', '/nostdlib+', '/target:library', '/optimize+', '/langversion:7.3', '/warnaserror+')
$compilerArgs += $references | ForEach-Object { '/reference:' + $_ }
$compilerArgs += '/out:' + (Join-Path $outputDir 'KK_DailyOutfits.dll')
$compilerArgs += Get-ChildItem -LiteralPath (Join-Path $projectDir 'src') -Filter '*.cs' | ForEach-Object FullName
& dotnet $compilerPath @compilerArgs
if ($LASTEXITCODE -ne 0) { throw "Compilation failed: $LASTEXITCODE" }
Write-Host "Built $outputDir\KK_DailyOutfits.dll (references game DLLs in place; no game files copied)."
