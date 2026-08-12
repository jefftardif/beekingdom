# Chat — reçu de lecture corrélé (2026-07-21)

## Résultat

Le curseur de lecture persistant n’est plus acquitté par un simple statut HTTP réussi. Le client désérialise maintenant la réponse `/read` comme une entrée inbox structurée et vérifie :

- `conversationId` exactement égal à la conversation demandée ;
- `readCursorSequence` supérieur ou égal à la séquence envoyée ;
- `unreadCount` et `mentionCount` non négatifs.

Une réponse vide, croisée avec une autre conversation, régressive ou contenant des compteurs invalides produit `InvalidResponse` avec `read_receipt_mismatch`. Le curseur local demeure dans la file persistante.

La concurrence monotone reste protégée : si une séquence plus récente est enregistrée localement pendant que l’ancienne requête est en vol, l’acquittement de l’ancienne ne retire jamais la plus récente.

## Validation

- reçu d’une autre conversation rejeté et curseur conservé ;
- reçu valide accepté ;
- concurrence séquence 5 en vol puis séquence 8 locale : seule 5 est acquittée, 8 demeure ;
- codec Unity prend maintenant en charge `RemoteInboxEntry` directement.

Suite isolée Communication : **107/107 tests réussis**, compilation sans erreur ni avertissement.

## Directive serveur

La route `POST /chat/v1/conversations/{conversationId}/read` doit répondre en camelCase avec l’entrée inbox durable résultant du commit, notamment `conversationId`, `readCursorSequence`, `unreadCount` et `mentionCount`. La séquence retournée doit être la valeur monotone réellement relue après persistance, jamais seulement la valeur demandée en mémoire.

Les tests SQL/HTTP doivent couvrir répétition, séquence inférieure, séquence supérieure concurrente, coupure après commit, reprise, réponse croisée artificielle et compteurs non négatifs. Le REST et tout événement inbox temps réel doivent exposer le même état après commit.

Le prochain candidat doit intégrer ce contrôle avec les autres reçus corrélés, révoquer son prédécesseur et rester `DeploymentAuthorized=false` jusqu’aux portes SQL, .NET 8, TLS/IIS et Android staging. Aucun transfert, déploiement, activation ni synchronisation n’est autorisé ici.
