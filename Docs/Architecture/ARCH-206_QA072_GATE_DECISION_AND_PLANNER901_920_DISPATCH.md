# ARCH-206 - QA-072 Gate Decision and Planner BEE-901 to BEE-920 Dispatch

Date: 2026-07-12

## Decision

QA-072 is accepted with reserves.

Gate decision:

- BEE-882 to BEE-886 local playable Hive loop: accepted.
- BEE-887 to BEE-900 support material: accepted as support only.
- BEE-881: still blocked.
- World map: not validated and not authorized for the next wave.

QA result:

- `QA_072_RESULT = PASS_WITH_RESERVES`

QA report:

- `C:/projets/beekingdom/QA/QA_DEMO_072_BEE882_900_VALIDATION.md`

## What Is Now Proven

The local playable Hive proof can demonstrate:

- Resource production/increase.
- Produce/spend loop.
- Reserved cost.
- Single-cost application guard.
- Upgrade decision/pending/refusal feedback.
- Training queue.
- Local troop arrival and local army snapshot.
- Non-mute buttons through accepted/refused/pending/server-required states.
- Refusal recovery guidance.
- Local/dev-only action source.
- Phone portrait usability with reserve.

## What Is Not Proven

The following must remain explicit non-claims:

- No official live server.
- No official endpoint.
- No official save.
- No official economy.
- No official persistent army.
- No BEE-881.
- No world map runtime validation.
- No exploration, alliance, war or MMO map.

## Active Reserves

These are non-blocking for QA-072 but must guide the next wave:

1. Structured machine-readable test output is missing.
   - Restore NUnit XML or produce equivalent JSON.
2. Physical device proof is not closed.
   - Phone portrait and tablet landscape must be tested on-device or documented with explicit limitation.
3. Portrait is usable but dense.
   - Improve comfort and hierarchy without hiding permanent HUD/menu surfaces.
4. Upgrade completion proof is weaker than training arrival proof.
   - Add a dedicated visible upgrade completion proof.
5. BEE-896 to BEE-900 remain support/protocol only.
   - They do not close final device gate readiness.

## Dispatch to Planner

Planner is authorized to compose BEE-901 to BEE-920.

Strict priority:

- Playable Hive product only.
- Player-visible function over architecture breadth.
- No world map.
- No BEE-881.
- No official live claims.

The next wave must focus on closing QA-072 reserves and making the player loop feel more like a real product:

- Structured test output.
- Physical device validation readiness.
- Portrait/tablet layout comfort.
- Dedicated upgrade completion proof.
- Action button reliability.
- Resource growth clarity.
- Upgrade affordance and confirmation.
- Training/troop feedback clarity.
- Army panel/local preview clarity.
- Recovery after refusal.

## Required Planner Output

Planner must create:

- `C:/projets/beekingdom/prompts_codex/BEE-901_*.md` through `BEE-920_*.md`
- `C:/projets/beekingdom/prompts_codex/rapports/Planner_BEE901_920_Report.md`

Each BEE must include:

- Pillar.
- Teams concerned matrix: Builder-A, Builder-B, Builder-C, Server-A, Demo-A, QA-A, UI-A/UI-B.
- Impact Demo.
- Impact UI.
- QA acceptance criteria.
- Explicit non-claims.

Final line required:

- `BEE-920_READY_FOR_ARCHITECT_VALIDATION = YES` or `NO`

