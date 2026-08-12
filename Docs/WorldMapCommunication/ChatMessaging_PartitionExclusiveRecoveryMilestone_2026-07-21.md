# Chat et messagerie — récupération exclusive par partition

Date : 2026-07-21  
Responsable : Communication

## Résultat

La composition distante crée maintenant une unique `ChatPersistenceGate` par partition joueur. Cette porte est partagée par les quatre journaux et par `ChatPendingPartitionRecovery`.

Chaque chargement, sauvegarde ou acquittement conserve la porte pendant toute sa transaction locale, y compris le cycle lecture-modification-écriture. Une quarantaine ou une restauration conserve la même porte pendant toute l'opération multi-clés : lecture des sources, copies, vérifications et suppressions. Une écriture concurrente ne peut donc plus s'insérer entre la vérification d'une copie et la remise à zéro de la partition.

La porte respecte l'annulation pour les opérations asynchrones et libère son sémaphore une seule fois. Les appels de récupération synchrones restent réservés à une action locale explicite; ils ne doivent pas être exécutés sur la boucle d'affichage Unity.

## Validation

- Compilation isolée Communication : réussie, sans erreur ni avertissement.
- Suite ciblée : 76/76 réussie.
- Un support de test bloque la copie de quarantaine alors que la porte est détenue.
- Une sauvegarde lancée en parallèle reste bloquée et ne modifie aucune clé pendant ce délai.
- Après libération, la quarantaine conserve l'ancien journal et la sauvegarde produit uniquement le nouveau journal actif.
- Aucun entrelacement ni perte observé; les 75 essais précédents restent verts.
- Aucun déploiement, activation ni synchronisation effectué.

## État du candidat serveur

Le candidat historique `20260721T170156Z` demeure révoqué. Le candidat durci `20260721T170435Z` reste la seule référence locale : base fail-closed, sans configuration Development, sans PDB ni motif de secret, build 0/0, tests 20/20, smoke publié vert et `DeploymentAuthorized=false`.

## Directive d'intégration

En Android staging, bloquer artificiellement une écriture du stockage sécurisé pendant une quarantaine, déclencher simultanément un nouvel envoi et vérifier qu'il attend sans appel HTTP. Après libération, l'ancien journal doit être uniquement en quarantaine et le nouvel envoi uniquement dans la file active; un drainage doit produire chaque reçu une seule fois. La récupération doit être exécutée hors du thread d'affichage. Aucun changement serveur ne doit permettre de contourner cette exclusion locale et aucun artefact ne doit être transféré tant que `DeploymentAuthorized=false`.
