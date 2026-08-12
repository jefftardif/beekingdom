# ARCH-184 - DEMO-067 And SERVER-038 Validation Dispatch

Date: 2026-07-12
Status: VALIDATED

## DEMO-067 Validation

DEMO-067 is accepted for QA-A.

Source report:

- `C:\projets\beekingdom\prompt_demo\rapports\DEMO-067_BEE821_827\DEMO-067_Report.md`

Official bundle:

- `C:\projets\beekingdom\prompt_demo\rapports\DEMO-067_BEE821_827\`

Validated scope:

- BEE-821 to BEE-827 only;
- phone portrait readable candidate;
- tablet landscape hive-dominant candidate;
- one-finger pan telemetry candidate;
- two-finger pinch telemetry candidate;
- fixed UI blocks hive gesture candidate;
- rapid tap upgrade guard;
- rapid tap training guard;
- deterministic local checks;
- no BEE-828+;
- no world map expansion;
- no official server progression;
- no official save;
- no official economy;
- no persistent official army.

Known reserve to QA:

- Physical real-device tactile proof remains the main reserve. Existing pan/pinch/UI proof is capture/telemetry candidate, not final physical validation.

Architect decision:

- Send QA-067 immediately.
- Hold Builder-A/B/C/UI-A runtime work until QA-A returns PASS, PASS WITH RESERVES, or BLOCKED.

## SERVER-038 Validation

SERVER-038 is accepted.

Source report:

- `C:\projets\beekingdom\prompt_server\rapports\SERVER-038 - Hive Loop Persistence Readiness Design Report.md`

Validated scope:

- persistence readiness models for `player_resources`, `hive_buildings`, `construction_queue`, `troop_counts`, `training_queue`, `idempotency_records`;
- future transaction policy;
- world/server scope;
- idempotency hash readiness;
- tests reported green;
- no production SQL migration;
- no live endpoint;
- no publish;
- no production write;
- no Unity change.

Architect decision:

- Server-A may continue to SERVER-039 in parallel.
- SERVER-039 must remain local/non-live and focus on SQL opt-in migration planning/dry-run artifacts only.

## Current Gate

Ruche playable product remains the priority.

No world map work is authorized by this validation.
