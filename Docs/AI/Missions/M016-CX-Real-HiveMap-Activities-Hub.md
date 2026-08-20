# M016-CX Real HiveMap Activities Hub

Owner: CX  
Reviewer: GPT  
Status: Implementation complete to safe checkpoint; not committed.

## Implemented

- Replaced the fake static Activities rows in `LivingHiveMenuCanvas` with a real HiveMap Activities entry.
- Added `LivingHiveActivitiesBridge` so the existing HiveMap bottom/menu Activities entry opens a HiveMap-owned fullscreen modal instead of toggling the legacy fake panel.
- Added `HiveMapActivitiesBootstrap`, auto-started on `Environment2D5D*` scenes.
- Reused existing server-backed/current controllers exposed by `MobileAccountSessionRuntimeBootstrap`:
  - `IHiveDailyRoundPanelController`;
  - `IHiveMilestoneEventPanelController`.
- Added HiveMap direct accessors for Daily Round and Milestone Event controllers, with `Unavailable*` fallbacks when no official gameplay session is configured.
- Implemented Daily Round display:
  - loading / unavailable / error states;
  - official progress facts;
  - server reward values;
  - refresh / claim / verify actions;
  - offline read-only messaging when provided by the controller.
- Implemented Milestone Event display:
  - loading / unavailable / error states;
  - server objective list;
  - completed objective count;
  - claim state;
  - event window status;
  - reward payload;
  - refresh / claim action.
- Removed the previous fake rows:
  - special gold harvest event;
  - weekly honey challenge;
  - daily nursery quest;
  - monthly zone discovery challenge.
- Deliberately did not migrate Mission Center rows because the available surface is legacy/local presentation without a comparable current server-backed model in M016 scope.
- Added fullscreen/modal integration:
  - Activities modal has a dedicated fullscreen background and header;
  - blue back-arrow convention is reused through `HiveViewProductUiPresenter.DrawPremiumBackButtonForExternalHost`;
  - modal close restores normal HiveMap presentation by clearing the Activities modal flag.
- Added input/presentation suppression while Activities is open:
  - queue sidebar hidden;
  - production badges hidden;
  - manual production bees/feedback hidden;
  - barrack/world floating overlays hidden;
  - construction/settings/alliance/champion/nursery/unsupported/Royal Palace overlays suppressed;
  - building upgrade click overlay suppressed;
  - overlay input gate treats Activities as blocking.

## Existing Systems Reused

- `HiveDailyRoundClient`
- `HiveDailyRoundPanelController`
- `HiveDailyRoundScreenModel`
- `HiveMilestoneEventClient`
- `HiveMilestoneEventPanelController`
- `HiveMilestoneEventScreenModel`
- `MobileAccountSessionRuntimeBootstrap`

No new server contract, reward table, event catalog, player progression source, or authentication path was invented.

## Authority Classification

Daily Round:

- SERVER-AUTHORITATIVE: facts, completed count, claim availability, reward amounts, receipt, revision, reset/day state.
- CURRENT CLIENT MODEL: loading/busy/error/read-only state, retry/claim enablement.
- LEGACY LOCAL/PREVIEW: none exposed in the M016 HiveMap hub.

Milestone Event:

- SERVER-AUTHORITATIVE: objective keys, done flags, required objective count, claim availability, reward payload, window end/expired state, revision.
- CURRENT CLIENT MODEL: loading/busy/error state.
- LEGACY LOCAL/PREVIEW: none exposed in the M016 HiveMap hub.

Mission Center:

- LEGACY LOCAL/PREVIEW in current inspected scope; deliberately omitted rather than copied as fake current gameplay.

## Unauthenticated Runtime Validation

Validated without solving authentication:

- C# compile succeeds with 0 errors:
  - `dotnet build BeeKingdom.Core.csproj --no-restore -v:minimal /clp:ErrorsOnly /nr:false`
  - `dotnet build BeeKingdom.LivingHiveMenu.csproj --no-restore -v:minimal /clp:ErrorsOnly /nr:false`
  - `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal /clp:ErrorsOnly /nr:false`
- The fake static Activities rows are removed from `LivingHiveMenuCanvas`.
- The Activities entry now routes to `LivingHiveActivitiesBridge.OpenOverlay()`.
- Without an authenticated official gameplay session, `MobileAccountSessionRuntimeBootstrap` returns unavailable Daily Round and Milestone Event controllers.
- The HiveMap modal displays honest unavailable/session-not-configured messaging instead of fake current-player values.
- Modal/input suppression is wired through the same narrow pattern already used by Research/Royal Palace-style fullscreen experiences.
- Research and Royal Palace were spot-checked at code level for obvious M016 regressions: M016 adds only Activities-open suppression checks and does not alter their content, naming, header logic, or gameplay behavior.

Unity batchmode validation was attempted with Unity 6000.5.3f1, but the local project was already open in another Unity instance. Unity refused a second batchmode project open, so Unity editor/import validation remains unavailable from this Codex run.

Play Mode validation was not performed because this mission must not spend time on authentication and the active Unity project instance is outside this command session.

## AUTHENTICATED PLAYER VALIDATION DEFERRED TO M016B

Final server-backed Activities acceptance still requires a real authenticated CEO/player session to validate:

- Daily Round server read succeeds through the official session;
- Daily Round claim/verify flow works against the live server;
- Milestone Event server read succeeds through the official session;
- Milestone Event claim flow works against the live server;
- server errors/authentication-required cases render correctly from real transport failures;
- no fake player/session data appears during authenticated or signed-out transitions;
- modal close/open behavior remains correct during live controller refreshes.

## M016A Dependency

Google auth restoration must occur before final server-backed Activities acceptance.

M016 intentionally does not fix or redesign Google authentication. The missing Google login currently blocks authenticated runtime validation, not the M016 implementation checkpoint.

## M015 Separation

`Docs/AI/Missions/M015-CX-Final-LivingHive-Player-Capability-Gap-Map.md` is unrelated accepted documentation. It was not modified by M016 and remains separate unless GPT explicitly authorizes inclusion later.

## Files Modified

- `Assets/BeeKingdom/Core/Integration/LivingHiveActivitiesBridge.cs`
- `Assets/BeeKingdom/Playground/HiveMapActivitiesBootstrap.cs`
- `Assets/BeeKingdom/Playground/MobileAccountSessionRuntimeBootstrap.cs`
- `Assets/Experiments/Environment2D5D/LivingHiveMenu/LivingHiveMenuCanvas.cs`
- `Assets/BeeKingdom/Playground/HiveMapQueueSidebarBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapOverlayInputGateBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapProductionBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapProductionInfoBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapBarrackBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapBuildingUpgradeClickBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapAllianceBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapConstructionBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapSettingsBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapChampionHallBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapNurseryBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapUnsupportedBuildingBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapRoyalPalaceBootstrap.cs`
- `Docs/AI/Missions/M016-CX-Real-HiveMap-Activities-Hub.md`

Unity-generated `.meta` files for new M016 scripts are present and should be included with M016 when commit authorization is given.

## Remaining Issues

- AUTHENTICATED PLAYER VALIDATION DEFERRED TO M016B.
- Unity batchmode validation could not run while another Unity instance had the project open.
- Mission Center remains outside M016 until a current authoritative model is identified or built.

## Confidence

MEDIUM-HIGH for the safe checkpoint implementation and unauthenticated behavior.

MEDIUM overall until M016B performs authenticated CEO/player runtime validation.
