# Bee Kingdom - Owner 5 Minute World Map Demo

Date locale: 2026-07-15

## Cadre a annoncer

- Demo locale uniquement.
- Aucun serveur, remote, gain officiel, persistance officielle, APK ou donnee reelle.
- Scene: `Assets/Scenes/WorldMapMmoFullscreenFoundation.unity`
- Terrain visible: Wave5 25x25 preserve.
- Mode 50x50: stress logique local, desactive par defaut, sans art terrain 50x50.

## Parcours 5 minutes

### 0:00 - 0:45: vue globale

- Ouvrir la scene World Map.
- Montrer le centre: Wave5, `PLAYER_TEST_HIVE`, `ENEMY_TEST_HIVE`, ressources, menaces, HUD fixe.
- Dire: le laboratoire local et les entites runtime sont testables sans serveur.

### 0:45 - 1:30: ruches et progression

- Dans `LAB LOCAL`, changer niveau/classe puis `Apply`.
- Montrer:
  - neutre pre-10;
  - classe niveau 10;
  - evolution niveau 35;
  - player/enemy avec surcouche faction separee.
- Utiliser `Reset` pour confirmer le retour stable.

### 1:30 - 2:25: lecture carte

- Ouvrir `LECTURE CARTE`.
- Activer/desactiver Ruches, Ressources, Menaces, BearDen.
- Utiliser `Selectionner plus proche`.
- Montrer la legende R1/R2/R3, T1-T7, solo/raid.
- Pan/zoom: verifier que le HUD reste fixe et que le terrain reste lisible.

### 2:25 - 3:20: ressources

- Selectionner une ressource pauvre, moyenne ou riche.
- Lancer la collecte locale.
- Montrer:
  - quantite;
  - trajet depart/retour;
  - etat `[X] epuise`;
  - respawn demo deterministe.

### 3:20 - 4:15: bestiaire

- Selectionner une menace T1 solo.
- Lancer le combat local.
- Selectionner une menace T7 raid.
- Montrer:
  - tier;
  - PV local;
  - composition requise;
  - resultat deterministe;
  - `official_gain=false` / aucune recompense officielle.

### 4:15 - 5:00: BearDen et readiness 50x50

- Montrer BearDen visible, cache, restaure.
- Expliquer que le stress 50x50 simule 2500 coordonnees logiques sans generer de terrain 50x50.
- Citer les seuils P4:
  - chunks actifs centre/NW/SE/densite: 25/9/9/25;
  - ressources texturees: 39;
  - bestiaire tier max: 7;
  - budgets/cache/terrain/allocation: PASS.

## Verdict a presenter

READY_FOR_OWNER_50X50_READINESS_DEMO=YES

## Preuves locales utiles

- `Docs\WorldMapRuntimeEntitiesWave1\AutomatedRegression_Report.md`
- `Docs\BuilderA\WorldMapRuntimeEntitiesWave1\AutomatedRegressionProof\AutomatedRegressionProofReceipt.md`
- `Docs\WorldMapRuntimeEntitiesWave1\WorldMap50x50Readiness_Report.md`
- `Docs\WorldMapRuntimeEntitiesWave1\MapReadingTools_Report.md`
- `Docs\WorldMapRuntimeEntitiesWave1\InteractionPolish_Report.md`
