[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$serverRoot = Split-Path -Parent $PSScriptRoot
$path = Join-Path $serverRoot 'src\BeeKingdom.Server\appsettings.Production.json'
$settings = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json

$failures = [System.Collections.Generic.List[string]]::new()
function Require([bool]$condition, [string]$message) {
    if (-not $condition) { $failures.Add($message) }
}

Require ($settings.Persistence.Provider -eq 'InMemory') 'Persistence:Provider doit rester InMemory dans le fichier Production suivi.'
Require ($settings.Chat.Enabled -eq $false) 'Chat:Enabled doit rester false.'
Require ($settings.Chat.RealtimeEnabled -eq $false) 'Chat:RealtimeEnabled doit rester false.'
Require ($settings.Chat.ProtocolVersion -eq 'chat-v1') 'Chat:ProtocolVersion doit rester chat-v1 sans strategie de migration.'
Require ([int]$settings.Chat.IdempotencyReceiptRetentionDays -ge 30 -and [int]$settings.Chat.IdempotencyReceiptRetentionDays -le 3650) 'La retention Production doit rester entre 30 et 3650 jours pour couvrir la fenetre client maximale de 29 jours.'
Require ($settings.Ops.RequireAdminKey -eq $true) 'Ops:RequireAdminKey doit rester true.'
Require ($settings.Ops.RequireMigrationApplyKey -eq $true) 'Ops:RequireMigrationApplyKey doit rester true.'
Require ([string]::IsNullOrEmpty($settings.Ops.AdminKey)) 'Ops:AdminKey ne doit pas etre stocke dans le depot.'
Require ([string]::IsNullOrEmpty($settings.Ops.AdminKeySha256)) 'Ops:AdminKeySha256 doit etre injecte hors depot.'
Require ([string]::IsNullOrEmpty($settings.Ops.MigrationApplyKey)) 'Ops:MigrationApplyKey ne doit pas etre stocke dans le depot.'
Require ([string]::IsNullOrEmpty($settings.Ops.MigrationApplyKeySha256)) 'Ops:MigrationApplyKeySha256 doit etre injecte hors depot.'
Require ([string]::IsNullOrEmpty($settings.SqlServer.ConnectionString)) 'SqlServer:ConnectionString doit etre vide en Production.'
Require ([string]::IsNullOrEmpty($settings.SqlServer.RuntimeConnectionString)) 'SqlServer:RuntimeConnectionString doit etre vide en Production.'
Require ([string]::IsNullOrEmpty($settings.SqlServer.MigrationConnectionString)) 'SqlServer:MigrationConnectionString doit etre vide en Production.'
Require ($settings.RuntimeHandshake.Availability -eq 'ServerInPreparation') 'RuntimeHandshake doit rester ServerInPreparation.'
Require ($settings.AccountSessionReadiness.OfficialPersistenceClaimAllowed -eq $false) 'OfficialPersistenceClaimAllowed doit rester false.'
Require ($settings.WorldMapReadiness.OfficialProgressionEnabled -eq $false) 'OfficialProgressionEnabled doit rester false.'
Require ($settings.SqlProductionDryRun.RequireBackupEvidence -eq $true) 'La preuve de sauvegarde doit rester obligatoire.'
Require ($settings.SqlProductionDryRun.RequireMaintenanceWindow -eq $true) 'La fenetre de maintenance doit rester obligatoire.'
Require ($settings.SqlProductionDryRun.RollbackPlanAcknowledged -eq $false) 'Le rollback ne doit pas etre pre-acquitte dans le depot.'

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    throw "Configuration Production non sure: $($failures.Count) echec(s)."
}

[pscustomobject]@{
    Success = $true
    PersistenceProvider = $settings.Persistence.Provider
    ChatEnabled = $settings.Chat.Enabled
    RealtimeEnabled = $settings.Chat.RealtimeEnabled
    ProtocolVersion = $settings.Chat.ProtocolVersion
    RuntimeAvailability = $settings.RuntimeHandshake.Availability
    ExternalSqlRequired = $true
    ExternalOpsSecretsRequired = $true
} | ConvertTo-Json -Depth 3
