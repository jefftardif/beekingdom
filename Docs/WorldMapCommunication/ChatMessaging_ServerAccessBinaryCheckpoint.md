# Bee Kingdom - ChatMessaging Server Access Binary Checkpoint

Date: 2026-07-16
Verdict: `A`

## Gates

- `SERVER_ADMIN_ACCESS=YES`
- `IIS_CONFIGURED=YES`
- `FIREWALL_CONFIGURED=YES`
- `WEBSOCKET_ENABLED=YES`
- `ORIGIN_TLS_CONFIGURED=YES`
- `CHAT_APP_BOUND=YES`
- `SQL_CONFIGURED=YES`
- `SQL_MIGRATIONS_APPLIED=YES`
- `PROD_CHAT_DEPLOY_READY=NO`

## Acces serveur

- Serveur: `srvesdt`
- IP: `104.129.128.136`
- Session distante: `srvesdt\rdp_jeff`
- Admin distant: `True`
- Transport utilise: WinRM / PowerShell Remoting

## IIS / runtime

- IIS Web Server: installe.
- ASP.NET Core Hosting Bundle .NET 8: installe.
- WebSocket Protocol: installe.
- Site IIS chat: `BeeKingdom.ChatApi`, `Started`.
- App pool chat: `BeeKingdom.ChatApi`, dedie.
- Dossier app: `C:\inetpub\BeeKingdom.ChatApi`.

## Bindings

- HTTP: `104.129.128.136:80:chat.dravii.com`
- HTTPS: `104.129.128.136:443:chat.dravii.com`
- Certificat: Let's Encrypt `CN=chat.dravii.com`
- Thumbprint: `B59F0868E0AA91FBC34AD635C11845AE03995847`
- Expiration: `2026-10-13 21:06:20`
- Renouvellement win-acme: `[IIS] BeeKingdom.ChatApi, chat.dravii.com`, due `2026/9/9`

## Firewall

- `BeeKingdom Chat HTTP 80`: enabled.
- `BeeKingdom Chat HTTPS 443`: enabled.
- IIS HTTP/HTTPS rules: enabled.
- Ports ecoutes par IIS/http.sys: `80`, `443`.
- Aucun port Kestrel public requis par le modele actuel.

## SQL

- Instance locale: `.\SQLEXPRESS01`.
- Database: `BeeKingdom`.
- Migrations appliquees: `010_schema_version.sql` a `060_chat_messaging.sql`.
- Tables chat confirmees: `ChatConversations`, `ChatConversationParticipants`, `ChatConversationSequences`, `ChatMessages`, `ChatInbox`, `ChatOutboxReceipts`, `ChatModerationReports`.
- Runtime SQL principal: `IIS APPPOOL\BeeKingdom.ChatApi`.
- Runtime SQL roles: `db_datareader`, `db_datawriter`.

## Checks

- DNS public: `chat.dravii.com` passe par Cloudflare.
- `https://chat.dravii.com/health`: 200.
- `https://chat.dravii.com/runtime/chat-readiness`: 200.
- `https://chat.dravii.com/chat/v1/capabilities`: 200.
- `https://chat.dravii.com/chat/v1/realtime/negotiate`: 200.

## Blocage restant

`PROD_CHAT_DEPLOY_READY=NO`

Blocage volontaire de bascule: `Chat__Enabled=false` et `Chat__RealtimeEnabled=false` restent actifs. Action minimale requise: confirmer backup/fenetre de maintenance/rollback, puis autoriser la bascule controlee `Chat__Enabled=true`.
