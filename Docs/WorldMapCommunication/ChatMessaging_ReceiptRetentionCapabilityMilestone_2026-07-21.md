# Chat et messagerie — rétention des reçus négociée

Date : 2026-07-21  
Responsable : Communication

## Résultat

Le contrat `RemoteCapabilities` contient maintenant `idempotencyReceiptRetentionDays`, décodé depuis le JSON camelCase du serveur. Pour un serveur actif, cette valeur est obligatoire et doit être au moins 2. Une valeur absente, nulle ou égale à 1 produit une décision incompatible `receipt_retention_invalid` avant acquisition de session, drainage ou mutation.

Après négociation, le fournisseur calcule sa fenêtre effective comme le minimum entre la politique locale et la rétention annoncée moins un jour complet de marge. `RemoteCapabilityDecision.EffectiveReplayMaxAgeDays` expose le résultat à la composition et à l'interface.

Exemple validé : politique locale de 29 jours et rétention serveur de 8 jours donnent une fenêtre effective de 7 jours. Une opération vieille de 8 jours est alors conservée localement avec `LocalOperationExpired` et zéro nouvel appel réseau.

Le serveur désactivé reste capable d'annoncer ses capabilities de préparation sans être rejeté sur ce champ : le contrôle devient obligatoire seulement lorsque `server=true`.

## Validation

- Compilation isolée Communication : réussie, sans erreur ni avertissement.
- Suite ciblée : 80/80 réussie.
- Codec Unity : `idempotencyReceiptRetentionDays: 30` devient 30.
- Serveur actif sans valeur : incompatibilité avant session.
- Rétention 8 jours + politique 29 : fenêtre effective 7, opération de 8 jours refusée, reçu local conservé, zéro requête de mutation.
- Les validations précédentes d'expiration, protection et drainage restent vertes.
- Aucun déploiement, activation ni synchronisation effectué.

## Inventaire candidat

`Server/artifacts/candidates/CANDIDATE-STATUS.json` reste l'autorité. `20260721T170156Z` et `20260721T170435Z` sont révoqués. `20260721T170747Z` est le seul candidat local courant au moment de ce rapport, mais il précède ce nouveau champ de contrat et doit donc être reconstruit après adaptation serveur. Il reste `DeploymentAuthorized=false` et ne doit pas être transféré.

## Directive d'intégration

Ajouter `idempotencyReceiptRetentionDays` à la réponse camelCase de `/chat/v1/capabilities`, avec la valeur effective réellement appliquée par le job de purge, pas seulement la valeur demandée en configuration. Les tests doivent prouver cohérence entre options validées, capabilities et purge pour les fournisseurs mémoire et SQL. Une rétention inférieure à 2 doit empêcher le démarrage actif. Mettre à jour le contrat JSON complet et reconstruire un nouveau candidat local; révoquer automatiquement `170747Z`. Garder `server=false`, `realtime=false`, `PreparationOnly` et `DeploymentAuthorized=false` jusqu'aux validations staging autorisées.
