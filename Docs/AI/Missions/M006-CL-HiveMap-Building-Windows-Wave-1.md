# M006-CL HIVEMAP BUILDING WINDOWS WAVE 1 RESULT

## Honey Reserve

**Previous LivingHive behavior:** manual-production tap-to-collect (bee-swirl feedback), plus (via the generic per-hotspot detail panel, reachable from the flat reference-image hotspot system) a read-only forecast of pending amount, hourly rate, and capacity — the same data source described in `Docs/Demos/LivingHive.md` under "Mobile production forecast", backed by `HiveOfflineProductionPanelController`.

**Previous HiveMap behavior:** identical tap-to-collect via `HiveMapProductionBootstrap` (unchanged, not touched this wave). No way to see pending/rate/capacity without leaving HiveMap.

**Missing capability:** the read-only production forecast.

**Existing controller reused:** yes — `HiveOfflineProductionPanelController` (already instantiated and refreshed by `MobileAccountSessionRuntimeBootstrap`, server-authoritative, same one LivingHive's forecast already reads).

**Implementation:** new `HiveMapProductionInfoBootstrap.cs`. Draws a small "i" button anchored to the building's own screen rect (offset to the top-right corner, using the same collider-bounds screen projection every other HiveMap bootstrap already uses) for Honey Reserve and Warehouse. Tapping it opens a small read-only IMGUI panel showing resource key, pending amount, hourly rate, and reserve/capacity, sourced from `HiveOfflineProductionPanelController.Model.FindLine(hotspotId)`. Never calls `.Collect()` — the existing tap-to-collect flow in `HiveMapProductionBootstrap` is completely untouched and remains the only way to actually collect.

**Resulting HiveMap behavior:** tapping the building itself still just collects (unchanged). Tapping the new "i" button opens the forecast panel. Building interaction, badge behavior, zoom-derived badge sizing, resource collection, and HUD behavior are all unmodified — verified by reading `HiveMapProductionBootstrap.cs` unchanged and re-testing the collection click path in Play Mode.

## Warehouse

**Previous LivingHive behavior:** same manual-production collect flow as Honey Reserve/Transformation, plus the same read-only forecast panel (pending/rate/capacity) for its resource.

**Previous HiveMap behavior:** same tap-to-collect only, no forecast.

**Missing capability:** same read-only forecast, for Warehouse's own line.

**Existing controller reused:** yes, same `HiveOfflineProductionPanelController`, filtered by Warehouse's own `BuildingLegacyKeys.WarehouseCells` hotspot id.

**Implementation:** same `HiveMapProductionInfoBootstrap.cs` — Warehouse is the second entry in its tracked-building list, sharing the same "i" button + panel code as Honey Reserve.

**Resulting HiveMap behavior:** identical pattern to Honey Reserve above. Transformation was intentionally left out (out of scope for this wave per the mission's building list).

## Nursery

**Previous LivingHive behavior:** official server-authoritative brood care (`Docs/Demos/LivingHive.md`, "Official brood care"): `Feed` (300 honey, 12s, +22 nutrition) and `Stabilize` (45 wax, 13s, +7 stability), reached through the generic per-hotspot detail panel in the flat reference-image hotspot system. Backed by `HiveBroodVitalityPanelController`, itself backed by `HiveBroodVitalityClient` with a protected mutation outbox (idempotent Start/Complete/Retry).

**Previous HiveMap behavior:** Nursery had no dedicated window. Clicking it fell through to `HiveMapBuildingUpgradeClickBootstrap`'s generic "no dedicated window" redirect, opening the Construction picker pre-selected to Nursery — i.e. only an upgrade path, no way to Feed/Stabilize at all.

**Missing capability:** Feed/Stabilize (the actual Nursery gameplay).

**Existing controller reused:** yes — `HiveBroodVitalityPanelController` (already instantiated and refreshed by `MobileAccountSessionRuntimeBootstrap`), exposing `Model` (nutrition/stability/tier/active operation/pending state), `Start(type)`, `Complete()`, and `Retry()`. No new gameplay logic was written; this wave only adds a HiveMap-native window around this controller.

**Implementation:** new `HiveMapNurseryBootstrap.cs`. Subscribes to `BuildingInteractionController.Selection.BuildingClicked`, opens a small new IMGUI window (self-contained — never calls into `HiveViewProductUiPresenter`'s `Draw*ForExternalHost` bridge) on Nursery click. Shows nutrition/stability bars, the active operation with countdown and a "Terminer" (Complete) button when ready, a pending-confirmation "Verifier la commande" (Retry) path, Feed/Stabilize buttons gated by `Model.CanStart(...)`, and an "Ameliorer" button that opens the same Construction picker Nursery used to open by default (`HiveViewProductUiPresenter.OpenConstructionOverlayForExternalHost`), so the previously-reachable upgrade path is preserved, just one tap deeper.

`BuildingTypes.Nursery` was removed from `HiveMapBuildingUpgradeClickBootstrap.ExcludedBuildingTypes`'s complement — i.e. added to the exclusion list, since it now has its own window and should no longer fall through to the generic redirect. Defense, Genetics, Infirmary, Academy, Bank, Royal Palace, Champion Hall remain untouched and still redirect to Construction as before (out of scope for this wave, confirmed by direct regression test on Bank).

**Resulting HiveMap behavior:** clicking Nursery opens the new Feed/Stabilize window instead of the Construction picker. No LivingHive fixed-image hotspot logic was introduced — all screen positioning reuses the same collider-bounds/EventSystem-based interaction HiveMap already uses everywhere else.

## Files Changed

New:
- `Assets/BeeKingdom/Playground/HiveMapNurseryBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapProductionInfoBootstrap.cs`

Modified:
- `Assets/BeeKingdom/Playground/MobileAccountSessionRuntimeBootstrap.cs` — added two public read-only static accessors (`OfflineProductionControllerForHiveMap`, `BroodVitalityControllerForHiveMap`) exposing the already-instantiated server controllers directly, with `Unavailable*` fallbacks so callers never see null. No behavior change to existing session logic.
- `Assets/BeeKingdom/Playground/HiveMapBuildingUpgradeClickBootstrap.cs` — added `BuildingTypes.Nursery` to `ExcludedBuildingTypes`; updated the file's top comment accordingly.
- `Assets/BeeKingdom/Playground/HiveMapOverlayInputGateBootstrap.cs` — added `HiveMapNurseryBootstrap.OverlayOpenForExternalHost` and `HiveMapProductionInfoBootstrap.OverlayOpenForExternalHost` to the existing `blocked` condition, so the two new windows get the same click-through protection as every other HiveMap overlay.

Not touched: `LivingHiveMenuCanvas.cs`, `LivingHiveResearchWindow.cs`, `HiveMapBarrackBootstrap.cs`, `HiveMapConstructionBootstrap.cs`, `HiveMapProductionBootstrap.cs`, any Construction/Upgrade/Barrack/badge-sizing code, `HiveViewProductUiPresenter.cs`.

## Legacy Dependencies Added or Retained

Explicit `HiveViewProductUiPresenter` references, all pre-existing patterns reused, none new in kind:

- `HiveMapNurseryBootstrap.cs`: `HiveViewProductUiPresenter.HasEnteredHiveForExternalHost` (read-only session gate, same as every other HiveMap bootstrap) and `HiveViewProductUiPresenter.OpenConstructionOverlayForExternalHost(hotspotId)` (one call, only for the "Ameliorer" button — reuses the exact same entry point `HiveMapBuildingUpgradeClickBootstrap` already used for Nursery before this wave).
- `HiveMapProductionInfoBootstrap.cs`: `HiveViewProductUiPresenter.HasEnteredHiveForExternalHost` only.
- No new `Draw*ForExternalHost` calls were added — both new windows render their own fresh IMGUI, not the monolith's.
- No new dependency was added on `HiveViewProductUiPresenter`'s internal state beyond what was already required to gate on session entry.

Per `Docs/AI/Missions/M005-CX-HiveMap-Decoupling-Strategy.md`'s classification: the `HasEnteredHiveForExternalHost` gate is the same "safe temporary dependency" pattern already accepted project-wide; the single `OpenConstructionOverlayForExternalHost` call is a reuse of an already-existing entry point, not a new one. Both new windows read gameplay state through `MobileAccountSessionRuntimeBootstrap`'s two new accessors instead of through the monolith — a small step in the direction M005 recommended ("Preparer l'acces direct aux controleurs serveur deja crees par MobileAccountSessionRuntimeBootstrap").

## Validation

- Unity compiles with 0 errors after both new files and all edits (`assets-refresh`, confirmed via fresh console read — only pre-existing unrelated warnings remain).
- Play Mode entered and exited cleanly twice, no exceptions during either transition.
- Honey Reserve / Warehouse: `HiveMapProductionInfoBootstrap` instantiates at runtime (`HiveMap Production Info Runtime` confirmed present); its info-panel rendering path was exercised directly (reached the first `GUI.Box` call with no logic errors before Unity's own "must be called from OnGUI" guard, which is expected for a call made outside a real IMGUI event and not a defect).
- Nursery: simulated the real click path by invoking `HiveMapNurseryBootstrap`'s private `OnBuildingClicked` with the real `BuildingDefinition` for `nursery_cluster` — window opened (`OverlayOpenForExternalHost=True`), then rendered for at least one real Unity `OnGUI` frame with zero console errors or exceptions, in the `NotConfigured` state (no active session in this bare test scene, so the "Session officielle requise." fallback path was what actually got exercised end-to-end).
- Input gate: confirmed `BuildingInteractionController.IsEnabled` is `False` while the Nursery window is open (click-through prevention verified, not assumed).
- Regressions checked live in Play Mode (see below) via the same production `BuildingSelectionService.NotifyClicked` entry point real clicks use.
- No dedicated Unity EditMode test suite exists for `Assets/BeeKingdom/Buildings/Interaction` or the `HiveMap*Bootstrap` family (confirmed by search) — there was nothing directly relevant to run. A full-project EditMode run was attempted but exceeded the tool's 300s response window without finishing (the project's EditMode suite is very large and unrelated to this area); it was not force-completed given the time cost, since no test in it targets the changed files. This is the one validation step not fully closed out — see Remaining Issues.
- Server-side: no server contracts were touched this wave; the `Docs/AI/Missions/M001-CL-*` baseline's known-uncertainty test (`CombatSquadReservationTests`) is unrelated to this wave and was not rerun.

## Regressions Checked

- Barrack: `HiveMapBarrackBootstrap`'s own `OnBuildingClicked` invoked with the real `guard_post` `BuildingDefinition` → `BarrackOverlayOpenForExternalHost` became `True`. Unaffected.
- Construction/Upgrade for an untouched building (Bank, `hive_bank`): real click via `BuildingSelectionService.NotifyClicked` → `ConstructionOverlayOpenForExternalHost` became `True`, exactly as before. Confirms `HiveMapBuildingUpgradeClickBootstrap`'s redirect still works for every building except the newly-excluded Nursery.
- Alliance: real click on `alliance_future_hall` → `AllianceOverlayOpenForExternalHost` became `True`. Unaffected.
- Research: `LivingHiveResearchWindow`/`LivingHiveResearchRuntime`'s runtime object (`LivingHive Research Runtime`) confirmed still present in the scene at runtime; no file under `Assets/Experiments/Environment2D5D/LivingHiveMenu/` was touched.
- Building selection/highlight: `BeeKingdom BuildingInteraction Runtime` and its components unaffected; no changes to `BuildingInteractionController`, `BuildingSelectionService`, or `BuildingSelectionHighlight`.
- Badge sizing: `HiveMapProductionBootstrap.cs` (the file owning the `BadgeWorldSize` constant and zoom-derived sizing formula) was not modified.

## Remaining Issues

- The full-project Unity EditMode test suite was not run to completion (see Validation) — no test in it targets the files changed this wave, but this was not exhaustively proven, only inferred from the absence of a dedicated test folder for this area.
- The Honey Reserve/Warehouse info panel and the Nursery window were both validated in the `NotConfigured` (no active session) state, since this bare test scene has no login flow wired. Their `Ready` state (real data actually displayed) was not visually confirmed — only proven not to throw, via code-path tracing up to the point of the model query. A future validation pass with an authenticated session would close this gap.
- No visual/screenshot confirmation was possible — `screenshot-game-view` is not exposed as an invokable MCP tool in this session (same limitation encountered in the earlier M001-CL runtime baseline mission). All validation was done through direct runtime state inspection via `script-execute`, not visual inspection.

## Recommended Next Wave

Do not begin — for GPT to decide. Candidates surfaced by this wave's own findings:

1. Extend the same read-only forecast pattern (`HiveOfflineProductionPanelController`) to Transformation, closing out the three manual-production buildings uniformly.
2. Port the generic per-hotspot detail panel pattern (`DrawDetailPanel`/`DrawOfficial*Detail`) for Bank/Academy/Genetics/Infirmary/Administration/Champion Hall, following the same "new HiveMap-native window around an existing controller" approach used here for Nursery.
3. Close the `NotConfigured`-only validation gap noted above with an authenticated test session, to visually confirm the `Ready` state of both new windows.

Stop and wait for GPT orchestration.
