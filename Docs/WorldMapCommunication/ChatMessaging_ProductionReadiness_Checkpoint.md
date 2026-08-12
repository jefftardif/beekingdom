# Bee Kingdom - ChatMessaging Production Readiness Checkpoint

Date: 2026-07-16
Scope: chat/messagerie serveur uniquement. Aucun deploiement live, aucun secret reel, aucun changement Unity.

## Statut court

- `TEMP_CHAT_DOMAIN=chat.dravii.com`
- `DNS_TEMP_SUBDOMAIN_CONFIGURED=YES`
- `CLOUDFLARE_PROXY_DETECTED=YES`
- `SERVER_ADMIN_ACCESS=YES`
- `IIS_CONFIGURED=YES`
- `IIS_BINDING_PLAN_READY=YES`
- `IIS_WEBSOCKET_READY=YES`
- `ORIGIN_TLS_READY=YES`
- `FIREWALL_READY=YES`
- `CHAT_APP_BOUND=YES`
- `SQL_CONFIGURED=YES`
- `SQL_MIGRATIONS_APPLIED=YES`
- `PROD_CHAT_DEPLOY_READY=NO`

## Observation DNS

`chat.dravii.com` resout actuellement vers `172.64.80.1`, ce qui indique un proxy Cloudflare actif. L'origine attendue reste `104.129.128.136`.

## Etat serveur applique

- Acces admin WinRM confirme sur `srvesdt` avec `srvesdt\rdp_jeff`.
- Package serveur copie dans `C:\inetpub\BeeKingdom.ChatApi`.
- Site IIS dedie cree et demarre: `BeeKingdom.ChatApi`.
- App pool dedie cree et demarre: `BeeKingdom.ChatApi`.
- Binding HTTP cree: `104.129.128.136:80:chat.dravii.com`.
- Certificat Let's Encrypt installe: `CN=chat.dravii.com`, thumbprint `B59F0868E0AA91FBC34AD635C11845AE03995847`, expiration `2026-10-13 21:06:20`.
- Binding HTTPS cree: `104.129.128.136:443:chat.dravii.com`.
- Renouvellement win-acme cree: `[IIS] BeeKingdom.ChatApi, chat.dravii.com`, due `2026/9/9`.
- WebSocket Protocol installe: `Web-WebSockets=Installed`.
- Firewall Windows 80/443 confirme, avec regles dediees `BeeKingdom Chat HTTP 80` et `BeeKingdom Chat HTTPS 443`.
- Aucun binding HTTPS wildcard ou sans hostname n'a ete cree.
- Chat reste desactive: `Chat__Enabled=false`, `Chat__RealtimeEnabled=false`.
- Aucun secret ajoute.
- Aucun site existant n'a ete modifie; le nouveau binding est host-specific.

## Etat SQL applique

- Instance utilisee: `.\SQLEXPRESS01`.
- Base creee: `BeeKingdom`.
- Outil migration copie: `C:\inetpub\BeeKingdom.Tools`.
- Migrations appliquees: `010_schema_version.sql` a `060_chat_messaging.sql`.
- Tables chat presentes:
  - `ChatConversations`
  - `ChatConversationParticipants`
  - `ChatConversationSequences`
  - `ChatMessages`
  - `ChatInbox`
  - `ChatOutboxReceipts`
  - `ChatModerationReports`
- Compte runtime SQL: `IIS APPPOOL\BeeKingdom.ChatApi`.
- Droits runtime: `db_datareader`, `db_datawriter`.

## Checks serveur

- Origine HTTP `/health`: 200.
- Origine HTTP `/runtime/chat-readiness`: 200, `enabled=false`, `realtimeEnabled=false`.
- Origine HTTP `/chat/v1/capabilities`: 200, `server=false`, `realtime=false`.
- Origine HTTP SignalR negotiate: 200, transports incluent WebSockets.
- Public `http://chat.dravii.com/health`: 200.
- Public `https://chat.dravii.com/health`: 200.
- Public `https://chat.dravii.com/runtime/chat-readiness`: 200, `enabled=false`, `realtimeEnabled=false`.
- Public `https://chat.dravii.com/chat/v1/capabilities`: 200, `server=false`, `realtime=false`.
- Public `https://chat.dravii.com/chat/v1/realtime/negotiate`: 200.

## Mise a jour effectuee

Le runbook `Docs/WorldMapCommunication/ChatMessaging_ProductionReadiness_Runbook.md` documente maintenant:

- bindings IIS `chat.dravii.com` sur 80/443;
- certificat origin TLS et recommandation Cloudflare `Full (strict)`;
- WebSocket Protocol IIS pour SignalR `/chat/v1/realtime`;
- modele IIS + ASP.NET Core Module recommande;
- alternative reverse proxy IIS vers Kestrel local `127.0.0.1:5000`;
- headers proxy et forwarded headers ASP.NET Core;
- firewall 80/443 public et port interne Kestrel non public;
- checks `/health`, `/runtime/chat-readiness`, `/chat/v1/capabilities`, SignalR handshake;
- rollback par feature flags, retrait bindings IIS ou DNS only diagnostic.

## Fichiers produits/modifies depuis le dernier checkpoint

- Modifie: `Docs/WorldMapCommunication/ChatMessaging_ProductionReadiness_Runbook.md`
- Modifie: `Docs/WorldMapCommunication/ChatMessaging_ProductionReadiness_Checkpoint.md`

## Blocage exact

`PROD_CHAT_DEPLOY_READY=NO`

Blocage restant: il manque backup evidence, fenetre de maintenance, secrets ops hors depot si les endpoints ops doivent etre exposes, et la decision de bascule `Chat__Enabled=true`.

Action requise cote utilisateur/serveur: valider Cloudflare en Full strict, confirmer backup/fenetre de maintenance, puis autoriser la bascule chat.

## Prochaine action concrete

Prochaine action: confirmer backup/fenetre de maintenance, puis tester une activation controlee en gardant la possibilite rollback par `Chat__Enabled=false`.

## Restant avant GO

Configuration Cloudflare finale, backup evidence, fenetre de maintenance et secrets ops hors depot si requis.
