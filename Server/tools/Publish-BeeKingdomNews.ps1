[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Slug,
    [Parameter(Mandatory = $true)][string]$TitleEn,
    [Parameter(Mandatory = $true)][string]$TitleFr,
    [Parameter(Mandatory = $true)][string]$ExcerptEn,
    [Parameter(Mandatory = $true)][string]$ExcerptFr,
    [Parameter(Mandatory = $true)][string]$BodyEn,
    [Parameter(Mandatory = $true)][string]$BodyFr
)

$ErrorActionPreference = "Stop"

if ($Slug -notmatch "^[a-z0-9]+(-[a-z0-9]+)*$" -or $Slug.Length -gt 200) {
    throw "Slug invalide: utilisez uniquement a-z, 0-9 et des tirets simples."
}

foreach ($required in @($TitleEn, $TitleFr, $BodyEn, $BodyFr)) {
    if ([string]::IsNullOrWhiteSpace($required)) { throw "Les deux titres et les deux contenus sont obligatoires." }
}

$inputs = [ordered]@{
    slug = $Slug
    titleEn = $TitleEn
    titleFr = $TitleFr
    excerptEn = $ExcerptEn
    excerptFr = $ExcerptFr
    bodyEn = $BodyEn
    bodyFr = $BodyFr
}

$json = $inputs | ConvertTo-Json -Compress
$json | gh workflow run publish-news.yml --repo jefftardif/beekingdom --ref deploy --json
if ($LASTEXITCODE -ne 0) { throw "Impossible de déclencher le workflow de publication." }

Write-Host "Publication demandée pour le slug: $Slug"
