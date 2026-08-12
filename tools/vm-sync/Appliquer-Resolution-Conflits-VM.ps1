[CmdletBinding()]
param(
    [string]$VmRoot = 'C:\projets\beekingdomgame-master',
    [string]$HostRoot = ''
)

$ErrorActionPreference = 'Stop'

function Get-NormalizedRoot {
    param([Parameter(Mandatory = $true)][string]$Path)

    return [IO.Path]::GetFullPath($Path).TrimEnd('\')
}

if ([string]::IsNullOrWhiteSpace($HostRoot)) {
    $HostRoot = Join-Path $PSScriptRoot '..\..'
}

$resolvedVmRoot = Get-NormalizedRoot -Path $VmRoot
$resolvedHostRoot = Get-NormalizedRoot -Path $HostRoot
if ($resolvedVmRoot.Equals($resolvedHostRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Lance cet outil depuis Z:\tools\vm-sync dans la VM, pas depuis la copie locale C:.'
}

$reportPath = Join-Path $resolvedVmRoot '.codex\vm-sync-last-report.txt'
if (-not (Test-Path -LiteralPath $reportPath -PathType Leaf)) {
    throw "Rapport de synchronisation introuvable: $reportPath"
}

$lines = [IO.File]::ReadAllLines($reportPath)
$sectionIndex = [Array]::IndexOf($lines, '[Conflicts]')
if ($sectionIndex -lt 0) {
    throw 'Le rapport ne contient pas de section [Conflicts].'
}

$conflicts = [Collections.Generic.List[string]]::new()
for ($index = $sectionIndex + 1; $index -lt $lines.Length; $index++) {
    $relativePath = $lines[$index].Trim()
    if ([string]::IsNullOrWhiteSpace($relativePath) -or $relativePath.StartsWith('[')) {
        break
    }
    if ([IO.Path]::IsPathRooted($relativePath) -or $relativePath -match '(^|[\\/])\.\.([\\/]|$)') {
        throw "Chemin de conflit non securitaire: $relativePath"
    }
    $conflicts.Add($relativePath.Replace('/', '\'))
}

if ($conflicts.Count -eq 0) {
    Write-Host 'Aucun conflit a resoudre.'
    exit 0
}

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backupRoot = Join-Path $resolvedVmRoot ".codex\vm-sync-conflict-backups\$stamp"
$manifest = [Collections.Generic.List[string]]::new()
$manifest.Add("Resolution UTC: $([DateTime]::UtcNow.ToString('o'))")
$manifest.Add("Source ordinateur: $resolvedHostRoot")
$manifest.Add('')

foreach ($relativePath in $conflicts) {
    $sourcePath = Join-Path $resolvedHostRoot $relativePath
    $targetPath = Join-Path $resolvedVmRoot $relativePath
    if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
        throw "Version fusionnee introuvable sur l'ordinateur: $sourcePath"
    }

    if (Test-Path -LiteralPath $targetPath -PathType Leaf) {
        $backupPath = Join-Path $backupRoot $relativePath
        [void](New-Item -ItemType Directory -Path (Split-Path -Parent $backupPath) -Force)
        Copy-Item -LiteralPath $targetPath -Destination $backupPath -Force
    }

    [void](New-Item -ItemType Directory -Path (Split-Path -Parent $targetPath) -Force)
    Copy-Item -LiteralPath $sourcePath -Destination $targetPath -Force

    $sourceHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $sourcePath).Hash
    $targetHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $targetPath).Hash
    if ($sourceHash -ne $targetHash) {
        throw "Verification SHA256 echouee: $relativePath"
    }
    $manifest.Add("$sourceHash`t$relativePath")
}

$manifestPath = Join-Path $backupRoot 'manifest.txt'
[void](New-Item -ItemType Directory -Path $backupRoot -Force)
$manifest | Set-Content -LiteralPath $manifestPath -Encoding UTF8

Write-Host "Versions fusionnees appliquees dans la VM: $($conflicts.Count)"
Write-Host "Sauvegarde des anciennes versions VM: $backupRoot"
Write-Host 'Lance maintenant la synchronisation depuis Z: pour fermer les conflits.'

