# M049-CL — Research Green Activity Pulse

Extends BeeKingdom's building-activity visual language (blue = Construction
upgrading) to Research, using a new emerald-green pulse driven by the real
server-authoritative Research operation state.

## 1. Architecture — generalized, not duplicated

New `Assets/BeeKingdom/Playground/BuildingActivityPulse.cs` — a small,
immutable struct (not a MonoBehaviour, not a framework) holding one
activity's color + alpha/width/intensity/period pulse parameters, plus the
exact easing curve already tuned and CEO-accepted for Construction
(M046C). It exposes `Configure(highlight)` (one-time setup) and
`Apply(highlight, time)` (per-frame), both operating on the existing,
reused `BuildingSelectionHighlight` component - the same silhouette-outline
technique Construction and normal building selection already use.

`BuildingActivityPulse.Construction` and `BuildingActivityPulse.Research`
are the only two presets today. A future Training/Healing/Manufacturing
pulse only needs a third preset + its own small bootstrap (not a new
system) - the struct is the reusable concept the mission asked for.

**Deliberately not touched**: `HiveMapBuildingUpgradeVisualStateBootstrap.cs`
(Construction's own bootstrap) was left 100% unmodified - it still uses
its own local constants, not `BuildingActivityPulse.Construction`. This
was a conscious choice: refactoring it to consume the new struct would
carry real risk of subtly changing CEO-approved, already-tested output for
zero functional gain, and the mission explicitly forbids touching M046
tuning or M045F. `BuildingActivityPulse.Construction`'s numbers are
verified (by test, see section 6) to exactly mirror the live bootstrap's
constants, so the two can never silently drift apart even though the code
paths remain separate.

## 2. New Research bootstrap

`Assets/BeeKingdom/Playground/HiveMapResearchVisualStateBootstrap.cs` -
structurally mirrors Construction's bootstrap's highlight-management
logic (create-if-needed / reuse / hide), but intentionally does NOT
include a click-preemption hook or a world completion-ready badge (out of
this mission's scope - see section 5).

Drives the pulse from a new accessor,
`HiveViewProductUiPresenter.IsOfficialResearchRunningForExternalHost()`,
which mirrors `ActiveOfficialUpgradeHotspotIdForExternalHost()`'s exact
shape: true only when a real `ActiveOperation.Status ==
HiveResearchClient.RunningStatus` - never `AwaitingCompletion`, never
inferred from whether the Research screen happens to be open.

The Research building is resolved through the existing mapping
architecture (`BuildingCatalog.TryGetByBuildingType(BuildingTypes.Research,
...)` + `controller.Registry.GetGameObjectByBuildingType(...)`) - no
hardcoded scene GameObject name. Research has exactly one physical
building regardless of which specific research (Danse des routes II, or
any other) is active, unlike Construction's per-hotspot model.

## 3. Color

`new Color(0.16f, 0.86f, 0.52f, 1f)` - emerald/living green: green
channel dominant but with enough blue to read as a jewel-toned emerald
rather than a flat lime or acid-green alarm color, verified by test
(section 6). Same alpha (0.88→1.0), outline width (4.5→8 texels), and
intensity (1.0→1.48) spans and the same 1.35s period as Construction's
current, CEO-accepted pulse - only the hue differs, per the mission's
explicit "same perceptual strength" requirement.

## 4. Lifecycle

Matches the mission's specification exactly, by construction:

- Idle → no pulse (bootstrap never creates a highlight).
- Running → pulse visible (real operation state, re-checked every frame).
- Research window closed → pulse continues (state check has no UI
  dependency at all - `IsOfficialResearchRunningForExternalHost()` never
  reads any screen/overlay-open flag).
- Pan/zoom → continues (the highlight is a real child GameObject of the
  building, not a screen-space overlay).
- AwaitingCompletion → pulse stops (`Status != RunningStatus` → `running
  = false` → `highlight.Hide()`).
- New research started → pulse resumes (fresh `Running` status detected
  next frame).

Construction and Research can pulse simultaneously: two independent
bootstraps, each owning its own `BuildingSelectionHighlight` instance on
its own building - no shared/global state, no "one active pulse" limit.

## 5. Completion-ready indicator gap (reported, not built)

Research currently has **no HiveMap world completion-ready
indicator/click-to-validate** analogous to Construction's
`DrawBuildingUpgradeReadyBadgeForExternalHost`/`TryCompleteReadyBuildingUpgradeOnTapForExternalHost`
(M045E/F) - confirmed by direct search, nothing found. The real "Étude
terminée · validation serveur requise" status text and "Terminer" button
already exist inside the Research screen itself and were not touched.
Per this mission's instruction, no new visual/interaction was invented to
fill this gap - flagged here as a real, separate decision for later
(should Research get the same building-click validation convention as
Construction?).

## 6. Tests

`Assets/BeeKingdom/Playground/Editor/BuildingActivityPulseTests.cs` - 6
tests against `BuildingActivityPulse` directly (a plain struct, no scene
dependency):

1. `ResearchPresetIsEmeraldGreen_NotConstructionBlue`
2. `ResearchPresetIsNotNeonLimeOrToxicGreen` - direct guardrail against
   the mission's explicit "not fluorescent lime/toxic green/neon" taste rule.
3. `ResearchAndConstruction_ShareTheSameAmplitudeSpans`
4. `ConstructionPreset_MatchesTheCurrentAcceptedBootstrapConstants` -
   proves `BuildingActivityPulse.Construction`'s numbers exactly mirror
   the live, untouched bootstrap's own constants.
5. `PulseValues_StayWithinConfiguredBoundsAcrossAFullPeriod`
6. `PulseIsClearlyPerceptible_NotATimidVariation` - direct regression
   against the mission-quoted M046 lesson (first Construction pulse was
   "technically working but practically invisible").

Result: **6/6 green**. Re-ran the existing Construction pulse test suite
(`SandboxLivingHiveBuildingUpgradeTests`, CX's) for regression: **9/10
green**, the one failure (`MonotonicProjectionNeverAuthorizesCompletion`,
an `Is.Zero`/`TimeSpan` NUnit assertion-shape issue) is the same
pre-existing, unrelated failure already documented in M045D/E/F/G -
unaffected by this mission, not this mission's file to fix.

**Not testable from this test layer** (same documented architecture
constraint as every other mission touching this presenter):
`IsOfficialResearchRunningForExternalHost()`'s live behavior and the
bootstrap's per-frame building resolution both depend on
`HiveViewProductUiPresenter`'s runtime static controller state, reachable
only in a live Play Mode session.

## 7. Runtime observation

Play Mode was not active at the time of this investigation (checked via
`editor-application-get-state`), so no live visual observation of the CEO's
real Research operation was possible or attempted. Nothing was fabricated
in its place - final visual certification is explicitly left to the CEO,
as required.

## 8. Compatibility

- Alliance Help Research (M048, just human-certified): untouched. No
  Research duration/cost/prerequisite/completion-semantics/server contract
  was read for writing, let alone modified.
- M045F click-preemption (`BuildingInteractionController.InteractionPreemptionHook`):
  not opened this mission.
- M046/M046C Construction pulse tuning: not opened this mission (see
  section 1).
- M047 Player Profile: `HiveViewProductUiPresenter.cs` had concurrent M047
  edits throughout (confirmed via `git status`/`git diff` before and
  during); this mission's own edit there was a small, additive new method
  appended near `OfficialResearchModel()`, applied cleanly without
  touching Player Profile code. No reset/stash/checkout/clean was performed.
- Also fixed, separately from this mission's own scope but blocking the
  CEO from entering Play Mode at all: a `CS0104` ambiguous-`Object`
  compile error in `LivingHiveMenuCanvas.cs` (both `using System;` and
  `using UnityEngine;` present, bare `Object.Destroy`/`DestroyImmediate`
  calls) - fixed by qualifying as `UnityEngine.Object`, unrelated to this
  mission's own architecture.

## 9. Performance

No per-frame material creation or mesh cloning: `BuildingSelectionHighlight.Show()`
(existing, unchanged) only clones the silhouette mesh once per
create/change, exactly like Construction; the pulse's own per-frame cost
is `SetVisualState(...)`, a cached-material color/property write, same as
Construction. No scene-wide search per frame beyond the existing single
`FindFirstObjectByType<BuildingInteractionController>()` cached once, same
pattern every other HiveMap bootstrap already uses.

---

## Final checklist

| # | Question | Answer |
|---|---|---|
| A | Existing Construction pulse architecture reused/generalized? | YES — shared `BuildingActivityPulse` struct + `BuildingSelectionHighlight`, Construction's own file untouched by design |
| B | Duplicate Research-only pulse system avoided? | YES |
| C | Real authoritative Research Running state used? | YES |
| D | Research building resolved through existing mapping? | YES |
| E | Emerald-green visual language implemented? | YES |
| F | Pulse amplitude uses current perceptible Construction baseline? | YES — identical spans, verified by test |
| G | Research pulse continues with Research UI closed? | YES |
| H | AwaitingCompletion stops Research pulse? | YES |
| I | Construction blue pulse preserved? | YES — file untouched, tests green |
| J | Construction + Research simultaneous pulses supported? | YES |
| K | Selection behavior preserved? | YES — separate highlight instance, same proven pattern as Construction |
| L | Alliance Help Research untouched? | YES |
| M | Mobile-safe implementation? | YES |
| N | Unity compile green? | YES |
| O | Focused tests green? | YES (6/6 new; 9/10 Construction regression, 1 pre-existing unrelated) |
| P | Research world completion-ready indicator already exists? | **NO** — gap reported, not built (section 5) |
| Q | READY FOR CEO VISUAL CERTIFICATION? | YES |

READY FOR CEO — INSPECT RESEARCH BUILDING AT NORMAL HIVEMAP ZOOM.
