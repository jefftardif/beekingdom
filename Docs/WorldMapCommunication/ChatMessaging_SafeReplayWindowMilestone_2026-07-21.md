# Chat et messagerie — fenêtre sûre de rejeu persistant

Date : 2026-07-21  
Responsable : Communication

## Résultat

Toutes les mutations persistantes portent désormais une date UTC analysable. Les messages utilisent leur `ClientCreatedAt`; les créations de conversation, signalements et curseurs de lecture conservent `EnqueuedAtUtc` dans leur représentation versionnée.

`ChatPendingReplayPolicy` impose une fenêtre locale de rejeu de 7 jours par défaut, configurable entre 1 heure et 29 jours. Cette borne maximale reste strictement inférieure à la rétention serveur de reçus annoncée à 30 jours. Une tolérance d'horloge de 5 minutes, configurable jusqu'à une heure, empêche aussi le rejeu d'une opération datée anormalement dans le futur.

Avant toute nouvelle tentative, les quatre chemins vérifient l'âge du reçu. Une opération hors fenêtre retourne `RemoteChatError.LocalOperationExpired`, code `local_operation_expired`, HTTP 0. Elle reste intacte dans son journal pour décision locale/quarantaine et ne produit aucun appel réseau. Le diagnostic contient uniquement l'opération et la catégorie d'erreur.

Cette politique évite qu'un client resté hors ligne au-delà de la rétention serveur rejoue une mutation dont le reçu d'idempotence aurait déjà été purgé, ce qui pourrait autrement recréer un effet accepté auparavant.

## Validation

- Compilation isolée Communication : réussie, sans erreur ni avertissement.
- Suite ciblée : 78/78 réussie.
- Les quatre types datés de 20 jours sous une fenêtre de 7 jours retournent `LocalOperationExpired`.
- Zéro appel REST et quatre journaux toujours présents après refus.
- Aucun corps, identifiant ou catégorie sensible dans les diagnostics.
- Les fenêtres inférieures à une heure, égales à 30 jours et les tolérances supérieures à une heure sont rejetées.
- Aucun déploiement, activation ni synchronisation effectué.

## Inventaire serveur reçu

L'autorité des candidats est désormais `Server/artifacts/candidates/CANDIDATE-STATUS.json`. Les candidats `20260721T170156Z` et `20260721T170435Z` sont révoqués et ne doivent jamais être promus ou transférés. Le seul candidat courant local est `Server/artifacts/candidates/BeeKingdom.Server.20260721T170747Z` : 54 fichiers, build 0/0, chat 20/20, configuration/persistence/migrations 21/21, smoke local 5092 vert, sans Development/PDB/secrets et `DeploymentAuthorized=false`. Le générateur révoque automatiquement l'ancien courant.

## Directive d'intégration

La rétention des reçus serveur et la fenêtre de rejeu client doivent être traitées comme un contrat conjoint. Ne jamais réduire la rétention serveur à 29 jours ou moins sans abaisser d'abord la limite maximale du client et publier une compatibilité de protocole. En staging, injecter les quatre opérations à 28 jours puis à 30 jours : à 28 jours sous politique 29, les reçus serveur doivent encore garantir l'idempotence; à 30 jours le client doit refuser avant HTTP. L'interface doit présenter une opération expirée comme nécessitant une décision, jamais comme envoyée ou supprimée. Aucun candidat ne doit être transféré tant que `DeploymentAuthorized=false`.
