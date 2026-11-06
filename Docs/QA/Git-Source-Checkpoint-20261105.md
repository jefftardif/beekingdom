# BeeKingdom - Git Source Checkpoint (5 November 2026)

## WIP only: not validated as deployable

This branch records in-flight PCJEFF Unity source without changing the active main worktree.

- Base: origin/main d74d8aa95c579ce8610e89dcfaa11216ede4bec8
- Included: cherry-picked World Map Plus fix, originally 610421f9.
- Copied: 30 modified tracked C# files, 77 untracked C# files, 77 related Unity .meta files, and 2 FR/EN localization JSON files.
- The Plus component, test and existing GUIDs were preserved from the earlier tested commit.
- All copied files were hashed and rechecked to detect concurrent edits.
- File hashes and group assignments: SourceSnapshot-20261105.json and SourceSnapshot-Groups-20261105.json.
- Initial scan of known hard-coded token patterns in copied C# matched zero files. This does not replace a security review.

## Deliberate exclusions

This is NOT a runnable Unity release. The active PCJEFF worktree still contains:
- Untracked artwork and Resources assets (approximately 193 files under Playground/Resources), plus other Unity assets and metadata.
- Modified font assets, NuGet plugin DLLs, package versions and ProjectSettings. These need independent compatibility review.
- Server changes, SQL migration scripts and production appsettings, which have NOT been staged or changed by this checkpoint.

No original uncommitted work has been deleted, reset, stashed, or overwritten.

## Mandatory regression gates before merging main

1. Independently compile the branch with Unity 6000.5.3f1 and compatible locked packages.
2. Audit and integrate exact artwork references, texture sizes, GUIDs, fonts, sprites and Android assets.
3. Re-run Hud/Chat/Plus tests and actual PlayMode flows: Plus visible on top of map, X/Escape/outside close, Chat returns, Bag, Settings, Army.
4. Re-run Alliance, infirmary, nursery, research, barracks and building tests, on desktop and mobile with FR/EN.
5. Verify map zoom stability, click geometry, land/water placement, and server-owned reservations.
6. Verify server API compatibility against the actual SRVESDT build.
7. Review secrets/security and any additional untracked dependency before integration.
8. Only then merge validated changes. Never deploy this WIP checkpoint to production.

The original PCJEFF main was NOT edited. New integration worktree:
C:\BeeKingdom\staging\BK-HudChat-Reconcile-20261105
