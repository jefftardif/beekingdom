# M047-CX - HiveMap Player Profile Migration

Date: 2026-09-04  
Agent: Architecte / Codex  
Repository: `C:/projets/beekingdomgame-master`

## Objective

Restore the player identity click flow in HiveMap:

1. click the top-left player identity / `Reine` HUD area;
2. open a clean compact player summary;
3. expose a visible `Profil` button;
4. open the detailed player profile view;
5. keep queues, world buildings, collection icons and ambient bees underneath the foreground UI.

LivingHive remains retired. No scene routing, BuildSettings change or runtime dependency on `Assets/Scenes/LivingHive.unity` was introduced.

## Historical Behavior Found

The historical/player-facing behavior is in `HiveViewProductUiPresenter`:

- `playerMenuOpen` was toggled by the top-left player HUD.
- `DrawTopHudOverlays()` drew the player panel after the HUD.
- The existing detailed content was `DrawPlayerPanel`: level card, statistics row, strategic path row and strategic profile rows.
- The old proof rows already documented the intent:
  - `player_button_top_left:true`
  - `player_button_contains:statistics,level,skills`
  - `player_menu_premium:header_band,level_card,stat_cards`
  - `top_dropdown_layer:above_queue_timers_and_hud`
  - `player_profile_rows:level,statistics,skills_non_overlapping`

The recovered detailed profile is therefore the existing player/profile panel content, migrated behind a compact summary and opened through the new `Profil` action.

## Broken HiveMap Path

The HiveMap runtime already toggled `playerMenuOpen`, but several external systems did not know that this state was an overlay:

- `HiveMapQueueSidebarBootstrap` did not include player summary/profile state in its `anyOverlayOpen` check.
- `HiveMapOverlayInputGateBootstrap` did not include player summary/profile state in `IsAnyOverlayBlocking()`.
- `HiveMapUiOcclusion` did not treat the player panel as an opaque UI rect.
- `ShouldBlockUnderlyingHiveChromeInput()` and `PremiumUiBlocksWorldInput()` did not include `playerMenuOpen`.

Root cause: the player panel was a monolith-local dropdown state, not fully exported to the HiveMap multi-bootstrap overlay architecture. As a result, the queue sidebar and world input could continue to draw/react over or under the panel.

## Implementation

`playerMenuOpen` now represents the compact Player Summary.

New state:

- `playerProfileOpen`

New runtime exposure:

- `PlayerSummaryOverlayOpenForExternalHost`
- `PlayerProfileOverlayOpenForExternalHost`

New proof/layout APIs:

- `PlayerSummaryPanelRectForProof(...)`
- `PlayerProfilePanelRectForProof(...)`
- `OpenPlayerSummaryForProof()`
- `OpenPlayerProfileFromSummaryForProof()`
- `PlayerSummaryForProof()`
- `TrimPlayerSummaryTextForProof(...)`

The summary shows:

- queen/avatar icon;
- real session `DisplayName` when an authenticated session exposes it;
- local display-name fallback when no authoritative session display name is available;
- royal core level;
- computed current power;
- session label;
- visible `Profil` button.

The detailed profile reuses the existing player profile content and remains within HiveMap production presentation.

## Layering And Input

Updated:

- `HiveMapQueueSidebarBootstrap`
- `HiveMapOverlayInputGateBootstrap`
- `HiveMapUiOcclusion`
- `HiveViewProductUiPresenter.ShouldBlockUnderlyingHiveChromeInput()`
- `HiveViewProductUiPresenter.PremiumUiBlocksWorldInput()`
- `HiveViewProductUiPresenter.FixedHiveUiRects(...)`

Behavior:

- queue sidebar hides while summary/profile is open;
- building/world input is blocked while summary/profile is open;
- M044 occlusion clips world presentation against the summary/profile rects;
- markers/bees are not globally disabled, only visually occluded by the opaque UI region;
- Escape/back closes summary/profile through the same premium-screen path.

## Text Layout

Summary labels use bounded rects, clipped GUI styles and `TrimForUi(...)`.

Proof checks cover:

- landscape summary/profile rects inside `1280x720`;
- portrait summary/profile rects inside `390x844`;
- long name trimming bounded to the requested character budget.

## Tests And Verification

Added focused assertions in:

- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveUiStabilizationTests.cs`

Coverage:

- player summary opens;
- summary exposes `Profil`;
- `Profil` opens the detailed profile and closes the summary;
- profile close restores closed state;
- player summary/profile block world input;
- rectangles stay inside landscape and portrait screens;
- long summary text is bounded;
- no LivingHive scene dependency is introduced.

Compilation:

- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal /clp:ErrorsOnly`
  - Passed: 0 errors, 238 warnings.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal /clp:ErrorsOnly`
  - Passed: 0 errors, 141 warnings.

Unity Test Runner:

- Command attempted with Unity `6000.5.3f1`, matching `ProjectSettings/ProjectVersion.txt`.
- Unity aborted because another Unity instance already had this project open.
- No XML result file was produced.

Play Mode:

- Not visually certified by CX in this pass.
- CEO runtime certification is still required.

## Concurrent Work Preservation

No reset, stash, revert, commit, push, server deployment or SQL migration was performed.

M045/M046 interaction and upgrade paths were not intentionally modified:

- `BuildingInteractionController.InteractionPreemptionHook` untouched;
- upgrade completion lifecycle untouched;
- upgrade pulse parameters untouched;
- Alliance Help state untouched.

`HiveViewProductUiPresenter` already contained concurrent CL/M048 diffs in the working tree; this report only claims the player profile changes listed above.

## Acceptance Checklist

A. Historical LivingHive Player Summary found? YES  
B. Historical detailed Profile found? YES  
C. Current HiveMap broken path identified? YES  
D. Root cause of rendering/layout failure proven? YES  
E. Existing functionality reused/migrated rather than duplicated? YES  
F. No LivingHive runtime dependency introduced? YES  
G. Player Summary now foreground/readable? YES by code/layering; pending Play Mode visual confirmation  
H. Queue cards no longer bleed through/intercept input? YES by overlay gate; pending Play Mode visual confirmation  
I. Text overflow fixed? YES by bounded layout/proof trimming; pending visual confirmation  
J. Profile button functional? YES by code/proof path  
K. Detailed Profile functional in HiveMap? YES by reused HiveMap presenter path  
L. Real authenticated player identity used? YES when the authenticated session exposes `DisplayName`  
M. Overlay/input gating correct? YES by code/proof path  
N. Close/back navigation correct? YES by code/proof path  
O. Concurrent M045/M046 work preserved? YES  
P. Unity compile green? YES  
Q. Relevant tests green? NO, Unity Test Runner blocked by already-open project instance  
R. Play Mode visually verified by CX? NO  
S. READY FOR CEO RUNTIME CERTIFICATION? YES

READY FOR CEO - TEST PLAYER SUMMARY -> PROFIL IN HIVEMAP.

