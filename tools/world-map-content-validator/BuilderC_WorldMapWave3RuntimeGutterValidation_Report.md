# Builder-C - World Map Wave 3 Runtime Bundle and Gutter Validation

Date: 2026-07-14  
Role: Builder-C, independent validation  
Scope: local Wave 3 runtime bundle, outside Unity and server

## Executive result

Builder-C independently recalculated the authoritative source hash, both bundle
inventories, every canonical crop, both reconstructions, every runtime tile and
every gutter pixel. No producer result was accepted as proof without a separate
calculation.

The two 54-file bundles are byte-identical. Their 25 canonical tiles decode to
the exact UI-B/master pixels. Their 25 runtime tiles each contain an unchanged
512x512 interior plus a two-pixel gutter sourced from the exact adjacent master
coordinates. All 40 internal boundaries, 80 internal sides and 20 external
clamp sides pass.

This result makes the bundle eligible for a future Unity integration step. It
does not claim that Unity integration, runtime streaming, an immense world or a
live server has been delivered.

## Inputs

- Authoritative master: `C:\projets\beekingdom\worldmap_art_wave5\UIB_ContinuousMaster5x5\master_5x5_2560.png`
- Authoritative UI-B tiles: `C:\projets\beekingdom\worldmap_art_wave5\UIB_ContinuousMaster5x5\tiles`
- Producer run 1: `C:\projets\beekingdomgame-master\artifacts\WorldMapWave3_RuntimeBundle_staging\run1`
- Producer run 2: `C:\projets\beekingdomgame-master\artifacts\WorldMapWave3_RuntimeBundle_staging\run2`
- Producer summary: `C:\projets\beekingdomgame-master\artifacts\WorldMapWave3_RuntimeBundle_staging\real_ingest_summary.json`
- Independent audit: `C:\projets\beekingdomgame-master\artifacts\BuilderC_WorldMapWave3_RuntimeAudit\independent_audit_post_negative.json`
- Negative sample evidence: `C:\projets\beekingdomgame-master\artifacts\BuilderC_WorldMapWave3_RuntimeAudit\negative_sample_result.json`

## Independent method

The Builder-C auditor does not import or call the producer slicer. It reads the
master and bundle files directly, decodes PNG pixels, derives each expected
crop and gutter from master coordinates, and compares the resulting arrays.
The producer verifier was replayed separately only as a second implementation.

Independent tool:

`C:\projets\beekingdomgame-master\tools\world-map-content-validator\audit_wave3_runtime_bundle.py`

Disposable negative-test runner:

`C:\projets\beekingdomgame-master\tools\world-map-content-validator\run_wave3_runtime_negative_sample.py`

## Source and inventory

| Check | Run 1 | Run 2 | Result |
|---|---:|---:|---:|
| Master dimensions | 2560x2560 RGB | 2560x2560 RGB | PASS |
| Master SHA-256 | `D3CDC2D...A95B4` | `D3CDC2D...A95B4` | PASS |
| Files inventoried | 54 | 54 | PASS |
| Missing files | 0 | 0 | PASS |
| Extra files | 0 | 0 | PASS |
| Forbidden extensions | 0 | 0 | PASS |
| Canonical PNGs | 25 | 25 | PASS |
| Runtime PNGs | 25 | 25 | PASS |

Full independently recalculated master SHA-256:

`D3CDC2DDE9D56CAC58BE6833790B6FD8FC38AC157F72A01DCEBD8117583A95B4`

Master RGB pixel SHA-256:

`4D6DAADC128C16912B8ED222F966D26C93AA300462EDD4D2C38E8A97C98C7181`

## Canonical pixel identity

| Check | Run 1 | Run 2 |
|---|---:|---:|
| Canonical dimensions | 25/25 at 512x512 | 25/25 at 512x512 |
| Manifest PNG hashes | 25/25 | 25/25 |
| Manifest pixel hashes | 25/25 | 25/25 |
| Differences from master crops | 0 channels | 0 channels |
| Differences from UI-B decoded tiles | 0 channels | 0 channels |
| In-memory reconstruction differences | 0 channels | 0 channels |
| Saved reconstruction differences | 0 channels | 0 channels |

The UI-B PNG files and bundle PNG files may use different lossless compression,
but their decoded canonical pixels are identical. The identity verdict is based
on decoded pixels and reconstructed master content, not accidental PNG encoding
identity.

## Runtime tiles and gutters

The expected runtime tile was independently derived by padding the complete
master by two edge-clamped pixels, then cropping the exact 516x516 window for
each tile. This checks corners and both axes together and prevents a manifest
from masking incorrect image content.

| Check | Run 1 | Run 2 |
|---|---:|---:|
| Runtime dimensions | 25/25 at 516x516 | 25/25 at 516x516 |
| Full runtime tile differences | 0 channels | 0 channels |
| Interior 512x512 differences | 0 channels | 0 channels |
| Manifest PNG hashes | 25/25 | 25/25 |
| Manifest pixel hashes | 25/25 | 25/25 |
| UV `2/516..514/516` exact | 25/25 | 25/25 |
| Internal true-neighbor sides | 80/80 | 80/80 |
| External clamp sides | 20/20 | 20/20 |
| Internal boundaries | 40/40 | 40/40 |
| Stretching observed/declared | NO/false | NO/false |

Internal gutters were compared against the exact neighboring master pixels,
not merely against the neighboring manifest entry. External clamping occurs
only on the 20 outer sides of the 5x5 macro master.

## Run determinism

All relative files were hashed independently after the negative test cleanup.

- Run 1 files: 54
- Run 2 files: 54
- Missing or extra relative paths: 0
- Different SHA-256 values: 0
- Independent tree digest run 1: `5FEDD45E61584F6B8283D7F6D2C1FFE6B462E5CB6C8E629CFC19D8A13FD2C8CC`
- Independent tree digest run 2: `5FEDD45E61584F6B8283D7F6D2C1FFE6B462E5CB6C8E629CFC19D8A13FD2C8CC`

The independent digest algorithm is Builder-C's inventory digest and is not
expected to equal the producer's differently constructed tree digest. Equality
between run 1 and run 2 is exact under both implementations.

## Verify replay and negative sample

The producer `macro_slicer.py verify` command was replayed read-only on run 1
and run 2. Both returned process code 0, 25/25 canonical tiles, 25/25 runtime
tiles, 40/40 boundaries, zero pixel mismatch and `PASS`.

Builder-C then copied run 1 into an isolated temporary QA directory and changed
one RGB channel at coordinate `(0, 258)` in the left gutter of
`runtime/tiles/R2C2_g2.png`.

Both validators rejected the copy with process code 2. The independent auditor
reported:

- `RUNTIME_PIXEL_ALTERATION`
- `RUNTIME_HASH_MISMATCH`
- `RUNTIME_PIXEL_HASH_MISMATCH`
- `RUNTIME_SIDE_PROVENANCE_OR_PIXEL_MISMATCH`
- `INTERNAL_GUTTER_BOUNDARY_FAILURE`

The temporary QA copy was automatically removed. A post-test independent audit
and producer verify returned PASS on the original source bundles with the same
independent tree digest as before the mutation.

## Content and non-claims

The bundle contains only PNG and JSON files. Its JSON claims explicitly set
`live_server`, `official_world_map`, `runtime_integration` and
`unity_dependency` to `false`; no route, pathfinding or overlay payload is
present.

The canonical decoded pixels are identical to the authoritative master already
reviewed in the separate Builder-C art report. That report found no painted
route/track, UI, text, hive, interactive resource, troop or flight trajectory.
Runtime tiles only add exact neighboring pixels or outer edge clamps, so they
introduce no new painted content.

No producer bundle, UI-B PNG, UI-B manifest, Unity file, scene or server file
was modified. Only Builder-C tools, evidence and this report were created.

## Regression status

The complete content-validator suite was rerun after this audit: **19/19 PASS**.
Wave 2 regressions and all Wave 3 negative fixtures remain green.

## Scope boundary

PASS covers a deterministic local 5x5 runtime-ready image bundle. It does not
prove Unity import settings, texture sampling in a scene, runtime streaming,
camera behavior, memory use on device, a final immense map, gameplay, economy
or live/server behavior. Those remain separate integration and QA gates.

## Report placement

The requested external report directory is read-only in this Builder-C
environment. The explicitly authorized fallback path is therefore used:

`C:\projets\beekingdomgame-master\tools\world-map-content-validator\BuilderC_WorldMapWave3RuntimeGutterValidation_Report.md`

## Final verdicts

WORLD_MAP_WAVE3_RUNTIME_BUNDLE_INTEGRITY = PASS

REAL_MASTER_CANONICAL_PIXEL_IDENTITY = PASS

RUNTIME_TRUE_NEIGHBOR_GUTTERS_40_OF_40 = PASS

RUN1_RUN2_BYTE_IDENTITY = PASS

READY_FOR_WORLD_MAP_WAVE3_UNITY_INTEGRATION = YES
