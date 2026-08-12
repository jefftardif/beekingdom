# Chat serveur — reçu de lecture corrélé

Date: 2026-07-21  
État: validation locale uniquement

`POST /chat/v1/conversations/{conversationId}/read` renvoie directement l'entrée Inbox durable après fusion atomique. Le JSON camelCase contient le même `conversationId`, `readCursorSequence` au moins égal à la séquence demandée, ainsi que `unreadCount` et `mentionCount` non négatifs. Le repository applique `max(current, requested)`, y compris en concurrence et lors d'un retry inférieur.

Les tests HTTP vérifient la concordance conversation/réponse; les tests service couvrent les mises à jour 10 puis 4, les curseurs concurrents et les reprises après reconstruction. La lecture ne crée pas de nouvelle mutation de message et un acquittement inférieur ne peut pas effacer une lecture plus récente. Les réponses de lecture et les événements éventuels utilisent la même entrée durable; aucune donnée de corps, token ou identifiant brut n'est journalisée.

Preuves du candidat regroupé:

- build 0 erreur / 0 avertissement;
- tests chat isolés: 21/21;
- suite HTTP net10: 240 réussis, 7 SQL opt-in ignorés, 0 échec, total 247;
- smoke `Healthy`, `chat-v1`, `server=false`, `realtime=false`, `PreparationOnly`;
- candidat reconstruit: `Server/artifacts/candidates/BeeKingdom.Server.20260721T180651Z`, 54 fichiers avant manifeste, `DeploymentAuthorized=false`; `175116Z` et les précédents sont révoqués.

Fichier modifié dans ce lot:

- `Server/tests/BeeKingdom.Tests/ChatMessagingEndpointTests.cs`

Les portes SQL jetable, .NET 8 natif, TLS/SNI/Full strict, IIS/proxy et Android restent ouvertes. Aucun transfert, déploiement, activation ou synchronisation n'a été effectué.
