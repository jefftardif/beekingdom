# Chat serveur — corps/cibles bornés et reçus corrélés

Date: 2026-07-21  
État: validation locale uniquement

## Contrat appliqué

Les requêtes `POST /chat/v1/*` sont limitées à 65 536 octets UTF-8 par défaut. `ChatOptions.MaxRequestBytes` accepte 1 024..1 048 576 octets et est appliqué avant lecture JSON via `IHttpMaxRequestBodySizeFeature`; un `Content-Length` supérieur est refusé en 413 `chat.invalid_request`. Les corps exacts sont acceptés, l'octet supplémentaire est refusé, et aucune donnée excédentaire n'est désérialisée ou persistée. Les limites IIS/proxy doivent rester au moins aussi strictes et sans page HTML de remplacement.

La cible complète `/chat/v1` (chemin + query) est bornée à 8 192 octets UTF-8, configurable 1 024..16 384. Au-delà, le serveur répond 414 structuré sans journaliser l'URL brute. Les curseurs de conversations restent opaques, liés au joueur, limités à 1 024 caractères, sans contrôle ni padding; un curseur invalide/altéré ou un `afterSequence` négatif donne `400 chat.invalid_request`. Les pages sont bornées à 1..100.

Les réponses chat sont plafonnées contractuellement à 1 048 576 octets (configuration documentée 1 024..4 194 304), les pages sont paginées et les tests refusent tout HTML intermédiaire. Les réponses JSON restent sous la borne, qu'elles soient compressées ou non; les diagnostics ne contiennent aucun corps, identifiant brut, token ou URL complète.

Les reçus corrélés sont maintenant stables en camelCase:

- création: `conversation`, `inbox`, `clientRequestId` exact;
- envoi: `message` cohérent (`messageId`, `conversationId`, `clientRequestId`, `body`, `sequence`, `senderPlayerId`, `acceptedAtUtc`) et `serverSequence == sequence`;
- signalement: `reportId`, `messageId`, `clientRequestId`, `status` non vides.

La création et le signalement reprennent les valeurs du même reçu/commit, y compris lors d'une déduplication. Les tests HTTP vérifient la concordance conversation/inbox et message/report. Une incohérence côté client reste une réponse 2xx invalide sans acquittement local; le serveur n'émet jamais de corps de message dans les erreurs.

## Preuves

- build Release: 0 erreur, 0 avertissement;
- tests chat isolés: 21/21;
- tests ciblés corps/cible/cursor/reçus: verts;
- suite HTTP complète complémentaire net10: 240 réussis, 7 SQL opt-in ignorés, 0 échec, total 247;
- smoke: `Healthy`, `chat-v1`, rétention 30, `server=false`, `realtime=false`, `PreparationOnly`;
- candidat courant: `Server/artifacts/candidates/BeeKingdom.Server.20260721T180651Z`, 54 fichiers avant manifeste, sans PDB/Development, `DeploymentAuthorized=false`;
- `175116Z` et tous les candidats antérieurs sont révoqués dans `CANDIDATE-STATUS.json`.

Les portes SQL jetable, .NET 8 natif, TLS/SNI/Full strict et Android/IIS staging restent ouvertes. Aucun transfert, déploiement, activation ou synchronisation n'a été effectué.

## Fichiers créés/modifiés

Créés:

- `Server/src/BeeKingdom.Chat/Diagnostics/ChatResponseBudget.cs`
- `Server/tests/BeeKingdom.Tests/ChatResponseBudgetTests.cs`
- `Docs/ProductionIntegration/ChatMessaging_ServerBoundedBodiesAndReceipts_2026-07-21.md`

Modifiés:

- `Server/src/BeeKingdom.Chat/Configuration/ChatOptions.cs`
- `Server/src/BeeKingdom.Chat/DependencyInjection/ChatServiceCollectionExtensions.cs`
- `Server/src/BeeKingdom.Chat/ChatService.cs`
- `Server/src/BeeKingdom.Chat/Models/ChatTransportDtos.cs`
- `Server/src/BeeKingdom.Server/Program.cs`
- `Server/src/BeeKingdom.Server/appsettings.json`
- `Server/src/BeeKingdom.Server/appsettings.Production.json`
- `Server/tests/BeeKingdom.Tests/ChatMessagingEndpointTests.cs`
- `Server/tests/BeeKingdom.Tests/ChatOptionsValidationTests.cs`
- `Server/tests/BeeKingdom.Tests/ChatTransportContractTests.cs`
- `Server/tools/Test-ChatStagingPreflight.ps1`
- `Server/artifacts/candidates/CANDIDATE-STATUS.json`

Artefact créé: `Server/artifacts/candidates/BeeKingdom.Server.20260721T180651Z/` et son manifeste.
