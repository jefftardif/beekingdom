# M046-CX - Building Upgrade Active Outline Pulse

Date: 2026-09-04  
Agent: Architecte / Codex  
Repository: `C:/projets/beekingdomgame-master`

## Objective

Make the existing thin blue outline around a building with an active upgrade feel alive through a subtle continuous pulse.

This is presentation only. No gameplay state, timer, operation mutation, server contract, SQL, auth, FTUE, WorldMap, terrain, scene, image or build setting was changed.

## Existing Outline Implementation

Component:

- `Assets/BeeKingdom/Buildings/Interaction/BuildingSelectionHighlight.cs`

Upgrade driver:

- `Assets/BeeKingdom/Playground/HiveMapBuildingUpgradeVisualStateBootstrap.cs`

State source:

- `HiveViewProductUiPresenter.ActiveOfficialUpgradeHotspotIdForExternalHost()`
- Backed by `HiveBuildingUpgradeScreenModel.ActiveOperation`
- Now gated to `HiveBuildingUpgradeClient.RunningStatus` only.

Material:

- Created once per `BuildingSelectionHighlight` instance.
- Stored in the component field `_material`.
- Assigned to the cloned silhouette renderer through `renderer.sharedMaterial = GetOutlineMaterial(texture)`.
- Not recreated per frame.

Shader:

- `Assets/Experiments/Environment2D5D/Shaders/ArtworkOutline.shader`
- Shader name: `BeeKingdom/Experiments/ArtworkOutline`

Shader properties used:

- `_MainTex`: source artwork texture.
- `_Color`: outline color and alpha.
- `_OutlineWidth`: current fixed value `2f`.
- `_AlphaCutoff`: current fixed value `8 / 255`.

Current upgrade color preserved:

- `new Color(0.35f, 0.75f, 1f, 1f)`

Current width preserved:

- `2f` texels.

Render queue:

- `3001`.

How activation works:

1. `HiveMapBuildingUpgradeVisualStateBootstrap.Update()` runs in HiveMap scenes.
2. It reads the active official upgrade hotspot from `HiveViewProductUiPresenter`.
3. It resolves the legacy hotspot through `BuildingCatalog.TryGetByLegacyKey(...)`.
4. It gets the scene GameObject via `BuildingInteractionController.Registry.GetGameObjectByBuildingType(...)`.
5. It adds/reuses `BuildingSelectionHighlight` on the target.
6. It sets `TintColor` to the existing blue.
7. It calls `Show(...)`, which clones the building artwork mesh and draws only the silhouette edge through `ArtworkOutline.shader`.

## Implementation

Pulse values are centralized in `HiveMapBuildingUpgradeVisualStateBootstrap`:

- period: `1.9s`;
- alpha min: `0.74`;
- alpha max: `1.0`;
- easing: `Mathf.SmoothStep`;
- wave: sine, continuous.

The pulse changes only the material alpha through the existing `BuildingSelectionHighlight.SetAlpha(...)` method.

No large glow, ring, particles, text, building scale, building blink, art mutation or new outline system was added.

## Lifecycle Rule

Idle:

- no active official upgrade operation;
- no upgrade outline pulse.

Running:

- active operation status is exactly `HiveBuildingUpgradeClient.RunningStatus`;
- existing blue outline appears and pulses.

Awaiting completion:

- active operation exists but status is `HiveBuildingUpgradeClient.AwaitingCompletionStatus`;
- pulse stops so the existing ready-to-validate feedback can remain the dominant completion signal.

Completed/validated:

- active operation is null after server completion;
- pulse stops.

## Selection Interaction

The upgrade pulse remains a separate `BuildingSelectionHighlight` instance attached to the upgrading building. The normal selection feedback remains driven by `BuildingSelectionFeedback`.

This preserves the existing rule:

- selection stays readable;
- upgrade state stays readable;
- neither path mutates building artwork;
- both reuse the same silhouette shader technique.

## Performance

The implementation is mobile-safe within the existing architecture:

- no per-frame material creation;
- no per-frame `Show(...)`;
- no per-frame mesh cloning;
- no new particles;
- no full-scene material scan;
- no shader variant generation;
- only a cached highlight alpha update while the same building remains active.

The only existing lookup remains the bootstrap's cached-controller path and the existing active-operation read.

## Tests Added

Updated:

- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveBuildingUpgradeTests.cs`

Coverage:

- running official upgrade returns the hotspot for pulse;
- awaiting-completion operation returns null so pulse stops;
- idle official state returns null;
- pulse alpha remains subtle and bounded;
- pulse color preserves the existing blue RGB.

## Verification

Compile:

- `dotnet restore Assembly-CSharp.csproj` passed after `Temp/obj` cache was missing.
- `dotnet restore Assembly-CSharp-Editor.csproj` passed after a parallel restore collision.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal /clp:ErrorsOnly` passed: 0 errors, 346 warnings.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal /clp:ErrorsOnly` passed: 0 errors, 141 warnings.

Conflict check:

- `git status --short` showed the M046 touched files plus an unrelated untracked `Docs/AI/Missions/M045D-CL-Construction-Help-Button-Runtime-Fix.md`.
- M045D was not touched.
- No overwrite/reset/stash/clean/checkout was performed.

Play Mode:

- Not visually executed in this pass.
- No visual PASS is claimed from compilation.

## Files Changed

- `Assets/BeeKingdom/Buildings/Interaction/BuildingSelectionHighlight.cs`
- `Assets/BeeKingdom/Playground/HiveMapBuildingUpgradeVisualStateBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveBuildingUpgradeTests.cs`
- `Docs/AI/Missions/M046-CX-Building-Upgrade-Outline-Pulse.md`

## CEO Visual Certification Steps

Use a building with a real active official upgrade:

1. Open HiveMap in Play Mode.
2. Start or observe a real active upgrade operation.
3. Confirm idle buildings have no upgrade pulse.
4. Confirm the upgrading building keeps the same thin blue outline.
5. Confirm only the outline alpha pulses gently.
6. Confirm the building artwork itself does not pulse, blink or scale.
7. Zoom in and out.
8. Pan the hive.
9. Open and close the building UI.
10. Select the building and another building to inspect selection interaction.
11. Wait for the operation to reach completion-ready.
12. Confirm the pulse stops and the existing ready-to-validate feedback remains clear.
13. Complete/validate the operation.
14. Confirm the pulse remains stopped.

## Acceptance Checklist

A. Existing outline implementation identified? YES  
B. Reused existing outline system? YES  
C. Pulse affects outline only? YES  
D. Existing blue preserved? YES  
E. Real active upgrade state drives pulse? YES  
F. Pulse stops after upgrade ends? YES  
G. Completion-ready feedback preserved? YES  
H. Selection/highlight regression-free? YES by architecture and compile-time coverage; pending visual confirmation  
I. Mobile-safe implementation? YES  
J. No per-frame material instantiation? YES  
K. Multiple active buildings supported? YES for the current single-operation server contract; future multi-upgrade would require the same per-building component pattern over a collection  
L. Unity compile green? YES  
M. Play Mode visually verified? NO  
N. Conflict with CL current work? NO detected; unrelated M045D file was left untouched  
O. READY FOR CEO VISUAL CERTIFICATION? YES

Final action: CEO should inspect one actively upgrading building in Play Mode before final visual PASS.
