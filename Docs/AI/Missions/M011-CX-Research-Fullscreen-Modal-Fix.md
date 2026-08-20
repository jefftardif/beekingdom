# M011-CX RESEARCH FULLSCREEN MODAL FIX RESULT

## Root Cause

Queue Sidebar leak:

`HiveMapQueueSidebarBootstrap` drew the left queue rail through `HiveViewProductUiPresenter.DrawQueueSidebarForExternalHost` whenever only the older monolith overlays were closed. The newer uGUI Research fullscreen window was not part of that condition, so the sidebar kept drawing over Research.

World/bee/badge leak:

Several HiveMap-specific IMGUI bootstraps render presentation independently of Research: manual production bees/collection feedback, production info buttons/panels, and Barrack training badges/feedback. Because IMGUI draws outside the uGUI raycast/Canvas layer contract, those visuals could appear above the Research fullscreen window even though the Research Canvas itself covered the screen.

Close/back inconsistency:

`LivingHiveResearchWindow` used simple text buttons (`<` and `X`) instead of the existing premium blue back-arrow visual used by the modern LivingHive/HiveMap windows via `closing_arrow.png` / `DrawPremiumBackButton`.

Input behavior:

`BuildingInteractionController` already respects uGUI raycasts, but Research was not represented in `HiveMapOverlayInputGateBootstrap`. That meant HiveMap-specific IMGUI/world bootstraps and the bottom/header canvas were not centrally suppressed while Research was open. The Research host also hid the HUD on open, but its internal `CloseRequested` event did not restore that HUD path directly.

## Changes

- Added `LivingHiveResearchRuntime.IsModalOpen` as the narrow modal state for Research.
- Made `LivingHiveResearchHost` listen to `LivingHiveResearchWindow.CloseRequested` so HUD restoration happens when Research closes itself through its own control or Escape path.
- Added Research modal state to `HiveMapOverlayInputGateBootstrap`.
- Suppressed queue sidebar drawing while Research is open.
- Suppressed manual production bees, collection feedback, and production collection routing while Research is open.
- Suppressed production info buttons/panel drawing while Research is open.
- Suppressed Barrack badge/feedback drawing and Barrack click routing while Research is open.
- Replaced the Research `<`/`X` text controls with the standard `closing_arrow.png` back control in the top-left position.
- Final CEO validation fix: shifted the Research banner title/subtitle start positions right of the blue back arrow, without changing the arrow or Research content.

## Files Changed

- `Assets/Experiments/Environment2D5D/LivingHiveMenu/LivingHiveResearchRuntime.cs`
- `Assets/Experiments/Environment2D5D/LivingHiveMenu/LivingHiveResearchHost.cs`
- `Assets/Experiments/Environment2D5D/LivingHiveMenu/LivingHiveResearchWindow.cs`
- `Assets/Experiments/Environment2D5D/LivingHiveMenu/LivingHiveResearchSpec.cs`
- `Assets/BeeKingdom/Playground/HiveMapOverlayInputGateBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapQueueSidebarBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapProductionBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapProductionInfoBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapBarrackBootstrap.cs`
- `Docs/AI/Missions/M011-CX-Research-Fullscreen-Modal-Fix.md`

Concurrent untouched files observed during the mission:

- `ProjectSettings/EditorBuildSettings.asset`
- `Docs/AI/Missions/M010-OC-LivingHive-Scene-Retirement-Dependency-Map.md`
- `Docs/AI/Missions/M012-OC-HiveMap-Build-Configuration-Alignment.md`

## Modal State Implementation

`LivingHiveResearchRuntime.IsModalOpen` returns `Window != null && Window.IsOpen`. This is intentionally narrow: it does not introduce a global overlay registry and only represents the already-existing Research fullscreen state.

HiveMap bootstraps read that state to suppress presentation or input-adjacent UI while Research owns the screen. Research gameplay, card state, costs, timers, dependencies, launch behavior, and persistence remain owned by `LivingHiveResearchWindow` / `LivingHiveResearchState`.

## Queue Sidebar Behavior

`HiveMapQueueSidebarBootstrap.OnGUI` now treats Research as an overlay condition and returns before drawing the queue sidebar. No queue state is destroyed or altered; the sidebar simply does not render while Research is open and resumes its normal draw path when Research closes.

## World Presentation Suppression

Suppressed while Research is open:

- manual production bees;
- manual collection feedback;
- production info buttons;
- production info panel drawing;
- Barrack ready/progress/claim feedback badges;
- queue sidebar.

Gameplay timers still tick. In particular, `HiveMapProductionBootstrap.Update` continues to call `TickManualProductionForExternalHost`, and Barrack training ticking remains in `Update`.

## Input Gating

`HiveMapOverlayInputGateBootstrap` now includes `LivingHiveResearchRuntime.IsModalOpen` in its blocked state. While Research is open:

- `BuildingInteractionController.IsEnabled` is set false;
- `LivingHiveMenuCanvas.SetInputBlocked(true)` disables rail/header raycasts;
- production collection routing also guards against Research modal state;
- Barrack click routing also guards against Research modal state.

Research's own uGUI interaction remains available because its Canvas/GraphicRaycaster is not disabled.

## Blue Back/Close Standardization

Research now uses the same existing navigation asset as the modern LivingHive/HiveMap IMGUI windows:

- `Assets/Art/UI/Navigation/closing_arrow.png`
- runtime fallback path: `Resources.Load<Texture2D>("UI/Navigation/closing_arrow")`

The control is placed at the standard top-left convention (`4,2,48,46`) and closes Research via the existing `RequestClose("back")` path. The previous text `X` close button was removed to avoid mixed visual languages.

Final manual-validation adjustment: `LivingHiveResearchWindow` now positions the banner title at `x = 68` and subtitle at `x = 70` instead of `x = 28/30`, including the screen-resize relayout path. This leaves clear spacing after the `4,2,48,46` blue back arrow while preserving the arrow itself and the rest of the Research layout.

## Validation

- Initial `git status --short` showed only an unrelated untracked M010 report.
- During work, OC/M012 files appeared and were left untouched.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal /clp:ErrorsOnly` initially failed because the generated `.csproj` is stale and does not include already-committed M009 bootstraps.
- Re-ran the build with temporary generated-project parity by adding the missing M009 bootstrap compile includes, then removed those temporary `.csproj` edits.
- Final C# validation result under generated-project parity: 0 errors, 210 warnings.
- After the CEO overlap fix, re-ran `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal /clp:ErrorsOnly` with the same temporary generated-project parity. Result: 0 errors, 210 warnings; temporary `.csproj` edits removed again.
- Unity Play Mode/runtime visual validation was not executed from this task because Unity processes were already present and no interactive Play Mode automation was run.

## Regression Checks

Compilation covered the modified code paths under generated-project parity. No gameplay logic was changed for:

- Research cards;
- Research costs;
- Research durations;
- Research dependencies;
- Research launch/completion state;
- queue state;
- resource totals;
- production ticking;
- Barrack training ticking.

Routing for other M006/M008/M009 windows was not manually smoke-tested in Play Mode during this task and should be checked by CEO validation.

## CEO Manual Validation Required

Check in `Environment2D5D_HiveMap_Test` Play Mode:

- Open Research: queue sidebar is hidden.
- Open Research: ambient/manual production bees and production/info badges are not visible over Research.
- Open Research: Barrack ready/training badges are not visible over Research.
- Open Research: bottom navigation/header cannot be clicked through the modal.
- Open Research: buildings and production collection do not trigger behind it.
- Research cards, filters, scrolling, launch behavior, and progress remain usable.
- Top-left blue back arrow is visible and closes Research.
- `RECHERCHE` and its subtitle start to the right of the back arrow with clean spacing and no overlap.
- After closing Research, queue sidebar, production badges, bottom navigation/header, and building clicks return normally.
- Spot-check Barrack, Nursery, Champion Hall, Academy, Defense, one production building, and one generic upgrade-only building.

## Legacy Dependencies Added or Retained

Retained:

- Existing `LivingHiveResearchWindow` and `LivingHiveResearchState`.
- Existing `HiveViewProductUiPresenter` external-host calls already used by HiveMap production, queue, and Barrack bootstraps.
- Existing navigation asset `closing_arrow.png`.

Added:

- No new monolith Research UI dependency.
- No old `HiveViewProductUiPresenter` Research port.
- No LivingHive fixed-coordinate hotspot logic.
- No global overlay registry.

## Remaining Issues

- The generated `Assembly-CSharp.csproj` remains stale relative to already-committed M009 files; Unity should regenerate it or M012 should own build configuration alignment.
- Manual visual validation is still required because compile success cannot prove layer ordering, perceived fullscreen ownership, or click-through behavior.
- `LivingHiveResearchSpec.CloseButtonRect` remains as an unused historical geometry helper after standardizing on the top-left back arrow; it was left in place to avoid unrelated cleanup.

## Confidence

MEDIUM-HIGH

The code-level root cause and modal boundary are clear, and compilation succeeds under generated-project parity. Confidence is not HIGH until CEO Play Mode validation confirms no remaining visual layer leaks.
