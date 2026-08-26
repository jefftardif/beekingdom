# M016E-CL AUTHENTICATED RESEARCH ASSEMBLY BOUNDARY FIX

## Compile Root Cause

`LivingHiveResearchHost.cs` lives in `Assets/Experiments/Environment2D5D/LivingHiveMenu/`, compiled into the `BeeKingdom.LivingHiveMenu` assembly (`BeeKingdom.LivingHiveMenu.asmdef`). Its `references` list is `UnityEngine.UI, BeeKingdom.Buildings, BeeKingdom.Localization, BeeKingdom.Core, BeeKingdom.LocalPreviewData, Unity.TextMeshPro` — no reference to the implicit default assembly (`Assembly-CSharp`), where `HiveViewProductUiPresenter` and `MobileAccountSessionRuntimeBootstrap` live (both under `Assets/BeeKingdom/Playground/`, no `.asmdef` of their own).

Unity does not allow a custom `.asmdef` assembly to reference the implicit default assembly at all — this is a hard platform rule, not a style preference. OC's `using BeeKingdom.Playground;` plus the two direct type references could not resolve, producing the reported `CS0103` errors.

## OC Change Intent

`git diff` against the last commit showed a small, clean, single-purpose change to `LivingHiveResearchHost.OnBuildingClicked`:

- if `HiveViewProductUiPresenter.ResearchOverlayOpenForExternalHost` is already true, do nothing;
- else if `MobileAccountSessionRuntimeBootstrap.IsResearchControllerAvailableForExternalHost()` is true, call `HiveViewProductUiPresenter.OpenResearchOverlayForExternalHost()` and return;
- otherwise fall through to the existing `BuildingWindowRouter.TryOpen(building)` (the local-preview fullscreen window).

Both referenced members already existed and were legal to call — `ResearchOverlayOpenForExternalHost` / `OpenResearchOverlayForExternalHost` / `CloseResearchOverlayForExternalHost` / `ToggleResearchOverlayForExternalHost` in `HiveViewProductUiPresenter.cs`, and `IsResearchControllerAvailableForExternalHost()` in `MobileAccountSessionRuntimeBootstrap.cs`. OC's intent was correct and matches the mission's own required result ("current server-backed/current Research data reaches the window"): route authenticated players with a configured official research session to the official, server-backed overlay, and leave everyone else on the existing local-preview window. The only defect was reaching those members through an illegal direct reference instead of a bridge.

## Assembly Boundary

`BeeKingdom.Core.Integration` (assembly `BeeKingdom.Core`, already in `BeeKingdom.LivingHiveMenu.asmdef`'s reference list) already holds this exact pattern twice — `LivingHiveActivitiesBridge` and `LivingHiveSettingsBridge` — and `LivingHiveMenuCanvas.cs` (same assembly as the Research host) already calls both directly (`LivingHiveActivitiesBridge.IsOpen`, `LivingHiveSettingsBridge.ToggleOverlay()`), confirming this is the established, already-working direction: Playground/authenticated runtime → bridge in `BeeKingdom.Core.Integration` → LivingHiveMenu consumes the bridge.

Added `LivingHiveResearchBridge` following that exact shape (`Func<bool> IsOfficialOpen`, `Func<bool> IsOfficialAvailable`, `Action OpenOfficialOverlay`, all set once via `SetHandlers`). `LivingHiveResearchHost` now depends only on this bridge; the `using BeeKingdom.Playground;` line and both direct type references were removed.

## Research Runtime Root Cause

This is the actual reason Research never opened for authenticated players, independent of the compile break: `HiveViewProductUiPresenter.OpenResearchOverlayForExternalHost()` only sets `activeHiveMenu = HiveMenuMode.Research` — it changes state but draws nothing. The only method that ever draws `HiveMenuMode.Research` content is `DrawActiveHiveMenuPanel`, which is itself only called from inside `DrawInternal` (behind the monolith's own full `Draw()` wrapper). In HiveMap, the *only* bootstrap that ever calls the full `Draw()` is `HiveMapSplashBootstrap`, and its `OnGUI()` explicitly stops calling it the moment `HiveViewProductUiPresenter.HasEnteredHiveForExternalHost` becomes true (`if (HasEnteredHiveForExternalHost) return;`). So `OpenResearchOverlayForExternalHost()` correctly flipped the internal state, but nothing in the post-login HiveMap OnGUI pipeline ever rendered it — a silent no-op from the player's perspective. Every other overlay (Barrack, Construction, Alliance, Settings, Communication) already has its own dedicated bootstrap calling a narrow `Draw*OverlayForExternalHost` method every frame; Research's Open/Close/Toggle triplet existed, but its matching `Draw*OverlayForExternalHost` and calling bootstrap did not.

Fix: added `HiveViewProductUiPresenter.DrawResearchOverlayForExternalHost(bool compact)` (mirrors `DrawAllianceOverlayForExternalHost` exactly: guard on `ResearchOverlayOpenForExternalHost`, `EnsureStyles()`, then draw). It is safe to call `DrawActiveHiveMenuPanel` directly from there because every other branch inside that method is gated on a different, mutually-exclusive `activeMainMenuId`/`activeHiveMenu` value. Added new `HiveMapResearchBootstrap.cs` (same auto-bootstrap pattern as every other `HiveMap*Bootstrap`) that (a) wires `LivingHiveResearchBridge.SetHandlers(...)` in `Start()`, and (b) calls `DrawResearchOverlayForExternalHost(compact)` every `OnGUI()` frame while the player has entered the hive.

Also found and fixed: neither `DrawResearchMenuPanel` (local-preview) nor `DrawOfficialResearchMenuPanel` (server-backed) passed `backButton: true` to their shared `DrawMenuHeader(...)` call, unlike every sibling panel reachable through the same router (e.g. `DrawArmyMenuPanel` does pass `true` and handles the returned click by closing). Without this, once the official overlay opened there was no way to close it from inside the panel. Added `backButton: true` plus the same close-handling block already used by Army (`AudioManager` click/close sounds, `CloseResearchOverlayForExternalHost()`, `return`) to both Research draw methods.

This does not touch `LivingHiveResearchWindow`/`LivingHiveResearchRuntime`/M011's modal-safe suppression wiring at all — that entire path (the local-preview fullscreen window and its `closing_arrow.png` back control) is untouched and still the fallback for every non-authenticated or session-unconfigured player, exactly as before.

## Queue `À valider` Root Cause

Traced `HiveViewProductUiPresenter.DrawQueueSidebarForExternalHost`: the Research card's status string is `officialResearch.ActiveOperation.IsAwaitingCompletion ? BeeLocalization.Text("research.official.queue.ready", "À valider") : ...`. This is a **genuine, correct business state** (localization key `research.official.queue.ready`) — it means a real server-side research operation has finished its timer and is waiting for the player to complete/claim it, the exact same "awaiting completion" concept already used for Construction (`building_upgrade.queue.ready`) and Training (`formation_readiness.official.queue.ready`). It is not a bug string, a legacy fallback, or a mislabeled state.

What made it look broken: `researchController.Refresh()` is called from exactly one place in the entire monolith — inside `RunOfficialResearchAction`, itself only reachable from a button click inside the Research panel. There was no periodic refresh anywhere (unlike `buildingUpgradeController`, which the queue sidebar itself refreshes every 5 seconds, and unlike recruitment, which has `RefreshDoctrineRecruitmentIfDueOrJustFinished()`). Combined with Research never opening (root cause above), the model was frozen at whatever it was at session start (`EnsureHiveThenRefreshGameplayState()`), and the player had no way to either see fresh state or act on it. So: **(b) controller not refreshed**, compounding with the missing-draw-call regression, not a stale LocalPreview model or a legacy fallback.

Fix: added the same periodic-refresh pattern already used for building upgrades to `DrawQueueSidebarForExternalHost`, on its own dedicated timer field (`officialResearchQueueLastLiveRefreshAt`, kept separate from `officialProductionDetailLastLiveRefreshAt` so the two cadences never interfere). Now, even before a player opens Research, the sidebar's "À valider" reflects a value refreshed at most 5 seconds ago; and once Research opens (fixed above), the player can act on it via the same "Terminer"/complete action already implemented in `DrawOfficialResearchMenuPanel`.

## Changes

1. **`LivingHiveResearchHost.cs`** — removed illegal `using BeeKingdom.Playground;` and direct `HiveViewProductUiPresenter`/`MobileAccountSessionRuntimeBootstrap` references; routes through `LivingHiveResearchBridge` instead. Behavior preserved exactly (open official when available and not already open, else fall back to the existing local-preview window).
2. **`LivingHiveResearchBridge.cs`** (new) — `BeeKingdom.Core.Integration` bridge, same shape as `LivingHiveActivitiesBridge`.
3. **`HiveMapResearchBootstrap.cs`** (new) — wires the bridge's handlers to the real presenter/session methods; draws the official overlay every frame while open.
4. **`HiveViewProductUiPresenter.cs`**:
   - added `DrawResearchOverlayForExternalHost(bool compact)` (the missing draw call — root cause of "Research window did not open when authenticated");
   - added `backButton: true` + close handling to `DrawResearchMenuPanel` and `DrawOfficialResearchMenuPanel`'s `DrawMenuHeader` calls (the overlay had no way to close once opened);
   - added a periodic `researchController.Refresh()` poll to `DrawQueueSidebarForExternalHost`, mirroring the existing building-upgrade refresh pattern, on a new dedicated timer field.
5. **`MobileAccountSessionRuntimeBootstrap.cs`** — added `ResearchControllerForHiveMap` (same `IHiveResearchPanelController`/`Unavailable*` accessor pattern already established for offline production, brood vitality, daily round, milestone event), used by `HiveMapResearchBootstrap` to force a fresh read right when the official overlay opens.
6. **`LivingHiveResearchBridgeTests.cs`** (new) — focused EditMode tests (below).

## Files Changed

- `Assets/Experiments/Environment2D5D/LivingHiveMenu/LivingHiveResearchHost.cs` (modified)
- `Assets/BeeKingdom/Core/Integration/LivingHiveResearchBridge.cs` (new)
- `Assets/BeeKingdom/Playground/HiveMapResearchBootstrap.cs` (new)
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` (modified)
- `Assets/BeeKingdom/Playground/MobileAccountSessionRuntimeBootstrap.cs` (modified)
- `Assets/BeeKingdom/Tests/Editor/Interaction/LivingHiveResearchBridgeTests.cs` (new)

Not touched: `LivingHiveResearchRuntime.cs` (already clean, no illegal references — only referenced by other files for its pre-existing `IsModalOpen`), `LivingHiveResearchWindow.cs`, `LivingHiveResearchSpec.cs`, `HiveMapOverlayInputGateBootstrap.cs` and `HiveMapQueueSidebarBootstrap.cs` (both already correctly included `HiveViewProductUiPresenter.ResearchOverlayOpenForExternalHost` in their suppression conditions from earlier M016E work — confirmed by reading, not assumed), Barrack, Construction, Alliance, Royal Palace, Nursery, Production, Activities code.

## Tests

Added `LivingHiveResearchBridgeTests.cs` (`Assets/BeeKingdom/Tests/Editor/Interaction/`, same location/namespace convention as the existing `LivingHiveMenuHeaderTests`/`LivingHiveMenuPortTests`):

- `UnconfiguredBridgeBehavesHonestly` — no handlers set: `IsOfficialOpen`/`IsOfficialAvailable` both false, `OpenOfficialOverlay()` never throws.
- `SetHandlersWiresAllThreeDelegatesIndependently` — delegates are live-queried, not cached at `SetHandlers` time.
- `HostRoutesToOfficialOverlayWhenBridgeReportsAvailable` — official available + not open → routes through the bridge, local-preview window never opens.
- `HostFallsBackToLocalPreviewWindowWhenNoOfficialSessionIsAvailable` — official unavailable → local-preview window opens, bridge never called.
- `HostDoesNotReopenWhenOfficialOverlayIsAlreadyOpen` — official already open → neither path fires again.
- `LivingHiveMenuAssemblyNeverReferencesTheDefaultPlaygroundAssembly` — reflects over `typeof(LivingHiveResearchHost).Assembly.GetReferencedAssemblies()` and asserts `Assembly-CSharp` is absent; turns this exact regression into a structural test failure instead of only a compiler error.

Result: 6/6 passed.

## Validation

- `assets-refresh`: 0 compile errors (confirmed via fresh console read, not just the refresh success message) after every edit, both `BeeKingdom.LivingHiveMenu` and `Assembly-CSharp`.
- Play Mode entered/exited cleanly in `Environment2D5D_HiveMap_Test` (the correct scene was reopened explicitly after an initial Play Mode attempt ran against an unrelated empty scene). Zero exceptions/errors across the whole session.
- `HiveMap Research Runtime` bootstrap instantiated correctly.
- Bridge wiring confirmed live: `LivingHiveResearchBridge.IsOfficialOpen/IsOfficialAvailable` correctly reflect `HiveViewProductUiPresenter.ResearchOverlayOpenForExternalHost` / `MobileAccountSessionRuntimeBootstrap.IsResearchControllerAvailableForExternalHost()` in real time (not cached).
- Research click route exercised through the real production path (`BuildingSelectionService.NotifyClicked`, the same event a real 3D click raises), unauthenticated: correctly fell back to the local-preview window (`LivingHiveResearchRuntime.IsModalOpen` became `True`), official overlay stayed closed.
- Official overlay path exercised directly (`OpenResearchOverlayForExternalHost()` → real `OnGUI` frames → `CloseResearchOverlayForExternalHost()`): zero errors/exceptions across multiple real frames, confirming the new `DrawResearchOverlayForExternalHost` call and the new header/close code render safely even in the `NotConfigured`/session-required state (no live session was available to authenticate with, per the mission's explicit prohibition on CEO credentials).
- Input gate: `BuildingInteractionController.IsEnabled` confirmed `False` while the official Research overlay was open (click-through prevention verified for the new path, not assumed).
- Regressions checked live via the same real click path: Barrack still opens (`BarrackOverlayOpenForExternalHost=True`), Alliance still opens (`AllianceOverlayOpenForExternalHost=True`), Research still opens the local-preview window a second time after all the above. Bank's click no longer opens Construction — traced to `HiveMapBuildingUpgradeClickBootstrap.ExcludedBuildingTypes`, which already includes `BuildingTypes.Bank` as of the pre-existing M013-CX wave 4 (unrelated to this mission; confirmed by reading the file, not assumed to be caused by this change).
- `LivingHiveResearchBridgeTests`: 6/6 passed. Ran the pre-existing `LivingHiveMenuPortTests` as an extra regression check on the same assembly: 6 pre-existing failures (rail entry count, Quests/Chat/Settings panel toggles) unrelated to Research/this mission's files — confirmed by inspection that this mission touched none of `LivingHiveMenuCanvas.cs`/`LivingHiveMenuSpec.cs`, so these are a stale/pre-existing test-vs-code mismatch, not a regression introduced here.
- No server-side changes made this mission; no server tests rerun.

## CEO Manual Validation Required

- Log in with Google auth, land in HiveMap, click Research: the official server-backed panel should now open (previously it silently did nothing).
- Confirm the panel shows real research offers/costs/active-operation state from the server, not local-preview placeholder cards.
- Confirm the new top-left back arrow/header closes the panel and returns control to HiveMap (building clicks, rail, header) without click-through.
- If a research operation is genuinely awaiting completion, confirm the queue sidebar's "À valider" card lets you open Research and tap "Terminer" to actually clear it — and that it's no longer permanently stuck once cleared.
- Log out / use a device without an authenticated official session and confirm Research still opens the existing local-preview fullscreen window exactly as before (no regression to the unauthenticated path).
- Spot-check Barrack, Construction/Upgrade, Alliance, Royal Palace, Nursery, Activities per the mission's PRESERVE list.

## Remaining Issues

- Could not validate the authenticated/official path visually or with real server data — no CEO credentials were used, per the mission's explicit instruction. All official-path validation was structural/state-level (see Validation).
- `LivingHiveMenuPortTests` has 6 pre-existing failures unrelated to this mission (rail entry count expectations stale against the current 5-entry `LivingHiveMenuSpec`, plus Quests/Chat/Settings panel-toggle assertions). Flagged for a separate, unrelated cleanup pass — not touched here per the mission's scope.
- `screenshot-game-view` remains unavailable as an invokable MCP tool in this session (same limitation noted in prior missions); all validation here was state/log-based, not visual.

## Confidence

MEDIUM-HIGH

The compile fix and assembly-boundary reasoning are certain (0 errors, structural test locks it in). The two runtime root causes (missing draw call; missing periodic research refresh) are supported by direct code tracing and confirmed live in Play Mode up to the point a real authenticated session is required. Not HIGH only because the actual official-session `Ready` rendering (real cards, real costs, real "À valider" clearing end-to-end) could not be observed with live server data under this mission's constraints.

---

## ADDENDUM — Freeze Investigation (post-CEO-report)

CEO reported that after authenticated Google login and successful HiveMap entry, clicking the Research building now freezes the entire Unity runtime — more severe than the earlier "does not open" symptom, and treated as the primary blocker.

### Investigation performed

Read every method in the click→open→refresh→draw chain end to end, specifically hunting for the patterns the mission listed as suspects:

- **Synchronous `.Result`/`.Wait()` on an async operation**: none found. Searched `HiveResearchPresentation.cs` (`HiveResearchPanelController`) and `HiveResearchClient.cs` explicitly for `.Result`, `.Wait()`, `GetAwaiter().GetResult()`, `lock`, `Monitor.Enter`, `.WaitOne(` — zero matches. `Refresh()`/`Start()`/`Complete()` are all `async void` fire-and-forget wrappers around `Task`-returning core methods, exactly matching the already-proven-working `HiveStockPanelController`/`HiveBroodVitalityPanelController`/`HiveOfflineProductionPanelController` pattern.
- **Blocking refresh on the main thread**: `RefreshCoreAsync` only awaits `client.ReadAsync(...)`, which itself only awaits `RequireSessionAsync`/`transport.SendAsync` — both `async`/`await` all the way down, every await using `.ConfigureAwait(false)`, consistent with every other `Hive*Client` in the project (16 total, all built on the same shared pattern). This shared layer is exercised successfully today by Barrack/Construction/Production per the CEO's own confirmation that those work post-login, making it an unlikely Research-only culprit.
- **Recursive open/refresh callback**: `ActivateHiveMenu(HiveMenuMode.Research, ...)` (called by `OpenResearchOverlayForExternalHost`) only sets local fields (`activeHiveMenu`, timestamps, a status message) - no event is raised, nothing calls back into `LivingHiveResearchBridge` or `LivingHiveResearchHost`. No recursion found.
- **Infinite loop/poll**: the only loop touched by this mission's own changes is `DrawOfficialResearchMenuPanel`'s `for` over `LocalPreviewResearchCatalog.All` (a small, fixed, already-loaded local catalog - not server data, cannot itself hang waiting on I/O). No `while(true)` or unbounded loop found anywhere in the reachable chain.
- **Deadlock between Research host/window and `HiveResearchPanelController`**: `LivingHiveResearchHost` never touches `HiveResearchPanelController` at all (that's precisely why the bridge exists) - there is no shared lock or handle between them that could deadlock.
- **Controller reconfiguration during the click**: confirmed `HiveMapResearchBootstrap.OpenOfficialOverlay()` calls only `OpenResearchOverlayForExternalHost()` (field assignment) and `ResearchControllerForHiveMap.Refresh()` (fire-and-forget on the *existing* controller) - it never calls `TryConfigureGameplayForActiveSession()` or otherwise tears down/rebuilds controllers (unlike `HiveMapActivitiesBootstrap.OpenModal()`, which does call full reconfiguration on every open and is a heavier, riskier pattern already in production use elsewhere).
- **Duplicated scene/bootstrap subscription**: `HiveMapResearchBootstrap` is *not* listed in `HiveMapRuntimeBootstrapInitializer.InitializeAllBootstraps` (unlike every sibling `HiveMap*Bootstrap`). Traced why this is not itself the freeze cause: `HiveMapSplashBootstrap` draws the login/auth screen *inside* the same already-loaded HiveMap scene rather than through a separate scene load, so `HiveMapResearchBootstrap`'s own `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` AutoStart already fires once, correctly, before login completes - no second scene load is needed to create it, and `Host.Attach(...)`/`LivingHiveResearchBridge.SetHandlers(...)` are each idempotent (`Attach` calls `Detach` first; `SetHandlers` just reassigns three delegate fields), so even a hypothetical double-init would not compound into a hang. Flagged as a real omission worth fixing for consistency (see Remaining Issues) but not identified as the freeze's root cause.

**No synchronous blocking construct, deadlock pattern, or infinite loop was found by static analysis** across every file this mission touches or calls into. This was verified as thoroughly as source review allows without a live authenticated session (explicitly out of scope - "Do not authenticate as CEO").

### Instrumentation added (safe, no behavior change)

Added `[M016E-FREEZE-PROBE]` tagged `Debug.Log` calls bracketing every candidate call, so the *next* repro's Console output will show the exact last checkpoint reached before the hang:

- `LivingHiveResearchHost.OnBuildingClicked` - logs entry, the `IsOfficialOpen`/`IsOfficialAvailable` bridge reads, and both the official and fallback branches (before/after each).
- `HiveMapResearchBootstrap.OpenOfficialOverlay` - logs before/after `OpenResearchOverlayForExternalHost()`, then the controller's `IsConfigured`/`IsBusy` state immediately before calling `.Refresh()`, then confirms the (synchronous part of the) call returned.
- `HiveMapResearchBootstrap.OnGUI` - logs the first frame `DrawResearchOverlayForExternalHost` is called while open, and its return, once per open (not every frame).
- `HiveResearchPanelController.RefreshCoreAsync` (`HiveResearchPresentation.cs`) - logs immediately before and after the one network `await client.ReadAsync(...)` call. If "before" logs but "after" never does, the hang is inside the transport/session layer, not this controller.
- `DrawOfficialResearchMenuPanel`'s definitions loop - logs start/end, throttled to once/second.
- The queue sidebar's periodic research refresh (`DrawQueueSidebarForExternalHost`) - logs each firing, to rule it in/out with evidence (it only runs while Research is *closed*, so it is structurally unlikely to be the click-freeze, but is logged anyway rather than assumed innocent).

Verified in Play Mode (unauthenticated, local-preview fallback path) and in the EditMode tests (which exercise the official-available branch with fake delegates): every probe fires and pairs correctly (each "before" has a matching "after"), zero exceptions, zero regressions, `LivingHiveResearchBridgeTests` still 6/6. This confirms the instrumentation itself does not alter behavior or introduce a new blocking point.

### What CEO should do next

Reproduce the freeze once more with this build. The Console will now show a sequence of `[M016E-FREEZE-PROBE]` lines ending at the exact call that never returns - please paste that tail (and any Unity stack trace/hang report if the Editor emits one, e.g. via a forced pause or crash dump) back for a targeted fix. Given the static review above, the most likely remaining candidates, in order of suspicion, are:

1. Something inside the shared session/transport layer (`RequireSessionAsync` → `refreshable.GetFreshSessionAsync`/`RefreshAfterUnauthorizedAsync`) specifically on **first-ever use for Research's `hiveId`/token combination** - since Research's official path was unreachable before this mission's fix, this exact call may never have executed in production before, unlike the already-proven Barrack/Construction/Production paths.
2. A Unity API called from a background-thread continuation after one of the `.ConfigureAwait(false)` awaits (none of the code reviewed does this today, but it's the one category that can't be fully ruled out by reading alone - it would depend on what the platform-specific OAuth/token-refresh implementation does internally).

### Changes made this addendum

- `LivingHiveResearchHost.cs`, `HiveMapResearchBootstrap.cs`, `HiveResearchPresentation.cs`, `HiveViewProductUiPresenter.cs` - diagnostic `Debug.Log` instrumentation only, all tagged `[M016E-FREEZE-PROBE]` and commented as temporary. No retries, no delays, no behavior suppression, no new blocking/async logic.

### Remaining Issues (addendum)

- `HiveMapResearchBootstrap.InitializeForScene(scene)` is missing from `HiveMapRuntimeBootstrapInitializer.InitializeAllBootstraps`, unlike every sibling bootstrap. Not identified as the freeze's cause (see above), but left as a real gap for a scenario this mission could not test (an explicit scene reload after login, rather than in-place login within an already-loaded HiveMap scene). Not fixed here to keep this addendum strictly diagnostic, per "do not suppress the freeze symptom" / minimal-change instructions - flagging for the next wave instead of bundling an unrelated fix into a freeze investigation.
- The freeze itself remains unresolved - this addendum adds only the instrumentation needed to localize it on the next repro, as instructed.

### Confidence (addendum)

MEDIUM

High confidence that no *statically visible* synchronous blocking construct exists in the reviewed code. Cannot be fully certain without a live repro, since the affected path (Research's official session/transport calls) has never executed in production before this mission and could carry a defect only reachable at runtime with a real token/session state.

Stop for GPT/CEO validation.
