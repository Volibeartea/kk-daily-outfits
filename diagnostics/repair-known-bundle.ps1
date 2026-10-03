$ErrorActionPreference = 'Stop'
if (Get-Process Koikatu -ErrorAction SilentlyContinue) { throw 'Close Koikatu before repair.' }
$target = 'D:\Koikatsu3.33Perfection\abdata\chara\co_bot_50.unity3d'
$source = 'D:\Koikatsu\Koikatsu3.33PerfectionBackup\abdata\chara\co_bot_50.unity3d'
$expectedSource = '3AAE581589C6E93E46B3E356E7882E005611542AD1A6828C32833731A6BD5B92'
if ((Get-FileHash -LiteralPath $source).Hash -ne $expectedSource) { throw 'Backup file changed; stop for inspection.' }
$recoveryDir = Join-Path 'D:\Koikatsu3.33Perfection\RepairBackups' ('dailyoutfits-investigation-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ'))
New-Item -ItemType Directory -Path $recoveryDir | Out-Null
$originalHash = (Get-FileHash -LiteralPath $target).Hash
$savedOriginal = Join-Path $recoveryDir 'co_bot_50.unity3d.original'
Copy-Item -LiteralPath $target -Destination $savedOriginal
if ((Get-FileHash -LiteralPath $savedOriginal).Hash -ne $originalHash) { throw 'Recovery copy verification failed; target unchanged.' }
Copy-Item -LiteralPath 'D:\Koikatsu3.33Perfection\output_log.txt' -Destination (Join-Path $recoveryDir 'before-repair-output_log.txt')
Copy-Item -LiteralPath $source -Destination $target -Force
if ((Get-FileHash -LiteralPath $target).Hash -ne $expectedSource) { throw 'Repaired file verification failed; original is in recovery directory.' }
@("Source: $source", "Target: $target", "Original SHA256: $originalHash", "Replacement SHA256: $expectedSource", 'Game runtime validation is still required. No save files or plugins were modified.') | Set-Content -LiteralPath (Join-Path $recoveryDir 'repair-record.txt') -Encoding utf8
Write-Output "Repaired file: $target"
Write-Output "Original and log preserved: $recoveryDir"
Write-Output "SHA256 verified: $expectedSource"
