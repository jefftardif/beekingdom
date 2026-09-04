# M046B-CX - Upgrade Outline Visibility Tuning

Date: 2026-09-04  
Agent: Architecte / Codex  
Repository: `C:/projets/beekingdomgame-master`

## Objective

M046 added a pulse to the active-upgrade blue outline, but CEO runtime feedback confirmed it was not visually acceptable:

- the line was much too thin;
- the pulse was not noticeable at normal HiveMap zoom;
- the effect could not be perceived on a real actively upgrading building.

M046B makes the existing upgrade outline more readable while preserving the same visual language: blue, silhouette-following, premium, subtle, no building blink, no particles and no new outline system.

## Existing Runtime Path

The active-upgrade outline still uses the existing path:

1. `HiveViewProductUiPresenter.ActiveOfficialUpgradeHotspotIdForExternalHost()`
2. `HiveMapBuildingUpgradeVisualStateBootstrap.Update()`
3. `BuildingCatalog.TryGetByLegacyKey(...)`
4. `BuildingInteractionController.Registry.GetGameObjectByBuildingType(...)`
5. `BuildingSelectionHighlight.Show(...)`
6. `ArtworkOutline.shader`

Shader:

- `Assets/Experiments/Environment2D5D/Shaders/ArtworkOutline.shader`
- property `_Color` drives color/alpha;
- property `_OutlineWidth` drives sampled edge thickness in texels;
- property `_AlphaCutoff` keeps the contour tied to the artwork alpha silhouette.

Material behavior:

- `BuildingSelectionHighlight` creates one cached material per highlight instance.
- M046B updates `_Color` and `_OutlineWidth` on that cached material.
- No per-frame material instantiation was added.

## Why M046 Was Not Perceptible

Cause proven from CEO runtime feedback plus the current runtime implementation:

- The outline shader emits only silhouette edge texels, not a glow field.
- The M046 base width was still the original `2f` texels.
- The M046 pulse only changed alpha between `0.74` and `1.0`.
- On detailed gold/blue building art at normal HiveMap zoom, a 2-texel edge with alpha-only variation is too small to read as a living pulse.
- The pulse code did run through the correct building path, but the property range had negligible visual impact in normal gameplay framing.

Classification from mission list:

- A. Pulse code not actually running: NO evidence.
- B. Wrong building receiving pulse: NO evidence.
- C. Property not reaching shader: NO evidence; `SetAlpha` reached cached material color.
- D. Shader property negligible visual influence: YES for alpha-only on a 2-texel edge.
- E. Alpha/intensity range too small: YES.
- F. Outline thickness too small: YES.
- G. Pulse period too slow/fast: PARTIAL; `1.9s` was acceptable but slower than needed for quick perception.
- H. Camera zoom makes effect sub-pixel: YES at normal/moderate zoom because base width was too low.
- I. Another material/state overrides outline: NO evidence.
- J. Pulse stops incorrectly: fixed in M046; still preserved.

## M046B Implementation

`BuildingSelectionHighlight` now supports per-instance outline width:

- `OutlineWidthTexels`
- `CurrentOutlineWidthForProof`
- `SetVisualState(float alpha, float outlineWidthTexels)`

Default selection/training behavior remains at `2f` unless the caller sets another width.

`HiveMapBuildingUpgradeVisualStateBootstrap` now configures only the upgrade-state highlight with stronger but still controlled values:

- minimum alpha: `0.86`
- maximum alpha: `1.0`
- minimum width: `4.5f` texels
- maximum width: `5.75f` texels
- period: `1.45s`
- easing: `Mathf.SmoothStep`

The minimum width is already above the old width, so the outline never collapses back to the nearly invisible M046 state. Width and alpha pulse together, giving the player a readable "currently upgrading" signal within one to two seconds at normal zoom.

## State Lifecycle

Idle:

- no running official upgrade operation;
- no upgrade pulse.

Running:

- active official operation status equals `HiveBuildingUpgradeClient.RunningStatus`;
- thicker blue silhouette outline appears and gently pulses.

AwaitingCompletion:

- `ActiveOfficialUpgradeHotspotIdForExternalHost()` returns null;
- upgrade pulse stops;
- CL's ready-to-validate badge path remains dominant.

Validated:

- active operation disappears from the official model;
- pulse remains stopped;
- ready icon disappears through existing M045E/M045F behavior.

## M045E/M045F Compatibility

Recent CL work in the same area is preserved:

- `ReadyToCompleteOfficialUpgradeHotspotIdForExternalHost()`
- `TryCompleteReadyBuildingUpgradeOnTapForExternalHost(...)`
- `DrawBuildingUpgradeReadyBadgeForExternalHost(...)`
- `BuildingInteractionController.InteractionPreemptionHook`
- ready badge rendering in `HiveMapBuildingUpgradeVisualStateBootstrap.OnGUI()`

M046B does not change the ready badge, validation priority, server completion path or click preemption behavior. It only changes the running-outline visual state.

## Selection Priority

Selection and upgrade remain separate `BuildingSelectionHighlight` instances:

- selection keeps the default gold contour and default `2f` width;
- active upgrade uses the same shader with the existing upgrade blue and thicker per-instance width;
- building artwork is not scaled, blinked, recolored or replaced.

No material-state thrashing was introduced: the running upgrade highlight updates the cached material on its own component instance.

## Tests

Updated:

- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveBuildingUpgradeTests.cs`

Coverage:

- running official upgrade enables the active-upgrade hotspot;
- awaiting-completion disables the pulse source;
- idle disables the pulse source;
- alpha stays within visible bounds;
- outline width never falls below the stronger minimum;
- width pulse has a meaningful amplitude;
- RGB remains the existing upgrade blue.

## Verification

Compilation:

- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal /clp:ErrorsOnly`
  - Passed: 0 errors, 261 warnings.
- First parallel `Assembly-CSharp-Editor` build hit a temporary file lock in `Temp/obj`.
- Retried editor build alone:
  - `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal /clp:ErrorsOnly`
  - Passed: 0 errors, 141 warnings.

Working tree / conflict notes:

- `git status --short` showed existing CL/M045 files and shared modified files.
- M046B preserved the M045E/M045F validation-ready code in `HiveMapBuildingUpgradeVisualStateBootstrap`.
- No reset, stash, clean, checkout, commit or push was performed.
- Unrelated M045D/M045E reports were not edited.

Play Mode:

- Not visually certified by Codex in this pass.
- CEO runtime feedback from M046 was used to identify the visibility failure.
- M046B is ready for CEO visual certification at normal HiveMap zoom.

## Files Changed

- `Assets/BeeKingdom/Buildings/Interaction/BuildingSelectionHighlight.cs`
- `Assets/BeeKingdom/Playground/HiveMapBuildingUpgradeVisualStateBootstrap.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveBuildingUpgradeTests.cs`
- `Docs/AI/Missions/M046B-CX-Upgrade-Outline-Visibility-Tuning.md`

## CEO Visual Certification

Please inspect one building with a real active upgrade at normal HiveMap zoom:

1. Confirm the outline is clearly thicker than before.
2. Confirm the pulse is noticeable within 1-2 seconds.
3. Confirm it remains premium/subtle, not alarm-like.
4. Confirm the building artwork itself stays static.
5. Confirm the outline follows the silhouette cleanly.
6. Confirm visibility at moderate zoom out.
7. Confirm no giant halo appears.
8. Confirm selection still works.
9. Let the operation reach `AwaitingCompletion`.
10. Confirm the pulse stops and the ready-validation icon remains dominant.

## Acceptance Checklist

A. Why previous pulse was not perceptible proven? YES  
B. Existing outline system reused? YES  
C. Base outline made visibly thicker? YES  
D. Pulse clearly perceptible at normal zoom? NO, pending CEO visual confirmation  
E. Minimum pulse state still visible? YES by configured bounds; pending visual confirmation  
F. Existing upgrade blue preserved? YES  
G. Building artwork itself remains static? YES  
H. AwaitingCompletion stops pulse? YES  
I. Ready-validation indicator preserved? YES  
J. Selection interaction preserved? YES by architecture; pending visual confirmation  
K. Mobile-safe implementation? YES  
L. No per-frame material instantiation? YES  
M. Unity compile green? YES  
N. Play Mode visually verified? NO  
O. READY FOR CEO VISUAL CERTIFICATION? YES

Final action: inspect the actively upgrading building at normal HiveMap zoom before declaring final visual PASS.
