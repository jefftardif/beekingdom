# Chat serveur — flux entrants validés avant fusion

Date: 2026-07-21  
État: validation locale uniquement

Les pages REST produites par le serveur restent bornées et cohérentes avant fusion client: `limit` 1..100, messages d'une seule conversation/joueur autorisé, identifiants distincts, séquences strictement croissantes et supérieures à `afterSequence`, `nextAfterSequence` nul ou au moins égal au dernier élément. Les conversations sont uniques et leurs curseurs restent opaques et joueur-scoped. Un joueur différent reçoit 403; une page croisée ne peut pas être fusionnée.

Le test temps réel existant vérifie que l'événement est publié après commit et lisibilité REST avec le même message, conversation, séquence, requête cliente, corps et horodatage. Le nouveau test couvre duplication, ordre, borne de corps, ciblage joueur/conversation et curseurs sûrs. Les erreurs de curseur, de route ou de corps restent structurées; aucun log ne contient ID brut, corps, curseur ou joueur.

Preuves du candidat successeur:

- build 0/0;
- tests chat isolés: 21/21;
- suite HTTP complémentaire net10: 240 réussis, 7 SQL opt-in ignorés, 0 échec, total 247;
- smoke `Healthy`, `chat-v1`, `server=false`, `realtime=false`, `PreparationOnly`;
- `DeploymentAuthorized=false`; SQL/.NET8/TLS/IIS/Android staging encore ouverts.

Fichier modifié:

- `Server/tests/BeeKingdom.Tests/ChatTransportContractTests.cs`

Rapport créé:

- `Docs/ProductionIntegration/ChatMessaging_ServerValidatedInboundStreams_2026-07-21.md`

Aucun transfert, déploiement, activation ou synchronisation finale.
