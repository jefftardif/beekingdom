# Chat — rejet des réponses tardives après changement de session (2026-07-21)

## Résultat

Chaque requête de `ServerChatProvider` est désormais liée à la génération de session active au moment de son départ.

Une déconnexion ou une incompatibilité compte-partition incrémente cette génération et purge l’état volatil. Si une ancienne réponse HTTP arrive ensuite, elle est rejetée avant :

- le renouvellement automatique d’un jeton après 401 ;
- la validation d’un reçu ;
- l’acquittement d’un journal persistant ;
- la fusion d’un message ou d’une séquence ;
- la mise en cache d’une traduction ou de capacités.

L’erreur locale est `Cancelled` avec le code sûr `local_session_changed`. Une opération mutante reste dans son journal et pourra être reprise idempotemment par la session correcte.

## Propriété de course

La déconnexion ne dépend plus uniquement de l’annulation coopérative du transport. Même si une implémentation HTTP termine normalement après le logout, sa réponse appartient à une génération révoquée et ne peut plus modifier l’état client.

Le contrôle est également effectué avant un éventuel rafraîchissement 401 afin qu’une ancienne requête ne provoque pas de renouvellement de session après la fermeture.

## Validation

- curseur de lecture persisté puis requête bloquée ;
- déconnexion pendant l’attente ;
- réponse 200 libérée après la purge ;
- résultat rejeté avec `local_session_changed` ;
- curseur conservé dans le journal, aucune séquence locale restaurée ;
- parcours normaux, 401 et isolation multi-compte précédents conservés ;
- suite isolée Communication : **123/123 tests réussis**, compilation sans erreur ni avertissement.

## Directive serveur et staging

Le serveur doit continuer à traiter les mutations de façon idempotente : une coupure client après commit peut laisser le client sans reçu, puis la reprise avec le même `ClientRequestId` ou curseur doit relire l’effet durable sans doublon. La matrice staging doit inclure une réponse volontairement retardée, logout avant réception, connexion B, puis retour A et drainage.

Le candidat serveur courant reste `BeeKingdom.Server.20260721T201425Z`, `DeploymentAuthorized=false`. Aucun nouveau candidat n’est nécessaire si les garanties serveur existantes couvrent déjà cette course; toute preuve additionnelle doit rester locale tant que les portes externes sont ouvertes.

Aucun transfert, déploiement, activation ni synchronisation n’est autorisé par ce jalon.
