# World Map Scenario Lab - 5 Minute Owner Demo Plan

Date locale: 2026-07-15

## Statut

`READY_FOR_OWNER_SCENARIO_DEMO_WHEN_P6_PASS=YES`

Raison: le rapport principal P6 et son recu local sont publies, lisibles et concluent `READY_FOR_OWNER_CONFIGURABLE_SCENARIO_TEST=YES`. Les gates P6 verifient le modele local-demo versionne, les presets scenarios, les deux ruches test editables, l'autorite locale, l'absence serveur/gain officiel, l'absence APK et l'absence de terrain 50x50 genere.

## Perimetre read-only

- Voie demo uniquement.
- Aucun changement Unity.
- Aucun changement PNG.
- Aucun APK.
- Aucun serveur.
- Aucune ancienne tache Codex lue.
- Chemin de sortie exclusif: `C:\projets\beekingdomgame-master\Docs\DemoRelay\WorldMapScenarioLab_5MinuteOwnerDemoPlan.md`

Limites a rappeler pendant la demo:

- Aucune carte terrain 50x50 generee.
- Le terrain visible reste Wave5 25x25 preserve.
- Aucune preuve device/APK.
- Aucune economie officielle.
- Toutes les interactions sont locales.
- Preuve attendue/observee: `server=false`, `official_gain=false`.

## Sources de preuve provisoires disponibles

Captures et manifeste:

- `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/FinalVisualSmokeProof/manifest.md`
- `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/FinalVisualSmokeProof/FVS_00_CENTER_LAB_HIVES.png`
- `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/FinalVisualSmokeProof/FVS_01_HIVE_PROGRESSION.png`
- `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/FinalVisualSmokeProof/FVS_02_RESOURCE_INTERACTION.png`
- `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/FinalVisualSmokeProof/FVS_03_BESTIARY_SOLO_RAID.png`
- `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/FinalVisualSmokeProof/FVS_04_BEAR_DEN_STATES.png`
- `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/FinalVisualSmokeProof/FVS_05_PAN_ZOOM_EDGE.png`

Recus:

- `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/RuntimeIntegrationProof/RuntimeEntitiesProofReceipt.md`
- `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/MapReadingToolsProof/MapReadingToolsProofReceipt.md`
- `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/WorldMap50x50ReadinessProof/WorldMap50x50ReadinessProofReceipt.md`

Chemins P6 reels valides:

- `P6_SCENARIO_LAB_REPORT`: `C:\projets\beekingdomgame-master\Docs\WorldMapRuntimeEntitiesWave1\RuntimeScenarioDataLayer_Report.md`
- `P6_SCENARIO_LAB_RECEIPT`: `C:\projets\beekingdomgame-master\Docs\BuilderA\WorldMapRuntimeEntitiesWave1\RuntimeScenarioDataLayerProof\RuntimeScenarioDataLayerProofReceipt.md`
- `P6_SCENARIO_LAB_CAPTURE_MANIFEST`: non publie dans les preuves P6 lues.
- `P6_SCENARIO_LAB_CAPTURE_DIR`: non publie dans les preuves P6 lues.

## Gates P6 verifiees

Depuis le rapport principal P6 et le recu local:

- Play Mode P6: PASS.
- Provider: `local_demo`.
- Data version: `world_map_scenario_data_v1`.
- Records/hives/resources/bestiary/events: `62/12/39/10/1`.
- `STABLE_ENTITY_IDS=PASS`.
- `NORMALIZED_COORDINATES=PASS`.
- `LOCAL_AUTHORITY_ADAPTER=PASS`.
- `SCENARIO_PRESETS=PASS`.
- `PLAYER_ENEMY_TEST_HIVES_EDITABLE=PASS`.
- `SERVER_OR_OFFICIAL_GAIN=NO`.
- `LEGACY_DEMO_REGRESSION=NO`.
- `server=false`.
- `official_gain=false`.
- Server/remote: ABSENT.
- APK rebuild: ABSENT.
- 50x50 terrain generation: ABSENT.
- `READY_FOR_OWNER_CONFIGURABLE_SCENARIO_TEST=YES`.

## Ordre de demonstration - 5 minutes

### 0:00-0:30 - Ouverture WorldMap Wave5

Montrer: `FVS_00_CENTER_LAB_HIVES.png`, puis `FVS_05_PAN_ZOOM_EDGE.png` si l'owner demande la lecture terrain.

Message owner:

- "On ouvre la WorldMap sur le terrain Wave5 preserve."
- "Le HUD reste fixe et la demo est locale."
- "Aucune carte terrain 50x50 n'est generee."

Preuves provisoires:

- `Wave5 25x25 visible terrain preserved: PASS`
- `Terrain preserved: PASS`
- `NO_50X50_ART_GENERATED=true`
- `WAVE5_25X25_VISIBLE_PRESERVED=true`
- `Center active chunks: 25`
- `North-west active chunks: 9`
- `South-east active chunks: 9`

### 0:30-1:20 - Preset Collecte R3

Action demo attendue P6:

1. Selectionner le preset `Collecte R3`.
2. Pointer une ressource riche R3.
3. Afficher la quantite initiale.
4. Lancer la collecte locale.
5. Montrer l'etat epuise.
6. Lancer ou attendre le respawn deterministe local.
7. Verifier que la quantite revient a la valeur de demo.

Capture minimale provisoire:

- `FVS_02_RESOURCE_INTERACTION.png`

Preuves provisoires:

- `Poor/medium/rich coverage: PASS`
- `Resource selection: PASS`
- `Local collection: PASS`
- `Depletion after collection: PASS`
- `Deterministic demo respawn: PASS`
- `Quantity before collection: 129`
- `Quantity after respawn: 129`
- `Selected resource proof: res_wax_32_30_0:rich:Cire`

Phrase a dire:

- "Le preset Collecte R3 sert a prouver selection, quantite, collecte, epuisement et respawn local, sans economie officielle."

### 1:20-2:20 - Preset Duel: PLAYER_TEST_HIVE vs ENEMY_TEST_HIVE

Action demo attendue P6:

1. Selectionner le preset `Duel`.
2. Montrer `PLAYER_TEST_HIVE`.
3. Montrer `ENEMY_TEST_HIVE`.
4. Modifier niveau, classe et soldats.
5. Montrer que les valeurs modifient le resultat local attendu.
6. Declencher `Reset`.
7. Verifier le retour deterministe aux valeurs de reference.

Captures minimales provisoires:

- `FVS_00_CENTER_LAB_HIVES.png`
- `FVS_01_HIVE_PROGRESSION.png`

Preuves provisoires:

- `PLAYER_TEST_HIVE` et `ENEMY_TEST_HIVE` visibles dans la capture centre.
- `FVS_01_HIVE_PROGRESSION.png`: ruche neutre pre-10, deux classes post-10, evolution N35, overlays faction separes.
- `HIVE_VISUAL_PROGRESSION_VISIBLE=PASS` selon le rapport QA source.

Phrase a dire:

- "Le duel ne simule pas une guerre serveur; il sert a verifier localement les parametres owner: niveau, classe, soldats, puis reset deterministe."

Preuve P6 requise avant `YES`:

- Le recu P6 doit nommer explicitement les champs modifiables.
- Le recu P6 doit prouver le reset deterministe du preset `Duel`.

### 2:20-3:15 - Preset Raid T7

Action demo attendue P6:

1. Selectionner le preset `Raid T7`.
2. Afficher la composition requise.
3. Afficher la composition disponible.
4. Lancer le resultat local.
5. Montrer explicitement `server=false` et `official_gain=false`.

Capture minimale provisoire:

- `FVS_03_BESTIARY_SOLO_RAID.png`

Preuves provisoires:

- `T1..T7 coverage: PASS`
- `Raid combat local: PASS`
- `No official gain/server: PASS`
- `Raid target: beast_t7_proof`
- `Last bestiary telemetry: T7 Reine frelon mode=raid_local required=336 available=456 result=win official_gain=false server=false`
- `Server/remote/officiel: ABSENT`

Phrase a dire:

- "Le raid T7 affiche requis, disponible et resultat local. La ligne de preuve importante est: `official_gain=false server=false`."

### 3:15-4:05 - Filtres, Proche, legende

Action demo attendue P6:

1. Activer/desactiver les filtres ruches, ressources, menaces et BearDen.
2. Utiliser `Proche` pour selectionner l'entite la plus proche.
3. Montrer la legende tiers/richness.
4. Verifier que le terrain reste non masque par defaut.

Captures minimales provisoires:

- `FVS_00_CENTER_LAB_HIVES.png`
- `FVS_05_PAN_ZOOM_EDGE.png`

Preuves provisoires:

- `Nearest node selection: PASS`
- `Filters hives/resources/threats/BearDen: PASS`
- `Fixed HUD rectangle: PASS`
- `Terrain unmasked by default: PASS`
- `Legend tiers/richness: PASS`
- `Status: Ruche proche: hive_player_test (23u)`

Phrase a dire:

- "Les filtres servent a lire la carte, pas a masquer le terrain. Proche selectionne la cible utile et la legende explique tiers et richesse."

### 4:05-4:35 - BearDen

Action demo attendue P6:

1. Afficher BearDen visible.
2. Masquer BearDen avec filtre ou etat demo.
3. Restaurer BearDen.
4. Confirmer que BearDen reste separe des entites scenario.

Capture minimale provisoire:

- `FVS_04_BEAR_DEN_STATES.png`

Preuves provisoires:

- `BearDen visible, cache, restaure, separe des entites.`
- `BEAR_DEN_REGRESSION=NO` dans le rapport QA existant.

Phrase a dire:

- "BearDen reste intact: visible, cache, restaure, et separe du ScenarioLab."

### 4:35-5:00 - Recap limites et statut P6

Message owner:

- "Cette demo est locale."
- "Aucun serveur, aucun APK, aucun device."
- "Aucune carte terrain 50x50 n'est generee."
- "Les resultats de collecte, duel et raid sont des resultats de laboratoire local."
- "La preuve finale P6 est jointe par rapport principal et recu local; aucune capture P6 dediee n'est inventee."

Statut a annoncer:

- `READY_FOR_OWNER_SCENARIO_DEMO_WHEN_P6_PASS=YES`

## Captures minimales

Minimum actuel reutilisable:

1. `FVS_00_CENTER_LAB_HIVES.png`
   - Ouverture WorldMap Wave5, `PLAYER_TEST_HIVE`, `ENEMY_TEST_HIVE`, HUD fixe, ressources, bestiaire.
   - SHA256: `8fc0641825a8414a7a63e7198a9da91241275bfeac89a28a8b6b6713c6f3446a`

2. `FVS_02_RESOURCE_INTERACTION.png`
   - Preset Collecte R3 provisoire: riche, quantite, collecte, epuise, respawn.
   - SHA256: `c0c8c6a39e3ff076c3b596feb655e1f26db9a32cea6e1d928c547e3c3522e31a`

3. `FVS_01_HIVE_PROGRESSION.png`
   - Duel/progression: niveau, classe, etat pre-10, N35, overlays faction.
   - SHA256: `9934f1588faef9a43aaf30dcad488ef405a224ba8d14c2748dd230d761c7f320`

4. `FVS_03_BESTIARY_SOLO_RAID.png`
   - Raid T7 provisoire: solo/raid, T7 local, sans gain officiel.
   - SHA256: `5396af4589b03eff17c1dd7e3b44288f3bfee1cf27dd5b84638ffd96b39179ef`

5. `FVS_04_BEAR_DEN_STATES.png`
   - BearDen visible/cache/restaure.
   - SHA256: `8ea8a4640f7543e9657d3b3bf2d57249199fcdcb266db5dffc5152756a186fe2`

6. `FVS_05_PAN_ZOOM_EDGE.png`
   - Pan/zoom, centre + bord nord-ouest, HUD fixe, terrain preserve.
   - SHA256: `8681515292d84b79ad135489313a59151790fa414576e1c4cf810abebceb227c`

Captures P6 dediees:

- Aucune capture P6 dediee ni manifeste de captures P6 n'est publie dans les deux preuves P6 lues.
- Ne pas inventer de capture ScenarioLab.
- Pour le parcours owner, reutiliser les captures FVS existantes ci-dessus comme support visuel et citer les gates P6 comme preuve runtime.

## Plan de secours

Si le preset `Collecte R3` n'est pas disponible:

- Montrer `FVS_02_RESOURCE_INTERACTION.png`.
- Lire le recu RuntimeIntegrationProof: `Local collection`, `Depletion after collection`, `Deterministic demo respawn`.
- Annoncer: "Le preset P6 est prouve par le recu local; si l'interface directe n'est pas disponible, cette capture illustre la mecanique sous-jacente deja validee."

Si le preset `Duel` n'est pas disponible:

- Montrer `FVS_00_CENTER_LAB_HIVES.png` et `FVS_01_HIVE_PROGRESSION.png`.
- Annoncer: "Les ruches test et la progression visuelle existent; le recu P6 confirme que PLAYER_TEST_HIVE et ENEMY_TEST_HIVE sont editables."

Si le preset `Raid T7` n'est pas disponible:

- Montrer `FVS_03_BESTIARY_SOLO_RAID.png`.
- Lire la telemetrie: `T7 Reine frelon mode=raid_local required=336 available=456 result=win official_gain=false server=false`.

Si les filtres/Proche/legende ne sont pas disponibles dans la session:

- Lire `MapReadingToolsProofReceipt.md`.
- Montrer `FVS_00_CENTER_LAB_HIVES.png` et `FVS_05_PAN_ZOOM_EDGE.png`.

Si BearDen n'est pas manipulable en direct:

- Montrer `FVS_04_BEAR_DEN_STATES.png`.
- Rappeler que la preuve actuelle couvre visible/cache/restaure et regression absente.

## Checklist P6 cloturee

Champs remplaces par les chemins reels:

- Rapport P6: OK.
- Recu Play Mode/local P6: OK.
- Dossier de captures P6: non publie.
- Manifeste P6 avec SHA256: non publie.

Le rapport/recu P6 contient explicitement:

- Presets scenarios: PASS.
- `PLAYER_TEST_HIVE` / `ENEMY_TEST_HIVE` editables: PASS.
- Records hives/resources/bestiary/events: `12/39/10/1`.
- Stable entity ids: PASS.
- Normalized coordinates: PASS.
- 25x25 to 50x50 reprojection: PASS.
- Local authority adapter: PASS.
- Legacy P1-P5 regression: PASS.
- `server=false`.
- `official_gain=false`.
- Server/remote: ABSENT.
- APK rebuild: ABSENT.
- 50x50 terrain generation: ABSENT.

Verdict:

`READY_FOR_OWNER_SCENARIO_DEMO_WHEN_P6_PASS=YES`
