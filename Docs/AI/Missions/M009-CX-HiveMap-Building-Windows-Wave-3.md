# M009-CX HIVEMAP BUILDING WINDOWS WAVE 3 RESULT

## Champion Hall

### Previous LivingHive behavior

LivingHive exposes Champion Bees through the legacy presenter flow, with UI state owned by `HiveViewProductUiPresenter` and related local/official champion state. The monolith contains a proof-only bridge for opening the panel, but no suitable player-facing external-host bridge for HiveMap.

### Previous HiveMap behavior

Champion Hall was excluded from dedicated HiveMap building windows and therefore fell through the generic upgrade-click bootstrap to the Construction picker.

### Current authoritative capability

Champion Hall has a reusable local catalog in `ChampionBeeCatalog` and a server-backed read model through `IHiveChampionBeeClient.ReadAsync`, exposed by `MobileAccountSessionRuntimeBootstrap.ChampionBeeClient` when the authenticated gameplay session enables the champion feature.

### Implementation

Added `HiveMapChampionHallBootstrap`, a HiveMap-native IMGUI window for `BuildingTypes.ChampionHall`. It opens from direct building clicks, displays the existing catalog, optionally reads the official server snapshot, and keeps progression mutations out of HiveMap for now. The existing upgrade path remains available through an `Ameliorer` button that calls the legacy Construction adapter.

### Resulting HiveMap behavior

Champion Hall now opens a building-specific HiveMap window instead of falling directly into Construction. Without a configured official client it honestly shows the local catalog only; with a configured client it shows owned/assigned/revision state from the server. It does not grant, level, assign, summon, equip, or invent any new champion gameplay.

## Academy

### Previous LivingHive behavior

No separate authoritative Academy gameplay controller was found. Research is already represented by `LivingHiveResearchWindow` and must remain distinct from Academy.

### Previous HiveMap behavior

Academy fell through to the generic Construction picker because it had no dedicated HiveMap window.

### Current authoritative capability

Academy is currently represented as a future building capability in the catalog. No separate server-backed Academy training/education controller was identified.

### Implementation

Extended `HiveMapUnsupportedBuildingBootstrap` to track `BuildingTypes.Academy` and show an Academy-specific status message. The message explicitly preserves Research ownership outside Academy and keeps upgrade one click deeper.

### Resulting HiveMap behavior

Academy now responds with an honest building-specific status window and an `Ameliorer` route, without redirecting to Research and without inventing an Academy progression surface.

## Defense

### Previous LivingHive behavior

LivingHive contains defense-adjacent preview/tutorial concepts and the project contains combat/perimeter server systems, but those systems are not currently owned by the Defense building as a supported player action.

### Previous HiveMap behavior

Defense fell through to the generic Construction picker because it had no dedicated HiveMap window.

### Current authoritative capability

Defense is currently cataloged as a future capability. Existing combat/perimeter systems remain separate flows and were not activated from the Defense building.

### Implementation

Extended `HiveMapUnsupportedBuildingBootstrap` to track `BuildingTypes.Defense` and show a Defense-specific status message. The existing upgrade path remains one click deeper.

### Resulting HiveMap behavior

Defense now responds with an honest building-specific status window and an `Ameliorer` route, without exposing combat, perimeter, PvP, flags, or debug behavior.

## Files Changed

- `Assets/BeeKingdom/Playground/HiveMapChampionHallBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapChampionHallBootstrap.cs.meta`
- `Assets/BeeKingdom/Playground/HiveMapBuildingUpgradeClickBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapUnsupportedBuildingBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapOverlayInputGateBootstrap.cs`
- `Docs/AI/Missions/M009-CX-HiveMap-Building-Windows-Wave-3.md`

## Existing Systems Reused

- `BuildingInteractionController.Selection.BuildingClicked`
- `BuildingTypes`, `BuildingDefinition`, and catalog/mapping ownership already used by HiveMap
- `ChampionBeeCatalog`
- `IHiveChampionBeeClient` through `MobileAccountSessionRuntimeBootstrap.ChampionBeeClient`
- `HiveViewProductUiPresenter.OpenConstructionOverlayForExternalHost` for the preserved upgrade route
- `HiveMapOverlayInputGateBootstrap` overlay gating

## Legacy Dependencies Added or Retained

Retained:

- `HiveViewProductUiPresenter.HasEnteredHiveForExternalHost` for the same HiveMap runtime gating pattern used by existing bootstraps.
- `HiveViewProductUiPresenter.OpenConstructionOverlayForExternalHost` for the existing Construction upgrade route.

Not added:

- No new LivingHive IMGUI draw bridge.
- No use of `SetChampionBeesPanelOpenForProof`.
- No dependency on LivingHive scene hotspots, fixed-image coordinates, or local preview-only champion/defense state.

## Server Authority

Champion Hall:

- Catalog display is client model-backed through `ChampionBeeCatalog`.
- Official owned/assigned/revision state is server-backed when `IHiveChampionBeeClient` is available.
- Mutating operations remain withheld from HiveMap until UX and server authority rules are explicitly defined.

Academy:

- Presentation/status only.
- No Academy-specific server controller was identified.

Defense:

- Presentation/status only.
- Combat/perimeter systems remain separate and are not exposed as Defense building behavior.

## Input/Overlay Behavior

`HiveMapOverlayInputGateBootstrap` now treats the Champion Hall window as a blocking overlay. The existing unsupported-building overlay gate continues to protect Genetics/Infirmary and now also covers Academy/Defense status windows.

## Validation

- Confirmed M008 baseline was present: latest commit was `e8e23c3 HiveMap building windows wave 2`.
- Initial working tree was clean before M009 edits.
- `dotnet restore Assembly-CSharp.csproj` succeeded.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal /clp:ErrorsOnly` succeeded after temporarily adding the new/stale-missing HiveMap bootstrap compile includes to the generated project file for validation parity, then those temporary project-file edits were removed.
- Unity batchmode was not run because the Unity editor was already open on `Environment2D5D_HiveMap_Test`.
- No authenticated Champion server session was available in this task, so the live server read path was compile-validated but not runtime-auth validated.

## Regression Checks

- Existing dedicated HiveMap windows for Nursery, production, Barrack, Research, Genetics, and Infirmary were not reworked.
- Bank and Royal Palace remain on the generic upgrade-click fallback.
- Champion Hall was removed from the generic fallback and now owns a direct read-only window plus upgrade route.
- Academy and Defense were removed from the generic fallback and now use the honest status-window pattern plus upgrade route.
- No scenes were modified.

## Discoveries

- Champion Hall is the only M009 target with a clear current reusable model/client pair.
- Academy should not be treated as Research. Research already has its own window and authority boundary.
- Defense has nearby systems, but no safe product/authority boundary currently ties them to the Defense building.
- The generated `Assembly-CSharp.csproj` was stale relative to recent HiveMap bootstrap files; Unity should regenerate it rather than committing project-file churn.

## Remaining Issues

- Champion Hall mutation actions need a separate product decision before they can be exposed safely in HiveMap.
- Academy needs a real gameplay/application contract before it should gain more than status plus upgrade.
- Defense needs an explicit supported building flow before combat/perimeter systems are connected to this node.
- Runtime click/overlay behavior should be smoke-tested in the Unity editor once orchestration allows it.

## Recommended Next Wave

1. Runtime-smoke Champion Hall in `Environment2D5D_HiveMap_Test` with and without a configured champion client.
2. Decide whether Champion Hall should expose any server mutations, then design explicit command UX with confirmation/error states.
3. Keep Academy separate from Research until an Academy-specific feature contract exists.
4. Keep Defense disconnected from combat/perimeter until product ownership is explicit.
5. Consider a small shared HiveMap IMGUI/status-window helper only if one more wave repeats the same layout logic.

## Confidence

MEDIUM-HIGH

The dependency boundaries and code integration are clear, and compile validation passed under generated-project parity. Confidence is not HIGH only because live Unity click testing and authenticated Champion server reads were not executed during this task.
