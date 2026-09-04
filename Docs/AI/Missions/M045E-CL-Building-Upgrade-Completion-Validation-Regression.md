# M045E-CL — Building Upgrade Completion Validation Regression

Real human runtime blocker: Jeff's Réserve de miel upgrade (Niv. 5→6)
finished (Construction queue shows "À valider") but no way to validate it
existed anywhere in the UI. The real operation was never touched, mutated,
completed, or reset by this investigation.

## 1. Current real operation — confirmed, not mutated

Not re-queried directly (no SQL/reflection used, per the mission's explicit
ban) - accepted as reported and cross-checked against the code path that
produces exactly that on-screen text: `HiveBuildingUpgradeScreenModel.CanComplete("honey_storage")`
is true whenever `ActiveOperation.Status == AwaitingCompletionStatus` and
`ActiveOperation.BuildingKey == "honey_storage"` - the server itself is the
one asserting "ready", the client only reflects it. No write was ever sent.

## 2. Concurrent M046-CX work — checked first, respected

`git status` before any edit showed uncommitted M046-CX changes to
`BuildingSelectionHighlight.cs`, `HiveMapBuildingUpgradeVisualStateBootstrap.cs`,
`HiveViewProductUiPresenter.cs`, and their own test file - plus their own
report (`Docs/AI/Missions/M046-CX-Building-Upgrade-Outline-Pulse.md`), read
in full before touching anything.

M046-CX added a subtle pulse to the existing blue "upgrading" outline and
explicitly stops that pulse once `ActiveOfficialUpgradeHotspotIdForExternalHost()`
returns null (which happens exactly when the operation reaches
`AwaitingCompletionStatus`) - their own report says the pulse stopping is
meant to let "the existing ready-to-validate feedback... remain the
dominant completion signal," but their own checklist honestly states this
was never visually verified (`M. Play Mode visually verified? NO`).

**No conflict**: this mission's changes only *add* new methods
(`ReadyToCompleteOfficialUpgradeHotspotIdForExternalHost`,
`TryCompleteReadyBuildingUpgradeOnTapForExternalHost`,
`DrawBuildingUpgradeReadyBadgeForExternalHost`) and a new `OnGUI`/click
subscription in the bootstrap M046-CX already touches - CX's own pulse
`Update()` logic was not modified. No reset/stash/checkout/clean was
performed on anyone's work.

## 3. Root cause — two real, distinct gaps, both proven by code trace

**A) The Construction screen itself had no way to reach its own "Terminer" button once ready.**

`DrawConstructionOverlayForExternalHost` (`HiveViewProductUiPresenter.cs`)
is the real screen the bottom-rail "Construction" button opens (traced and
confirmed in M045D-CL against this exact CEO's screenshot text). Its
`runningHere` flag was `ActiveOperation != null && BuildingKey ==
selectedHotspot.HotspotId` - **true regardless of whether the operation
was still running or already `AwaitingCompletion`**. Whenever `runningHere`
is true, the screen unconditionally draws the "Amelioration en cours :
100%" progress display; the `else if` branch that already contains the
real "Terminer" button (`OfficialBuildingUpgradeActionLabel` /
`RunOfficialBuildingUpgradeAction` -> `buildingUpgradeController.Complete()`)
was structurally unreachable once an upgrade finished. **This is the exact
failing condition** - not a missing feature, a pre-existing gate that never
distinguished "still running" from "done, awaiting validation."

**B) No world-marker/tap-to-validate exists for building upgrades in HiveMap.**

The mission's "previously validated," "dedicated visual asset/indicator"
is `DrawBuildingUpgradeReadyMarkers` (draws an "upgrade-ready" icon) +
`TryCompleteReadyBuildingUpgradeOnTap` (completes on a direct building
tap) - both real, both still present in the code, both still fully
functional... but both live entirely inside the **legacy reference-hotspot
renderer** (`DrawReferenceHotspots`, flat-image coordinate space,
`ReferencePoint`/`artRect`) - the LivingHive-only rendering path that
CLAUDE.md's standing rule (2026-09-03) says must never be used again. This
marker was **never ported to HiveMap** in the first place - not a
regression caused by any recent mission, a gap that predates the
LivingHive ban and was simply never noticed because Jeff's own testing
moved to HiveMap before this exact scenario (a *production* building's
upgrade finishing while the player is not already on the Construction
screen) came up.

Training already has the HiveMap-native equivalent
(`TryClaimReadyTrainingOnTapForExternalHost` /
`DrawTrainingReadyBadgeForExternalHost`, wired into
`HiveMapBarrackBootstrap`) - building upgrades never got the same
treatment.

## 4. Answering the mission's specific gating questions

- **A) Completion-ready state not reaching the visual layer?** No - the
  server truth (`CanComplete`/`IsAwaitingCompletion`) was always correct
  and already read by the code; the gap was in what the UI *did* with it.
- **B) Building-id mapping failure?** No - `selectedHotspot.HotspotId` /
  `ActiveOperation.BuildingKey` comparison was already correct and unchanged.
- **C-F) Disabled GameObject / offscreen / sorting / suppressed by
  another overlay?** Not applicable to the primary cause - no GameObject-based
  indicator existed in HiveMap to be disabled or mispositioned in the
  first place (root cause B, section 3).
- **G) Recent M045/M045B/M045D changes changed the presenter path?**
  No - M045D added `DrawAllianceHelpAction` inside the SAME `if
  (runningHere)` block, correctly gated on `!IsAwaitingCompletion` already
  (it never showed while awaiting completion) - M045D did not cause or
  worsen this bug, it happened to be edited adjacent to it.
- **H) M046/CX touched the same architecture?** Yes, the outline pulse
  bootstrap - correctly stops on completion, does not conflict, see
  section 2.
- **I) An old "hide world overlays while UI open" rule suppressing it?**
  No such rule was ever reached, since nothing drew the badge in HiveMap
  before this fix.
- **J) Callback/event subscription lost?** No prior subscription existed
  to lose - this is a net-new addition (section 5).

## 5. Fix

**A) `DrawConstructionOverlayForExternalHost`** - `runningHere` (official
branch) now additionally requires `!ActiveOperation.IsAwaitingCompletion`.
Once an operation finishes, execution now correctly falls through to the
pre-existing `else if (DrawPreviewActionButton(...))` branch, which was
never touched or reimplemented - it already computed the right label
("Terminer") and already called the right real action
(`RunOfficialBuildingUpgradeAction` -> `buildingUpgradeController.Complete()`).
No new button, no new server call - the existing one simply became
reachable. Works for any building, not a Réserve de miel special case (no
hardcoded key anywhere in the change).

**B) HiveMap world badge + tap-to-validate**, added to
`HiveMapBuildingUpgradeVisualStateBootstrap.cs` (the same bootstrap
M046-CX's pulse already lives in, extended without touching their `Update()`
pulse logic):

- `HiveViewProductUiPresenter.ReadyToCompleteOfficialUpgradeHotspotIdForExternalHost()`
  - companion to CX's `ActiveOfficialUpgradeHotspotIdForExternalHost()`,
    returns the building key only while genuinely `AwaitingCompletion`.
- `TryCompleteReadyBuildingUpgradeOnTapForExternalHost(hotspotId)` - thin
  public wrapper around the pre-existing, unchanged
  `TryCompleteReadyBuildingUpgradeOnTap`, which already calls the real
  `RunOfficialBuildingUpgradeAction`/`Complete()` path.
- `DrawBuildingUpgradeReadyBadgeForExternalHost(rect, time, glowSize)` -
  new pulsing "upgrade-ready" icon, same shape/sizing convention as the
  already-shipped `DrawTrainingReadyBadgeForExternalHost`.
- The bootstrap now subscribes to `BuildingInteractionController.Selection.BuildingClicked`
  (completing on tap when the clicked building matches the ready hotspot)
  and draws the badge in `OnGUI`, respecting `HiveMapOverlayInputGateBootstrap.IsAnyOverlayBlocking()`
  (M044's shared occlusion gate - the badge never draws while a full-screen
  overlay is open, same rule every other HiveMap world marker already
  follows).

No hardcoded building key, no special case for Jeff, no special case for
level 5→6 in either fix.

## 6. State transition - unchanged, still correct

Running → (CX's pulse, unaffected) → 100% reached / awaiting completion →
pulse stops (CX, unaffected) + ready badge appears (this mission) → CEO
taps badge or opens Construction and taps "Terminer" (either path calls
the same real `Complete()`) → level increments server-side → operation
clears → both the button and the badge naturally disappear next frame
(their gating conditions, `CanComplete`/`IsAwaitingCompletion`, become
false) → building returns to idle. No auto-validation was added - the
timer reaching zero alone changes nothing; an explicit tap/click is still
required, exactly as the mission requires.

## 7. Rendering / occlusion

The new badge reuses the exact same `ScreenRectFor` world→screen
projection and `HiveMapOverlayInputGateBootstrap.IsAnyOverlayBlocking()`
gate every other HiveMap world marker (`HiveMapProductionBootstrap`,
`HiveMapBuildingUpgradeClickBootstrap`'s prerequisite glow) already uses -
no new rendering technique, no old hack reintroduced.

## 8. Tests

Unity's own EditMode/PlayMode test execution was **deliberately not run**
this mission: the Editor was found with Play Mode active and paused
(`IsPlaying: true, IsPaused: true`) when checked - almost certainly the
CEO's own preserved reproduction session. Running tests risks exiting or
disturbing Play Mode, directly contradicting this mission's explicit
"preserve current state" instruction. Compilation was confirmed clean
(`assets-refresh` succeeded with 0 errors) before and after every edit.
CX's own test file was read and confirmed to only add new tests (58
insertions, zero modifications to existing ones) - nothing this mission
touched could regress them, and this mission's own changes were not
covered by new automated tests for the same reason (would require
entering/exiting Play Mode).

Recommended once the CEO's session is free: re-run
`SandboxLivingHiveBuildingUpgradeTests` and `HiveBuildingUpgradeClientTests`.

## 9. Scope discipline

No building cost, upgrade duration, Alliance Help balance, Alliance
membership, persistence architecture, FTUE, WorldMap, LivingHive, or Build
Settings was touched.

## 10. Deployment

Unity-side only. No server code read or touched. No deployment needed.

---

## Final checklist

| # | Question | Answer |
|---|---|---|
| A | Current real operation confirmed completion-ready? | YES (accepted from server-truth-driven `CanComplete`, never re-mutated or re-queried) |
| B | Existing validation indicator implementation found? | YES — `DrawBuildingUpgradeReadyMarkers`/`TryCompleteReadyBuildingUpgradeOnTap` (legacy, LivingHive-only, never ported) |
| C | Existing asset/path reused? | YES — same "upgrade-ready" icon, same `TryCompleteReadyBuildingUpgradeOnTap`/`RunOfficialBuildingUpgradeAction`/`Complete()` call chain |
| D | Exact reason indicator disappeared proven? | YES — two causes: (1) the Construction screen's own gate never let its own existing "Terminer" button become reachable once ready; (2) the legacy world-marker was never ported to HiveMap |
| E | Building ID mapping correct? | YES — no mismatch anywhere in either fix |
| F | Render/sorting state correct? | YES — reuses the established HiveMap marker rendering/occlusion pattern |
| G | Real validation handler still exists? | YES — `RunOfficialBuildingUpgradeAction`/`buildingUpgradeController.Complete()`, unchanged |
| H | Restored icon invokes real server validation? | YES |
| I | M046 conflict detected? | NO — read CX's uncommitted work first, extended without touching their pulse logic, no overwrite |
| J | M044 occlusion respected? | YES |
| K | Building upgrade tests green? | Not re-run this mission (Play Mode active/paused, preserved deliberately) — compile confirmed green |
| L | Unity compile green? | YES |
| M | READY FOR CEO CLICK ON VALIDATION ICON? | YES |

READY FOR CEO — CLICK THE BUILDING UPGRADE VALIDATION ICON ONCE.
