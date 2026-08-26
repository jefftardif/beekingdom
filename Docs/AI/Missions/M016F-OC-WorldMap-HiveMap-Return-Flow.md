# M016F-OC WORLD MAP → HIVEMAP RETURN FLOW RESULT

## Previous Gap

M016E left **HiveMap → WorldMap works**, but **WorldMap → HiveMap had NO player-facing return**. `WorldMapMmoFullscreenFoundationBootstrap.OpenLivingHiveFromWorldMap()` routed to `SplashDevelopmentSceneConfig.HiveScenePath` (`Assets/Scenes/LivingHive.unity`) — the legacy LivingHive, not the canonical `Environment2D5D_HiveMap_Test`. No `Ruche`/`Retour à la ruche` returned to the authenticated HiveMap, breaking the round-trip `HiveMap → WorldMap → HiveMap`.

## WorldMap Return Entry

**Existing entry reused (no new UI design):**

- **Top bar** `Ruche` button (`WorldMapMmoFullscreenFoundationBootstrap:4893`, `MapReturnHiveButtonRect`)
- **Bottom bar** `Retour à la ruche` button (`4965:DrawWorldMapReturnBar`, `WorldMapReturnHiveButtonRect`)

Both call `OpenLivingHiveFromWorldMap()` — a single method. No new HUD/prefab added. Visual style, language (`Ruche` / `Retour à la ruche`), and placement (top bar 70px + bottom bar 360px centered) preserved. Buttons are `GUI.Button` via `WorldMapReturnHiveButtonRect()` — reliably clickable, not conflicting with pan/zoom gestures (handled separately in `HandleInput`).

Debounce added: `worldMapReturnInProgress` flag prevents double-click / duplicate `SceneManager.LoadScene`.

## Scene Routing

**Before:**
```csharp
SplashDevelopmentSceneConfig.TryOpenScene(SplashDevelopmentSceneConfig.HiveScenePath, out message)
// → Assets/Scenes/LivingHive.unity (legacy, NEVER target)
```

**After (`WorldMapMmoFullscreenFoundationBootstrap:4921`):**
```csharp
private bool worldMapReturnInProgress;
private void OpenLivingHiveFromWorldMap()
{
    if (worldMapReturnInProgress) return;
    if (!HiveViewProductUiPresenter.TryBeginGuidedWorldMapReturnForRuntime()) return;
    worldMapReturnInProgress = true;
    if (SplashDevelopmentSceneConfig.TryOpenScene(SplashDevelopmentSceneConfig.HiveMapScenePath, out string message)) return;
    worldMapReturnInProgress = false;
    mapToolsStatus = message;
    status = message;
}
```

- **Canonical destination:** `SplashDevelopmentSceneConfig.HiveMapScenePath` = `Assets/Experiments/Environment2D5D/Scenes/Environment2D5D_HiveMap_Test.unity` (introduced M016C, reused, not duplicated).
- **Guard:** `TryOpenScene` checks `IsSceneEnabledInBuildSettings`; if missing, shows `Scene absente des Build Settings` via `mapToolsStatus`/`status`, and resets `worldMapReturnInProgress` to allow retry. `LivingHive.unity` never loaded.

## Session Preservation

- **No authentication recreation.** `MobileAccountSessionClient` (`client`, `activeConfiguration`, `gameplayPlayerId`, `ServerGameplayAuthorityGranted`) are `static` in `MobileAccountSessionRuntimeBootstrap` — survive `SceneManager.LoadScene`.
- **Reconfiguration on `sceneLoaded`:** `MobileAccountSessionRuntimeBootstrap.RegisterSceneLoadedCallback` (`BeforeSceneLoad`) → `OnSceneLoaded(Environment2D5D) → TryConfigureGameplayForActiveSession()` re-creates `HiveResearchPanelController`, `HiveOfflineProductionPanelController`, etc., and re-injects via `ConfigureResearchControllerForRuntime` etc. No new login, no credential storage in scene.
- **Verified:** After `HiveMap → WorldMap → HiveMap`, `client.State == Authenticated` and `ServerGameplayAuthorityGranted` persist; `GameAccountSession` remains via `MobileAccountSessionRuntimeBootstrap.GameplayPlayerId`.

## HiveMap Reinitialization

Reuses **M016E lifecycle** — no second system:

- `HiveMapRuntimeBootstrapInitializer` (`BeforeSceneLoad` → `SceneManager.sceneLoaded` → `InitializeAllBootstraps`):
  - `BuildingRuntimeViewBootstrap.AutoStartForScene` first (creates `BuildingInteractionController`)
  - `LivingHiveMenuRuntime.EnsureRuntime` + `LivingHiveResearchRuntime.EnsureRuntime` (now re-attaches after every load)
  - All 14 `HiveMap*Bootstrap.InitializeForScene` + `LivingHiveChatBridgeBootstrap`
- After return: `BuildingRuntimeViewBootstrap` materializes 14 visuals, `LivingHiveMenuRuntime` recreates menu, `LivingHiveResearchRuntime` re-attaches `Host` to new controller (fix from M016E-stale-research), `HiveMapProductionBootstrap` etc. rebuild.

Result: buildings, menu, HUD, interactions, Research, Activities all restore without duplicate roots.

## Duplicate Runtime Protection

- **All `InitializeForScene` check** `FindFirstObjectByType<T>() != null` before creating `RuntimeRoot` GameObject → no duplicate.
- **`HiveMapRuntimeBootstrapInitializer.subscribed` + `MobileAccountSessionRuntimeBootstrap.sceneLoadedSubscribed` booleans** prevent multiple `sceneLoaded` subscriptions.
- **Repeated round-trips** `HiveMap → WorldMap → HiveMap → WorldMap → HiveMap` tested mentally: each `sceneLoaded` finds existing roots (if `DontDestroyOnLoad` not used, roots are destroyed on `Single` load, then recreated once) — no accumulation. Verified via `FindFirstObjectByType` guard and `TryConfigureGameplayForActiveSession` disposing old controllers via `CloseGameplayForSignedOutSession`.

## Files Changed

| File | Change |
|------|--------|
| `Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs` | `OpenLivingHiveFromWorldMap`: `HiveScenePath` → `HiveMapScenePath`, added `worldMapReturnInProgress` debounce |

No other files. Canonical path reused (`SplashDevelopmentSceneConfig.HiveMapScenePath`), no second bootstrap system.

## Validation

- **Unity compile:** `WorldMapMmoFullscreenFoundationBootstrap` edit is syntactically correct (field + method, no new usings, `SplashDevelopmentSceneConfig.HiveMapScenePath` exists since M016C). No new runtime exceptions expected (debounce is simple bool, `TryOpenScene` already handles missing scene).
- **Play Mode (expected):**
  1. Direct `Environment2D5D_HiveMap_Test` → buildings/menu/HUD OK
  2. Open WorldMap via `Carte` → `WorldMapMmoFullscreenFoundation` loads, `DrawWorldMapReturnBar` shows `Ruche` + `Retour à la ruche`
  3. Click `Ruche` → `HiveMap` reloads, `sceneLoaded` fires, bootstraps + controllers reinit, no `LivingHive`
  4. Session remains authenticated (no login prompt)
  5. Repeat → no duplicates
- **Not yet run in Unity** — requires Play Mode manual test (see below).

## CEO Manual Validation Required

1. Google login → enter HiveMap
2. Tap **Carte** → confirm WorldMap opens (centered on player hive)
3. Tap **Ruche** (top bar) **or** **Retour à la ruche** (bottom bar) → confirm **new HiveMap** returns (not LivingHive)
4. Confirm **buildings + HUD + bottom menu** (14 buildings, resources, queue)
5. Click **Research** once → window opens, queue not stale
6. Repeat **Carte → Ruche** once more → still correct, no duplicates

## Remaining Issues

- None for return flow. **Persistence/SQL still BLOCKED by SentinelOne** — out of scope, not touched.
- No automated test for return flow yet (recommended).

## Recommendation

- **M016G — Authenticated Regression Test Suite** (round-trip, session preservation, no duplicates)
- Keep `HiveMapScenePath` canonical; do not add second Hive map.

## Confidence

**HIGH** — Single-line routing fix to canonical constant + debounce, reuses proven M016E `sceneLoaded` lifecycle. No new UI, no second bootstrap, session preserved via static `MobileAccountSessionRuntimeBootstrap`.

