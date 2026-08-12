# Bee Kingdom - ChatMessaging Server Implementation Plan

**Date :** 2026-07-16  
**Statut :** plan d'implementation serveur, non deploye  
**Perimetre :** chat et messagerie persistante Bee Kingdom cote serveur  
**Cible connue :** Windows Server 2025, IP `104.129.128.136`, Canada  
**Hors perimetre initial :** carte 50x50, PNG, Wave5, BearDen, APK, scene Unity, activation live sans validation explicite

## 1. Etat actuel exact

### 1.1 Documents locaux lus

Les documents locaux consultes en priorite sont :

- `Docs/WorldMapCommunication/ChatMessaging_LocalArchitecture_Spec.md`
- `Docs/WorldMapCommunication/ChatMessaging_LocalArchitecture_Spec.ValidationReceipt.md`
- `Docs/WorldMapCommunication/ChatMessaging_LocalDataLayer_Report.md`

Ils etablissent deja le contrat local cible :

- quatre canaux : `alliance`, `server`, `private`, `leaders`;
- idempotence par `clientRequestId`;
- non-lus par curseur monotone par utilisateur et conversation;
- messages persistants avec `messageId`, `conversationId`, `sequence`, `state`, `moderation`, `schemaVersion`;
- support de `contentParts`, emojis, mentions, notifications, outbox hors ligne et reconnexion;
- moderation/anti-spam simules localement;
- `server=false`, `official_gain=false`, `networkTransport=none` pour le provider local;
- aucun backend live, DNS, TLS, SQL ou donnees reelles active par ces livrables.

Le rapport local indique aussi qu'un prototype Unity/local data layer existe deja et que les tests locaux ciblent les quatre canaux, l'annonce dirigeants, le prive hors ligne, l'idempotence et les non-lus. Ce plan ne modifie pas ce code Unity.

### 1.2 Etat reel du dossier `Server`

Le dossier `Server` contient une base serveur ASP.NET Core/.NET 8 existante :

- solution : `Server/BeeKingdom.Server.slnx`;
- application principale : `Server/src/BeeKingdom.Server/BeeKingdom.Server.csproj`;
- modules existants : `BeeKingdom.Accounts`, `BeeKingdom.Authentication`, `BeeKingdom.Colony`, `BeeKingdom.Database`, `BeeKingdom.Gateway`, `BeeKingdom.Infrastructure`, `BeeKingdom.Persistence`, `BeeKingdom.Protocol`, `BeeKingdom.Server`, `BeeKingdom.Shared`, `BeeKingdom.Simulation`, `BeeKingdom.Tools`;
- cible configuree : Windows Server 2025, IIS, .NET 8;
- scripts SQL actuels : schema version, accounts, authentication sessions, colonies, colony snapshots;
- runbooks deploy existants : publication, installation staging, rollback, IIS/HTTPS/SQL, SQL readiness;
- artefacts publies deja presents sous `Server/artifacts`, notamment un package `SERVER-056`.

Endpoints actuellement exposes par `Server/src/BeeKingdom.Server/Program.cs` :

- public readiness : `/health`, `/protocol/ping`, `/runtime/handshake`, `/runtime/server-first-readiness`, `/runtime/account-session-readiness`, `/runtime/world-map-readiness`, `/runtime/world-registry-readiness`, `/runtime/world-identity-readiness`;
- operations : `/ops/migrations/pending`, `/ops/migrations/apply`, `/ops/migrations/rollback-plan`, `/ops/monitoring`, `/ops/readiness`, `/ops/sql-production-dry-run`;
- comptes/auth : `/auth/login`, `/auth/refresh`, `/auth/validate`, `/auth/logout`, `/accounts`, `/accounts/{accountId}`, profile/preferences;
- gateway HTTP minimal : `/gateway/connections`, `/gateway/connections/{connectionId}/authenticate`, disconnect, statistics;
- colonies/simulation : endpoints de preparation existants.

Ce qui n'existe pas encore cote serveur :

- aucun projet/module `BeeKingdom.Chat`;
- aucun endpoint `/chat` ou `/messages`;
- aucun WebSocket, SignalR hub ou `UseWebSockets`;
- aucune table SQL de chat;
- aucune persistance officielle des messages;
- aucun endpoint d'historique, non-lus, mentions, moderation chat ou reception hors ligne;
- aucun contrat client/serveur active pour Unity.

### 1.3 Etat de production

La configuration production reste volontairement non live :

- `Persistence:Provider` vaut encore `InMemory` dans `appsettings.Production.json`;
- les secrets SQL et ops ne sont pas dans le depot;
- les readiness endpoints declarent `ProductionRouteProven=false`, comptes/sessions non live, synchronisation temps reel non activee;
- les runbooks indiquent explicitement que live chat, alliances persistantes, PvP, economie, matchmaking et rankings ne sont pas production ready.

Conclusion : le backend Bee Kingdom possede une fondation ASP.NET Core/IIS/SQL preparee, mais le chat/messagerie live n'est pas implemente.

## 2. Choix technique recommande pour Windows Server 2025

### 2.1 Recommandation principale

Ajouter un module serveur dedie dans l'architecture existante :

- `Server/src/BeeKingdom.Chat`
- `Server/src/BeeKingdom.Chat.SqlServer` ou une zone SQL dans `BeeKingdom.Chat`
- schemas SQL dans `Server/src/BeeKingdom.Database/Scripts/060_chat_messaging.sql`
- tests dans `Server/tests/BeeKingdom.Tests/ChatMessaging*Tests.cs`
- endpoints REST et hub temps reel branches dans `BeeKingdom.Server`.

Stack recommandee :

- ASP.NET Core .NET 8, conserve pour compatibilite avec le serveur existant;
- IIS + ASP.NET Core Hosting Bundle sur Windows Server 2025, conserve comme chemin de deploiement;
- SQL Server comme source de verite persistante;
- SignalR pour temps reel si le client Unity peut accepter le client .NET/Unity SignalR;
- WebSocket brut en fallback si le client Unity ne peut pas embarquer SignalR proprement;
- JSON UTF-8 strict, `System.Text.Json`, pas de HTML dans `body`;
- stockage emojis/mentions en JSON valide ou tables normalisees selon besoin de requetage.

Decision pratique : commencer par SignalR cote serveur avec une couche `IChatRealtimeDispatcher`, puis garder le contrat d'evenement independant du transport. Si Unity pose un probleme de dependance, remplacer seulement l'adaptateur transport par WebSocket brut.

### 2.2 Pourquoi ne pas creer un serveur separe maintenant

Le besoin parle d'un chantier serveur separe, pas necessairement d'un binaire totalement distinct. L'existant a deja :

- auth/session;
- comptes et `PlayerId`;
- readiness ops;
- SQL migration runner;
- IIS/HTTPS runbooks;
- gateway rate limiter;
- tests HTTP.

Il est donc plus sur d'ajouter un domaine `Chat` isole dans la solution serveur actuelle. Un second service physique pourra venir plus tard si la charge chat le justifie.

## 3. Schema de donnees propose

Toutes les dates sont UTC. Les identifiants exposes au client restent opaques. Les colonnes `GameServerId` et `WorldId` doivent etre presentes pour rester compatibles avec la gouvernance multi-monde deja documentee.

### 3.1 Tables principales

`ChatConversations`

| Colonne | Type SQL Server | Notes |
|---|---|---|
| `ConversationId` | uniqueidentifier | PK |
| `GameServerId` | uniqueidentifier | scope serveur |
| `WorldId` | uniqueidentifier | scope monde |
| `ChannelType` | nvarchar(32) | `alliance`, `server`, `private`, `leaders` |
| `AudienceKey` | nvarchar(128) | ex. `server:{id}`, `alliance:{id}`, `leaders:{id}`, `private:{id}` |
| `Title` | nvarchar(160) null | projection, pas source d'identite |
| `CreatedByPlayerId` | uniqueidentifier null | null pour conversation systeme |
| `CreatedAtUtc` | datetimeoffset(7) | creation |
| `LastMessageId` | uniqueidentifier null | cache |
| `LastActivityAtUtc` | datetimeoffset(7) null | tri inbox |
| `RetentionPolicy` | nvarchar(64) | `alliance_standard`, etc. |
| `SchemaVersion` | int | depart `1` |

Contraintes :

- unique `(GameServerId, WorldId, ChannelType, AudienceKey)` pour les singletons alliance/server/leaders;
- index `(GameServerId, WorldId, LastActivityAtUtc desc)`.

`ChatConversationParticipants`

| Colonne | Type | Notes |
|---|---|---|
| `ConversationId` | uniqueidentifier | FK |
| `PlayerId` | uniqueidentifier | participant |
| `Role` | nvarchar(32) | `member`, `officer`, `leader`, `moderator`, `system` |
| `JoinedAtUtc` | datetimeoffset(7) | debut acces |
| `RemovedAtUtc` | datetimeoffset(7) null | fin acces futur |
| `CanRead` | bit | projection de permission |
| `CanWrite` | bit | projection de permission |

PK `(ConversationId, PlayerId)`.

`ChatMessages`

| Colonne | Type | Notes |
|---|---|---|
| `MessageId` | uniqueidentifier | PK |
| `ConversationId` | uniqueidentifier | FK |
| `GameServerId` | uniqueidentifier | scope |
| `WorldId` | uniqueidentifier | scope |
| `ChannelType` | nvarchar(32) | denormalise pour requetes |
| `SenderPlayerId` | uniqueidentifier | emetteur |
| `SenderDisplayNameSnapshot` | nvarchar(80) | affichage historique |
| `Body` | nvarchar(1000) | texte canonique sans HTML |
| `ContentPartsJson` | nvarchar(max) | text/emoji/mention ordonnes |
| `MentionsJson` | nvarchar(max) | mentions ciblees |
| `EmojiJson` | nvarchar(max) | emojis detectes |
| `ReplyToMessageId` | uniqueidentifier null | reponse |
| `ClientCreatedAtUtc` | datetimeoffset(7) | horloge client |
| `AcceptedAtUtc` | datetimeoffset(7) | horloge serveur |
| `Sequence` | bigint | monotone par conversation |
| `ClientRequestId` | nvarchar(128) | idempotence |
| `State` | nvarchar(32) | `accepted`, `hidden`, `deleted`, `expired`, etc. |
| `ModerationStatus` | nvarchar(32) | `clear`, `pending`, `blocked`, `masked`, `review` |
| `ModerationReasonCode` | nvarchar(64) null | code stable |
| `EditedAtUtc` | datetimeoffset(7) null | edition |
| `DeletedAtUtc` | datetimeoffset(7) null | tombstone |
| `SchemaVersion` | int | depart `1` |

Contraintes et index :

- unique `(ConversationId, Sequence)`;
- unique `(SenderPlayerId, ConversationId, ClientRequestId)`;
- index `(ConversationId, Sequence desc)`;
- index `(GameServerId, WorldId, ChannelType, AcceptedAtUtc desc)`;
- full text ou index externe plus tard seulement si recherche historique demandee.

`ChatInbox`

| Colonne | Type | Notes |
|---|---|---|
| `PlayerId` | uniqueidentifier | PK part |
| `ConversationId` | uniqueidentifier | PK part |
| `LastMessageId` | uniqueidentifier null | cache |
| `LastActivityAtUtc` | datetimeoffset(7) null | tri |
| `ReadCursorSequence` | bigint | curseur monotone |
| `UnreadCount` | int | projection recalculable |
| `MentionCount` | int | projection recalculable |
| `IsMuted` | bit | supprime alertes, pas badge |
| `IsArchived` | bit | projection |
| `UpdatedAtUtc` | datetimeoffset(7) | cache |

`ChatOutboxReceipts`

| Colonne | Type | Notes |
|---|---|---|
| `PlayerId` | uniqueidentifier | emetteur |
| `ConversationId` | uniqueidentifier | cible |
| `ClientRequestId` | nvarchar(128) | idempotence |
| `PayloadHash` | varbinary(32) | anti-replay divergent |
| `MessageId` | uniqueidentifier null | resultat si accepte |
| `AcceptedAtUtc` | datetimeoffset(7) null | resultat |
| `LastErrorCode` | nvarchar(64) null | erreur definitive/transitoire |

PK `(PlayerId, ConversationId, ClientRequestId)`.

### 3.2 Tables notifications, moderation et audit

`ChatNotifications`

- `NotificationId` uniqueidentifier PK;
- `PlayerId`;
- `Kind` : `new_message`, `mention`, `delivery`, `moderation`, `provider`;
- `ConversationId`, `MessageId` nullable;
- `Priority`;
- `IsRead`;
- `CreatedAtUtc`;
- `DeepLink`;
- `PayloadJson`.

`ChatModerationReports`

- `ReportId`;
- `MessageId`;
- `ReporterPlayerId`;
- `Category`;
- `DetailsHash` nullable;
- `CreatedAtUtc`;
- `Status` : `open`, `reviewed`, `closed`;
- `ResolutionCode` nullable.

`ChatModerationActions`

- `ActionId`;
- `MessageId`;
- `ActorPlayerId` nullable;
- `ActionType` : `mask`, `delete`, `restore`, `suspend_sender`, `rate_limit`;
- `ReasonCode`;
- `CreatedAtUtc`;
- `PolicyVersion`;
- `AuditJson`.

`ChatConversationSequences`

- `ConversationId` PK;
- `NextSequence` bigint.

Cette table permet d'allouer les sequences sous transaction SQL avec verrou court. Alternative : sequence SQL par partition, mais une table explicite est plus simple a migrer et tester.

## 4. API REST proposee

Base prefix : `/chat/v1`.

Toutes les routes mutantes exigent une session valide. Les reponses d'erreur doivent utiliser des codes stables : `unauthorized`, `forbidden`, `not_found`, `validation_failed`, `rate_limited`, `duplicate_suppressed`, `moderation_blocked`, `temporarily_unavailable`.

### 4.1 Capacites et conversations

`GET /chat/v1/capabilities`

Retour :

- canaux supportes;
- limites `bodyMaxChars`, `maxRecipients`, quotas;
- support `emoji`, `mentions`, `offlineDelivery`, `readCursors`, `moderationReports`;
- version protocole chat;
- `server=true`, `official_gain=false`.

`GET /chat/v1/conversations?channelType=&cursor=&limit=`

Retour pagine des conversations visibles par le joueur authentifie.

`POST /chat/v1/conversations`

Creation de conversation privee ou resolution d'une conversation singleton.

Body minimal :

```json
{
  "channelType": "private",
  "participantIds": ["player-guid-a", "player-guid-b"],
  "clientRequestId": "create_private_001"
}
```

`GET /chat/v1/conversations/{conversationId}`

Retourne metadata, permissions courantes, participants autorises et projection inbox.

### 4.2 Messages

`GET /chat/v1/conversations/{conversationId}/messages?afterSequence=&beforeSequence=&limit=50`

Pagination par sequence. Le serveur borne `limit` et renvoie `nextCursor`.

`POST /chat/v1/conversations/{conversationId}/messages`

Body :

```json
{
  "clientRequestId": "send_player_queen_000012",
  "body": "Rendez-vous a la porte nord !",
  "contentParts": [
    { "kind": "text", "text": "Rendez-vous a la porte nord !" }
  ],
  "mentions": [],
  "emoji": [],
  "replyToMessageId": null,
  "clientCreatedAt": "2026-07-16T14:00:00Z"
}
```

Reponse :

```json
{
  "message": {},
  "deduplicated": false,
  "serverSequence": 12
}
```

Regles :

- idempotence stricte sur `(playerId, conversationId, clientRequestId)`;
- si le meme `clientRequestId` revient avec un hash different, retourner `409 idempotency_conflict`;
- le serveur attribue `messageId`, `acceptedAt`, `sequence`, `state`.

`POST /chat/v1/conversations/{conversationId}/read`

Body :

```json
{ "sequence": 12 }
```

Le curseur est monotone. Une sequence plus basse ne modifie rien.

`POST /chat/v1/messages/{messageId}/report`

Body :

```json
{ "category": "spam", "details": "optional player text" }
```

`POST /chat/v1/messages/{messageId}/delete`

Suppression/tombstone selon permissions.

### 4.3 Leaders et annonces

`POST /chat/v1/alliances/{allianceId}/announcements`

Route reservee `leader`/`officer` selon politique. Cree un message dans `alliance:{allianceId}` avec `metadata.kind=leader_announcement` et notification a tous les membres non retires.

`GET /chat/v1/alliances/{allianceId}/leaders/conversation`

Retourne ou cree la conversation singleton `leaders:{allianceId}` si le joueur a le droit.

## 5. WebSocket / SignalR propose

### 5.1 Hub

Endpoint recommande :

- SignalR : `/chat/v1/realtime`
- fallback WebSocket brut : `/chat/v1/ws`

Authentification :

- access token via header `Authorization: Bearer <accessToken>` pour REST;
- pour SignalR Unity, utiliser access token provider ou query `access_token` uniquement sur TLS, avec logs qui masquent la query;
- le serveur valide la session via `AuthenticationManager.ValidateToken`.

### 5.2 Enveloppe evenement

Conserver l'enveloppe locale :

```json
{
  "eventId": "evt_...",
  "eventType": "message.created",
  "occurredAt": "2026-07-16T14:01:00Z",
  "conversationId": "alliance:...",
  "sequence": 12,
  "actorId": "player_...",
  "payload": {},
  "provider": "server",
  "schemaVersion": 1
}
```

Evenements minimum :

- `conversation.created`
- `message.created`
- `message.delivered`
- `message.updated`
- `message.moderated`
- `message.deleted`
- `inbox.updated`
- `presence.changed`
- `sync.completed`
- `provider.status.changed`

### 5.3 Groupes temps reel

Groupes serveur :

- `conversation:{conversationId}`;
- `player:{playerId}`;
- `alliance:{allianceId}`;
- `leaders:{allianceId}`;
- `server:{gameServerId}:{worldId}`.

Le hub ne doit jamais faire confiance a un group join demande par le client. Le serveur calcule les groupes a partir des permissions et de la session.

## 6. Securite et authentification

### 6.1 Authentification

Court terme compatible avec l'existant :

- utiliser les access tokens existants de `AuthenticationManager`;
- valider `PlayerId`, `AccountId`, `SessionId`;
- refuser toute route chat si token invalide, expire ou revoque.

Point important : les tokens actuels sont opaques et geres cote serveur, pas de JWT standard expose. Le plan doit donc brancher le chat sur `AuthenticationManager` plutot que supposer un JWT externe.

Moyen terme :

- rendre le store de tokens/session SQL-backed avant production chat;
- ajouter une policy ASP.NET Core `BeeAuthenticatedPlayer`;
- centraliser le parsing `Authorization` pour REST et temps reel.

### 6.2 Autorisation

Le serveur fait autorite sur :

- appartenance alliance;
- roles `member`, `officer`, `leader`, `moderator`;
- appartenance au serveur/monde;
- participants d'une conversation privee;
- blocages utilisateur;
- suspensions et moderation.

Tant que le module alliance live n'existe pas, utiliser un `IChatAudienceResolver` avec implementation preparation/in-memory test, puis implementation SQL/alliance officielle plus tard.

### 6.3 Anti-abus

Reprendre les limites locales comme base, mais les appliquer cote serveur :

- longueur body : 500 caracteres Unicode normalises au depart;
- limite par joueur/conversation : 10 messages / 10 secondes, 50 / minute;
- limite globale joueur : compatible avec `GatewayOptions`;
- creation de conversations privees : 10 / heure;
- destinataires groupe prive : 20 max;
- duplicate exact : fenetre 30 secondes;
- contenu trop proche : limiter apres 3 messages en 20 secondes;
- payload max : borne stricte en bytes.

Ajouter :

- rate limiting par IP;
- journal d'audit pour moderation;
- masquage secrets dans logs;
- blocage HTML/script;
- normalisation Unicode avant moderation, stockage original seulement si accepte;
- `reasonCode` stable pour tous les refus.

### 6.4 Donnees et confidentialite

- TLS obligatoire en production;
- pas de messages dans logs applicatifs sauf hash/identifiants;
- retention executee par job serveur;
- tombstones conserves selon politique;
- rapports moderation separes du contenu visible;
- exports admin non prevus dans la premiere version.

## 7. Plan de deploiement sur `104.129.128.136`

Aucun deploiement live ne doit etre fait sans validation explicite.

### 7.1 Preconditions

- nom DNS decide, par exemple `api.beekingdom.example` ou `chat.beekingdom.example`;
- record DNS `A` vers `104.129.128.136`;
- certificat TLS valide pour le nom DNS;
- firewall entrant :
  - 443 ouvert public;
  - 80 ouvert uniquement si redirection/ACME necessaire;
  - SQL non expose public;
  - RDP restreint par IP/VPN si utilise;
- IIS installe avec ASP.NET Core Hosting Bundle .NET 8;
- SQL Server disponible localement ou sur hote prive;
- comptes SQL separes : runtime et migrations;
- secrets hors depot : connection strings, ops keys, future chat moderation keys;
- backup SQL verifie;
- maintenance window et rollback approuves.

### 7.2 Configuration environnement

Variables minimales a definir cote IIS/app pool :

```powershell
ASPNETCORE_ENVIRONMENT=Production
Persistence__Provider=SqlServer
SqlServer__RuntimeConnectionStringName=BeeKingdomRuntime
SqlServer__MigrationConnectionStringName=BeeKingdomMigrations
ConnectionStrings__BeeKingdomRuntime=<secret>
ConnectionStrings__BeeKingdomMigrations=<secret>
Ops__RequireAdminKey=true
Ops__AdminKeySha256=<hash>
Ops__RequireMigrationApplyKey=true
Ops__MigrationApplyKeySha256=<hash-distinct>
ServerIdentity__GameServerId=<stable-guid>
ServerIdentity__DefaultWorldId=<stable-guid>
ServerIdentity__ShardName=<stable-name>
Chat__Enabled=false
Chat__RealtimeEnabled=false
```

`Chat__Enabled=false` doit rester la valeur par defaut jusqu'a validation fonctionnelle, tests de charge, DNS/TLS et politique moderation.

### 7.3 Etapes de deploiement non live

1. Publier un package staging local.
2. Installer dans un nouveau repertoire versionne sur le serveur.
3. Configurer IIS site/app pool avec HTTPS.
4. Verifier `/health`, `/protocol/ping`, `/runtime/handshake`.
5. Verifier `/ops/readiness` avec admin key.
6. Appliquer migrations SQL uniquement apres backup et validation.
7. Verifier readiness SQL et absence de secrets dans les reponses.
8. Activer `Chat__Enabled=true` seulement en environnement staging ferme.
9. Executer tests REST chat et tests temps reel.
10. Laisser `Chat__RealtimeEnabled=false` tant que le client Unity n'a pas valide le transport.
11. Ouvrir beta restreinte uniquement apres validation explicite.

## 8. Etapes d'implementation ordonnees

### Phase 0 - Contrat et garde-fous

1. Ajouter ce plan et le faire valider.
2. Creer `ChatMessaging_ServerContract.md` avec schemas JSON definitifs REST/evenements.
3. Ajouter un readiness endpoint non live `/runtime/chat-readiness`.
4. Ajouter config `ChatOptions` avec `Enabled=false`, `RealtimeEnabled=false`.
5. Ajouter tests garantissant qu'aucun endpoint chat mutant n'est live si `Chat__Enabled=false`.

### Phase 1 - Domaine serveur pur

1. Creer `BeeKingdom.Chat`.
2. Ajouter modeles : conversation, message, inbox, notification, moderation report.
3. Ajouter interfaces : `IChatRepository`, `IChatService`, `IChatAudienceResolver`, `IChatModerationService`, `IChatRateLimiter`, `IChatRealtimeDispatcher`.
4. Ajouter implementation in-memory de test.
5. Porter les invariants du provider local vers tests serveur.

### Phase 2 - SQL

1. Ajouter migration `060_chat_messaging.sql`.
2. Ajouter rollback SQL correspondant.
3. Ajouter repository SQL transactionnel.
4. Implementer allocation de sequence par conversation.
5. Tester idempotence SQL, pagination, curseur non-lu, retention/tombstone.

### Phase 3 - REST

1. Brancher `/chat/v1/capabilities`.
2. Brancher list/get/create conversations.
3. Brancher get/send messages.
4. Brancher mark read.
5. Brancher report/delete.
6. Brancher leaders/announcements.
7. Ajouter tests HTTP avec session authentifiee.

### Phase 4 - Temps reel

1. Ajouter SignalR ou WebSocket adapter derriere `IChatRealtimeDispatcher`.
2. Ajouter groupes calcules serveur.
3. Publier `message.created`, `inbox.updated`, `message.moderated`.
4. Ajouter reconnexion : `syncConversation(conversationId, afterSequence)`.
5. Ajouter tests d'integration temps reel.

### Phase 5 - Moderation, anti-spam, retention

1. Normalisation Unicode.
2. Liste de termes/version de politique.
3. Rate limits par player/session/IP/conversation.
4. Duplicate suppression.
5. Reports moderation.
6. Job retention/expiration.
7. Audit des actions moderator/admin.

### Phase 6 - Client contract sans modification Unity initiale

1. Documenter l'adaptateur futur `ServerChatProvider`.
2. Fournir fixtures de reponses REST et evenements.
3. Comparer le contrat serveur aux types du provider local.
4. Identifier uniquement les deltas necessaires, sans modifier Unity.

### Phase 7 - Staging ferme

1. Activer SQL en staging.
2. Activer `Chat__Enabled=true` derriere auth.
3. Garder `Chat__RealtimeEnabled=false` puis l'activer seulement pour test ferme.
4. Tests de charge : historique, burst messages, reconnexion, offline delivery.
5. Validation securite : TLS, firewall, logs, absence secrets, permissions.

## 9. Risques et prerequis DNS/TLS/firewall

### 9.1 Risques techniques

- SignalR peut etre plus lourd a integrer dans Unity qu'un WebSocket brut.
- Les tokens actuels sont opaques/in-memory selon configuration; la production chat exige sessions/tokens persistants ou validation centralisee robuste.
- Les roles alliance ne semblent pas encore fournis par un module alliance live; il faut un resolver temporaire strict.
- La sequence par conversation doit etre transactionnelle pour eviter les doublons sous charge.
- Les compteurs `UnreadCount` peuvent diverger si maintenus seulement en cache; ils doivent etre recalculables depuis messages + curseurs.
- Les messages prives hors ligne exigent une vraie inbox serveur, pas seulement un push temps reel.
- La moderation automatique peut bloquer trop ou pas assez; les `reasonCode` doivent rester stables.
- La retention doit conserver les tombstones sans exposer le contenu expire.

### 9.2 Risques operationnels

- DNS absent ou non pointe vers `104.129.128.136` bloque TLS public.
- Certificat TLS manquant ou mal renouvele bloque WebSocket/SignalR fiable.
- IIS doit autoriser WebSocket Protocol si WebSocket/SignalR est active.
- Proxy/IIS timeouts doivent etre ajustes pour connexions temps reel.
- Firewall 443 doit etre ouvert; SQL ne doit pas etre expose public.
- Secrets dans variables IIS doivent etre masques et absents des rapports.
- Backup SQL doit etre prouve avant migrations.
- Rollback doit couvrir binaire + schema SQL.

### 9.3 Decisions a valider avant implementation

- Nom DNS officiel.
- SignalR vs WebSocket brut pour Unity.
- Source d'autorite alliance/roles.
- Politique moderation initiale.
- Retention finale par canal.
- Limites de charge cible : joueurs connectes simultanes, messages/minute, taille historique.
- Activation ou non d'un domaine separe `chat.<domain>`.

## 10. Checkpoint

Le chantier est pret pour validation d'architecture. Aucun deploiement live, aucune modification Unity, aucune scene, aucun asset, aucun APK, aucun BearDen, aucune carte 50x50 et aucun endpoint chat live n'ont ete actives par ce plan.

Prochaine action recommandee apres validation : Phase 0, ajouter le readiness endpoint `/runtime/chat-readiness`, `ChatOptions`, et les tests de garde-fou qui prouvent que le chat serveur reste non live par defaut.
