# Bee Kingdom - ChatMessaging Server IIS Checkpoint

Date: 2026-07-16
Scope: configuration serveur chat/API uniquement. Aucun secret reel, aucun changement Unity.

Derniere verification: 2026-07-16, reprise checkpoint coordinateur.

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

## Session serveur

- Serveur: `srvesdt`
- IP: `104.129.128.136`
- Session distante: `srvesdt\rdp_jeff`
- Admin: `True`

## Actions appliquees

- Copie du package Bee Kingdom Server vers `C:\inetpub\BeeKingdom.ChatApi`.
- Creation du site IIS dedie `BeeKingdom.ChatApi`.
- Creation de l'app pool dedie `BeeKingdom.ChatApi`.
- Binding HTTP host-specific: `104.129.128.136:80:chat.dravii.com`.
- Certificat Let's Encrypt installe pour `chat.dravii.com`.
- Binding HTTPS host-specific: `104.129.128.136:443:chat.dravii.com`.
- Renouvellement win-acme configure: `[IIS] BeeKingdom.ChatApi, chat.dravii.com`, due `2026/9/9`.
- Installation IIS WebSocket Protocol: `Web-WebSockets=Installed`.
- Creation/activation firewall:
  - `BeeKingdom Chat HTTP 80`
  - `BeeKingdom Chat HTTPS 443`
- Configuration app sans secret:
  - `ASPNETCORE_ENVIRONMENT=Production`
  - `Persistence__Provider=SqlServer`
  - `ConnectionStrings__BeeKingdomRuntime=Server=.\SQLEXPRESS01;Database=BeeKingdom;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;`
  - `ConnectionStrings__BeeKingdomMigrations=Server=.\SQLEXPRESS01;Database=BeeKingdom;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;`
  - `Chat__Enabled=false`
  - `Chat__RealtimeEnabled=false`

## SQL local

- Instance: `.\SQLEXPRESS01`.
- Database: `BeeKingdom`.
- Migrations appliquees: `010_schema_version.sql` a `060_chat_messaging.sql`.
- Tables chat confirmees: `ChatConversations`, `ChatConversationParticipants`, `ChatConversationSequences`, `ChatMessages`, `ChatInbox`, `ChatOutboxReceipts`, `ChatModerationReports`.
- Principal SQL runtime: `IIS APPPOOL\BeeKingdom.ChatApi`.
- Droits runtime: `db_datareader`, `db_datawriter`.

## Non-interference IIS

- Aucun binding existant d'autres sites n'a ete modifie.
- Aucun binding HTTPS wildcard ou sans hostname n'a ete ajoute.
- Les sites existants restent separes et listes dans IIS.
- Le nouveau site utilise un app pool dedie et un dossier dedie.

## Checks

- Verification admin: `srvesdt\rdp_jeff`, `IS_ADMIN=True`.
- Site IIS `BeeKingdom.ChatApi`: `Started`.
- Bindings actifs:
  - `104.129.128.136:80:chat.dravii.com`
  - `104.129.128.136:443:chat.dravii.com`
- WebSocket Protocol: `Installed`.
- Firewall:
  - `BeeKingdom Chat HTTP 80=True`
  - `BeeKingdom Chat HTTPS 443=True`
  - regles IIS HTTP/HTTPS=True
- Ports publics ecoutes par IIS/http.sys:
  - `80`
  - `443`
- Certificat: `CN=chat.dravii.com`, Let's Encrypt, thumbprint `B59F0868E0AA91FBC34AD635C11845AE03995847`, expiration `2026-10-13 21:06:20`.
- Renouvellement: `[IIS] BeeKingdom.ChatApi, chat.dravii.com`, due `2026/9/9`.
- `http://104.129.128.136/health` avec host `chat.dravii.com`: 200.
- `http://104.129.128.136/runtime/chat-readiness` avec host `chat.dravii.com`: 200, chat desactive.
- `http://104.129.128.136/chat/v1/capabilities` avec host `chat.dravii.com`: 200, server false.
- SignalR negotiate origine HTTP: 200, WebSockets annonce.
- `http://chat.dravii.com/health`: 200.
- `https://chat.dravii.com/health`: 200.
- `https://chat.dravii.com/runtime/chat-readiness`: 200, chat desactive.
- `https://chat.dravii.com/chat/v1/capabilities`: 200, server false.
- `https://chat.dravii.com/chat/v1/realtime/negotiate`: 200.

## Blocage exact

Production chat reste non prete car backup evidence, fenetre de maintenance, secrets ops hors depot si requis et bascule `Chat__Enabled=true` ne sont pas encore valides.

## Prochaine action concrete

Valider Cloudflare Full strict, backup/fenetre de maintenance et rollback, puis effectuer une activation controlee de `Chat__Enabled=true` si la fenetre de maintenance est confirmee.
