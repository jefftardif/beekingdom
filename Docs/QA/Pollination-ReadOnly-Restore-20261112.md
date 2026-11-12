# BeeKingdom - Pollination read-only restoration (2026-11-12)

Branch: integration/pollination-readonly-20261112
Base: main a36d771f

## Root cause
The Unity runtime configuration lacked pollinationFeatureEnabled, defaulting to false.
Pollination client code and artwork were present only as untracked PCJEFF work.
The editor-only opt-in marker had been deleted, hiding the world-map entry.

## Recovery
- The authenticated client, premium window, data projection and sprites are restored on this isolated branch.
- The serialized pollination flag is enabled for an authenticated read-only feature.
- Client-side writes are explicitly disabled: start cohort, start expedition, create alliance floral project.
- World-map IMGUI and gesture handling yield to the premium pollination modal.
- The official PCJEFF workspace is already enabled and compiles with zero C# errors.

## Verification and limits
- SRVESDT health HTTP 200.
- Floral catalogue returns HTTP 401 without authentication, so the endpoint is active.
- Civil expedition endpoint returns HTTP 503 game.civil_expeditions_disabled.
- Six SQL pollination tables and migrations 096, 097 and 098 exist; read-only authorized SQL gate passed.
- A clean main compile is blocked by pre-existing untracked dependencies from chat, academy and territory work. This candidate must not be merged blindly.
- Unity Test Runner and GameView visual confirmation remain pending.
- No IIS binaries, production SQL, or server feature flags were changed.

## Before enabling server mutations
Follow P19 readiness requirements: independently verified SQL restore, mission settlement validation, authenticated ops checks and multi-player acceptance.