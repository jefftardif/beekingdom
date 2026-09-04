# M045D-CL — Construction Help Button Runtime Fix

Real human runtime repro: Jeff started a genuine ~7-minute Construction
upgrade (Réserve de miel, Niv. 5 → 6, ~6 min 58 s remaining shown live) and
no "Demander de l'aide" action appeared anywhere on the running screen.
Current operation was never touched, cancelled, sped up, or mutated by
this investigation.

## 1. Root cause — proven, not guessed

**M045B wired the wrong screen.**

The CEO reaches Construction via the bottom-rail "Construction" button,
which opens `DrawConstructionOverlayForExternalHost(bool compact)`
(`Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs:4216`). Its
own inline detail block — not a call to any other method — renders exactly
the screenshot's text: `"Niveau actuel : {level}"` (line 4270), then
`OfficialBuildingUpgradeStatusText` ("Niv. {0} → {1} · {2} restantes",
line 4271), then, while running, `"Amelioration en cours : {progress}%"`
+ a progress bar (lines 4289-4290) — a byte-for-byte match against the
CEO's screenshot.

M045B instead wired `DrawAllianceHelpAction` into
`DrawOfficialBuildingUpgradeOnlyDetail` (line ~42455) — a **different**
method, reached from a different UI path (a reference-hotspot detail
panel), never called by `DrawConstructionOverlayForExternalHost`. The two
screens both show a building's upgrade progress using the same underlying
`HiveBuildingUpgradeScreenModel`, which is why M045B's own manual read of
the code looked plausible but was never actually exercised by the real
click path the CEO uses.

**Research has the exact same structural mistake**, traced in section 4.
**Training does not** (see section 5) — `DrawBarrackOverlayForExternalHost`
already calls `DrawOfficialBarrackContent`, the same method M045B wired.

## 2. Answering the mission's specific questions

- `model.ActiveOperation != null` — true (a real operation is running).
- `ActiveOperation.BuildingKey` vs `selectedHotspot.HotspotId` — **no
  mismatch**. `DrawConstructionOverlayForExternalHost` itself already
  gates `runningHere` on `ActiveOperation.BuildingKey ==
  selectedHotspot.HotspotId` (line 4260) before ever reaching the block
  M045D now extends — if these didn't match, the countdown/progress text
  the CEO *did* see wouldn't have rendered either. `selectedHotspot.HotspotId`
  is the same canonical building-mapping-table id used everywhere else in
  this file; no new/second ID vocabulary was introduced.
- `IsAwaitingCompletion` — false (operation still running, ~7 min
  duration, nowhere near completion at the time of the screenshot).
- `estimatedOriginalDurationSeconds` — computed as `CompletesAtUtc -
  StartedAtUtc` on the exact same `ActiveOperation` the countdown reads;
  for a 7-minute (420s) configured duration this is ≈420, comfortably
  above the 300s eligibility hint. **Not the cause** — the code computing
  it (identical formula M045B already used correctly elsewhere) was simply
  never reached for this screen, because the call to
  `DrawAllianceHelpAction` didn't exist here at all before this fix.
- Alliance membership / feature flag / API availability — **not the
  cause**. `DrawAllianceHelpAction` itself (unchanged) correctly reads
  `allianceCenterController.Model?.Overview` and calls the real M045C
  endpoints; none of that logic was ever reached for this screen because
  the call site was missing, not because a condition inside it evaluated
  false.
- Rect/layout — not applicable; the call didn't exist, so there was
  nothing to be mis-positioned. The new call sites use the same on-screen
  coordinate space as the surrounding controls already visible in the
  screenshot (`margin`/`contentWidth` for Construction, `action`'s own
  Rect for Research), not arbitrary coordinates.

**Root cause, one sentence**: M045B added `DrawAllianceHelpAction` to a
building/research detail screen that exists in this codebase but is not
the one the real bottom-rail Construction/Research buttons actually open.

## 3. Fix — Construction

`DrawConstructionOverlayForExternalHost`'s own `official && runningHere`
branch (the block that already draws the "Amelioration en cours : X%"
label and progress bar) now also draws `DrawAllianceHelpAction` right
below the progress bar, using the exact same `officialModel.ActiveOperation`
and `selectedHotspot.HotspotId` already in scope — no new lookup, no new
ID mapping, no hardcoded building key. Works identically for any building,
not just Réserve de miel.

## 4. Fix — Research

Traced the real path: `DrawResearchOverlayForExternalHost` →
`DrawActiveHiveMenuPanel` → `DrawResearchMenuPanel` →
`DrawOfficialResearchMenuPanel` (when a real session is configured, which
it is in production) — a per-card research catalog list, structurally
parallel to Construction's bug. `DrawResearchFullscreenCard` (wired in
M045B) is a separate, differently-reached screen with the same underlying
model but never invoked from the real Research button.

Fixed the same way: the running card's status-text row is replaced with
`DrawAllianceHelpAction` (only while genuinely running and not awaiting
completion) - the passive "X restantes" text this replaced was redundant
with the progress bar directly above it. Uses the real `model.ActiveOperation`
and `researchId` already in scope for that card - works for any research,
not a special case.

## 5. Research/Training cross-check

- **Research**: same structural mistake found and fixed (section 4).
- **Training**: verified NOT broken by the same cause.
  `DrawBarrackOverlayForExternalHost` (the real bottom-rail "Entraînement"
  path) calls `DrawOfficialBarrackContent` directly when a real session is
  configured — the exact method M045B wired `DrawAllianceHelpAction` into.
  No fix needed; confirmed by direct code trace, not assumed.

## 6. Smallest robust fix — confirmed

No hardcoded building/research id anywhere in either fix. No special case
for Jeff, for Réserve de miel, or for level 5→6. Both fixes reuse the
exact same `estimatedOriginalDurationSeconds` formula and the same
`DrawAllianceHelpAction` helper M045B already built — only the call SITE
was wrong, not the shared logic underneath it, which required no changes.

## 7. Regression

- Unity compile: **0 errors** (`assets-refresh` after the edit).
- `AllianceHelpClientTests`: **5/5 green** (Unity EditMode, executed live —
  Editor recovered since M045C).
- `HiveBuildingUpgradeClientTests`: **15/15 green**.
- `AllianceClientTests`: **16/16 green**.
- No server code touched by this mission — no server tests re-run, none
  needed.

## 8. Deployment

**Unity-side only.** No server code was read, touched, or needs
redeployment. `AllianceHelp:Enabled=true` and the 091 migration (both from
M045C) are unaffected and remain exactly as they were.

## 9. Human runtime proof

Not performed by this agent - no Help request created, no proof hook
used, current real operation untouched throughout this entire
investigation and fix.

---

## Final checklist

| # | Question | Answer |
|---|---|---|
| A | Exact runtime screen path identified? | YES — `DrawConstructionOverlayForExternalHost` (Construction), `DrawOfficialResearchMenuPanel` via `DrawResearchMenuPanel`/`DrawActiveHiveMenuPanel`/`DrawResearchOverlayForExternalHost` (Research) |
| B | DrawAllianceHelpAction actually called before fix? | NO (on either real screen) |
| C | Exact gating condition that failed? | None inside `DrawAllianceHelpAction` — the call site itself was missing on the real screen; M045B wired a different, non-reachable-from-here method |
| D | BuildingKey/HotspotId match? | YES — no mismatch, verified against the same comparison the existing countdown already depends on |
| E | Computed duration in seconds? | ≈420 (7 min configured), correct formula, simply never reached before this fix |
| F | Jeff Alliance client state correct? | YES — untouched, unaffected by this bug |
| G | Feature flag/client capability state correct? | YES — unaffected |
| H | Button rect/layout valid? | YES — placed in the same coordinate space as the surrounding real controls, no clipping/occlusion introduced |
| I | Root cause proven? | YES |
| J | Fix shared across all eligible Construction upgrades? | YES — no per-building special case |
| K | Research same issue? | YES — found and fixed |
| L | Training same issue? | NO — verified already correctly wired |
| M | Unity compile green? | YES |
| N | Relevant tests green? | YES (5+15+16 = 36/36) |
| O | Server deployment required? | NO |
| P | READY FOR CEO REAL UI RETEST? | YES |

READY FOR CEO — RETEST ELIGIBLE CONSTRUCTION UI.
