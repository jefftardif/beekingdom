# M097 — Training / Healing / tiers

## Cause racine
Les validations client Training/Healing et la migration serveur Healing exigeaient encore la durée initiale après accélération. Les timers expirés acceptaient un SpeedUp inutile ; le rejeu idempotent renvoyait un succès sans réponse/reçu, et le snapshot SpeedUps indiquait toujours « running ».

## Fichiers changés
- `Assets/BeeKingdom/Networking/HiveDoctrineRecruitmentClient.cs`, `HiveBroodVitalityClient.cs`.
- `Assets/BeeKingdom/Tests/Editor/HiveDoctrineRecruitmentClientTests.cs`, `HiveBroodVitalityClientTests.cs`.
- `Server/src/BeeKingdom.HiveOperations/HiveStateMigrator.cs`, `OperationTimerReduction.cs`, `SpeedUpContracts.cs`.
- `Server/tests/BeeKingdom.HiveOperations.Tests/TroopsTimersRegressionTests.cs` et ce rapport.

## Implémentation
Durées accélérées admises entre zéro et la durée catalogue ; refus des accélérations Training/Healing déjà terminées, reçu original restitué au rejeu avec snapshot courant et statut de fin correct. Aucun changement d’équilibrage : T1 ×1, T2 ×1,25, T3 ×1,50, bonus limité à la famille promue ; promotions immédiates, sans timer ni SpeedUp dédié.

## Tests / compilation
93 tests réussis : 53 cœur serveur, 9 HTTP/adaptateurs, 31 clients NUnit via harnais .NET (sources réelles, références Unity 6000.5.3f1). Les 17 nouvelles régressions serveur couvrent lancement, coûts, refus anticipé/occupé, échéance naturelle, accélérations partielles/totales/spécifiques/universelles, reconstruction du dépôt JSON, completion/rejeu et promotions persistées jusqu’au plafond ; 8 échecs initiaux puis 7 échecs de réponse de rejeu reproduits avant correction.

Compilation serveur et client .NET réussie, avertissements préexistants ; revue ciblée et `git diff --check` sans anomalie. Preuves locales non commitées : `TestResults/M097/` (`M097-core.trx`, `M097-http.trx`, `M097-client.trx`, `M097-final-regressions.trx`, harnais `ClientRegression.csproj`).

## Points ouverts
Unity EditMode/assemblages jeu-éditeur complets non certifiés : deux lancements arrêtés avant compilation, IPC Unity Package Manager indisponible (code 1) ; tests de présentation et validation visuelle non exécutés. Synchronisation CIGM indisponible : aucun outil mémoire exposé dans cette session.

## Acceptation
- [x] Training/Healing : lancement, timers, completion et persistance/rechargement vérifiés automatiquement.
- [x] Contrats HTTP, snapshots accélérés, cache client hors ligne et reprise d’authentification vérifiés.
- [x] SpeedUps : consommation unique, rejeu exploitable et refus après expiration.
- [x] Promotions : débits uniques, persistance, plafond T3 et +25 points de pourcentage par tier.
- [x] Corrections ciblées ; World Map marches et Chat Royal inchangés.
- [x] Travail limité au worktree ; aucun push ni merge main.
- [ ] Compilation complète et tests Unity : bloqués par l’environnement.
