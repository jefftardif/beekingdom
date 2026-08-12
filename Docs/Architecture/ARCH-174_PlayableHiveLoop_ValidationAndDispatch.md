# ARCH-174 - Playable Hive Loop Validation And Dispatch

Date: 2026-07-12
Role: Architect
Status: pass with reserves, dispatch active

## Context

The project was drifting too much toward visual proof and world-map work while the Hive was still not playable enough. The current priority is the player-facing Hive loop:

- resources increase;
- a building can be selected;
- a building can be upgraded locally;
- costs, duration, progress and level changes are visible;
- troops can be trained locally;
- buttons must not be mute;
- tablet landscape and phone portrait must remain readable;
- no local preview may claim official MMO/server progression.

## Inputs Reviewed

- `C:/projets/beekingdom/prompts_codex/rapports/BuilderA_PlayableHiveLoop_Report.md`
- `C:/projets/beekingdom/prompt_demo/rapports/DEMO-064_PlayableHiveLoop/PlayableHiveLoop_Manifest.md`
- `C:/projets/beekingdom/prompt_demo/rapports/DEMO-064_PlayableHiveLoop/PlayableHive_TabletLandscape_1920x1200.png`
- `C:/projets/beekingdom/prompt_demo/rapports/DEMO-064_PlayableHiveLoop/PlayableHive_Upgrade_Strip.png`
- `C:/projets/beekingdom/prompt_demo/rapports/DEMO-064_PlayableHiveLoop/PlayableHive_Training_Strip.png`
- `C:/projets/beekingdom/prompt_ui/rapports/UI-060_PLAYABLE_HIVE_LOOP_UX_SPEC.md`
- `C:/projets/beekingdomgame-master/Docs/BuilderB/PlayableHiveLoop_Support.md`
- `C:/projets/beekingdomgame-master/Docs/BuilderC/PlayableHiveLoop_TestMatrix.md`
- `C:/projets/beekingdom/prompt_server/rapports/SERVER-032 - Playable Hive Loop Future Readiness Contracts Report.md`

## Validation

Builder-A produced a valid local playable Hive candidate. The proof shows:

- resource values changing;
- upgrade state before / running / complete;
- level increase after upgrade;
- cost removal after upgrade;
- training queue and troop count increase;
- selected-zone halo and detail panel;
- explicit non-official / preview state;
- tablet landscape and phone portrait captures.

This is not a finished MMO loop. It is a local preview loop that proves the product direction is becoming interactive instead of static.

## Reserves

- The right panel is still visually dense.
- Some text is small or cramped.
- The proof is not a final QA pass.
- The loop is not server-authoritative.
- There is no save, no official economy, no official army persistence.
- Gesture proof still needs formal validation, especially pan versus pinch on tablet.

## Decision

Pass with reserves.

The work is good enough to move to Demo-A for official evidence and QA-A after Demo-A. Builder-A must not receive a new runtime modification task until Demo-A has captured the proof and QA-A has reviewed it.

## Dispatch Rules

- Demo-A: run official proof pass now.
- QA-A: wait for Demo-A output.
- UI-A: refine UX constraints for the next Builder pass, especially readability and non-mute buttons.
- Builder-B: support only, no runtime conflict.
- Builder-C: checklist and regression matrix only, no runtime conflict.
- Server-A: continue future server contract work, no Unity dependency.
- Planner: plan next BEE wave around Hive playable loop completion before world-map expansion.
- Builder-A: hold until Demo-A and QA-A results are available.

## Next Product Priority

The next accepted runtime pass must make the Hive feel like a small working game:

1. the player sees resources increase;
2. the player upgrades a selected building;
3. the player trains basic troops;
4. every visible button responds or explains why it cannot;
5. the interface remains readable on tablet landscape and phone portrait.

