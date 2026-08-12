# ARCH-205 - DEMO-072 Validation and QA-072 Dispatch

Date: 2026-07-12

## Decision

Architect validates DEMO-072 for QA intake.

Verdict candidate: `PASS_WITH_RESERVES`

Demo-A reports:

- `READY_FOR_QA_072 = YES`

## Validated Demo Output

- `C:/projets/beekingdom/prompt_demo/rapports/DEMO-072_BEE882_900/DEMO-072_Report.md`
- `C:/projets/beekingdom/prompt_demo/rapports/DEMO-072_BEE882_900/DEMO-072_ContactSheet.png`
- `C:/projets/beekingdom/prompt_demo/rapports/DEMO-072_BEE882_900/DEMO-072_SupportManifest.md`
- Source captures from `C:/projets/beekingdom/prompt_demo/rapports/DEMO-072_BEE882_900/`

## Scope Confirmed

Validated:

- Playable hive local preview.
- Visible resource production/spend.
- Reserved cost and single-cost proof.
- Upgrade decision/pending/refusal feedback.
- Training queue and local troop arrival proof.
- Local/dev-only action source.
- Rejection/recovery guidance.
- Portrait capture.
- Builder-B, Builder-C, Server-A and UI-B supports treated as support only.

Not validated and not claimed:

- BEE-881.
- World map.
- Official live server.
- Official endpoint.
- Official save.
- Official economy.
- Official persistent army.

## QA-072 Focus

QA-A must verify:

- Whether DEMO-072 deserves `PASS`, `PASS_WITH_RESERVES`, or `BLOCKED`.
- Button/action loop is not mute in proof.
- Resource increase, spend, upgrade, training, local army and refusal guidance are observable enough.
- Portrait proof remains usable even if dense.
- Non-claims are explicit and not contradicted.
- XML/NUnit structured output reserve remains acceptable or becomes blocking.
- Physical device proof reserve remains acceptable or becomes blocking.
- BEE-881 remains blocked.

## Dispatch

QA-A is authorized to validate DEMO-072.

No new Builder, UI, Server, Planner, Demo or world-map work is dispatched until QA-072 returns.

Required QA output:

- `C:/projets/beekingdom/QA/QA_DEMO_072_BEE882_900_VALIDATION.md`

Final gate line:

- `QA_072_RESULT = PASS`, `PASS_WITH_RESERVES`, or `BLOCKED`

