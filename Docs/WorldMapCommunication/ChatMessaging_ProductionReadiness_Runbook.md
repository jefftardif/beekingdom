# Bee Kingdom - ChatMessaging Production Readiness Runbook

Date: 2026-07-16
Target: Windows Server 2025 / IIS / SQL Server
Production IP known: `104.129.128.136`
Temporary chat domain: `chat.dravii.com`
Scope: chat/messagerie serveur uniquement.

## Decision actuelle

`READY_FOR_PROD_DEPLOY=BLOCKED_EXTERNAL_INPUT`
`TEMP_CHAT_DOMAIN=chat.dravii.com`
`DNS_TEMP_SUBDOMAIN_CONFIGURED=YES`
`CLOUDFLARE_PROXY_DETECTED=YES`
`SERVER_ADMIN_ACCESS=YES`
`IIS_CONFIGURED=YES`
`IIS_BINDING_PLAN_READY=YES`
`IIS_WEBSOCKET_READY=YES`
`ORIGIN_TLS_READY=YES`
`FIREWALL_READY=YES`
`CHAT_APP_BOUND=YES`
`SQL_CONFIGURED=YES`
`SQL_MIGRATIONS_APPLIED=YES`
`PROD_CHAT_DEPLOY_READY=NO`

Les gates locales sont vertes, le package local est pret, et le sous-domaine temporaire `chat.dravii.com` est cree. La verification locale resout actuellement vers `172.64.80.1`, ce qui indique un proxy Cloudflare actif devant l'origine `104.129.128.136`. Une configuration serveur minimale a ete appliquee sur `srvesdt`: site IIS dedie, app pool dedie, bindings HTTP/HTTPS host-specific, certificat Let's Encrypt, WebSocket Protocol, firewall 80/443 et SQL Server local. Le deploiement chat production reste bloque uniquement par la decision de bascule controlee `Chat__Enabled=true`, secrets ops si necessaires, backup/fenetre de maintenance et validation finale.

## Entrees externes obligatoires

Deja fourni:

- DNS temporaire: `chat.dravii.com`.
- Record DNS attendu cote zone: `A` vers `104.129.128.136`.
- Resolution publique observee: `172.64.80.1`, compatible avec Cloudflare proxy orange actif.
- Acces admin WinRM confirme: `srvesdt\rdp_jeff`, admin `True`.
- IIS site/app HTTP cree: `BeeKingdom.ChatApi`.
- App pool dedie: `BeeKingdom.ChatApi`.
- Dossier applicatif: `C:\inetpub\BeeKingdom.ChatApi`.
- Binding HTTP: `104.129.128.136:80:chat.dravii.com`.
- Certificat Let's Encrypt installe dans `LocalMachine\WebHosting`.
- Binding HTTPS: `104.129.128.136:443:chat.dravii.com`.
- Renouvellement win-acme ajoute: `[IIS] BeeKingdom.ChatApi, chat.dravii.com`, prochaine echeance apres `2026/9/9`.
- WebSocket Protocol IIS: installe.
- Firewall Windows: regles `BeeKingdom Chat HTTP 80` et `BeeKingdom Chat HTTPS 443` activees.
- Checks origine HTTP: `/health`, `/runtime/chat-readiness`, `/chat/v1/capabilities` retournent 200 avec chat desactive.
- SQL local choisi: `.\SQLEXPRESS01`.
- Base creee: `BeeKingdom`.
- Migrations appliquees: `010_schema_version.sql` a `060_chat_messaging.sql`.
- Tables chat presentes: `ChatConversations`, `ChatConversationParticipants`, `ChatConversationSequences`, `ChatMessages`, `ChatInbox`, `ChatOutboxReceipts`, `ChatModerationReports`.
- Runtime app pool SQL: `IIS APPPOOL\BeeKingdom.ChatApi`, roles `db_datareader` et `db_datawriter`.

Fournir et valider avant GO:

- Backup evidence avant bascule chat active.
- Compte de service Windows/IIS App Pool, droits fichiers, droits SQL minimaux.
- Regles firewall entrantes/sortantes attendues, au minimum 80/443 publics si IIS gere HTTP/HTTPS.
- Binding IIS final: host name `chat.dravii.com`, port 443, certificat.
- Strategie reverse proxy: IIS in-process/out-of-process vers Kestrel, ou IIS frontal standard ASP.NET Core.
- Configuration Cloudflare: proxy orange ou DNS only temporaire.
- Configuration ASP.NET Core forwarded headers si Cloudflare/IIS reverse proxy est utilise.
- Fenetre de maintenance.
- Methode rollback approuvee.
- Valeurs secretes ops hors depot: admin key hash et migration apply key hash.

## Gates GO/NO-GO

GO seulement si tout est vrai:

- `dotnet build Server/BeeKingdom.Server.slnx --no-restore` PASS.
- `dotnet test Server/tests/BeeKingdom.Tests/BeeKingdom.Tests.csproj --no-build` PASS.
- Packages Release locaux disponibles.
- `Chat:Enabled=false` en production au moment du deploiement initial.
- `Chat:RealtimeEnabled=false` en production au moment du deploiement initial, sauf decision de bascule explicite.
- `Persistence:Provider=SqlServer` configure uniquement dans l'environnement cible, pas avec secret dans le depot.
- `060_chat_messaging.sql` inclus dans les migrations.
- Backup SQL verifie.
- `chat.dravii.com` resout vers `104.129.128.136`.
- Si Cloudflare proxy orange est actif, `chat.dravii.com` peut resoudre vers une IP Cloudflare; dans ce cas verifier dans Cloudflare que l'origine cible reste `104.129.128.136`.
- TLS valide sur IIS pour `chat.dravii.com`.
- Firewall ouvert uniquement sur ports requis, au minimum 80/443 entrants.
- Cloudflare WebSocket support actif si proxy orange utilise.
- IIS site/app cree pour Bee Kingdom Chat/API avec binding `chat.dravii.com` sur 443 et 80 si redirection HTTP vers HTTPS.
- ASP.NET Core Hosting Bundle installe/valide si IIS heberge l'application via ASP.NET Core Module.
- Modele d'hebergement confirme: ASP.NET Core Module IIS recommande, ou reverse proxy IIS vers Kestrel local.
- `/health`, `/runtime/chat-readiness`, `/chat/v1/capabilities` valides sur `https://chat.dravii.com`.
- SignalR handshake valide sur `https://chat.dravii.com/chat/v1/realtime` en staging/local avant activation production.
- Rollback executable.

NO-GO immediat si:

- Un secret reel doit etre commite.
- Le certificat TLS pour `chat.dravii.com` est absent ou invalide.
- La connexion SQL production n'est pas testable.
- `chat.dravii.com` ne resout ni vers l'origine `104.129.128.136` en DNS only ni vers Cloudflare proxy valide.
- Le backup n'est pas disponible.
- Le compte de service n'a pas de droits explicites.
- Les endpoints ops/migrations ne sont pas proteges.
- IIS WebSocket Protocol n'est pas actif alors que SignalR doit etre active.
- Les headers proxy/forwarded headers ne sont pas correctement transmis ou interpretes.
- Le chat devrait etre active sans fenetre de controle.

## DNS, TLS, Cloudflare et SignalR

Etat actuel:

- `TEMP_CHAT_DOMAIN=chat.dravii.com`
- `DNS_TEMP_SUBDOMAIN_CONFIGURED=YES`
- `CLOUDFLARE_PROXY_DETECTED=YES`
- Record attendu: `chat.dravii.com A 104.129.128.136`
- Resolution publique observee: `chat.dravii.com A 172.64.80.1`

Verification DNS locale:

```powershell
Resolve-DnsName chat.dravii.com -Type A
```

Cloudflare, si utilise:

- Recommandation cible: mode SSL/TLS `Full (strict)`.
- Activer/conserver le support WebSocket Cloudflare.
- Proxy orange possible si le certificat origine IIS pour `chat.dravii.com` est valide, si l'origine Cloudflare pointe bien vers `104.129.128.136`, et si SignalR negocie correctement.
- Si diagnostic reseau/TLS/SignalR necessaire, passer temporairement `chat.dravii.com` en `DNS only`, verifier l'origine IIS directement, puis reactiver le proxy orange.
- Eviter `Flexible TLS`, car il casse le modele de confiance origine et peut masquer des problemes HTTPS/IIS.

SignalR/WebSocket:

- Hub: `https://chat.dravii.com/chat/v1/realtime`.
- WebSocket attendu via HTTPS/TLS, avec fallback SignalR possible selon client.
- IIS doit avoir WebSockets active: `Web-WebSockets`.
- Si reverse proxy explicite est ajoute devant Kestrel, il doit transmettre `Upgrade`, `Connection`, `Host`, `X-Forwarded-Proto` et `X-Forwarded-For`.
- Cote ASP.NET Core, valider la prise en compte de `X-Forwarded-For`, `X-Forwarded-Proto` et `Host` avant d'activer redirection HTTPS stricte derriere Cloudflare.
- Garder `Chat__RealtimeEnabled=false` au deploiement initial, puis activer seulement apres validation REST et verification d'appartenance conversation cote hub.

## Plan IIS et modele d'hebergement

Gate actuel:

- `IIS_BINDING_PLAN_READY=YES`
- `IIS_CONFIGURED=YES`
- `IIS_WEBSOCKET_READY=YES`
- `ORIGIN_TLS_READY=YES`
- `FIREWALL_READY=YES`
- `CHAT_APP_BOUND=YES`

Site/app cible:

- Site IIS: `BeeKingdom.Server`.
- Hostname HTTP: `chat.dravii.com` sur port 80.
- Hostname HTTPS: `chat.dravii.com` sur port 443.
- App pool: `BeeKingdom.Server`.
- Identite recommandee: compte de service dedie ou `ApplicationPoolIdentity` avec droits minimaux.
- Dossier applicatif: `C:\inetpub\BeeKingdom.Server`.
- Logs IIS: `%SystemDrive%\inetpub\logs\LogFiles`.
- Logs applicatifs: stdout ASP.NET Core desactive par defaut, a activer temporairement seulement pour diagnostic controle.

Modele recommande:

- Priorite: IIS + ASP.NET Core Module avec package publie `BeeKingdom.Server`.
- Port public: 443, et 80 uniquement pour redirection HTTP vers HTTPS ou challenge ACME.
- Aucun port Kestrel public.
- Si modele out-of-process/Kestrel local est retenu, limiter Kestrel a `127.0.0.1:5000`.

Modele alternatif:

- IIS reverse proxy explicite vers Kestrel local `http://127.0.0.1:5000`.
- Port interne `5000` non public et bloque au firewall entrant externe.
- Proxy doit transmettre `Host`, `X-Forwarded-For`, `X-Forwarded-Proto`, `Upgrade` et `Connection`.
- ASP.NET Core doit etre configure pour interpreter les forwarded headers afin d'eviter boucles de redirection HTTPS ou mauvais scheme.

Pre-requis IIS:

```powershell
Install-WindowsFeature Web-Server,Web-Asp-Net45,Web-WebSockets
```

Pre-requis runtime:

- Installer/valider ASP.NET Core Hosting Bundle compatible .NET 8.
- Redemarrer IIS apres installation du Hosting Bundle.
- Verifier que WebSocket Protocol est installe avant activation SignalR.

Firewall:

- Autoriser TCP 80 entrant uniquement si redirection HTTP vers HTTPS ou challenge certificat requis.
- Autoriser TCP 443 entrant.
- Ne pas exposer le port interne Kestrel si reverse proxy utilise.
- Ne pas exposer SQL publiquement.

## Etat IIS applique sur le serveur

Session:

- Serveur: `srvesdt` / `104.129.128.136`.
- Session admin: `srvesdt\rdp_jeff`.
- Admin distant confirme: `SERVER_ADMIN_ACCESS=YES`.

Configuration appliquee:

- Package copie dans `C:\inetpub\BeeKingdom.ChatApi`.
- Site IIS cree: `BeeKingdom.ChatApi`.
- App pool cree: `BeeKingdom.ChatApi`, `managedRuntimeVersion=""`, `startMode=AlwaysRunning`.
- Binding HTTP cree: `104.129.128.136:80:chat.dravii.com`.
- Certificat Let's Encrypt cree: `CN=chat.dravii.com`, thumbprint `B59F0868E0AA91FBC34AD635C11845AE03995847`, expiration `2026-10-13 21:06:20`.
- Binding HTTPS cree: `104.129.128.136:443:chat.dravii.com`.
- Renouvellement win-acme cree: `[IIS] BeeKingdom.ChatApi, chat.dravii.com`, due `2026/9/9`.
- WebSocket Protocol installe: `Web-WebSockets=Installed`.
- Firewall:
  - `BeeKingdom Chat HTTP 80=True`
  - `BeeKingdom Chat HTTPS 443=True`
  - regles IIS existantes HTTP/HTTPS aussi actives.

Configuration application:

- `ASPNETCORE_ENVIRONMENT=Production`
- `Chat__Enabled=false`
- `Chat__RealtimeEnabled=false`
- `Persistence__Provider=InMemory`
- Aucun secret ajoute.

Checks origine HTTP avec host header `chat.dravii.com`:

- `http://104.129.128.136/health` -> 200.
- `http://104.129.128.136/runtime/chat-readiness` -> 200, `enabled=false`, `realtimeEnabled=false`.
- `http://104.129.128.136/chat/v1/capabilities` -> 200, `server=false`, `realtime=false`.
- `POST http://104.129.128.136/chat/v1/realtime/negotiate?negotiateVersion=1` -> 200, transports incluent WebSockets.

Checks publics:

- `http://chat.dravii.com/health` -> 200 via Cloudflare.
- `https://chat.dravii.com/health` -> 200.
- `https://chat.dravii.com/runtime/chat-readiness` -> 200, `enabled=false`, `realtimeEnabled=false`.
- `https://chat.dravii.com/chat/v1/capabilities` -> 200, `server=false`, `realtime=false`.
- `POST https://chat.dravii.com/chat/v1/realtime/negotiate?negotiateVersion=1` -> 200.

SQL:

- Instance: `.\SQLEXPRESS01`.
- Database: `BeeKingdom`.
- Connection runtime IIS: Integrated Security, `Encrypt=True`, `TrustServerCertificate=True`.
- App pool SQL principal: `IIS APPPOOL\BeeKingdom.ChatApi`.
- Runtime DB roles: `db_datareader`, `db_datawriter`.
- Migration runner executed from `C:\inetpub\BeeKingdom.Tools`.
- Applied scripts:
  - `010_schema_version.sql`
  - `011_schema_version_uniqueness.sql`
  - `020_accounts.sql`
  - `030_authentication_sessions.sql`
  - `040_colonies.sql`
  - `050_colony_snapshots.sql`
  - `060_chat_messaging.sql`

Non-interference IIS:

- Les sites existants restent separes.
- Aucun binding existant d'autres sites n'a ete modifie.
- Le seul binding ajoute est host-specific: `104.129.128.136:80:chat.dravii.com`.
- Le binding HTTPS ajoute est host-specific: `104.129.128.136:443:chat.dravii.com`.
- Aucun binding HTTPS wildcard ou sans host n'a ete ajoute.

## Package local

Package serveur prepare localement:

```powershell
dotnet publish Server/src/BeeKingdom.Server/BeeKingdom.Server.csproj -c Release -r win-x64 --self-contained false -o Server/artifacts/chat-prod-prep/BeeKingdom.Server
```

Chemin:

- `Server/artifacts/chat-prod-prep/BeeKingdom.Server`

Package outils migrations prepare localement:

```powershell
dotnet publish Server/src/BeeKingdom.Tools/BeeKingdom.Tools.csproj -c Release -r win-x64 --self-contained false -o Server/artifacts/chat-prod-prep/BeeKingdom.Tools
```

Chemin:

- `Server/artifacts/chat-prod-prep/BeeKingdom.Tools`

Ne pas copier sur `104.129.128.136` avant validation des entrees externes restantes.

## Configuration production attendue

Le fichier source `Server/src/BeeKingdom.Server/appsettings.Production.json` garde:

```json
{
  "Persistence": {
    "Provider": "InMemory"
  },
  "Chat": {
    "Enabled": false,
    "RealtimeEnabled": false
  }
}
```

Pour production reelle, fournir les overrides hors depot via IIS environment variables, fichier securise non versionne ou secret store:

```powershell
setx ASPNETCORE_ENVIRONMENT Production /M
setx Persistence__Provider SqlServer /M
setx ConnectionStrings__BeeKingdomRuntime "<SECRET_RUNTIME_SQL_CONNECTION>" /M
setx ConnectionStrings__BeeKingdomMigrations "<SECRET_MIGRATION_SQL_CONNECTION>" /M
setx SqlServer__RuntimeConnectionStringName BeeKingdomRuntime /M
setx SqlServer__MigrationConnectionStringName BeeKingdomMigrations /M
setx Ops__AdminKeySha256 "<SECRET_ADMIN_KEY_SHA256>" /M
setx Ops__MigrationApplyKeySha256 "<SECRET_MIGRATION_KEY_SHA256>" /M
setx ASPNETCORE_URLS "http://127.0.0.1:5000" /M
setx Chat__Enabled false /M
setx Chat__RealtimeEnabled false /M
```

Remplacer les placeholders sur le serveur uniquement. Ne jamais stocker ces valeurs dans le depot.

Forwarded headers:

- Si IIS agit seulement via ASP.NET Core Module standard, valider le comportement natif avant ajout de configuration.
- Si Cloudflare + reverse proxy IIS/Kestrel est utilise, ajouter une configuration ASP.NET Core explicite pour `X-Forwarded-For` et `X-Forwarded-Proto`, avec liste de proxies connus/restreints.
- Ne pas activer une confiance large sur tous les proxies en production sans restriction reseau.

## Etapes Windows Server 2025 / IIS

Preparation serveur:

```powershell
Install-WindowsFeature Web-Server,Web-Asp-Net45,Web-WebSockets
```

Installer le .NET Hosting Bundle compatible .NET 8 si absent, puis redemarrer IIS:

```powershell
iisreset
```

Creer le dossier applicatif:

```powershell
New-Item -ItemType Directory -Path "C:\inetpub\BeeKingdom.Server" -Force
```

Creer ou valider le compte de service/App Pool:

```powershell
New-WebAppPool -Name "BeeKingdom.Server"
Set-ItemProperty IIS:\AppPools\BeeKingdom.Server -Name managedRuntimeVersion -Value ""
Set-ItemProperty IIS:\AppPools\BeeKingdom.Server -Name processModel.identityType -Value ApplicationPoolIdentity
```

Copier le package uniquement apres GO:

```powershell
Copy-Item -Path "<LOCAL_PACKAGE>\*" -Destination "C:\inetpub\BeeKingdom.Server" -Recurse -Force
```

Creer le site IIS pour `chat.dravii.com`:

```powershell
New-Website -Name "BeeKingdom.Server" -PhysicalPath "C:\inetpub\BeeKingdom.Server" -Port 80 -HostHeader "chat.dravii.com" -ApplicationPool "BeeKingdom.Server"
```

Ajouter le binding HTTPS apres import/selection du certificat:

```powershell
New-WebBinding -Name "BeeKingdom.Server" -Protocol https -Port 443 -HostHeader "chat.dravii.com"
```

Lier le certificat TLS `chat.dravii.com` par thumbprint via IIS/PowerShell selon la methode operationnelle validee.

Firewall minimal:

```powershell
New-NetFirewallRule -DisplayName "BeeKingdom HTTP" -Direction Inbound -Protocol TCP -LocalPort 80 -Action Allow
New-NetFirewallRule -DisplayName "BeeKingdom HTTPS" -Direction Inbound -Protocol TCP -LocalPort 443 -Action Allow
```

Ouvrir le port SQL uniquement entre le serveur applicatif et le serveur SQL si SQL est separe. Ne pas ouvrir SQL publiquement.

## Etapes SQL

Avant migration:

1. Confirmer backup complet et restauration testee.
2. Confirmer identites SQL separees runtime/migration.
3. Confirmer droits:
   - runtime: lecture/ecriture applicative sur tables Bee Kingdom necessaires.
   - migration: droits schema/migration uniquement pendant fenetre controlee.
4. Confirmer `Persistence__Provider=SqlServer` seulement sur cible.

Verifier les migrations pendantes via endpoint ops securise:

```powershell
Invoke-RestMethod -Method GET -Uri "https://chat.dravii.com/ops/migrations/pending" -Headers @{ "X-BeeKingdom-Admin-Key" = "<SECRET_ADMIN_KEY>" }
```

Appliquer les migrations via endpoint ops securise:

```powershell
Invoke-RestMethod -Method POST -Uri "https://chat.dravii.com/ops/migrations/apply" -Headers @{ "X-BeeKingdom-Migration-Key" = "<SECRET_MIGRATION_KEY>" }
```

Alternative console hors trafic web, sur serveur cible avec le package `BeeKingdom.Tools` et configuration secrete locale:

```powershell
dotnet BeeKingdom.Tools.dll diagnostics
dotnet BeeKingdom.Tools.dll migrate
```

Verifier que `060_chat_messaging.sql` est applique et que les tables chat existent.

## Basculement chat controle

Deploiement initial:

- Garder `Chat__Enabled=false`.
- Garder `Chat__RealtimeEnabled=false`.
- Verifier `https://chat.dravii.com/health`.
- Verifier `https://chat.dravii.com/runtime/chat-readiness`.
- Verifier `https://chat.dravii.com/chat/v1/capabilities`.
- Verifier que `https://chat.dravii.com/chat/v1/realtime` est route par IIS/SignalR mais refuse ou reste inactif tant que `Chat__RealtimeEnabled=false`.
- Verifier logs IIS/app sans erreurs.

Activation REST chat:

```powershell
setx Chat__Enabled true /M
iisreset
```

Verifier:

- `https://chat.dravii.com/runtime/chat-readiness` indique `enabled=true`.
- Mutations REST chat fonctionnent avec compte test autorise.
- SQL contient conversations/messages test.
- Aucune erreur auth ou migration.

Activation temps reel plus tard:

```powershell
setx Chat__RealtimeEnabled true /M
iisreset
```

Precondition avant realtime large:

- `ChatRealtimeHub.JoinConversation` doit verifier l'appartenance conversation cote serveur avant ajout groupe, ou l'exposition doit rester limitee a staging.

## Validation post-deploiement

Verifier:

- HTTPS 200 sur `https://chat.dravii.com/health`.
- HTTPS 200 sur `https://chat.dravii.com/runtime/chat-readiness`.
- HTTPS 200 sur `https://chat.dravii.com/chat/v1/capabilities`.
- Route SignalR presente sur `https://chat.dravii.com/chat/v1/realtime`.
- SignalR handshake valide avec client de test staging/local quand `Chat__RealtimeEnabled=true`.
- WebSocket upgrade fonctionne via Cloudflare proxy orange ou en DNS only diagnostic.
- Auth login existant fonctionne.
- `POST /chat/v1/conversations` fonctionne avec chat active.
- `POST /chat/v1/conversations/{conversationId}/messages` persiste en SQL.
- idempotence `clientRequestId` renvoie deduplication.
- inbox/unread mis a jour.
- moderation report cree.
- logs sans exception.
- CPU/memoire/IIS worker stable.

## Rollback

Rollback fonctionnel sans perte schema:

1. Desactiver chat:

```powershell
setx Chat__RealtimeEnabled false /M
setx Chat__Enabled false /M
iisreset
```

2. Retirer ou neutraliser l'exposition IIS si necessaire:

```powershell
Remove-WebBinding -Name "BeeKingdom.Server" -Protocol https -Port 443 -HostHeader "chat.dravii.com"
Remove-WebBinding -Name "BeeKingdom.Server" -Protocol http -Port 80 -HostHeader "chat.dravii.com"
```

Alternative diagnostic reseau: repasser temporairement `chat.dravii.com` en Cloudflare `DNS only` ou desactiver le proxy orange pour isoler origine/TLS/WebSocket.

3. Revenir au package precedent:

```powershell
Stop-Website -Name "BeeKingdom.Server"
Rename-Item "C:\inetpub\BeeKingdom.Server" "C:\inetpub\BeeKingdom.Server.failed.$(Get-Date -Format yyyyMMddHHmmss)"
Copy-Item -Path "<PREVIOUS_PACKAGE>\*" -Destination "C:\inetpub\BeeKingdom.Server" -Recurse -Force
Start-Website -Name "BeeKingdom.Server"
```

4. Garder les tables chat en place sauf decision DBA explicite.
5. Restaurer SQL depuis backup uniquement si corruption ou migration erronee est confirmee.
6. Capturer logs application/IIS avant suppression du package echoue.

Rollback data:

- Preferer rollback fonctionnel par flags.
- Ne supprimer aucune table chat sans sauvegarde et validation.
- Si restauration SQL est requise, restaurer vers base separee d'abord pour comparaison.

## Etat final de ce runbook

Gates locales:

- Build serveur: PASS.
- Suite tests serveur: PASS.
- Packages locaux: PASS.
- Domaine temporaire: `chat.dravii.com`.
- DNS temporaire: `A` vers `104.129.128.136`.
- SQL schema/migration: PRET.
- Production disabled by default: PASS.
- Secrets dans depot: AUCUN AJOUT.

Blocage restant:

`READY_FOR_PROD_DEPLOY=BLOCKED_EXTERNAL_INPUT`
`TEMP_CHAT_DOMAIN=chat.dravii.com`
`DNS_TEMP_SUBDOMAIN_CONFIGURED=YES`
`CLOUDFLARE_PROXY_DETECTED=YES`
`SERVER_ADMIN_ACCESS=YES`
`IIS_CONFIGURED=YES`
`IIS_BINDING_PLAN_READY=YES`
`IIS_WEBSOCKET_READY=YES`
`ORIGIN_TLS_READY=YES`
`FIREWALL_READY=YES`
`CHAT_APP_BOUND=YES`
`SQL_CONFIGURED=YES`
`SQL_MIGRATIONS_APPLIED=YES`
`PROD_CHAT_DEPLOY_READY=NO`

Liste exacte restante a fournir: certificat TLS pour `chat.dravii.com`, binding IIS valide, confirmation firewall 80/443, strategie Cloudflare proxy orange vs DNS only, configuration reverse proxy/Kestrel/IIS, appsettings staging/prod hors depot, acces SQL production, compte de service/IIS, acces admin serveur, backup evidence, fenetre de maintenance, valeurs secretes ops hors depot.
