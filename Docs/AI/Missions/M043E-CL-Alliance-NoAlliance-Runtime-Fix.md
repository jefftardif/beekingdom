# M043E-CL — Alliance NoAlliance Runtime Fix

## 1. Symptom

CEO Play Mode certification test #1: Alliance Center opened but rendered the
IN_ALLIANCE shell (Chef: —, Officiers: 0, Membres: — / —, RÔLE: Membre,
"Quitter l'alliance" visible) for an account that has never created or
joined an Alliance.

## 2. Investigation path taken

Live Unity Editor inspection was attempted first (per the mission's request
to inspect the live `AllianceCenterPanelController.Model` during the CEO's
active session) but the Unity Editor MCP connection was unreachable this
turn ("Unable to connect", a different failure mode than the earlier
"unresponsive after N retries" pattern — the bridge itself, not just a busy
Editor). Diagnosis therefore proceeded through direct source inspection plus
a real, executable server-side proof of the wire contract, in the exact
order the mission specified: server truth → wire contract → controller
mapping → presenter branching.

## 3. Server truth

Not directly queryable against the CEO's live account this session (no
running server instance with the CEO's real data was available to inspect).
However, the **client-side bug fully reproduces the symptom independent of
server truth** — see section 6 — so the fix does not depend on first
proving what the CEO's actual membership row is. `AllianceService.
GetMyAlliance` (unchanged this session, already tested in
`AllianceServiceTests.cs` since M043B:
`GetMyAlliance_ReturnsNullWhenPlayerHasNoActiveMembership`) correctly
returns `null` for an account with no active membership, and
`MyAllianceOverviewResponse.From(null)` correctly maps that to
`MyAllianceOverviewResponse.None` (`HasAlliance=false, Alliance=null,
Membership=null`) — no server-side change was needed or made.

## 4. Wire contract — proven correct

New `Server/tests/BeeKingdom.Tests/MyAllianceOverviewWireContractTests.cs`
(3 tests, all passing) proves the exact JSON the server produces:

```
MyAllianceOverviewResponse.None → {"hasAlliance":false}
```

No `"alliance"` or `"membership"` key at all — `BeeJson.CreateDefaultOptions()`'s
`DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull` omits null
properties entirely rather than writing explicit `null` values. A round-trip
test confirms deserializing that exact JSON back through the server's own
record type never produces a non-null-but-empty `Alliance`/`Membership` —
disproving the "another M041-class wrapper-shape bug" hypothesis for this
specific endpoint. A second test proves the `HasAlliance=true` shape nests
real camelCase `alliance`/`membership` objects correctly.

A new Unity-side test,
`AllianceClientTests.RealJsonCodec_NoAllianceServerResponse_
DeserializesToHasAllianceFalseWithNullNestedObjects`, feeds the **exact**
proven server JSON string (`{"hasAlliance":false}`) through the **real**
`SystemTextGameJsonCodec` (not the `TypeCapturingTransport` mock used
everywhere else in that file, which hands back a pre-built C# object and
never actually touches JSON — it could not have caught a real wire-shape
bug). Result: `HasAlliance=false`, `Alliance=null`, `Membership=null` — the
Unity DTO layer is also proven correct. A companion test proves the
`HasAlliance=true` shape parses correctly too. **The wire contract, both
server- and client-side, is not the bug.**

## 5. Controller state mapping — inspected, proven correct

`AllianceCenterPanelController.RefreshCoreAsync` (unchanged this session):

```csharp
if (overview == null || !overview.HasAlliance || overview.Alliance == null || overview.Membership == null)
{
    ... Model = AllianceCenterPresentation.NoAlliance(...); return;
}
```

This check is complete and correct — any of the four conditions (including
a structurally-empty-but-non-null `Alliance`, which section 4 proves cannot
happen from a real server response, but the check guards it anyway) routes
to `NoAlliance`. **The controller's own state mapping is not the bug
either.**

## 6. Root cause — proven, in the presenter

`HiveViewProductUiPresenter.DrawAllianceHeadquartersScreen`'s branching
logic (unchanged since M043, this is the actual defect):

```csharp
if (allianceModel != null && allianceModel.State == AllianceCenterScreenState.NoAlliance)
{ DrawAllianceNoAllianceScreen(compact); return; }
if (allianceModel != null && (allianceModel.State == Loading || allianceModel.State == NotConfigured) && !allianceModel.HasAlliance)
{ DrawAllianceLoadingScreen(compact); return; }
// falls through into the full IN_ALLIANCE shell for EVERYTHING ELSE
```

This only special-cases exactly two conditions. **Any other state —
`Error`, `Mutating`, or even `Loading`/`NotConfigured` when `HasAlliance`
happens to be `true` from stale prior data — falls straight through into
the full IN_ALLIANCE draw path**, which then reads
`allianceCenterController.Model?.Overview?.Xxx ?? "—"` throughout (the
M043 "never fabricate, show — instead" convention) — this is exactly why
every field showed `"—"` instead of crashing: the null-safe fallback code
built in M043 was silently masking a genuine non-Ready state as if it were
a real, empty alliance shell, instead of that state never reaching this
screen at all.

**Confirmed contributing factor, proven not the primary cause but real**:
`SyncAllianceRuntimeStateFromController()`'s early-return branch (when
`!model.HasAlliance`) cleared the member roster and activity feed but never
reset `alliancePlayerRole`, which stays at its static-field initializer
default, `"membre"`, on any session where a valid alliance was never
successfully loaded — this explains the specific `"RÔLE : Membre"` text
observed (not a "default `AllianceRole.Member` enum value 0 masquerading as
real data" bug — `RemoteAllianceRole.Member` is never read as a role source
in the no-overview path at all; it's a plain leftover string default).
**Disproven**: no default/uninitialized `RemoteAllianceRole` enum from a DTO
is ever the source of this behavior — the string was never touched, not
defaulted from a zero-value enum.

The most likely trigger for the CEO's specific session: the very first
`Refresh()` call (from `EnsureHiveThenRefreshGameplayState` on entering the
hive) hit a transient failure — a real network hiccup, or a
timing race where the session token wasn't fully ready at that exact
instant — before ever completing a first successful `NoAlliance` or `Ready`
resolution, landing on `State = Error` with `Overview = null` (the
constructor's initial `Loading(null)` carried forward). That `Error` state
was never one of the two special-cased conditions above, so it fell through
into the shell.

## 7. Fix

Two changes to `HiveViewProductUiPresenter.cs`, both minimal and targeted:

1. **`DrawAllianceHeadquartersScreen`** rewritten to explicit per-state
   branching exactly matching the mission's required invariant table:
   - `NoAlliance` → `DrawAllianceNoAllianceScreen`.
   - `Ready` → shell (guaranteed valid `Overview` by construction —
     `AllianceCenterPresentation.Ready(...)` never produces a null one).
   - `Mutating`/`Error` **with** a structurally valid prior `Overview`
     (real `AllianceId`) → shell, retry-in-place (correct UX — e.g. a Kick
     mutation in flight, or a transient refresh failure on an alliance
     already successfully loaded once).
   - `Mutating`/`Error` **without** a valid prior `Overview` →
     `DrawAllianceNoAllianceScreen` (whose own `State == Error` branch
     already renders the real `ErrorCode` with a Retry button — it was
     already correct, just unreachable for this exact case before this
     fix).
   - `Loading`/`NotConfigured` with a valid prior `Overview` (a quiet
     background refresh) → keep showing the shell; without one → 
     `DrawAllianceLoadingScreen`.
   - A structurally valid `Overview` is defined once, explicitly:
     `Overview != null && Overview.AllianceId != Guid.Empty` — never
     `State != NoAlliance` as a stand-in for "safe to render".
2. **`SyncAllianceRuntimeStateFromController`**'s no-overview early return
   now also resets `alliancePlayerRole = string.Empty` (previously left
   untouched) — defense in depth; every existing switch on this string
   already treats an unrecognized value as "no elevated role", so this is
   a pure hygiene fix, not new behavior to test.

No new Alliance functionality was added. No server code was changed (the
wire contract was proven correct, not touched).

## 8. Tests

**Server** (new, `MyAllianceOverviewWireContractTests.cs`, 3/3 passing):
exact JSON shape for `HasAlliance=false` (no nested keys at all),
exact nested shape for `HasAlliance=true`, and a round-trip proving no
empty-but-non-null objects ever result.

**Unity** (extended `AllianceClientTests.cs`, 2 new tests): real
`SystemTextGameJsonCodec` deserialization of the exact proven server JSON
for both the no-alliance and has-alliance cases — the only tests in this
file (or the project) that exercise the actual JSON layer for this endpoint
rather than a type-only mock.

**Controller/presenter state-machine tests were not added** —
`AllianceCenterPanelController` and `HiveViewProductUiPresenter` both live
in the implicit default `Assembly-CSharp` (no `.asmdef`), and
`Assets/BeeKingdom/Tests/` compiles under its own `BeeKingdom.Tests.asmdef`,
which structurally cannot reference the default assembly (a hard Unity
constraint, already documented in this exact project — see
`Tests/Editor/Interaction/LivingHiveResearchBridgeTests.cs`'s
`LivingHiveMenuAssemblyNeverReferencesTheDefaultPlaygroundAssembly`, and
re-confirmed in M043B/M043C for this exact pair of classes). This is why
the fix in section 7 could only be verified by direct source proof (traced
every branch by hand against the mission's required state table) plus the
wire-contract tests above, not by a controller-level or presenter-level
automated test.

**Unity compile/test execution status**: the Unity Editor MCP connection
was unreachable for the entirety of this mission ("Unable to connect",
retried twice) — **the fix has not been compile-confirmed or test-run live
this session.** This mirrors the exact situation from the tail end of
M043B, which was later confirmed clean once Jeff reopened the Editor. The
same is expected here but is **not yet proven** — this is the one honest
gap in this report.

**Server**: `dotnet test` — **456/456 passed, 0 failed, 8 skipped, 464
total** (461 from M043C + 3 new wire-contract tests). No flaky failure
observed this run.

## 9. Live retest preparation

No Alliance was created for the CEO's account. If server truth is genuinely
"no active membership," that remains true after this fix — nothing in this
change touches account or membership data, only how the client renders
whatever state it receives.

**Before the CEO retests**: Unity must be reopened/reconnected and a clean
`assets-refresh` + the two new/extended test files run, per this project's
own standing rule, before trusting this fix in Play Mode. That confirmation
was not obtainable this session (section 8).

**Expected retest**: CEO closes Alliance Center, reopens it → NO_ALLIANCE
screen (RECHERCHER / CRÉER / INVITATIONS), never the shell with dashes.

## 10. Final verdict

- A. Server membership truth identified? **NO** — not queried against the
  CEO's live account (no reachable running instance with that data this
  session); not required for the fix, which is provably correct for any
  server truth (see section 6).
- B. Exact failing layer identified? **YES** — presenter state branching
  (`DrawAllianceHeadquartersScreen`), not server, not wire contract, not
  controller.
- C. Root cause proven? **YES** — by direct source trace, not guessed;
  wire contract and controller mapping independently proven correct first
  (sections 4–5), isolating the defect to the one remaining layer.
- D. NoAlliance mapping corrected? **YES** — explicit per-state branching
  replacing the old two-condition special-case logic.
- E. Default Member enum cannot fake membership? **YES, disproven as the
  cause** — `RemoteAllianceRole.Member`/enum value 0 was never the source;
  the stale `alliancePlayerRole` string default was, and is now reset
  explicitly (section 7, item 2).
- F. Exact wire response regression tested? **YES** — both server-side
  (exact JSON string assertion) and Unity-side (real codec against that
  exact string).
- G. Unity compile clean? **NOT CONFIRMED THIS SESSION** — Editor
  unreachable; no reason to expect a problem (the change is small and
  mirrors existing patterns exactly), but this is not proof.
- H. Targeted tests green? **Server: YES (3/3 new, 456/456 total). Unity:
  NOT RUN THIS SESSION** (Editor unreachable).
- I. READY FOR CEO RETEST #1? **NOT YET** — pending the Unity
  compile/test confirmation in section 8/G/H, which requires Jeff to have
  Unity open and reachable. Once that single confirmation lands (same
  pattern as M043B → M043C), this fix should be ready for the CEO's
  NO_ALLIANCE retest described in section 9.

## 11. M043F-CL — compile/test confirmation (addendum)

Unity reconnected. `assets-refresh` → clean, zero `error CS` (two passes,
before and after the fixes below). `AllianceClientTests`: **12/12 PASS**,
including both new M043E JSON tests
(`RealJsonCodec_NoAllianceServerResponse_...`,
`RealJsonCodec_HasAllianceResponse_...`). `PlayerDirectoryClientTests`:
**7/7 PASS**.

`HiveMapBuildSettingsRegressionTests` initially **failed 3/5** —
`ProjectSettings/EditorBuildSettings.asset`'s live in-memory state had
drifted again: `LivingHive.unity` was back as the first enabled scene. This
is the exact same self-perpetuating mechanism proven in
`M043D-CL-LivingHive-Runtime-Regression.md` (`PlaygroundPlayModeStartScene.
EnsureSceneEnabled`, `[InitializeOnLoad]`, fires on every domain reload) —
reopening Unity with `LivingHive.unity` as the active scene retriggered it.
Not caused by M043E or this mission's own changes. Restored using the
identical, already-approved M043D procedure (push the known-good scene list
into `EditorBuildSettings.scenes` live, then `git checkout HEAD --
ProjectSettings/EditorBuildSettings.asset` to sync the on-disk file) — no
new functionality, a re-application of an established fix. Re-ran: **5/5
PASS**, confirmed stable across a subsequent `assets-refresh`.

### M043F final verdict

- A. Unity compile clean? **YES**
- B. M043E JSON tests pass? **YES** (2/2)
- C. AllianceClientTests pass? **YES** (12/12)
- D. PlayerDirectoryClientTests pass? **YES** (7/7)
- E. HiveMap routing regression tests pass? **YES** (5/5, after re-applying the M043D fix for a recurrence unrelated to M043E)
- F. READY FOR CEO NO_ALLIANCE RETEST? **YES**

**READY FOR CEO NO_ALLIANCE RETEST.**
