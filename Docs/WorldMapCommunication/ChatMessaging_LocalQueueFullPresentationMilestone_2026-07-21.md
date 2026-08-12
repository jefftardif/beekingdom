# Chat et messagerie — signal de file locale pleine

Date : 2026-07-21  
Responsable : Communication

## Résultat

La saturation d'un journal hors ligne est maintenant exposée au consommateur du fournisseur avec un état distinct et stable : `RemoteChatError.LocalQueueFull`, code `local_queue_full` et statut HTTP 0. Elle ne peut plus être confondue avec une panne réseau, une limite serveur 429 ou une réponse invalide.

Le signal couvre l'envoi d'un message, la création d'une conversation, un signalement de modération et un nouveau curseur de lecture. Le texte destiné à l'interface indique de rétablir la connexion avant de tenter une nouvelle opération. L'opération refusée n'est jamais présentée comme mise en attente.

Un diagnostic sûr `local_queue_full` est produit avec seulement le type d'opération et la capacité du journal. Il n'inclut aucun corps, titre, identifiant de conversation ou message, catégorie de signalement, identifiant de requête ou jeton.

## Validation

- Compilation isolée Communication : réussie, sans erreur ni avertissement.
- Suite ciblée : 66/66 réussie.
- Les quatre chemins retournent `LocalQueueFull`, `local_queue_full` et HTTP 0 avant tout appel REST.
- Les quatre diagnostics portent la capacité attendue et aucun contenu sensible injecté par les essais.
- Aucun déploiement, activation ni synchronisation effectué.

## Préparation SQL reçue

L'Intégrateur a ajouté `Server/tools/Test-SqlDisposable.ps1`. Le script exige `BEE_SQL_INTEGRATION_CONNECTION_STRING` au niveau du processus, accepte exclusivement LocalDB avec Integrated Security, refuse credentials et toute cible distante, n'affiche jamais la chaîne et ne lance que `SqlServerOptInIntegrationTests`. Les refus d'une configuration absente et de la cible distante `104.129.128.136` ont été vérifiés avant connexion. Le parcours positif demeure non exécuté parce que LocalDB n'est pas installé dans cet environnement.

Le scénario serveur « journal plein → drainage idempotent → place libérée → nouvelle opération » et la distinction saturation locale/statut serveur sont désormais documentés. Aucun accès SQL externe n'a été tenté.

## Directive d'intégration

Le client Unity doit localiser un message dédié à `LocalQueueFull`, désactiver seulement la nouvelle action concernée et proposer une remise en ligne; il ne doit ni afficher un faux 429 ni réessayer en boucle. Les métriques serveur ne doivent pas compter `local_queue_full` comme une requête reçue. Le scénario de staging doit prouver zéro appel HTTP pour l'opération refusée, puis un drainage idempotent et une nouvelle opération acceptée après libération. Les portes de production restent fermées.
