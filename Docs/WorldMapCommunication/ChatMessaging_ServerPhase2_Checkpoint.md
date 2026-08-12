# Bee Kingdom - ChatMessaging Server Phase 2 Checkpoint

**Date :** 2026-07-16  
**Statut :** checkpoint Phase 2 local  
**Live deploy :** non effectue  
**Secrets :** aucun secret ajoute  
**Unity/runtime/assets :** non modifies

## Fichiers deja modifies ou ajoutes

Code serveur :

- `Server/src/BeeKingdom.Chat/Repositories/SqlChatRepository.cs`
- `Server/src/BeeKingdom.Chat/ChatService.cs`
- `Server/src/BeeKingdom.Chat/ChatManager.cs`
- `Server/src/BeeKingdom.Chat/Models/ChatContracts.cs`
- `Server/src/BeeKingdom.Server/Program.cs`

Tests :

- `Server/tests/BeeKingdom.Tests/ChatMessagingEndpointTests.cs`
- `Server/tests/BeeKingdom.Tests/PersistenceProviderSelectionTests.cs`
- `Server/tests/BeeKingdom.Tests/SqlServerOptInIntegrationTests.cs`

Documentation :

- `Docs/WorldMapCommunication/ChatMessaging_UnityClientServerContract.md`
- `Docs/WorldMapCommunication/ChatMessaging_ServerPhase2_Report.md`
- `Docs/WorldMapCommunication/ChatMessaging_ServerPhase2_Checkpoint.md`

## Statut SqlChatRepository

`SqlChatRepository` est implemente sur le schema `060_chat_messaging.sql`.

Fonctions couvertes :

- conversations;
- participants;
- sequence monotone par conversation;
- messages;
- inbox/unread/mentions;
- outbox/idempotence;
- moderation reports;
- selection DI `Persistence:Provider=SqlServer`.

Test SQL reel prepare :

- `SqlServerChatRepositoryRoundTripsConversationMessageInboxAndIdempotence`
- opt-in LocalDB seulement via `BEE_SQL_INTEGRATION_CONNECTION_STRING`;
- ignore par design si la variable n'est pas configuree;
- refuse les cibles distantes.

## Statut permissions par canal

- `Server` : joueur authentifie sur serveur/monde.
- `Private` : participants seulement.
- `Alliance` : exige `requesterAllianceRole` = `member`, `officer` ou `leader`.
- `Leaders` : exige `requesterAllianceRole` = `officer` ou `leader`.
- Annonces dirigeants : endpoint dedie exige `officer` ou `leader`.

Limite connue : `requesterAllianceRole` est un garde local/staging. La source finale doit etre un resolver serveur officiel d'appartenance alliance et roles.

## Statut tests

Derniere verification locale :

- build : reussie, 0 erreur, 0 avertissement;
- tests cibles : 22 reussis, 1 ignore opt-in SQL, 0 echec;
- suite serveur complete : 185 reussis, 7 ignores opt-in SQL, 0 echec.

## Blocages eventuels

Aucun blocage pour le livrable Phase 2 local.

Blocages pour production/live :

- pas de validation explicite de deploiement;
- pas de DNS/TLS/firewall valide dans ce chantier;
- pas de source officielle alliance/roles encore branchee;
- SignalR dispatcher effectif encore noop;
- test SQL reel non execute ici sans LocalDB opt-in;
- `Chat:Enabled=false` et `Chat:RealtimeEnabled=false` restent les gates production.

## Prochaine action minimale

Prochaine tranche minimale recommandee :

1. Ajouter `IChatAudienceResolver` officiel pour remplacer `requesterAllianceRole`.
2. Implementer `SignalRChatRealtimeDispatcher` derriere `Chat:RealtimeEnabled`.
3. Executer le test SQL opt-in sur LocalDB avec `BEE_SQL_INTEGRATION_CONNECTION_STRING`.
4. Conserver production fermee tant que staging ferme n'est pas valide.

## Continuation

Le rapport final Phase 2 est deja publie dans :

- `Docs/WorldMapCommunication/ChatMessaging_ServerPhase2_Report.md`

Aucun live deploy, aucun secret et aucun changement Unity n'ont ete effectues.
