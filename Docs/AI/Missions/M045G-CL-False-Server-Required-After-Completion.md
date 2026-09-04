# M045G-CL — False "serveur requis" After Successful Building Completion

Real runtime report: CEO clicked "Terminer" on a completion-ready Caserne,
the real server completion succeeded (level advanced to 6), but the UI
displayed "serveur requis" - a false/misleading message given the server
action had already succeeded.

## 1. Investigation

Traced the full chain: `DrawConstructionOverlayForExternalHost` → "Terminer"
button → `RunOfficialBuildingUpgradeAction` →
`buildingUpgradeController.Complete()` →
`HiveBuildingUpgradePanelController.CompleteCoreAsync()`.

**Ruled out, with proof:**

- `CompleteCoreAsync`'s success path is correct and unchanged-worthy: on a
  successful `client.CompleteAsync(...)` response it sets `Model =
  HiveBuildingUpgradePresentation.Ready(snapshot, ...)` - never
  `NotConfigured`, never an error state. The only place
  `HiveBuildingUpgradeScreenState.NotConfigured` can ever be produced in
  this codebase is `UnavailableHiveBuildingUpgradePanelController`'s fixed
  model (`HiveBuildingUpgradePresentation.cs:286`) - a completely
  different controller instance, only used when no official session is
  configured at all.
- `OfficialBuildingUpgradeActionLabel`'s own `model == null || model.State
  == NotConfigured` branch (the literal source of the "Serveur requis"
  string at `HiveViewProductUiPresenter.cs:20892`) is **structurally
  unreachable** for a real Terminer click: it is only ever called from
  inside the `if (official)` branch of `DrawConstructionOverlayForExternalHost`,
  which itself requires `OfficialBuildingUpgradeConfigured()` (`buildingUpgradeController
  != null && buildingUpgradeController.IsConfigured`) to be true in the
  same frame - so by the time that label function runs, `model` cannot be
  the `NotConfigured` one.
- `DrawPreviewActionButton`'s disabled-click fallback (the mechanism that
  writes `localPreviewLastActionStatus`/shows a "blocked" message) only
  runs when the button was **disabled** at click time
  (`!enabled && GUI.Button(...)`, short-circuited otherwise) - the CEO's
  Terminer click succeeded, meaning the button was enabled at that exact
  click, so this exact branch could not have fired for that specific click.
- A live, read-only reflection check against the CEO's actual running
  session (Play Mode active, mid-investigation) confirmed
  `buildingUpgradeController.Model.State == Ready`,
  `localPreviewLastActionStatus`/`localPreviewLoopMessage` both held
  correct, real official text ("Le serveur réserve les ressources et le
  créneau…", "Session officielle · serveur autoritaire") from the CEO's
  next action (a new upgrade already started) - not "Serveur requis" -
  confirming the false message was transient, already overwritten by
  subsequent real activity by the time this mission investigated, and
  confirming these shared fields DO correctly reflect real official state
  once written by an official code path.

**Root architectural cause, proven by direct code reading (not a single
pinpointed write, but a confirmed mechanism):** `localPreviewDisabledReason`
/`localPreviewLoopMessage`/`localPreviewLastActionStatus` are shared,
file-scoped mutable fields written by dozens of unrelated local-preview
code paths across this ~45,000-line file every frame (insufficient
resources, training queue busy, a dev-only QA harness's own literal
`"Service futur requis"`, and many more) - `DrawPreviewActionButton`'s
disabled-click branch (`HiveViewProductUiPresenter.cs:43012`, pre-fix)
always read whichever one of these happened to have been written LAST
that frame by any code that ran earlier in the same draw pass, completely
decoupled from which specific button was actually clicked. A real,
official, server-backed button (like Construction's own action button)
sharing this scratch state with ~100 unrelated local-preview buttons means
any transient disabled-click on it (e.g. a legitimate momentary `IsBusy`
state right around a real mutation) could surface a stale, unrelated
message - including, plausibly, "Serveur requis" if that shared field had
last been written by an unrelated preview-only code path anywhere else on
screen that session. This exact hypothesis was the mission's own primary
suspect and is the one this investigation could substantiate architecturally
where a single definitive originating write-site could not be pinned down
retroactively (state had already moved on in the live session by the time
of investigation).

## 2. Fix

`DrawPreviewActionButton` gained one new optional parameter,
`Func<string> officialDisabledReason = null` - when supplied, the
disabled-click branch uses it instead of the shared
`localPreviewDisabledReason` scratch field. Every one of this helper's
existing ~100 call sites omits the parameter and is completely unaffected
(default `null` preserves the exact prior behavior everywhere else - this
mission touched no local-preview screen).

Construction's own "Terminer"/action button now passes
`() => OfficialBuildingUpgradeStatusText(selectedHotspot.HotspotId)` - the
same real, state-derived status text already shown elsewhere on that same
screen (`OfficialBuildingUpgradeStatusText` already correctly reports
"Le serveur valide le nouveau niveau…" while completing, "Travaux
terminés · validation serveur requise" only while genuinely still awaiting
completion, real remaining-time text while running, etc.) - so if this
button is ever clicked while legitimately disabled again, the message the
player sees is guaranteed to reflect the REAL current official state of
THIS building's own operation, never a stale unrelated scratch value from
anywhere else in the game.

## 3. Failure semantics preserved

Server-required errors still report correctly - `OfficialBuildingUpgradeStatusText`
itself is unchanged; if `buildingUpgradeController` genuinely reports an
`Error` state (a real network/server failure), this function already
returns `OfficialBuildingUpgradeErrorText(model.ErrorCode)`, the real error
text, exactly as before. Only the *source* the disabled-click fallback
reads from changed for this one button - not what counts as disabled, not
completion semantics, not the server action itself.

## 4. M045F compatibility

Not touched. `BuildingInteractionController.InteractionPreemptionHook`/
`DispatchClick` (click-priority routing) and this mission's fix (button
label/feedback text) are in completely separate code paths - a click
completed via the world-tap path never reaches `DrawPreviewActionButton`
at all (it goes straight to `RunOfficialBuildingUpgradeAction` via
`TryCompleteReadyBuildingUpgradeOnTapForExternalHost`), and a click on the
Construction screen's own button still goes through M045F's `DispatchClick`
gate first for world-raycast purposes, unaffected by this UI-layer change.

## 5. Regression

Unity compile: **0 errors** (`assets-refresh` confirmed clean after the
edit). Tests were **not executed live** this mission: the Editor was found
with Play Mode active and paused (likely the CEO's own session) both before
and after this fix - running EditMode tests risks disturbing that
preserved state, so this was deliberately skipped, consistent with the
same judgment call made in M045E-CL/M045F-CL. The change is small and
additive (one new optional parameter, default-null everywhere else, one
call site updated) with no plausible mechanism to regress the ~100
untouched call sites; recommend `HiveBuildingUpgradeClientTests` re-run
once the Editor is free.

## 6. Follow-up not done (out of this mission's scope)

Research (`DrawOfficialResearchMenuPanel`) and Training/Barrack share the
exact same `DrawPreviewActionButton`/shared-scratch-field architecture and
could theoretically surface the same class of stale message on their own
completion buttons. Not touched here - this mission was scoped to the
reported Construction/Caserne bug only ("No unrelated changes"). Flagging
for a future pass if the same symptom is ever reported on those screens.

---

## Final checklist

| # | Question | Answer |
|---|---|---|
| A | Exact source of "serveur requis" proven? | Mechanism proven architecturally (shared, multi-writer `localPreviewDisabledReason` scratch field read by an unrelated disabled-click handler); the exact single originating write on the CEO's specific occurrence could not be retroactively pinned (state had already moved on by the time of investigation) — see section 1 for what was definitively ruled out |
| B | Successful completion path no longer emits it? | YES — Construction's action button now sources its disabled-reason from real, state-derived official status text, never the shared scratch field |
| C | Real server-required failure still reports correctly? | YES — `OfficialBuildingUpgradeErrorText`/error-state handling unchanged |
| D | Building completion semantics unchanged? | YES — no change to `RunOfficialBuildingUpgradeAction`, `Complete()`, or any server call |
| E | Unity compile green? | YES |
| F | Tests green? | Not executed live (Play Mode active/paused, preserved deliberately) — compile confirmed clean |
| G | READY FOR CEO RETEST? | YES |
