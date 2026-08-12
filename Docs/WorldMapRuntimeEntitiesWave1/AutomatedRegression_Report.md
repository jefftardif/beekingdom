# WorldMap Runtime Entities Wave1 - Automated Regression Report

Date locale: 2026-07-15

## Cadre

- Scene cible: `Assets/Scenes/WorldMapMmoFullscreenFoundation.unity`
- Regression locale Play Mode couvrant P1 a P4.
- Aucun APK, serveur, remote, persistance officielle, gain officiel, DNS/TLS/SQL ou donnee reelle.
- Aucun terrain 50x50 genere.
- Wave5 25x25, BearDen et LAB LOCAL preserves.

## Verification

- Compilation Unity batchmode: PASS, zero erreur.
- Play Mode automated regression: PASS.
- Recu: `C:\projets\beekingdomgame-master\Docs\BuilderA\WorldMapRuntimeEntitiesWave1\AutomatedRegressionProof\AutomatedRegressionProofReceipt.md`
- Log compilation: `C:\projets\beekingdomgame-master\Logs\automated_regression_p4_compile_retry.log`
- Log Play Mode: `C:\projets\beekingdomgame-master\Logs\automated_regression_p4_playmode_retry.log`

## Couverture

- Wave5 manifest et tuiles visibles: PASS
- BearDen visible/cache/restaure: PASS
- LAB deux ruches, reset, collecte et combat local: PASS
- Progression ruches H1/H2/H3, neutre N4, cinq classes N10, evolution N35: PASS
- R1/R2/R3, selection, collecte, epuisement, respawn local: PASS
- M1 T1..T7, solo et raid local: PASS
- Filtres, selection proche, legende: PASS
- Polish interactions: quantite, trajectoire, epuisement, respawn, combat: PASS
- Stress logique 50x50: PASS

## Seuils observes

- Tuiles visibles Wave5: 3/3
- Ressources texturees: 39
- Bestiaire texture: 11
- Bestiaire tier max: 7
- Catalogue logique 50x50: 2500 coordonnees
- Chunks actifs centre/NW/SE/densite: 25/9/9/25
- Budgets/cache/terrain/allocation 50x50: PASS

## Verdict

AUTOMATED_REGRESSION_P4=PASS
WAVE5_TERRAIN_REGRESSION=NO
BEAR_DEN_REGRESSION=NO
READY_FOR_P5_DEMO_PACKAGE=YES
