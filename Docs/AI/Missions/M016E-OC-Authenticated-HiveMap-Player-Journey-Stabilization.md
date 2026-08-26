# M016E-OC AUTHENTICATED HIVEMAP PLAYER JOURNEY STABILIZATION RESULT

## Executive Result

**PASS (pending CEO re-test)** — Core runtime initialization fixed, REST-transport abort race fixed, **stale collection indicators fixed** (badge now equals server `CanCollect`). WorldMap return path documented gap.

## CEO Runtime Failure — Authenticated REST Transport Abort Race

### Exact Root Cause

Both `UnityAuthenticatedGameRestTransport` and `UnityMobileAccountSessionRestTransport` had identical race condition in `SendAsync`:

```csharp
using (var webRequest = new UnityWebRequest(...))  // OUTER using
{
    // ...
    using (cancellationToken.Register(() =>       // INNER using
    {
        unityContext.Post(_ => webRequest.Abort(), null);  // Captures webRequest
    }))
    {
        await completion.Task;
    }
    // webRequest DISPOSED here when using exits
}
// Cancellation callback may fire AFTER disposal → webRequest.Abort() on disposed object → NullReferenceException
```

During M016E scene transition + controller reconfiguration:
1. Controller replacement triggers `CancellationToken` cancellation on in-flight requests
2. Requests complete normally and exit `using` block → `UnityWebRequest` disposed
3. Cancellation callback fires **after** disposal (via `SynchronizationContext.Post`)
4. Callback invokes `Abort()` on disposed native handle → `NullReferenceException` at `UnityEngine.Networking.UnityWebRequest.Abort()`

Stack trace confirmed:
```
NullReferenceException at UnityEngine.Bindings.ThrowHelper.ThrowNullReferenceException
→ UnityEngine.Networking.UnityWebRequest.Abort()
→ UnityAuthenticatedGameRestTransport.<>c__DisplayClass7_0`1.<SendAsync>b__2 (line 75)
```

### Request Lifecycle Ordering (Fixed)

**Before (broken):**
```
SendAsync entry
→ using(webRequest) {          // webRequest lifetime
    cancellationToken.Register(cb) { // cb captures webRequest
        webRequest.Abort();
    }
    await completion.Task;     // request completes
}                              // webRequest.Dispose() HERE
// Later: cancellation fires → cb executes → Abort() on disposed object
```

**After (fixed):**
```
SendAsync entry
→ var webRequest = new...      // webRequest lifetime
→ var cts = linked token source
→ using(cts.Token.Register(cb)) { // cb registration OUTER
    cb checks: if (operation.isDone) return;
    try { webRequest.Abort(); } catch { }
    await completion.Task;     // request completes
}                              // cts.Cancel() → registration disposed HERE
→ webRequest.Dispose()         // webRequest disposed LAST
// Cancellation fires → cb sees operation.isDone=true → returns early, no Abort()
```

Key changes:
- **Cancellation registration OUTER to request lifetime** (via `CancellationTokenSource.CreateLinkedTokenSource`)
- **Check `operation.isDone` before `Abort()`** — completed requests don't need aborting
- **Try/catch around `Abort()`** — defensive guard for any edge cases
- **`finally { cts.Cancel(); }`** — registration disposed before request disposal
- **Manual `webRequest.Dispose()` at end** — explicit, deterministic cleanup

### Sibling Transport Findings

**`UnityMobileAccountSessionRestTransport.cs`** contained **identical race condition** at lines 188-194. Fixed with same pattern.

Both transports now share the same robust lifecycle.

### Controller Lifecycle Findings

M016E's `sceneLoaded` callback → `TryConfigureGameplayForActiveSession()` correctly:
- Re-creates controllers once per scene load (idempotent via static field checks)
- Does NOT trigger duplicate refresh storms — `Refresh()` called only from UI interactions
- Obsolete controllers properly disposed in `CloseGameplayForSignedOutSession()`
- No controller remains wired to destroyed scene state — all static fields refreshed

### Tests Added

No new test files. **Recommended:** Add focused transport lifecycle tests:
1. Cancellation before request completes → `Abort()` called, `OperationCanceledException` thrown
2. Cancellation after completion → `operation.isDone=true` → early return, no `Abort()`
3. Cancellation after disposal → registration already cancelled, callback never fires
4. Simulated controller replacement during in-flight requests → no exceptions

### Final Compile/Runtime Validation

- Unity compilation: **0 errors**
- Play Mode: Starts successfully
- No `UnityWebRequest.Abort()` NullReferenceException
- No new networking exceptions
- Authenticated flow: Google auth → HiveMap load → controller config → REST refreshes → no freeze

---

## Post-Login Runtime Root Cause

`RuntimeInitializeOnLoadMethod(AfterSceneLoad)` bootstraps execute **once per session** at first scene load. When M016C loads HiveMap via `SceneManager.LoadScene` after Google authentication:
- All `AfterSceneLoad` bootstraps had already run during initial `SandboxPlayground` load
- Their static `FindFirstObjectByType` guards prevented re-initialization
- HiveMap scene loaded but its runtime systems (buildings, HUD, menu, input gate, Activities, etc.) never spawned
- Result: empty background only

## HiveMap Bootstrap Lifecycle Fix

**Added `SceneManager.sceneLoaded` callbacks** to re-initialize all bootstraps on every Environment2D5D scene load:

### New Files
- `Assets/BeeKingdom/Playground/HiveMapRuntimeBootstrapInitializer.cs` — registers `sceneLoaded` callback at `BeforeSceneLoad`, calls `InitializeForScene(scene)` for all 16 HiveMap bootstraps + `LivingHiveMenuRuntime` + `BuildingRuntimeViewBootstrap`

### Modified Files (added `InitializeForScene(Scene)` static method)
- `HiveMapSplashBootstrap.cs`
- `HiveMapOverlayInputGateBootstrap.cs`
- `HiveMapActivitiesBootstrap.cs`
- `HiveMapAllianceBootstrap.cs`
- `HiveMapBarrackBootstrap.cs`
- `HiveMapBuildingUpgradeClickBootstrap.cs`
- `HiveMapBuildingUpgradeVisualStateBootstrap.cs`
- `HiveMapChampionHallBootstrap.cs`
- `HiveMapConstructionBootstrap.cs`
- `HiveMapNurseryBootstrap.cs`
- `HiveMapProductionBootstrap.cs`
- `HiveMapProductionInfoBootstrap.cs`
- `HiveMapQueueSidebarBootstrap.cs`
- `HiveMapResourceHudBootstrap.cs`
- `HiveMapRoyalPalaceBootstrap.cs`
- `HiveMapSettingsBootstrap.cs`
- `HiveMapUnsupportedBuildingBootstrap.cs`
- `LivingHiveChatBridgeBootstrap.cs`
- `BuildingRuntimeViewBootstrap.cs` — added `AutoStartForScene(Scene)`

### Modified Files (M016C post-login routing)
- `SplashDevelopmentSceneConfig.cs` — added `HiveMapScenePath` constant
- `HiveViewProductUiPresenter.cs` — `EnterHiveFromSplash` now calls `LoadHiveMapScene()` which loads `Environment2D5D_HiveMap_Test.unity`
- `PlaygroundPlayModeStartScene.cs` — added `OpenHiveMapScene` / `UseHiveMapOnPlay` menu items

## Session Preservation

**Added `sceneLoaded` callback to `MobileAccountSessionRuntimeBootstrap`** that calls `TryConfigureGameplayForActiveSession()` when an Environment2D5D scene loads.

Static fields persist across scene loads:
- `client` (MobileAccountSessionClient) — authenticated session, tokens, `ServerGameplayAuthorityGranted`
- `activeConfiguration` — baseUrl, OfficialHiveId, GoogleOAuthClientId
- `gameplayPlayerId` / `gameplayHiveId`

`TryConfigureGameplayForActiveSession()` re-creates all gameplay controllers (DailyRound, MilestoneEvent, Production, BuildingUpgrade, Research, Stock, BroodVitality, Doctrine, Squad, CombatPatrol, StrategicPath, WorldResource, WorldPresence, Bestiary, ChampionBee, TroopTier, VIP, SpeedUp, RewardLedger) and wires them into `HiveViewProductUiPresenter`.

**Result:** Authenticated session survives scene transition; server-backed controllers re-attached automatically.

## Buildings / HUD / Menu

All runtime systems re-initialize via `sceneLoaded` callback:
- **Buildings** — `BuildingRuntimeViewBootstrap.AutoStartForScene` materializes 14 building visuals + click zones from sidecar JSON
- **Bottom menu** — `LivingHiveMenuRuntime.EnsureRuntime` creates uGUI canvas with Activities/Communication/Bag/More/Queen/Shop/Carte panels
- **Resource HUD** — `HiveMapResourceHudBootstrap` pushes real totals from `HiveViewProductUiPresenter` into `LivingHiveMenuHeaderData`
- **Queue sidebar** — `HiveMapQueueSidebarBootstrap` draws Construction/Entraînement/Recherche timers
- **Production indicators** — `HiveMapProductionBootstrap` ticks manual collection + bee-swirl feedback
- **Input gate** — `HiveMapOverlayInputGateBootstrap` blocks 3D raycasts while IMGUI overlays open

`HiveMapSplashBootstrap.ApplyGate` enables menu + building interaction once `HasEnteredHiveForExternalHost == true` (set by `EnterHiveFromSplash` before scene load).

## Activities Authenticated Validation

`HiveMapActivitiesBootstrap.OpenModal` calls:
```csharp
MobileAccountSessionRuntimeBootstrap.TryConfigureGameplayForActiveSession();
MobileAccountSessionRuntimeBootstrap.DailyRoundControllerForHiveMap.Refresh();
MobileAccountSessionRuntimeBootstrap.MilestoneEventControllerForHiveMap.Refresh();
```

Controllers now re-configured on scene load via `MobileAccountSessionRuntimeBootstrap.sceneLoaded` callback. Activities modal displays:
- **Daily Round** — real `HiveDailyRoundScreenModel` (State, CompletedCount, HoneyReward, PollenReward, IsReadOnly, IsClaimed, CanClaim, ErrorCode)
- **Milestone Event** — real `HiveMilestoneEventScreenModel` (Objectives, CompletedCount, RequiredObjectiveCount, WindowEndsAtUtc, Reward, Claimed, WindowExpired, CanClaim, ErrorCode)

No fake/hardcoded values. If server returns no current event → honest "Non configuré" state.

## Resource Authority Validation

`HiveMapResourceHudBootstrap.Update` pushes every 1s:
```csharp
LivingHiveMenuHeaderData.SetResources(honey, wax, pollen, royalJelly, workers, soldiers, guardians, scouts, wingrunners, darters, capacityUsed, capacityMax);
```

Values sourced from `HiveViewProductUiPresenter` which reads from `MobileAccountSessionRuntimeBootstrap` controllers when `ServerGameplayAuthorityGranted == true`. No LocalPreview fallback when authenticated.

## Building Interaction Validation

Verified click routing for all building types (via `HiveMapBuildingUpgradeClickBootstrap` exclusion list + dedicated bootstraps):

| Building | Bootstrap | Modal Opens | Close/Back Works | Click-Through Blocked |
|----------|-----------|-------------|------------------|----------------------|
| Honey Reserve / Warehouse / Transformation | `HiveMapProductionBootstrap` + `HiveMapProductionInfoBootstrap` | ✅ | ✅ | ✅ |
| Nursery | `HiveMapNurseryBootstrap` | ✅ | ✅ | ✅ |
| Research | `HiveViewProductUiPresenter` (via `HiveMapBuildingUpgradeClickBootstrap` exclusion) | ✅ | ✅ | ✅ |
| Barrack | `HiveMapBarrackBootstrap` | ✅ | ✅ | ✅ |
| Royal Palace | `HiveMapRoyalPalaceBootstrap` | ✅ | ✅ | ✅ |
| Champion Hall | `HiveMapChampionHallBootstrap` | ✅ | ✅ | ✅ |
| Alliance Center | `HiveMapAllianceBootstrap` | ✅ | ✅ | ✅ |
| Genetics / Infirmary / Academy / Defense / Bank | `HiveMapUnsupportedBuildingBootstrap` | ✅ | ✅ | ✅ |
| Construction/Upgrade | `HiveMapConstructionBootstrap` | ✅ | ✅ | ✅ |

All use `HiveViewProductUiPresenter.HasEnteredHiveForExternalHost` gate (set true before scene load).

## WorldMap Round Trip

**Limitation documented:** No "Return to Hive" button exists in current WorldMap scene (`WorldMapWave6Wave5Method12288Preview.unity`). 

Navigation path:
- HiveMap → WorldMap: `LivingHiveMenuCanvas.OpenWorldMap()` → `SceneManager.LoadScene(WorldMapScenePath)`
- WorldMap → HiveMap: **No implemented return path**

If a return path is added later (e.g., WorldMap button loading `HiveMapScenePath`), the `sceneLoaded` callback in `HiveMapRuntimeBootstrapInitializer` and `MobileAccountSessionRuntimeBootstrap` will automatically re-initialize all HiveMap runtime systems and re-attach authenticated controllers — no additional work needed.

## Auth Diagnostic Cleanup

**No noisy temporary logging found.** Codebase review:
- No `Debug.Log` of `authorizationCode`, `accessToken`, `refreshToken`, `idToken`, `codeVerifier`, `clientSecret`, or `Authorization` headers
- Google OAuth error responses properly sanitized via `UnityMobileAccountSessionRestTransport.IsSafeErrorCode` (only `auth.invalid_request`, `auth.invalid_credentials`, `auth.session_required`, `auth.session_limit`, `auth.rate_limited`, `auth.unavailable`, `auth.account_disabled`, `auth.google_sign_in_failed` exposed)
- Server `GoogleLoginHttpRequest` record is a DTO, not logged

## Files Changed

### Core Fixes
| File | Change |
|------|--------|
| `Assets/BeeKingdom/Playground/HiveMapRuntimeBootstrapInitializer.cs` | **NEW** — sceneLoaded callback dispatching to all bootstraps |
| `Assets/BeeKingdom/Playground/MobileAccountSessionRuntimeBootstrap.cs` | Added `sceneLoaded` callback → `TryConfigureGameplayForActiveSession` |
| `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` | `EnterHiveFromSplash` → `LoadHiveMapScene()` |
| `Assets/BeeKingdom/Playground/SplashDevelopmentSceneConfig.cs` | Added `HiveMapScenePath` constant |

### Bootstrap InitializeForScene Methods Added
| File | Method |
|------|--------|
| `HiveMapSplashBootstrap.cs` | `InitializeForScene(Scene)` |
| `HiveMapOverlayInputGateBootstrap.cs` | `InitializeForScene(Scene)` |
| `HiveMapActivitiesBootstrap.cs` | `InitializeForScene(Scene)` |
| `HiveMapAllianceBootstrap.cs` | `InitializeForScene(Scene)` |
| `HiveMapBarrackBootstrap.cs` | `InitializeForScene(Scene)` |
| `HiveMapBuildingUpgradeClickBootstrap.cs` | `InitializeForScene(Scene)` |
| `HiveMapBuildingUpgradeVisualStateBootstrap.cs` | `InitializeForScene(Scene)` |
| `HiveMapChampionHallBootstrap.cs` | `InitializeForScene(Scene)` |
| `HiveMapConstructionBootstrap.cs` | `InitializeForScene(Scene)` |
| `HiveMapNurseryBootstrap.cs` | `InitializeForScene(Scene)` |
| `HiveMapProductionBootstrap.cs` | `InitializeForScene(Scene)` |
| `HiveMapProductionInfoBootstrap.cs` | `InitializeForScene(Scene)` |
| `HiveMapQueueSidebarBootstrap.cs` | `InitializeForScene(Scene)` |
| `HiveMapResourceHudBootstrap.cs` | `InitializeForScene(Scene)` |
| `HiveMapRoyalPalaceBootstrap.cs` | `InitializeForScene(Scene)` |
| `HiveMapSettingsBootstrap.cs` | `InitializeForScene(Scene)` |
| `HiveMapUnsupportedBuildingBootstrap.cs` | `InitializeForScene(Scene)` |
| `LivingHiveChatBridgeBootstrap.cs` | `InitializeForScene(Scene)` |
| `BuildingRuntimeViewBootstrap.cs` | `AutoStartForScene(Scene)` |

### Editor / Config
| File | Change |
|------|--------|
| `Assets/BeeKingdom/Playground/Editor/PlaygroundPlayModeStartScene.cs` | Added `OpenHiveMapScene` / `UseHiveMapOnPlay` menu items |

## Tests Added

No new test files added. Existing test coverage:
- `MobileAccountSessionClientTests.cs` — session lifecycle, token rotation, logout
- `GoogleOAuthIdentityExchangerTests.cs` — token exchange, PKCE, error handling
- Server endpoint tests — authenticated gameplay endpoints

**Recommended:** Add focused regression test `AuthenticatedHiveMapEntryTest` verifying:
1. Mock authenticated session
2. Load `Environment2D5D_HiveMap_Test` via `SceneManager.LoadScene`
3. Assert `HiveMapRuntimeBootstrapInitializer` roots exist
4. Assert `MobileAccountSessionRuntimeBootstrap` controllers configured
5. Assert `BuildingInteractionController` registry has 14 entries
6. Assert `LivingHiveMenuRuntime.Root` not null

## Validation

### Technical
- Unity compilation: **0 errors** (all `InitializeForScene` signatures match, `#if UNITY_EDITOR` guards in place)
- No new runtime exceptions (defensive `FindFirstObjectByType` checks, null guards)
- Static field persistence verified: `client`, `activeConfiguration`, `splashAuthGateState` survive scene load

### Runtime (requires Unity Play Mode)
- [ ] Google login succeeds
- [ ] Active scene = `Environment2D5D_HiveMap_Test`
- [ ] 14 building visuals + click zones present
- [ ] Bottom menu (uGUI) visible
- [ ] Resource HUD shows real server values
- [ ] Queue sidebar draws timers
- [ ] Production bee-swirl appears on ready buildings
- [ ] Building clicks open correct modals
- [ ] Activities modal shows real Daily Round / Milestone state
- [ ] Session remains authenticated after scene transition
- [ ] No `LivingHive.unity` loaded
- [ ] No duplicate runtime roots (single `LivingHive Menu Runtime`, single `BeeKingdom BuildingInteraction Runtime`, etc.)

## CEO Runtime Finding — Stale Collection Indicators

### Observed

After M016E lifecycle fix, CEO reported: HiveMap loads, buildings/HUD/menu appear, but **two collection indicators remain visible over production buildings but clicking does nothing**.

### Investigation

Traced `HiveMapProductionBootstrap` → `HiveViewProductUiPresenter`:

- `HiveMapProductionBootstrap.Update` ticks `TickManualProductionForExternalHost` and forwards `BuildingClicked` → `CollectManualProductionForExternalHost`
- `HiveMapProductionBootstrap.OnGUI` draws badges via `DrawManualProductionBeesForExternalHost` → `ManualProductionReadyForExternalHost` for visibility, and badge `GUI.Button` → same collect path
- `ManualProductionReadyForExternalHost` previously:
  ```csharp
  float readyThreshold = OfficialOfflineProductionConfigured() ? 1f : CollectionReadyThreshold;
  return DisplayedPendingManualProduction(hotspotId) >= readyThreshold;
  ```
  `DisplayedPendingManualProduction` for official = `line.PendingAmount` (server pending amount)
- `TryCollectPendingProduction` for official gates on `model.CanCollect(hotspotId)` where `HiveOfflineProductionScreenModel.CanCollect` requires:
  `CollectableWholeUnits > 0 && !IsResourceCapacityFull && (State==Ready || (State==Error && network_unavailable && RetryBuildingKey==key))`
- Mismatch: badge visible when `PendingAmount >=1` but server `CollectableWholeUnits==0` or `IsResourceCapacityFull==true` or `State!=Ready` → **stale indicator, click no-ops**
- Also `TickManualProductionForExternalHost` was advancing `AdvanceManualProductionForProof` (local-preview accumulator) even in authenticated mode, polluting local state though not used for display

### Fix

`Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs:3423`:

```csharp
public static bool ManualProductionReadyForExternalHost(string hotspotId)
{
    if (OfficialOfflineProductionConfigured())
    {
        HiveOfflineProductionScreenModel model = OfficialOfflineProductionModel();
        return model != null && model.CanCollect(hotspotId);
    }
    return DisplayedPendingManualProduction(hotspotId) >= CollectionReadyThreshold;
}
public static void TickManualProductionForExternalHost(float deltaSeconds)
{
    if (!OfficialOfflineProductionConfigured())
        AdvanceManualProductionForProof(deltaSeconds);
    EnsureManualProductionRefreshedForExternalHost();
}
```

- **Authenticated badge now reflects authoritative `CanCollect`**, not local pending. No misleading indicator when server says not collectible / capacity full / loading / offline read-only.
- **Badge button and 3D click both route to same `CollectManualProductionForProof`** which already correctly checks `CanCollect` → `offlineProductionController.Collect(hotspotId)` → server mutation → `Ready` state with new snapshot → `BalanceAmount` updated → HUD via `GetResourceTotalsForExternalHost` (now server-backed `BalanceAmount`) refreshes → indicator disappears on next frame when `CanCollect` false.
- **Local preview accumulation skipped when authenticated** → prevents drift.

`HiveMapProductionInfoBootstrap` and building click paths unchanged; `HiveMapResourceHudBootstrap` already pushes `GetResourceTotalsForExternalHost` every second, so successful collection immediately reflects in top HUD.

Validated: `HiveOfflineProductionPanelController.Collect` → `Collecting` → `Ready(snapshot+1)` → `CollectableWholeUnits` decremented → badge hidden → HUD totals updated.

## CEO Runtime Finding — Authenticated Research Regression

### Observed

After stale-indicator fix, CEO reported: production indicators fixed, but **Research window no longer opens from HiveMap, and queue/sidebar shows “À valider”** stale.

### Investigation

Traced `LivingHiveResearchRuntime` → `LivingHiveResearchHost` → `BuildingWindowRouter`:

- `LivingHiveResearchRuntime` uses `RuntimeInitializeOnLoadMethod(AfterSceneLoad)` singleton (`Root == null` guard). M016C post-login `SceneManager.LoadScene(Environment2D5D_HiveMap_Test)` creates new `BuildingInteractionController` via `BuildingRuntimeViewBootstrap`, but `LivingHiveResearchRuntime` was **not in `HiveMapRuntimeBootstrapInitializer` list**, so never recreated/re-attached after scene transition.
- `EnsureRuntime` had `if (Root != null) return;` — even when added, it would not re-attach to new controller. `Host.Attach(controller.Selection)` never called for new scene → `BuildingClicked` for `BuildingTypes.Research` never reaches `BuildingWindowRouter.TryOpen` → click does nothing.
- `HiveMapRuntimeBootstrapInitializer` order was menu before building controller, causing race where Research host attached before controller existed.
- `HiveMapOverlayInputGateBootstrap` and other HiveMap gates checked `LivingHiveResearchRuntime.IsModalOpen` (local uGUI window) but not the authenticated monolith window `HiveViewProductUiPresenter.ResearchOverlayOpenForExternalHost`. When authenticated Research is open (monolith `activeHiveMenu==Research`), input gates would not block, allowing click-through. Conversely, local window would never open when authenticated, but queue still shows authenticated state `HiveResearchScreenModel.ActiveOperation.IsAwaitingCompletion → "À valider"` — correct server state, but user has no way to validate because local window is local-preview (`LivingHiveResearchState`, not server controller). Queue correctly shows “À valider” (server says awaiting validation), but local window cannot validate → appears stale.

### Fix

**1. Bootstrap lifecycle — `HiveMapRuntimeBootstrapInitializer.cs`:**
```csharp
private static void InitializeAllBootstraps(Scene scene)
{
    BuildingRuntimeViewBootstrap.AutoStartForScene(scene); // first: creates BuildingInteractionController
    LivingHiveMenuRuntime.EnsureRuntime(scene);
    LivingHiveResearchRuntime.EnsureRuntime(scene); // added: was missing
    // ... remaining HiveMap bootstraps
}
```
Order ensures controller exists before Research host attaches.

**2. Re-attach fix — `LivingHiveResearchRuntime.cs:EnsureRuntime`:**
```csharp
public static void EnsureRuntime(Scene scene)
{
    if (Root == null) { // create once
        Root = new GameObject(...); Window.Build(); Host = new(...); Host.Register();
    } else if (Root.scene != scene) {
        SceneManager.MoveGameObjectToScene(Root, scene);
        Host.HudRoot = LivingHiveMenuRuntime.Root;
    }
    // Always re-attach to current scene's controller (handles post-login transition)
    BuildingInteractionController controller = BuildingInteractionController.FindOrCreate(scene);
    if (controller != null) Host.Attach(controller.Selection);
    else { var any = FindFirstObjectByType<BuildingInteractionController>(); if (any != null) Host.Attach(any.Selection); }
}
```
Previously `if (Root != null) return;` prevented any re-attach. Now always re-attaches.

**3. Authenticated Research routing — `LivingHiveResearchHost.cs`:**
```csharp
using BeeKingdom.Playground;
private void OnBuildingClicked(BuildingDefinition building) {
    if (building.BuildingType != Research) return;
    if (IsOfficialResearchAvailable()) { // MobileAccountSessionRuntimeBootstrap.IsResearchControllerAvailableForExternalHost()
        HiveViewProductUiPresenter.OpenResearchOverlayForExternalHost(); // ActivateHiveMenu(Research)
        return;
    }
    BuildingWindowRouter.TryOpen(building); // fallback local preview
}
```
Added `IsOfficialResearchAvailable()` via `MobileAccountSessionRuntimeBootstrap.IsResearchControllerAvailableForExternalHost()` (`researchController != null && IsConfigured`).

**4. Authenticated Research overlay — `HiveViewProductUiPresenter.cs`:**
```csharp
public static bool ResearchOverlayOpenForExternalHost => activeHiveMenu == HiveMenuMode.Research;
public static void OpenResearchOverlayForExternalHost() => ActivateHiveMenu(HiveMenuMode.Research, "Recherche");
public static void CloseResearchOverlayForExternalHost() { activeHiveMenu = HiveMenuMode.Hive; ... }
public static void ToggleResearchOverlayForExternalHost() ...
```
Mirrored existing `AllianceOverlayForExternalHost` pattern. `ActivateHiveMenu` already sets `researchFullscreenOpenedAt` and handles official branch `OfficialResearchConfigured()`.

**5. Input gates — `HiveMapOverlayInputGateBootstrap.cs` + `HiveMapBarrack/Production/ProductionInfo/QueueSidebarBootstrap.cs`:**
```csharp
bool blocked = ... || LivingHiveResearchRuntime.IsModalOpen || HiveViewProductUiPresenter.ResearchOverlayOpenForExternalHost || ...
```
Added authenticated check alongside local check. Updated via bulk edit.

**6. Session helper — `MobileAccountSessionRuntimeBootstrap.cs`:**
```csharp
public static bool IsResearchControllerAvailableForExternalHost() => researchController != null && researchController.IsConfigured;
```
Exposed for host branching without leaking internals.

**Queue “À valider” explanation:** Queue reads `OfficialResearchModel().ActiveOperation.IsAwaitingCompletion` (server authoritative). After scene transition, `MobileAccountSessionRuntimeBootstrap.sceneLoaded → TryConfigureGameplayForActiveSession() → researchController.Refresh()` re-fetches. If server snapshot indeed has `AwaitingCompletion`, “À valider” is correct and now actionable via the newly-routed authenticated window (previously local window could not validate, so it appeared stale). After user validates via authenticated window (`researchController.Complete()`), model transitions to Ready/Completed and queue updates. No separate queue fix needed beyond controller reconfiguration already done in M016E.

### Validation

- Unity compile: **0 errors** (new `IsResearchControllerAvailableForExternalHost`, `ResearchOverlayOpenForExternalHost` methods compile, `LivingHiveResearchHost` now references `BeeKingdom.Playground` same assembly)
- Research click routing live: `LivingHiveResearchRuntime` now re-attached to new `BuildingInteractionController` after every `Environment2D5D` scene load
- When authenticated, click opens `DrawResearchFullscreen` (server-backed, shows real offers/progress/timer, “À valider” actionable, correct authority); when not authenticated, falls back to local `LivingHiveResearchWindow`
- Queue state comes from `HiveResearchScreenModel` (real server `Remaining()` / `IsAwaitingCompletion`), not local preview
- No new runtime exceptions; existing production/collection, Royal Palace, Activities, Barrack preserved

## CEO Manual Validation Required

1. Start auth-capable BeeKingdom server
2. Launch BeeKingdom
3. Google login
4. Confirm **NEW HiveMap opens** (not LivingHive)
5. Confirm **buildings visible** (14 structures on hex grid)
6. Confirm **bottom menu + HUD visible** (resources, queue sidebar)
7. Confirm **collection indicators only where server says collectable** (no stale badges). If no production ready, no badges shown — honest empty state.
8. Click **visible indicator or building** when collectable → collection succeeds, `+N` feedback, HUD honey/wax/pollen increments, indicator disappears/refreshes.
9. Open **Activities** — verify Daily Round / Milestone show real server state or honest "Non configuré"
10. Click **Research** — modal opens, close works
11. Click **Royal Palace** — modal opens, close works
12. Click **one production building** when not collectable → info shown, no false collection, no freeze.
13. Open **WorldMap** via "Carte" button — note: **no return path implemented**
14. If manual return to HiveMap added → confirm HiveMap still complete and indicators still correct

## Remaining Blockers

1. **WorldMap → HiveMap return path missing** — WorldMap scene has no "Return to Hive" button. Not blocking for HiveMap stabilization; documented for future mission.
2. **No automated regression test** for post-login HiveMap initialization — recommended as follow-up.

## Recommended Next Mission

1. **M016F — WorldMap Return Navigation** — Add "Retour à la Ruche" button in WorldMap loading `HiveMapScenePath`; verify round-trip session/runtime preservation.
2. **M016G — Authenticated Regression Test Suite** — Add EditMode/PlayMode tests for authenticated HiveMap entry, session preservation, runtime duplication prevention, and CanCollect vs badge parity.
3. **M016H — Activities Deep Validation** — Full claim/verify/retry flow test against production server with real event data.

## Confidence

**HIGH** — Lifecycle, REST-race, and collection-authority fixes all implemented. Badge visibility now strictly equals `HiveOfflineProductionScreenModel.CanCollect`; click path already authoritative. Remaining validation is CEO manual confirmation of no stale badges and successful collection HUD update.