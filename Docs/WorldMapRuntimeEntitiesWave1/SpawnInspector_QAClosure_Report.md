# Spawn Inspector P7 - QA Evidence Closure Report

Date locale: 2026-07-15

Objet: fermeture des insuffisances de preuve `P7-QA-B01` a `P7-QA-B06` relevees dans `Docs/QARelay/SpawnInspector_P7_QA_Verdict.md`.

## Sources finales

- Matrice QA: `Docs/QARelay/WorldMapSpawnDistribution_QA_Matrix.md`.
- Recu Play Mode detaille: `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/SpawnInspectorProof/SpawnInspectorProofReceipt.md`.
- Rapport d'integration corrige: `Docs/WorldMapRuntimeEntitiesWave1/SpawnInspectorIntegration_Report.md`.
- Compilation finale: `Logs/spawn_inspector_p7_evidence_closure_compile_sealed.log`.
- Play Mode final: `Logs/spawn_inspector_p7_evidence_closure_playmode_sealed.log`.
- Unity: `6000.2.10f1`; compilation PASS; Play Mode harness borne PASS.

## Fermeture B01-B06

| Defaut | Preuve ajoutee | Resultat |
| --- | --- | --- |
| P7-QA-B01 | Seeds/versions, A1/A2 `f17362b9`, comparaison separee IDs/positions/tiers/richesses/flags, B `7b8adab4`, tableaux de fenetres, maxima, reprojection, chevauchements et acces | FERME |
| P7-QA-B02 | Lignes individualisees `P7-NEG-001` a `P7-NEG-008`, injection, rejet attendu, resultat observe | FERME, 8/8 PASS |
| P7-QA-B03 | Un candidat force dans chacune des zones BearDen/eau/falaise/evenement, motif de rejet et nouveau controle apres reprojection | FERME, 4/4 PASS, acceptes=0 |
| P7-QA-B04 | Centre, N/S/E/W, NW/NE/SW/SE et densest sur 25x25 et 50x50 logique, coords bornees et budgets par ligne | FERME |
| P7-QA-B05 | Critiques=0, mineurs=8, selection proche PASS, T1-T4 solo, T5-T7 raid, T7 solo refuse, R1-R3 textuels, overlay invariant | FERME |
| P7-QA-B06 | `server=false`, `official=false`, `official_gain=false`, `remote_calls=0`, negatif official gain rejete | FERME |

## Resultats auditables

### Seeds

- Seed A: `738921`; A1=`f17362b9`; A2=`f17362b9`.
- Egalite A1/A2: compte PASS, IDs PASS, positions PASS, tiers PASS, richesses PASS, flags PASS.
- Seed B: `918337`; hash=`7b8adab4`; distribution changee PASS.
- Seed B: `25/2/9/3`; budgets preserves PASS.
- Version alternative `spawn_v2_proof`: hash=`ab507cde`; variation versionnee PASS.

### Couverture des fenetres

Valeurs: `chunks/ruches/ressources/menaces`.

| Grille | Centre | N | S | E | W | NW | NE | SW | SE | Densest |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 25x25 | 25/2/11/7 | 15/9/30/10 | 15/6/30/9 | 15/13/30/5 | 15/9/30/13 | 9/4/18/8 | 9/6/18/5 | 9/7/18/9 | 9/6/18/4 | 25/22/50/19 |
| 50x50 logique | 25/8/43/8 | 15/5/22/6 | 15/6/21/4 | 15/6/27/2 | 15/6/25/3 | 9/2/14/2 | 9/2/13/0 | 9/5/13/4 | 9/4/15/1 | 25/14/40/14 |

- Maxima: chunks=25, ruches=22, ressources=50, menaces=19.
- Limites: chunks<=25, ruches<=25, ressources<=75, menaces<=25.
- Reprojection: chunks dans 0..49 et local dans 0..1; PASS.
- 50x50: 2500 coordonnees logiques; cache chunks `25 -> 25`; aucun terrain cree.

### Exclusions forcees

| Zone | Soumis | Rejete | Accepte | Motif | Apres reprojection |
| --- | ---: | ---: | ---: | --- | --- |
| BearDen | 1 | 1 | 0 | `ExclusionVolumeHit:BearDen` | Rejete, meme motif |
| Eau | 1 | 1 | 0 | `ExclusionVolumeHit:water` | Rejete, meme motif |
| Falaise | 1 | 1 | 0 | `ExclusionVolumeHit:cliff` | Rejete, meme motif |
| Evenement reserve | 1 | 1 | 0 | `ExclusionVolumeHit:reserved_event` | Rejete, meme motif |

`accepted_entities_inside_exclusions=0`

### Negatifs

| ID | Resultat observe | Statut |
| --- | --- | --- |
| P7-NEG-001 | `DeterminismMismatch` | PASS |
| P7-NEG-002 | `DensityBudgetExceeded(chunks=26,hives=26,resources=76,threats=26)` | PASS |
| P7-NEG-003 | `ExclusionVolumeHit:BearDen` | PASS |
| P7-NEG-004 | Rejets eau, falaise et evenement reserves individualises | PASS |
| P7-NEG-005 | `RaidRequired:T7` | PASS |
| P7-NEG-006 | `NormalizedCoordinateOutOfRange` | PASS |
| P7-NEG-007 | `DiagnosticOverlayDefaultOn` | PASS |
| P7-NEG-008 | `OfficialGainForbidden` | PASS |

### Interactions et autorite

- Chevauchements critiques de selection a `<=0.001` unite: 0.
- Proximites mineures a `<=48` unites: 8; selection proche: PASS.
- T1-T4=`solo`; T5-T7=`raid`; T7 solo refuse: PASS.
- `[R1] pauvre`, `[R2] moyen`, `[R3] riche`: distincts sans couleur, PASS.
- Overlay OFF/ON: `f17362b9` / `f17362b9`, distribution inchangee.
- `server=false`, `official=false`, `official_gain=false`, `remote_calls=0`.

### Ressources d'execution

- Cache textures observe: Wave5=15, entites runtime=22, total=`37/96`.
- Allocations du thread pendant le stress logique: `0/2000000` octets.
- Regression P1-P6 imbriquee: PASS.

## Limites de la preuve

- Aucun fichier de scene, terrain, tuile, master, source BearDen, PNG ou APK n'a ete modifie.
- Aucun serveur, remote, gain officiel ou persistence officielle n'a ete utilise.
- Les huit proximites mineures sont rapportees; elles ne sont pas classees critiques car les centres restent distincts et la selection proche retourne la cible attendue.
- Le present rapport ferme la suffisance des preuves cote producteur. La mise a jour du verdict QA read-only reste une action de contre-validation separee.

## Gates

```text
P7_QA_EVIDENCE_CLOSURE=PASS
P7_NEGATIVE_TESTS_8_OF_8=PASS
FORCED_EXCLUSIONS=PASS
WINDOW_COVERAGE=PASS
AUTHORITY_FLAGS=PASS
READY_FOR_QA_P7_REVIEW=YES
```
