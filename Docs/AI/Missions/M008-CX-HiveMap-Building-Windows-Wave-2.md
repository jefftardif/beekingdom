# M008-CX HIVEMAP BUILDING WINDOWS WAVE 2 RESULT

## Transformation

- previous LivingHive capability: manual production for `wax_workshop` / Transformation, with the same useful forecast pattern as Honey Reserve and Warehouse: pending amount, hourly rate, reserve/capacity. LivingHive also had local preview tutorial/production paths, but the useful current capability is the production forecast backed by the official production model.
- previous HiveMap behavior: Transformation already participated in `HiveMapProductionBootstrap` for tap-to-collect, badge display, zoom-derived badge sizing, collection feedback, HUD refresh and building selection. It did not have the M006 info button/forecast panel.
- authoritative controller/model: `HiveOfflineProductionPanelController` via `MobileAccountSessionRuntimeBootstrap.OfflineProductionControllerForHiveMap`; model lookup by legacy key `wax_workshop`.
- implementation: extended `HiveMapProductionInfoBootstrap`'s existing tracked production building list with `BuildingTypes.Transformation` and added its display label. `HiveMapProductionBootstrap` was not modified.
- resulting HiveMap behavior: Transformation now gets the same small read-only `i` forecast button as Honey Reserve/Warehouse, while tap-to-collect and badges remain owned by the existing production bootstrap.

## Infirmary

- previous LivingHive capability: `infirmary_grove` is marked `future` / "Fonctionnalite a venir" in both the monolith hotspot table and `BuildingCatalog`. No authoritative Infirmary gameplay panel/controller was found. Healing exists as a `SpeedUpCategory.Healing` concept for timers, but not as an Infirmary building controller.
- previous HiveMap behavior: clicking Infirmary fell through to `HiveMapBuildingUpgradeClickBootstrap`, opening the Construction picker pre-selected to Infirmary.
- authoritative controller/model: no Infirmary-specific server controller/model currently exists. The only supported current action is building upgrade through the existing Construction/Upgrade path.
- implementation: added `HiveMapUnsupportedBuildingBootstrap`. Infirmary now opens a HiveMap-native IMGUI status window explaining that official care gameplay is not yet exposed, with `Fermer` and `Ameliorer`. `Ameliorer` preserves the previous Construction picker path via `HiveViewProductUiPresenter.OpenConstructionOverlayForExternalHost(building.LegacyKey)`.
- resulting HiveMap behavior: Infirmary click no longer silently jumps straight to Construction; it shows an honest building-specific status window and keeps upgrade access one tap deeper.

## Genetics

- previous LivingHive capability: `genetics_garden` is marked `future` / "Fonctionnalite a venir". Docs/code contain research/genetics boundary/prototype references, but also indicate that genetics choices/mutations are not currently wired as official gameplay.
- previous HiveMap behavior: clicking Genetics fell through to the generic Construction picker pre-selected to Genetics.
- authoritative controller/model: no current server-backed Genetics controller/model exists. Current supported capability is only building upgrade.
- implementation: same `HiveMapUnsupportedBuildingBootstrap` handles Genetics with a building-specific message: genetics remains future and mutation/progression choices are not server-backed.
- resulting HiveMap behavior: Genetics click opens a HiveMap-native status window plus preserved `Ameliorer` access instead of converting prototype genetics into official gameplay.

## Files Changed

- `Assets/BeeKingdom/Playground/HiveMapProductionInfoBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapBuildingUpgradeClickBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapUnsupportedBuildingBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapUnsupportedBuildingBootstrap.cs.meta`
- `Assets/BeeKingdom/Playground/HiveMapOverlayInputGateBootstrap.cs`
- `Docs/AI/Missions/M008-CX-HiveMap-Building-Windows-Wave-2.md`

No scenes were modified. No commit was made.

## Legacy Dependencies Added or Retained

- `HiveViewProductUiPresenter.HasEnteredHiveForExternalHost`: retained by existing HiveMap bootstraps and used by the new status bootstrap as the same session/entry gate pattern.
- `HiveViewProductUiPresenter.OpenConstructionOverlayForExternalHost`: retained for Infirmary/Genetics `Ameliorer`, preserving the exact upgrade route those clicks previously used.
- `HiveOfflineProductionPanelController` direct access: reused through `MobileAccountSessionRuntimeBootstrap.OfflineProductionControllerForHiveMap`, not through a new monolith draw bridge.
- No new LivingHive fixed-image coordinates, hotspot hit testing or local-preview gameplay state were introduced.

## Server Authority

- Transformation forecast: server-backed when the official session/controller is configured; `NotConfigured` fallback remains explicit.
- Infirmary: no server-backed gameplay action found beyond building upgrade.
- Genetics: no server-backed gameplay action found beyond building upgrade.
- Infirmary/Genetics status messages are presentation-only and do not create gameplay state.

## Validation

- `git status --short` before implementation confirmed M006 was committed (`08cab18 HiveMap building windows wave 1`) and M007 documentation files were untracked; M007 files were not modified.
- `dotnet build Assembly-CSharp.csproj --no-restore`: success, 0 errors, 210 warnings. Warnings are existing Unity/project warnings; the new status bootstrap was adjusted to avoid adding `FindFirstObjectByType` deprecation warnings.
- `git diff --check`: success, no whitespace errors.
- Unity batchmode compile/Play Mode could not be run: Unity reported another instance already had `C:/projets/beekingdomgame-master` open, so batchmode aborted before import/play.
- `dotnet test BeeKingdom.Tests.csproj --no-restore --filter ...`: returned exit code 0 but produced no test output; `--list-tests` also produced no output, so this was not counted as meaningful coverage.

## Regression Checks

- Honey Reserve/Warehouse: existing `HiveMapProductionInfoBootstrap` path preserved; only Transformation was appended to the same tracked list.
- Transformation tap-to-collect/badge/HUD: `HiveMapProductionBootstrap.cs` was not modified.
- Nursery: `HiveMapNurseryBootstrap.cs` was not modified; its overlay gate flag remains.
- Barrack/Research/Construction/Settings/Alliance: not modified.
- Generic upgrade fallback: still applies to Defense, Academy, Bank, Royal Palace and Champion Hall. Genetics/Infirmary are excluded only because they now show their own status window with an upgrade button.
- Input gating: `HiveMapOverlayInputGateBootstrap` now includes `HiveMapUnsupportedBuildingBootstrap.OverlayOpenForExternalHost`.

## Remaining Issues

- No authenticated session was available, so Ready/server-backed visual state for Transformation forecast was not exercised.
- Unity Play Mode validation was blocked by the already-open Unity project instance.
- Infirmary and Genetics still have no official gameplay controller. M008 intentionally did not convert future/prototype behavior into official gameplay.

## Recommended Next Wave

1. Add a small test/proof harness for HiveMap building click routing so exclusions like Nursery/Genetics/Infirmary are covered without full Play Mode.
2. Schedule authenticated Player Journey validation for production forecast Ready state.
3. Decide whether Infirmary or Genetics should receive real server contracts before adding gameplay UI.

## Confidence

MEDIUM

The implementation is intentionally narrow and compiles cleanly, but full Play Mode and authenticated validation were blocked in this session.
