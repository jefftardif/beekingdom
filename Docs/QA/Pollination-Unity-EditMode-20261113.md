# BeeKingdom - Pollination Unity validation (2026-11-13)

## Actual PCJEFF Editor Test Runner evidence
Unity 6000.5.3f1, main local working tree, authenticated production
configuration with pollination activated but client mutations disabled.

EditMode groups run through the Unity TestRunnerApi in the existing Editor:
- PollinationP5EditorTests: 4 passed, 0 failed
- PollinationPremiumWindowEditorTests: 7 passed, 0 failed
- PollinationFloralArtworkEditorTests: 1 passed, 0 failed
- PollinationReadOnlyIntegrationTests: 3 passed, 0 failed

**Total 15 passed, 0 failed, 0 skipped.**

Result recorded 2026-11-13 21:53:07 UTC.
Original local evidence:
C:\projets\beekingdomgame-master\Temp\BKPollinationEditMode.results.txt
and C:\projets\beekingdomgame-master\Logs\Editor.log.

Tests check premium UI, floral artwork, authenticated feature flag,
world-map entry-point wiring, and no mutation commands in read-only mode.

## Release gates
- A visual authenticated GameView session has not yet been certified.
- Clean main checkout still requires existing unrelated unpublished
  Chat, Academy and Territory components.
- SRVESDT appeared offline at the time of this review; the last authorized
  read-only SQL check had passed with six tables and migrations 096-098.
- Server P19 write operations must remain disabled until backup-restore,
  settlements and multiplayer acceptance checks are complete.
- This isolated branch is not automatically merged into main.

The one-shot runner is opt-in and scene-safe: it only starts on a
Temp/BKPollinationEditMode.request marker.
## Follow-up PlayMode smoke attempt
A separate one-test PlayMode world-map load was attempted in the active
Unity editor, but did not reach its scene/screenshot assertions. It was
interrupted after getting stuck at the Test Runner SandboxPlayground
transition. No passing PlayMode result is claimed. The EditMode 15/15
result above is complete and independent.
## Real WorldMap runtime observation (2026-11-13)
The existing Unity Editor was safely restored to the official
WorldMapMmoFullscreenFoundation scene (no scene save). A one-time
runtime observer started from the actual configured SandboxPlayground
PlayMode start scene, then loaded the official WorldMap in memory.

The resulting non-mutating observation was:
- SCENE=WorldMapMmoFullscreenFoundation
- MAP_COMPONENT=True
- POLLINATION_OVERLAY=True
- PREMIUM_WINDOW_COMPONENT=True
- BUTTON_RECT_INSIDE_SCREEN=True
- SESSION_CONFIGURED=False
- FEATURE_READY=False
- SITE_COUNT=0
- PRESENTATION_STATE=NotConfigured
- VISIBLE_BUTTON_EXPECTED=False

The pollination components ARE created when the world map is loaded,
but no signed-in player/hive session was available in this QA run.
The button is deliberately hidden while unauthenticated. This is not
a verified authenticated gameplay test.

A 1920x1080 screenshot was saved at:
C:\projets\beekingdomgame-master\Temp\BKPollinationRuntimeProbeMap.png

The observer stopped Play and restored the official map in the Editor.
No fake player, server mutation, SQL write or test rewards were used.