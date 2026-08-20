# M013-CX HiveMap Building Windows Wave 4

## Executive Summary

M013 exposes Wave 4 building behavior in HiveMap without migrating LivingHive windows or inventing new gameplay.

Implemented:

- `Bank` now opens an honest HiveMap status window through the existing unsupported/future building pattern.
- `RoyalPalace` now opens a fullscreen HiveMap-native Coeur royal / Administration modal with the useful current subset recovered from LivingHive: level, level-cap role, upgrade state/action, progress, refresh and colony overview access.
- `Administration` is documented as the existing `administration_core` legacy key mapped to `BuildingTypes.RoyalPalace`; there is no separate `BuildingTypes.Administration`.
- The generic upgrade-click fallback no longer intercepts Bank or RoyalPalace.
- Modal gating now follows the M011 Research pattern for RoyalPalace and the recovered Colony Overview overlay: queue sidebar, world bees/badges, Barrack feedback and click-through are suppressed while the modal owns the screen.

No scene files were modified. No gameplay was redesigned.

## Scope

Mission targets:

- Bank
- Administration
- Royal Palace

Files changed:

- `Assets/BeeKingdom/Playground/HiveMapRoyalPalaceBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapRoyalPalaceBootstrap.cs.meta`
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`
- `Assets/BeeKingdom/Playground/HiveMapUnsupportedBuildingBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapBuildingUpgradeClickBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapOverlayInputGateBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapQueueSidebarBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapProductionBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapProductionInfoBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapBarrackBootstrap.cs`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.fr-CA.json`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.en-US.json`
- `Docs/AI/Missions/M013-CX-HiveMap-Building-Windows-Wave-4.md`

## Inspection Findings

### Bank

Catalog and mapping:

- `BuildingTypes.Bank`
- legacy key `hive_bank`
- label `Banque`
- catalog state `future`
- catalog disclosure `Fonctionnalite a venir.`

Server/client inspection found stock, reward ledger, VIP and other account-backed clients, but no bank-owned gameplay controller and no server-backed Bank action. `HiveBuildingUpgradeClient` validates only a limited supported set for official upgrades and does not accept `hive_bank`.

Decision:

- Do not expose stock, reward ledger, VIP, exchange, loans, currency, investments or premium mechanics through Bank.
- Reuse `HiveMapUnsupportedBuildingBootstrap` with a Bank-specific status message.
- Preserve the existing one-tap-deeper `Ameliorer` path through Construction.

Authority classification:

- Building presence/catalog: client catalog.
- Building-specific gameplay: unavailable/future.
- Upgrade access: legacy/provisional Construction entry; no Bank-specific official upgrade endpoint.

### Administration

There is no separate `BuildingTypes.Administration`.

The catalog entry named `Administration` uses legacy key `administration_core`, and `BuildingMappingTable` maps it to:

- `BuildingTypes.RoyalPalace`
- `BuildingLegacyKeys.AdministrationCore`

Decision:

- Do not create a parallel Administration type or duplicate window.
- Treat Administration as the presentation label for RoyalPalace / Coeur royal.

Authority classification:

- Separate Administration building: not present in the current code model.
- Administration behavior: handled by RoyalPalace / `administration_core`.

### Royal Palace

Catalog and mapping:

- `BuildingTypes.RoyalPalace`
- legacy key `administration_core`
- catalog label `Administration`
- role `Coeur royal - centre de gestion de la ruche`
- state `active`
- disclosure that the Coeur royal level caps other buildings.

LivingHive monolith contains a richer IMGUI detail block around `DrawAdministrationCoreDetail`, plus an official-building branch around `DrawOfficialBuildingUpgradeOnlyDetail`.

Useful existing RoyalPalace / Administration capabilities found:

- Coeur royal level:
  - official/current source when configured: `HiveBuildingUpgradeScreenModel.LevelFor("administration_core")`;
  - fallback source in LivingHive: persisted local preview level through `EnsureLocalPreviewLevel("administration_core")`.
- Relationship to other building caps:
  - current client rule: the Coeur royal level is the cap used by `UpgradeDisabledReason` / prerequisite redirect for non-core buildings.
- Upgrade state/action:
  - server-authoritative source when configured: `IHiveBuildingUpgradePanelController`, `HiveBuildingUpgradeScreenModel`, `HiveBuildingUpgradeClient`;
  - local preview fallback: existing LivingHive preview upgrade queue/state.
- Upgrade progress:
  - server-authoritative source when configured: `HiveBuildingUpgradeScreenModel.Progress01`;
  - local preview fallback: existing `UpgradeProgress01`.
- Colony Overview:
  - current client overlay in the monolith, opened from `DrawAdministrationCoreDetail`.

The safe reusable behavior for HiveMap is therefore not just role + Construction access. The recovered subset is level, authority label, cap explanation, upgrade status/progress/action, refresh, and Colony Overview access.

`HiveBuildingUpgradeClient` supports `administration_core`, so the Construction path can route through the existing official upgrade behavior when configured.

Decision:

- Add a dedicated `HiveMapRoyalPalaceBootstrap`.
- Open on `BuildingTypes.RoyalPalace`.
- Show the Coeur royal / Administration role, current level, level authority, cap role and upgrade state.
- Use targeted external-host bridges for RoyalPalace level/status/action rather than copying `DrawAdministrationCoreDetail`.
- Preserve direct upgrade action through the existing official/local upgrade path.
- Preserve access to the existing Colony Overview overlay.
- Do not extract or expose strategic path, power, decrees, taxes, prestige, class changes or kingdom buffs from this window.

Authority classification:

- Catalog role/disclosure: client catalog.
- Coeur royal level: server-authoritative when `HiveBuildingUpgradePanelController` is configured; otherwise current LivingHive local preview fallback, labelled as preview.
- Level-cap relationship: current client model, enforced by existing upgrade-disabled/prerequisite logic.
- Upgrade status/action/progress: server-authoritative when configured; otherwise legacy local preview.
- Colony Overview: current client overlay.
- Power/class/strategic path: existing separate systems, not RoyalPalace building UI in this mission.

## Implementation Details

### New Royal Palace Window

Added `HiveMapRoyalPalaceBootstrap`.

Behavior:

- Auto-starts only in loaded scenes whose active scene name starts with `Environment2D5D`.
- Waits until HiveMap has entered the Hive.
- Subscribes to `BuildingInteractionController.Selection.BuildingClicked`.
- Opens only for `BuildingTypes.RoyalPalace`.
- Draws an incremental fullscreen IMGUI modal:
  - opaque full-screen veil/background
  - banner/top header
  - standard blue back arrow at `4,2,48,46`
  - title `PALAIS ROYAL` / `ROYAL PALACE`, starting at `x = 68` to preserve the M011 arrow/title spacing
  - secondary context using the existing Administration/Core role
  - current level
  - authority/source label
  - cap explanation
  - catalog role/disclosure
  - upgrade status
  - upgrade progress bar
  - `Rafraichir`
  - `Vue colonie`
  - direct upgrade action label from the existing upgrade model
- `Rafraichir` calls the existing building-upgrade controller refresh when official upgrade is configured.
- The upgrade button calls the existing official/local upgrade action path for `administration_core`.
- `Vue colonie` opens the existing Colony Overview overlay through a targeted external-host bridge.

### RoyalPalace Modal/Header Correction

After CEO partial validation, the useful RoyalPalace functionality was accepted but the presentation still leaked HiveMap world layers. The correction reuses the M011 modal-integration pattern:

- added `HiveMapRoyalPalaceBootstrap.ModalOpenForExternalHost`, true while either the RoyalPalace fullscreen surface or Colony Overview is open;
- converted the RoyalPalace surface from a floating box to a fullscreen modal with opaque background;
- added a clear top header using the current HiveMap/LivingHive visual language;
- reused `closing_arrow.png` through `HiveViewProductUiPresenter.DrawPremiumBackButtonForExternalHost`;
- kept title text clear of the arrow using the corrected M011 spacing convention.

### Final Naming/Header Correction

After final CEO review, the RoyalPalace functionality and modal behavior were accepted, but two naming/header issues remained.

Corrected:

- RoyalPalace fullscreen title now uses the official building display name:
  - French: `PALAIS ROYAL`
  - English: `ROYAL PALACE`
- `Cœur royal` remains only as role/concept text inside the content and localization, not as the building window title.
- `administration_core` legacy key and `BuildingTypes.RoyalPalace` mapping were not changed.
- Colony Overview now has a fullscreen title:
  - French: `VUE DE LA COLONIE`
  - English: `COLONY OVERVIEW`
- Final visual adjustment: Colony Overview title/subtitle now live in the top header beside the blue back arrow, matching the approved RoyalPalace header. The previous lower horizontal title banner is no longer used for the main Colony Overview screen.
- Colony Overview title starts at `x = 68`, with subtitle at `x = 70`, matching the Research/RoyalPalace blue-arrow spacing convention.

Localization keys added/updated:

- `building.administration_core.name`
- `building.administration_core.fullscreen_title`
- `ui.colony_overview.fullscreen_title`

Suppressed while `ModalOpenForExternalHost` is true:

- Queue Sidebar;
- manual-production bees;
- manual collection feedback;
- production info badges/panel drawing;
- production collection routing;
- Barrack ready/progress/claim feedback;
- Barrack click routing;
- upgrade prerequisite glow.

Gameplay timers continue:

- manual production ticking continues in `HiveMapProductionBootstrap.Update`;
- Barrack training ticking continues in `HiveMapBarrackBootstrap.Update`.

### Bank Status

Extended `HiveMapUnsupportedBuildingBootstrap`.

Behavior:

- Tracks `BuildingTypes.Bank`.
- Shows title `Banque`.
- Shows Bank-specific future/status copy explaining that stocks, rewards and official resources remain in their dedicated panels and clients, and that no separate bank action is server-backed today.
- Preserves `Ameliorer` through Construction.

### Generic Upgrade Fallback

Updated `HiveMapBuildingUpgradeClickBootstrap`.

Behavior:

- Added `BuildingTypes.Bank` to excluded building types.
- Added `BuildingTypes.RoyalPalace` to excluded building types.

This prevents the generic Construction picker from racing or replacing the new building-specific windows.

### Input Gating

Updated `HiveMapOverlayInputGateBootstrap`.

Behavior:

- Added `HiveMapRoyalPalaceBootstrap.ModalOpenForExternalHost` to the blocked overlay list.
- Bank is covered by the existing `HiveMapUnsupportedBuildingBootstrap.OverlayOpenForExternalHost` gate.

This blocks click-through to 3D buildings, bottom nav and header/menu canvas while the new RoyalPalace window or the recovered Colony Overview overlay is open.

## Server Authority Classification

### Server-backed / directly reusable

- `HiveBuildingUpgradeClient` supports `administration_core`.
- `HiveBuildingUpgradePanelController` / `HiveBuildingUpgradeScreenModel` provide server-authoritative level, offer, operation, cost, duration, progress and action state when configured.
- The RoyalPalace window now uses the existing official/local upgrade action path for `administration_core`.

### Server-backed but not Bank-owned

- `HiveStockSnapshotClient`
- `HiveRewardLedgerClient`
- `HiveVipClient`
- `StrategicPathClient`

These exist, but M013 does not attach them to Bank or RoyalPalace because the repository does not model them as building-specific actions for those clicks.

### Not server-backed for this mission

- Bank gameplay.
- Separate Administration building behavior.
- Royal decrees, taxes, prestige, currency exchange, loans, investment or kingdom buffs.
- Colony Overview, which is a current client overlay rather than a server contract.

## Legacy Dependencies Used

Acceptable temporary bridge:

- `HiveViewProductUiPresenter.RoyalPalaceLevelForExternalHost`
- `HiveViewProductUiPresenter.RoyalPalaceLevelAuthorityForExternalHost`
- `HiveViewProductUiPresenter.RoyalPalaceUpgradeStatusForExternalHost`
- `HiveViewProductUiPresenter.RoyalPalaceUpgradeActionLabelForExternalHost`
- `HiveViewProductUiPresenter.RoyalPalaceUpgradeActionEnabledForExternalHost`
- `HiveViewProductUiPresenter.RoyalPalaceUpgradeProgressForExternalHost`
- `HiveViewProductUiPresenter.RefreshRoyalPalaceUpgradeForExternalHost`
- `HiveViewProductUiPresenter.RunRoyalPalaceUpgradeActionForExternalHost`
- `HiveViewProductUiPresenter.OpenColonyOverviewForExternalHost`
- `HiveViewProductUiPresenter.DrawColonyOverviewOverlayForExternalHost`
- `HiveViewProductUiPresenter.ColonyOverviewOpenForExternalHost`
- `HiveViewProductUiPresenter.DrawPremiumBackButtonForExternalHost`

Reason:

- They expose existing RoyalPalace state/action without copying LivingHive IMGUI layout.
- They preserve official upgrade behavior where configured.
- They keep Colony Overview access available from HiveMap while LivingHive scene retirement continues.

Legacy dependencies not added:

- No `DrawAdministrationCoreDetail` bridge.
- No HiveLedger proof bridge.
- No StrategicPath proof bridge.
- No fixed LivingHive detail layout reuse.
- No scene dependency on `LivingHive.unity`.

## Manual CEO Validation Checklist

Recommended Play Mode checks:

1. Open `Environment2D5D_HiveMap_Test`.
2. Enter HiveMap.
3. Tap Bank.
4. Confirm a Banque status window opens instead of silently routing to Construction.
5. Confirm the Bank window blocks clicks on buildings and bottom/header UI underneath.
6. Confirm `Fermer` closes the Bank window.
7. Confirm `Ameliorer` opens Construction preselected to the Bank entry or shows the existing Construction behavior for that building.
8. Tap RoyalPalace / Administration / Coeur royal.
9. Confirm the Coeur royal window opens.
10. Confirm a clear `PALAIS ROYAL` / `ROYAL PALACE` fullscreen header appears.
11. Confirm the blue back arrow appears at top-left and the title starts to the right of it.
12. Confirm Queue Sidebar is hidden.
13. Confirm ambient/manual-production bees, production info badges and collection feedback are hidden.
14. Confirm Barrack ready/progress/claim feedback is hidden.
15. Confirm the RoyalPalace window blocks click-through.
16. Confirm the blue back arrow closes it cleanly and restores normal HiveMap presentation.
17. Confirm the displayed Coeur royal level and authority label are visible.
18. Confirm the cap explanation is visible.
19. Confirm the upgrade status/progress area is visible.
20. Confirm the upgrade button starts/completes/refreshes using the existing upgrade behavior for `administration_core`.
21. Confirm `Vue colonie` opens the existing Colony Overview overlay, shows `VUE DE LA COLONIE` / `COLONY OVERVIEW`, and that its back button closes/returns correctly.
22. Confirm existing Research, Champion Hall, Academy, Defense, Infirmary and Genetics windows still open as before.

## Non-Goals Preserved

M013 deliberately did not:

- add Bank loans, interest, investment, currency exchange or premium financial mechanics;
- add royal decrees, taxes, prestige, buffs or class powers;
- migrate LivingHive windows wholesale;
- modify scenes;
- modify generated project files permanently;
- duplicate Administration as a separate building type;
- attach stock/reward/VIP/strategic path systems to Bank without a product-backed building ownership model;
- expose StrategicPath, power/class panels or proof-only methods from the RoyalPalace window.

## Validation

Compilation:

- Command: `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal /clp:ErrorsOnly`
- Result after final Colony Overview header-layout correction: success, 0 errors, 311 warnings.

Note:

- `Assembly-CSharp.csproj` did not permanently include all recent HiveMap bootstrap scripts. For compile validation only, the missing script includes were added temporarily, build was run, then the `.csproj` was restored to no diff.

Diff validation:

- Final `.csproj` diff: none.
- No scene files modified.
- No commit performed.

Play Mode:

- Unity processes were present, but no safe automated Play Mode control was performed from this task context.
- CEO manual validation remains required for the corrected RoyalPalace fullscreen/modal window.

## Risks

- RoyalPalace now depends on targeted `HiveViewProductUiPresenter` external-host bridges. This is an intentional temporary adapter while the official upgrade/current client models are strangled out of the monolith.
- If no official upgrade controller is configured, the level/action falls back to the existing LivingHive local preview path and is labelled as preview rather than server-authoritative.
- The recovered Colony Overview overlay is still drawn by the monolith through an external-host bridge; M013 only makes it accessible and modal-safe from HiveMap, it does not migrate its presentation.
- Bank still has only a future/status surface. This is intentional because current code does not provide a bank-owned authoritative gameplay controller.
- The next product decision may choose whether Bank should own stock/reward ledger entry points, but that should be a product/API decision, not an architectural shortcut.

## Recommendation

Accept M013 if manual Play Mode confirms:

- Bank opens a clear future/status window.
- RoyalPalace / Administration opens a fullscreen functional Coeur royal modal with level, cap, upgrade state/action and Colony Overview access.
- Both flows preserve upgrade access where expected.
- No click-through occurs while either window is open.

Later work should only deepen these windows when a server-backed product capability exists for the building itself.

## Confidence

HIGH
