# M004-OC HIVEMAP MIGRATION PROVENANCE

**Project:** BeeKingdom
**Mission:** M004
**Owner:** OC
**Status:** CLOSED
**Historical record:** YES
**Date:** 2026-08-20

---

## CONTEXT

CL completed M003 and produced the LivingHive → HiveMap migration inventory.

Important product rule from CEO/GPT:

> If a feature/window has already been ported to HiveMap and modified afterward, the HiveMap version is authoritative and must never be overwritten by an older LivingHive version.

LivingHive is now considered a source of remaining useful functionality, not the reference implementation for features already evolved in HiveMap.

## OBJECTIVE

Determine the provenance and divergence history of all HiveMap features/windows that CL classified as:

* ALREADY PORTED
* PARTIALLY PORTED

The goal is to know which HiveMap implementations are already newer than their LivingHive equivalents and therefore must be protected from regression during the migration project.

This is an inspection mission only.

## INSPECT AT MINIMUM

* Splash / Auth / Login
* Building selection / click interaction
* Construction / Upgrade
* SpeedUp
* Queue Sidebar
* Barrack / troop training
* Manual resource collection
* Settings
* Communication mini-chat
* Alliance compact overlay
* Research
* Bag / resources detail
* Bottom navigation rail
* Notifications / badges

Also include any additional migrated HiveMap feature discovered during inspection.

## METHOD

Use repository history where useful:

* `git log`
* `git show`
* `git blame`
* file history
* relevant documentation
* current code comparison

For each feature, determine:

1. Was functionality originally taken from LivingHive?
2. Was a new HiveMap-specific implementation created?
3. Has the HiveMap implementation changed since the initial port?
4. Does LivingHive still contain functionality absent from the HiveMap version?
5. Would copying the LivingHive implementation now cause a regression?

## CLASSIFICATION

Assign one of these migration protections:

### PROTECT HIVEMAP

HiveMap is clearly newer and/or product-modified after migration. Never replace it with LivingHive.

### MERGE FUNCTIONALITY

HiveMap is authoritative, but LivingHive still contains useful behavior that should be selectively integrated.

### SHARED LEGACY

HiveMap currently invokes essentially the same LivingHive implementation. No independent HiveMap version yet exists.

### SAFE TO REPLACE

Rare case: HiveMap contains only a placeholder or inferior temporary implementation and LivingHive contains the intended maintained version.

### UNKNOWN

History/evidence is insufficient.

## IMPORTANT RULES

* Do not modify any code.
* Do not migrate anything.
* Do not redesign UI.
* Do not infer "newer" only from file timestamps.
* Prefer Git history and actual functional differences.
* Do not classify a feature as SAFE TO REPLACE merely because LivingHive has more code.

The objective is preventing regression, not maximizing reuse.

---

## EXECUTION LOG

### Git History Analysis

**Phase 1 - Baseline Recovery:** `4e88f68` (~Aug 10) — "BASELINE: recover latest LivingHive production state"

**Phase 2 - Migration:** `0e2af83` (Aug 18) — "HiveMap: sidecar + contexte + overview fix + 14 bâtiments positionnés"
- Created first HiveMap bootstraps: `HiveMapAllianceBootstrap`, `HiveMapSplashBootstrap`, `LivingHiveChatBridge*`
- Ported `LivingHiveMenu` (Canvas, Header, Visuals, ResearchWindow, Spec, State, Runtime, Host) to uGUI

**Phase 3 - Full Migration:** `7f3fc18` (Aug 19) — "HiveMap + LivingHive + Auth + Production bootstrap (2026-08-19)"
- Created 11 HiveMap*Bootstrap files: Alliance, Barrack, BuildingUpgradeClick, Construction, OverlayInputGate, Production, QueueSidebar, ResourceHud, Settings, VisualState, (Splash from Phase 2)
- All bootstraps are thin adapters: translate 3D building clicks → `HiveViewProductUiPresenter.ForExternalHost` methods

**Phase 4 - Post-Migration Fix:** `79f6660` (Aug 19) — "Bring troop training under real server authority, fix Barrack UX"
- `HiveMapBarrackBootstrap` wired to real `CombatRecruitmentService` instead of local-preview
- Added tap-to-claim, ready badge, claim sound, training highlight, server capacity

### Key Finding
The HiveMap bootstraps are **thin adapters** that translate 2.5D building clicks and scene events into calls to `HiveViewProductUiPresenter`'s `ForExternalHost` bridge methods. The actual UI panels (IMGUI overlays for Construction, Barrack, Alliance, Settings, Chat, SpeedUp, Queue Sidebar) **are still implemented in the monolith** and shared with LivingHive.

Only **two HiveMap-specific uGUI implementations exist**: `LivingHiveMenuCanvas` (bottom rail + header) and `LivingHiveResearchWindow` (fullscreen research). These are in the `BeeKingdom.LivingHiveMenu` package and used by HiveMap.

---

## PROVENANCE MATRIX

| Feature | LivingHive Origin | HiveMap Implementation | Post-Port Changes | Protection | Evidence |
|---|---|---|---|---|---|
| **Splash / Auth / Login** | Monolith `HiveViewProductUiPresenter.Draw()` (IMGUI) | `HiveMapSplashBootstrap` gates monolith splash, plays music, skips tutorial | None since `0e2af83` | **PROTECT HIVEMAP** | `HiveMapSplashBootstrap.cs` created `0e2af83`, unmodified; gates `LivingHiveMenuRuntime.Root` + `BuildingInteractionController` |
| **Building Selection / Click Interaction** | Monolith hotspot raycast on reference image | `BuildingRuntimeViewBootstrap` creates 3D colliders; `HiveMap*Bootstrap` subscribe to `BuildingSelection.BuildingClicked` | `HiveMapBarrackBootstrap` updated `79f6660` for server-authoritative training | **PROTECT HIVEMAP** | 11 HiveMap bootstraps created `7f3fc18`; Barrack further modified `79f6660` |
| **Construction / Upgrade** | Monolith `DrawConstructionOverlayForExternalHost` (IMGUI) | `HiveMapConstructionBootstrap` + `HiveMapBuildingUpgradeClickBootstrap` + `HiveMapBuildingUpgradeVisualStateBootstrap` | None since `7f3fc18` | **PROTECT HIVEMAP** | Three dedicated bootstraps created `7f3fc18`; prerequisite glow, upgrading tint |
| **SpeedUp** | Monolith `DrawSpeedUpOverlayForExternalHost` (IMGUI) | **No HiveMap bootstrap** — called directly from Barrack/Construction bootstraps | None | **SHARED LEGACY** | `HiveMapBarrackBootstrap.OnGUI` + `HiveMapConstructionBootstrap.OnGUI` call `DrawSpeedUpOverlayForExternalHost` |
| **Queue Sidebar** | Monolith `DrawQueueSidebarForExternalHost` (IMGUI) | `HiveMapQueueSidebarBootstrap` hides sidebar when overlays open | None since `7f3fc18` | **PROTECT HIVEMAP** | Created `7f3fc18`; hides on Alliance/Chat/Barrack/Construction open |
| **Barrack / Troop Training** | Monolith local-preview training flow | `HiveMapBarrackBootstrap` → **wired to real `CombatRecruitmentService`** `79f6660` | **Major: server-authoritative** `79f6660` | **PROTECT HIVEMAP** | `79f6660` added tap-to-claim, ready badge, training highlight, server capacity |
| **Manual Resource Collection** | Monolith local-preview manual production | `HiveMapProductionBootstrap` computes on-screen rects from 3D colliders + camera zoom | Badge size unified with Barrack `7f3fc18` (Jeff 2026-08-19) | **PROTECT HIVEMAP** | Created `7f3fc18`; zoom-derived badge sizing (unique to HiveMap) |
| **Settings** | Monolith `DrawSettingsOverlayForExternalHost` (IMGUI) | `HiveMapSettingsBootstrap` bridges to `LivingHiveSettingsBridge` | None since `7f3fc18` | **SHARED LEGACY** | Created `7f3fc18`; thin bridge to monolith IMGUI |
| **Communication / Mini-Chat** | Monolith `DrawCommunicationOverlayForExternalHost` (IMGUI) | `LivingHiveChatBridgeBootstrap` wires real server chat to `LivingHiveChatBridge`; HiveMap uses same IMGUI overlay | Chat-only activation `TryActivateChatOnlyForActiveSession` | **SHARED LEGACY** | `LivingHiveChatBridgeBootstrap` created `0e2af83`; `HiveMapOverlayInputGateBootstrap` blocks input |
| **Alliance Compact Overlay** | Monolith `DrawAllianceOverlayForExternalHost` (IMGUI) | `HiveMapAllianceBootstrap` triggers on `ALLIANCE_CENTER` click | None since `7f3fc18` | **PROTECT HIVEMAP** | Created `0e2af83`, updated `7f3fc18`; building-click trigger (not rail button) |
| **Research** | Monolith `DrawResearchFullscreen` (IMGUI) | **`LivingHiveResearchWindow` (uGUI, full port)** + `LivingHiveResearchRuntime` attaches to building clicks | Visual polish `7f3fc18` (animations, filter rail, progress bars) | **PROTECT HIVEMAP** | Full uGUI port created `0e2af83`, enhanced `7f3fc18`; no monolith IMGUI dependency |
| **Bag / Resources Header** | Monolith resource totals in top HUD | `HiveMapResourceHudBootstrap` pushes live server totals → `LivingHiveMenuHeaderData` with roll-up animation | None since `7f3fc18` | **PROTECT HIVEMAP** | Created `7f3fc18`; 1s poll, roll-up animation in `LivingHiveMenuCanvas` |
| **Bottom Navigation Rail** | Monolith 10-item landscape rail (IMGUI) | **`LivingHiveMenuCanvas` (uGUI, full port)** 5-btn portrait / 10-btn landscape | Responsive header `cc730e1`; roll-up animation `7f3fc18` | **PROTECT HIVEMAP** | Ported `6038d82` (87/87 tests); header integrated `cc730e1`; fluid animation `7f3fc18` |
| **Badge Sizing (Zoom-Derived)** | Monolith overlay badges (IMGUI) | **HiveMap-specific**: `HiveMapProductionBootstrap` + `HiveMapBarrackBootstrap` compute zoom-derived badge sizes from 3D colliders | Unified size formula `7f3fc18` (Jeff 2026-08-19) | **PROTECT HIVEMAP** | `BadgeWorldSize = 10.8f` shared; camera orthographicSize scaling |

---

## PROTECT HIVEMAP

**Must never be overwritten by LivingHive implementations.**

| Feature | Why Protected |
|---|---|
| **Splash / Auth / Login** | `HiveMapSplashBootstrap` gates entire HiveMap scene (music, `LivingHiveMenuRuntime`, building clicks) until monolith splash completes — LivingHive has no equivalent gate |
| **Building Click Interaction** | 11 dedicated `HiveMap*Bootstrap` files translate 3D collider clicks → monolith actions; LivingHive uses 2D hotspot raycasts on reference image |
| **Construction / Upgrade** | Three bootstraps (`Construction`, `BuildingUpgradeClick`, `BuildingUpgradeVisualState`) handle prerequisite glow, upgrading tint, click-to-open-picker — LivingHive has no 3D building equivalents |
| **Barrack / Troop Training** | **`79f6660` wired to real `CombatRecruitmentService`** — server-authoritative costs, timers, capacity (nursery level), tap-to-claim badge, claim sound, zero-latency ready detection. LivingHive uses local-preview only. |
| **Manual Resource Collection** | `HiveMapProductionBootstrap` computes on-screen rects from 3D colliders + camera zoom; unified `BadgeWorldSize=10.8f` shared with Barrack. LivingHive uses fixed reference-image coordinates. |
| **Queue Sidebar** | `HiveMapQueueSidebarBootstrap` hides sidebar when ANY full-screen overlay open (Alliance/Chat/Barrack/Construction) — LivingHive monolith handles this internally |
| **Alliance Overlay** | Triggered by `ALLIANCE_CENTER` building click (not rail button) — HiveMap-specific interaction model |
| **Research Window** | **Full uGUI port** (`LivingHiveResearchWindow`): filter rail, responsive grid, progress bars, open/close animation, Escape key. LivingHive monolith uses IMGUI `DrawResearchFullscreen`. |
| **Bag / Resources Header** | `HiveMapResourceHudBootstrap` pushes live server totals → `LivingHiveMenuHeaderData` with roll-up animation. LivingHive monolith has hardcoded preview values. |
| **Bottom Navigation Rail** | **Full uGUI port** (`LivingHiveMenuCanvas`): 5/10 buttons, premium sprites, dividers, glow, progress line, header chips with roll-up animation. LivingHive monolith uses IMGUI `DrawBottomRail`. |
| **Badge Sizing (Zoom-Derived)** | `BadgeWorldSize = 10.8f` + `pixelsPerWorldUnit = Screen.height / (2 * orthographicSize)` — computes once per frame, shared by Production + Barrack. LivingHive has no zoom. |

---

## MERGE FUNCTIONALITY

**HiveMap stays authoritative, but LivingHive contains useful behavior to selectively integrate.**

| Feature | LivingHive Behavior to Merge |
|---|---|
| **Settings Overlay** | Monolith IMGUI Settings (reduced motion, economy mode, sound, music, language — PlayerPrefs-backed) is **complete and tested**. HiveMap bridges to it via `LivingHiveSettingsBridge`. No HiveMap-specific version needed. |
| **Communication / Chat** | `LivingHiveChatBridgeBootstrap` wires real server chat (SignalR + REST) to `LivingHiveChatBridge`. HiveMap reuses monolith IMGUI mini-chat overlay. Server-backed chat is **not in LivingHiveMenu** — only in bridge bootstrap. |
| **SpeedUp Overlay** | Monolith `DrawSpeedUpOverlayForExternalHost` handles server-authoritative inventory + timers + idempotent apply. HiveMap calls it from Barrack/Construction. No HiveMap-specific version. |

---

## SHARED LEGACY

**HiveMap directly invokes the same LivingHive/monolith implementation. No independent HiveMap version exists.**

| Feature | Shared Implementation |
|---|---|
| **Construction Overlay** | `HiveViewProductUiPresenter.DrawConstructionOverlayForExternalHost` (IMGUI) — called by `HiveMapConstructionBootstrap` |
| **Alliance Overlay** | `HiveViewProductUiPresenter.DrawAllianceOverlayForExternalHost` (IMGUI) — called by `HiveMapAllianceBootstrap` |
| **Barrack Overlay** | `HiveViewProductUiPresenter.DrawBarrackOverlayForExternalHost` (IMGUI) — called by `HiveMapBarrackBootstrap` (but logic is now server-backed) |
| **Communication / Mini-Chat Overlay** | `HiveViewProductUiPresenter.DrawCommunicationOverlayForExternalHost` (IMGUI) — called by `LivingHiveChatBridgeBootstrap` + `HiveMapOverlayInputGateBootstrap` |
| **Settings Overlay** | `HiveViewProductUiPresenter.DrawSettingsOverlayForExternalHost` (IMGUI) — called by `HiveMapSettingsBootstrap` |
| **SpeedUp Overlay** | `HiveViewProductUiPresenter.DrawSpeedUpOverlayForExternalHost` (IMGUI) — called by Barrack/Construction bootstraps |
| **Queue Sidebar** | `HiveViewProductUiPresenter.DrawQueueSidebarForExternalHost` (IMGUI) — called by `HiveMapQueueSidebarBootstrap` |
| **Manual Production Bees/Feedback** | `HiveViewProductUiPresenter.DrawManualProductionBeesForExternalHost` + `DrawManualCollectionFeedbackForExternalHost` — called by `HiveMapProductionBootstrap` |

---

## SAFE TO REPLACE

| Feature | Assessment |
|---|---|
| **None** | No HiveMap feature is a mere placeholder. Every HiveMap bootstrap either (a) adapts the monolith to 2.5D (3D colliders, camera zoom, building-click triggers) or (b) is a full uGUI port superior to the monolith IMGUI. LivingHive has **more code** in some areas (e.g., full simulation prototype in `LivingHiveDemoBootstrap`), but that code is **incompatible** with HiveMap's architecture (3D scene, server-authoritative, no reference image). |

---

## UNKNOWN

| Feature | Reason |
|---|---|
| **World Map / Surface Switch** | `LivingHiveMenuCanvas` has a "Carte" panel + `OpenWorldMap()` that loads `WorldMapWave6Wave5Method12288Preview.unity`. HiveMap has `WorldMapMmoFullscreenFoundationBootstrap` but its relationship to the menu's surface switch is unclear. |
| **Queen Profile / Shop Panels** | `LivingHiveMenuCanvas` builds uGUI `QueenProfile` and `Shop` panels (from monolith `DrawPortraitTopHud`). HiveMap uses same header — but whether these panels are fully functional or stubs is undetermined. |
| **Combat / Doctrine / Formation / Patrol** | `HiveViewProductUiPresenter` has `CombatDoctrine`, `CombatFormationReadiness`, `CombatRecruitment`, `CombatSquadReservation`, `HivePerimeterSortie` — some enabled in `appsettings.Production.json`. HiveMap bootstraps don't explicitly wire these; unclear if they work via monolith. |

---

## Migration Rules Derived From History

1. **Never copy LivingHive IMGUI code to HiveMap**. HiveMap's architecture is 3D colliders + camera zoom + building-click triggers. LivingHive's IMGUI assumes a flat reference image with fixed hotspot coordinates.

2. **HiveMap bootstraps are adapters, not reimplementations**. They subscribe to `BuildingInteractionController.Selection.BuildingClicked` and call `HiveViewProductUiPresenter.ForExternalHost` methods. The monolith remains the single source of truth for panel UI.

3. **uGUI ports in `BeeKingdom.LivingHiveMenu` ARE the HiveMap versions**. `LivingHiveMenuCanvas` (rail + header) and `LivingHiveResearchWindow` are full uGUI ports with animations, responsive layouts, and roll-up number animations. They supersede the monolith IMGUI equivalents.

4. **Post-migration server wiring wins**. `HiveMapBarrackBootstrap` was updated in `79f6660` to use real `CombatRecruitmentService` instead of local-preview. This is the pattern: migrate UI adapter first, then wire to server authority.

5. **Zoom-derived visual sizing is HiveMap-specific**. The `BadgeWorldSize` + `pixelsPerWorldUnit` formula (computed once per frame, shared by Production + Barrack) has no LivingHive equivalent. Do not port fixed-pixel badge code.

6. **Input gating is mandatory for IMGUI overlays**. `HiveMapOverlayInputGateBootstrap` disables `BuildingInteractionController` and `LivingHiveMenuRuntime.CanvasComponent` when any IMGUI overlay opens. LivingHive monolith has `PremiumUiBlocksWorldInput()` but HiveMap calls `Draw*ForExternalHost` directly, bypassing that protection.

7. **LivingHiveMenu package is the uGUI layer for HiveMap**. It has no dependency on the default assembly. Cross-assembly bridges (`LivingHiveChatBridge`, `LivingHiveSettingsBridge`) allow default-assembly code to talk to it.

---

## Risks

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| **Accidentally porting LivingHive IMGUI hotspot logic to HiveMap** | High | Breaks 3D building clicks, wrong coordinates | Enforce: all HiveMap UI triggers go through `BuildingInteractionController` + `ForExternalHost` |
| **Overwriting `LivingHiveMenuCanvas`/`LivingHiveResearchWindow` with LivingHive IMGUI** | Medium | Lose uGUI animations, responsive layout, roll-up numbers | Mark package `BeeKingdom.LivingHiveMenu` as **authoritative for HiveMap uGUI** |
| **Forgetting `HiveMapOverlayInputGateBootstrap` when adding new IMGUI overlay** | Medium | Clicks leak through to buildings/rail | Add new overlay flags to `HiveMapOverlayInputGateBootstrap.blocked` condition |
| **Diverging badge sizing between Production and Barrack** | Low | Visual inconsistency | Keep `BadgeWorldSize` + `pixelsPerWorldUnit` as shared constants or utility |
| **Assuming LivingHiveDemoBootstrap patterns apply to HiveMap** | Medium | Introduces simulation prototype code, legacy camera/bee visuals | HiveMap uses `BuildingRuntimeViewBootstrap` + monolith external host; no `LivingHiveDemoBootstrap` code |

---

## Confidence

**HIGH** — Based on:
- Complete Git history for all `HiveMap*Bootstrap.cs` files (created `7f3fc18`, mostly unmodified)
- `79f6660` clearly documents Barrack server-authoritative migration
- Code inspection confirms all HiveMap bootstraps are thin adapters to `HiveViewProductUiPresenter.ForExternalHost`
- `LivingHiveMenuCanvas` + `LivingHiveResearchWindow` are full uGUI ports with features absent from monolith (animations, roll-up, responsive grid)
- No HiveMap feature found that is a placeholder or inferior to LivingHive equivalent

---

**Report complete. Awaiting GPT orchestration.**