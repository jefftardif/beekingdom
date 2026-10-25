# BeeKingdom P19 readiness: strictly READ-ONLY; never change the API, SQL, or player state.
[CmdletBinding()]
param(
 [string]$SiteRoot='C:\inetpub\BeeKingdomApi',
 [string]$ApiBase='https://api-ops.beekingdomgame.com',
 [string]$SqlInstance='.\SQLEXPRESS01',
 [string]$Database='BeeKingdom',
 [string]$OutputPath=''
)
$ErrorActionPreference='Stop'
$block=New-Object 'System.Collections.Generic.List[string]'
function Block([string]$key) {if(-not $block.Contains($key)){[void]$block.Add($key)}}
function Code([string]$path) {
 try { return [int](Invoke-WebRequest -UseBasicParsing -Uri ($ApiBase.TrimEnd('/')+$path) -TimeoutSec 6).StatusCode }
 catch {if($_.Exception.Response) {return [int]$_.Exception.Response.StatusCode};return -1}
}
$r=[ordered]@{
  title='P19_POLLINATION_READ_ONLY_PREFLIGHT'
  checkedAtUtc=[DateTimeOffset]::UtcNow.ToString('o')
  changedProduction=$false
  iisPoolStarted=$false
  liveP18BinaryParity=$false
  existingBinaryBackup=$false
  sqlBackupRestoreVerified=$false
  flags=[ordered]@{}
  routes=[ordered]@{}
  sql=[ordered]@{
    currentIdentityHasReadAccess=$false
    requiredTablesVerified=$false
    tableCount=0
    pendingFlights=$null
    dueFlights=$null
  }
  blockers=@()
  rolloutAllowed=$false
}
try {
  $cfg=Get-Content (Join-Path $SiteRoot 'appsettings.Production.json') -Raw | ConvertFrom-Json
  foreach($n in @('Alliance','FloralSites','CivilPopulation','CivilExpeditions','FloralAllianceProjects')) {
    $v=$cfg.PSObject.Properties[$n].Value
    $r.flags[$n]=[ordered]@{
      enabled=[bool]$v.Enabled
      readOnlyPilot=[bool]$v.ReadOnlyPilot
      drainExistingFlights=[bool]$v.DrainExistingFlights
    }
  }
  if(-not $r.flags.Alliance.enabled){Block 'ALLIANCE_DISABLED'}
  if(-not $r.flags.FloralSites.enabled){Block 'FLORAL_SITES_DISABLED'}
  if(-not $r.flags.CivilPopulation.enabled){Block 'CIVIL_POPULATION_DISABLED'}
  if($r.flags.CivilExpeditions.enabled -or $r.flags.FloralAllianceProjects.enabled){Block 'P19_FLAGS_ALREADY_OPEN'}
} catch {Block 'CONFIG_CANNOT_BE_VALIDATED'}
try {
  $appcmd=Join-Path $env:windir 'System32\inetsrv\appcmd.exe'
  $value=@(& $appcmd list apppool BeeKingdomApi /text:state 2>$null)
  $r.iisPoolStarted=($LASTEXITCODE -eq 0 -and ($value -join '').Trim() -eq 'Started')
} catch {}
if(-not $r.iisPoolStarted){Block 'IIS_POOL_NOT_STARTED'}
$r.routes['health']=Code '/health'
$r.routes['floralUnauthenticated']=Code '/game/v1/world/floral-sites'
$r.routes['civilUnauthenticated']=Code '/game/v1/hives/00000000-0000-0000-0000-000000000001/civil-population'
$r.routes['expeditionsUnavailable']=Code '/game/v1/hives/00000000-0000-0000-0000-000000000001/civil-expeditions'
$r.routes['projectsUnavailable']=Code '/alliance/v1/alliances/00000000-0000-0000-0000-000000000001/floral-projects'
if($r.routes.health -ne 200){Block 'API_HEALTH_NOT_200'}
if($r.routes.floralUnauthenticated -ne 401 -or $r.routes.civilUnauthenticated -ne 401){Block 'READ_ACCESS_GUARD_UNEXPECTED'}
if($r.routes.expeditionsUnavailable -ne 503 -or $r.routes.projectsUnavailable -ne 503){Block 'DISABLED_ROUTE_GUARD_UNEXPECTED'}
try {
 $verified='C:\BeeKingdom\staging\pollination-P18-binary-20261024\verified'
 $ok=$true
 foreach($dll in @('BeeKingdom.Server.dll','BeeKingdom.HiveOperations.dll')){
   $a=Join-Path $SiteRoot $dll;$b=Join-Path $verified $dll
   if(-not (Test-Path $a) -or -not (Test-Path $b) -or (Get-FileHash $a -Algorithm SHA256).Hash -ne (Get-FileHash $b -Algorithm SHA256).Hash){$ok=$false}
 }
 $r.liveP18BinaryParity=$ok
} catch {}
if(-not $r.liveP18BinaryParity){Block 'PRODUCTION_BINARIES_NOT_VERIFIED'}
$r.existingBinaryBackup=Test-Path 'C:\BeeKingdom\deploy-backups\P18CivilReadOnly-20261024-020926'
if(-not $r.existingBinaryBackup){Block 'P18_BINARY_BACKUP_ABSENT'}
# A binary rollback does not prove SQL database restorability.
Block 'SQL_PRODUCTION_BACKUP_RESTORE_NOT_VERIFIED'
$exe=Get-Command 'C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\180\Tools\Binn\SQLCMD.EXE' -ErrorAction SilentlyContinue
if(-not $exe){$exe=Get-Command sqlcmd.exe -ErrorAction SilentlyContinue}
if(-not $exe){Block 'SQLCMD_MISSING'}
else {
  $q="SET NOCOUNT ON; SELECT 'TABLE=' + name FROM sys.tables WHERE schema_id = SCHEMA_ID('dbo') AND name IN ('FloralSiteStates','FloralCivilContributions','FloralCivilActiveMissions','FloralAllianceProjects','FloralAllianceProjectMembers','FloralAllianceProjectDeliveries','FloralAllianceProgression'); IF OBJECT_ID('dbo.FloralCivilActiveMissions','U') IS NOT NULL BEGIN SELECT 'PENDING=' + CONVERT(varchar(24),COUNT_BIG(*)) FROM dbo.FloralCivilActiveMissions; SELECT 'DUE=' + CONVERT(varchar(24),COUNT_BIG(*)) FROM dbo.FloralCivilActiveMissions WHERE ReturnsAtUtc <= SYSUTCDATETIME(); END"
  try {
    # Existing Windows identity only; no credentials, impersonation or elevation.
    $lines=@(& $exe.Source -S $SqlInstance -d $Database -E -C -b -l 5 -W -h -1 -Q $q 2>&1)
    if($LASTEXITCODE -ne 0){Block 'SQL_READ_NOT_AUTHORIZED_FOR_CURRENT_IDENTITY'}
    else {
      $r.sql.currentIdentityHasReadAccess=$true
      $names=@()
      foreach($line in $lines){
        $v=("$line").Trim()
        if($v -match '^TABLE=([A-Za-z0-9_]+)$'){$names+=$Matches[1]}
        if($v -match '^PENDING=(\d+)$'){$r.sql.pendingFlights=[long]$Matches[1]}
        if($v -match '^DUE=(\d+)$'){$r.sql.dueFlights=[long]$Matches[1]}
      }
      $r.sql.tableCount=@($names|Select-Object -Unique).Count
      $r.sql.requiredTablesVerified=$r.sql.tableCount -eq 7
      if(-not $r.sql.requiredTablesVerified){Block 'SQL_TABLES_096_TO_098_NOT_VERIFIED'}
      if($null -eq $r.sql.dueFlights){Block 'SQL_DUE_INDEX_NOT_VERIFIED'}
    }
  } catch {Block 'SQL_READ_NOT_AUTHORIZED_FOR_CURRENT_IDENTITY'}
}
$r.blockers=@($block.ToArray())
$r.rolloutAllowed=($block.Count -eq 0)
$json=$r | ConvertTo-Json -Depth 8
if($OutputPath){
  $target=[IO.Path]::GetFullPath($OutputPath)
  $site=[IO.Path]::GetFullPath($SiteRoot).TrimEnd('\')+'\'
  if($target.StartsWith($site,[StringComparison]::OrdinalIgnoreCase)){throw 'REPORT_CANNOT_BE_PUBLICLY_EXPOSED'}
  $parent=Split-Path -Parent $target
  if(-not (Test-Path $parent)){throw 'OUTPUT_DIRECTORY_MISSING'}
  [IO.File]::WriteAllText($target,$json,(New-Object System.Text.UTF8Encoding($false)))
}
Write-Output $json
