[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$HostRoot,

    [string]$VmRoot = 'C:\projets\beekingdomgame-master'
)

$ErrorActionPreference = 'Stop'
$ExpectedVersion = '6000.5.3f1'
$ExpectedRevision = 'c2eb47b3a2a9'

try {
    $unityProcesses = Get-Process -Name Unity -ErrorAction SilentlyContinue
    if ($null -ne $unityProcesses) {
        throw 'Unity est encore ouvert dans la VM. Ferme Unity avant la reparation.'
    }

    $resolvedHostRoot = (Resolve-Path -LiteralPath $HostRoot).Path
    $resolvedVmRoot = (Resolve-Path -LiteralPath $VmRoot).Path
    $sourcePath = Join-Path $resolvedHostRoot 'ProjectSettings\ProjectVersion.txt'
    $targetPath = Join-Path $resolvedVmRoot 'ProjectSettings\ProjectVersion.txt'

    if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
        throw "Reference officielle introuvable: $sourcePath"
    }
    if (-not (Test-Path -LiteralPath $targetPath -PathType Leaf)) {
        throw "Projet local VM introuvable: $targetPath"
    }

    $officialContent = Get-Content -LiteralPath $sourcePath -Raw
    if ($officialContent -notmatch [regex]::Escape("m_EditorVersion: $ExpectedVersion") -or
        $officialContent -notmatch [regex]::Escape($ExpectedRevision)) {
        throw "La reference partagee n'est pas la version officielle $ExpectedVersion ($ExpectedRevision)."
    }

    if ($sourcePath.Equals($targetPath, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'La source et la destination sont identiques. Ce reparateur doit etre lance dans la VM depuis Z:.'
    }

    $backupDirectory = Join-Path $resolvedVmRoot '.codex\unity-version-repair'
    New-Item -ItemType Directory -Path $backupDirectory -Force | Out-Null
    $timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $backupPath = Join-Path $backupDirectory "ProjectVersion.before-$timestamp.txt"
    Copy-Item -LiteralPath $targetPath -Destination $backupPath -Force
    Copy-Item -LiteralPath $sourcePath -Destination $targetPath -Force

    $sourceHash = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash
    $targetHash = (Get-FileHash -LiteralPath $targetPath -Algorithm SHA256).Hash
    if ($sourceHash -ne $targetHash) {
        throw 'La verification SHA256 du fichier restaure a echoue.'
    }

    Write-Host "Version officielle restauree: $ExpectedVersion ($ExpectedRevision)" -ForegroundColor Green
    Write-Host "Sauvegarde de l'ancien fichier: $backupPath" -ForegroundColor Cyan
    Write-Host ''
    Write-Host "Ne rouvre le projet qu'avec Unity 6000.5.3f1." -ForegroundColor Yellow
    Write-Host 'Relance ensuite Verifier-Synchronisation.cmd avant toute synchronisation.' -ForegroundColor Yellow
    exit 0
}
catch {
    Write-Host ''
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
