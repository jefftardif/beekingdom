#requires -Version 5.1
# BeeKingdom semantic terrain production preflight. READ ONLY.
# No game settings, SQL data, or live IIS content are modified.
[CmdletBinding()]
param(
 [string]$ApiBase='https://api-ops.beekingdomgame.com',
 [string]$ApiRoot='C:\inetpub\BeeKingdomApi',
 [string]$ReportPath='C:\BeeKingdom\staging\WorldMapSemantic-ProductionGate-20261106.json'
)
$ErrorActionPreference='Stop'
$reasons=New-Object 'System.Collections.Generic.List[string]'
$healthy=$false
try {
 $h=Invoke-RestMethod -Uri ($ApiBase.TrimEnd('/')+'/health') -TimeoutSec 8
 $healthy=($h.status -eq 'Healthy')
 if(-not $healthy){$reasons.Add('Production API health not Healthy')}
} catch {$reasons.Add('Production API health endpoint not reachable')}
try {
 $state=Invoke-RestMethod -Uri ($ApiBase.TrimEnd('/')+'/runtime/world-map-readiness') -TimeoutSec 8
 if($state.environment -ne 'Production'){$reasons.Add('Wrong environment')}
 if($state.worldMapStatus -ne 'Live'){$reasons.Add('WorldMapStatus is not Live')}
 if($state.readOnly -ne $false -or $state.nonLive -ne $false){$reasons.Add('World map remains read-only / non-live')}
 if($state.productionRouteProven -ne $true){$reasons.Add('Production game route not proven')}
 if($state.mapGameplayEnabled -ne $true){$reasons.Add('Map gameplay disabled')}
 if($state.realTimeSynchronizationEnabled -ne $true){$reasons.Add('World synchronization disabled')}
} catch {$reasons.Add('World-map readiness unavailable')}
$approvedRevision=''
$cfgPath=Join-Path $ApiRoot 'appsettings.Production.json'
if(Test-Path -LiteralPath $cfgPath){
 try{
  $cfg=Get-Content -LiteralPath $cfgPath -Raw|ConvertFrom-Json
  $semantic=$cfg.WorldMapSemanticTerrain
  $approvedRevision=[string]$semantic.ApprovedRevision
  if($semantic.Enabled -ne $true -or [string]::IsNullOrWhiteSpace($approvedRevision) -or
     [string]::IsNullOrWhiteSpace([string]$semantic.ApprovedManifestPath) -or
     -not (Test-Path -LiteralPath ([string]$semantic.ApprovedManifestPath))){
     $reasons.Add('Approved version-pinned semantic terrain absent')
  }
  $sql=$cfg.WorldMapAtomicReservations
  if($sql.Enabled -ne $true -or $sql.ServerEnforced -ne $true -or
     [string]::IsNullOrWhiteSpace([string]$sql.SchemaVersion)){
     $reasons.Add('Atomic SQL spatial reservations not configured')
  }
 }catch{$reasons.Add('Semantic/SQL production configuration invalid')}
}else{$reasons.Add('Production appsettings missing')}
# This endpoint must be implemented in the gameplay server before live
# activation. A mere config flag or shared DLL does not establish authority.
try{
 $proof=Invoke-RestMethod -Uri ($ApiBase.TrimEnd('/')+'/runtime/world-map-semantic-placement-readiness') -TimeoutSec 8
 if($proof.serverEnforced -ne $true -or $proof.atomicSqlReservations -ne $true -or
    $proof.terrainReviewed -ne $true -or $proof.legacyMaskDisabled -ne $true){
    $reasons.Add('Runtime semantic admission / atomic SQL proof insufficient')
 }
}catch{$reasons.Add('No live server endpoint proving semantic admission and atomic reservations')}
$unique=@($reasons | Select-Object -Unique)
$report=[ordered]@{
 schema='bk-worldmap-semantic-activation-gate-v1'
 checkedAtUtc=[DateTime]::UtcNow.ToString('o')
 readOnlyCheck=$true;productionChanged=$false;sqlWritten=$false
 apiHealthy=$healthy;approvedTerrainRevision=$approvedRevision
 liveActivationReady=($unique.Count -eq 0);blockerCount=$unique.Count;blockers=$unique
}
$parent=Split-Path -Path $ReportPath -Parent
if(-not (Test-Path -LiteralPath $parent)){[IO.Directory]::CreateDirectory($parent)|Out-Null}
[IO.File]::WriteAllText($ReportPath,($report|ConvertTo-Json -Depth 6),[Text.UTF8Encoding]::new($false))
Write-Output ('API_HEALTH='+$(if($healthy){'PASS'}else{'FAIL'}))
Write-Output ('WORLD_MAP_ACTIVATION='+$(if($unique.Count -eq 0){'READY'}else{'BLOCKED'}))
foreach($reason in $unique){Write-Output ('BLOCKER='+$reason)}
Write-Output ('NO_PRODUCTION_MUTATION=True')
Write-Output ('REPORT='+$ReportPath)
if($unique.Count){exit 2}
exit 0