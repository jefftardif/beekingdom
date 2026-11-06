# BeeKingdom — independent Unity regression verification

Date: 5 November 2026
Branch: integration/bk-hud-chat-20261105

## Independent editor compilation

The separate Unity 6000.5.3f1 checkout was compiled using a working Unity Package Manager. The environment required process-local ALLUSERSPROFILE/ProgramData/TMP values because the remote shell omitted ALLUSERSPROFILE; normal UPM startup otherwise crashed in getLocalConfigFolder. No Windows user/security setting was changed.

Compilation completed with editor exit code 0 and generated C# assemblies. The temporary noUpm run was diagnostic only, and is not a valid compilation result because it omitted UI/TextMeshPro packages.

## Regression tests actually run

Batch mode, EditMode:
- WorldMapSharedPanelForegroundTests
- AllianceActivitiesRecoveryTests
- InfirmaryEntryRecoveryTests
- PollinationPremiumWindowEditorTests

Initial: 45 passed / 47, 2 failed due stale Alliance expectations. Confirmed the current code includes a real server-backed read-only Territory eighth tab, and the premium Alliance window now uses a Back button instead of the old Activities header shortcut. Updated the assertions to verify current behavior and read-only handling, not to hide failures.

Second run: **47 passed / 47, 0 failed**.

Additional EditMode suites:
- BKStudioInfirmaryRecoveryTests
- BarrackActivitiesRestorationTests
- SandboxLivingHiveOfficialResearchTests
- WorldMapLocalWorldOccupancyTests
- WorldMapVisualSeparationTests
- RoyalPalacePremiumWindowTests

Result: **45 passed / 45, 0 failed**.

Total independently executed: **92 passed / 92, 0 failed**.

Reports retained outside Git worktree:
C:\BeeKingdom\staging\BK-HudChat-Reconcile-20261105-edit-tests-retry.xml
C:\BeeKingdom\staging\BK-HudChat-Reconcile-20261105-edit-extra.xml
C:\BeeKingdom\staging\BK-HudChat-Reconcile-20261105-unity-ipc-retry.log

## Boundaries and deployment gate

This worktree is a source-only WIP checkpoint. It does not yet contain all uncommitted artwork, package changes, fonts, DLLs or project settings from the active PCJEFF project. Tests do not establish complete visual fidelity, actual Chat connectivity, resource placement geometry in live World Map, bilingual mobile safe areas, Android or integration with SRVESDT.

The production server was NOT changed, and this client branch must not be merged into main or deployed without the missing asset and full gameplay verification.

Unity import deleted many .meta files in this isolated checkout because corresponding art was excluded. These imported/generated worktree changes are **not** part of the checkpoint commit; the tracked files are restored to their committed state after the test run. The original PCJEFF main worktree was not altered.
