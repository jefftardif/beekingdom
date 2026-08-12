# Runtime Scenario Data Layer - P6 QA Verdict

Date locale: 2026-07-15

Role: QA-Relay documentaire, sans execution Unity.

## Sources validees

- Rapport P6 principal: `Docs/WorldMapRuntimeEntitiesWave1/RuntimeScenarioDataLayer_Report.md`
- Recu P6: `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/RuntimeScenarioDataLayerProof/RuntimeScenarioDataLayerProofReceipt.md`
- Matrice QA P6: `Docs/QARelay/WorldMapScenarioDataLayer_QA_Matrix.md`
- Regression P1-P5: `Docs/WorldMapRuntimeEntitiesWave1/WorldMap50x50Readiness_ConsolidatedDemoReport.md`

## Exclusions confirmees

| Exclusion | Statut documentaire |
| --- | --- |
| Unity/scene/PNG/APK modifies par QA | ABSENT |
| Wave5 25x25 / 625 tuiles / master terrain modifies | ABSENT |
| BearDen source modifie | ABSENT |
| Terrain 50x50 genere | ABSENT |
| Serveur / remote / donnees reelles | ABSENT |
| Gain officiel / persistence officielle | ABSENT |

## Gates positives

| Gate QA P6 | Preuve documentaire | Verdict |
| --- | --- | --- |
| Entity IDs uniques/stables | `STABLE_ENTITY_IDS=PASS`; recu `Stable entity ids: PASS` | PASS |
| Coordonnees normalisees | `NORMALIZED_COORDINATES=PASS`; recu `Normalized coordinates: PASS` | PASS |
| Reprojection 25x25 -> 50x50 | Rapport/recu `25x25 to 50x50 reprojection: PASS`; terrain 50x50 absent | PASS |
| Serialization/version locale | `Data version: world_map_scenario_data_v1`; modele `schema_version/world_id/world_grid_version/authority_version` | PASS |
| Provider local | Provider `local_demo`; `server=false`; `official_gain=false`; `official=false` | PASS |
| Presets Collecte R3 / Duel / Raid T7 | Rapport: presets `Collecte R3`, `Duel ruches`, `Raid T7`; recu `Scenario presets: PASS` | PASS |
| Deux ruches test editables | `PLAYER_TEST_HIVE / ENEMY_TEST_HIVE editables: PASS`; recu PASS | PASS |
| Filtres/Proche/legende/BearDen/pan-zoom | Couverts par regression P1-P5 consolidee et preservee | PASS |
| Budgets 50x50 | P1-P5 consolide: catalogue 2500; chunks 25/9/9/25; budgets cache/terrain/allocation PASS | PASS |
| Regression P1-P5 | Rapport P6: `Regression demo P1-P5: PASS`; recu `Legacy P1-P5 regression: PASS` | PASS |

## Tests negatifs documentes/observables

| Test negatif | Preuve documentaire | Verdict |
| --- | --- | --- |
| ID vide/duplique | Rapport P6: refuse par `StableIdsPass` | PASS |
| Coordonnees hors [0,1] | Rapport P6: refuse par `NormalizedCoordinatesPass` | PASS |
| Reprojection hors 50x50 | Rapport P6: refuse par `Reprojection50x50Pass` | PASS |
| Autorite non locale/officielle | Rapport P6: refuse par `LocalAuthorityAdapterPass` | PASS |
| Scenario absent/non applicable | Rapport P6: refuse par `ScenarioPresetsPass` | PASS |
| Regression P1-P5 | Rapport P6: refuse par `LegacyDemoRegressionNo` | PASS |
| Classe invalide explicite | Non nommee explicitement dans le rapport/recu P6 | NOTE |
| Quantite negative explicite | Non nommee explicitement dans le rapport/recu P6 | NOTE |
| T7 lance en solo explicite | Non nomme explicitement dans le rapport/recu P6 | NOTE |

## Autorite locale

- Rapport P6: Provider `local_demo`.
- Rapport P6: `server=false`.
- Rapport P6: `official_gain=false`.
- Recu P6: `Server/remote: ABSENT`.
- Recu P6: `official_gain: false`.
- Gate P6: `SERVER_OR_OFFICIAL_GAIN=NO`.

Verdict: PASS.

## Non-regression P1-P5

- P1 50x50 readiness sans art 50x50: PASS.
- P2 outils de lecture carte: PASS.
- P3 polish interactions: PASS.
- P4 regression automatique locale Play Mode: PASS.
- P5 package demo owner: PASS.
- Wave5 terrain regression: NO.
- BearDen regression: NO.
- APK rebuild: NO.
- Terrain 50x50 genere: NO.

Verdict: PASS.

## Notes QA

- Les gates positives P6 et le recu P6 sont coherents.
- Les tests negatifs documentes sont des refus controles, pas des crashes.
- Trois cas negatifs de la matrice QA initiale ne sont pas nommes explicitement dans le rapport/recu: classe invalide, quantite negative, T7 solo. Ils peuvent etre couverts indirectement par les gates de scenario/validation, mais QA-Relay ne les declare pas explicitement prouves.
- Cette note ne bloque pas la validation owner configurable scenario test, car les gates P6 publiees sont PASS et aucun risque serveur/officiel/regression n'est observe dans les documents.

## Gates

QA_P6=PASS_WITH_NOTES

READY_FOR_OWNER_CONFIGURABLE_SCENARIO_TEST_QA=YES

## Surveillance P7

Statut au moment de ce verdict:

- Rapport P7 principal: NON TROUVE dans `Docs/WorldMapRuntimeEntitiesWave1`.
- Matrice de validation P7 prete: `Docs/QARelay/WorldMapSpawnDistribution_QA_Matrix.md`.
- Validation P7: PENDING_MAIN_REPORT.
