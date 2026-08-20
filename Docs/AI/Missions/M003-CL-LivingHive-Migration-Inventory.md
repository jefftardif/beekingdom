# M003-CL LIVINGHIVE MIGRATION INVENTORY

**Project:** BeeKingdom
**Mission:** M003
**Owner:** CL
**Status:** CLOSED
**Historical record:** YES
**Date:** ~2026-08-19

---

## HISTORICAL REPORT — PARTIALLY RECONSTRUCTED

**Notice:** The exact original M003 report is not available in the repository. This document has been reconstructed from Git history, the M004 provenance analysis, and repository state. Some details may be incomplete.

---

## CONTEXT (Reconstructed)

CL produced the LivingHive → HiveMap migration inventory, classifying features as ALREADY PORTED, PARTIALLY PORTED, or NOT YET PORTED. This inventory informed the M004 provenance analysis and established the product rule that HiveMap versions modified after migration are authoritative and must not be overwritten by older LivingHive versions.

## OBJECTIVE (Reconstructed)

Create a comprehensive inventory of all LivingHive features and their HiveMap migration status, enabling the M004 provenance analysis.

## INVENTORY (Reconstructed from M004 Analysis & Git History)

### ALREADY PORTED (HiveMap has functional equivalent)

| Feature | HiveMap Implementation | Evidence |
|---|---|---|
| Splash / Auth / Login | `HiveMapSplashBootstrap` gates monolith splash | Created `0e2af83` |
| Building Selection / Click | `BuildingRuntimeViewBootstrap` + 11 `HiveMap*Bootstrap` | Created `7f3fc18` |
| Construction / Upgrade | 3 bootstraps: Construction, BuildingUpgradeClick, BuildingUpgradeVisualState | Created `7f3fc18` |
| Queue Sidebar | `HiveMapQueueSidebarBootstrap` | Created `7f3fc18` |
| Barrack / Troop Training | `HiveMapBarrackBootstrap` — **server-authoritative** `79f6660` | Updated `79f6660` |
| Manual Resource Collection | `HiveMapProductionBootstrap` (zoom-derived badges) | Created `7f3fc18` |
| Settings | `HiveMapSettingsBootstrap` → `LivingHiveSettingsBridge` | Created `7f3fc18` |
| Communication / Mini-Chat | `LivingHiveChatBridgeBootstrap` + `HiveMapOverlayInputGateBootstrap` | Created `0e2af83`/`7f3fc18` |
| Alliance Overlay | `HiveMapAllianceBootstrap` (building-click trigger) | Created `0e2af83` |
| Research | **`LivingHiveResearchWindow` (uGUI full port)** + `LivingHiveResearchRuntime` | Created `0e2af83`, enhanced `7f3fc18` |
| Bag / Resources Header | `HiveMapResourceHudBootstrap` → `LivingHiveMenuHeaderData` | Created `7f3fc18` |
| Bottom Navigation Rail | **`LivingHiveMenuCanvas` (uGUI full port)** | Ported `6038d82`, enhanced `cc730e1`/`7f3fc18` |
| Badge Sizing (Zoom-Derived) | `HiveMapProductionBootstrap` + `HiveMapBarrackBootstrap` shared formula | `7f3fc18` (Jeff 2026-08-19) |

### PARTIALLY PORTED (HiveMap has adapter, monolith still owns panel UI)

| Feature | HiveMap Adapter | Monolith Panel (Shared) |
|---|---|---|
| Construction Overlay | `HiveMapConstructionBootstrap` | `DrawConstructionOverlayForExternalHost` |
| Barrack Overlay | `HiveMapBarrackBootstrap` | `DrawBarrackOverlayForExternalHost` (server-backed) |
| Alliance Overlay | `HiveMapAllianceBootstrap` | `DrawAllianceOverlayForExternalHost` |
| Communication Overlay | `LivingHiveChatBridgeBootstrap` | `DrawCommunicationOverlayForExternalHost` |
| Settings Overlay | `HiveMapSettingsBootstrap` | `DrawSettingsOverlayForExternalHost` |
| SpeedUp Overlay | Called from Barrack/Construction | `DrawSpeedUpOverlayForExternalHost` |
| Queue Sidebar | `HiveMapQueueSidebarBootstrap` | `DrawQueueSidebarForExternalHost` |

### NOT YET PORTED / UNKNOWN STATUS (Reconstructed)

| Feature | Status | Notes |
|---|---|---|
| World Map / Surface Switch | UNKNOWN | `LivingHiveMenuCanvas` has "Carte" panel; `WorldMapMmoFullscreenFoundationBootstrap` exists |
| Queen Profile / Shop Panels | UNKNOWN | uGUI panels in `LivingHiveMenuCanvas` — functionality unverified |
| Combat / Doctrine / Formation / Patrol | UNKNOWN | Enabled in `appsettings.Production.json` but no HiveMap bootstraps |
| Notifications / Badges | PROTECT HIVEMAP | Zoom-derived badge system is HiveMap-specific |

## KEY PRODUCT RULE ESTABLISHED

> If a feature/window has already been ported to HiveMap and modified afterward, the HiveMap version is authoritative and must never be overwritten by an older LivingHive version.

This rule was formalized based on the finding that:
- `HiveMapBarrackBootstrap` was updated post-migration (`79f6660`) for server-authoritative training
- `LivingHiveMenuCanvas` and `LivingHiveResearchWindow` are full uGUI ports with features absent from monolith IMGUI (animations, roll-up numbers, responsive grid)
- HiveMap-specific adaptations exist (zoom-derived badge sizing, 3D building click triggers, input gating)

## EVIDENCE SOURCES

- Commit `0e2af83`: Initial HiveMap bootstraps + LivingHiveMenu uGUI port
- Commit `7f3fc18`: Full 11 HiveMap bootstraps + LivingHiveMenu enhancements
- Commit `79f6660`: Barrack server-authoritative migration
- Commit `6038d82`: "FEAT: port LivingHive bottom menu to uGUI (87/87 tests)"
- Commit `cc730e1`: "feat(ui): integrate responsive hive header"
- M004 provenance analysis (this mission's sister report)

## LIMITATIONS OF RECONSTRUCTION

The following could not be verified from available evidence:
- Exact classification CL assigned to each feature (ALREADY PORTED vs PARTIALLY PORTED vs NOT YET PORTED)
- Whether CL identified additional features not discovered in M004 analysis
- CL's assessment of migration effort/complexity per feature
- Any features CL marked as "SAFE TO REPLACE" (M004 found none)
- Exact date of M003 completion

## RELATED COMMITS

- `4e88f68` — BASELINE: recover latest LivingHive production state
- `0e2af83` — HiveMap sidecar + context + overview + LivingHiveMenu port
- `6038d82` — Port LivingHive bottom menu to uGUI (87/87 tests)
- `cc730e1` — Integrate responsive hive header
- `7f3fc18` — 11 HiveMap bootstraps + LivingHiveMenu enhancements
- `79f6660` — Barrack server-authoritative migration

---

*This report was reconstructed on 2026-08-20 by OC as part of M007 mission history consolidation.*