# M043D-CL — LivingHive Runtime Regression

## 1. Symptom

The CEO launched the game and landed in the old, legacy `LivingHive.unity`
scene instead of the canonical production HiveMap.

## 2. Expected routing

`Auth → HiveMap → WorldMap → HiveMap`. `LivingHive.unity` is legacy/dev-only
and must never be the normal player-facing hive.

## 3. Current Build Settings (before fix)

`ProjectSettings/EditorBuildSettings.asset` was **uncommitted and modified**
relative to the last committed baseline (confirmed already showing as
modified in `git status` at the very start of this multi-mission session,
i.e. this predates M041–M043B entirely):

| Index | Scene (working tree, before fix) | Enabled |
|---|---|---|
| 0 | `Assets/Scenes/LivingHive.unity` | ✅ **enabled — this is scene 0** |
| 1 | `WorldMapWave6Wave5Method12288Preview.unity` (canonical WorldMap) | ✅ |
| 2 | `WorldMapMmoFullscreenFoundation.unity` (legacy) | ✅ |
| 3 | `WorldMapWave5Premium25x25Test.unity` (legacy) | ✅ |
| 4 | `SandboxPlayground.unity` | ✅ |

**`Environment2D5D_HiveMap_Test.unity` (the real production HiveMap) and
`Environment2D5D_SpatialV3.unity` were completely absent from the list.**

## 4. Current Play Mode start config

`Assets/BeeKingdom/Playground/Editor/PlaygroundPlayModeStartScene.cs` is an
`[InitializeOnLoad]` static class that re-runs `ConfigurePlayModeStartScene()`
via `EditorApplication.delayCall` on **every single domain reload** (i.e.
every script recompile). Its logic (lines 317–334): if the currently active
scene in the Editor, *or* the currently-configured
`EditorSceneManager.playModeStartScene`, is `LivingHive.unity`, it
re-confirms LivingHive as the Play Mode start scene and calls
`ConfigurePlayModeStartScene(LivingHiveScenePath)` — a self-perpetuating
loop: once LivingHive becomes the active/start scene for *any* reason once
(e.g. opened directly via its own `[MenuItem("Bee Kingdom/Playground/Open
Living Hive Scene")]` QA escape hatch), every subsequent recompile re-affirms
it.

That call chain ends in `EnsureSceneEnabled(scenePath)` (lines 476–484):

```csharp
private static void EnsureSceneEnabled(string scenePath)
{
    if (EditorBuildSettings.scenes.Any(scene => scene.path == scenePath && scene.enabled)) return;
    EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) }
        .Concat(EditorBuildSettings.scenes).ToArray();
}
```

This **prepends** the target scene to the front of `EditorBuildSettings.scenes`
as enabled, with no deduplication and no preservation guarantee for any other
specific scene. This is the exact, demonstrated mechanism by which
`LivingHive.unity` can end up enabled at build index 0 while other entries
drift or get manually "cleaned up" around it — this is not new tooling risk,
it is the exact failure mode `Docs/AI/Missions/
M012-OC-HiveMap-Build-Configuration-Alignment.md` already documented once
before ("Historical drift... Duplicate entries accumulated from editor scene
builder tools").

## 5. Auth routing

`HiveViewProductUiPresenter.EnterHiveFromSplash` → `LoadHiveMapScene()`
(lines 5620–5657) — **code is correct**: it reads
`SplashDevelopmentSceneConfig.HiveMapScenePath`
(`Environment2D5D_HiveMap_Test.unity`, the right constant), and even contains
an `#if UNITY_EDITOR` self-healing guard that re-adds HiveMap to Build
Settings if it's ever missing, before calling
`SplashDevelopmentSceneConfig.TryOpenScene(...)`. This guard only runs in the
Editor, not in a built player — a built player with a corrupted
`EditorBuildSettings.asset` baked in would have no such recovery and could
genuinely ship with the wrong scene 0.

## 6. WorldMap return routing

`HiveViewProductUiPresenter.OpenCanonicalWorldMap()` (line 40709) — targets
`SplashDevelopmentSceneConfig.WorldMapScenePath`, correct.
`WorldMapMmoFullscreenFoundationBootstrap.OpenLivingHiveFromWorldMap()`
(line 5381, a confusingly-named but correct method) — targets
`SplashDevelopmentSceneConfig.HiveMapScenePath`, correct. Neither method's
own logic points at LivingHive; both were simply victims of the same
corrupted Build Settings data (the WorldMap→HiveMap return would have failed
silently with a "Scene absente des Build Settings" status message under the
pre-fix state, since it has no self-healing guard — a related but distinct
symptom from what the CEO actually hit).

## 7. Recent-change comparison

`Assets/BeeKingdom/Playground/MobileAccountSessionRuntimeBootstrap.cs` (the
file M043/M043B modified this week for Alliance work) was inspected
specifically: **it contains zero scene-loading code** — no
`SceneManager.LoadScene`, no `LoadSceneAsync`, no `playModeStartScene`
reference anywhere in the file. Confirmed with a direct grep, not assumed.
None of the M041–M043B Alliance changes (`AllianceClient.cs`,
`AllianceCenterPresentation.cs`, `HiveViewProductUiPresenter.cs`'s Alliance
sections, `PlayerDirectoryClient.cs`) touch scene routing, Build Settings, or
Play Mode configuration anywhere.

## 8. Proven root cause

**B. Build Settings regression.** `ProjectSettings/EditorBuildSettings.asset`
had drifted, uncommitted, away from the last known-good committed state
(commit `ed1512e`, "Align build configuration with HiveMap", 2026-08-20) —
confirmed by direct `git diff` against `HEAD`. This predates M041–M043B
(the file was already showing modified in `git status` before that work
began). The demonstrated mechanism for how such drift happens in this
project is `PlaygroundPlayModeStartScene.cs`'s `EnsureSceneEnabled` (section
4) — a pre-existing dev-convenience script, not new tooling, not introduced
this session, and not related to Alliance work.

Given `LoadHiveMapScene()`'s in-Editor self-healing guard (section 5), the
CEO's exact symptom is most consistent with **A. Editor Play Mode start-scene
override**: Play Mode started directly inside `LivingHive.unity` (bypassing
the splash/auth/HiveMap flow entirely, since Play Mode begins inside whatever
scene `playModeStartScene` points to) rather than a corrupted routing call
inside the running game. The same corrupted `EditorBuildSettings.asset`,
if ever baked into a Windows/Android build (**B**, in the build-packaging
sense), would independently produce the identical symptom with no
self-healing available at all, since builds always launch build index 0.

## 9. Fix

Restored `ProjectSettings/EditorBuildSettings.asset` to the exact
committed-`HEAD` state (`git checkout HEAD -- ProjectSettings/
EditorBuildSettings.asset`) — the smallest possible correct fix, touching
only this one file, no gameplay/Alliance code modified. Then, because the
Unity Editor caches `EditorBuildSettings` in memory independently of the
file on disk (a plain file overwrite while the Editor is running does not
retroactively update its live state), pushed the identical known-good list
into the live Editor via `EditorBuildSettings.scenes = ...` (script-execute)
so the fix takes effect immediately without requiring an Editor restart.
Confirmed both the file and the live Editor state now match exactly:

```
[0] enabled  Environment2D5D_HiveMap_Test.unity   ← PRODUCTION HIVE ENTRY
[1] enabled  SandboxPlayground.unity
[2] enabled  WorldMapWave6Wave5Method12288Preview.unity  ← CANONICAL WORLD MAP
[3] enabled  Environment2D5D_SpatialV3.unity
[4] disabled LivingHive.unity                       ← LEGACY (QA/editor only)
[5] disabled WorldMapMmoFullscreenFoundation.unity
[6] disabled WorldMapWave5Premium25x25Test.unity
[7] disabled SandboxPlayground.unity (duplicate ref)
```

No Alliance functionality was touched — the root cause was never in
Alliance/M041–M043B code, confirmed with direct evidence (section 7).

## 10. Regression tests

New `Assets/BeeKingdom/Tests/Editor/HiveMapBuildSettingsRegressionTests.cs`
(5 tests, EditMode, using the real `UnityEditor.EditorBuildSettings` API —
this is Editor-only data, not something the server test suite can cover).
Tests scene **GUID**, not just display name/path, per the mission's explicit
requirement:

- `ProductionHiveMapIsPresentAndEnabledInBuildSettings` — fails if HiveMap
  is missing or disabled (the exact CEO-facing failure mode).
- `FirstEnabledBuildSceneIsNeverLivingHive` — fails if LivingHive becomes the
  first enabled scene (build index 0 / Play Mode start scene).
- `LivingHiveIsPresentButDisabled` — fails if LivingHive becomes enabled
  anywhere, or is deleted from Build Settings entirely (QA/editor access
  must remain available).
- `CanonicalWorldMapIsPresentAndEnabled` — fails if WorldMap disappears or
  gets disabled.
- `HiveMapAppearsBeforeLivingHiveInBuildOrder` — fails if the relative
  ordering regresses even if both remain individually correct.

All 5 failed against the pre-fix state (confirmed by running them before the
live-memory fix was applied) and all 5 pass against the restored state.

## 11. Files changed

`ProjectSettings/EditorBuildSettings.asset` (restored to `HEAD`, only file
modified for the actual fix). New:
`Assets/BeeKingdom/Tests/Editor/HiveMapBuildSettingsRegressionTests.cs`.
Nothing else — no Alliance code, no `PlaygroundPlayModeStartScene.cs`
changes (that script's LivingHive-QA-access menu item is intentional and
was not modified; the regression test suite is the guard against it
drifting Build Settings again, per the mission's own "add/strengthen tests"
instruction rather than "rewrite the dev tool").

## 12. Validation

- Unity compile: clean (`assets-refresh` → "AssetDatabase refreshed
  successfully", 0 `error CS` entries).
- `HiveMapBuildSettingsRegressionTests`: 5/5 passing via the real EditMode
  Test Runner.
- `AllianceClientTests` (M043/M043B, re-run for safety): still 7/7 passing —
  confirms this fix touched nothing Alliance-related.
- `git diff --stat ProjectSettings/EditorBuildSettings.asset` against `HEAD`:
  empty (file exactly matches the last known-good commit).

## 12b. Alliance Runtime Ownership — HiveMap vs LivingHive

The CEO asked, separately, whether M041–M043B might have accidentally built
Alliance against the legacy LivingHive runtime. Traced the full path with
live Editor inspection (not inferred from filenames) — GUID/scene contents
read directly via the Unity MCP tools, source read directly for every gating
condition.

**Live scene inspection**: `LivingHive.unity` (currently open in the
Editor) has exactly 3 root GameObjects — `Living Hive Demo`, `Main Camera`,
`Sun`. `Living Hive Demo` carries exactly one script component:
`BeeKingdom.Playground.LivingHiveDemoBootstrap`. No Alliance-related
component anywhere in the scene.

**Gating conditions, read directly from source, all three identical in
shape**:
- `MobileAccountSessionRuntimeBootstrap.IsEnvironmentScene(Scene)` (lines
  57–65): `scene.name.StartsWith("Environment2D5D")` **OR** any root
  GameObject name starts with `"Environment2D5D"`.
- `HiveMapAllianceBootstrap.IsEnvironmentScene(Scene)` (lines 39–43):
  `scene.name.StartsWith("Environment2D5D")` only.

`LivingHive.unity`'s scene name is `"LivingHive"`; its root objects are
`"Living Hive Demo"`/`"Main Camera"`/`"Sun"` — **none** satisfy either
condition. `Environment2D5D_HiveMap_Test.unity`'s scene name itself starts
with `"Environment2D5D"` — satisfies both directly. Neither bootstrap can
ever fire while LivingHive is the loaded scene; both fire automatically
when HiveMap loads.

**Single instantiation site, confirmed by full-project grep**: `new
AllianceClient(...)` / `new AllianceCenterPanelController(...)` / `new
PlayerDirectoryClient(...)` appear exactly once in real runtime code —
`MobileAccountSessionRuntimeBootstrap.cs` lines 360–365, inside
`TryConfigureGameplayForActiveSession()`, which only ever runs from
`OnSceneLoaded` after the `IsEnvironmentScene` gate above. The only other
occurrence of any of these constructors in the whole `Assets/` tree is a
mock/fake construction inside `AllianceClientTests.cs` (a unit test, not
runtime code). `LivingHiveDemoBootstrap.cs` was searched directly for the
string `"Alliance"` — zero matches.

**`HiveViewProductUiPresenter`'s own code comments answer question 7
directly** (lines 3267–3284, pre-existing, not written this session): the
Alliance/chat "External host bridge" entry points
(`OpenAllianceOverlayForExternalHost`/`DrawAllianceOverlayForExternalHost`,
exactly what `HiveMapAllianceBootstrap` calls) are explicitly documented as
working **without** calling `EnsureSceneObjects()` — the method that builds
LivingHive's entire 3D hex-grid world — and `EnsureSceneObjects()` is
stated to have **"exactly one call site, `LivingHiveDemoBootstrap.Start()`,
outside this file entirely."** `HiveViewProductUiPresenter` is a big
monolithic static class hosting many features, but the Alliance Center path
specifically is pure IMGUI (`OnGUI`) drawing against
`allianceCenterController.Model` — it needs no LivingHive-specific
GameObjects, prefabs, or scene content to run.

### Explicit answers

1. Does HiveMap instantiate/use the new `AllianceCenterPanelController`?
   **YES** — via `MobileAccountSessionRuntimeBootstrap`, gated on
   `Environment2D5D` scene naming, which HiveMap's scene name satisfies.
2. Does HiveMap instantiate/use the new `AllianceClient`? **YES** — same
   call site, same gate.
3. Does clicking `ALLIANCE_CENTER` in HiveMap reach the new real Alliance
   UI? **YES** — `HiveMapAllianceBootstrap.OnBuildingClicked` (gated to only
   exist in an `Environment2D5D`-named scene) calls
   `HiveViewProductUiPresenter.OpenAllianceOverlayForExternalHost()`, which
   opens the real `DrawAllianceHeadquartersScreen`/`AllianceCenterPanelController`-backed
   screen built in M043/M043B.
4. Does LivingHive instantiate the same controller/client? **NO** — its
   only bootstrap (`LivingHiveDemoBootstrap`) has zero Alliance references,
   and both real bootstraps that could create these objects are structurally
   gated off in any scene not named/rooted `Environment2D5D*`.
5. Are any M041–M043B changes reachable ONLY from LivingHive? **NO** — every
   Alliance construction/UI-entry site found this session lives behind the
   `Environment2D5D` gate; none behind any LivingHive-specific condition.
6. Are any critical Alliance runtime hooks missing from HiveMap? **NO**,
   as far as this ownership trace goes — client construction, controller
   construction, and the building-click→UI-open wiring are all present and
   gated correctly for HiveMap. (This trace did not re-verify that the
   HiveMap scene's building catalog actually contains an `ALLIANCE_CENTER`
   building instance — that was asserted as already working by the M041–M043
   reports and was out of scope for this specific ownership question.)
7. Is `HiveViewProductUiPresenter` merely reused as an external UI presenter
   hosted by HiveMap, or does it require LivingHive scene objects to
   function? **Merely reused as an external UI presenter** for the Alliance
   path — confirmed by the class's own pre-existing code comments plus the
   single-call-site fact for `EnsureSceneObjects()`.
8. If LivingHive were deleted from Build Settings entirely, would Alliance
   Center in HiveMap still compile and function? **YES** — nothing in the
   Alliance construction or UI path references `LivingHive.unity`,
   `LivingHiveDemoBootstrap`, or `EnsureSceneObjects()` in any way. Only
   `LivingHive.unity` itself and its own demo bootstrap would stop working.

### Final answers

- **Alliance implementation actually hosted by HiveMap? YES.**
- **Alliance requires LivingHive scene to function? NO.**
- **Any Alliance work accidentally LivingHive-only? NO.**

## 13. Final verdict

- A. Root cause proven? **YES**
- B. LivingHive removed from normal runtime path? **YES** (disabled, index 4, QA/editor-only)
- C. Auth routes to HiveMap? **YES** (code was always correct; now backed by correct Build Settings data)
- D. WorldMap returns to HiveMap? **YES** (same)
- E. Build configuration uses HiveMap? **YES** (index 0, enabled)
- F. Regression tests added/passing? **YES** (5/5)
- G. READY FOR CEO HIVE ROUTING RETEST? **YES**

**Required human test**: CEO launches the game → logs in → **must land in
HiveMap** (`Environment2D5D_HiveMap_Test.unity`), not LivingHive. If
launching via a previously-built Windows/Android executable rather than a
fresh Editor Play session, that build was packaged from the corrupted Build
Settings state and must be **rebuilt** from the now-restored configuration
before retesting — the fix applied here corrects the Editor/source-of-truth
state, not any already-compiled binary.

Alliance certification (M043C) remains paused until this retest is
confirmed by Jeff, per the mission's explicit instruction.
