# WorldMap Runtime Entities Wave1 - Unity Integration Report

Date locale: 2026-07-15

## Verdict

RUNTIME_ENTITIES_WAVE1_UNITY_INTEGRATION=PASS

## Portee integree

- Ressources premium R1/R2/R3 copiees dans `Assets/BeeKingdom/Playground/Resources/WorldMapRuntimeEntitiesWave1/`.
- Bestiaire premium M1 copie dans `Assets/BeeKingdom/Playground/Resources/WorldMapRuntimeEntitiesWave1/`.
- WorldMap runtime branchee sur les PNG premium via `Resources.Load`.
- Les ressources runtime couvrent Nectar, Pollen, Eau, Cire, Miel, Gelee royale, Propolis.
- Le bestiaire local/demo ajoute des silhouettes T1..T7 en spawn seed deterministe, hors serveur et hors recompense officielle.
- Un noeud bestiaire temoin est present au centre pour preuve reproductible.

## Garanties preservees

- Scene canonique preservee: `Assets/Scenes/WorldMapMmoFullscreenFoundation.unity`.
- Wave5 25x25 preserve.
- 625 tuiles et master terrain non modifies.
- BearDen preserve, separe et non remplace.
- Mission 1 LAB LOCAL preservee.
- Aucun APK reconstruit.
- Aucun serveur, remote, donnee reelle, publication, DNS/TLS/SQL.

## Verification

- Compilation Unity 6000.2.10f1: PASS, zero erreur compile.
- Play Mode runtime entities harness: PASS.
- Recu de preuve: `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/RuntimeIntegrationProof/RuntimeEntitiesProofReceipt.md`

Resultats du recu:

- Ressources actives/proof: 39.
- Ressources avec texture premium chargee: 39.
- Eau presente: PASS.
- Miel present: PASS.
- Bestiaire actif/proof: 10.
- Bestiaire avec texture premium chargee: 10.
- Tier max bestiaire dans fenetre proof: 6.
- Serveur/remote/officiel: ABSENT.

## Fichiers principaux

- `Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs`
- `Assets/BeeKingdom/Playground/Editor/WorldMapRuntimeEntitiesProofHarness.cs`
- `Assets/BeeKingdom/Playground/Resources/WorldMapRuntimeEntitiesWave1/R1/`
- `Assets/BeeKingdom/Playground/Resources/WorldMapRuntimeEntitiesWave1/R2/`
- `Assets/BeeKingdom/Playground/Resources/WorldMapRuntimeEntitiesWave1/R3/`
- `Assets/BeeKingdom/Playground/Resources/WorldMapRuntimeEntitiesWave1/M1/`

## Prochaine etape locale conseillee

Faire une passe visuelle owner dans Unity sur centre, deux zooms et deplacements courts. Si valide, la suite locale naturelle est d'etendre le meme branchement runtime aux ruches de classe H2/H3 hors LAB LOCAL, en gardant les overlays de faction separes.
