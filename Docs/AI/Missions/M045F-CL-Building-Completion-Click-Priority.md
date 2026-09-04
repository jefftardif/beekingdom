# M045F-CL — Building Completion Click Priority

Real runtime bug: clicking the completion-ready ready-icon/building opened
the normal building interface instead of validating the finished upgrade.

## 1. Root cause — proven

`BuildingInteractionController.Selection.BuildingClicked` is a plain
multicast `event Action<BuildingDefinition>` with no consumption
mechanism. My M045E-CL fix subscribed a NEW handler to this same event
(`HiveMapBuildingUpgradeVisualStateBootstrap.OnBuildingClicked`) to
complete the ready upgrade - but the building's own dedicated window
opener (e.g. `HiveMapBarrackBootstrap.OnBuildingClicked`) is ALSO
independently subscribed to the identical event. Both are invoked on the
same click, in an order this controller has no control over (each
bootstrap subscribes lazily, the first frame it finds the controller) -
so the same click both validated the upgrade AND opened the building
window. This is the exact, proven failing condition.

## 2. Fix — a single, deterministic preemption point

Rather than trying to order multiple independent event subscribers
against each other (fragile, order not controllable), the click-priority
decision now happens **before** `BuildingClicked` is ever raised:

`BuildingInteractionController` (`Assets/BeeKingdom/Buildings/Interaction/
BuildingInteractionController.cs`) gained a single static hook:

```
public static Func<BuildingDefinition, bool> InteractionPreemptionHook;
```

`HandlePointer()` now resolves which building was hit, then calls a new
extracted method, `DispatchClick(definition)`:

```
public void DispatchClick(BuildingDefinition definition)
{
    if (definition == null) return;
    if (InteractionPreemptionHook != null && InteractionPreemptionHook(definition)) return;

    _selection.NotifyClicked(definition);
    _selection.Select(definition);
}
```

If the hook returns `true`, `NotifyClicked`/`Select` never run at all -
no `BuildingClicked` subscriber (Barrack, Production, generic
Construction-click, ...) ever sees that click. Exactly one action per
click, by construction, regardless of subscription order. A plain
delegate (not an event) so there is only ever one owner deciding
preemption - `BuildingInteractionController` stays in the
`BeeKingdom.Buildings` assembly and never needs to reference
`HiveViewProductUiPresenter`/anything in the default `Assembly-CSharp`
assembly (the same cross-assembly boundary this project has respected
everywhere else).

`HiveMapBuildingUpgradeVisualStateBootstrap.cs` installs this hook (once,
the first frame it resolves the controller) instead of subscribing to
`BuildingClicked`:

```
private static bool TryCompleteReadyUpgradeOnClick(BuildingDefinition building)
{
    if (building == null) return false;
    string hotspotId = BuildingMappingTable.GetByBuildingType(building.BuildingType).LegacyKey;
    string readyHotspotId = HiveViewProductUiPresenter.ReadyToCompleteOfficialUpgradeHotspotIdForExternalHost();
    if (string.IsNullOrEmpty(readyHotspotId) || !string.Equals(hotspotId, readyHotspotId, StringComparison.Ordinal)) return false;
    HiveViewProductUiPresenter.TryCompleteReadyBuildingUpgradeOnTapForExternalHost(hotspotId);
    return true;
}
```

- Returns `false` (does not consume) for any building whose own upgrade is
  not `AwaitingCompletion` - normal opening is completely unaffected.
- Returns `true` (consumes) for the one building whose own operation is
  ready, **even if** the real completion call inside fails - matching the
  required failure semantics: `TryCompleteReadyBuildingUpgradeOnTapForExternalHost`
  →`TryCompleteReadyBuildingUpgradeOnTap`→`RunOfficialBuildingUpgradeAction`
  →`buildingUpgradeController.Complete()` is the same real server call the
  Construction screen's own "Terminer" button uses (unchanged this
  mission), which already surfaces/logs errors and leaves the operation in
  `AwaitingCompletion` on failure - the click is still consumed, so the
  building window never opens to mask a failed validation, and the next
  click retries the same real path.
- Server truth (`ReadyToCompleteOfficialUpgradeHotspotIdForExternalHost`)
  is re-read on every click - no stale/cached "ready" flag, so a
  no-longer-ready building (already validated, or a different building
  entirely) naturally stops being preempted the moment the server state
  changes.

One authoritative completion function (`RunOfficialBuildingUpgradeAction`)
- the ready-icon and "click anywhere on the building" both funnel through
the exact same `DispatchClick`→hook→`TryCompleteReadyBuildingUpgradeOnTapForExternalHost`
path; there was never a second implementation to reconcile.

## 3. Scope of interception - only the ready building's own click

The hook compares the clicked building's own legacy key against
`ReadyToCompleteOfficialUpgradeHotspotIdForExternalHost()`, which itself
only ever returns a value when `HiveBuildingUpgradeScreenModel.ActiveOperation.IsAwaitingCompletion`
is true for that one operation - `BuildingUpgradeService` only allows one
active construction hive-wide, so this is inherently scoped to a single
building today, and the comparison is per-clicked-building (not a global
"any ready upgrade blocks every click" flag) - a future multi-operation
server contract would only need `ReadyToCompleteOfficialUpgradeHotspotIdForExternalHost`
extended to a collection, not a rewrite of this dispatch mechanism.

## 4. Concurrent M046/M046B-CX work - checked, respected

`git status` before editing showed CX actively iterating on
`HiveMapBuildingUpgradeVisualStateBootstrap.cs` and
`BuildingSelectionHighlight.cs` concurrently (their own new
`Docs/AI/Missions/M046B-CX-Upgrade-Outline-Visibility-Tuning.md` appeared
mid-mission). One transient compile error was observed
(`SandboxLivingHiveBuildingUpgradeTests.cs` referencing a since-renamed
CX test method) and resolved itself within seconds once their edit
settled - not caused by, or fixed by, this mission.

This mission's edits to the shared bootstrap file only touch the
click/`OnDestroy`/hook-install portions (the `Update()` pulse math,
`ApplyUpgradePulse`, `BuildingSelectionHighlight.SetVisualState`/
`OutlineWidthTexels` CX added are untouched by this mission). No
reset/stash/checkout/clean was performed. A separate, still-failing,
**pre-existing** CX test (`MonotonicProjectionNeverAuthorizesCompletion`,
a `NUnit Is.Zero` vs `TimeSpan` assertion-shape issue, unrelated to click
routing) was left as-is - not this mission's file to fix, and unrelated
to the bug being investigated.

## 5. Tests

New file: `Assets/BeeKingdom/Tests/Editor/Interaction/BuildingInteractionControllerClickPriorityTests.cs`
(6 tests, all against the real `BuildingInteractionController`/`DispatchClick`,
not a mock):

1. `NoHookInstalled_ClickDispatchesNormally` - baseline, unaffected building opens.
2. `HookReturnsTrue_ClickIsFullyConsumed_BuildingWindowDoesNotOpen` - the
   core bug this mission fixes: a preempted click never fires `BuildingClicked`
   and never selects/opens the building.
3. `HookReturnsFalse_NormalOpeningIsPreserved` - not-ready buildings open exactly as before.
4. `HookOnlyPreemptsItsOwnTargetBuilding_OtherBuildingsStayClickable` - a
   different building's click is unaffected by another building's ready state.
5. `RepeatedClicksOnReadyBuilding_EachConsumedExactlyOnce_HookInvokedOncePerClick` -
   double-click safety: one hook call per click, never a duplicate/batched action.
6. `AfterCompletionHookStopsMatching_NextClickOnSameBuildingOpensNormally` -
   no persistent interception once the server no longer reports the
   building as ready.

Icon-vs-anywhere-on-building parity (mission test #3) is not a separate
test: both are the same `Physics.Raycast` hit on the same building
collider feeding the same `DispatchClick` call - there was never a second
code path to diverge.

Results: **6/6 new tests green**. Regression: `BuildingSelectionServiceTests`
11/11 green, `HiveBuildingUpgradeClientTests` 15/15 green.
`SandboxLivingHiveBuildingUpgradeTests` 9/10 green (the one pre-existing
CX failure noted in section 4, unrelated). Unity compile: 0 errors.

## 6. M046/M046B compatibility

Not modified: `ApplyUpgradePulse`, `UpgradeOutlinePulseAlpha/Width`,
`BuildingSelectionHighlight.SetVisualState`/`OutlineWidthTexels`. The
pulse's own lifecycle (`ActiveOfficialUpgradeHotspotIdForExternalHost`
returning null once `AwaitingCompletion`, stopping the pulse) is untouched
and independent of this mission's click-routing fix - both read the same
underlying server state but through separate accessors
(`ActiveOfficialUpgradeHotspotIdForExternalHost` for "running" vs.
`ReadyToCompleteOfficialUpgradeHotspotIdForExternalHost` for "awaiting
completion", both added across M045E/M045F, neither overlapping).

## 7. M044 occlusion

Unaffected - `DrawBuildingUpgradeReadyBadgeForExternalHost`'s own
`HiveMapOverlayInputGateBootstrap.IsAnyOverlayBlocking()` gate (added in
M045E) is unchanged. The click-preemption hook itself needs no separate
occlusion check: `HandlePointer()` already returns at its very first line
whenever `controller.IsEnabled` is false, which
`HiveMapOverlayInputGateBootstrap` already sets during any blocking
overlay - the hook is simply never reached in that state.

## 8. Deployment

Unity-side only. No server code read or touched.

---

## Final checklist

| # | Question | Answer |
|---|---|---|
| A | Actual click pipeline identified? | YES — `BuildingInteractionController.HandlePointer` → `DispatchClick` → `Selection.BuildingClicked` (multiple independent subscribers) |
| B | Why building window won before fix proven? | YES — two independent `BuildingClicked` subscribers both fired on the same click; the event has no consumption mechanism |
| C | AwaitingCompletion now has click priority? | YES — via a single preemption hook checked before any subscriber runs |
| D | Whole building target validates? | YES — any click hitting the building's collider reaches `DispatchClick`, not just the badge |
| E | Ready icon uses same path? | YES — same `Physics.Raycast` → `DispatchClick`, no second implementation |
| F | Click consumed after validation action? | YES — hook returning true skips `NotifyClicked`/`Select` entirely |
| G | Normal building opening preserved when not ready? | YES — verified by test and code trace |
| H | Failed completion cannot silently fall through? | YES — hook still returns true (consumes) even if the real completion call fails |
| I | Double completion prevented? | YES — server truth re-read per click; consumed clicks never reach `NotifyClicked` |
| J | M046 pulse unaffected? | YES — no pulse code touched |
| K | Unity compile green? | YES |
| L | Relevant tests green? | YES — 6/6 new, 11/11 + 15/15 regression, 1 pre-existing unrelated CX failure noted |
| M | READY FOR CEO BUILDING CLICK RETEST? | YES |

READY FOR CEO — CLICK THE COMPLETION-READY CASERNE ONCE.
