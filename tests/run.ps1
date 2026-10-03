param([string]$GamePath = 'D:\Koikatsu3.33Perfection2')
$ErrorActionPreference = 'Stop'
$projectDir = Split-Path $PSScriptRoot -Parent
$managedDir = Join-Path $GamePath 'Koikatu_Data\Managed'
$testDir = Join-Path $projectDir 'test-output'
New-Item -ItemType Directory -Force -Path $testDir | Out-Null
$sdkLine = (& dotnet --list-sdks | Select-Object -Last 1)
if ($sdkLine -notmatch '^(\S+)\s+\[(.+)\]$') { throw 'A .NET SDK is required.' }
$compilerPath = Join-Path (Join-Path $Matches[2] $Matches[1]) 'Roslyn\bincore\csc.dll'
$compilerArgs = @('/nologo', '/noconfig', '/nostdlib+', '/target:library', '/warnaserror+')
foreach ($name in @('mscorlib.dll','System.dll','System.Core.dll','UnityEngine.dll','Assembly-CSharp.dll','Assembly-CSharp-firstpass.dll')) {
    $compilerArgs += '/reference:' + (Join-Path $managedDir $name)
}
$compilerArgs += '/out:' + (Join-Path $testDir 'CopyTests.dll')
$compilerArgs += @((Join-Path $projectDir 'src\UnderwearCopy.cs'), (Join-Path $projectDir 'src\WardrobePaths.cs'), (Join-Path $PSScriptRoot 'CopyTests.cs'))
$compilerArgs += @((Join-Path $projectDir 'src\WearHistory.cs'), (Join-Path $PSScriptRoot 'HistoryTests.cs'))
& dotnet $compilerPath @compilerArgs
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
# Use Windows PowerShell's .NET Framework runtime for the game's CLR 2 assemblies.
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'invoke-framework.ps1') -GamePath $GamePath -TestDir $testDir
if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
