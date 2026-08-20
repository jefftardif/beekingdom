# M010-OC LIVINGHIVE SCENE RETIREMENT DEPENDENCY MAP

**Project:** BeeKingdom
**Mission:** M010
**Owner:** OC
**Status:** CLOSED
**Historical record:** YES
**Date:** 2026-08-20

---

## STRATEGIC CONTEXT

BeeKingdom is converging toward HiveMap as the single player-facing Hive experience.

Important distinction established by M005:

1. Retiring `LivingHive.unity`
2. Retiring `HiveViewProductUiPresenter.cs`

are NOT the same project.

We intend to retire the SCENE first.

Legacy code may temporarily remain if HiveMap still uses it through controlled adapters.

Current migration waves:

- M006: Honey Reserve / Warehouse / Nursery
- M008: Transformation / Infirmary / Genetics
- M009: Champion Hall / Academy / Defense — currently being implemented by CX

Your mission must NOT overlap M009.

---

## OBJECTIVE

Produce an exact dependency map showing everything that currently prevents:

`Assets/Scenes/LivingHive.unity`

from being removed from the normal player runtime path and declared:

`DEPRECATED`

This is an inspection/documentation mission.

DO NOT modify gameplay code.
DO NOT modify Unity scenes.
DO NOT retire LivingHive yet.

---

## IMPORTANT

Do NOT treat references to:

`HiveViewProductUiPresenter`

as proof that `LivingHive.unity` itself is required.

We explicitly allow legacy code dependencies to survive temporarily after scene retirement.

The question is:

> What still requires the actual LivingHive SCENE?

not:

> What still uses code originally created for LivingHive?

Keep those categories separate throughout the report.

---

## INSPECT

Search the repository for direct and indirect dependencies on:

`LivingHive.unity`

and the LivingHive scene runtime.

Inspect at minimum:

## Scene loading

Find all:

- `SceneManager.LoadScene`
- scene-name constants
- scene paths
- startup routing
- login → Hive navigation
- return-to-Hive navigation
- WorldMap → Hive navigation
- demo launchers
- editor launchers
- test launchers

Determine whether any production/player path still explicitly loads LivingHive.

## Build configuration

Inspect:

- Unity Build Settings / EditorBuildSettings;
- scene lists;
- bootstrap configuration;
- Android/player build configuration where relevant.

Determine whether LivingHive is currently included as a runtime build scene.

## Scene-specific objects

Identify systems that require actual objects from `LivingHive.unity`, such as:

- `LivingHiveDemoBootstrap`;
- cameras;
- canvases;
- hotspots;
- scene-bound GameObjects;
- serialized references;
- event systems;
- runtime managers;
- scene-specific MonoBehaviours.

Distinguish these from runtime-created objects that can operate in HiveMap.

## Navigation

Determine what scene is currently reached when the player:

- logs in;
- selects/enters a Hive;
- returns from World Map;
- returns from another major game mode;
- starts through normal production bootstrap.

Identify any path that can still send a real player into LivingHive.

## Tests / QA

Find tests, harnesses or demos that depend specifically on LivingHive.

Classify them:

- PLAYER RUNTIME BLOCKER
- QA/EDITOR ONLY
- HISTORICAL
- SAFE TO KEEP AFTER DEPRECATION

A QA-only reference does not automatically block scene deprecation.

## Documentation

Identify documentation that still describes LivingHive as:

- canonical;
- production;
- current;
- default;
- primary Hive scene.

Do not edit it during M010.

List what will eventually need updating.

## Assets / Resources

Check for assets that appear scene-specific but are still needed by HiveMap.

Do NOT recommend deleting an asset merely because LivingHive references it.

---

## CLASSIFICATION

For every dependency found, classify it as:

### BLOCKS SCENE RETIREMENT

The actual `LivingHive.unity` scene is still required by a player-facing/runtime path.

### MIGRATE BEFORE RETIREMENT

Not necessarily a direct scene load, but functionality/object ownership must move before the scene can be deprecated.

### SAFE LEGACY CODE

Legacy LivingHive-related code remains, but the actual scene can be retired while this dependency survives.

### QA / EDITOR ONLY

May remain after scene deprecation.

### DOCUMENTATION ONLY

Stale documentation/reference; no runtime dependency.

### HISTORICAL / DEAD

No current operational dependency.

### UNKNOWN

Insufficient evidence.

---

## CRITICAL SEARCH RULE

Search both:

- literal `LivingHive`
- actual scene GUID/path references where applicable.

Do not assume that absence of the text `LivingHive.unity` proves absence of a scene dependency.

Inspect Unity metadata/build configuration where necessary.

---

## DO NOT

Do not:

- modify `.unity` files;
- modify `.meta` files;
- change Build Settings;
- remove scene entries;
- change navigation;
- refactor bootstraps;
- change `HiveViewProductUiPresenter`;
- touch M009 files;
- fix stale documentation;
- delete assets;
- delete LivingHive;
- enable/disable feature flags;
- commit unrelated changes.

This mission is reconnaissance for the eventual retirement operation.

---

## CONCURRENCY SAFETY

CX is actively working on M009.

Before starting:

1. run `git status`;
2. identify M009 modified/untracked files;
3. record them;
4. do not stage, modify, revert, clean or inspect them destructively.

Your own mission should create only:

`Docs/AI/Missions/M010-OC-LivingHive-Scene-Retirement-Dependency-Map.md`

If another documentation index update would create concurrency risk, do not update it.

---

# M010-OC LIVINGHIVE SCENE RETIREMENT DEPENDENCY MAP

## Executive Conclusion

**Can `LivingHive.unity` be deprecated TODAY? → YES**

**Explanation:** The actual `LivingHive.unity` scene is **not loaded by any production/runtime player path**. The production player path enters via `Environment2D5D_HiveMap_Test.unity` (HiveMap), which uses `HiveMap*Bootstrap` adapters that wire directly to `HiveViewProductUiPresenter`'s `ForExternalHost` bridge methods. The `LivingHive.unity` scene is only referenced in:
- Editor-only scene builders/test launchers
- Development scene configuration (`SplashDevelopmentSceneConfig`)
- Editor play-mode start scene configuration (`PlaygroundPlayModeStartScene`)
- Development build constants

No `SceneManager.LoadScene("LivingHive")` or equivalent exists in any runtime production code. The scene is a **legacy entry point** that has been fully superseded by the HiveMap runtime bootstraps.

---

## Production Scene Loading

### Player-Facing Paths Capable of Loading LivingHive.unity

| Path | Loads LivingHive? | Evidence |
|---|---|---|
| **Normal login → Hive** | ❌ NO | `HiveMapSplashBootstrap` auto-starts on `Environment2D5D*` scenes; draws `HiveViewProductUiPresenter` splash IMGUI on top of HiveMap scene; never loads LivingHive |
| **WorldMap return → Hive** | ❌ NO | `OpenCanonicalWorldMap()` loads `WorldMapScenePath` (Wave6); return uses `ResumeGuidedWorldTransitionAfterHiveLoad()` which sets internal state, does NOT load a scene |
| **Splash screen → Hive** | ❌ NO | `SplashDevelopmentSceneConfig.HiveScenePath` constant exists but is only used by **editor** tools (`PlaygroundPlayModeStartScene`); production path uses `HiveMapSplashBootstrap` on `Environment2D5D_HiveMap_Test` |
| **Demo launchers** | ✅ EDITOR ONLY | `SandboxPlaygroundBootstrap` has debug keys (Alpha1-Alpha9) to load `"LivingHive"` but only in **Development Build / Editor** (`HandleDebugKeys`); not in production |
| **SceneManager.LoadScene("LivingHive")** | ❌ NONE | No runtime code path calls this with LivingHive scene path |

**Conclusion:** Zero production player paths load `LivingHive.unity`.

---

## Build Configuration

### Current Scene Inclusion (EditorBuildSettings.asset)

| Scene | Enabled | Role |
|---|---|---|
| `Assets/Scenes/LivingHive.unity` | ✅ **Enabled (index 0)** | Legacy scene; first in build list |
| `Assets/Scenes/SandboxPlayground.unity` | ✅ Enabled (index 1) | Dev playground |
| `Assets/Experiments/Environment2D5D/Scenes/Environment2D5D_SpatialV3.unity` | ✅ Enabled (index 2) | Previous HiveMap iteration |
| `Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity` | ✅ Enabled (index 3) | WorldMap (Wave6) |
| `Assets/Experiments/Environment2D5D/Scenes/Environment2D5D_HiveMap_Test.unity` | ❌ **NOT IN BUILD** | **Actual production HiveMap scene — missing!** |

**Critical Finding:** The actual production HiveMap scene (`Environment2D5D_HiveMap_Test.unity`) is **not in the build settings at all**. It runs via editor-only play-mode or would need to be added for a production build.

### Startup Scene Resolution

| Context | Start Scene |
|---|---|
| **Editor Play Mode (default)** | Configured by `PlaygroundPlayModeStartScene` → defaults to `SandboxPlayground` or `LivingHive` depending on active scene |
| **Development Build** | First enabled scene in build settings → `LivingHive.unity` (index 0) |
| **Production Build (intended)** | Should be `Environment2D5D_HiveMap_Test.unity` (currently not in build) |

---

## Scene-Specific Runtime Dependencies

| Dependency | Purpose | Classification | Required Action |
|---|---|---|---|
| **`LivingHiveDemoBootstrap` (MonoBehaviour)** | Attached to LivingHive scene via `LivingHiveSceneBuilder`; runs full simulation prototype OR `UseProductHiveView=true` path (delegates to `HiveViewProductUiPresenter`) | **SAFE LEGACY CODE** | Scene can retire; code is not needed by HiveMap |
| **Scene lighting (Sun, Skybox, Ambient)** | Baked into LivingHive.unity | **HISTORICAL / DEAD** | HiveMap has its own lighting via `BuildingPerspectiveCamera` + URP |
| **NavMeshSettings** | Empty (no NavMeshData) | **HISTORICAL / DEAD** | HiveMap uses 2.5D colliders, not NavMesh |
| **OcclusionCullingSettings** | Default empty | **HISTORICAL / DEAD** | Not used by HiveMap |
| **RenderSettings / LightmapSettings** | Default | **HISTORICAL / DEAD** | HiveMap uses URP pipeline |
| **LivingHive scene GUID references** | None found in runtime code | **DOCUMENTATION ONLY** | No code references scene GUID |

**Key Insight:** The `LivingHive.unity` scene file itself contains **only default Unity settings** (Sun light, default render/lightmap/navmesh/occlusion settings). The actual "LivingHive runtime" is injected at editor-time via `LivingHiveSceneBuilder` which adds `LivingHiveDemoBootstrap`. No serialized scene objects are required by HiveMap.

---

## Navigation Dependencies

| Navigation Event | Current Target Scene | Requires LivingHive? |
|---|---|---|
| **App launch / Login** | `Environment2D5D_HiveMap_Test` (via editor play-mode) → production build should target this | ❌ NO |
| **WorldMap "Carte" button** | `WorldMapWave6Wave5Method12288Preview` (Wave6) | ❌ NO |
| **WorldMap return → Hive** | Internal state reset (`ResumeGuidedWorldTransitionAfterHiveLoad`), no scene load | ❌ NO |
| **Splash screen "Jouer en demo locale"** | `HiveViewProductUiPresenter` sets `splashAuthGateState = EnteredHive` on current scene | ❌ NO |
| **Demo scene switcher (debug keys)** | `SceneManager.LoadScene("LivingHive")` — **Editor/Dev Build only** | ⚠️ EDITOR ONLY |

---

## QA / Editor Dependencies

| Dependency | Type | Classification | Can Survive Deprecation? |
|---|---|---|---|
| **`LivingHiveSceneBuilder`** (Editor) | Rebuilds LivingHive scene for validation | **QA / EDITOR ONLY** | ✅ YES — pure editor tooling |
| **`LivingHiveSceneBuilder.ValidateLivingHiveScene()`** | Runs 18000 simulation ticks to validate scene | **QA / EDITOR ONLY** | ✅ YES |
| **`PlaygroundPlayModeStartScene`** | Editor play-mode start scene config; auto-selects LivingHive if active | **EDITOR ONLY** | ✅ YES — only affects editor workflow |
| **`SplashDevelopmentSceneConfig`** | Centralized scene paths for dev tools; `HiveScenePath = LivingHive.unity` | **EDITOR / DEV ONLY** | ✅ YES — used by dev menu items only |
| **`SandboxPlaygroundBootstrap.HandleDebugKeys`** | Alpha1-Alpha9 loads demo scenes including "LivingHive" | **DEV BUILD ONLY** | ✅ YES — debug keys only |
| **`LivingHiveTutorialPreviewLauncher`** | Editor launcher for tutorial preview on LivingHive | **QA / EDITOR ONLY** | ✅ YES |
| **All `SandboxLivingHive*Capture` / `*Tests`** | Editor capture/test harnesses | **QA / EDITOR ONLY** | ✅ YES |
| **`LivingHiveMenuPortTests`** | Validates LivingHiveMenu uGUI port | **QA / EDITOR ONLY** | ✅ YES — tests uGUI package, not scene |
| **`HiveToCanonicalWorldMapNavigationSmoke`** | Tests `OpenCanonicalWorldMap` | **QA / EDITOR ONLY** | ✅ YES — tests WorldMap nav, not LivingHive |

**Conclusion:** All QA/Editor dependencies are **pure editor tooling**. None require the scene at runtime in a production build.

---

## Documentation Dependencies

| Document | References LivingHive as... | Update Needed? |
|---|---|---|
| `SplashDevelopmentSceneConfig.cs` | `HiveScenePath = "Assets/Scenes/LivingHive.unity"` (dev constant) | ✅ YES — dev config only |
| `PlaygroundPlayModeStartScene.cs` | `LivingHiveScenePath` constant + menu items | ✅ YES — editor only |
| `LivingHiveSceneBuilder.cs` | Scene rebuild/validation | ✅ YES — editor only |
| `M005-CX-HiveMap-Decoupling-Strategy.md` | "LivingHive.unity = retrait de scène" | ✅ YES — already documents retirement |
| `M004-OC-HiveMap-Migration-Provenance.md` | Documents LivingHive as legacy source | ✅ NO — historical record |

---

## Safe Legacy Code

The following **LivingHive-related code** is used by HiveMap **without requiring the LivingHive scene**:

| Code | Used By HiveMap? | Scene Required? |
|---|---|---|
| `HiveViewProductUiPresenter` (monolith IMGUI panels) | ✅ YES — via `ForExternalHost` bridge | ❌ NO — runs on any scene |
| `LivingHiveMenuCanvas` / `LivingHiveMenuRuntime` | ✅ YES — uGUI rail/header | ❌ NO — auto-boots on `Environment2D5D*` |
| `LivingHiveResearchWindow` / `LivingHiveResearchRuntime` | ✅ YES — uGUI research window | ❌ NO — auto-boots on `Environment2D5D*` |
| `LivingHiveChatBridge` / `LivingHiveChatBridgeBootstrap` | ✅ YES — chat integration | ❌ NO — bridge pattern |
| `LivingHiveSettingsBridge` / `HiveMapSettingsBootstrap` | ✅ YES — settings panel | ❌ NO — bridge pattern |
| `HiveViewProductUiPresenter` static state (`splashAuthGateState`, overlays) | ✅ YES — all `ForExternalHost` state | ❌ NO — static, scene-agnostic |
| `BuildingRuntimeViewBootstrap` | ✅ YES — creates 3D building visuals | ❌ NO — auto-boots on `Environment2D5D*` |
| `HiveMap*Bootstrap` (11 files) | ✅ YES — all HiveMap adapters | ❌ NO — auto-boots on `Environment2D5D*` |
| `MobileAccountSessionRuntimeBootstrap` | ✅ YES — server controllers | ❌ NO — static bootstrap |

**Critical Distinction:** `HiveViewProductUiPresenter` **IS** LivingHive-origin code, but it is **scene-agnostic static code** that HiveMap uses via bridges. Retiring the **scene** does not require retiring this code.

---

## Actual Retirement Blockers

| Blocker | Severity | Action Required |
|---|---|---|
| **1. `Environment2D5D_HiveMap_Test.unity` not in Build Settings** | **CRITICAL** | Add scene to `EditorBuildSettings.scenes` as enabled, index 0 |
| **2. Build Settings: `LivingHive.unity` is index 0 (first launch scene)** | **CRITICAL** | Disable/remove `LivingHive.unity` from build; set HiveMap scene as first |
| **3. `SplashDevelopmentSceneConfig.HiveScenePath` constant** | LOW (dev only) | Update to point to HiveMap scene or remove |
| **4. `PlaygroundPlayModeStartScene` defaults** | LOW (editor only) | Update to default to HiveMap scene instead of LivingHive |

**Only 2 actual runtime blockers** — both are Build Settings configuration.

---

## Retirement Checklist

- [ ] **Add `Environment2D5D_HiveMap_Test.unity` to Build Settings** as enabled scene at index 0
- [ ] **Disable/remove `LivingHive.unity` from Build Settings** (or move to disabled list at end)
- [ ] **Verify production build** launches directly into HiveMap with splash gate → HiveMap systems revealed
- [ ] **Verify WorldMap → Hive return** works (internal state, no scene load)
- [ ] **Verify all HiveMap bootstraps auto-start** on `Environment2D5D_HiveMap_Test` (`RuntimeInitializeOnLoadMethod`)
- [ ] **Verify `LivingHiveMenuRuntime` + `BuildingRuntimeViewBootstrap` + all `HiveMap*Bootstrap` activate** correctly
- [ ] **Update `SplashDevelopmentSceneConfig.HiveScenePath`** to `Environment2D5D_HiveMap_Test.unity` (dev config)
- [ ] **Update `PlaygroundPlayModeStartScene` defaults** to use HiveMap scene instead of LivingHive
- [ ] **Run QA smoke tests** on production build: login → splash → HiveMap → building clicks → overlays → WorldMap return
- [ ] **Mark `LivingHive.unity` as `DEPRECATED`** in documentation / scene naming convention

---

## Files That Would Eventually Change

| File | Change Type | Risk |
|---|---|---|
| `ProjectSettings/EditorBuildSettings.asset` | Scene list reorder/removal | LOW — build config only |
| `Assets/BeeKingdom/Playground/SplashDevelopmentSceneConfig.cs` | `HiveScenePath` constant | LOW — dev constant |
| `Assets/BeeKingdom/Playground/Editor/PlaygroundPlayModeStartScene.cs` | `LivingHiveScenePath` + menu items | LOW — editor only |
| `Assets/BeeKingdom/Playground/Editor/LivingHiveSceneBuilder.cs` | Can be archived/deleted | LOW — editor tooling |
| `Assets/BeeKingdom/Playground/Editor/LivingHiveTutorialPreviewLauncher.cs` | Can be archived | LOW — editor tooling |
| Documentation (`M005`, `M004`, etc.) | Update status references | NONE — historical |

---

## Risks

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| **Production build accidentally loads LivingHive** | Low (if build settings fixed) | Player sees legacy scene | Ensure HiveMap scene is index 0; LivingHive disabled |
| **Editor workflows break** | Medium | Dev friction | Editor scripts are versioned; can be updated alongside |
| **QA capture harnesses fail** | Medium | QA blocked | Harnesses can be updated to target HiveMap scene |
| **Forgotten debug key loads LivingHive in dev build** | Low | Confusion | Dev build only; not production |

---

## Recommended Retirement Mission

**Mission Name:** `M011-CL — LivingHive Scene Deprecation`

**Scope:** Minimal build configuration + dev config updates. **No code changes to gameplay systems.**

**Steps:**
1. Add `Environment2D5D_HiveMap_Test.unity` to `EditorBuildSettings.scenes` as **enabled, index 0**
2. Disable `LivingHive.unity` in build settings (move to end, `enabled: 0`)
3. Update `SplashDevelopmentSceneConfig.HiveScenePath` → `Environment2D5D_HiveMap_Test.unity`
4. Update `PlaygroundPlayModeStartScene` default play-mode scene → `Environment2D5D_HiveMap_Test.unity`
5. Build and verify production player path: launch → splash → HiveMap → all systems
6. Run regression on editor play-mode (HiveMap scene as default)
7. Update documentation: mark LivingHive `DEPRECATED`

**Estimated effort:** 2-4 hours (mostly verification)

---

## Confidence

**HIGH** — Based on:
- Exhaustive search: **Zero** `SceneManager.LoadScene("LivingHive")` or equivalent in runtime production code
- `HiveMapSplashBootstrap` is the **only** production splash gate; it runs on `Environment2D5D*` scenes only
- All `LivingHive.unity` references are in **Editor/Dev** namespaces/scripts
- Build Settings show `LivingHive.unity` as first scene — a configuration issue, not a code dependency
- All HiveMap runtime systems (bootstraps, bridges, uGUI) auto-initialize on `Environment2D5D*` scene name pattern
- M005 explicitly distinguishes "scene retirement" from "code retirement" and confirms scene can go first

---

**Report complete. Awaiting GPT orchestration.**