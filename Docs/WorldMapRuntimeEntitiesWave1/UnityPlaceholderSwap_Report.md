# WorldMap Runtime Entities Wave1 - Unity Placeholder Swap Report

## Statut

APPROVED_LOCAL_SWAP_APPLIED

## Portee

Le swap a ete applique uniquement au laboratoire local de la scene canonique `Assets/Scenes/WorldMapMmoFullscreenFoundation.unity`.

Les placeholders IMGUI de `PLAYER_TEST_HIVE` et `ENEMY_TEST_HIVE` utilisent maintenant les PNG premium locaux quand ils sont disponibles, avec fallback automatique vers le rendu geometrique precedent si une texture manque.

## Assets copies sous Resources

Racine:

`Assets/BeeKingdom/Playground/Resources/WorldMapRuntimeEntitiesWave1/`

Assets principaux:

- `H2/hive_nurturer_l10.png` pour `PLAYER_TEST_HIVE`
- `H2/hive_striker_l10.png` pour `ENEMY_TEST_HIVE`

Fallbacks de niveau copies:

- `H1/hive_neutral_l1.png`
- `H1/hive_neutral_l4.png`
- `H1/hive_neutral_l7.png`
- `H1/hive_neutral_l9.png`
- `H3/hive_nurturer_l20.png`
- `H3/hive_nurturer_l35.png`
- `H3/hive_nurturer_l50.png`
- `H3/hive_striker_l20.png`
- `H3/hive_striker_l35.png`
- `H3/hive_striker_l50.png`

## Code modifie

- `Assets/BeeKingdom/Playground/WorldMapLocalLabRuntime.cs`
- `Assets/BeeKingdom/Playground/Editor/WorldMapLocalLabProofHarness.cs`

## Validation

Compilation Unity:

- Log: `C:\projets\beekingdomgame-master\Logs\worldmap_local_lab_premium_swap_compile.log`
- Resultat: PASS.
- Erreurs C#: 0.
- Validation scene: PASS.

Play Mode harness:

- Log: `C:\projets\beekingdomgame-master\Logs\worldmap_local_lab_premium_swap_playmode.log`
- Recu: `C:\projets\beekingdomgame-master\Docs\BuilderA\WorldMapTestHivesCombatCollectionLab\PlayerProof\WorldMapLocalLabProofReceipt.md`
- Play Mode: PASS.
- Reset defaults: PASS.
- Deterministic collection: PASS.
- Deterministic combat: PASS.
- Local only: PASS.
- Premium hive textures loaded: PASS.

Note: les captures PNG batchmode restent indisponibles avant timeout borne, comme avant. Les gates logiques Play Mode sont executés et valides.

## Non-regression

- Tuiles Wave5 modifiees: non.
- Master terrain modifie: non.
- BearDen modifie ou active: non.
- Ours ajoute: non.
- APK reconstruit: non.
- Serveur, remote, persistance officielle ou donnees reelles: non.

## Verdict

UNITY_PLACEHOLDER_SWAP_LOCAL_LAB_READY=YES
