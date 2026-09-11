# M051B-CL — Alliance Research Production Deployment + Certification Gate

Deployment continuation of M051. No donation created, no resources spent,
no certification performed.

## 1. Commit scope

`HiveViewProductUiPresenter.cs` carries uncommitted work from three sources
this session (M049B, CX's Player Profile migration, M051) - a clean
file-level `git add` would have swept in code the CEO did not authorize
committing here. Extracted only the two M051 hunks (the "Recherches" tab
dispatch branch + the new `DrawAllianceResearchTab`/`DrawAllianceTechnologyCard`
methods) via a hand-built patch, verified with `git apply --cached --check`
before applying, and staged every other M051 file individually. `git diff
--cached --stat` was inspected before committing to confirm exactly the
M051 file set and nothing else. `git status` after the commit confirms
every excluded file (the 5 CX-owned bootstrap files, M049/M049B/M049C work,
the ad hoc `LivingHiveMenuCanvas.cs` compile fix, other mission reports) is
still present and unmodified in the working tree - nothing lost, nothing
reset/stashed/cleaned.

Committed: **25 files**, `d011212c`.

## 2. Pre-deploy safety

1. `git status` / `git diff --stat` reviewed (section 1).
2. M051 file set confirmed intentional.
3. `092_alliance_research.sql` confirmed registered in
   `DatabaseCatalog.Migrations` (grep-verified before commit).
4. `092_alliance_research.rollback.sql` confirmed present on disk.
5. `AllianceResearchOptions.Enabled` confirmed defaulting to `false` (plain
   C# `bool`, no `appsettings.json` override exists for
   `AllianceResearch` anywhere in the repo - grep-verified).
6. Alliance Test [BKT] confirmed intact post-deploy (section 6).

## 3. Unity test closeout

Editor was confirmed still in Play Mode (`Application.isPlaying=true`, via
read-only `script-execute` probe) at every check point this mission,
including immediately before writing this report. Per this mission's own
instruction ("when Editor is safely out of Play Mode") and this session's
established discipline, **`AllianceResearchClientTests` was not run** -
Play Mode never became free. This did not block deployment (server-side
only); it remains the one open item.

## 4. Server pre-deploy verification

- `dotnet build` on `BeeKingdom.Server`: **0 errors**.
- `dotnet test --filter AllianceResearchServiceTests|AllianceResearchProductionAndCapacityBonusAppliesFromResolver`:
  **14/14 green**.
- The pre-existing, unrelated `091_alliance_help.sql` checked-in-file drift
  (documented in M051's own report) was **not touched** and did not block
  the real migration runner - confirmed live post-deploy: `GET
  /ops/migrations/pending` returned exactly `["092_alliance_research.sql"]`
  before applying (091 was not pending - already applied in M045C), and `[]`
  after.

## 5. Deploy

- Pushed `main` -> `origin/main` (`9c9bac2e..d011212c`).
- Pushed `main` -> `origin/deploy` (fast-forward, verified
  `git merge-base --is-ancestor origin/deploy main` first) - this is the
  real production trigger (`.github/workflows/deploy.yml`, manual-promotion
  branch per the 2026-08-19 convention, not push-to-main).
- GitHub Actions run `33910808126` completed **success** in 1m7s (`gh run
  list --workflow=deploy.yml`), including its own built-in smoke test
  against `https://api-ops.beekingdomgame.com/`.
- `GET /health` verified directly afterward: `200 Healthy`.

## 6. Migration

Applied via the protected `/ops/migrations/apply` endpoint (Admin Key +
Migration Apply Key provided live by the CEO, verified by first calling
`GET /ops/migrations/pending` successfully before using the apply key for
anything). Response: `{"status":"Applied"}`. Re-checked `GET
/ops/migrations/pending` immediately after: `[]` - confirms `dbo.AllianceResearch`
was created and the migration runner considers the catalog fully applied
(no fake rows inserted to "prove" it - the repository's own `ExecuteAtomicallyAsync`/
`ReadAsync` code path against that exact table is what M051's SQL
repository already targets, structurally verified by code, not by writing
test data).

## 7. Enable feature

CEO ran, on the IIS server directly (I have no remote shell access to that
host - only HTTPS to the deployed API):

```
appcmd.exe set config "BeeKingdomApi/" -section:system.webServer/aspNetCore /+"environmentVariables.[name='AllianceResearch__Enabled',value='true']" /commit:apphost
appcmd.exe recycle apppool "BeeKingdomApi"
```

(First attempt failed - `appcmd` isn't on `PATH` by default; the full
`C:\Windows\System32\inetsrv\appcmd.exe` path was needed, and the two
commands had to run as two separate invocations rather than pasted as one
line.) Confirmed successful by the CEO. `GET /health` re-verified `200
Healthy` afterward.

## 8. Non-destructive live API proof

- `GET /alliance/v1/research` with **no** auth header: `401
  {"code":"alliance.session_required",...}` - proves deployed, routed, and
  authentication-protected.
- `GET /ops/migrations/pending` (Admin Key): `200 []` - proves the feature's
  own migration is fully applied server-side.
- **Real-member snapshot read performed and successful.** Once the CEO
  logged into the Hive in the same Play Mode session
  (`HasEnteredHiveForExternalHost` flipped to `true`), a genuinely
  read-only call was made through the real, already-authenticated
  controller (`AllianceCenterPanelController.RefreshResearch()` - a `GET`
  only, never a `POST`, no donation possible through this call) and the
  resulting cached model was read back:

  ```
  Loaded=True TechCount=9 MyContribution=0 ErrorCode=
  first tech id=prosperity_shared_reserves_i required=60 current=0 completed=False available=True
  ```

  All 9 catalog technologies returned, first tier `Available` (no
  prerequisite), zero progress and zero contribution (exactly the expected
  fresh state - nothing has ever been donated), no error code. This is the
  real production API, the real authenticated player, the real
  `dbo.AllianceResearch` table (created by the migration in section 6),
  round-tripped end to end - with no mutation performed.

## 9. Alliance Test preservation

Verified via the **public, unauthenticated** `GET
/alliance/v1/alliances/search?nameOrTag=BKT` (no player session needed,
genuinely read-only, no risk to your account):

```
{"name":"Alliance Test","tag":"BKT","memberCount":2,"joinMode":"InviteOnly",...}
```

Confirms the alliance exists and still has exactly 2 members. Role
verification (Stara = Chef, Jeff = Officier) is not exposed by this public
endpoint and I hold no member-level credentials to call the authenticated
members-list endpoint - not independently re-observed this pass. No code
in M051/M051B touches `AllianceService`, membership, roles, chat, help, or
activity at all (confirmed by the exact file list in section 1), so there
is no mechanism by which this deployment could have altered them.

## 10. Bonus safety

`092_alliance_research.sql` creates `dbo.AllianceResearch` with **zero
rows** (a plain `CREATE TABLE`, no seed data - confirmed by reading the
script itself, section 6 confirms no rows were inserted to test it).
`AllianceResearchBonusResolver.ResolveForAllianceAsync` reads that table via
`IAllianceResearchRepository.ReadAsync`, which returns `null` for an
Alliance with no row - the resolver returns `AllianceGameplayBonus.None`
(all-zero) in that case (code path, `AllianceResearchBonusResolver.cs`).
Since no Alliance in production has ever donated (feature was disabled
until this deployment), every Alliance's row is absent, so every player's
resolved bonus is `None` right now - existing production gameplay values
(resource production, storage capacity, combat power) are provably
unchanged by this deployment.

---

## Final checklist

| # | Question | Answer |
|---|---|---|
| A | M051 Unity focused tests executed? | **NO** — Editor remained in Play Mode throughout |
| B | Unity focused tests green? | N/A (not executed) |
| C | Server focused tests green? | YES — 14/14 |
| D | M051 committed? | YES — `d011212c` |
| E | M051 pushed? | YES — `origin/main` and `origin/deploy` |
| F | Server deployed? | YES — GitHub Actions run 33910808126, success |
| G | `/health` Healthy? | YES |
| H | Migration 092 applied? | YES — `{"status":"Applied"}`, pending list now `[]` |
| I | `dbo.AllianceResearch` available? | YES (structurally confirmed via migration-runner state; no fake rows inserted) |
| J | `AllianceResearch:Enabled=true`? | YES — confirmed by CEO, `/health` still `200` after recycle |
| K | `GET /alliance/v1/research` deployed and protected? | YES — `401 session_required` with no auth |
| L | Read-only real-member snapshot successful? | YES — real session, 9 technologies returned, 0 progress/0 contribution as expected, no mutation |
| M | Alliance Test preserved 2 members? | YES — public search endpoint, real-time |
| N | Stara still Chef? | Not independently re-verified (no public role data; no membership code touched) |
| O | Jeff still Officier? | Not independently re-verified (same reason) |
| P | No donation fabricated? | YES |
| Q | No player resources spent? | YES |
| R | Existing Alliance Help/Chat preserved? | YES — untouched, not opened this mission |
| S | READY FOR CEO STAGE 1? | YES |

READY FOR CEO STAGE 1 — OPEN ALLIANCE CENTER → RECHERCHES. DO NOT DONATE YET.
