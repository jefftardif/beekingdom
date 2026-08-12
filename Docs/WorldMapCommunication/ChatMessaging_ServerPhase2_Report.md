# Bee Kingdom - ChatMessaging Server Phase 2 Report

**Date :** 2026-07-16  
**Statut :** Phase 2 locale implementee et testee  
**Live deploy :** non effectue  
**Production target :** `104.129.128.136` non touchee  
**Perimetre respecte :** serveur chat/messagerie et documentation contrat seulement; aucun changement Unity runtime, PNG, Wave5, BearDen, APK ou carte 50x50

## Resume

Phase 2 rend la couche chat prete pour une persistance SQL reelle et documente le contrat client Unity, sans activer le live.

Principales avancees :

- `SqlChatRepository` implemente sur le schema `060_chat_messaging.sql`;
- selection DI `Persistence:Provider=SqlServer` branche maintenant `SqlChatRepository`;
- tests HTTP renforces pour permissions `Alliance`, `Leaders` et annonces dirigeants;
- test SQL opt-in LocalDB ajoute pour conversation, message, inbox et idempotence;
- contrat JSON Unity cree;
- production reste fermee avec `Chat:Enabled=false` et `Chat:RealtimeEnabled=false`.

## Fichiers principaux modifies

- `Server/src/BeeKingdom.Chat/Repositories/SqlChatRepository.cs`
- `Server/src/BeeKingdom.Chat/ChatService.cs`
- `Server/src/BeeKingdom.Chat/ChatManager.cs`
- `Server/src/BeeKingdom.Chat/Models/ChatContracts.cs`
- `Server/src/BeeKingdom.Server/Program.cs`
- `Server/tests/BeeKingdom.Tests/ChatMessagingEndpointTests.cs`
- `Server/tests/BeeKingdom.Tests/PersistenceProviderSelectionTests.cs`
- `Server/tests/BeeKingdom.Tests/SqlServerOptInIntegrationTests.cs`

## Fichiers ajoutes

- `Docs/WorldMapCommunication/ChatMessaging_UnityClientServerContract.md`
- `Docs/WorldMapCommunication/ChatMessaging_ServerPhase2_Report.md`

## SqlChatRepository

Le repository SQL implemente maintenant :

- `SaveConversation`
- `GetConversation`
- `GetConversationByAudience`
- `ListConversations`
- `ListParticipants`
- `GetParticipant`
- `NextSequence`
- `GetOutboxReceipt`
- `SaveOutboxReceipt`
- `SaveMessage`
- `GetMessage`
- `ListMessages`
- `SaveInbox`
- `GetInbox`
- `ListInboxEntries`
- `SaveModerationReport`

Notes techniques :

- ADO.NET direct, conforme aux repositories existants;
- `SqlConnectionFactory` utilise pour runtime connection;
- `ChatConversationSequences` utilise transaction `Serializable` + `UPDLOCK/HOLDLOCK` pour allocation monotone;
- `SaveConversation` et `SaveMessage` utilisent transaction locale;
- payloads complexes stockes en JSON avec `BeeJson`;
- outbox idempotente via cle `(PlayerId, ConversationId, ClientRequestId)`.

## Permissions et canaux

Regles renforcees :

- `Server` : joueur authentifie sur serveur/monde;
- `Private` : participants seulement;
- `Alliance` : exige `requesterAllianceRole` = `member`, `officer` ou `leader`;
- `Leaders` : exige `requesterAllianceRole` = `officer` ou `leader`;
- annonces dirigeants : endpoint dedie exige `officer` ou `leader`.

Endpoint ajoute :

- `POST /chat/v1/alliances/{allianceId}/announcements`

Limite explicite : la source d'autorite alliance live n'existe pas encore dans ce chantier. Phase 2 utilise donc `requesterAllianceRole` comme garde local/staging contractuel. Le branchement final doit remplacer ce hint par une resolution serveur officielle d'appartenance/role.

## Contrat Unity

Contrat cree :

- `Docs/WorldMapCommunication/ChatMessaging_UnityClientServerContract.md`

Il documente :

- readiness;
- capabilities;
- auth bearer;
- enums JSON en chaines;
- creation conversations `Server`, `Private`, `Alliance`, `Leaders`;
- envoi message;
- idempotence;
- historique et read cursor;
- annonces dirigeants;
- moderation report;
- reconnect;
- evenement temps reel prepare.

Le serveur ASP.NET Core expose maintenant les enums JSON en chaines via `JsonStringEnumConverter`; les valeurs numeriques restent acceptees par compatibilite .NET.

## Gates

Toujours fermes par defaut :

- `Chat:Enabled=false` dans `appsettings.json`;
- `Chat:Enabled=false` dans `appsettings.Production.json`;
- `Chat:RealtimeEnabled=false` dans les deux configurations;
- `Persistence:Provider=InMemory` en production;
- aucun secret ajoute;
- aucun DNS/TLS/firewall/IIS modifie;
- aucun appel ou deploiement sur `104.129.128.136`.

## Tests

Commandes executees :

- `dotnet build Server/BeeKingdom.Server.slnx --no-restore`
- `dotnet test Server/tests/BeeKingdom.Tests/BeeKingdom.Tests.csproj --no-build --filter "ChatMessagingEndpointTests|PersistenceProviderSelectionTests|DatabaseMigrationTests|SqlServerChatRepositoryRoundTripsConversationMessageInboxAndIdempotence"`
- `dotnet test Server/tests/BeeKingdom.Tests/BeeKingdom.Tests.csproj --no-build`

Resultats :

- build : reussie, 0 erreur, 0 avertissement;
- tests cibles : 22 reussis, 1 ignore opt-in SQL, 0 echec;
- suite serveur complete : 185 reussis, 7 ignores opt-in SQL, 0 echec.

Le test SQL ajoute est :

- `SqlServerChatRepositoryRoundTripsConversationMessageInboxAndIdempotence`

Il est ignore ici car `BEE_SQL_INTEGRATION_CONNECTION_STRING` n'est pas configure. Comme les autres tests SQL existants, il exige LocalDB, Integrated Security, et refuse les cibles distantes.

## Limites restantes

- La source d'autorite alliance/roles doit etre remplacee par un resolver serveur officiel.
- Le dispatcher SignalR reste `NoopChatRealtimeDispatcher`; le hub est prepare mais pas actif tant que `Chat:RealtimeEnabled=false`.
- Anti-spam avance, moderation automatique et retention job restent a implementer.
- Le mode SQL chat est code, mais pas execute ici sans LocalDB opt-in.
- Aucun adaptateur Unity n'est implemente; seul le contrat client/serveur est documente.

## Prochaine tranche recommandee

Phase 3 devrait traiter :

1. `IChatAudienceResolver` officiel branche sur alliances/roles serveur;
2. `SignalRChatRealtimeDispatcher` avec groupes `player`, `conversation`, `alliance`, `leaders`;
3. tests de reconnexion et replay `afterSequence`;
4. rate limiting chat persistant ou distribue;
5. job retention/tombstones;
6. staging local ferme avec `Chat:Enabled=true`, SQL LocalDB ou SQL staging non public.
