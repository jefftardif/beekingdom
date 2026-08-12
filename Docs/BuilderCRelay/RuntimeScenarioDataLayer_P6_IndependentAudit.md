# Runtime Scenario Data Layer P6 - Independent Audit

Date locale: 2026-07-15

## Portee

Audit technique independant Builder-C Relay contre:

- `Docs/BuilderCRelay/WorldMapScenarioDataLayer_TechnicalContract.md`
- `Docs/WorldMapRuntimeEntitiesWave1/RuntimeScenarioDataLayer_Report.md`
- `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/RuntimeScenarioDataLayerProof/RuntimeScenarioDataLayerProofReceipt.md`

Contraintes respectees:

- Aucun fichier Unity modifie.
- Aucun PNG modifie.
- Aucun APK modifie.
- Aucun terrain, master terrain, BearDen source, serveur, remote ou donnee reelle modifie.

## Synthese preuves lues

Rapport P6 principal:

- Scene cible: `Assets/Scenes/WorldMapMmoFullscreenFoundation.unity`.
- Portee: couche donnees locale-demo versionnee et scenarios configurables World Map.
- Compilation Unity batchmode: PASS, zero erreur.
- Play Mode P6: PASS.
- Provider: `local_demo`.
- Data version: `world_map_scenario_data_v1`.
- Records/hives/resources/bestiary/events: `62/12/39/10/1`.
- Serveur/remote: ABSENT.
- `official_gain=false`.
- APK rebuild: ABSENT.
- Terrain 50x50 genere: ABSENT.

Recu P6:

- Play Mode: PASS.
- Stable entity ids: PASS.
- Normalized coordinates: PASS.
- 25x25 to 50x50 reprojection: PASS.
- Local authority adapter: PASS.
- Scenario presets: PASS.
- Player/enemy test hives editable: PASS.
- Legacy P1-P5 regression: PASS.
- `SERVER_OR_OFFICIAL_GAIN=NO`.
- `LEGACY_DEMO_REGRESSION=NO`.
- `READY_FOR_OWNER_CONFIGURABLE_SCENARIO_TEST=YES`.

## Audit schema, versions, IDs, coordonnees

Contrat attendu:

- `schema_version`
- `world_id`
- `world_grid_version`
- `authority_version`
- `official`
- `source_kind`
- `entity_id`
- `entity_family`
- `entity_type`
- `chunk_id_logical`
- `world_coord_normalized`
- `tier_or_level`
- `variant`
- `spawn_state`
- `spawn_seed_version`

Preuve observee:

- Le rapport declare le modele local versionne `world_map_scenario_data_v1`.
- Les familles `hive`, `resource`, `bestiary`, `event` sont presentes.
- Les champs contractuels principaux sont listes dans le modele runtime.
- Le recu declare `Stable entity ids: PASS`.
- Le recu declare `Normalized coordinates: PASS`.

Verdict:

- `P6_SCHEMA_VERSION_IDS_COORDS=PASS`

Note:

- L'audit se base sur le rapport et le recu. Il n'a pas reexecute de test negatif sur fichier/runtime.

## Audit provider local

Contrat attendu:

- Provider local-demo separe d'une future autorite serveur.
- `LocalDemo` et `SeedPreview` doivent rester `official=false`.
- Aucun serveur/remote/donnee reelle.
- Aucun gain officiel calcule par client.

Preuve observee:

- Rapport: Provider local-demo `local_demo`.
- Rapport: `server=false`.
- Rapport: `official_gain=false`.
- Rapport: aucune connexion, remote ou donnee reelle.
- Recu: `Provider: local_demo`.
- Recu: `Server/remote: ABSENT`.
- Recu: `official_gain: false`.
- Gate: `SERVER_OR_OFFICIAL_GAIN=NO`.

Verdict:

- `P6_LOCAL_PROVIDER_AUTHORITY=PASS`

## Audit presets

Contrat attendu:

- Scenarios Collecte, Duel, Raid exposes en preview locale.
- Preview/receipt et autorite officielle doivent rester separes.
- Les ruches test configurables doivent rester disponibles.

Preuve observee:

- Rapport: presets `Collecte R3`, `Duel ruches`, `Raid T7`.
- Rapport: `PLAYER_TEST_HIVE / ENEMY_TEST_HIVE` editables.
- Recu: `Scenario presets: PASS`.
- Recu: `Player/enemy test hives editable: PASS`.

Verdict:

- `P6_SCENARIO_PRESETS=PASS`

Note:

- Le rapport prouve l'existence et le gate des presets. Il ne donne pas le detail complet de chaque delta scenario dans le recu.

## Audit reprojection

Contrat attendu:

- Reprojection 25x25 -> 50x50 depuis `world_coord_normalized`.
- Aucun terrain 50x50 genere.
- Coordonnees hors bornes refusees ou bloquees par gate.

Preuve observee:

- Rapport: reprojection logique 25x25 vers 50x50 depuis `world_coord_normalized`, sans terrain 50x50.
- Rapport: test negatif "Reprojection hors 50x50" refuse par `Reprojection50x50Pass`.
- Recu: `25x25 to 50x50 reprojection: PASS`.
- Recu: `50x50 terrain generation: ABSENT`.

Verdict:

- `P6_REPROJECTION_25X25_50X50=PASS`

## Audit budgets et regression

Contrat attendu:

- Fenetre active et caches ne regressent pas les acquis P1-P5.
- Aucun chargement terrain 50x50.
- Aucun APK rebuild.
- Pas de mutation terrain/PNG/BearDen.

Preuve observee:

- Rapport: Wave5 25x25, 625 tuiles, BearDen, LAB LOCAL, P1-P5 preserves.
- Rapport: Regression demo P1-P5: PASS.
- Recu: `Legacy P1-P5 regression: PASS`.
- Rapport/recu: APK rebuild ABSENT.
- Rapport/recu: terrain 50x50 genere ABSENT.

Verdict:

- `P6_BUDGETS_AND_REGRESSION=PASS_WITH_NOTES`

Notes:

- Les preuves lues confirment la non-regression P1-P5 et l'absence de terrain/APK.
- Le recu P6 ne repete pas explicitement les chiffres de budgets 25 chunks / 96 textures / allocations. Ces budgets restent couverts par les preuves P1/50x50 anterieures et le contrat Builder-C, mais pas redetailles dans le recu P6.

## Audit absence d'autorite officielle client

Contrat attendu:

- Le client ne calcule jamais l'etat officiel.
- `TrySubmitOfficial` ou equivalent doit refuser sans autorite serveur.
- Les recompenses locales doivent rester `official_gain=false`.

Preuve observee:

- Rapport: aucun serveur, remote, donnee reelle, gain officiel.
- Rapport: `official=false`, `source_kind=local_demo`.
- Rapport: autorite non locale/officielle refusee par `LocalAuthorityAdapterPass`.
- Recu: `Local authority adapter: PASS`.
- Recu: `Server/remote: ABSENT`.
- Recu: `official_gain: false`.
- Gate: `SERVER_OR_OFFICIAL_GAIN=NO`.

Verdict:

- `P6_NO_CLIENT_OFFICIAL_AUTHORITY=PASS`

## Reserves techniques

Reserves non bloquantes:

- L'audit est documentaire et independant, mais il ne relance pas Unity ni les tests batch.
- Le recu confirme les gates globaux; il ne detaille pas les donnees completes de chaque record.
- Les budgets chiffrés de performance ne sont pas recopies dans le recu P6, meme si la regression P1-P5 est PASS et que les preuves 50x50 precedentes couvraient ces budgets.

## P7 watch status

Recherche immediate apres audit P6:

- `SpawnInspectorIntegration_Report.md`: non trouve dans `Docs`.
- Recu `SpawnInspector`: non trouve dans `Docs`.
- Documents P7 presents: contrat Builder-C P7, UI spec P7, QA matrix P7, demo plan P7.

Statut:

- `P7_SPAWN_INSPECTOR_REPORT_FOUND=NO`
- `P7_SPAWN_INSPECTOR_AUDIT_STATUS=PENDING_EVIDENCE`

Des que `SpawnInspectorIntegration_Report.md` et son recu apparaissent, l'audit P7 doit etre produit contre `Docs/BuilderCRelay/WorldMapSpawnDistribution_TechnicalContract.md`.

## Gates

- `P6_SCHEMA_VERSION_IDS_COORDS=PASS`
- `P6_LOCAL_PROVIDER_AUTHORITY=PASS`
- `P6_SCENARIO_PRESETS=PASS`
- `P6_REPROJECTION_25X25_50X50=PASS`
- `P6_BUDGETS_AND_REGRESSION=PASS_WITH_NOTES`
- `P6_NO_CLIENT_OFFICIAL_AUTHORITY=PASS`

BUILDER_C_P6_AUDIT=PASS_WITH_NOTES

READY_FOR_P7=YES

Le `PASS_WITH_NOTES` valide la consommation P7, avec reserve documentaire: l'audit n'a pas relance Unity et le recu P6 ne redetaille pas les budgets chiffres deja couverts par les preuves 50x50 precedentes.
