param(
    [Parameter(Mandatory = $true)]
    [string]$BackupPath,

    [string]$TargetPath = "",

    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($TargetPath)) {
    $TargetPath = Join-Path $root "artifacts\BeeKingdom.Server"
}

$resolvedBackup = (Resolve-Path -LiteralPath $BackupPath).Path
$resolvedTargetParent = (Resolve-Path -LiteralPath (Split-Path -Parent $TargetPath)).Path
$targetFullPath = [System.IO.Path]::GetFullPath($TargetPath)

if (-not (Test-Path -LiteralPath (Join-Path $resolvedBackup "BeeKingdom.Server.dll"))) {
    throw "BackupPath does not look like a BeeKingdom.Server package: $resolvedBackup"
}

if (-not $targetFullPath.StartsWith($resolvedTargetParent, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Resolved target path is outside its parent directory: $targetFullPath"
}

Write-Host "Bee Kingdom Server rollback"
Write-Host "Backup: $resolvedBackup"
Write-Host "Target: $targetFullPath"

if ($WhatIf) {
    Write-Host "WhatIf: rollback validation completed. No files changed."
    return
}

if (Test-Path -LiteralPath $targetFullPath) {
    Remove-Item -LiteralPath $targetFullPath -Recurse -Force
}

Copy-Item -LiteralPath $resolvedBackup -Destination $targetFullPath -Recurse -Force

Write-Host "Rollback completed."
