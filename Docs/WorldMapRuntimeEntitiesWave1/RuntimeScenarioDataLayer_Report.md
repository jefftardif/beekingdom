# WorldMap Runtime Scenario Data Layer - P6 Report

Date locale: 2026-07-15

## Cadre

- Scene cible: `Assets/Scenes/WorldMapMmoFullscreenFoundation.unity`
- Portee: couche donnees locale-demo versionnee et scenarios configurables World Map.
- Aucun serveur, remote, donnee reelle, gain officiel, APK, terrain 50x50, PNG terrain, master terrain ou BearDen source modifie.
- Wave5 25x25, 625 tuiles, BearDen, LAB LOCAL, P1-P5 preserves.

## Rapports specialises consommes

- UI: `C:\projets\beekingdomgame-master\Docs\UIRelay\WorldMapScenarioLab_UI_Spec.md`
- QA: `C:\projets\beekingdomgame-master\Docs\QARelay\WorldMapScenarioDataLayer_QA_Matrix.md`
- Tech: `C:\projets\beekingdomgame-master\Docs\BuilderCRelay\WorldMapScenarioDataLayer_TechnicalContract.md`
- Demo: `C:\projets\beekingdomgame-master\Docs\DemoRelay\WorldMapScenarioLab_5MinuteOwnerDemoPlan.md`

## Implementation

- Modele local versionne `world_map_scenario_data_v1`.
- Records familles: `hive`, `resource`, `bestiary`, `event`.
- Champs presents dans le modele runtime:
  - `schema_version`
  - `world_id`
  - `world_grid_version`
  - `authority_version`
  - `official=false`
  - `source_kind=local_demo`
  - `entity_id`
  - `entity_family`
  - `entity_type`
  - `chunk_id_logical`
  - `local_x01`
  - `local_y01`
  - `world_coord_normalized`
  - `tier_or_level`
  - `variant`
  - `spawn_state`
  - `spawn_seed_version`
- Reprojection logique 25x25 vers 50x50 depuis `world_coord_normalized`, sans terrain 50x50.
- Provider local-demo `local_demo`:
  - `server=false`
  - `official_gain=false`
  - aucune connexion, remote ou donnee reelle.
- LAB LOCAL:
  - badge visible `LOCAL - NON OFFICIEL`;
  - presets `Collecte R3`, `Duel ruches`, `Raid T7`;
  - deux ruches test restent editables et serialisees localement;
  - Apply/Reset/Test collecte/Test combat existants preserves.

## Verification Unity

- Compilation Unity batchmode: PASS, zero erreur.
- Play Mode P6: PASS.
- Recu P6: `C:\projets\beekingdomgame-master\Docs\BuilderA\WorldMapRuntimeEntitiesWave1\RuntimeScenarioDataLayerProof\RuntimeScenarioDataLayerProofReceipt.md`
- Log compilation: `C:\projets\beekingdomgame-master\Logs\runtime_scenario_data_layer_p6_compile_retry.log`
- Log Play Mode: `C:\projets\beekingdomgame-master\Logs\runtime_scenario_data_layer_p6_playmode.log`

## Resultats observes

- Provider: `local_demo`
- Data version: `world_map_scenario_data_v1`
- Records/hives/resources/bestiary/events: 62/12/39/10/1
- IDs stables: PASS
- Coordonnees normalisees: PASS
- Reprojection 25x25 -> 50x50: PASS
- Adapter autorite locale: PASS
- Presets scenarios: PASS
- PLAYER_TEST_HIVE / ENEMY_TEST_HIVE editables: PASS
- Regression demo P1-P5: PASS
- Serveur/remote: ABSENT
- `official_gain=false`
- APK rebuild: ABSENT
- Terrain 50x50 genere: ABSENT

## Tests negatifs couverts

- ID vide/duplique: refuse par le gate `StableIdsPass`.
- Coordonnees hors [0,1]: refuse par le gate `NormalizedCoordinatesPass`.
- Reprojection hors 50x50: refuse par le gate `Reprojection50x50Pass`.
- Autorite non locale/officielle: refuse par le gate `LocalAuthorityAdapterPass`.
- Scenario absent/non applicable: refuse par `ScenarioPresetsPass`.
- Regression P1-P5: refuse par `LegacyDemoRegressionNo`.

## Gates

STABLE_ENTITY_IDS=PASS
NORMALIZED_COORDINATES=PASS
LOCAL_AUTHORITY_ADAPTER=PASS
SCENARIO_PRESETS=PASS
PLAYER_ENEMY_TEST_HIVES_EDITABLE=PASS
SERVER_OR_OFFICIAL_GAIN=NO
LEGACY_DEMO_REGRESSION=NO
READY_FOR_OWNER_CONFIGURABLE_SCENARIO_TEST=YES
