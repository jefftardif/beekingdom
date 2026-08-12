# World Map Entities - 5 Minute Owner Demo Plan

Date locale: 2026-07-15

## Cadre

Objectif: fournir un parcours owner court, rejouable et strictement local pour demontrer les entites runtime de la World Map sans attendre Unity.

Sources bornees utilisees:

- `Docs/Recovery/BeeKingdom_Relay_Progress.md`
- `Docs/WorldMapRuntimeEntitiesWave1/FinalVisualSmokeQA_Report.md`
- `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/FinalVisualSmokeProof/manifest.md`
- `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/RuntimeIntegrationProof/RuntimeEntitiesProofReceipt.md`
- `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/MapReadingToolsProof/MapReadingToolsProofReceipt.md`
- `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/WorldMap50x50ReadinessProof/WorldMap50x50ReadinessProofReceipt.md`

Contraintes confirmees:

- Demo locale uniquement.
- Aucun serveur, remote, DNS/TLS/SQL, donnee reelle ou gain officiel.
- Aucun APK/device.
- Aucun changement Unity/PNG dans cette releve.
- Les captures GameView batch restent indisponibles avant timeout borne; les captures FVS sont des compositions Unity Editor locales utilisant les vrais assets PNG Wave5, BearDen et WorldMapRuntimeEntitiesWave1 importes dans Unity.

Verdict source: `READY_FOR_OWNER_WORLD_MAP_ENTITIES_DEMO=YES`.

## Parcours owner 5 minutes

### 0:00-0:35 - Ouverture locale et promesse de demo

Montrer `FVS_00_CENTER_LAB_HIVES.png`.

Points a dire:

- La scene cible est `Assets/Scenes/WorldMapMmoFullscreenFoundation.unity`.
- La preuve est locale, sans serveur et sans gain officiel.
- Le centre montre le terrain Wave5, `PLAYER_TEST_HIVE`, `ENEMY_TEST_HIVE`, les ressources, le bestiaire et le HUD fixe `LAB LOCAL`.
- Les validations sources indiquent: compilation Unity PASS, Runtime Entities Play Mode PASS, LAB LOCAL Play Mode PASS.

Preuve associee:

- `FINAL_VISUAL_SMOKE_QA=PASS_WITH_NOTES`
- `RUNTIME_ENTITIES_WAVE1_UNITY_INTEGRATION=PASS`

### 0:35-1:15 - Lecture carte: centre, coins, pan et zoom

Montrer `FVS_05_PAN_ZOOM_EDGE.png`.

Points a dire:

- La demo couvre le centre et le bord nord-ouest.
- Le HUD reste fixe pendant le pan/zoom.
- La preuve composee ne montre aucune tuile manquante.
- Le readiness 50x50 est valide sans generation d'art 50x50: Wave5 25x25 preserve, budgets PASS, cache stable PASS.

Preuves associees:

- `WORLD_MAP_50X50_READINESS_P1=PASS`
- Centre active chunks: 25
- North-west active chunks: 9
- South-east active chunks: 9
- Catalog hives/resources/bestiary: 725/3740/699
- `NO_50X50_ART_GENERATED=true`
- `WAVE5_25X25_VISIBLE_PRESERVED=true`

### 1:15-2:00 - Progression ruche: pre-10, classes, N35

Montrer `FVS_01_HIVE_PROGRESSION.png`.

Points a dire:

- La progression visuelle de ruche est visible.
- La capture couvre une ruche neutre pre-10, deux classes post-10 et une evolution N35.
- Les overlays de faction sont separes pour eviter de confondre niveau, classe et appartenance.
- `PLAYER_TEST_HIVE` et `ENEMY_TEST_HIVE` sont integres comme entites de test runtime local.

Preuve associee:

- `HIVE_VISUAL_PROGRESSION_VISIBLE=PASS`
- `HIVE_RUNTIME_PROGRESSION_INTEGRATION=PASS`

### 2:00-2:45 - Ressources: pauvre, moyen, riche, epuisement, respawn

Montrer `FVS_02_RESOURCE_INTERACTION.png`.

Points a dire:

- Les ressources pauvre/moyenne/riche sont visibles et selectionnables.
- La demo montre la quantite, l'etat epuise, puis le respawn deterministe de demo.
- Le comportement reste local, sans economie officielle.

Preuves associees:

- Resource nodes visible/proof: 39
- Textured resource nodes: 39
- Poor/medium/rich coverage: PASS
- Resource selection: PASS
- Local collection: PASS
- Depletion after collection: PASS
- Deterministic demo respawn: PASS
- Quantity before collection: 129
- Quantity after respawn: 129
- Selected resource proof: `res_wax_32_30_0:rich:Cire`

### 2:45-3:30 - Player/enemy, bestiaire, T1 solo et T7 raid

Montrer `FVS_03_BESTIARY_SOLO_RAID.png`, puis rappeler `FVS_00_CENTER_LAB_HIVES.png` si besoin pour `PLAYER_TEST_HIVE` et `ENEMY_TEST_HIVE`.

Points a dire:

- Le bestiaire couvre la lecture solo et raid.
- T1..T7 est valide dans la preuve logique; la capture illustre T1 solo et T7 raid.
- Le combat est local et ne donne aucun gain officiel.
- `LAB LOCAL` sert a declencher les tests de collecte/combat et a garder la demo autonome.

Preuves associees:

- Bestiary nodes visible/proof: 10
- Textured bestiary nodes: 10
- T1..T7 coverage: PASS
- Bestiary selection: PASS
- Solo combat local: PASS
- Raid combat local: PASS
- No official gain/server: PASS
- Raid target: `beast_t7_proof`
- Last bestiary telemetry: `T7 Reine frelon mode=raid_local required=336 available=456 result=win official_gain=false server=false`

### 3:30-4:10 - BearDen: visible, cache, restaure

Montrer `FVS_04_BEAR_DEN_STATES.png`.

Points a dire:

- BearDen est demontre dans trois etats: visible, cache, restaure.
- BearDen reste separe des entites runtime Wave1.
- La regression BearDen est absente.

Preuves associees:

- `BEAR_DEN_REGRESSION=NO`
- BearDen visible/cache/restaure: PASS en preuve visuelle composee, regression logique absente.

### 4:10-4:45 - Filtres, legende, recherche/selection

Montrer `FVS_00_CENTER_LAB_HIVES.png` ou la session locale si disponible.

Points a dire:

- Les outils de lecture carte P2 sont integres.
- Les filtres disponibles couvrent ruches, ressources, menaces et BearDen.
- La legende couvre tiers/richness.
- La selection du noeud le plus proche est validee; statut observe: `Ruche proche: hive_player_test (23u)`.
- La recherche est a montrer des qu'elle est exposee dans l'interface integree; ne pas la presenter comme preuve finale si elle n'apparait pas dans le build local courant.

Preuves associees:

- `MAP_READING_TOOLS_P2=PASS`
- Nearest node selection: PASS
- Filters hives/resources/threats/BearDen: PASS
- Fixed HUD rectangle: PASS
- Terrain unmasked by default: PASS
- Legend tiers/richness: PASS

### 4:45-5:00 - Cloture et limites

Points a dire:

- La demo est prete pour owner: `READY_FOR_OWNER_WORLD_MAP_ENTITIES_DEMO=YES`.
- Les preuves sont locales et bornees.
- Aucun serveur, device, APK, donnee reelle, gain officiel ou publication n'est inclus dans ce parcours.
- Les captures GameView batch ne sont pas revendiquees; les captures FVS sont la preuve visuelle locale disponible.

## Liste minimale de captures

Toutes les captures sont dans:

`Docs/BuilderA/WorldMapRuntimeEntitiesWave1/FinalVisualSmokeProof/`

1. `FVS_00_CENTER_LAB_HIVES.png`
   - Usage demo: ouverture centre, `PLAYER_TEST_HIVE`, `ENEMY_TEST_HIVE`, HUD fixe, ressources, bestiaire, `LAB LOCAL`.
   - SHA256: `8fc0641825a8414a7a63e7198a9da91241275bfeac89a28a8b6b6713c6f3446a`

2. `FVS_01_HIVE_PROGRESSION.png`
   - Usage demo: ruche neutre pre-10, classes post-10, N35, overlays faction.
   - SHA256: `9934f1588faef9a43aaf30dcad488ef405a224ba8d14c2748dd230d761c7f320`

3. `FVS_02_RESOURCE_INTERACTION.png`
   - Usage demo: pauvre/moyen/riche, selection, quantite, epuise, respawn.
   - SHA256: `c0c8c6a39e3ff076c3b596feb655e1f26db9a32cea6e1d928c547e3c3522e31a`

4. `FVS_03_BESTIARY_SOLO_RAID.png`
   - Usage demo: T1 solo, T7 raid, combat local sans gain officiel.
   - SHA256: `5396af4589b03eff17c1dd7e3b44288f3bfee1cf27dd5b84638ffd96b39179ef`

5. `FVS_04_BEAR_DEN_STATES.png`
   - Usage demo: BearDen visible, cache, restaure.
   - SHA256: `8ea8a4640f7543e9657d3b3bf2d57249199fcdcb266db5dffc5152756a186fe2`

6. `FVS_05_PAN_ZOOM_EDGE.png`
   - Usage demo: centre + bord nord-ouest, pan/zoom, HUD fixe, absence de tuile manquante dans la composition.
   - SHA256: `8681515292d84b79ad135489313a59151790fa414576e1c4cf810abebceb227c`

## Manifeste et recus a joindre

Joindre au paquet owner minimal:

- `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/FinalVisualSmokeProof/manifest.md`
- `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/RuntimeIntegrationProof/RuntimeEntitiesProofReceipt.md`
- `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/MapReadingToolsProof/MapReadingToolsProofReceipt.md`
- `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/WorldMap50x50ReadinessProof/WorldMap50x50ReadinessProofReceipt.md`
- `Docs/WorldMapRuntimeEntitiesWave1/FinalVisualSmokeQA_Report.md`

## Script oral ultra-court

"Cette demo est volontairement locale. Elle prouve que la World Map preserve Wave5, affiche les ruches player/enemy, les ressources, le bestiaire et BearDen, avec HUD LAB LOCAL fixe. On commence au centre, on verifie le pan/zoom jusqu'au bord nord-ouest, puis on passe sur la progression ruche pre-10, classes et N35. Ensuite on montre les ressources pauvre/moyen/riche avec quantite, epuisement et respawn deterministe. Le bestiaire valide le solo et le raid local jusqu'au T7, sans gain officiel ni serveur. BearDen reste visible/cache/restaure sans regression. Les filtres, la legende et la selection proche sont valides; la recherche est a montrer uniquement des qu'elle apparait dans l'interface integree. Fin de demo: aucune preuve serveur/device/APK n'est revendiquee."

## Risques de presentation

- Ne pas presenter les captures FVS comme des screenshots GameView batch; ce sont des compositions Unity Editor locales avec assets reels.
- Ne pas promettre serveur, economie, gain officiel, APK ou test device.
- Ne pas decrire la recherche comme preuve finale si elle n'est pas visible dans l'interface locale au moment de la demo.
- Ne pas modifier BearDen, les 625 tuiles Wave5, le master terrain, Unity ou les PNG pour cette releve.

## Verdict demo relay

`WORLD_MAP_ENTITIES_5_MINUTE_OWNER_DEMO_PLAN=READY`

