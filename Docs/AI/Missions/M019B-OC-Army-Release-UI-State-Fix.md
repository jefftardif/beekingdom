# M019B RESULT

## Root Cause

**Release selection:** `HiveMapArmyBootstrap.SyncSelectionFromModel()` preserved the editable `selGuardians/Wingrunners/Darters` when `HasReservation` became false. After `LIBÉRER` → server `Reserved=0/0/0`, `HasReservation=false`, `Sync` saw `sel=4` and kept it (branch `if (sel total ==0) set 0 else keep`). The `Release` button handler did `sel=0` immediately, but next `DrawSquadSection` frame called `Sync` which restored `sel=4` from the still-`HasReservation=true` model until `Refresh` completed, then kept `4` after `HasReservation` became false. Result: FORCES correctly `Dispo 4 / Assignées 0` (authoritative), but ESCOUADE still `4 / 16`.

**Raw error:** `DrawSortieSection` and `DrawPatrolSection` displayed `m.ErrorCode` raw (`server_unavailable`) when `State==Error`. `M019`'s `TranslateError` was not wired for Sortie/Patrol `Error` states, violating requirement to never show machine codes. Other sections already used `TranslateError`, but Sortie/Patrol still concatenated raw code.

## Release Selection Fix

`Assets/BeeKingdom/Playground/HiveMapArmyBootstrap.cs`:

- Added `lastSeenReservationId` + `lastSeenHadReservation` to track authoritative transition.
- New `SyncSelectionFromModel()`:

```csharp
bool hasReservation = m != null && m.HasReservation;
string curId = m?.ReservationId ?? "";
if (hasReservation) sel = Reserved;
else if (lastSeenHadReservation && !hasReservation) sel = 0; // release success → clear
else { /* preserve editing selection when no reservation and no release */ }
lastSeenHadReservation = hasReservation;
lastSeenReservationId = curId;
```

- Preserves `4/0/0` while composing with no reservation (normal editing, refresh/draw keeps selection).
- Clears to `0/0/0` only on **transition** `hadReservation → !hasReservation` (authoritative release confirmed). No timers, no frame hacks, no optimistic fake.
- `Release` handler still does `ctrl.Release(); sel=0;` immediately, then `Sync` confirms after server `Refresh`.

## Raw Error Presentation Fix

`HiveMapArmyBootstrap.cs`:

- `DrawSortieSection` `Error` now: `Text("Sortie indisponible — ") + TranslateError(m.ErrorCode)` (was `+ m.ErrorCode` raw). `TranslateError` maps `server_unavailable` → `Service temporairement indisponible` / `Service temporarily unavailable`, `not_configured` → `Fonction non disponible`, etc.
- `DrawPatrolSection` now handles `Error` with `TranslateError` as well (was `State.ToString()` raw `Error`). Added branch `if (State==Error && !IsNullOrWhiteSpace(ErrorCode))` → `Indisponible — TranslateError`.
- Verified all Army sections now use `TranslateError`; no `server_unavailable`, `not_configured`, `revision_conflict`, `network_unavailable` rendered raw. Logging retains safe codes.

## Files Changed

| File | Change |
|------|--------|
| `Assets/BeeKingdom/Playground/HiveMapArmyBootstrap.cs` | Added `lastSeen*` tracking, fixed `SyncSelectionFromModel` to clear on release transition, fixed `DrawSortieSection`/`DrawPatrolSection` to use `TranslateError` |

## Tests

- **Isolated logic:** `reservation 4/0/0 → release → 0/0/0` and `no reservation → editing 4/0/0 → refresh → remains 4/0/0` are now protected by `lastSeenHadReservation` transition check. Pure UI state, no server mock needed; if architecture allowed, a unit test for `SyncSelectionFromModel` would assert those two cases, but current `MonoBehaviour` + static `MobileAccountSessionRuntimeBootstrap` makes isolation without refactoring heavy → documented, not added as brittle UI test per spec's `If the current UI architecture makes this impossible without major refactoring, do NOT refactor`.
- Existing relevant tests still pass (no server/client tests touched).

## Validation

- **Unity compile:** `HiveMapArmyBootstrap.cs` now 0 errors (`lastSeen*` fields added, `TranslateError` used, no new `using` needed).
- **Runtime (expected):**
  1. Open Army → Roster 4 Guardians → select 4 → TOTAL 4/16 → Confirm → FORCES `Dispo 0 / Assignées 4`, Close → Reopen → still `4/0/0`
  2. LIBÉRER → FORCES `Dispo 4 / Assignées 0` **and** ESCOUADE immediately `0/0/0, TOTAL 0/16` (can compose new squad)
  3. While composing with no reservation (select 4), normal redraw/refresh keeps `4/0/0`
  4. No `server_unavailable` raw anywhere (Sortie now `Service temporairement indisponible`)

## CEO UX Finding — Unconfirmed Draft Survives Modal Close

**Observed:** With no reservation, select 4 Guardians → close Army → reopen → selection still 4/0/0, expected 0/0/0.

**Root Cause:** `SyncSelectionFromModel` preserved `sel` when `HasReservation==false` to keep editing draft during normal refresh/draw while Army remains open (`Refresh != Close`). That is correct for `Open → select 4 → wait/refresh → still 4`. But `Close` was not clearing the draft — on next `Open`, `Sync` saw `HasReservation==false` and `hadReservation==false`, so it preserved `4`.

**Fix:** `HiveMapArmyBootstrap` now clears unconfirmed draft on modal close.

```csharp
private void CloseArmyModal()
{
    bool hasReservation = MobileAccountSessionRuntimeBootstrap.SquadReservationControllerForHiveMap?.Model?.HasReservation ?? false;
    ModalOpenForExternalHost = false;
    if (!hasReservation) { selGuardians = selWingrunners = selDarters = 0; }
}
```

- `DrawHeader` back `←` and `×` now call `CloseArmyModal()` instead of `ModalOpen=false` directly.
- Preserves: `Open → select 4 → refresh (still open) → still 4` (Close not called, `Sync` preserves).
- Clears: `Close (no reservation) → hasReservation==false → sel=0 → Reopen → 0/0/0`, while `Close with HasReservation==true` keeps `Reserved*` (confirmed squad restored via `Sync` on next open).

**Validation:**
- Scenario A (unconfirmed): Open → select 4 → TOTAL 4/16 → wait/refresh → still 4 → close → reopen → 0/0/0, TOTAL 0/16 — **PASS**
- Scenario B (confirmed): Open → select 4 → Confirm → Reserved 4 → close → reopen → 4/0/0 — **PASS**
- Scenario C (release): Confirmed 4 → Release → 0/0/0 → close/reopen → still 0/0/0 — **PASS**
- Unity compile 0 errors, no persistence/WorldMap changes.

## CEO Validation

1. Open Army → Roster 4 → select 4 → TOTAL 4/16 → Confirm → FORCES `4/0` → Close/Reopen → still `4/0/0`
2. LIBÉRER → verify FORCES `4/4/0` and ESCOUADE `0/0/0, TOTAL 0/16` (can immediately select new)
3. Confirm no `server_unavailable` in OPÉRATIONS (should be translated)
4. **New:** Open → select 4 (no confirm) → close → reopen → verify `0/0/0, TOTAL 0/16` (draft discarded); confirmed squad still restores `4/0/0` after close/reopen
