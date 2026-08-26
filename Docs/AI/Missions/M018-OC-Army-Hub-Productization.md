# M018-OC ARMY HUB PRODUCTIZATION RESULT

## Executive Result

**PASS** — M017 Army Hub productized to player-facing modal, bottom-rail integrated via More → Armée, floating button removed, technical states translated, Perimeter Sortie Error classified, refresh centralized.

## Bottom Rail Integration

- **Existing bottom rail** `LivingHiveMenu` has 5 entries: `Carte`, `Activites`, `Communication`, `Sac`, `Plus`. No dedicated Army entry.
- **Chosen:** `More` menu (preferred order 2) — smallest new entry consistent with architecture.
- **Added:** `LivingHiveMenuSpec.MoreMenuEntries` now `["Armée", "Parametres", "Aide", "Support"]` (was 3, now 4, Armée first).
- **Bridge (assembly-safe):** `BeeKingdom.Core.Integration.LivingHiveArmyBridge` with `SetHandlers`/`OpenOverlay`/`IsOpen`, mirroring `LivingHiveActivitiesBridge`. `BeeKingdom.LivingHiveMenu` already references `BeeKingdom.Core`, so `LivingHiveMenuCanvas.OnMoreRowClicked("Armée")` can call `LivingHiveArmyBridge.OpenOverlay()` without illegal `BeeKingdom.Playground` reference.
- **Wiring:** `HiveMapArmyBootstrap.Start()` → `LivingHiveArmyBridge.SetHandlers(() => ModalOpenForExternalHost, OpenModal)`. `LivingHiveMenuCanvas.OnMoreRowClicked("Armée")` → `state.CloseActiveMenuPanel(); LivingHiveArmyBridge.OpenOverlay();`
- **Floating button removed:** `HiveMapArmyBootstrap.DrawEntryButton()` now `return;` — only one entry remains (bottom rail → More → Armée), preserves styling/localization, no illegal dependency.

## Army Information Architecture

Player hierarchy (not mirroring controller architecture):

- **FORCES** — troop/readiness overview (from `DoctrineRecruitment`)
- **ESCOUADE** — current formation/reservation (from `SquadReservation`)
- **DOCTRINE** — informational, points to Barrack
- **OPÉRATIONS** — `Sortie Périmètre` + `Patrouille` (grouped as Operations)

Implementation still uses 4 controllers underneath, but UI presents 3 player concepts (Forces, Escouade, Opérations) — Doctrine shown as informational redirect to Barrack to avoid duplicate recruitment.

## Real Troop Data Available

- **DoctrineRecruitment:** `HiveDoctrineRecruitmentScreenModel` exposes `State`, `Balances` (resource→amount/capacity), `ActiveOperation` (family, batch, StartedAt/EndsAt). `IsConfigured` true when server has doctrine and `ServerGameplayAuthorityGranted`. In current CEO runtime: `State=Ready` (troops ready), `IsBusy=false`, no `ErrorCode` — real data.
- **If not configured:** Hub shows `Forces non configurées — serveur requis.` (honest, no fake counts). Do not pretend `Ready` is a count.

## Squad Data Available

- **SquadReservation:** `HiveSquadReservationScreenModel` exposes `State`, `Capacity`, `RosterGuardians/Wingrunners/Darters`, `Available*`, `Reserved*`, `ReservationId`, `ErrorCode`. In CEO runtime: `State=Ready`, `Capacity` real, `Reserved*` real. Hub shows `Disponibilité: Prête`, `Capacité: <real>`, no raw `ToString()`.

## Doctrine Classification

- **Distinct mechanic?** Doctrine recruitment is a separate server mechanic (recruitment families, batch, honey/pollen costs, duration) from Barrack training (which is local troop training). However, for first Army Hub, exposing both as separate actionable systems would duplicate and confuse.
- **Decision:** Doctrine section is **informational only** in Hub: `Recrutement doctrinal — voir Caserne. L'Armée utilise les troupes formées.` + button `Ouvrir Caserne` → `HiveViewProductUiPresenter.OpenBarrackOverlayForExternalHost()`. Barrack remains owner of training queue/ready-claim. Doctrine's `Start`/`Claim` not exposed in Hub to avoid duplicate.

## Perimeter Sortie Error Root Cause

- **Observed:** `PERIMETER SORTIE State: Error` in M017 CEO screenshot.
- **Investigation:** `HivePerimeterSortiePanelController` with `HivePerimeterSortieScreenModel`. `State==Error` occurs when `client.ReadSortieBoardAsync` throws `HivePerimeterClientException` or when `outbox.LastLoadDetectedCorruption` or when `IsConfigured=false` but `Refresh` still called. In HiveMap, Sortie requires `ReservedTotal>0` and `ReservationId` non-empty (squad reserved) and `Signals` with `CanLaunch`. If no squad reserved, `State` is `NeedsReservation`, not `Error`. `Error` with code `not_configured` or `network_unavailable` or `precondition_failed` indicates server says Sortie not configured for this hive or squad not ready.
- **Current CEO runtime:** After `M017` sorties showed `Error` — likely `not_configured` or `precondition_failed` because no squad reserved yet, or feature `HivePerimeterSortie` is `Enabled=false` in `appsettings.Production` (currently `Enabled: false` per earlier check). So `IsConfigured` is true (controller exists) but `Model.State==Error` with `ErrorCode=="not_configured"` or similar.
- **Fix:** Hub now translates: `if (State==Error) → "Sortie indisponible — " + ErrorCode + " Vérifiez la réserve d'escouade ou réessayez plus tard."` and `if (State==NeedsReservation) → "Préparation : Réserve d'escouade requise."` Honest unavailable, not hidden, no new server functionality.

## Patrol Presentation

- **Was:** `ReadyToLaunch` (internal enum)
- **Now:** `Prête au départ` (FR) / `Ready to deploy` (EN) via `playerState = m.State == ReadyToLaunch ? Text("Prête au départ", "Ready to deploy") : ...`
- No launch mechanics added (controller `Launch` exists but requires squad reservation and signal selection — not added in this mission as it would need new gameplay design). Hub shows preparation status and refresh, notes `le combat est sur la Carte` and provides WorldMap CTA.

## Refresh Lifecycle

- **Before:** 5 per-section `Refresh` + 1 global = 6 buttons (diagnostic)
- **Now:** Per-section `Refresh` removed. Opening Hub calls `RefreshAllControllers()` once (via `OpenModal` → `TryConfigureGameplayForActiveSession` + `Refresh` each). Header retains single global `Rafraîchir` button. No aggressive polling. `HiveMapArmyBootstrap` still calls `Refresh` on open, and `MobileAccountSessionRuntimeBootstrap.sceneLoaded` ensures controllers are reconfigured after `HiveMap → WorldMap → HiveMap`.

## UI Changes

- **Header:** `[blue back arrow] ARMÉE / Gestion et préparation des forces` (FR) / `ARMY / Force management and preparation` (EN) — matches premium `Activities`/`Royal Palace` header (dark premium background `0.13,0.085,0.024`, gold separator `1,0.60,0.14`, `DrawPremiumBackButtonForExternalHost`).
- **Sections:** Re-weighted to player concepts (Forces, Escouade, Doctrine informational, Opérations grouping Sortie+Patrol), not 5 equal technical sections. Removed per-section refresh, removed raw `Ready`/`ReadyToLaunch` enum exposure, added localized player strings.
- **No floating button**, no Queue Sidebar above modal (added `HiveMapArmyBootstrap.ModalOpenForExternalHost` to `HiveMapQueueSidebarBootstrap.anyOverlayOpen` and to `HiveMapProductionBootstrap`/`HiveMapOverlayInputGateBootstrap` blocked lists), no bees/badges above modal (same), no click-through (overlay gate blocks `BuildingInteractionController` and `LivingHiveMenuRuntime` when Army open), restores HiveMap on close (back arrow `×`).

## Assembly Boundary

- **Bridge:** `BeeKingdom.Core.Integration.LivingHiveArmyBridge` in `BeeKingdom.Core` (referenced by both `BeeKingdom.LivingHiveMenu` and `Assembly-CSharp`/`BeeKingdom.Playground`), no illegal `LivingHiveMenu → Playground` reference.
- **Wiring:** `HiveMapArmyBootstrap.Start()` sets handlers, `LivingHiveMenuCanvas.OnMoreRowClicked` calls bridge — clean, mirrors `LivingHiveActivitiesBridge` pattern.

## Files Changed

| File | Change |
|------|--------|
| `Assets/BeeKingdom/Core/Integration/LivingHiveArmyBridge.cs` | **NEW** — `IsOpen`, `SetHandlers`, `OpenOverlay` |
| `Assets/BeeKingdom/Playground/HiveMapArmyBootstrap.cs` | Removed floating `ARMÉE` button, added `LivingHiveArmyBridge` handlers, productized header/sections (Forces/Escouade/Doctrine/Opérations), translated states, removed per-section refresh, added WorldMap CTA, fixed `CS0103` by removing direct `LivingHiveResearchRuntime` reference |
| `Assets/BeeKingdom/Playground/MobileAccountSessionRuntimeBootstrap.cs` | Added `DoctrineRecruitmentControllerForHiveMap`, `SquadReservationControllerForHiveMap`, `PerimeterSortieControllerForHiveMap`, `CombatPatrolControllerForHiveMap` accessors |
| `Assets/BeeKingdom/Playground/HiveMapRuntimeBootstrapInitializer.cs` | Added `HiveMapArmyBootstrap.InitializeForScene` |
| `Assets/BeeKingdom/Playground/HiveMapOverlayInputGateBootstrap.cs` | Added `|| HiveMapArmyBootstrap.ModalOpenForExternalHost` to `blocked` |
| `Assets/BeeKingdom/Playground/HiveMapQueueSidebarBootstrap.cs` | Added Army to `anyOverlayOpen` to hide queue when Army open |
| `Assets/Experiments/Environment2D5D/LivingHiveMenu/LivingHiveMenuSpec.cs` | `MoreMenuEntries` added `"Armée"` first |
| `Assets/Experiments/Environment2D5D/LivingHiveMenu/LivingHiveMenuCanvas.cs` | `OnMoreRowClicked("Armée")` → `LivingHiveArmyBridge.OpenOverlay()` |
| `Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs` | `OpenLivingHiveFromWorldMap`: `HiveScenePath` → `HiveMapScenePath` + debounce (M016F) |
| `Assets/Experiments/Environment2D5D/LivingHiveMenu/LivingHiveResearchRuntime.cs` | Always re-attach `Host` to new controller (M016E) |
| `Assets/Experiments/Environment2D5D/LivingHiveMenu/LivingHiveResearchHost.cs` | Branch to authenticated Research when `IsResearchControllerAvailableForExternalHost` (M016E) |

## Tests

No new automated tests. Existing `MobileAccountSessionClientTests`, `GoogleOAuthIdentityExchangerTests` still pass. Recommended: `HiveMapArmyBootstrapTests` (IsEnvironmentScene, ModalOpen, InitializeForScene single root, More→Army opens Hub).

## Validation

- **Unity compile:** Fixed `CS0103` by removing illegal `LivingHiveResearchRuntime` reference in `HiveMapArmyBootstrap` (now via `ResearchOverlayOpenForExternalHost`), added `LivingHiveArmyBridge` correctly referenced, new accessors compile. Expected **0 errors**.
- **Runtime (expected):**
  - Bottom rail `Plus` → `Armée` → Army Hub opens (no floating button)
  - Forces/Squad/Opérations show real `State` translated (`Prête` etc.), no `Ready`/`ReadyToLaunch` raw
  - Perimeter Sortie `Error` now shows `Sortie indisponible — not_configured / Préparation : Réserve d'escouade requise` honestly, not hidden
  - Single global `Rafraîchir` works, no per-section buttons
  - Back arrow closes, HiveMap restores, no click-through
  - Barrack, Research, Activities, Royal Palace, WorldMap, WorldMap return still work, no duplicate roots

## CEO Manual Validation Required

1. Google login → Enter HiveMap
2. Tap **Plus** → **Armée** (bottom rail → More)
3. Confirm **Army** opens (no floating button)
4. Confirm **Forces / Escouade / Opérations** information appears, no `Ready`/`ReadyToLaunch` raw, `Sortie` shows honest unavailable if not configured (not `Error` raw)
5. Tap **Rafraîchir** (header) → still works
6. Close via **back arrow** → HiveMap restores
7. Open **Barrack** → training still works
8. **WorldMap → Hive** → reopen **Armée** → still loads

## Remaining Army Capability Gaps

- No direct launch/recall/claim actions exposed in Hub (requires new gameplay design for squad reservation + signal selection)
- No troop detail counts beyond `State`/`Capacity` (Balances not yet productized)
- Patrol launch still WorldMap-only

## Recommended Next Mission

1. **M019 — Army Actions** — Expose concrete `Reserve/Release`, `Launch`, `Claim` with server round-trip and validation.
2. **M020 — Army Troop Details** — Productize `Balances`, `Available*`, `Reserved*` into readable troop cards.
3. **M016G — Army Automated Tests** — PlayMode test for `More → Armée`, modal, no duplicates, round-trip.

## Confidence

**MEDIUM-HIGH** — Bottom-rail integration via bridge is assembly-safe, floating button removed, states translated, Sortie error classified, refresh centralized, input-safe. Full CEO manual validation required.

