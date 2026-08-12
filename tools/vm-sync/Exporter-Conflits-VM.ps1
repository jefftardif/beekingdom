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
    Write-Host 'Aucun conflit a exporter.'
    exit 0
}

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$exportRoot = Join-Path $resolvedHostRoot ".codex\vm-sync-conflicts\$stamp"
$manifest = [Collections.Generic.List[string]]::new()
$manifest.Add("Export UTC: $([DateTime]::UtcNow.ToString('o'))")
$manifest.Add("Rapport VM: $reportPath")
$manifest.Add('')

foreach ($relativePath in $conflicts) {
    foreach ($side in @(
        @{ Name = 'vm'; Root = $resolvedVmRoot },
        @{ Name = 'ordinateur'; Root = $resolvedHostRoot }
    )) {
        $sourcePath = Join-Path $side.Root $relativePath
        if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
            $manifest.Add("ABSENT`t$($side.Name)`t$relativePath")
            continue
        }

        $destinationPath = Join-Path (Join-Path $exportRoot $side.Name) $relativePath
        $destinationDirectory = Split-Path -Parent $destinationPath
        [void](New-Item -ItemType Directory -Path $destinationDirectory -Force)
        Copy-Item -LiteralPath $sourcePath -Destination $destinationPath -Force
        $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $destinationPath).Hash
        $manifest.Add("$hash`t$($side.Name)`t$relativePath")
    }
}

$manifestPath = Join-Path $exportRoot 'manifest.txt'
$manifest | Set-Content -LiteralPath $manifestPath -Encoding UTF8

Write-Host "Conflits exportes: $($conflicts.Count)"
Write-Host "Dossier sur l'ordinateur principal: $exportRoot"
Write-Host 'Aucun fichier du projet n''a ete remplace.'

