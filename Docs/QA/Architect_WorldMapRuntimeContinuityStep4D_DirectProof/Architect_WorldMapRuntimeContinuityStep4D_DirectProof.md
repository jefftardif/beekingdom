# Architect - World Map Runtime Continuity Step4D Direct Proof

Date: 2026-07-14

## Scope

This is a local Unity proof for `Assets/Scenes/WorldMapMmoFullscreenFoundation.unity` after the Step4C clamp/no-wrap correction and the Step4D deterministic proof controls.

It validates the current 3x3 macro runtime surface only. It does not claim that the Wave3 5x5 art is integrated, that a physical Android device was tested, or that any staging/live service is active.

## Why the first automated set was rejected

The automatic mixed-resolution queue was not accepted as evidence:

- its PNG was one state behind the manifest;
- the portrait request remained at 1920x1080;
- Unity deletes the `Temp` capture directory on close.

No product PASS was inferred from that set. The exact states were instead applied one at a time, allowed to settle, captured in the correct Game View resolution, and copied out of `Temp` before Unity was closed.

## Preserved evidence

Archive: `C:\projets\beekingdomgame-master\Docs\QA\Architect_WorldMapRuntimeContinuityStep4D_DirectProof`

| Timestamp | Resolution | Zoom | Center chunk | UV rect | Result |
| --- | --- | ---: | --- | --- | --- |
| 10:54:00Z | 720x1280 | 1.10 | C32_32 | 0.258,0.068,0.748,0.938 | PASS |
| 10:56:15Z | 1920x1080 | 0.85 | C32_32 | 0,0.222,1,0.784 | PASS |
| 10:56:20Z | 1920x1080 | 1.10 | C32_32 | 0.068,0.258,0.938,0.748 | PASS |
| 10:56:25Z | 1920x1080 | 1.35 | C32_32 | 0.133,0.295,0.873,0.711 | PASS |
| 10:56:29Z | 1920x1080 | 1.10 | C32_32 | 0.068,0.258,0.938,0.748 | PASS |
| 10:56:34Z | 1920x1080 | 1.10 | C35_32 | 0.085,0.258,0.955,0.748 | PASS |
| 10:56:39Z | 1920x1080 | 1.10 | C36_32 | 0.09,0.258,0.96,0.748 | PASS |

Every manifest reports:

- expected and actual resolution equal;
- 25 active chunks;
- UV coordinates bounded in `[0,1]`;
- atlas loaded with `Clamp` wrap mode;
- `master_5x5_integrated = false`;
- `server_live = false`;
- no retouch and no masking overlay.

## Visual inspection

The seven unretouched PNGs were inspected individually.

- Landscape zooms 0.85, 1.10 and 1.35 show one continuous surface.
- Portrait 720x1280 is correctly framed and responsive.
- The C32_32 -> C35_32 -> C36_32 sequence moves terrain continuously.
- No image boundary, chunk grid, repeated strip, black band, hole, flash, frozen texture or camouflage overlay is visible.
- Hives, resources, HUD and flight overlays remain separate from the terrain.
- Flight arcs remain aerial; no road graph is used as movement logic.
- The historical DEMO-096 vertical Repeat seam is not reproduced.

## Technical corroboration

Builder-A's Step4D control validation completed with process exit code 0 and no C# compilation error. The exact manual proof above closes the perceptual portion that self-checks alone could not close.

## Reservations

1. The automatic multi-state capture queue has a one-frame/resolution defect. This is P2 proof-tooling debt, not a runtime map blocker, because the exact manual evidence is coherent and preserved.
2. Evidence is from the Unity Editor Game View, not a physical Android device.
3. The current proof uses the 3x3 macro surface. Wave3 5x5 integration remains a separate gated step.

## Verdict

```text
ARCHITECT_WORLD_MAP_RUNTIME_CONTINUITY_STEP4D = PASS_WITH_RESERVES
EXACT_SEVEN_STATE_MANIFEST_SET = PASS
LANDSCAPE_ZOOM_085_110_135 = PASS
PORTRAIT_720X1280_ZOOM_110 = PASS
PAN_C32_C35_C36_CONTINUITY = PASS
VISIBLE_TILE_OR_CHUNK_BOUNDARY = NO
ATLAS_REPEAT_OR_BAND = NO
OVERLAYS_SEPARATE_AND_AIR_ONLY = PASS
WAVE3_5X5_INTEGRATED = NO
READY_FOR_WORLD_MAP_WAVE3_UNITY_INTEGRATION_AFTER_ART_GUTTER_HANDOFF_GATES = YES
```

