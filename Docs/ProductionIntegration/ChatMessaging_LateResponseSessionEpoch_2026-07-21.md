# Réponse tardive et génération de session

Le service serveur committe le message et son reçu avant la publication temps réel. Une annulation/déconnexion après commit peut donc abandonner la réponse sans perdre l’opération: une reprise avec le même `(PlayerId, ConversationId, ClientRequestId)` retourne le même message avec `Deduplicated=true`, sans nouvelle séquence ni doublon.

Preuve déterministe existante: `CancellationBeforeCommitHasNoEffectAndDisconnectAfterCommitReplaysReceipt` vérifie à la fois l’absence d’effet avant commit et la reprise idempotente après annulation pendant la publication.

## Scénario Android staging

1. A lance une requête retardée liée à l’époque de session A.
2. Logout; purger les états volatils, conserver le journal partitionné A.
3. B se connecte; toute réponse tardive A est ignorée avant refresh, reçu, acquittement ou cache.
4. Retour A; restaurer le journal et drainer avec le même reçu.
5. Vérifier une seule séquence, un seul message et aucun doublon après la réponse tardive abandonnée.

Aucune modification runtime n’est requise pour ce jalon. Candidat conservé: `BeeKingdom.Server.20260721T201425Z`, `DeploymentAuthorized=false`.
