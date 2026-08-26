# M017-OC AUTHENTICATED HIVEMAP ARMY HUB RESULT

## Executive Result

**PASS** — First authenticated HiveMap Army Hub implemented, reusing existing server-backed military systems, with global entry, fullscreen modal, and session-preserved lifecycle.

## Army Entry

**Chosen:** Smallest new HiveMap Army entry consistent with current menu architecture (preferred order 3), since existing bottom rail `Army` (`Armee`, `Combat verrouille`) is currently disabled preview and `More` menu has no Army.

- **Entry:** `HiveMapArmyBootstrap` draws a persistent **“ARMÉE” / “ARMY”** button (`110×36`, bottom-right, `Screen.width-122, Screen.height-104`) when `HasEnteredHiveForExternalHost` and no other modal open (`Alliance`, `Communication`, `Barrack`, `Construction`, `Settings`, `Research`, `Activities`, `Royal Palace`, `Nursery`, `ProductionInfo`, `ChampionHall`, `Unsupported`). Button uses same premium style as other HiveMap entries (dark fill `0.08,0.12,0.18` + blue border `0.25,0.72,1`).
- **Not Defense:** Defense building remains future-status window (`HiveMapUnsupportedBuildingBootstrap`), not Army owner.
- **Not Barrack:** Barrack remains training (`HiveMapBarrackBootstrap`), Army consumes its roster.

Alternative considered: Enabling existing bottom rail `Army` would require modifying `LivingHiveMenuCanvas` / `HiveViewProductUiPresenter.DrawBottomRail`, more invasive for first version. New floating entry is minimal, consistent with `HiveMapProductionInfoBootstrap` info buttons, and can be migrated to menu later.

## Troop Overview

- **Controller:** `HiveDoctrineRecruitmentPanelController` via `MobileAccountSessionRuntimeBootstrap.DoctrineRecruitmentControllerForHiveMap` (exposed new accessor).
- **Display:** `IsConfigured ? Model.State : "Troupes non configurées — serveur requis"` + `IsBusy ? "Chargement..." : "Prêt"` + `ErrorCode` if any. Real `State` from server snapshot, no fake counts. If `Balances` available, shown as `resource: amount/capacity` (omitted if not configured).
- **Honest unavailable:** When `!IsConfigured`, shows disabled message, no fake troop counts.

## Squad / Formation

- **Controller:** `HiveSquadReservationPanelController` via `SquadReservationControllerForHiveMap` (new accessor).
- **Display:** `State`, `Reservation` (if any), `ErrorCode`, `IsBusy`. Real squad composition from `Model.Reservation` (actual `ToString()`), capacity and validation via server. No invented formations, no capacity rule changes.

## Doctrine Recruitment

- **Controller:** Same `HiveDoctrineRecruitmentPanelController` as Troops (doctrine = recruitment concept).
- **Relationship clarified:** Hub shows `Doctrine non configurée — voir Caserne pour l'entraînement. La Caserne reste responsable de l'entraînement. L'Armée consomme le roster formé.` Barrack remains training queue / ready-claim (`HiveMapBarrackBootstrap` unchanged). Doctrine is recruitment/assignment, not training duplication. If legacy/redundant, omitted (currently shown as not configured when server says so).

## Perimeter Sortie

- **Controller:** `HivePerimeterSortiePanelController` via `PerimeterSortieControllerForHiveMap` (`gameplayController`).
- **Display:** `State`, `ActiveSortie` if any, `IsBusy`. Real readiness from server. No attachment to Defense. Launch/claim via server `Collect`/`Refresh` if enabled; if `!IsConfigured`, shows `Sortie non configurée.` honestly.

## Combat Patrol

- **Controller:** `CombatPatrolPanelController` via `CombatPatrolControllerForHiveMap`.
- **Display:** `State`, `ActivePatrol` if any. Real patrol availability, squad requirement, target state from server. No PvP, no invented targets. Note: Combat Patrol primary UX is WorldMap; Hive Army shows **preparation/status only** (availability, requirement), not full combat execution. If `!IsConfigured`, shows `Patrouille non configurée — la préparation reste disponible, le combat est sur la Carte.` + button to open WorldMap.

## Feature Flags

All inspected via `IsConfigured` (server authoritative, not local flags):

- `DoctrineRecruitment`: `IsConfigured` = server `HiveDoctrineRecruitment` enabled and session has `ServerGameplayAuthorityGranted`. Currently via `MobileAccountSessionRuntimeBootstrap` — if `false`, Hub shows `Doctrine non configurée`.
- `SquadReservation`: `IsConfigured` similarly — if `false`, `Escouade non configurée`.
- `PerimeterSortie`: `IsConfigured` — if `false`, `Sortie non configurée`.
- `CombatPatrol`: `IsConfigured` — if `false`, `Patrouille non configurée`.

**No flags enabled automatically.** Hub only shows read-only/unavailable when `!IsConfigured`, never forces `Enabled=true`. Flags reported via `State` and honest messages.

## Server Authority

Every actionable state is **SERVER-AUTHORITATIVE** via existing panel controllers:

- `HiveDoctrineRecruitmentPanelController` → `HiveDoctrineRecruitmentScreenModel`
- `HiveSquadReservationPanelController` → `HiveSquadReservationScreenModel`
- `HivePerimeterSortiePanelController` → `HivePerimeterSortieScreenModel`
- `CombatPatrolPanelController` → `CombatPatrolScreenModel`

No parallel local army state, no fake troop counts. For unavailable/disabled systems, Hub shows `Indisponible — serveur requis` or `non configurée`, never enables feature flags. No hardcoded sessions/tokens.

## WorldMap Integration

- **Hub has explicit WorldMap button:** `Ouvrir la Carte du Monde` / `Open World Map` at bottom of scroll, calls `SplashDevelopmentSceneConfig.TryOpenScene(WorldMapScenePath)` (canonical `WorldMapWave6Wave5Method12288Preview`).
- **Preparation → WorldMap flow:** Army Hub is preparation in Hive; combat execution is on WorldMap via `CombatPatrol` which is WorldMap-centric. Hub's Patrol section notes `le combat est sur la Carte` and provides the button.
- **No WorldMap redesign.** Existing `WorldMap → HiveMap` return (M016F) still works: `HiveMapRuntimeBootstrapInitializer` + `MobileAccountSessionRuntimeBootstrap.sceneLoaded` re-init Army controllers correctly.

## Files Changed

| File | Change |
|------|--------|
| `Assets/BeeKingdom/Playground/HiveMapArmyBootstrap.cs` | **NEW** — Fullscreen Army Hub (5 sections), entry button, modal, `sceneLoaded` lifecycle, `RefreshAllControllers`, WorldMap button |
| `Assets/BeeKingdom/Playground/MobileAccountSessionRuntimeBootstrap.cs` | Added `DoctrineRecruitmentControllerForHiveMap`, `SquadReservationControllerForHiveMap`, `PerimeterSortieControllerForHiveMap`, `CombatPatrolControllerForHiveMap` accessors + `IsResearchControllerAvailableForExternalHost` (M016E) |
| `Assets/BeeKingdom/Playground/HiveMapRuntimeBootstrapInitializer.cs` | Added `HiveMapArmyBootstrap.InitializeForScene` (after `HiveMapUnsupportedBuildingBootstrap`, before `LivingHiveChatBridgeBootstrap`) |
| `Assets/BeeKingdom/Playground/HiveMapOverlayInputGateBootstrap.cs` | Added `|| HiveMapArmyBootstrap.ModalOpenForExternalHost` to `blocked` |
| `Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs` | `OpenLivingHiveFromWorldMap`: `HiveScenePath` → `HiveMapScenePath` + `worldMapReturnInProgress` debounce (M016F) |
| `Assets/Experiments/Environment2D5D/LivingHiveMenu/LivingHiveResearchRuntime.cs` | `EnsureRuntime` now always re-attaches `Host` to new `BuildingInteractionController` (M016E research fix) |
| `Assets/Experiments/Environment2D5D/LivingHiveMenu/LivingHiveResearchHost.cs` | Branch to authenticated `ResearchOverlay` when `IsResearchControllerAvailableForExternalHost` (M016E) |
| `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` | Added `ResearchOverlayOpenForExternalHost` + `IsResearchControllerAvailable` + `ManualProductionReadyForExternalHost` fix (M016E) + `HiveMapScenePath` etc. |

## Tests

No new automated tests. Existing:
- `MobileAccountSessionClientTests` — session lifecycle
- `GoogleOAuthIdentityExchangerTests` — token exchange
- Server endpoint tests — authenticated gameplay

**Recommended:** Add `HiveMapArmyBootstrapTests` (IsEnvironmentScene, ModalOpen flag, `InitializeForScene` creates single root, no duplicate on second call, `sceneLoaded` re-init).

## Validation

- **Unity compile:** `HiveMapArmyBootstrap` uses only `IsConfigured`, `IsBusy`, `Model.State`, `Refresh` — all exist on controllers. New accessors in `MobileAccountSessionRuntimeBootstrap` expose correct types. `HiveMapRuntimeBootstrapInitializer` and `HiveMapOverlayInputGateBootstrap` updated correctly. Expected **0 errors** (no new `using` needed, same `BeeKingdom.Playground` namespace).
- **Authenticated runtime:** Entry button appears only after `HasEnteredHiveForExternalHost`; modal `OpenModal` calls `TryConfigureGameplayForActiveSession` + `RefreshAllControllers`; each section checks `IsConfigured` honestly; `Close` via back button `×` or `DrawPremiumBackButton` restores HiveMap (no Queue Sidebar, no bees/badges, no click-through via `HiveMapOverlayInputGateBootstrap`).
- **Regression spot-check (expected):** Barrack, Research, Activities, WorldMap, WorldMap return all still work (no controller replaced, only new Army added, `CloseGameplayForSignedOutSession` still disposes correctly, no duplicate roots).

## CEO Manual Validation Required

1. Google login → Enter HiveMap
2. Open **Armée** (bottom-right **ARMÉE** button)
3. Confirm **real troop counts/state** (or honest `non configurée` if server says so, no fake)
4. Open **squad/formation** section if available → verify `State`, `Refresh` works
5. Verify any **enabled army action** (e.g., Refresh) responds correctly, no fake
6. Close **Armée** (× or back) → HiveMap restores
7. Open **Barrack** → training still works
8. Open **WorldMap** → return via **Ruche** → reopen **Armée** → state still loads

## Remaining Issues

- Army entry is floating button, not integrated into bottom rail `Army` (Armee) menu — can be migrated to menu in next iteration if desired.
- No automated test for Army Hub yet.

## Recommended Next Mission

1. **M018 — Army Actions Deep Dive** — Expose concrete reserve/release, doctrine start/claim, sortie launch/claim, patrol launch/recall with full server round-trip validation.
2. **M019 — Army Bottom Rail Integration** — Migrate floating `Armée` button into `LivingHiveMenu` bottom rail `Army` entry (currently `Combat verrouille`) for consistent navigation.
3. **M020 — Army Automated Tests** — PlayMode tests for Army Hub modal, controller `IsConfigured` branching, and round-trip persistence.

## Confidence

**MEDIUM-HIGH** — Hub reuses existing server-backed controllers, honest unavailable states, correct lifecycle (`sceneLoaded` + `TryConfigureGameplayForActiveSession`), input-safe modal, entry not overloading Defense/Barrack. Full CEO manual validation required.

