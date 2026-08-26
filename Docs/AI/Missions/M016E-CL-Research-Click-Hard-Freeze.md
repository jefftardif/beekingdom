# M016E-CL RESEARCH CLICK HARD FREEZE RESULT

## Exact Freeze Root Cause

**Not conclusively identified yet.** This report documents an exhaustive trace and static-analysis pass across the entire reachable click→open→refresh→draw chain, using CEO-supplied log evidence, and lands on a much more precise instrumentation state than before — but the exact blocking statement has not been isolated with certainty and requires one more repro to confirm.

## Last Synchronous Call Before Freeze

CEO supplied two `[M016E-FREEZE-PROBE]` Console lines, confirmed (by direct follow-up) to be **consecutive with nothing between them, and the literal last lines logged before the game became unresponsive**:

```
[M016E-FREEZE-PROBE] after client.ReadAsync returned
  at HiveResearchPanelController/<RefreshCoreAsync>d__30:MoveNext() (HiveResearchPresentation.cs:453)
  at UnitySynchronizationContext:ExecuteTasks()

[M016E-FREEZE-PROBE] queue sidebar periodic researchController.Refresh() t=24,33707
  at HiveViewProductUiPresenter:DrawQueueSidebarForExternalHost(bool) (HiveViewProductUiPresenter.cs:4276)
  at HiveMapQueueSidebarBootstrap:OnGUI() (HiveMapQueueSidebarBootstrap.cs:63)
  at UnityEditor.GUIView:ProcessEvent
```

This is a critical, direct finding: **`client.ReadAsync` — the actual authenticated HTTP call — completed successfully, with no exception.** The network/session/transport layer is therefore *not* the thing that hangs; execution returned from it cleanly.

The second line is the real anomaly. `HiveMapQueueSidebarBootstrap.OnGUI()` only reaches `DrawQueueSidebarForExternalHost` (and its periodic `researchController.Refresh()`) when its own `anyOverlayOpen` guard is **false**, which explicitly includes `HiveViewProductUiPresenter.ResearchOverlayOpenForExternalHost`. For this log to fire immediately after a Research refresh completed, one of two things is true:

1. This "after client.ReadAsync returned" line was a **collapsed repeat** of an earlier (non-unique-text) log entry from the *periodic* queue-sidebar refresh cycle (which also calls the same code path), not from the original click — Unity's Console collapses identical repeated messages into one line, and the original instrumentation used static text with no per-call identifier, so this could not be ruled out from the pasted evidence alone.
2. `ResearchOverlayOpenForExternalHost` was genuinely `false` at that moment even though a research refresh had just been triggered — meaning either the click never actually opened the overlay, or something closed it again almost immediately.

Both are live hypotheses. Neither could be confirmed or ruled out from the two-line fragment provided, which is why the instrumentation below was added before finalizing this report.

## Authenticated Research Lifecycle

Traced fully:

`MobileAccountSessionRuntimeBootstrap.OnSceneLoaded` → `TryConfigureGameplayForActiveSession()` → (unconditionally) `CloseGameplayForSignedOutSession()` first, then rebuilds every controller including `researchController`, then `HiveViewProductUiPresenter.ConfigureResearchControllerForRuntime(researchController)`.

Confirmed facts:
- **Only one `researchController` instance exists at a time** — it's a single private static field in `MobileAccountSessionRuntimeBootstrap`, reassigned (not appended) on every reconfiguration.
- **`TryConfigureGameplayForActiveSession()` unconditionally tears down and rebuilds every controller on *every* `SceneManager.sceneLoaded` event**, with no guard against firing while a controller has in-flight async work. This is pre-existing behavior (not introduced by this mission), but Research's official path had never been reachable before this mission's earlier fix, so this is the *first* time a live Research refresh could ever race against such a reconfiguration.
- No evidence was found that this reconfiguration actually fires during a normal authenticated session after initial login (no code path was found that calls `SceneManager.LoadScene`/`LoadSceneAsync` during ordinary HiveMap play) — but CEO's own log excerpt mentioned "a chat session `OperationCanceledException` during scene lifecycle," which is only explainable by an actual scene lifecycle event occurring. This is circumstantial, not proven, for the *specific* freeze repro.
- No event/callback exists that would cause a "refresh completing" to recursively trigger another refresh on its own — `HiveResearchPanelController` exposes no `Model`-changed event; nothing subscribes to one.
- `HiveMapResearchBootstrap` (this mission's new bootstrap) is not listed in `HiveMapRuntimeBootstrapInitializer.InitializeAllBootstraps`, unlike every sibling `HiveMap*Bootstrap`. Confirmed this does not prevent the bridge from being wired in the actual repro scenario (HiveMap loads once, login happens in-place via `HiveMapSplashBootstrap`'s in-scene gate, so the bootstrap's own `AutoStart` already covers it) — but it remains a real inconsistency, flagged below.

## Queue `À valider` Root Cause

Unchanged from the prior investigation, reconfirmed: `officialResearch.ActiveOperation.IsAwaitingCompletion` (localization key `research.official.queue.ready`) is a **legitimate business state** — a real server-side research operation has finished its timer and is waiting for the player to complete/claim it, structurally identical to Construction's and Training's own "ready to claim" states. Not a stale/fake label. The queue sidebar row is not itself implicated in the freeze by the code review performed (the periodic refresh it triggers is throttled to once per 5 seconds and guards on `!researchController.IsBusy`), though its appearance immediately after "after client.ReadAsync returned" in the CEO log is the specific anomaly this report's new instrumentation targets.

## Assembly Boundary

No change this pass. `LivingHiveResearchHost.cs` still depends only on `LivingHiveResearchBridge` (`BeeKingdom.Core.Integration`), confirmed by the existing `LivingHiveMenuAssemblyNeverReferencesTheDefaultPlaygroundAssembly` test, which still passes. No new dependency introduced.

## Changes

No behavioral code was changed this pass (per the mission's instruction to trace first and avoid arbitrary fixes without a confirmed root cause). Only additional **diagnostic instrumentation** was added, replacing/extending the previous pass's probes to close the specific gap the CEO log exposed (collapsed/ambiguous log entries, no visibility into unexpected `Close` calls, no visibility into the queue sidebar's exact state when it fires while research is configured):

1. **`HiveResearchPresentation.cs` (`RefreshCoreAsync`)** — every call now gets a unique, monotonically increasing `callId` (`RefreshCoreAsync#N`), so overlapping/rapid calls (click-triggered vs. the periodic queue-sidebar tick) are individually traceable instead of collapsing into one Console line. Extended the bracket to cover the *entire* method, not just the network await: added logs after model construction (`"model built"`) and in the `finally` block (`"method end, busy=false"`), plus logging the actual exception object on any unexpected catch. This closes the exact gap in the previous pass — we now know precisely whether a freeze happens during the network call, during model construction, or after the method has fully returned.
2. **`HiveViewProductUiPresenter.CloseResearchOverlayForExternalHost()`** — now logs every call (Unity Console captures the full managed stack trace automatically), so an unexpected/spurious close (e.g. a stray back-button hit test firing on the same click that opened Research) will be immediately visible with its exact caller.
3. **`HiveMapQueueSidebarBootstrap.OnGUI()`** — now logs, once per transition, the exact moment the sidebar draws (and is about to periodically refresh) while `MobileAccountSessionRuntimeBootstrap.IsResearchControllerAvailableForExternalHost()` is true, including the live values of `ResearchOverlayOpenForExternalHost` and `LivingHiveResearchRuntime.IsModalOpen` at that exact moment — this directly answers whether the overlay-open state is desynced from what the click just set.

All additions are `Debug.Log` only — no retries, no delays, no suppressed exceptions, no behavior change. Verified via `assets-refresh` (0 errors) and a Play Mode smoke test (click → local-preview fallback path, since this test environment has no authenticated session → clean open/close, zero exceptions, zero regressions).

## Files Changed

- `Assets/BeeKingdom/Playground/HiveResearchPresentation.cs` — instrumentation only (see above).
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` — instrumentation only (`CloseResearchOverlayForExternalHost` logging).
- `Assets/BeeKingdom/Playground/HiveMapQueueSidebarBootstrap.cs` — instrumentation only (state-capture logging).

No other files touched this pass. `LivingHiveResearchHost.cs`, `HiveMapResearchBootstrap.cs`, `LivingHiveResearchBridge.cs` unchanged from the prior mission (their earlier probes remain in place and still fire correctly per this pass's smoke test).

## Temporary Diagnostics Used/Removed

**Used, not removed** — the mission requires localizing the exact blocking call, and the evidence gathered so far is suggestive but not conclusive. All `[M016E-FREEZE-PROBE]` logging from this mission and the prior one remains in place, now with the added `RefreshCoreAsync#N` call-id disambiguation and two new capture points (`CloseResearchOverlayForExternalHost` stack trace, queue-sidebar state snapshot). Removing this now would destroy the ability to resolve the CEO's report on the very next repro. Recommend keeping all of it until the root cause is confirmed and fixed, then stripping every `[M016E-FREEZE-PROBE]` line in one pass.

## Tests

`LivingHiveResearchBridgeTests` (from the prior mission) re-run after these changes: 6/6 passed, no regression. No new automated test was added this pass, because the suspected root cause is not yet confirmed — per the mission's own guidance, a regression test is "especially valuable" once the actual cause (recursive callback / duplicated subscription / synchronous wait / controller recreation) is identified; writing one against an unconfirmed hypothesis would test the wrong thing.

## Validation

- **Compilation**: 0 errors after every edit (`assets-refresh`, confirmed via fresh Console read). No `BeeKingdom.LivingHiveMenu` or `Assembly-CSharp` errors.
- **Runtime (unauthenticated smoke test, this environment has no live session)**: clicked Research via the real production click path (`BuildingSelectionService.NotifyClicked`) — correctly fell back to the local-preview window, opened and closed cleanly, zero exceptions, zero new console errors, `EditorApplication.isPlaying` toggled cleanly on both ends.
- Could not reproduce the authenticated freeze itself in this environment — no CEO credentials were used, per every prior instruction in this mission chain.

## CEO Manual Validation Required

Please reproduce once more with this build, and paste back the full `[M016E-FREEZE-PROBE]` tail from the Console (not filtered/searched — the raw scrollback from a few lines before the click up through the last line before the freeze). With the new per-call `RefreshCoreAsync#N` ids and the two new capture points, this should show conclusively either:
- the exact statement that never completes (if it's inside `RefreshCoreAsync` or the transport layer after all), or
- that `CloseResearchOverlayForExternalHost()` fires unexpectedly right after open (with the stack trace revealing the spurious caller), or
- that the queue sidebar is drawing (and refreshing) *while* `ResearchOverlayOpenForExternalHost` is true — a distinct bug (input-gate/state desync) that would explain the symptom without the "hang" being inside Research's own code at all.

Test steps for CEO:

1. Google login.
2. Arrive in HiveMap.
3. Click Research once.
4. Confirm window opens without freeze.
5. Confirm queue Research state is coherent.
6. Close Research.
7. Confirm HiveMap remains responsive.

## Remaining Issues

- Root cause not yet confirmed — this report is a diagnostic-hardening pass, not a fix.
- `HiveMapResearchBootstrap` is still missing from `HiveMapRuntimeBootstrapInitializer.InitializeAllBootstraps`. Reasoned through why it doesn't appear to matter for the current repro (see Authenticated Research Lifecycle above), but left unfixed to avoid bundling an unrelated change into an active freeze investigation, per the mission's own minimal-change instruction.
- `TryConfigureGameplayForActiveSession()`'s unconditional teardown-and-rebuild of every controller on every scene-load event, with no protection for in-flight async operations, remains a real architectural hazard independent of whether it's this specific freeze's cause. Worth its own follow-up once Research's freeze is closed.

---

## ADDENDUM 2 — CEO Full-Chain Log Confirms Refresh Completes Cleanly

CEO reproduced again and supplied a screenshot of the console showing the complete sequence for one click (all at 14:39:58, all within ~0.6ms of each other by the printed `t=` values):

```
before Refresh() controllerType=HiveResearchPanelController IsConfigured=True IsBusy=False t=36,30129
RefreshCoreAsync#4 before client.ReadAsync(hiveId=5b9f2835-5eda-4f02-9fa8-0f99794f7438)
after Refresh() call returned (async-void...) t=36,30179
OpenOfficialOverlay() returned t=36,30192
RefreshCoreAsync#4 after client.ReadAsync returned, building model
RefreshCoreAsync#4 model built
RefreshCoreAsync#4 method end, busy=false
```

**Then nothing else `[M016E-FREEZE-PROBE]` — followed six seconds later by an unrelated Unity MCP plugin warning (`BufferedFileLogStorage Flush called but already disposed`), which is tooling-infrastructure noise (the MCP plugin's own log storage being torn down, not game code) and was not treated as evidence per the mission's own instruction not to over-index on unrelated logs.**

This is now conclusive on one point: **the entire click → open → refresh → network round-trip → model-construction → method-end chain completes successfully, in full, with zero exceptions.** Every step this mission's instrumentation could observe up through the async refresh finishing has been directly proven clean by CEO's own log, not inferred.

### What this rules out

- The click handler, `OpenResearchOverlayForExternalHost`, `Refresh()`'s synchronous entry, the network call itself, `HiveResearchPresentation.Ready`/`Project`'s model construction, and the `finally` block are **all confirmed non-blocking**.

### What this points to next

The freeze must be in whatever runs **after** `RefreshCoreAsync#4 method end`. The previous instrumentation pass had a real blind spot here: `HiveMapResearchBootstrap.OnGUI()`'s draw probe only logged the *first* `OnGUI` call after opening — and since `Model` starts as an empty placeholder and only becomes populated with real, previously-never-exercised server data (offers, an active operation, balances) once this refresh completes, the *specific* draw call most likely to hit a data-dependent bug was never being logged at all. Re-read every method in `DrawOfficialResearchMenuPanel`'s per-card loop (`OfficialResearchActionLabel`, `OfficialResearchStatusText`, `FormatBuildingUpgradeDuration`, `Progress01`/`Remaining`) — all bounded, no loops, no recursion — but none of it had been exercised with a real `ActiveOperation` before, which is exactly the state CEO's account may be in (consistent with the earlier "À valider" report, implying a real awaiting-completion research operation exists).

### Instrumentation strengthened (still no behavior change)

- `HiveMapResearchBootstrap.OnGUI()` now logs **every** draw call while Research is open (not just the first), each with its own sequence number, capped at 40 logs so it cannot spam forever if the panel is healthy and stays open.
- `DrawOfficialResearchMenuPanel`'s per-card loop now logs entry/exit for **each individual research card** (`card[i]=researchId start/end`) during its existing once-per-second probe window, so if the hang is specific to one card/offer/active-operation combination, the log will stop mid-loop at that exact card instead of only bracketing the whole loop.

Verified: 0 compile errors, `LivingHiveResearchBridgeTests` still 6/6, Play Mode smoke test clean. Could not exercise the new per-card/per-draw-call logging against real official data in this environment (forcing `activeHiveMenu` directly still leaves `HasEnteredHiveForExternalHost=false` without a real login, which gates `HiveMapResearchBootstrap.OnGUI()` entirely - confirmed via direct query, not assumed).

## Confidence

MEDIUM

The chain up through the async refresh completing is now proven clean by direct, first-party CEO log evidence rather than static reasoning alone. The remaining unknown is now narrowly scoped to the draw path with real populated data (never exercised before), and the new instrumentation is specifically built to localize it there on the next repro - either to a specific card/researchId, or to prove the draw path is also clean, in which case suspicion would shift to `BuildingSelectionService.Select()`/`BuildingSelectionFeedback` (fires on every click, not yet instrumented) as the next target.

Stop for GPT/CEO validation.
