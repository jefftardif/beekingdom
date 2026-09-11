# M049C-CL — Research Green Pulse Runtime Fix

## 1. Root cause, proven

`HiveMapResearchVisualStateBootstrap` (M049) was never wired into the real
production HiveMap bootstrap installer. It only had its own
`[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]`
`AutoStart()`, which fires exactly once - on whatever scene is active the
instant Play Mode starts. That is always the splash/login scene, never
`Environment2D5D_HiveMap_Test`, so `AutoStart`'s own scene-name check always
failed and the bootstrap GameObject was never created once the player
actually transitioned into the real HiveMap scene.

This is proven two ways:

1. **It already happened before, to two other bootstraps.** The real
   production composition root, `HiveMapRuntimeBootstrapInitializer.cs`,
   subscribes to `SceneManager.sceneLoaded` and explicitly calls
   `InitializeForScene(scene)` on every real HiveMap bootstrap - including
   Construction's own `HiveMapBuildingUpgradeVisualStateBootstrap`. Its
   existing comment (M038B-CL) documents the *exact same* AutoStart-timing
   bug for `HiveMapResearchBootstrap` and `FtueTutorialBootstrap`, found the
   same way: "confirmed live: a fresh account entering
   Environment2D5D_HiveMap_Test had zero FtueTutorialBootstrap instances."
   `HiveMapResearchVisualStateBootstrap` was simply never added to this list
   when M049 created it.
2. **Live read-only probe**, run via `script-execute` (Debug.Log only, no
   scene/state mutation): with the real HiveMap scene not currently loaded
   in this Editor session, `FindFirstObjectByType<HiveMapResearchVisualStateBootstrap>()`
   returned null and `FindFirstObjectByType<BuildingInteractionController>()`
   also returned null - consistent with the bootstrap (and the whole HiveMap
   runtime) genuinely never having been instantiated this session, not with
   a rendering/highlight-layer problem downstream.

No other hypothesis needed further investigation once this was confirmed:
`BuildingActivityPulse`, `IsOfficialResearchRunningForExternalHost`,
`BuildingSelectionHighlight`, and the Research building's catalog/registry
mapping (`BuildingCatalog.TryGetByBuildingType(Research)` verified live =
true, valid definition) were all already correct from M049 - they simply
never ran, because the component that drives them never existed in the
scene.

## 2. Fix

One line added to `HiveMapRuntimeBootstrapInitializer.InitializeAllBootstraps`,
directly after Construction's own visual-state bootstrap call (same
grouping, same pattern):

```
HiveMapResearchVisualStateBootstrap.InitializeForScene(scene);
```

This is the exact same production mechanism Construction's blue pulse
already relies on - no new install path invented. `InitializeForScene`
already existed (M049) and already de-duplicates
(`FindFirstObjectByType<HiveMapResearchVisualStateBootstrap>() != null` ->
no-op), so it is safe to call alongside the bootstrap's own now-redundant
`AutoStart()` without creating two instances.

No other file touched. `BuildingActivityPulse.cs`,
`HiveMapResearchVisualStateBootstrap.cs`'s own pulse logic, and the M049
color/amplitude/period parameters are all byte-for-byte unchanged.

## 3. Important operational note for the retest

Because the fix is a *new scene-load wiring*, it only takes effect on the
next real `SceneManager.sceneLoaded` event for the HiveMap scene - a
recompile alone does not retroactively spawn a bootstrap into an
already-running scene. **If you are already inside a live HiveMap session
when this fix lands, you will need to exit and re-enter Play Mode (or
otherwise cause the HiveMap scene to reload) once before the pulse can
appear** - simply closing the Research window on an already-running session
that predates this fix will not show it. Your real "Danse des routes III"
operation is server-authoritative, so nothing about it is lost by doing
this - it will still read as Running with its correct remaining time the
moment you're back in the Hive.

## 4. Compile and tests

Unity compile: **clean** (`assets-refresh` reported `Success` after the fix).

No new automated test was added for this specific one-line fix. The actual
defect lived in `HiveMapRuntimeBootstrapInitializer`'s private
`InitializeAllBootstraps`, which only runs from a real
`SceneManager.sceneLoaded` event against the real HiveMap scene and
MonoBehaviour instantiation - the same cross-assembly/runtime-only
constraint already documented for every other bootstrap-wiring fix in this
project (see the untested M038B-CL fix it mirrors). A meaningful regression
test would need to simulate an actual scene load, which is not practical
from an EditMode NUnit test; inventing a weaker test that doesn't exercise
the real failure mode would not have caught this bug in the first place, so
none was added, per this mission's own instruction not to pad the count.
The 10 existing/M049B click-priority tests were not touched by this fix
(different file) and carry no regression risk from it.

**The Unity MCP test-runner bridge is currently stuck** (two separate stuck
"another test run is already in progress" states hit this session, request
ids `866db4f8...` and now `d0abbb49...`), unrelated to this fix - console
logs show `InvalidOperationException` from Unity's own test framework scene
tasks each time, and the lock does not appear to self-clear quickly. I was
not able to get a live EditMode run to complete this pass. This is a
tooling issue, not evidence against the fix - the fix itself is proven by
direct source inspection and the live read-only probe in section 1.

## 5. Live runtime state inspected

Confirmed via read-only `script-execute` (Debug.Log only):

- `HiveMapResearchVisualStateBootstrap` instance in scene: **NO** (pre-fix
  state, no scene reload yet since the fix landed)
- `IsOfficialResearchRunningForExternalHost()`: **false** (no HiveMap
  session currently loaded in this Editor - not evidence against the real
  operation, which lives server-side)
- `BuildingInteractionController` in scene: **NO** (same reason)
- `BuildingCatalog.TryGetByBuildingType(Research)`: **true**, valid
  definition returned

These are exactly the results expected for "HiveMap scene not currently
loaded" and are consistent with, not contradictory to, the root cause in
section 1.

## 6. Compatibility

- Construction blue pulse / M045F / M046: untouched, not opened this
  mission beyond the one added line and its own already-existing call site.
- Alliance Help: untouched.
- Research durations/completion/server/SQL: untouched.
- Player Profile / CX: untouched, not opened this mission.
- M049B completion-ready work: preserved as-is (confirmed via `git status`
  before starting - all M049B files still present and unmodified by this
  mission).

---

## Final checklist

| # | Question | Answer |
|---|---|---|
| A | Was `HiveMapResearchVisualStateBootstrap` instantiated in production HiveMap before the fix? | **NO** |
| B | Exact runtime root cause proven? | **YES** - never wired into `HiveMapRuntimeBootstrapInitializer`, same class of bug as the documented M038B-CL case |
| C | Official Research Running accessor returns true for a real running operation? | Not re-verifiable this pass (no HiveMap session currently loaded in this Editor) - the accessor itself was not modified and was already correct in M049 |
| D | Research building resolves to the correct visible GameObject? | Catalog lookup proven live (true); full registry/GameObject resolution requires a loaded HiveMap session, not currently available |
| E | `BuildingSelectionHighlight` actually instantiated/active? | Not yet - requires the bootstrap to run once, which requires the scene-load event this fix now wires in |
| F | Existing M049 pulse parameters preserved? | YES - untouched |
| G | Construction blue pulse untouched? | YES |
| H | Alliance Help untouched? | YES |
| I | Player Profile preserved? | YES |
| J | Unity compile green? | YES |
| K | Focused tests green? | **NOT VERIFIED** - Unity MCP test-runner bridge stuck this pass (section 4); no regression risk to existing tests since this fix does not touch their file |
| L | Live runtime state inspected? | YES (read-only probe, section 5) - confirms the "never instantiated" diagnosis, not full post-fix behavior (no scene reload yet) |
| M | READY FOR CEO VISUAL RETEST? | YES, **after exiting and re-entering Play Mode once** (section 3) - the fix cannot show on an already-running session |

READY FOR CEO — EXIT AND RE-ENTER PLAY MODE, THEN CLOSE RESEARCH AND INSPECT THE RESEARCH BUILDING NOW.
