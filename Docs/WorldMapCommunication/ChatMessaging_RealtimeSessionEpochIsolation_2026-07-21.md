# Chat — isolation des événements temps réel par génération de session (2026-07-21)

## Résultat

Les événements temps réel sont désormais liés à la génération de session sous laquelle ils ont été reçus.

La génération est vérifiée :

- après validation de la session et acquisition du verrou de réconciliation ;
- après une éventuelle récupération REST destinée à combler un trou de séquence ;
- immédiatement avant la fusion du message dans le flux local.

Une déconnexion ou un changement de compte révoque donc également les événements déjà en attente du verrou. Ils échouent avec `Cancelled` / `local_session_changed` et ne peuvent pas repeupler un flux purgé.

## Scénario concurrent prouvé

1. un événement de séquence 2 détecte le trou 1 et bloque pendant la récupération REST ;
2. l’événement de séquence 1 est reçu sous la même ancienne session et attend le verrou ;
3. le joueur se déconnecte ;
4. la réponse REST est libérée ;
5. les deux événements sont rejetés comme appartenant à une génération révoquée ;
6. la séquence confirmée reste à zéro et aucun message n’est restauré.

## Validation

- course entièrement déterministe avec transport et source de session injectables ;
- aucun délai arbitraire dans le test ;
- réconciliation, ordre, doublons et secours polling préexistants conservés ;
- suite isolée Communication : **124/124 tests réussis**, compilation sans erreur ni avertissement.

## Directive serveur et staging

Après une reconnexion, le serveur temps réel doit établir une nouvelle association authentifiée et ne jamais réutiliser les abonnements de l’ancienne connexion. Le client repart ensuite de sa dernière séquence confirmée au moyen de REST/polling. La matrice staging doit injecter un événement en retard au moment exact du logout et vérifier qu’il n’apparaît ni sous B ni après retour de A avant la réconciliation autorisée.

Le candidat serveur courant reste `BeeKingdom.Server.20260721T201425Z`, `DeploymentAuthorized=false`. Aucun nouveau candidat n’est requis si l’isolation des connexions et groupes SignalR est déjà démontrée.

Aucun transfert, déploiement, activation ni synchronisation n’est autorisé par ce jalon.
