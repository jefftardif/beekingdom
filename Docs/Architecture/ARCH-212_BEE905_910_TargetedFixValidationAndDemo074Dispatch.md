# ARCH-212 - BEE-905/BEE-910 Targeted Fix Validation and DEMO-074 Dispatch

Date: 2026-07-12

## Decision

Architect validates Builder-A targeted reserve fix for DEMO-074 intake.

Builder-A report:

- `C:/projets/beekingdom/prompts_codex/rapports/BuilderA_BEE905_910_TargetedReserveFix_Report.md`

Builder-A status:

- `READY_FOR_DEMO_074_TARGETED_FIX = YES`

## Validated Fixes

### BEE-905 Manifest Coherence

Validated:

- `training_arrival_visible:true`
- `training_delta:+6 Eclaireuses`
- `local_army_counts:Soldats 18 / Gardiennes 8 / Eclaireuses 11`
- `manifest_contradiction_closed:true`
- No old `Eclaireuses 5` claim remains for the corrected training-arrival proof.

### BEE-910 UI-Button Gesture Blocking

Validated:

- `gesture_ui_blocks_hive:True`
- `fixed_ui_blocks_hive_gesture:True`
- `ui_button_blocks_hive_gesture:true`
- `hive_pan_delta_after_ui_drag:0,0`
- `hive_pinch_delta_after_ui_drag:0`
- `hive_zoom_changed_by_ui_drag:false`

## Source Bundle

- `C:/projets/beekingdom/prompt_demo/rapports/DEMO-074_BEE905_910_TargetedFix_Source/`
- `DEMO-074_BEE905_910_TargetedFix_Manifest.md`
- `DEMO-074_BEE905_910_TargetedFix_MachineReadableSummary.json`

## Remaining Out-of-Scope Reserve

Physical device proof remains open. DEMO-074 must not claim real phone/tablet proof unless real device evidence exists.

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

Demo-A is authorized to run DEMO-074 targeted validation.

QA-A waits for Demo-A `READY_FOR_QA_074 = YES`.

## Required Demo Output

- `C:/projets/beekingdom/prompt_demo/rapports/DEMO-074_BEE905_910_TargetedFix/DEMO-074_Report.md`

Final line:

- `READY_FOR_QA_074 = YES` or `NO`

