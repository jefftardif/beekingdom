# Bee Kingdom - ChatMessaging Pre Live Switch Checkpoint

Date: 2026-07-16
Scope: bascule controlee chat uniquement. Aucun changement Unity, autres sites IIS, bindings existants hors `BeeKingdom.ChatApi`, PNG, Wave5, BearDen ou APK.

## Etat avant changement

- Site IIS: `BeeKingdom.ChatApi`
- Site state: `Started`
- Bindings:
  - `104.129.128.136:80:chat.dravii.com`
  - `104.129.128.136:443:chat.dravii.com`
- Persistence: `SqlServer`
- SQL instance: `.\SQLEXPRESS01`
- SQL database: `BeeKingdom`
- Migrations: appliquees jusqu'a `060_chat_messaging.sql`
- `Chat__Enabled=false`
- `Chat__RealtimeEnabled=false`

## Preconditions deja confirmees

- `IIS_CONFIGURED=YES`
- `FIREWALL_CONFIGURED=YES`
- `WEBSOCKET_ENABLED=YES`
- `ORIGIN_TLS_CONFIGURED=YES`
- `CHAT_APP_BOUND=YES`
- `SQL_CONFIGURED=YES`
- `SQL_MIGRATIONS_APPLIED=YES`

## Rollback exact

Si un check critique echoue apres bascule:

```powershell
$webConfigPath = 'C:\inetpub\BeeKingdom.ChatApi\web.config'
[xml]$webConfig = Get-Content $webConfigPath
$envVars = $webConfig.configuration.location.'system.webServer'.aspNetCore.environmentVariables.environmentVariable
($envVars | Where-Object name -eq 'Chat__Enabled').value = 'false'
($envVars | Where-Object name -eq 'Chat__RealtimeEnabled').value = 'false'
$webConfig.Save($webConfigPath)
Import-Module WebAdministration
Restart-WebAppPool -Name 'BeeKingdom.ChatApi'
```

Rollback target:

- `Chat__Enabled=false`
- `Chat__RealtimeEnabled=false`
- conserver IIS/TLS/SQL en place.
