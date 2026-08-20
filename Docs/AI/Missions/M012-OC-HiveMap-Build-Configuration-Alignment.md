# M012-OC HIVEMAP BUILD CONFIGURATION ALIGNMENT RESULT

## Previous Configuration

| Index | Scene | Enabled | Role |
|-------|-------|---------|------|
| 0 | `Assets/Scenes/LivingHive.unity` | ✅ | Legacy Hive (production default — INCORRECT) |
| 1 | `Assets/Scenes/SandboxPlayground.unity` | ✅ | Dev playground |
| 2 | `Assets/Experiments/Environment2D5D/Scenes/Environment2D5D_SpatialV3.unity` | ✅ | Previous HiveMap iteration |
| 3 | `Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity` | ✅ | WorldMap (Wave6) |
| 4-24 | Duplicate `SandboxPlayground.unity` / `LivingHive.unity` | ❌ | Junk duplicates |
| 25 | `WorldMapMmoFullscreenFoundation.unity` | ❌ | Legacy WorldMap foundation |
| 26 | `WorldMapWave5Premium25x25Test.unity` | ❌ | Legacy Wave5 test |
| 27 | `SandboxPlayground.unity` (duplicate) | ❌ | Junk |

**Critical Issues:**
- `LivingHive.unity` at build index 0 — would launch as first scene in any build
- `Environment2D5D_HiveMap_Test.unity` (actual production HiveMap) **NOT IN BUILD SETTINGS**
- 20+ junk duplicate entries cluttering build settings

## Root Cause

Historical drift: `LivingHive.unity` was the original production Hive scene. When HiveMap (`Environment2D5D_HiveMap_Test.unity`) was created, it was never added to Build Settings. The legacy scene remained at index 0. Duplicate entries accumulated from editor scene builder tools.

## Changes

**File Modified:** `ProjectSettings/EditorBuildSettings.asset`

### Build Scene List — New Configuration

| Index | Scene | Enabled | Role |
|-------|-------|---------|------|
| 0 | `Assets/Experiments/Environment2D5D/Scenes/Environment2D5D_HiveMap_Test.unity` | ✅ | **Production HiveMap entry** |
| 1 | `Assets/Scenes/SandboxPlayground.unity` | ✅ | Dev playground (required by Android internal build) |
| 2 | `Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity` | ✅ | WorldMap (Wave6) |
| 3 | `Assets/Experiments/Environment2D5D/Scenes/Environment2D5D_SpatialV3.unity` | ✅ | Reference HiveMap iteration |
| 4 | `Assets/Scenes/LivingHive.unity` | ❌ | Legacy — available for QA/editor |
| 5 | `Assets/Scenes/WorldMapMmoFullscreenFoundation.unity` | ❌ | Legacy WorldMap foundation |
| 6 | `Assets/Scenes/WorldMapWave5Premium25x25Test.unity` | ❌ | Legacy Wave5 test |
| 7 | `Assets/Scenes/SandboxPlayground.unity` | ❌ | Single reference duplicate (cleaned from 20+) |

**Summary of changes:**
- ✅ Added `Environment2D5D_HiveMap_Test.unity` as **build index 0** (production entry)
- ✅ Moved `LivingHive.unity` to **disabled** (index 4) — available for QA/editor
- ✅ Enabled `Environment2D5D_SpatialV3.unity` as reference
- ✅ Removed **20+ junk duplicate entries**
- ✅ Preserved `SandboxPlayground.unity` at index 1 (required by `AndroidBuild.cs` internal test build)
- ✅ Preserved `WorldMapWave6Wave5Method12288Preview.unity` (canonical WorldMap)
- ✅ Preserved all legacy scenes as disabled for QA/editor access

## Final Build Scene Order

```
[0] ✅ Environment2D5D_HiveMap_Test.unity          ← PRODUCTION HIVE ENTRY
[1] ✅ SandboxPlayground.unity                      ← DEV PLAYGROUND (AndroidBuild requires this)
[2] ✅ WorldMapWave6Wave5Method12288Preview.unity   ← CANONICAL WORLD MAP
[3] ✅ Environment2D5D_SpatialV3.unity              ← REFERENCE / LEGACY HIVEMAP
[4] ❌ LivingHive.unity                             ← LEGACY (QA/editor only)
[5] ❌ WorldMapMmoFullscreenFoundation.unity        ← LEGACY WORLD MAP
[6] ❌ WorldMapWave5Premium25x25Test.unity          ← LEGACY WAVE5
[7] ❌ SandboxPlayground.unity (ref)                ← CLEANED DUPLICATE
```

## LivingHive Status

| Aspect | Status |
|--------|--------|
| Scene file | Preserved (`Assets/Scenes/LivingHive.unity`) |
| Build inclusion | **Disabled** (index 4) |
| Editor/QA access | **Preserved** — available via editor scene load, scene builders, play-mode config |
| Runtime production path | **Excluded** — will not load in any build |
| M010 retirement readiness | **Configuration blocker resolved** — only code migration remains (M011) |

## Validation

| Check | Result |
|-------|--------|
| Unity compilation | ✅ **0 errors** (batchmode verified) |
| HiveMap scene in build | ✅ Index 0, enabled |
| LivingHive disabled | ✅ Index 4, disabled |
| WorldMap preserved | ✅ Index 2, enabled |
| SandboxPlayground preserved | ✅ Index 1, enabled (AndroidBuild requirement met) |
| Legacy scenes available for QA | ✅ All disabled but present |
| Junk duplicates removed | ✅ 20+ entries cleaned |
| Unity compilation | ✅ **0 CS errors** |

## Files Changed

| File | Change |
|------|--------|
| `ProjectSettings/EditorBuildSettings.asset` | Complete rebuild of scene list — only modified file |

## Remaining Retirement Blockers

| Blocker | Status |
|---------|--------|
| **Build configuration** | ✅ **RESOLVED** — HiveMap at index 0, LivingHive disabled |
| **Code migration (M011)** | ⏳ Pending — `HiveViewProductUiPresenter` and other legacy code still used via bridges (per M005/M010 this is allowed) |
| **Scene file deletion** | ⏳ Not required — scene can remain as disabled asset |

## Recommendation

Configuration is now aligned with product direction:
- **Production builds** will launch `Environment2D5D_HiveMap_Test.unity` (HiveMap)
- **WorldMap** navigation works via `WorldMapWave6Wave5Method12288Preview.unity`
- **LivingHive** is available for editor/QA but excluded from runtime
- **Android internal test builds** still work (require `SandboxPlayground` at index 1)

Ready for commit. No gameplay code touched. Editor/QA tooling preserved.

## Confidence

**HIGH** — Minimal, targeted configuration change. All validation passes. No gameplay or server code modified.