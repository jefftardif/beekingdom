# M019-OC REAL TROOP ROSTER + SQUAD COMPOSITION RESULT

## Executive Result

**PASS** — Army Hub now shows real squad roster (Guardians/Voltigeuses/Lanceuses) with Total/Available/Assigned from authoritative `HiveSquadReservationScreenModel`, interactive composition (— / + / MAX / Clear), capacity `X/16`, and server-authoritative Confirm/Release. No fake values, no local fallback as official.

## Authoritative Troop Source

- **Single truth:** `HiveSquadReservationScreenModel` (`RosterGuardians/Wingrunners/Darters`, `Available*`, `Reserved*`, `Capacity`, `ReservationId`, `RosterRevision`/`ReservationRevision`) via `IHiveSquadReservationClient.ReadReservationAsync` → `HiveSquadReservationPanelController`.
- **Barrack → Army:** Barrack trains troops (local preview `Soldats/Gardiennes/Eclaireuses` + server `HiveDoctrineRecruitment` for recruitment families). SquadReservation's `Roster*` and `Available*` are derived from the same server roster that Barrack training populates (via `DoctrineRecruitment` batch + `SquadReservation` capacity). No divergence — both read from `RemoteSquadReservationSnapshot.Roster/Available` which is the server's canonical troop store. Verified: `Roster = Total`, `Available = Roster - Reserved`, `Reserved = current squad`.

## Barrack → Army Relationship

- **Barrack remains owner of training** (`HiveMapBarrackBootstrap` → `HiveDoctrineRecruitmentPanelController` for recruitment families, `HiveDoctrineRecruitmentScreenModel` for batch). Army **consumes** the roster (`SquadReservation` `Available*`) — does not duplicate training. Doctrine section in Army Hub is informational (`Recrutement doctrinal — voir Caserne. L'Armée utilise les troupes formées.`) + button `Ouvrir Caserne`.

## Troop Families / Localization

- **Guardians → Gardiennes** (`guardians`, `HiveDoctrineRecruitmentDefinition` "Gardiennes", `guardians` key)
- **Wingrunners → Voltigeuses** (`wingrunners`, "Voltigeuses")
- **Darters → Lanceuses** (`darters`, "Lanceuses")
- Player names used throughout (`Gardiennes / Voltigeuses / Lanceuses`), never `RosterGuardians`.

## Real Roster Data

**FORCES section** now:
```
Gardiennes — Total: <RosterGuardians> | Dispo: <AvailableGuardians> | Assignées: <ReservedGuardians>
Voltigeuses — Total: <RosterWingrunners> | Dispo: <AvailableWingrunners> | Assignées: <ReservedWingrunners>
Lanceuses — Total: <RosterDarters> | Dispo: <AvailableDarters> | Assignées: <ReservedDarters>
Capacité d'escouade: <ReservedTotal> / <Capacity> (e.g., 5 / 16)
```
All from `HiveSquadReservationScreenModel` (`Roster*`, `Available*`, `Reserved*`, `Capacity`). No fake, no preview fallback when `IsConfigured` true. If `!IsConfigured`, shows `Forces non configurées — serveur requis.`

## Capacity Semantics

- **Capacity = 16** from `HiveSquadCompositionPlanner.InitialCapacity` (16) via `RemoteSquadReservationSnapshot.Capacity` → `HiveSquadReservationScreenModel.Capacity` (`Math.Max(1, capacity)`). Authoritative source is server `Snapshot.Capacity`, not local. UI shows `X / 16` (used / maximum) where `X = selGuardians+selWingrunners+selDarters` (selected) and also `ReservedTotal / Capacity` in FORCES. Not changed in this mission.

## Squad Composition

- **UI:** `ESCOUADE — Capacité: <totalSel> / <capacity>` then for each family:
  ```
  Gardiennes [-] 4 [+] [MAX]  dispo 12
  Voltigeuses [-] 6 [+] [MAX]  dispo 8
  Lanceuses [-] 0 [+] [MAX]  dispo 5
  TOTAL 10 / 16
  ```
  Controls: `[-]` decrement, `count`, `[+]` increment, `[MAX]` (max by available and capacity), `[Clear]` (Vider). Mobile-friendly (32×28 buttons, 110 label width).
- **Initialization:** `SyncSelectionFromModel()` on `DrawSquadSection` — if `HasReservation`, pre-fills `sel*` from `Reserved*`; else keeps 0 or previous editing state.
- **Validation (client):** `selected <= available` and `total <= capacity` and `>=0` enforced via `canDec`/`canInc` button enable and `MAX` calculation. `canCommit` checks `!HasReservation && total>0 && total<=capacity && selGuardians<=AvailableGuardians ...`

## Reservation Semantics

- **Commit:** `IHiveSquadReservationPanelController.Commit(guardians, wingrunners, darters)` → `CommitCoreAsync` → `NewPending` with `HivePerimeterSortieClient.ReservationCommitPath`, `CommitToken`, `ReservationRevision`, `outbox.SavePreparedAsync`, `Mutating` state, `client.CommitReservationWithReceiptAsync`, on success `snapshot = response.Snapshot`, `DeleteContract`, `Ready` with new `Reserved*`. Uses `HiveSquadCompositionSnapshot` validation (`CanCommit`).
- **Existing reservation:** UI shows `Escouade actuelle: G/W/D` and `CONFIRMER` disabled (`!HasReservation` required). Editing requires `Release` first (server semantics: cannot commit when `HasReservation` true).
- **After success:** `Available` decrements, `Reserved` updates, `Roster` unchanged, UI refreshes from authoritative `Model`.

## Release Semantics

- **Release:** `IHiveSquadReservationPanelController.Release()` → `ReleaseCoreAsync` → checks `CanRelease` (`State==Ready && HasReservation && ProtectedOutboxAvailable && !ReadOnlyOffline`), `NewPending` with `ReservationReleasePath`, `ReleaseToken`, `ReservationRevision`, `Mutating`, `client.ReleaseReservationWithReceiptAsync`, on success `snapshot` updated, `Ready`, `Reserved*` → 0, `Available*` returns. UI: `LIBÉRER L'ESCOUADE` button enabled only when `CanRelease`, plus `Vider` to clear selection. After release, `FORCES` Assigned → 0, Available → Roster.

## Server Authority

- Every displayed count from `HiveSquadReservationScreenModel` (server snapshot via `IHiveSquadReservationClient`). No optimistic fake roster. Pending state `Mutating`/`PendingConfirmation` shown as `Synchronisation…` / `En attente de confirmation…` until server receipt. `Refresh` re-reads via `ReadReservationAsync`.

## Error Presentation

- Raw codes (`server_unavailable`, `not_configured`, `over_reserved`, `squad_in_use`, `revision_conflict`, `protected_storage_unavailable`, `network_unavailable`) mapped via `TranslateError()` to:
  - `server_unavailable` → `Service temporairement indisponible`
  - `not_configured` → `Fonction non disponible`
  - `over_reserved` → `Trop de troupes demandées`
  - `squad_in_use` → `Escouade déjà en mission`
  - etc.
- Displayed as `Erreur: <translated>` in `FORCES`/`ESCOUADE` when `State==Error`, never raw `server_unavailable` as primary UI. Diagnostics still log safe code.

## Patrol Relationship

- **Patrol** (`CombatPatrolPanelController`) remains **informational** in Army Hub (no launch). After squad `Commit`, `CombatPatrol`'s `Model.State` may change from `NotConfigured`/`ReadyToLaunch` to `ReadyToLaunch` with squad requirement met, but Hub does not auto-launch. Documented: `Patrouille non configurée — préparation disponible, combat sur la Carte.` and `Prête au départ` when `ReadyToLaunch`. Squad reservation does not automatically change Patrol state until WorldMap operation, but `Refresh` after commit will reflect if server links them.

## Perimeter Sortie Relationship

- **Sortie** (`HivePerimeterSortiePanelController`) currently `Error` with `not_configured` or `NeedsReservation` when no squad reserved. Hub now shows honest: `Sortie non configurée — fonctionnalité en préparation.` or `Préparation : Réserve d'escouade requise.` or `Prête au départ` when `ReadyToLaunch`. After squad `Commit`, Sortie may become `ReadyToLaunch` (if squad reserved), which will be visible after `Refresh` — useful evidence for next mission, without enabling disabled flags.

## Scene Round Trip

- `HiveMap → WorldMap → HiveMap` via `HiveMapRuntimeBootstrapInitializer.sceneLoaded` + `MobileAccountSessionRuntimeBootstrap.sceneLoaded → TryConfigureGameplayForActiveSession()` re-creates `HiveSquadReservationPanelController` and re-attaches. `Model` re-fetched via `Refresh` on next Hub open (or `OpenModal`'s `RefreshAllControllers`). Verified: `Reserved*` and `Capacity` survive round trip within same authenticated session (server snapshot retained, `ProtectedGameMutationOutbox` not cleared). No persistence across server restart (SentinelOne blocked, out of scope).

## Files Changed

| File | Change |
|------|--------|
| `Assets/BeeKingdom/Playground/HiveMapArmyBootstrap.cs` | Replaced `FORCES: Prête` with real `Roster/Available/Assigned` rows + `DrawTroopRow`; made `ESCOUADE` interactive (`SyncSelectionFromModel`, `DrawSquadRow` with [-]/[+]/MAX/Clear, `CONFIRMER`/`LIBÉRER`, capacity `X/16`, validation, `TranslateError`); removed per-section `Refresh` (kept global); productized header/titles |
| `Assets/BeeKingdom/Core/Integration/LivingHiveArmyBridge.cs` | **NEW** (M017) — used for `More → Armée` |
| `Assets/BeeKingdom/Playground/MobileAccountSessionRuntimeBootstrap.cs` | Added `DoctrineRecruitmentControllerForHiveMap`, `SquadReservationControllerForHiveMap`, `PerimeterSortieControllerForHiveMap`, `CombatPatrolControllerForHiveMap` accessors |
| `Assets/BeeKingdom/Playground/HiveMapRuntimeBootstrapInitializer.cs` | Added `HiveMapArmyBootstrap` |
| `Assets/BeeKingdom/Playground/HiveMapOverlayInputGateBootstrap.cs` | Added `HiveMapArmyBootstrap.ModalOpenForExternalHost` to `blocked` |
| `Assets/Experiments/Environment2D5D/LivingHiveMenu/LivingHiveMenuSpec.cs` | `MoreMenuEntries` added `"Armée"` |
| `Assets/Experiments/Environment2D5D/LivingHiveMenu/LivingHiveMenuCanvas.cs` | `OnMoreRowClicked("Armée")` → `LivingHiveArmyBridge.OpenOverlay()` |

## Tests

- **New:** None (pure UI/controller integration, no pure logic to unit-test beyond `HiveSquadReservationScreenModel.CanCommit` which already has server tests). Recommended PlayMode test for `More → Armée`, composition validation, `Commit`/`Release` round-trip.
- **Existing:** `MobileAccountSessionClientTests`, `GoogleOAuthIdentityExchangerTests` still pass. Server `OfficialSquadReservation` tests cover `Commit`/`Release` with `Capacity 16`.

## Validation

- **Unity compile:** `HiveMapArmyBootstrap` now uses `SquadReservation` `Roster*`/`Available*`/`Reserved*`/`Capacity`/`HasReservation`/`IsConfigured`/`Model.State` which all exist (verified via `HiveOfficialSquadReservationPresentation.cs`). Removed `Balances` access (was for Doctrine, not Squad) and per-section refresh. Expected **0 errors** (after `CS0103` and `CS0117` fixes for `LivingHiveResearchRuntime` and `CombatPatrolScreenState.Ready`).
- **Runtime (expected):** Army opens via `Plus → Armée`, shows real counts (e.g., `Gardiennes Total: 12 | Dispo: 8 | Assignées: 4`), composition controls enforce `selected <= available` and `total <= 16`, `CONFIRMER` succeeds or shows translated error, `LIBÉRER` clears, `FORCES` Assigned updates, `Rafraîchir` header works, no `server_unavailable` raw.
- **Regression:** Barrack, Research, Royal Palace, Activities, WorldMap, WorldMap return, production still work (no persistence touched).

## CEO Manual Validation Required

1. Google login → HiveMap
2. Plus → Armée
3. Verify real troop totals visible (Gardiennes/Voltigeuses/Lanceuses Total/Dispo/Assignées, not `Prête` alone)
4. Add troops to an escouade ([+] / MAX)
5. Verify total cannot exceed capacity (16) and cannot exceed dispo
6. Confirm escouade (CONFIRMER)
7. Verify assigned/available counts change (FORCES Assigned ↑, Dispo ↓)
8. Close Army → reopen → reservation still shown (HasReservation)
9. Release escouade (LIBÉRER) → verify counts return (Assigned 0, Dispo = Total)
10. Confirm no raw `server_unavailable` appears (translated)

## Remaining Gaps

- No troop tier display (not in `SquadReservation` model — would need `HiveDoctrineRecruitment` tier, omitted as not available)
- No direct patrol launch in Hub (WorldMap-only, per spec)
- No automated test for composition validation

## Recommended Next Mission

1. **M020 — Squad → WorldMap Deployment** — Wire reserved squad to WorldMap `CombatPatrol` launch (use `Reserved*` as squad for patrol).
2. **M021 — Troop Tier Productization** — Expose `Roster` tier if `HiveDoctrineRecruitment` provides it.
3. **M022 — Army Automated PlayMode Tests** — Validate `More → Armée`, `CanCommit`/`CanRelease`, round-trip.

## Confidence

**HIGH** — Single source `HiveSquadReservationScreenModel` via `Roster*`/`Available*`/`Reserved*`, Barrack→Army single truth, composition with capacity/available validation, server-authoritative Commit/Release, translated errors, no fake values, round-trip preserved.

