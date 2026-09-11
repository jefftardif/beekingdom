# M049B-CL — Operation Completion World Feedback

Replaces Construction's temporary code-generated ready icon with the real
`upgrade_ready.png` asset, adds a matching world completion indicator for
Research, and generalizes the M045F click-priority mechanism so both can
share it instead of Research needing a second, competing click router.

## 1. Construction — real asset, not a placeholder

`DrawBuildingUpgradeReadyBadgeForExternalHost` (the world-space "ready to
validate" badge, M045E) used to pass the icon key `"upgrade-ready"` (hyphen)
to `DrawGameIcon`. `GetIconTexture` tries `Resources.Load<Texture2D>(
"PremiumBeeIcons/" + key)` first - but the real asset on disk is named
`upgrade_ready.png` (underscore), so that lookup always missed and silently
fell through to `IsPremiumRuntimeIcon("upgrade-ready") == true` ->
`CreateIconTexture` - the code-drawn hexagon/chevron placeholder Jeff flagged.

Fix: the badge now passes `"upgrade_ready"` (matching the real filename), so
`Resources.Load` finds the actual asset on the first try and it is cached
into `PremiumIconKeys` exactly like `troops-ready.png` already is for the
Barrack - same existing mechanism, no new icon-loading code, nothing
generated/recolored. `ScaleMode.ScaleToFit` (already in `DrawGameIcon`)
keeps the real aspect ratio. No other change to `DrawBuildingUpgradeReadyBadgeForExternalHost`'s
positioning, glow, or pulse - only which texture gets drawn.

The legacy `DrawBuildingUpgradeReadyMarkers` code path (its own comment: "the
legacy reference-hotspot renderer (LivingHive-only, never reachable from
HiveMap)") was intentionally left untouched - CLAUDE.md bans LivingHive.unity
entirely, and that renderer is dead weight for the real HiveMap screen Jeff
actually uses.

## 2. Shared rendering, generalized instead of duplicated

`DrawBuildingUpgradeReadyBadgeForExternalHost` was split into a thin gate
(`ReadyToCompleteOfficialUpgradeHotspotIdForExternalHost() != null`) plus a
new shared `DrawOperationCompletionBadge(rect, time, glowSize, iconKey)` -
the exact same pulse math, glow, and `DrawGameIcon` call as before, just
parameterized by icon key. A new `DrawResearchReadyBadgeForExternalHost`
gates on Research's own readiness and calls the same shared renderer with
`"research_ready"`. One rendering architecture, two thin callers - not two
badge systems.

## 3. Research gets its own real asset - not a placeholder reuse

The mission's own fallback policy allowed temporarily reusing
`upgrade_ready.png` for Research if no dedicated asset existed yet. That
turned out to already be moot: `Assets/BeeKingdom/Playground/Resources/
PremiumBeeIcons/research_ready.png` is already present on disk (a matching
"open book + checkmark" emerald-themed asset, not yet committed to git - the
`.png` itself is gitignored like all `PremiumBeeIcons/*.png`, only its
`.meta` shows as new in `git status`). Research's badge uses that real,
dedicated asset directly via the exact same `GetIconTexture`/`Resources.Load`
path as Construction - no placeholder reuse needed, and nothing generated.

## 4. Research completion accessors (new, mirror Construction's shape)

- `ReadyToCompleteOfficialResearchForExternalHost()` - real
  `ActiveOperation.IsAwaitingCompletion` gate, returns the awaiting
  `ResearchId` or null. Mirrors `ReadyToCompleteOfficialUpgradeHotspotIdForExternalHost`.
- `TryCompleteReadyResearchOnTapForExternalHost()` - calls
  `RunOfficialResearchAction(researchId)`, the exact same private method the
  Research screen's own "Terminer" button already calls (confirmed at both
  its screen call sites). No second completion implementation, no local
  unlock, no timer manipulation.

## 5. Click routing generalized, not duplicated

`BuildingInteractionController.InteractionPreemptionHook` (M045F) stays a
single `Func<BuildingDefinition,bool>`, still the only thing `DispatchClick`
consults. New `RegisterCompletionPreemption`/`UnregisterCompletionPreemption`
let more than one contextual-completion handler share it: each call adds
its own small predicate to an internal list; the hook itself becomes one
small dispatcher that stops at the first handler reporting it consumed the
click. Construction's bootstrap now calls `RegisterCompletionPreemption`
instead of assigning the hook directly (and unregisters on `OnDestroy`);
Research's new bootstrap registers its own handler the same way. Direct
assignment of `InteractionPreemptionHook` (what the existing M045F tests do)
still works unchanged and bypasses the list entirely - both are supported.

Research's handler (`TryCompleteReadyResearchOnClick`, mirrors Construction's
`TryCompleteReadyUpgradeOnClick` exactly): matches only
`BuildingTypes.Research`, checks the real awaiting-completion gate, calls
`TryCompleteReadyResearchOnTapForExternalHost()`, and always returns `true`
once matched - consuming the click even if the real completion call fails
(the failure is surfaced/logged by `RunOfficialResearchAction`'s own error
handling, the operation stays `AwaitingCompletion`, next click retries).

## 6. In-flight guard

No new debounce state was added. `RunOfficialResearchAction` is already
gated by `OfficialResearchActionEnabled`, which checks
`researchController.IsBusy` - and `IsBusy` flips `true` synchronously the
instant `Complete()` is called, before any `await` (confirmed by reading
`HiveResearchController.CompleteCoreAsync`). A rapid second tap while the
first completion is still in flight still passes the world-click's own
"is it ready" gate (the server hasn't answered yet) but is a no-op once it
reaches `RunOfficialResearchAction`, exactly like Construction's identical,
already CEO-tested pattern. Documented here rather than duplicated as new
state, per the mission's own "generalize, don't duplicate" direction.

## 7. Research bootstrap additions

`HiveMapResearchVisualStateBootstrap.cs` (M049's pulse-only file) gained,
this mission: `RegisterCompletionPreemption` installation (once, in
`Update()`), `TryCompleteReadyResearchOnClick`, an `OnGUI()` that gates on
`HiveMapOverlayInputGateBootstrap.IsAnyOverlayBlocking()` (same M044
occlusion gate Construction's own `OnGUI` already uses) before drawing
`DrawResearchReadyBadgeForExternalHost`, and its own small `ScreenRectFor`
collider-projection helper (a direct copy of Construction's - each bootstrap
owns this utility independently; the *shared* architecture is the badge
renderer and the click router, not this projection math). `OnDestroy`
unregisters the handler.

The M049 pulse logic itself was not touched: `Update()` still only shows
the emerald pulse while `IsOfficialResearchRunningForExternalHost()` is
true, which is already false during `AwaitingCompletion` - so the pulse and
the new badge are structurally mutually exclusive, exactly as required,
with no new flag needed.

## 8. Lifecycle (Research)

Idle -> nothing. Running -> emerald pulse (M049, unchanged). Research window
open/closed -> no effect on either pulse or badge (both read only real
operation state). AwaitingCompletion -> pulse stops (existing M049 logic),
badge appears (new). Click anywhere on the Research building while the badge
is showing -> real completion, click consumed, Research UI does not open.
Validated -> `ActiveOperation` clears/changes, badge's own gate goes false
next frame, building returns to normal click-to-open. New research started
-> pulse resumes.

## 9. Compatibility

- Alliance Help (Construction/Research/Training, M045-M048): untouched -
  no Help code opened this mission.
- Player Profile / M047 (CX): `HiveViewProductUiPresenter.cs` still carries
  concurrent CX changes (confirmed via `git status`/`git diff` before
  editing); this mission's edits were small, additive, and placed away from
  Player Profile code. No reset/stash/checkout/clean was performed.
- M046/M046C Construction blue pulse tuning: not opened this mission.
- M049 Research green pulse: not retuned: `BuildingActivityPulse.cs` and
  `HiveMapResearchVisualStateBootstrap.cs`'s existing pulse logic are
  unchanged; only new methods were appended.
- Queue sidebar: not touched - world indication is additive only.
- No server/SQL work: purely Unity-client-side.

## 10. Compile and tests

Unity compile: **clean** (`assets-refresh` after all edits reported
`Success`; one real error was caught and fixed along the way - adding
`using System;` to the click-priority test file for the new
`Func<BuildingDefinition,bool>` locals exposed the same bare-`Object`
ambiguity class as the earlier `LivingHiveMenuCanvas.cs` fix, in that file's
own pre-existing `Object.DestroyImmediate` call; fixed by qualifying it as
`UnityEngine.Object.DestroyImmediate`).

**Tests could not be executed this pass.** Console logs show the Unity
Editor was in Play Mode when the EditMode run was requested; Unity's own
test framework refuses scene setup/restore during Play Mode
(`InvalidOperationException: This cannot be used during play mode`) and its
internal "run in progress" state did not clear afterward - every retry since
returns "another test run is already in progress" against the same stuck
request id. The same exact failure signature is also present in the
project's logs from earlier today (11:06), unrelated to this mission, so
this is a pre-existing, self-contained failure mode of the MCP test-runner
bridge colliding with Play Mode - not something this mission's code caused,
and Unity's own SceneManager API refused the risky calls rather than
executing them, so there is no indication your live Play Mode session itself
was disrupted. I stopped retrying rather than keep hammering a stuck lock.

11 focused tests were added/extended in
`Assets/BeeKingdom/Tests/Editor/Interaction/BuildingInteractionControllerClickPriorityTests.cs`
(the 6 existing M045F tests, unmodified, plus 4 new ones covering
`RegisterCompletionPreemption`/`UnregisterCompletionPreemption`: two
independent handlers coexisting without interference, duplicate
registration being a no-op, unregistering one handler leaving the other
intact, and the hook clearing once the last handler is removed) - written
and self-reviewed, not yet machine-verified. The remaining test items from
the mission's list that require live Research/Construction operation state
(pulse-vs-badge exclusivity, real completion call routing, rapid-tap
behavior against the live server) were already understood to require a real
Play Mode session per this project's established cross-assembly testing
constraint, and are part of your own Stage 1-3 certification below.

## 11. What still needs your eyes

- **Dedicated Research completion asset**: exists already
  (`research_ready.png`), so the mission's Alpha fallback (reusing
  `upgrade_ready.png` for Research) was not needed. Nothing further to
  commission unless you want a different final design.
- Once you're free to leave Play Mode (or the next time the Editor restarts),
  ask me to run `BuildingInteractionControllerClickPriorityTests` and the
  Unity compile check again before certifying - I could not confirm the new
  tests are green this pass.

---

## Final checklist

| # | Question | Answer |
|---|---|---|
| A | Construction badge uses the real `upgrade_ready.png` asset (not code-generated)? | YES |
| B | Construction: all M045E/M045F behavior preserved exactly? | YES - only the icon key changed |
| C | Research: new world completion indicator added, reusing Construction's rendering architecture? | YES - shared `DrawOperationCompletionBadge` |
| D | Research indicator uses a dedicated real asset (not the Alpha `upgrade_ready.png` placeholder)? | YES - `research_ready.png` already exists |
| E | Research whole-building click validates via the real "Terminer" server action? | YES - `RunOfficialResearchAction`, same method the screen's own button calls |
| F | Click on Research while AwaitingCompletion does not also open the Research UI? | YES - consumed via `InteractionPreemptionHook` |
| G | Normal click on Research (not AwaitingCompletion) still opens the Research UI? | YES - handler falls through unchanged |
| H | Click-priority mechanism generalized, not a second competing router? | YES - `RegisterCompletionPreemption` |
| I | Priority: completion-ready action beats normal building interaction? | YES |
| J | Failed completion still consumes the click, never falls through? | YES - handler always returns true once matched |
| K | In-flight guard against rapid repeated taps? | YES - reuses `researchController.IsBusy`, same mechanism Construction relies on |
| L | M049 green pulse preserved (Running only, never with the badge)? | YES - untouched, structurally exclusive |
| M | M046 blue pulse preserved? | YES - untouched |
| N | Queue sidebar kept, world indication additive only? | YES |
| O | M044 occlusion respected by both indicators? | YES - both use `HiveMapOverlayInputGateBootstrap.IsAnyOverlayBlocking()` |
| P | Player Profile / concurrent CX work preserved? | YES |
| Q | Alliance Help untouched? | YES |
| R | Unity-only, no server/SQL work? | YES |
| S | Focused tests written? | YES (4 new + 6 existing preserved) - **not machine-verified this pass**, see section 10 |
| T | READY FOR CEO STAGE 1? | **NOT YET** - Unity test run is blocked by a stuck MCP test-runner lock; ask me to verify compile+tests once you can free the Editor briefly |
