# Chat et messagerie — reprise coordonnée des files persistantes

Date : 2026-07-21  
Responsable : Communication

## Résultat

`ServerChatProvider` expose maintenant `GetPendingQueueStatusAsync` et `DrainPendingAsync`. L'interface peut connaître séparément et globalement le nombre de conversations, messages, curseurs de lecture et signalements encore en attente, sans lire leur contenu.

Le drainage est sérialisé afin qu'une seule reprise soit active à la fois. L'ordre respecte les dépendances : créations de conversation, messages, curseurs de lecture, puis signalements. Les reçus et identifiants idempotents existants restent utilisés par chaque opération.

`ChatPendingDrainResult` fournit l'état initial, l'état restant, le nombre terminé et `IsComplete`. Si une panne survient après une réussite partielle, `ChatPendingDrainException` conserve ce résultat partiel et l'exception d'origine; les entrées non acquittées restent dans leurs journaux. Une annulation demandée demeure une annulation et n'est pas transformée en panne de drainage.

Les diagnostics `pending_drain_started`, `pending_drain_completed` et `pending_drain_incomplete` ne contiennent que des compteurs et une catégorie d'erreur. Aucun contenu ou identifiant métier n'est journalisé.

## Validation

- Compilation isolée Communication : réussie, sans erreur ni avertissement.
- Suite ciblée : 68/68 réussie.
- Drainage complet de quatre types : état initial 4, terminé 4, restant 0.
- Échec 503 après création réussie : résultat partiel terminé 1, message restant 1, conversation restante 0.
- Les diagnostics de reprise ne divulguent ni corps ni identifiants injectés.
- Aucun déploiement, activation ni synchronisation effectué.

## Directive d'intégration

En staging, déclencher un drainage à la reconnexion avec les quatre types présents et vérifier l'ordre des appels, les reçus idempotents, l'absence de doublon et les compteurs avant/après. Injecter ensuite un 503 après une première réussite : le serveur doit conserver la réussite acquittée et accepter au prochain drainage uniquement les éléments restants. L'interface doit afficher une reprise partielle sans prétendre que toute la file est envoyée. Les portes de production restent fermées jusqu'à l'hôte staging autorisé et aux validations SQL/TLS/Android prévues.
