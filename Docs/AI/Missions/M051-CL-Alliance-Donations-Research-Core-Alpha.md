# M051-CL — Alliance Donations + Alliance Research Core Alpha

Server-authoritative collective Alliance progression: a compact 9-technology,
3-branch Alpha tree, owned by the Alliance (not any one member), funded by
real player resource donations, with real gameplay bonuses.

## 1. Server model

New `Server/src/BeeKingdom.Alliance/Research/`:

- `AllianceResearchCatalog.cs` — static Alpha content (9 technologies, stable
  IDs, never localized strings as identity): **Prospérité**
  (`prosperity_shared_reserves_i/ii`, `prosperity_colony_logistics` — real
  resource-production % bonus), **Coopération**
  (`cooperation_coordinated_aid_i/ii`, `cooperation_collective_mobilization`
  — real resource-storage-capacity % bonus), **Défense Royale**
  (`defense_common_discipline_i/ii`, `defense_royal_guard` — real
  combat-power % bonus). Each tier requires the previous one in its branch.
  Donation cost/progress follows the mission's own example exactly: 100
  Honey / 50 Pollen / 25 Wax → +10 progress (tier 3 costs scale up modestly:
  150/200 honey-equivalent bundles).
- `AllianceResearchModels.cs` / `AllianceResearchOptions.cs` — `AllianceResearchState`
  is the Alliance-owned aggregate (`Dictionary<TechnologyId, Progress>` +
  `Dictionary<PlayerId, Contribution>` + an idempotency-id set), never
  duplicated into any member's own `PlayerHiveState`.
- `IAllianceResearchRepository` / `InMemoryAllianceResearchRepository` /
  `SqlAllianceResearchRepository` — one JSON-blob row per Alliance, same
  proven shape as `dbo.HivePlayerStates`/`SqlHiveStateRepository`, mutated
  under a real exclusive `sys.sp_getapplock` per AllianceId (SQL) / a
  per-alliance semaphore (InMemory) - concurrent donations to the same
  Alliance always serialize, never race.
- `AllianceResearchService.cs` — `GetSnapshotAsync` (member-only read) and
  `DonateAsync` (see section 3 for the atomicity strategy).
- `AllianceResearchBonusResolver.cs` — resolves a player's *current*
  membership's completed technologies into an aggregated bonus, fresh every
  call, never cached/baked.
- `AllianceGameplayBonus.cs` (new, in `BeeKingdom.HiveOperations`) +
  `AllianceGameplayBonusResolverAdapter.cs` (in `BeeKingdom.Alliance.Research`)
  — a tiny port/adapter pair so `HiveOperations` (production, combat) never
  has to depend on `Alliance` (which already depends on `HiveOperations` -
  the reverse would be circular). `HiveOperations` only knows "some resolver
  can hand me a player's bonus bps"; only the adapter knows Alliance
  Research exists.

Endpoints (`Server/src/BeeKingdom.Server/Program.cs`), same auth/error
convention as every other Alliance endpoint:
`GET /alliance/v1/research`, `POST /alliance/v1/research/{technologyId}/donate`.

## 2. Real bonus integration - one clean point per system, not duplicated

- **Production % + Capacity %** (Prospérité/Coopération) → `HiveOfflineProductionService.EffectiveRate`/
  `EffectiveCapacity` gained an additional `long` bps parameter, resolved
  once per request via the new optional `IAllianceGameplayBonusResolver`
  constructor dependency, merged into the *existing* bps aggregation
  (alongside personal-Research and Strategic Path bonuses that already
  worked this way) - no formula duplicated, no new calculation path.
- **Combat power %** (Défense Royale) → `CombatPatrolService`'s existing
  `MergedPowerBonus(...)` merge (already combining Champion Bee/Troop
  Tier/Strategic Path bonuses at its 3 real call sites: preview, launch,
  claim/recall resolution) gained a 4th source, broadcast uniformly across
  all 3 troop families (none of the Alpha technologies specialize by
  family).
- Both integrations are via an **optional** constructor parameter
  (`= null`), so every existing caller/test that doesn't know about
  Alliance keeps compiling and behaving exactly as before
  (`AllianceGameplayBonus.None`). Production DI (`Program.cs`) wires the
  real resolver into both services.

## 3. Donation atomicity - the real, documented compromise

A donation spans two aggregates with no shared transaction in this
codebase's architecture (the donating player's own `PlayerHiveState` and
the Alliance's own `AllianceResearchState`). This mirrors exactly the
compromise `AllianceHelpService` already established for its own
two-aggregate contribute flow (see that service's own class comment):

1. **Resources debited first**, atomically on the player's own hive state,
   idempotent via `PlayerHiveState.Receipts` - the same mechanism every
   other paid action in this codebase already uses (Champion Bee
   level-ups, etc.). Insufficient resources → rejected here, nothing else
   happens.
2. **Alliance progress + contribution applied second**, atomically on the
   Alliance's own state, independently idempotent via its own
   `ProcessedDonationIds` set (guards a retry of *this* step alone).

If a technology completes via a concurrent donation between this player's
pre-check and their own atomic step 2, their contribution total **still
increases** (their resources really were spent for something real) even
though that specific technology's progress can no longer advance past its
requirement - there is no code path where resources vanish with literally
nothing recorded for the player. This is documented, not silent.

Concurrent donations to the *same* technology are additionally guaranteed
correct at the storage layer by the exclusive per-Alliance lock (section
1) - two `+10` donations landing "at nearly the same time" (the mission's
own example) both apply; neither is lost.

## 4. Client (Unity)

- `AllianceClient.cs` — `RemoteAllianceTechnology`/`RemoteAllianceResearchSnapshot`/
  `RemoteAllianceResearchDonateResult` DTOs (field-for-field mirror of the
  server contract) + `GetAllianceResearchAsync`/`DonateToAllianceResearchAsync`.
- `AllianceCenterPresentation.cs` — `AllianceResearchScreenModel` (one
  shared model for the whole tab, `AllianceResearchDonationState` as the
  **single** in-flight guard the mission asked for: only one donation may
  be in flight at a time across the whole tab, not per-technology) +
  `RefreshResearch()`/`DonateToResearch()` on `AllianceCenterPanelController`,
  same throttled-refresh/fire-and-forget/error-surfacing conventions as the
  existing Alliance Help entry points.
- `HiveViewProductUiPresenter.cs` — the Alliance Center's "Recherches" tab
  already existed as a tab button/label but routed into the generic
  `DrawAllianceComingSoon` placeholder (same class of gap already fixed
  once for Journal/Chat, per that code's own M043O/M043Q comments). Now
  routes to a real `DrawAllianceResearchTab`: a scrollable list of
  technology cards (name, description, progress bar, donation cost in real
  resource names, DONNER button), states visually distinct
  (Locked/Available/Completed via color + badge text), contribution totals
  shown at the top, no fake buttons on locked/completed cards.

## 5. What was intentionally NOT built

- No Activity entry per donation (spam) - only a real completion would
  warrant one, and Alpha ships without inventing that event shape yet since
  the mission explicitly said "do not build an elaborate rewards system."
- Alliance Chat, Alliance Help, Player Profile, FTUE, PvP, Diplomacy,
  premium currency, building upgrade system: not opened.
- No LivingHive dependency anywhere in this mission's code.

## 6. SQL migration - NOT applied

`092_alliance_research.sql` (+ `.rollback.sql`) creates `dbo.AllianceResearch`
(one JSON-blob row per Alliance). **Registered** in the real migration
source (`DatabaseCatalog.Migrations` - not just a loose `Scripts/*.sql`
file, per the standing lesson from M045C) and a matching checked-in file
exists so the repo's own `CatalogSqlMatchesCheckedInScriptFiles` regression
test passes. **Not applied to production** - explicit CEO authorization
required (same `/ops/migrations/apply` flow used for `091_alliance_help.sql`)
before Alliance Research can go live. Until applied, production must keep
`AllianceResearch:Enabled=false` (default `false`, same pattern as
`AllianceHelp`).

## 7. Compile and tests

Server: `dotnet build`/`dotnet test` on `BeeKingdom.Server` and
`BeeKingdom.HiveOperations` - **0 errors**. **14 new tests, all green**:

- `AllianceResearchServiceTests.cs` (13 tests) — covers mission items 1-15:
  read state, non-member/locked/completed/insufficient-resources rejection,
  successful donation (debit + progress + contribution), persistence
  (shared across a second read - "Stara sees Jeff's donation"), concurrent
  donations from two members landing without loss, completion happening
  exactly once and clamping at `RequiredProgress`, prerequisite unlock,
  idempotent retry not double-charging, and both membership-bonus semantics
  (leaving removes the bonus, joining an alliance with completed tech
  grants it immediately).
- `HiveOfflineProductionServiceTests.cs` (+1 test) — item 16: a real
  resolver-supplied bonus measurably changes `EffectiveRate`/`EffectiveCapacity`'s
  output, and the baseline (no resolver registered) is unchanged.

Full existing suite (522 tests across `BeeKingdom.Tests` +
`BeeKingdom.HiveOperations.Tests`) re-run: **0 regressions**. One
pre-existing, unrelated failure remains (`091_alliance_help.sql`'s
checked-in file content has already drifted from its catalog entry - not
touched by, or introduced by, this mission).

Unity: compile verified clean via `assets-refresh` after every edit. 3 new
wire-level `AllianceResearchClientTests.cs` tests written (real endpoint
paths, real request bodies, technology-id URL-escaping) mirroring
`AllianceHelpClientTests.cs`'s established pattern for the same
cross-assembly-testability constraint (`AllianceCenterPanelController`'s
state machine lives in `Assembly-CSharp`, unreachable from
`BeeKingdom.Tests.asmdef`). **Not machine-verified this pass** - the Unity
Editor was confirmed live in Play Mode (`Application.isPlaying=true`, read
via a read-only `script-execute` probe) at the point tests would have run;
per this session's established discipline, EditMode tests were not
attempted against a live session.

## 8. Human certification

Not started - this report is the handoff point. No Alliance Test/production
data was mutated automatically; no donation was fabricated against Jeff's
or Stara's real accounts.

---

## Final checklist

| # | Question | Answer |
|---|---|---|
| A | Alliance Research server model implemented? | YES |
| B | Stable technology IDs/catalog implemented? | YES |
| C | Durable Alliance-owned progress implemented? | YES |
| D | Player contributions persisted? | YES |
| E | Donation uses real player resources? | YES |
| F | Donation atomic? | YES — two independently-idempotent steps, see section 3 |
| G | Idempotency/double-click protected? | YES — server-side (Receipts + ProcessedDonationIds) and client-side (single in-flight guard) |
| H | Concurrent donations safe? | YES — exclusive per-Alliance lock, test-proven |
| I | Prerequisites implemented? | YES |
| J | Completion persisted exactly once? | YES — clamped at RequiredProgress, test-proven |
| K | At least one real Alliance bonus applied to gameplay? | YES — three: production %, capacity %, combat power % |
| L | Membership bonus semantics correct? | YES — resolved fresh from current membership, test-proven both directions |
| M | Existing Alliance Center UI reused? | YES — the pre-existing "Recherches" tab, previously a placeholder |
| N | Research/Technology screen functional? | YES |
| O | Activity only records meaningful completion milestones? | YES — no per-donation spam (none built yet at all, by design) |
| P | Alliance Chat untouched? | YES |
| Q | Alliance Help untouched? | YES |
| R | No LivingHive runtime dependency? | YES |
| S | Server focused tests green? | YES — 14/14 new, 0 regressions in 522 existing |
| T | Unity compile green? | YES |
| U | Unity focused tests green? | Written, **not machine-verified this pass** — Editor confirmed live in Play Mode |
| V | SQL migration required? | YES — `092_alliance_research.sql`, registered, not applied |
| W | Production deployment required before CEO test? | YES — migration apply + `AllianceResearch:Enabled=true` |
| X | READY FOR CEO STAGE-1 CERTIFICATION? | **NOT YET** — needs the deployment in V/W first, and Unity test verification once the Editor is free |

If SQL migration is required: **YES**, do not apply — needs the same
authorized `/ops/migrations/apply` flow (with the existing admin/migration
keys) as `091_alliance_help.sql`, at a time you choose.

If production deployment is required: **YES** — commit/push (not done
without authorization), deploy, apply the migration, then set
`AllianceResearch:Enabled=true` in the IIS app pool environment, before any
CEO/Stara certification can begin.
