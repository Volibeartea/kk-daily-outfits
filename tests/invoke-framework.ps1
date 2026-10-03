param([string]$GamePath, [string]$TestDir)
$ErrorActionPreference = 'Stop'
$managedDir = Join-Path $GamePath 'Koikatu_Data\Managed'
[System.Reflection.Assembly]::LoadFrom((Join-Path $managedDir 'UnityEngine.dll')) | Out-Null
[System.Reflection.Assembly]::LoadFrom((Join-Path $managedDir 'Assembly-CSharp-firstpass.dll')) | Out-Null
[System.Reflection.Assembly]::LoadFrom((Join-Path $managedDir 'Assembly-CSharp.dll')) | Out-Null
$testAssembly = [System.Reflection.Assembly]::LoadFrom((Join-Path $TestDir 'CopyTests.dll'))
$scratch = Join-Path $TestDir ([Guid]::NewGuid().ToString('N'))
$result = [CopyTests]::Run([string]$scratch)
Write-Host $result
Write-Host ([HistoryTests]::Run())
