param([string]$RepositoryRoot = "")
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\..\.."))
}

$fixturePath = Join-Path $RepositoryRoot "Docs\AI\Missions\Pollinisation\Fixtures\FloralSites.Wave6.QA.example.json"
$mapPath = Join-Path $RepositoryRoot "Assets\BeeKingdom\Playground\WorldMapMmoFullscreenFoundationBootstrap.cs"
if (!(Test-Path -LiteralPath $fixturePath) -or !(Test-Path -LiteralPath $mapPath)) {
    throw "Pollination QA fixtures or canonical world map unavailable."
}

$config = Get-Content -LiteralPath $fixturePath -Raw -Encoding UTF8 | ConvertFrom-Json
$source = [IO.File]::ReadAllText($mapPath)
$surface = [regex]::Match($source, 'private const string WorldId = "([^"]+)"')
if (!$surface.Success) {
    throw "The canonical Wave6 world surface identity is unavailable."
}
$surfaceId = $surface.Groups[1].Value
$seen = New-Object "System.Collections.Generic.HashSet[string]"
$count = 0

if ($config.FloralSites.Enabled -or $config.CivilPopulation.Enabled -or $config.CivilExpeditions.Enabled -or $config.FloralAllianceProjects.Enabled) {
    throw "The QA fixture must never activate any pollination feature."
}

foreach ($site in $config.FloralSites.Sites) {
    if ($site.MapSurfaceId -cne $surfaceId) {
        throw "QA site $($site.SiteId) refers to a different map surface."
    }
    if (-not $seen.Add([string]$site.MapAnchorId)) {
        throw "QA anchor $($site.MapAnchorId) is duplicated."
    }
    $literal = '"' + [string]$site.MapAnchorId + '"'
    if (-not $source.Contains($literal)) {
        throw "QA site $($site.SiteId) does not use an existing world-map node."
    }
    $count++
}

if ($count -lt 2) {
    throw "At least two QA sites must be checked."
}
Write-Output "P6_WAVE6_ANCHOR_QA_PASS=$count SURFACE=$surfaceId FLAGS=OFF"
