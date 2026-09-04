# M046C-CX - Upgrade Pulse Perceptual Amplitude

Date: 2026-09-04  
Agent: Architecte / Codex  
Repository: `C:/projets/beekingdomgame-master`

## Objective

CEO tested M046B in real Play Mode and confirmed the base outline is now clearly visible, but the pulse remains practically invisible, even when zooming in.

M046C keeps the accepted thicker baseline and increases only the perceptual amplitude of the existing running-upgrade outline.

No architecture replacement, gameplay change, building animation, particles or new outline system was introduced.

## Runtime Failure Acknowledged

M046B values were mathematically valid but visually insufficient:

- alpha: `0.86 -> 1.00`
- width: `4.5 -> 5.75` texels
- period: `1.45s`

Human runtime certification overrides the previous proof bounds. The failure was amplitude, not lifecycle or routing.

## Existing System Preserved

The active-upgrade outline still uses the same chain:

1. `HiveViewProductUiPresenter.ActiveOfficialUpgradeHotspotIdForExternalHost()`
2. `HiveMapBuildingUpgradeVisualStateBootstrap.Update()`
3. `BuildingCatalog.TryGetByLegacyKey(...)`
4. `BuildingInteractionController.Registry.GetGameObjectByBuildingType(...)`
5. `BuildingSelectionHighlight.Show(...)`
6. `ArtworkOutline.shader`

`BuildingInteractionController.InteractionPreemptionHook` and the M045F click-priority path were not modified.

## M046C Tuning

The minimum state remains the accepted M046B baseline readability:

- alpha min: `0.88`
- outline width min: `4.5f` texels
- intensity min: `1.0f`

The peak is now deliberately stronger:

- alpha max: `1.0`
- outline width max: `8.0f` texels
- intensity max: `1.48f`

Period:

- `1.35s`

The pulse now drives three visual channels on the same cached outline material:

- opacity;
- silhouette-band width;
- same-blue RGB intensity through `_Intensity`.

The semantic color remains BeeKingdom upgrade blue. The shader multiplies the existing `_Color.rgb` by `_Intensity`; it does not introduce a white flash, cyan swap, bloom, particles or a shield effect.

## Shader Change

`ArtworkOutline.shader` gained one optional property:

- `_Intensity ("Outline Intensity", Float) = 1`

Default value is `1`, so normal selection/highlight callers keep their existing look unless they explicitly set another value.

The fragment shader still discards non-edge pixels and returns only the silhouette contour.

## BuildingSelectionHighlight Change

`BuildingSelectionHighlight` now supports:

- `OutlineIntensity`
- `CurrentIntensityForProof`
- `SetVisualState(float alpha, float outlineWidthTexels, float intensity)`

The material remains cached per highlight instance. M046C updates material properties only; it does not instantiate materials per frame.

## State Lifecycle

Unchanged from M046/M046B:

- `Running`: strong blue breathing outline.
- `AwaitingCompletion`: pulse source returns null; pulse stops immediately.
- `Validated`: active operation disappears; outline remains stopped.
- `Idle`: no active upgrade outline.

## Tests

Updated:

- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveBuildingUpgradeTests.cs`

Proof bounds now cover:

- running-only source;
- awaiting-completion stop;
- alpha range;
- width range and substantial amplitude;
- intensity range and substantial amplitude;
- preserved RGB language.

Tests do not claim visual PASS.

## Verification

Compilation:

- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal /clp:ErrorsOnly`
  - Passed: 0 errors, 261 warnings.
- First parallel editor build hit the known temporary lock in `Temp/obj`.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal /clp:ErrorsOnly`
  - Passed after rerun alone: 0 errors, 141 warnings.

No commit and no push were performed.

## CEO Visual Retest

Final visual PASS belongs to CEO.

Retest at normal HiveMap gameplay zoom with a real active upgrade:

1. Base outline remains clearly visible.
2. Pulse is obvious within one cycle.
3. Pulse is significantly more noticeable than M046B.
4. Effect remains premium rather than alarm-like.
5. No giant halo or neon shield appears.
6. Building artwork remains static.
7. AwaitingCompletion stops the pulse.
8. Ready-validation icon remains dominant.

## Acceptance Checklist

A. M046B runtime failure acknowledged? YES  
B. Accepted thicker baseline preserved? YES  
C. Pulse amplitude substantially increased? YES  
D. Width variation substantially increased? YES  
E. Peak visibly stronger than baseline by design? YES  
F. Same BeeKingdom blue language preserved? YES  
G. No giant glow/neon effect introduced? YES  
H. Building artwork remains static? YES  
I. AwaitingCompletion lifecycle preserved? YES  
J. M045F click routing untouched? YES  
K. Mobile-safe? YES  
L. Unity compile green? YES  
M. READY FOR CEO VISUAL RETEST? YES

