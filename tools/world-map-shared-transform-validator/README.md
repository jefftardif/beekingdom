# World Map Shared Transform Validator

Standalone Step5A regression gate. It does not load Unity and does not modify
`Assets`, scenes, textures, manifests, or server files.

## Accepted oracle

```text
screen = viewport_center + (world - camera_center) * zoom
world  = camera_center + (screen - viewport_center) / zoom
```

Terrain samples, hives, resources, selections, and flight anchors must all use
this transform or its exact inverse. HUD rectangles remain in screen space.

## Commands

Generate and validate the known negative fixture:

```powershell
python shared_transform_validator.py generate-fixture `
  --kind current-defect `
  --output current_defect.json

python shared_transform_validator.py validate-evidence `
  --input current_defect.json `
  --output current_defect_validation.json
```

Generate and validate the expected positive fixture:

```powershell
python shared_transform_validator.py generate-fixture `
  --kind positive-shared `
  --output positive_shared.json

python shared_transform_validator.py validate-evidence `
  --input positive_shared.json `
  --output positive_shared_validation.json
```

Audit a Builder-A candidate without modifying it:

```powershell
python shared_transform_validator.py audit-source `
  --source C:\path\to\WorldMapMmoFullscreenFoundationBootstrap.cs `
  --tile-dir C:\path\to\UIB_ContinuousMaster5x5_v1 `
  --output source_audit.json
```

Run tests:

```powershell
python -m unittest discover -s tests -v
```

The negative fixture and the current source audit are expected to return exit
code `2`. The positive fixture and a conforming Builder-A candidate return `0`.

## Refusal rules

- fullscreen UV projection independent from the world camera;
- a shared primary terrain path that can fall back to a decoupled UV path;
- any fake-map fallback reachable when the Wave3 provider fails to load;
- terrain/entity pan delta mismatch or terrain response below 95 percent;
- terrain/entity zoom factor or pivot mismatch;
- `TextureWrapMode.Repeat` or non-clamp evidence;
- pilot art repeated/modulo-populated as a logical 64x64 world;
- a Wave3 inventory other than 25 distinct 516x516 PNG hashes and IDs;
- missing explicit world bounds for pilot art;
- HUD rectangle movement during pan or zoom;
- any live-server claim in the evidence.

Static source inspection is support evidence. Final Step5A closure still needs
runtime telemetry or interactive proof exported with terrain and entity anchors
from the same frames.

## Rendered tile seam gate

Run the independent rendered-frame gate after a three-capture pan proof:

```powershell
python rendered_tile_seam_validator.py `
  --telemetry C:\path\to\WorldMapStep5A_PanProofTelemetry.json `
  --capture-dir C:\path\to\WorldMapStep5APanProofHarness `
  --tile-dir C:\path\to\UIB_ContinuousMaster5x5_v1 `
  --source C:\path\to\WorldMapMmoFullscreenFoundationBootstrap.cs `
  --output C:\path\to\Run10_TileSeamValidation.json `
  --run-id RUN10 `
  --verdict-key FINAL_RUN10_TILE_SEAM_GATE
```

The gate projects all internal 512-unit tile boundaries with the camera values
from telemetry, excludes HUD rectangles and screen edges, and rejects coherent
dark strips at rendered boundaries. It independently rechecks all 40 internal
neighbor boundaries, 80 true-neighbor gutter sides, 20 outer Clamp sides,
Unity import settings, runtime Clamp/inner UV settings, and the absence of a
boundary-covering strip in the terrain draw method. A dark camouflage line is
a failure, not an acceptable way to hide a seam.

For a mixed-orientation twelve-frame zoom proof, each sample may declare its
own `screen_size`. Require the complete media inventory explicitly:

```powershell
python rendered_tile_seam_validator.py `
  --telemetry C:\path\to\WorldMapStep5A_ZoomProofTelemetry.json `
  --capture-dir C:\path\to\WorldMapStep5AZoomProofHarness `
  --tile-dir C:\path\to\UIB_ContinuousMaster5x5_v1 `
  --source C:\path\to\WorldMapMmoFullscreenFoundationBootstrap.cs `
  --external-run-receipt C:\path\to\ZoomProofExternalRunReceipt.md `
  --output C:\path\to\ZoomProofValidation.json `
  --run-id STEP5A_ZOOM12 `
  --verdict-key FINAL_ZOOM_PROOF_GATE `
  --expected-capture-count 12 `
  --required-size 1920x1080:6 `
  --required-size 720x1280:6
```

This profile additionally rejects undecodable or duplicate PNGs, a mismatch
between declared and decoded dimensions, Repeat/modulo operations in the
terrain draw method, a zero-distance overlay anchor, shared zoom-ratio drift,
HUD movement beyond one pixel, an unexecuted negative fixture, and a mismatch
between telemetry and the external Unity exit receipt.
