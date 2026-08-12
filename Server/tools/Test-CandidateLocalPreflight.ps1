[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$CandidatePath,
    [switch]$RunSmoke,
    [ValidateRange(1024,65535)] [int]$SmokePort = 5111
)
$ErrorActionPreference = 'Stop'
$candidate = [System.IO.Path]::GetFullPath($CandidatePath)
$serverRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..')).TrimEnd('\') + '\'
if (-not $candidate.StartsWith((Join-Path $serverRoot 'artifacts\candidates\'), [StringComparison]::OrdinalIgnoreCase)) { throw 'Le candidat doit rester sous Server/artifacts/candidates.' }
$manifestPath = Join-Path $candidate 'candidate.manifest.json'
if (-not (Test-Path $manifestPath)) { throw 'Manifest candidat absent.' }
$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$mismatches = [System.Collections.Generic.List[string]]::new()
foreach ($entry in $manifest.Files) {
    $path = Join-Path $candidate $entry.Path
    if (-not (Test-Path -LiteralPath $path)) { $mismatches.Add($entry.Path); continue }
    if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $entry.Sha256) { $mismatches.Add($entry.Path) }
}
if ($mismatches.Count) { throw "Divergences SHA-256: $($mismatches -join ', ')" }
$settings = Get-Content (Join-Path $candidate 'appsettings.Production.json') -Raw | ConvertFrom-Json
if ($settings.Chat.Enabled -ne $false -or $settings.Chat.RealtimeEnabled -ne $false -or $settings.Persistence.Provider -ne 'InMemory') { throw 'Configuration candidat non fail-closed.' }
if (Test-Path (Join-Path $candidate 'appsettings.Development.json')) { throw 'Configuration Development présente.' }
if ((Get-ChildItem $candidate -Recurse -Filter '*.pdb').Count) { throw 'Symboles PDB présents.' }
$smoke = $null
if ($RunSmoke) {
    $smokeScript = Join-Path $PSScriptRoot 'Test-ProductionLocal.ps1'
    $assembly = Join-Path $candidate 'BeeKingdom.Server.dll'
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $smokeScript -Port $SmokePort -NoBuild -AssemblyPath $assembly
    if ($LASTEXITCODE -ne 0) { throw 'Smoke local échoué.' }
    $smoke = 'Healthy'
}
[pscustomobject]@{ Success=$true; Candidate=(Split-Path $candidate -Leaf); ManifestFiles=$manifest.Files.Count; HashMismatches=0; ChatEnabled=$settings.Chat.Enabled; RealtimeEnabled=$settings.Chat.RealtimeEnabled; Smoke=$smoke; DeploymentAuthorized=$false } | ConvertTo-Json
