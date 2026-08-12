# WorldMap Spawn Inspector Integration - P7 Report

Date locale: 2026-07-15

Statut final de la preuve: `PASS`

## Cadre execute

- Scene Play Mode cible: `Assets/Scenes/WorldMapMmoFullscreenFoundation.unity`.
- Modifications de code limitees au bootstrap et au harness P7 autorises.
- Aucun changement de scene, gameplay visible, terrain, tuile, master, source BearDen, PNG, APK, serveur ou remote.
- La grille 50x50 est simulee par des coordonnees et fenetres logiques uniquement.
- Le harness est borne a 180 secondes.

## Verification Unity finale

- Unity: `6000.2.10f1`.
- Compilation batchmode: PASS, code de sortie 0, aucune erreur C#.
- Log compilation: `C:\projets\beekingdomgame-master\Logs\spawn_inspector_p7_evidence_closure_compile_sealed.log`.
- Play Mode harness: PASS.
- Log Play Mode: `C:\projets\beekingdomgame-master\Logs\spawn_inspector_p7_evidence_closure_playmode_sealed.log`.
- Recu final: `C:\projets\beekingdomgame-master\Docs\BuilderA\WorldMapRuntimeEntitiesWave1\SpawnInspectorProof\SpawnInspectorProofReceipt.md`.
- Recu genere a: `2026-07-15T14:46:08.3914302Z`.

## Determinisme et variation

- Seed A: `738921`; seed B: `918337`.
- Versions: `spawn_v1`, `exclusion_v1`, `wave5_25x25_to_logical_50x50_v1`.
- A1 hash: `f17362b9`.
- A2 hash apres parcours centre-voisin-centre: `f17362b9`.
- Comparaison A1/A2 IDs, positions, tiers, richesses et flags: PASS pour chaque champ.
- B hash: `7b8adab4`; distribution differente: PASS.
- Seed B chunks/ruches/ressources/menaces: `25/2/9/3`; budgets preserves: PASS.
- Changement de version vers `spawn_v2_proof`, hash `ab507cde`: PASS avec budgets preserves.

## Fenetres et budgets

Chaque ligne contient `chunks/ruches/ressources/menaces`.

| Grille | Centre | N | S | E | W | NW | NE | SW | SE | Densest |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 25x25 | 25/2/11/7 | 15/9/30/10 | 15/6/30/9 | 15/13/30/5 | 15/9/30/13 | 9/4/18/8 | 9/6/18/5 | 9/7/18/9 | 9/6/18/4 | 25/22/50/19 |
| 50x50 logique | 25/8/43/8 | 15/5/22/6 | 15/6/21/4 | 15/6/27/2 | 15/6/25/3 | 9/2/14/2 | 9/2/13/0 | 9/5/13/4 | 9/4/15/1 | 25/14/40/14 |

- Maxima toutes fenetres: `25/22/50/19`, sous `25/25/75/25`.
- Coordonnees logiques 50x50 auditees: 2500; aucun terrain 50x50 genere.
- Cache chunks avant/apres stress logique: `25/25`.
- Textures en cache: Wave5=15, entites runtime=22, total=`37/96`.
- Allocations mesurees sur le thread pendant le stress logique: `0/2000000` octets.

## Exclusions et negatifs

- Candidats forces BearDen/eau/falaise/evenement reserve: 1 soumis, 1 rejete, 0 accepte pour chaque zone.
- Motifs: `ExclusionVolumeHit:BearDen`, `ExclusionVolumeHit:water`, `ExclusionVolumeHit:cliff`, `ExclusionVolumeHit:reserved_event`.
- Les quatre exclusions sont reappliquees apres reprojection: PASS.
- `accepted_entities_inside_exclusions=0`.
- `P7-NEG-001` a `P7-NEG-008`: 8/8 PASS avec resultat individualise dans le recu.

## Interaction et lisibilite

- Chevauchements critiques, definis comme centres selectionnables indiscernables a `<=0.001` unite: `0`.
- Proximites mineures a `<=48` unites: `8`, listees comme compteur non bloquant.
- Selection de l'entite proche attendue: PASS.
- T1-T4 solo: PASS; T5-T7 raid: PASS; demande T7 solo refusee avec `RaidRequired:T7`.
- R1/R2/R3: `[R1] pauvre`, `[R2] moyen`, `[R3] riche`; lecture sans couleur: PASS.
- Reprojection 50x50: 20 enregistrements, chunks X/Y `23..27`, local `0.002451..0.99118`: PASS.
- Overlay OFF/ON: hashes `f17362b9` / `f17362b9`; distribution inchangee: PASS.

## Autorite locale

- `server=false`.
- `official=false`.
- `official_gain=false`.
- `remote_calls=0`.
- Provider local-only et negatif `official_gain=true`: PASS.
- Regression P1-P6 executee par la preuve imbriquee: PASS.

## Gates

```text
P7_QA_EVIDENCE_CLOSURE=PASS
P7_NEGATIVE_TESTS_8_OF_8=PASS
FORCED_EXCLUSIONS=PASS
WINDOW_COVERAGE=PASS
AUTHORITY_FLAGS=PASS
READY_FOR_QA_P7_REVIEW=YES
```

Ce rapport atteste la production de la preuve P7 finale. Il ne remplace pas la contre-validation independante du verdict QA dans `Docs/QARelay`.
