# Bee Kingdom - ChatMessaging Server Phase 1 Report

**Date :** 2026-07-16  
**Statut :** Phase 1 locale implementee et testee  
**Live deploy :** non effectue  
**IP production :** `104.129.128.136` non touchee  
**Perimetre respecte :** serveur chat/messagerie uniquement; aucun changement Unity, PNG, Wave5, BearDen, APK ou carte 50x50

## Resume

Phase 1 ajoute une tranche serveur stable pour le chat/messagerie Bee Kingdom dans l'architecture ASP.NET Core existante.

Le chat reste non live par defaut :

- `Chat:Enabled=false`
- `Chat:RealtimeEnabled=false`
- `Persistence:Provider=InMemory` en production
- aucun secret ajoute
- aucun deploiement effectue

Quand `Chat:Enabled=true` en test local, le serveur expose une premiere implementation REST authentifiee par access token existant, avec repository in-memory, conversations, messages, inbox, idempotence d'envoi et moderation report.

## Fichiers principaux ajoutes

- `Server/src/BeeKingdom.Chat/BeeKingdom.Chat.csproj`
- `Server/src/BeeKingdom.Chat/Configuration/ChatOptions.cs`
- `Server/src/BeeKingdom.Chat/Models/ChatChannelType.cs`
- `Server/src/BeeKingdom.Chat/Models/ChatPermissionRole.cs`
- `Server/src/BeeKingdom.Chat/Models/ChatMessageState.cs`
- `Server/src/BeeKingdom.Chat/Models/ChatModerationStatus.cs`
- `Server/src/BeeKingdom.Chat/Models/ChatRecords.cs`
- `Server/src/BeeKingdom.Chat/Models/ChatContracts.cs`
- `Server/src/BeeKingdom.Chat/Repositories/IChatRepository.cs`
- `Server/src/BeeKingdom.Chat/Repositories/InMemoryChatRepository.cs`
- `Server/src/BeeKingdom.Chat/Repositories/SqlChatRepository.cs`
- `Server/src/BeeKingdom.Chat/Realtime/ChatRealtimeHub.cs`
- `Server/src/BeeKingdom.Chat/Realtime/IChatRealtimeDispatcher.cs`
- `Server/src/BeeKingdom.Chat/Realtime/NoopChatRealtimeDispatcher.cs`
- `Server/src/BeeKingdom.Chat/ChatService.cs`
- `Server/src/BeeKingdom.Chat/ChatManager.cs`
- `Server/src/BeeKingdom.Chat/DependencyInjection/ChatServiceCollectionExtensions.cs`
- `Server/src/BeeKingdom.Database/Scripts/060_chat_messaging.sql`
- `Server/tests/BeeKingdom.Tests/ChatMessagingEndpointTests.cs`

## Fichiers principaux modifies

- `Server/BeeKingdom.Server.slnx`
- `Server/src/BeeKingdom.Server/BeeKingdom.Server.csproj`
- `Server/src/BeeKingdom.Server/Program.cs`
- `Server/src/BeeKingdom.Server/appsettings.json`
- `Server/src/BeeKingdom.Server/appsettings.Production.json`
- `Server/src/BeeKingdom.Database/DatabaseCatalog.cs`
- `Server/src/BeeKingdom.Database/DatabaseRollbackCatalog.cs`
- `Server/tests/BeeKingdom.Tests/BeeKingdom.Tests.csproj`
- `Server/tests/BeeKingdom.Tests/DatabaseMigrationTests.cs`
- `Server/tests/BeeKingdom.Tests/HttpEndpointTests.cs`

## Contrat serveur minimal

Canaux couverts :

- `Alliance`
- `Server`
- `Private`
- `Leaders`

Modeles persistants prepares :

- `ChatConversation`
- `ChatConversationParticipant`
- `ChatMessage`
- `ChatInboxEntry`
- `ChatOutboxReceipt`
- `ChatModerationReport`
- `ChatEventEnvelope`

Invariants Phase 1 :

- idempotence par `(playerId, conversationId, clientRequestId)`;
- sequence monotone par conversation dans le repository in-memory;
- messages acceptes avec `MessageId`, `AcceptedAtUtc`, `Sequence`, `State=Accepted`;
- inbox par joueur/conversation avec unread et mention count;
- permissions minimales par participant;
- `officialGain=false`;
- mutations refusees si `Chat:Enabled=false`.

## Endpoints crees

Public/non secret :

- `GET /runtime/chat-readiness`
- `GET /chat/v1/capabilities`

REST authentifie par `Authorization: Bearer <accessToken>` :

- `GET /chat/v1/conversations`
- `POST /chat/v1/conversations`
- `GET /chat/v1/conversations/{conversationId}/messages`
- `POST /chat/v1/conversations/{conversationId}/messages`
- `POST /chat/v1/conversations/{conversationId}/read`
- `POST /chat/v1/messages/{messageId}/report`

Temps reel prepare :

- `GET /chat/v1/realtime` via SignalR hub
- le hub abort la connexion tant que `Chat:Enabled=false` ou `Chat:RealtimeEnabled=false`
- le dispatcher actuel est `NoopChatRealtimeDispatcher`; publication live effective reportee a la phase temps reel suivante

## Schema SQL prepare

Migration ajoutee :

- `060_chat_messaging.sql`

Tables preparees :

- `dbo.ChatConversations`
- `dbo.ChatConversationParticipants`
- `dbo.ChatConversationSequences`
- `dbo.ChatMessages`
- `dbo.ChatInbox`
- `dbo.ChatOutboxReceipts`
- `dbo.ChatModerationReports`

Rollback ajoute :

- `060_rollback_chat_messaging.sql` dans `DatabaseRollbackCatalog`

Note : le repository SQL est volontairement un stub Phase 1. Le schema est pret, mais le runtime SQL chat complet est planifie Phase 2.

## Tests ajoutes ou ajustes

Nouveaux tests :

- `ChatReadinessAndCapabilitiesStayNonLiveByDefault`
- `ChatMutationEndpointIsGatedWhenDisabled`
- `ChatPrivateConversationSendReadAndIdempotenceWorkWhenEnabled`

Tests migration ajustes :

- presence de `060_chat_messaging.sql`
- presence des tables chat dans le catalogue
- rollback chat en ordre inverse avant les tables existantes
- endpoint rollback ops attend maintenant 5 scripts

## Verification executee

Commandes locales executees :

- `dotnet restore Server/BeeKingdom.Server.slnx`
- `dotnet build Server/BeeKingdom.Server.slnx --no-restore`
- `dotnet test Server/tests/BeeKingdom.Tests/BeeKingdom.Tests.csproj --no-build --filter "ChatMessagingEndpointTests|DatabaseMigrationTests"`
- `dotnet test Server/tests/BeeKingdom.Tests/BeeKingdom.Tests.csproj --no-build`

Resultats :

- build : reussie, 0 erreur, 0 avertissement
- tests cibles : 15 reussis, 0 echec
- suite serveur complete : 183 reussis, 6 ignores opt-in SQL, 0 echec

## Gates et limites

Gates actifs :

- production garde `Chat:Enabled=false`;
- production garde `Chat:RealtimeEnabled=false`;
- mutations REST retournent `503 chat_disabled` quand le gate est ferme;
- routes REST mutantes exigent un access token valide;
- hub SignalR ferme la connexion si le gate realtime est ferme;
- aucun secret, DNS, TLS, firewall, IIS ou SQL live n'a ete modifie;
- aucune action sur `104.129.128.136`.

Limites connues Phase 1 :

- repository SQL non implemente, seulement schema prepare;
- roles alliance/leaders utilisent une permission minimale de participant, pas encore une source d'autorite alliance live;
- moderation automatique et anti-spam avances restent a implementer;
- dispatcher temps reel effectif non branche;
- retention job non implemente;
- contrat Unity non modifie.

## Prochaine tranche recommandee

Phase 2 devrait implementer le repository SQL transactionnel :

1. allocation atomique de `ChatConversationSequences`;
2. persistence SQL conversations/messages/inbox/outbox;
3. tests SQL opt-in locaux;
4. recalcul fiable des unread/mentions;
5. resolver d'audience alliance/leaders branche sur une source serveur officielle ou stub gouverne;
6. publication SignalR effective derriere `Chat:RealtimeEnabled=true`.
