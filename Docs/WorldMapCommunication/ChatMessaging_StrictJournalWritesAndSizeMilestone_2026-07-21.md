# Chat et messagerie — écritures strictes et taille bornée des journaux

Date : 2026-07-21  
Responsable : Communication

## Résultat

Les mêmes invariants qui protègent la restauration sont maintenant appliqués avant chaque écriture. Aucun message sans conversation, corps, reçu ou date analysable; aucune création avec participant vide; aucun signalement incomplet; aucun curseur ou compteur négatif ne peut produire une nouvelle image persistée.

`ChatPendingJournalPolicy` borne aussi la représentation JSON de chaque journal. La valeur par défaut est 1 048 576 caractères, configurable entre 1 024 et 8 388 608 par `RemoteChatClientOptions.MaxPendingSerializedCharactersPerJournal`. La taille est vérifiée avant analyse d'une valeur restaurée et avant remplacement de la valeur existante.

Une nouvelle image trop grande produit `ChatPendingJournalSizeException`, normalisée par le fournisseur en `LocalQueueFull`; l'ancienne valeur n'est pas remplacée et aucun appel réseau n'est lancé. Une valeur déjà présente qui dépasse la politique est traitée comme non conforme, conservée exactement et exposée comme `LocalStorageUnavailable` pour permettre sa quarantaine.

## Validation

- Compilation isolée Communication : réussie, sans erreur ni avertissement.
- Suite ciblée : 75/75 réussie.
- Les quatre types d'entrée invalides sont refusés avant toute écriture.
- Un message de 2 000 caractères sous une politique de 1 024 ne produit aucune valeur locale ni aucun appel REST et retourne `LocalQueueFull`.
- Une valeur restaurée de 1 025 caractères reste strictement inchangée et est refusée avant analyse JSON.
- Les limites de configuration hors plage sont rejetées.
- Aucun déploiement, activation ni synchronisation effectué.

## Candidat serveur reçu

L'Intégrateur a réellement publié et vérifié un candidat local via `Server/tools/New-ProductionCandidateLocal.ps1` : contrôle configuration, build Release, 20 tests chat, publication, inspection de configuration embarquée, smoke depuis la DLL publiée et manifeste SHA-256.

L'inspection a ensuite révélé que le premier candidat `BeeKingdom.Server.20260721T170156Z` embarquait `appsettings.Development.json`, des PDB et une base permissive. Il est explicitement historique et inutilisable. La base est maintenant fail-closed (SQL vide et Ops requis), les exceptions restent exclusivement dans la source Development, Development est exclu de la publication, les PDB sont interdits et un scan JSON/config refuse `Password`, `User Id` et `Bearer`.

Le seul candidat de référence est désormais `Server/artifacts/candidates/BeeKingdom.Server.20260721T170435Z` : 54 fichiers avant manifeste, build 0/0, tests 20/20 et smoke publié vert sur le port local 5090. Il porte toujours explicitement `DeploymentAuthorized=false`. Aucun transfert n'a eu lieu.

## Directive d'intégration

Le candidat local ne doit pas être transféré tant que les portes restent fermées. Ajouter aux validations Android/staging : tentative d'écriture dépassant la limite locale = zéro HTTP et ancienne valeur inchangée; journal restauré surdimensionné = `LocalStorageUnavailable` et quarantaine possible. Les limites serveur de corps restent indépendantes et doivent continuer à provenir des capabilities négociées; la borne de journal ne doit jamais servir à accepter un message dépassant `bodyMaxCharacters`.
