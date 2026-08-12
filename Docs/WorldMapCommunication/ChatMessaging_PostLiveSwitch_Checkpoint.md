# Bee Kingdom - ChatMessaging Post Live Switch Checkpoint

Date: 2026-07-16
Scope: bascule controlee chat live sur `chat.dravii.com`. Aucun changement Unity, autres sites IIS, bindings hors `BeeKingdom.ChatApi`, PNG, Wave5, BearDen ou APK.

## Verdict

- `CHAT_ENABLED=YES`
- `CHAT_REALTIME_ENABLED=YES`
- `PROD_CHAT_DEPLOY_READY=YES`
- `PUBLIC_HEALTH_OK=YES`
- `PUBLIC_READINESS_OK=YES`
- `PUBLIC_CAPABILITIES_OK=YES`
- `SIGNALR_NEGOTIATE_OK=YES`
- `ROLLBACK_READY=YES`

## Changement applique

Site IIS: `BeeKingdom.ChatApi`

Flags appliques dans `C:\inetpub\BeeKingdom.ChatApi\web.config`:

- `Chat__Enabled=true`
- `Chat__RealtimeEnabled=true`
- `Persistence__Provider=SqlServer`

App pool recycle:

- `Restart-WebAppPool -Name 'BeeKingdom.ChatApi'`

## Checks post-bascule

Depuis le serveur vers `https://chat.dravii.com`:

- `/health`: 200.
- `/runtime/chat-readiness`: 200, `enabled=true`, `realtimeEnabled=true`.
- `/chat/v1/capabilities`: 200, `server=true`, `realtime=true`.
- `/chat/v1/realtime/negotiate?negotiateVersion=1`: 200, transports incluent WebSockets.

SQL non destructif:

- `ChatConversationCount=0`
- `ChatMessageCount=0`
- `MigrationCount=7`

DNS/connectivite:

- DNS public via `1.1.1.1`: `chat.dravii.com -> 172.64.80.1`.
- DNS public via `8.8.8.8`: `chat.dravii.com -> 104.21.14.55`, `172.67.202.110`.
- GET public force via Cloudflare `104.21.14.55`: `https://chat.dravii.com/health` -> 200.
- Note: la resolution DNS par defaut de la session locale a temporairement retourne `chat.dravii.com` non resolu, mais les resolvers publics, le serveur, Cloudflare et les endpoints HTTPS sont OK.

## Rollback pret

Commande rollback:

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
- IIS/TLS/SQL restent en place.
