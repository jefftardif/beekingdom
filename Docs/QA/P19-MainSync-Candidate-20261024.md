# BeeKingdom — candidat de fusion main / pollinisation (24 octobre 2026)

## Périmètre et sécurité

Branche Git : `integration/pollination-main-sync-20261024`.
Fusion propre de `origin/main` et `origin/integration/pollinisation-unified-20261023` dans un worktree distinct (`C:\BeeKingdom\staging\BK-PollinationMainSync-20261024`), sans modifier le worktree `C:\projets\beekingdomgame-master` du créateur.

La fusion était sans conflit textuel, mais elle **perdait neuf fichiers essentiels au mode P18 lecture seule**, présents dans la source homologuée `pollinisation-main-snapshot-20261023`. Les neuf fichiers serveur/tests ont été repris avec égalité SHA-256 après copie dans le candidat :
- `CivilPopulationService.cs`, `FloralAllianceProjectModels.cs`, `SqlFloralAllianceProjectService.cs`, `DurableJsonHiveStateRepository.cs` et `Program.cs`.
- Tests `CivilPopulationServiceTests.cs`, `FloralAllianceProjectRulesTests.cs`, `CivilPopulationEndpointTests.cs`, `FloralAllianceProjectEndpointTests.cs`.

Protection vérifiée : en `CivilPopulation.ReadOnlyPilot=true`, une lecture ne modifie ni portefeuille ni révision ni population ; création de cohorte `503`. Un projet floral est lisible seulement selon `CanRead` et ses mutations restent `503` en `ReadOnlyPilot` (`CanCreate=false`). Le correctif `FileShare.Delete` empêche une régression du renouvellement atomique des fichiers JSON Windows.

## Résultats exécutés dans ce candidat

- Suite métier complète `BeeKingdom.HiveOperations.Tests` : **330 réussis / 330**, aucun échec.
- Suite API complète `BeeKingdom.Tests` : **716 réussis, 9 ignorés / 725**, aucun échec.
- Filtres ciblés civil/floral : **40 métier**, **22 API**, sans échec.
- `pollination-unity-client-qa` : `P5_PRESENTATION_QA_PASS`.
- SQL QA isolée `(localdb)\BKPollinationQA` : `SQL_QA_PASS`, stock floral partagé par deux ruches, transactions concurrentes, rollback forcé, idempotence et règlement automatique au retour, `SQL_QA_CLEANUP=COMPLETE`.
- SQL QA collective isolée : `P4_SQL_QA_PASS` (anti-solo, deux membres distincts, niveau d'alliance crédité une fois, changement d'alliance sans attribution rétroactive), `P4_SQL_QA_CLEANUP=COMPLETE`.
- Les sept fichiers principaux de l'interface de pollinisation du candidat ont été comparés par SHA-256 avec ceux de la session Unity PCJEFF qui avait passé **11 tests EditMode sur 11** et affiché `[BKT] Alliance Test`.

Journaux locaux : `C:\BeeKingdom\RECOVERY\P19-merged-*-20261024.log`.

## Production SRVESDT — NON MODIFIÉE

L'API reste sur les binaires P18, HTTP 200, sauvegarde binaire trouvée. `FloralSites.Enabled=true`, `CivilPopulation.Enabled=true` avec `ReadOnlyPilot=true` ; `CivilExpeditions.Enabled=false` et `FloralAllianceProjects.Enabled=false`. Le vérificateur `Server/ops/pollination-production-readiness/Verify-P19ReadOnly.ps1` a renvoyé **`rolloutAllowed=false`** :
1. Identité distante sans droits de lecture SQL pour `BeeKingdom`.
2. Restauration de sauvegarde SQL de production non démontrée.

Le SQL `Server/ops/pollination-production-preflight/P19-DatabaseReadOnly.sql` permet une validation par un principal SQL déjà autorisé, sans modifier les données. Aucun jeton ni permission n'a été emprunté ou élevé.

## Suite de déploiement

Avant `main` officiel et tout basculement P19 : résultat SQL production autorisé (sept tables + deux index + échéances), sauvegarde SQL restaurée en test, contrôle réel du worker de règlement, passage en jeu avec deux comptes authentifiés et revue visuelle des bâtiments en cours. Préparer ensuite un déploiement binaire/configuration avec sauvegarde, retour arrière et activation progressive.

**Décision technique : candidat prêt pour intégration/QA élargie ; production P19 = NO-GO jusqu'aux preuves SQL et restauration.** Ne jamais déployer directement les DLL du candidat sur SRVESDT sans vérifier leur parité P18 et les conditions précédentes.
