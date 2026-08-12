# ARCH-211 - QA-073 Reserve Decision and Builder-A Targeted Fix

Date: 2026-07-12

## Decision

QA-073 is accepted with reserves.

QA report:

- `C:/projets/beekingdom/QA/QA_DEMO_073_BEE901_920_VALIDATION.md`

QA result:

- `QA_073_RESULT = PASS_WITH_RESERVES`

Architect decision:

- Do not start a broad new Planner wave yet.
- First close the two local/runtime reserves that are directly actionable by Builder-A.
- Keep physical device proof as a separate later gate requiring real device evidence.

## Accepted Progress

QA confirms:

- Upgrade completion visible.
- Resource/cap/reserved-cost clarity.
- Buttons non-mute.
- Refusal recovery.
- One-finger pan proof.
- Two-finger pinch proof.
- HUD/panels/navigation fixed.
- JSON fallback acceptable for this gate.
- Non-claims intact.

## Targeted Fixes Required Now

### Fix 1 - BEE-905 Manifest Coherence

Problem:

- Visual proof shows `Resultat: +6 Eclaireuses`.
- Local army `Ecl.` count is visible at `11`.
- Batch/runtime proof expects training arrival.
- Source manifest still says:
  - `training_arrival_visible:false`
  - `training_delta:none`
  - preserved army count still lists old `Eclaireuses 5`

Required:

- Export/manifest must match visual/runtime proof.
- Training arrival fields must be coherent.

### Fix 2 - UI-Button Gesture Blocking Proof

Problem:

- Pan/pinch proof passes.
- HUD/panels/navigation fixed pass.
- Manifest still says:
  - `gesture_ui_blocks_hive:False`
  - `fixed_ui_blocks_hive_gesture:False`
- No dedicated proof shows that UI button gestures do not pan/zoom the hive.

Required:

- Add explicit proof that UI button tap/drag blocks hive pan/zoom.
- Manifest and machine-readable evidence must report this as true if proven.

## Physical Device Reserve

Physical phone/tablet proof remains open.

Do not claim it is closed unless real device evidence exists.

## Scope Guard

Still forbidden:

- BEE-881 unlock or implementation.
- World map runtime.
- Exploration.
- Alliance/war/MMO map.
- Official live server claim.
- Official endpoint.
- Official save/economy/persistent army.

## Dispatch

Builder-A is authorized to do the targeted correction.

Demo-A waits for Builder-A.

QA-A waits for Demo-A.

## Required Builder-A Output

- `C:/projets/beekingdom/prompts_codex/rapports/BuilderA_BEE905_910_TargetedReserveFix_Report.md`
- Updated source bundle under:
  - `C:/projets/beekingdom/prompt_demo/rapports/DEMO-074_BEE905_910_TargetedFix_Source/`

Final line:

- `READY_FOR_DEMO_074_TARGETED_FIX = YES` or `NO`

