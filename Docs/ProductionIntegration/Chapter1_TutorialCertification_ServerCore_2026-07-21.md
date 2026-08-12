# Certification serveur du chapitre 1 — noyau fermé

## État

Le noyau de machine d’état est implémenté sans crédit économique. Les étapes
autorisées et strictement ordonnées sont `tutorial_intro_acknowledged`,
`hive_surface_acknowledged`, puis `tutorial_sequence_completed`. Chaque
commande exige joueur/ruche, `expectedRevision` et une clé d’idempotence. Aucun
horodatage client n’est autoritaire.

Un rejeu avec même clé et même empreinte retourne le reçu existant. Une clé
réutilisée avec une autre charge est rejetée par le mécanisme de reçu. Un saut,
un retour arrière ou une révision obsolète est refusé. La dernière étape produit
une preuve opaque aléatoire et conserve `InstallationComplete=false`. Cette
certification tutorielle ne prouve ni économie ni timers; un futur prédicat
séparé devra établir `AuthoritativeEconomyComplete` et
`AuthoritativeTimedOperationsComplete` avant toute installation complète.

Le modèle persistant passe de v4 à v5 avec `Chapter1CertificationState`.
`HiveStateMigrator` et DurableJson conservent les états antérieurs sans activer
la certification. Aucun événement économique, timer ou action de construction
n’est prétendu certifié par ce noyau; ces contrats autoritaires restent à
définir.

## Portée volontairement fermée

La route HTTP dédiée et son feature flag restent à ajouter dans une suite
`Game/Tutorial` séparée. Tant que cette route n’existe pas, aucune surface de
production ne peut déclencher la certification. La dotation fondatrice reste
derrière `FoundingFoundation:Enabled=false`.

## Fichiers modifiés

- `Server/src/BeeKingdom.HiveOperations/HiveOperationModels.cs`
- `Server/src/BeeKingdom.HiveOperations/HiveOperationService.cs`
- `Server/src/BeeKingdom.HiveOperations/HiveStateMigrator.cs`
- `Docs/ProductionIntegration/Chapter1_TutorialCertification_ServerCore_2026-07-21.md`

Tests HiveOperations : 16/16, incluant preuve finale non vide de 32 caractères
hexadécimaux et valeurs de ressources non triviales inchangées. Suite serveur
complète : 253 réussis, 7 tests SQL ignorés, 260 total. Build Release de
`Server/BeeKingdom.Server.slnx` : 0 erreur, 2 avertissements existants
Microsoft.Data.SqlClient. Les preuves incluent rejeu final immédiat avec même preuve,
même date UTC et même révision, saut/retour arrière, clé neuve sur révision
obsolète, conflit de charge, échecs idempotents et migration explicite v4 avec
`InstallationComplete=true` neutralisée. Compilation du projet HiveOperations : 0 erreur,
0 avertissement. Aucun
candidat n’a été construit et aucun déploiement/activation/synchronisation
n’a été effectué.
Le rejeu d’un reçu après progression ultérieure renverrait l’état courant du
repository ; la preuve d’égalité de révision est donc volontairement limitée au
rejeu immédiat, qui est la sémantique actuelle du noyau.
