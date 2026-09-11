# M048-CL — Alliance Help Research + Training Closeout

Construction is already human-certified with real Jeff/Stara accounts and
was NOT reworked. This mission closes out Research and Training.

## 1. Real renderer verification (not trusting code presence alone)

**Research**: `DrawResearchOverlayForExternalHost` (real bottom-rail
"Recherche" button) → `DrawActiveHiveMenuPanel` → `DrawResearchMenuPanel`
→ `DrawOfficialResearchMenuPanel` (real session configured, which it is
in production). `DrawAllianceHelpAction` is present in exactly this
method (confirmed at the current call site, still intact since M045D -
not touched by the concurrent M047 Player Profile work in the same file).
`DrawResearchFullscreenCard` (also wired, from M045B) remains a
separately-reached, non-primary screen - unchanged, not the CEO's real path.

**Training**: `DrawBarrackOverlayForExternalHost` (real bottom-rail
"Entraînement" button) → `DrawOfficialBarrackContent`. `DrawAllianceHelpAction`
confirmed present at the exact real call site (from M045B, never needed
correction - Training was already wired into the right screen).

No LivingHive runtime path involved anywhere in either trace.

## 2. Eligibility - real balance, not manufactured

**Research: eligible, real options exist.** Read directly from
`Server/src/BeeKingdom.HiveOperations/HiveOperationService.cs`'s
`ResearchCatalog` (unchanged, not touched this mission):

| Research | Duration | Eligible (≥300s)? |
|---|---|---|
| foraging_routes_i / tempered_combs_i / pollen_sorting_i | 120s | No |
| foraging_routes_ii / tempered_combs_ii / pollen_sorting_ii | 360s (6 min) | **Yes** |
| foraging_routes_iii / tempered_combs_iii / pollen_sorting_iii | 720s (12 min) | **Yes** |
| sealed_reserves | 1200s (20 min) | **Yes** |

A live, read-only reflection check against the CEO's actual current
session (Play Mode active, not disturbed) confirmed **zero completed
research** and no active operation right now - meaning every tier-2/3
research is currently blocked by its own real, server-enforced
prerequisite (`research_prerequisite_missing`, `HiveOperationService.cs:442`).
This is real game design, not something this mission can or should bypass.

**Exact real first step for the CEO** (see the STOP instruction at the end
of this report).

**Training: genuinely ineligible by current Alpha balance - not touched.**
Read directly from `Server/src/BeeKingdom.HiveOperations/CombatRecruitmentService.cs:20-25`
(unchanged): all three families (`guardians`, `wingrunners`, `darters`)
have a hardcoded `TimeSpan.FromSeconds(14)` duration - 14 seconds, far
below the 300s Alpha threshold, and no other real Training operation
exists in this codebase. No duration was changed to make this eligible,
per the mission's explicit instruction.

**UI correctly hides Help for this case**, verified by code trace, not
guessed: `DrawAllianceHelpAction`'s own eligibility guard
(`estimatedOriginalDurationSeconds < minEligibleDurationSecondsHint (300)
→ return`, added M045B, unchanged) means the Training screen's call
(`(active.EndsAtUtc - active.StartedAtUtc).TotalSeconds` = 14 for any real
Training operation) never even reaches the point of drawing a button - no
broken/disabled placeholder shown, the control simply and correctly does
not appear, exactly the same behavior already proven correct for
short Construction upgrades.

**TRAINING HUMAN HELP TEST CURRENTLY INELIGIBLE BY BALANCE.**

## 3. M045G-style hardening applied where concretely applicable

Per the mission's instruction (harden only the real paths touched here,
not a global refactor): Research's and Training's own official action
buttons (`OfficialResearchActionLabel`/`RunOfficialResearchAction` in
`DrawOfficialResearchMenuPanel`; `OfficialDoctrineRecruitmentActionLabel`/
`RunOfficialDoctrineRecruitmentAction` in `DrawOfficialBarrackContent`,
via `RegisterFtueTrainingButtonAndDraw`) both previously called
`DrawPreviewActionButton` without the `officialDisabledReason` override
M045G-CL introduced for Construction - meaning a disabled click on either
could have surfaced the same class of stale, unrelated shared-scratch-state
message Construction was hardened against.

Both now pass their own real, state-derived status text
(`OfficialResearchStatusText(researchId)` /
`OfficialDoctrineRecruitmentStatusText(officialSelectedTroopFamily)`) as
`officialDisabledReason`, exactly mirroring Construction's fix. Every
other of `DrawPreviewActionButton`'s ~100 call sites is untouched
(the parameter is optional, default `null`, zero behavior change
elsewhere). `RegisterFtueTrainingButtonAndDraw` (Training's thin FTUE
wrapper) gained the same optional pass-through parameter, nothing else.

## 4. Alliance Help request/status flow

Unchanged and reused as-is for Research/Training - both call the exact
same `DrawAllianceHelpAction`, `AllianceCenterPanelController.RequestHelp`/
`RefreshHelpOperationState`/`GetHelpOperationState`, and
`AllianceClient.CreateHelpRequestAsync`/`GetMyOpenHelpRequestAsync`
Construction already proved end-to-end with real accounts. No second
Help subsystem exists or was created. Duplicate-request prevention is the
same client-side Sending/Requested guard plus the server's real unique
index on `(RequestingPlayerId, OperationCategory, OperationTargetId)
WHERE Status = 'Open'` (091 migration, already deployed, unchanged).

## 5. Server truth / no fake timer

`ResearchAdapter_HelpReducesRealResearchTimer` and
`TrainingAdapter_HelpReducesRealTrainingTimer`
(`Server/tests/BeeKingdom.Tests/AllianceHelpServiceTests.cs:368,385`,
already existed from M045, unchanged) prove server-side that a help
contribution reduces the REAL `HiveResearchState.ActiveOperation.EndsAtUtc`
/ `DoctrineRosterState.ActiveOperation.EndsAtUtc` - the same fields
`researchController`/`doctrineRecruitmentController` read for the
countdown the player sees. No parallel/local timer exists for either.

## 6. Alliance membership - untouched

No kick/leave/transfer/dissolve/recreate/role-change was performed.
Alliance Test [BKT] (Stara Chef, Jeff Officier) was only read (via the
same live reflection check in section 2), never mutated.

## 7. Concurrency - CX/M045/M046/M047 preserved

`git status`/`git diff` checked before and during editing.
`HiveViewProductUiPresenter.cs` had ongoing concurrent M047 (Player
Profile) edits in the same working tree throughout this mission (the file
changed on disk mid-session more than once); this mission's edits were
narrow, targeted string replacements that applied cleanly each time
without touching Player Profile code, M046 pulse tuning, or M045F's
`BuildingInteractionController`/click-preemption code (none of those
files were opened for writing this mission). No reset/stash/checkout/clean
was performed.

## 8. Healing - deferred, confirmed intact

Not this mission's objective. Confirmed by code trace only (no combat/injury
manipulated): `DrawBroodVitalityAllianceHelpAction` (M045E) remains wired
into `DrawOfficialBroodVitalityDetail`, correctly gated the same way as
Training - real Feeding/Stabilization durations are 12s/13s
(`BroodVitalityCareService.cs`, unchanged), below threshold, so the
control correctly stays hidden. Human Healing certification remains
deferred until a legitimate ≥5-minute Healing operation exists, per this
mission's instruction not to manipulate combat to force one.

## 9. Regression

Server: `AllianceHelpServiceTests` filtered run - **21/21 green** (0
server code changed this mission - test run is a confirmation, not a new
risk). Unity compile: **0 errors** (`assets-refresh` confirmed clean after
every edit, including while M047 was concurrently saving to the same
file). Unity EditMode tests **not executed live**: Play Mode was found
active and paused (the CEO's own session, consistent with the pattern in
M045E/F/G) both before and after this mission's edits - running tests
risks disturbing that preserved state, so this was deliberately skipped
again. `AllianceHelpClientTests` (category-agnostic wire-level coverage,
already exercises Research/Training/Construction/Healing identically)
was not re-run live for the same reason; nothing in its five tests could
be affected by this mission's UI-only changes.

## 10. No server deployment

No server code was read or modified this mission (only Unity-side
`HiveViewProductUiPresenter.cs`). Nothing to deploy.

---

## Final checklist

| # | Question | Answer |
|---|---|---|
| A | Real HiveMap Research renderer proven? | YES — `DrawOfficialResearchMenuPanel` |
| B | Research Help control wired into that exact renderer? | YES (from M045D, reverified intact) |
| C | Legitimate ≥5min Research operation available? | YES (foraging_routes_ii/iii, tempered_combs_ii/iii, pollen_sorting_ii/iii, sealed_reserves) — currently blocked by a real, unbypassed prerequisite (0 research completed yet) |
| D | Research ready for CEO two-player Help test? | YES — after one real, fast (120s) prerequisite research, see the exact first step below |
| E | Real HiveMap Training renderer proven? | YES — `DrawOfficialBarrackContent` |
| F | Training Help control wired into that exact renderer? | YES (from M045B, already correct) |
| G | Legitimate ≥5min Training operation available? | NO — all 3 families are 14s |
| H | Training ready for CEO two-player Help test? | **INELIGIBLE** (by current Alpha balance, correctly hidden, not a bug) |
| I | Research/Training official feedback protected from unrelated stale scratch-state? | YES — both hardened this mission, same M045G principle |
| J | Existing Alliance Help server implementation reused unchanged? | YES |
| K | Existing eligibility/balance unchanged? | YES |
| L | Existing Alliance Test membership untouched? | YES |
| M | Healing left intact/deferred appropriately? | YES |
| N | No LivingHive runtime dependency introduced? | YES |
| O | Concurrent CX/M045/M046 work preserved? | YES |
| P | Unity compile green? | YES |
| Q | Relevant tests green? | YES (server 21/21; Unity tests deliberately not re-run live, Play Mode preserved) |
| R | Server deployment required? | NO |
| S | READY FOR CEO RUNTIME CERTIFICATION? | YES (Research); Training correctly out of scope for Alpha until balance changes |

## Exact first real step for the CEO — Research

You currently have **0 completed research**, so every ≥5-minute research
is still locked by its own real prerequisite. Start with any cheap tier-1
research first (displayed duration: **2 min**, e.g. "Ateliers de cire
tempérée" / tempered_combs_i, or either of the other two tier-1s) and let
it finish normally.

Once that one tier-1 is done, its tier-2 unlocks with a displayed duration
of **6 min** (e.g. tempered_combs_ii) - that is the real operation to
start for the actual Alliance Help test: start it, confirm "Demander de
l'aide" appears, click it once, then have Stara help it from Alliance
Center → Aides, exactly like the Construction test.

Do not start the tier-1 and the eligible tier-2 as the same click - the
tier-1 is unavoidable real prep, the tier-2 is the one to test Help on.
