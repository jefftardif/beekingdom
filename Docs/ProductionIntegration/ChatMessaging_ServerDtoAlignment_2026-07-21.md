# Alignement DTO serveur–Unity — 2026-07-21

## Contrat vérifié

Les réponses HTTP utilisent la politique ASP.NET Core camelCase. Une conversation expose `conversationId`, `title`, `channelType`, `lastSequence`, `readCursorSequence`, `unreadCount` et `mentionCount`. Un message expose `messageId`, `conversationId`, `clientRequestId`, `senderPlayerId`, `senderDisplayName`, `channelType`, `body`, `sequence` et `acceptedAtUtc`, ainsi que les champs historiques nécessaires à la compatibilité (`gameServerId`, `worldId`, `clientCreatedAtUtc`, `senderDisplayNameSnapshot`). Aucun renommage silencieux n’a été effectué.

La liste des conversations est construite à partir de l’identité authentifiée et du dépôt, puis chaque entrée d’inbox est relue pour ce même joueur et cette conversation. Les contrôles d’appartenance et de lecture restent appliqués par le service. Les compteurs et curseurs ne proviennent donc pas d’un état client.

La création réutilise l’inbox issue du même résultat durable. Les mutations de lecture restent liées au joueur authentifié. La fermeture d’un overlay est un état d’interface local : elle ne déclenche ni logout, ni déconnexion, ni suppression de session côté serveur.

## Preuves locales

- `ChatTransportContractTests`: 18/18, incluant les champs camelCase et les identifiants/horodatages de reçu.
- Suite serveur ciblée et compilation précédentes: sans erreur; les tests SQL restent conditionnés à une instance SQL isolée non disponible dans cette VM.
- Aucun secret, corps de message ou identifiant brut n’est ajouté aux journaux.

## Fichiers modifiés

- `Server/src/BeeKingdom.Chat/Models/ChatTransportDtos.cs`
- `Server/src/BeeKingdom.Chat/ChatService.cs`
- `Server/src/BeeKingdom.Chat/ChatManager.cs`
- `Server/src/BeeKingdom.Server/Program.cs`
- `Server/tests/BeeKingdom.Tests/ChatTransportContractTests.cs`
- `Docs/ProductionIntegration/ChatMessaging_ServerDtoAlignment_2026-07-21.md`

## État d’intégration

Le candidat local courant est `BeeKingdom.Server.20260721T225554Z`, manifeste de 55 fichiers. `DeploymentAuthorized=false`, Chat/Realtime restent désactivés et la préparation demeure locale uniquement. Aucun transfert ou activation n’a été effectué.
