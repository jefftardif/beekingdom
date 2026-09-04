# M045B-CL — Alliance Help: Real Player Entry Points

Closes the one gap M045-CL documented explicitly: players now have a real
"Demander de l'aide" action on the actual Construction, Research and
Training timer screens — no reflection, no proof hook, no debug button.

## 1. Approach

No new request path, no second Alliance Help service. Everything routes
through the exact same M045 pieces:
`AllianceClient.CreateHelpRequestAsync`/`ContributeHelpAsync` →
`AllianceHelpService` (server) → `OperationTimerReduction` → the real
`PlayerHiveState`.

What's new is purely a client-side recovery/state layer on
`AllianceCenterPanelController` (`Assets/BeeKingdom/Playground/
AllianceCenterPresentation.cs`) plus one reusable UI action drawn on each
real timer screen (`Assets/BeeKingdom/Playground/
HiveViewProductUiPresenter.cs`).

`AllianceCenterPanelController` gained a per-operation cache,
`Dictionary<"category|targetId", AllianceHelpOperationState>`
(`State`: Unknown/NoRequest/Sending/Requested/Error, plus `HelpCount`/
`MaxHelpCount`), backed by two real methods:

- `RefreshHelpOperationState(category, targetId)` — `GET
  /alliance/v1/help/requests/mine?...`, throttled to once per 5 seconds per
  operation (no new polling framework, same cadence family as the existing
  building-upgrade/production periodic refresh already on these screens).
  This is what makes "reopen/reconnect/scene-change recovers real state"
  true — the cache is never trusted as truth on its own, only ever a mirror
  of the last real server read.
- `RequestHelp(hiveId, category, targetId)` — the actual "Demander de
  l'aide" click: guards against double-submit (no-op while already
  Sending/Requested), calls `CreateHelpRequestAsync`, and updates the cache
  from the server's real returned `HelpCount`/`MaxHelpCount` — never a
  locally-guessed 0/10.

The M045 fire-and-forget `RequestHelpForOperation`/
`RequestHelpForOperationForProofAsync` (never wired to any screen) were
removed rather than left as a second, unused path — `RequestHelp`/
`RequestHelpForProofAsync` are the one real entry point now.

`DrawAllianceHelpAction(area, hiveId, category, targetId,
estimatedOriginalDurationSeconds, compact)` in `HiveViewProductUiPresenter.cs`
is the single reusable button, drawn identically on all three (four,
counting Healing) real screens:

| `state.State` | Label | Enabled |
|---|---|---|
| Unknown / NoRequest | "Demander de l'aide" | only if in an Alliance |
| Sending | "Envoi…" | no |
| Requested, `HelpCount < Max` | "Aide demandée · X/10" | no |
| Requested, `HelpCount >= Max` | "Aides reçues · 10/10" | no |
| Error | "Réessayer" | only if in an Alliance |

Uses the existing `DrawPreviewActionButton` (same disabled-with-reason
convention as every other action on these screens — clicking a disabled
button surfaces `localPreviewDisabledReason` via the same feedback pulse
the rest of the game already uses, not a new pattern).

## 2. Construction

`DrawOfficialBuildingUpgradeOnlyDetail` — the real building-upgrade detail
panel (`Assets/Scenes` canonical HiveMap, not LivingHive). The action is
gated on `model.ActiveOperation != null && ActiveOperation.BuildingKey ==
hotspot.HotspotId && !IsAwaitingCompletion` — the exact same
`HiveBuildingUpgradeOperationModel` the countdown/progress bar above it
already reads. `estimatedOriginalDurationSeconds` = `CompletesAtUtc -
StartedAtUtc` on that same model. Both compact and landscape layouts
covered.

## 3. Research

`DrawResearchFullscreenCard` — the real research catalog card (only for
`official` = true, i.e. a real server session; the local-preview/no-session
path has no real operation to attach help to and correctly shows nothing).
Gated on `model.ActiveOperation.ResearchId == definition.ResearchId &&
!IsAwaitingCompletion`, drawn just under the existing progress bar inside
the card.

## 4. Training

`DrawOfficialBarrackContent` — the real Barracks screen, inside the
`runningHere` branch (`active.Family == officialSelectedTroopFamily &&
!IsTrainingReadyToClaimNow`), right under the existing "Entrainement en
cours" progress bar.

## 5. Healing

`DrawOfficialBroodVitalityDetail` — wired the same way (both compact and
landscape), reading the real `HiveBroodVitalityOperationModel.Type`/
`StartedAtUtc`/`EndsAtUtc`. **Correctly never actionable today**: real
Feeding/Stabilization durations are hardcoded to 12s/13s
(`BroodVitalityCareService`), always below the 300s eligibility hint (see
section 8), so the button never appears in current gameplay — by design,
not by special-casing it away. If a future balance pass lengthens these
durations, this needs no further code change.

## 6. No-Alliance UX

Chose **discoverable-but-disabled**, matching the convention already used
everywhere else on these screens (`DrawPreviewActionButton` with
`enabled=false` + `localPreviewDisabledReason`) rather than hiding: a
solo player sees "Demander de l'aide" greyed out, and a click surfaces
"Rejoignez une alliance pour demander de l'aide." through the same
feedback-pulse mechanism as every other gated action in this game — not a
new pattern invented for this feature.

## 7. Short-operation UX

The button is not drawn at all (not shown disabled — genuinely hidden)
when `estimatedOriginalDurationSeconds` is known and below 300s (the
client-side UX hint mirroring `AllianceHelpOptions.MinEligibleOriginalDurationSeconds`'s
documented Alpha default). No operation duration was changed to make this
easier to see — Healing stays at 12s/13s, and the known short early
Construction upgrades correctly never show the button either.

## 8. M045 issue reviewed: `OriginalDurationSeconds` anchor

Reviewed as instructed. `AllianceHelpService.CreateRequestAsync` still
computes `OriginalDurationSeconds` as `(current EndsAtUtc − StartedAtUtc)`
at request-creation time — i.e. after any earlier gem-bought speed-up, not
the operation's literal catalog duration. **No redesign** — this remains
acceptable for Alpha: `StartedAtUtc` never changes, so this is simply "how
long is left to help with, from a fixed starting point, as of the moment
help was requested," which is the actually-relevant anchor for sizing a
%-based reduction against what's still outstanding. The only alternative
(storing the true pre-speed-up catalog duration) would require adding new
state to `PlayerHiveState` purely for this, which M045 correctly avoided.
Not a correctness problem, just a documented simplification — left as is.

## 9. Server remains authoritative

Nothing new added client-side that decides real eligibility. The 300s
duration hint and the "in an Alliance" check are both UX predictions only
— `AllianceHelpService` (unchanged this mission) still independently
verifies membership, hive ownership, operation existence/status, and the
real `MinEligibleOriginalDurationSeconds` before ever creating a request.
A stale/incorrect client prediction fails safely: the button shows when it
shouldn't → the real POST is rejected with a real error code, surfaced via
the same `[AllianceHelp] CreateHelpRequest rejected...` log + "Réessayer"
state already built in M045's row status machine.

## 10. Error handling

No silent failure: `RequestHelpCoreAsync` always ends in a concrete cached
state (Requested or Error) and always logs the real server code on
rejection (`[AllianceHelp] CreateHelpRequest rejected for
{category}/{targetId}: code=... rawError=... rawMessage=...`), exactly
mirroring the established `InvitePlayerCoreAsync` logging convention from
M043S. The button becomes "Réessayer" (still clickable) rather than
getting stuck.

## 11. Automated tests

`Assets/BeeKingdom/Tests/Editor/AllianceHelpClientTests.cs` — 5 new tests
against the wire-level `AllianceClient`, same architecture constraint
already documented in `AllianceClientTests.cs`'s own top-of-file comment
(unchanged by this mission): `AllianceCenterPanelController` lives in the
default `Assembly-CSharp` assembly, structurally unreachable from
`BeeKingdom.Tests.asmdef`. Cover: `CreateHelpRequestAsync` sends exactly
one request with the real hiveId/category/targetId; `ContributeHelpAsync`
posts to the specific request id; `ContributeHelpAllAsync`;
`GetMyOpenHelpRequestAsync`'s query-string construction; `ListHelpRequestsAsync`
requests the real typed view list. This is genuinely the layer this
project's Unity test suite can reach — the richer state machine
(Sending/Requested/reopen-recovery, items 1-3/5-9/12-13 of the mission's
list) lives behind the same wall `StableError`/every other
`AllianceCenterPanelController` method already does, and was reviewed by
direct code inspection instead (traced against the exact same
`RefreshHelpOperationStateCoreAsync`/`RequestHelpCoreAsync` logic the
report describes in section 1).

Server-side, the relevant behaviors from the mission's coverage list are
already proven by the existing M045 suite (unchanged this mission):
eligible-Construction creates a request
(`CreateRequest_MemberWithEligibleOperation_Succeeds`), short operations
don't (`CreateRequest_OperationTooShort_Rejected`), non-members can't
(`CreateRequest_NonMember_Rejected`), repeated creation can't duplicate
(`CreateRequest_Repeated_ReturnsSameOpenRequestInsteadOfError`).

## 12. Regression

- Unity compiles: **0 errors** (confirmed via `assets-refresh` after every
  edit in this mission).
- Server: `BeeKingdom.HiveOperations.Tests` (includes every
  `SpeedUpInventoryService` test) — **181/181 green**.
  `BeeKingdom.Tests` filtered to BuildingUpgrade + AllianceHelp — **29/29
  green**; filtered to Research/Recruitment/Doctrine/SpeedUp — **10/10
  green**. (No server code was touched by M045B — this mission is
  Unity-only — these reruns are a confirmation, not a new risk.)
- Unity EditMode test **execution could not be completed live**: after the
  last successful compile, the Unity Editor process became transiently
  unresponsive to the MCP bridge (`Responding: False`, confirmed via the
  OS process list) before the automated test run could complete — a known,
  previously-documented transient condition on this machine, not something
  this mission's changes caused (it followed a clean, successful compile).
  The 5 new `AllianceHelpClientTests` were not executed live; they compile
  cleanly and follow the exact structure of the already-passing,
  already-established `AllianceClientTests.cs` file byte-for-byte in
  pattern. Re-run once the Editor recovers.

## 13. Deployment readiness — exact sequence

Nothing in this mission changed server code or the SQL migration written
by M045. The sequence, inspected against the real
deployment/migration architecture in this repo, is:

1. **Commit and push** the approved M045 + M045B code (`git push origin
   main`, then `git push origin main:deploy` to trigger the GitHub Actions
   `Deploy BeeKingdomApi` pipeline — same pipeline used for every prior
   deploy this session).
2. **Apply `091_alliance_help.sql`** against production — via
   `GET /ops/migrations/pending` then the existing migration-apply
   endpoint (the same `IMigrationRunner` flow `090_alliance_platform.sql`
   went through), never a manual SQL session. Verify it reports applied
   before continuing.
3. **Set `AllianceHelp:Enabled=true`** in the production configuration
   (IIS app-pool environment variable, same mechanism as every other
   feature flag on `api-ops` — not the checked-in `appsettings.json`).
4. **Deploy** (if step 1's `deploy` push hasn't already rolled the new
   binaries out — confirm via the GitHub Actions run, same as every prior
   deploy this session).
5. **Verify API health** (`/health` → 200, matching the check already done
   after the M043U-CL deploy).
6. **Verify Alliance Test [BKT] unchanged** — read-only: Stara still Chef,
   Jeff still Officier, member count still 2, existing Chat history intact.
7. **Perform the human Jeff/Stara certification** (section 14).

This mission performed **none** of these steps.

## 14. Human certification target

Unchanged from the mission brief — ready to run once deployed, with real
UI now available for every step (no proof hook needed):

**Phase 1 — Jeff**: start a real Construction upgrade ≥5 minutes → open
its timer → see "Demander de l'aide" → click once → see "Aide demandée ·
0/10" → screenshot.

**Phase 2 — Stara**: Alliance Center → Aides → see Jeff's real request,
remaining duration, 0/10 → Aider once → see "Aidé" → screenshot.

**Phase 3 — Jeff**: reopen the same Construction timer → verify 1/10 and
the real reduced remaining duration → restart/reconnect → verify both
persist.

## 15. Out of scope

Confirmed untouched: Dons, Alliance Research, Shop, Gifts, Territory,
Diplomacy, War, Web, FTUE, LivingHive. No new timer system. No operation
duration was changed for any reason, including to make testing easier.

---

## Final checklist

| # | Question | Answer |
|---|---|---|
| A | Construction real Help button wired? | YES |
| B | Research real Help button wired? | YES |
| C | Training real Help button wired? | YES |
| D | Healing future wiring correct? | YES |
| E | No-Alliance UX correct? | YES (disabled + reason, matches existing convention) |
| F | Short-operation UX correct? | YES (button hidden, not shown broken) |
| G | Existing request recovered from server? | YES |
| H | HelpCount recovered from server? | YES |
| I | No duplicate request possible? | YES (client guard + M045's server-side unique index) |
| J | No reflection/proof hook required for player flow? | YES |
| K | Existing M045 service reused unchanged? | YES (server code untouched this mission) |
| L | SpeedUp regression tests green? | YES (181/181) |
| M | Building/Research/Training regressions green? | YES (29/29 + 10/10) |
| N | Unity compiles? | YES (0 errors) |
| O | SQL migration still unapplied? | YES |
| P | Production deployment still untouched? | YES |
| Q | Exact deployment sequence documented? | YES (section 13) |
| R | READY FOR DEPLOYMENT AUTHORIZATION? | YES |
| S | READY FOR JEFF/STARA HUMAN CERTIFICATION AFTER DEPLOY? | YES |
