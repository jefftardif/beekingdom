[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$ShareName = 'BeeKingdomHost'
$ProjectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$ExpectedProjectRoot = 'C:\projets\beekingdomgame-master'
$Account = "$env:COMPUTERNAME\$env:USERNAME"

try {
    if (-not $ProjectRoot.Equals($ExpectedProjectRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Le projet doit etre dans $ExpectedProjectRoot. Dossier actuel: $ProjectRoot"
    }

    $projectVersion = Join-Path $ProjectRoot 'ProjectSettings\ProjectVersion.txt'
    if (-not (Test-Path -LiteralPath $projectVersion -PathType Leaf)) {
        throw "Projet Unity Bee Kingdom invalide: $ProjectRoot"
    }

    $existingShare = Get-SmbShare -Name $ShareName -ErrorAction SilentlyContinue
    if ($null -ne $existingShare) {
        if (-not $existingShare.Path.Equals($ProjectRoot, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Le partage $ShareName existe deja pour un autre dossier: $($existingShare.Path)"
        }

        Set-SmbShare -Name $ShareName `
            -CachingMode None `
            -FolderEnumerationMode AccessBased `
            -EncryptData $true `
            -Force | Out-Null
        Grant-SmbShareAccess -Name $ShareName -AccountName $Account -AccessRight Change -Force | Out-Null
        Write-Host "Le partage $ShareName existait deja et a ete verifie." -ForegroundColor Green
    }
    else {
        New-SmbShare -Name $ShareName `
            -Path $ProjectRoot `
            -ChangeAccess $Account `
            -CachingMode None `
            -FolderEnumerationMode AccessBased `
            -EncryptData $true `
            -Description 'Bee Kingdom - synchronisation VM locale' | Out-Null
        Write-Host "Le partage $ShareName a ete cree." -ForegroundColor Green
    }

    Write-Host ''
    Write-Host 'Depuis la VM en session standard, ouvre:' -ForegroundColor Cyan
    Write-Host "\\$env:COMPUTERNAME\$ShareName" -ForegroundColor White
    Write-Host ''
    Write-Host "Compte a utiliser si Windows le demande: $Account" -ForegroundColor Cyan
    Write-Host 'Utilise le mot de passe Windows du compte principal, pas son NIP.'
    exit 0
}
catch {
    Write-Host ''
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
