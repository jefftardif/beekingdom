# Spawn Inspector P7 - Technical Re-Review

Date locale: 2026-07-15

## Portee

Re-revue technique independante Builder-C Relay, read-only, contre les nouvelles preuves P7:

- `Docs/WorldMapRuntimeEntitiesWave1/SpawnInspector_QAClosure_Report.md`
- `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/SpawnInspectorProof/SpawnInspectorProofReceipt.md`
- `Docs/BuilderCRelay/WorldMapSpawnDistribution_TechnicalContract.md`
- `Docs/BuilderCRelay/SpawnInspectorIntegration_P7_IndependentAudit.md`

Contraintes respectees:

- Aucun fichier Unity modifie.
- Aucun PNG modifie.
- Aucun APK modifie.
- Aucun terrain, scene, master terrain, source BearDen, serveur, remote ou donnee reelle modifie.

## Verdict synthese

Les nouvelles preuves ferment les reserves non bloquantes du precedent audit P7:

- Hashes A1/A2/B explicites et coherents.
- Couverture fenetres 25x25 et 50x50 logique complete.
- Exclusions forcees BearDen/eau/falaise/evenement reserve avec acceptes=0.
- Tests negatifs P7-NEG-001 a P7-NEG-008: 8/8 PASS.
- Cache textures: Wave5=15/96, total=37/96.
- Max entites observees: chunks/ruches/ressources/menaces = 25/22/50/19.
- Allocations stress logique: 0/2,000,000 octets.
- Overlay OFF/ON invariant sur hash `f17362b9`.
- Authority flags: `server=false`, `official=false`, `official_gain=false`, `remote_calls=0`.

Verdict global:

BUILDER_C_P7_REREVIEW=PASS

READY_FOR_P8_TELEMETRY_EXECUTION=YES

## Audit seeds, versions, hashes

Preuve attendue par contrat:

- Meme seed/version => meme distribution logique.
- Seed differente => distribution differente avec budgets preserves.
- Changement de version seed => hash different et audite.

Preuves observees:

- seed A: `738921`.
- A1 hash: `f17362b9`.
- A2 hash apres parcours centre-voisin-centre: `f17362b9`.
- A1/A2 count: PASS.
- A1/A2 IDs: PASS.
- A1/A2 positions: PASS.
- A1/A2 tiers: PASS.
- A1/A2 richness: PASS.
- A1/A2 flags: PASS.
- seed B: `918337`.
- B hash: `7b8adab4`.
- Seed B distribution changed: PASS.
- Seed B budgets preserved: PASS.
- Version alternative: `spawn_v2_proof` / `ab507cde`.
- Spawn seed version change audited: PASS.

Verdict:

P7_HASHES_A1_A2_B=PASS

## Audit fenetres 25x25

Preuve attendue par contrat:

- Fenetre active <= 25 chunks.
- Bords clamps.
- Coins reduits a 9 chunks.
- Budgets hives/resources/threats <= 25/75/25.

Preuves observees 25x25:

| Fenetre | Chunks | Ruches | Ressources | Menaces | In bounds | Budgets |
| --- | ---: | ---: | ---: | ---: | --- | --- |
| center | 25 | 2 | 11 | 7 | PASS | PASS |
| N | 15 | 9 | 30 | 10 | PASS | PASS |
| S | 15 | 6 | 30 | 9 | PASS | PASS |
| E | 15 | 13 | 30 | 5 | PASS | PASS |
| W | 15 | 9 | 30 | 13 | PASS | PASS |
| NW | 9 | 4 | 18 | 8 | PASS | PASS |
| NE | 9 | 6 | 18 | 5 | PASS | PASS |
| SW | 9 | 7 | 18 | 9 | PASS | PASS |
| SE | 9 | 6 | 18 | 4 | PASS | PASS |
| densest | 25 | 22 | 50 | 19 | PASS | PASS |

Max 25x25 observe:

- chunks=25.
- ruches=22.
- ressources=50.
- menaces=19.

Verdict:

P7_WINDOW_25X25=PASS

## Audit fenetres 50x50 logique

Preuve attendue par contrat:

- Catalogue 50x50 logique = 2500 coordonnees.
- Aucun terrain 50x50 cree.
- Fenetre active <= 25.
- Bords et coins bornes.
- Reprojection chunk/local dans les bornes.

Preuves observees 50x50:

| Fenetre | Chunks | Ruches | Ressources | Menaces | In bounds | Budgets |
| --- | ---: | ---: | ---: | ---: | --- | --- |
| center | 25 | 8 | 43 | 8 | PASS | PASS |
| N | 15 | 5 | 22 | 6 | PASS | PASS |
| S | 15 | 6 | 21 | 4 | PASS | PASS |
| E | 15 | 6 | 27 | 2 | PASS | PASS |
| W | 15 | 6 | 25 | 3 | PASS | PASS |
| NW | 9 | 2 | 14 | 2 | PASS | PASS |
| NE | 9 | 2 | 13 | 0 | PASS | PASS |
| SW | 9 | 5 | 13 | 4 | PASS | PASS |
| SE | 9 | 4 | 15 | 1 | PASS | PASS |
| densest | 25 | 14 | 40 | 14 | PASS | PASS |

Autres preuves:

- Logical 50x50 coordinates: 2500.
- Logical 50x50 terrain generated: false.
- chunk_cache_before_after_50x50: 25/25.
- no_50x50_terrain_generated: true.
- Reprojection records checked: 20.
- Reprojected chunk X range: 23..27.
- Reprojected chunk Y range: 23..27.
- Reprojected local range: 0.002451..0.99118.
- reprojection_50x50_pass: PASS.

Verdict:

P7_WINDOW_50X50_REPROJECTION=PASS

## Audit exclusions forcees

Preuve attendue par contrat:

- BearDen/eau/falaise/evenement reserve rejettent les candidats interdits.
- Reprojection reapplique les exclusions.
- Aucune entite acceptee dans une exclusion.

Preuves observees:

| Zone | Soumis | Rejete | Accepte | Motif | Apres reprojection |
| --- | ---: | ---: | ---: | --- | --- |
| BearDen | 1 | 1 | 0 | ExclusionVolumeHit:BearDen | PASS, meme motif |
| water | 1 | 1 | 0 | ExclusionVolumeHit:water | PASS, meme motif |
| cliff | 1 | 1 | 0 | ExclusionVolumeHit:cliff | PASS, meme motif |
| reserved_event | 1 | 1 | 0 | ExclusionVolumeHit:reserved_event | PASS, meme motif |

Autre preuve:

- accepted_entities_inside_exclusions: 0.
- Forced exclusions: PASS.

Verdict:

P7_FORCED_EXCLUSIONS=PASS

## Audit tests negatifs

Preuve attendue par contrat:

- Les injections adversariales doivent etre refusees par un motif explicite.

Preuves observees:

| ID | Observed | Result |
| --- | --- | --- |
| P7-NEG-001 | DeterminismMismatch | PASS |
| P7-NEG-002 | DensityBudgetExceeded(chunks=26,hives=26,resources=76,threats=26) | PASS |
| P7-NEG-003 | ExclusionVolumeHit:BearDen | PASS |
| P7-NEG-004 | ExclusionVolumeHit:water; ExclusionVolumeHit:cliff; ExclusionVolumeHit:reserved_event | PASS |
| P7-NEG-005 | RaidRequired:T7 | PASS |
| P7-NEG-006 | NormalizedCoordinateOutOfRange | PASS |
| P7-NEG-007 | DiagnosticOverlayDefaultOn | PASS |
| P7-NEG-008 | OfficialGainForbidden | PASS |

Autre preuve:

- Negative tests passed: 8/8.

Verdict:

P7_NEGATIVE_TESTS=PASS

## Audit distances, selection et lisibilite

Preuve attendue par contrat:

- Chevauchements critiques absents.
- Proximites mineures auditees.
- Selection proche garde la cible attendue.
- R1/R2/R3 lisibles sans couleur.
- T1-T4 solo, T5-T7 raid, T7 solo refuse.

Preuves observees:

- critical_overlaps: 0.
- minor_overlaps: 8.
- overlap thresholds critical/minor: 0.001/48.
- nearest selection expected = selected pour `preview:BK-DEMO-WORLD-WAVE5-LOCAL:grid_25x25_v1:bestiary:C30_39:b0:spawn_v1:738921`.
- nearest selection: PASS.
- combat_t1_t4_solo: PASS (`T1=solo,T2=solo,T3=solo,T4=solo`).
- combat_t5_t7_raid: PASS (`T5=raid,T6=raid,T7=raid`).
- combat_t7_solo_refused: PASS (`RaidRequired:T7`).
- richness R1/R2/R3: `[R1] pauvre` / `[R2] moyen` / `[R3] riche`.
- richness_r1_r2_r3_readable: PASS.
- richness_readable_without_color: PASS.

Verdict:

P7_SELECTION_DISTANCE_READABILITY=PASS

## Audit cache, allocations et regression

Preuve attendue par contrat:

- Cache terrain <= 96.
- Entites runtime cachees sans explosion.
- Stress 50x50 <= 2 MB, cible observee 0.
- Pas de terrain 50x50.
- Regression P1-P6 preservee.

Preuves observees:

- wave5_cached_textures: 15/96.
- runtime_entity_texture_cache_entries: 22.
- total_cached_textures: 37/96.
- allocated_bytes_50x50_stress: 0/2000000.
- chunk_cache_before_after_50x50: 25/25.
- no_50x50_terrain_generated: true.
- density_budgets: PASS.
- P1-P6 regression: PASS.

Verdict:

P7_CACHE_ALLOCATIONS_REGRESSION=PASS

## Audit overlay invariant

Preuve attendue par contrat:

- Overlay diagnostic OFF par defaut.
- Overlay ON/OFF ne modifie pas la distribution.

Preuves observees:

- diagnostic_overlay_default_off: PASS.
- overlay OFF hash: `f17362b9`.
- overlay ON hash: `f17362b9`.
- overlay_distribution_unchanged: PASS.
- P7-NEG-007 rejette `DiagnosticOverlayDefaultOn`.

Verdict:

P7_OVERLAY_INVARIANT=PASS

## Audit autorite et remote

Preuve attendue par contrat:

- Toute sortie locale reste non officielle.
- Pas de serveur.
- Pas de remote.
- Pas de gain officiel.
- Les tentatives `official_gain=true` sont rejetees.

Preuves observees:

- server=false.
- official=false.
- official_gain=false.
- remote_calls=0.
- authority_validation: PASS (`local_only_authority`).
- P7-NEG-008: `OfficialGainForbidden`, PASS.
- SERVER_OR_OFFICIAL_GAIN=NO.
- AUTHORITY_FLAGS=PASS.

Verdict:

P7_AUTHORITY_REMOTE=PASS

## Comparaison au contrat Builder-C P7

| Domaine contrat | Preuve finale | Verdict |
| --- | --- | --- |
| seed/version/world/chunk | seed A/B, spawn_v1, exclusion_v1, world_grid_version | PASS |
| IDs stables | A1/A2 IDs PASS, format preview stable | PASS |
| R1-R3 | richesse textuelle R1/R2/R3 lisible sans couleur | PASS |
| T1-T7 | T1-T4 solo, T5-T7 raid, T7 solo refuse | PASS |
| caps fenetre | max 25/22/50/19 sous limites 25/25/75/25 | PASS |
| exclusions | 4/4 forcees rejetees, acceptes=0 | PASS |
| distances/selection | critiques=0, selection proche PASS | PASS |
| streaming/cache | Wave5 15/96, total 37/96, chunk cache stable | PASS |
| allocations | 0/2MB | PASS |
| reprojection | 20 records, chunks/local bornes, 50x50 PASS | PASS |
| inspection locale | overlay invariant, details et gates receipt | PASS |
| autorite | server=false, official=false, remote_calls=0 | PASS |

## Limites restantes

Limites non bloquantes:

- Les huit proximites mineures restent documentees; elles sont non critiques et la selection proche retourne la cible attendue.
- La preuve reste locale-demo/playmode; elle ne valide pas serveur, persistence officielle, economie officielle ou combat officiel.

## Gates

```text
P7_HASHES_A1_A2_B=PASS
P7_WINDOW_25X25=PASS
P7_WINDOW_50X50_REPROJECTION=PASS
P7_FORCED_EXCLUSIONS=PASS
P7_NEGATIVE_TESTS=PASS
P7_SELECTION_DISTANCE_READABILITY=PASS
P7_CACHE_ALLOCATIONS_REGRESSION=PASS
P7_OVERLAY_INVARIANT=PASS
P7_AUTHORITY_REMOTE=PASS
```

BUILDER_C_P7_REREVIEW=PASS

READY_FOR_P8_TELEMETRY_EXECUTION=YES

Le PASS est strict pour l'objectif P7 local-demo: les anciennes reserves de preuve sont fermees par le rapport QA closure et le recu detaille. Le passage P8 peut se concentrer sur la telemetrie, sans transformer ces preuves locales en autorite officielle.
