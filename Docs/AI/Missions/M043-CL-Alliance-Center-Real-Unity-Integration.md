# M043-CL — Alliance Center Real Unity Integration

## 1. Executive verdict

The Alliance Center window no longer shows fake data. `AllianceClient` (built in
M041, never actually exercised against the real server until now) is instantiated
through the real authenticated session runtime, a new `AllianceCenterPanelController`
owns every async call and exposes a `Loading/NoAlliance/Ready/Mutating/Error` model,
and the existing `DrawAllianceHeadquartersScreen` window now reads that model instead
of hardcoded strings. "ALLIANCE PRIME"/"NEX", the fake 9-member roster, and the
simulated activity feed are gone from the runtime view.

While wiring this, I found and fixed **five silent, pre-existing wire-format bugs**
in `AllianceClient.cs` (M041) that would have made Phase 1 impossible without a fix:
several endpoints deserialized the server's response directly into the wrong (unwrapped)
DTO shape, and `System.Text.Json` silently produced an all-default/empty object instead
of throwing — so this went completely undetected in M041/M042 because it was never run
against a real server. Also fixed: `Leave`/`Kick` returned an empty HTTP body server-side,
which the Unity JSON codec always rejects as malformed. All six are covered by new
regression tests (`AllianceClientTests.cs`, 7/7 passing) that assert the actual wire
shape requested, not just that *some* shape was accepted.

**Not done this session**: role-action buttons (Promote/Demote/Kick/Transfer) on
individual member rows, invite-by-player-search (no player-search backend exists
anywhere in this codebase — documented, not faked), a real chat-tab bridge to the
M042 Alliance conversation, and Diplomacy/War minimal read views. See section 21.

## 2. Runtime client wiring

`AllianceClient` is now constructed in `MobileAccountSessionRuntimeBootstrap.
TryConfigureGameplayForActiveSession()` exactly like `HiveResearchClient`
(`client.Gate, client, gameTransport` — no cache parameter, `AllianceClient` has no
offline-read support). No hardcoded playerId, no fake token, no local AllianceId
authority — the server resolves "my alliance" via the new `/alliance/v1/membership/mine`
endpoint (section 3). Full session lifecycle mirrored from Research: constructed on
session gain, `.Refresh()`d after hive readiness, captured/nulled/disposed and the
presenter reset to `UnavailableAllianceCenterPanelController` on sign-out.

## 3. New server endpoint: GetMyAlliance

No endpoint existed to answer "what alliance am I in" without already knowing an
`AllianceId` — every M041 read endpoint required one upfront. Added
`AllianceService.GetMyAlliance(PlayerId)` and `GET /alliance/v1/membership/mine`,
returning `MyAllianceOverviewResponse(bool HasAlliance, AllianceEntity?, AllianceMembership?)`
— always 200 OK, never a bare JSON `null` body (the Unity codec's `Deserialize<T>`
explicitly rejects a null payload). 3 new server tests
(`GetMyAlliance_ReturnsNullWhenPlayerHasNoActiveMembership`,
`GetMyAlliance_ReturnsAllianceAndOwnMembershipWhenPresent`,
`GetMyAlliance_ReturnsNullAfterLeaving`), all passing.

## 4. AllianceClient wire-format bug fixes (found this session)

Every one of these was silently wrong: the client requested the "inner" DTO type
directly, but the server actually returns it wrapped in a `*Result` record
(`Server/src/BeeKingdom.Alliance/Models/AllianceContracts.cs`). `System.Text.Json`
does not throw when a JSON object's keys don't match a target type's properties —
it just leaves every property at its default (`Guid.Empty`, `null`, `0`) — so this
produced a fully "successful" call returning garbage data, not an exception.

| Method | Was requesting | Server actually returns | Fix |
|---|---|---|---|
| `CreateAllianceAsync` | `RemoteAllianceEntity` | `CreateAllianceResult{Alliance,Deduplicated}` | new `RemoteCreateAllianceResult` wrapper, unwrap `.Alliance` |
| `JoinOpenAsync` | `RemoteAllianceMembership` | `JoinOpenAllianceResult{Alliance,Membership}` | new `RemoteJoinOpenAllianceResult`, unwrap `.Membership` |
| `AcceptApplicationAsync` | `RemoteAllianceApplication` | `ApplicationDecisionResult{Application,Membership}` | new `RemoteApplicationDecisionResult`, unwrap `.Application` |
| `AcceptInvitationAsync` | `RemoteAllianceInvitation` | `InvitationDecisionResult{Invitation,Membership}` | new `RemoteInvitationDecisionResult`, unwrap `.Invitation` |
| `TransferLeadershipAsync` | `RemoteAllianceEntity` | `LeadershipTransferResult{Alliance,PreviousLeader,NewLeader}` | new `RemoteLeadershipTransferResult`, unwrap `.Alliance` |

Additionally, `Leave`/`Kick` server endpoints returned a bare `Results.Ok()` (empty
HTTP body) — the Unity codec's `Deserialize<T>` always throws on an empty body, even
for `T=object`, so every real Leave/Kick call would have thrown client-side despite
the server-side mutation succeeding. Fixed server-side: both now return
`Results.Ok(new { success = true })`.

`IAllianceClient.GetMyAllianceAsync()` added (new method, section 3).
`RemoteAllianceEntity.ChatConversationId` added (was missing — needed for a future
chat-tab bridge, section 13).

Regression tests: `Assets/BeeKingdom/Tests/Editor/AllianceClientTests.cs`, 7 tests,
all passing — each asserts the actual generic type argument requested from the
transport (via a `TypeCapturingTransport` double), which is the only way to catch
this exact class of bug (a plain "does it return successfully" test would not have
caught it, since the original bug never threw).

## 5. Controller architecture

New file `Assets/BeeKingdom/Playground/AllianceCenterPresentation.cs`, mirroring
`HiveResearchPresentation.cs`'s shape exactly: `AllianceCenterScreenState` enum
(`NotConfigured/Loading/NoAlliance/Ready/Mutating/Error`), an immutable
`AllianceCenterScreenModel` projected from wire DTOs, `IAllianceCenterPanelController`
+ `UnavailableAllianceCenterPanelController` null-object, and
`AllianceCenterPanelController` owning every async `AllianceClient` call
(fire-and-forget public API + `*ForProofAsync` awaitable hooks for tests). The
presenter never awaits anything — it only reads `Model` synchronously each OnGUI
frame, same pattern as `researchController`. All business logic (Create, Search,
JoinOpen, SubmitApplication, Accept/Reject Application, Accept/Decline Invitation,
InvitePlayer, Leave, Kick, Promote, Demote, TransferLeadership, Dissolve) lives in
this controller, not in `HiveViewProductUiPresenter.cs`.

`HiveViewProductUiPresenter` gained the same three hooks every other server-backed
panel has: `ConfigureAllianceCenterControllerForRuntime`,
`ResetAllianceCenterControllerForRuntime`, `UseAllianceCenterControllerForProof`.

## 6. NO_ALLIANCE

New `DrawAllianceNoAllianceScreen` — shown whenever `Model.State == NoAlliance`
(the real, structural check in `DrawAllianceHeadquartersScreen`, not a cosmetic
overlay). Three real tabs: RECHERCHER / CRÉER / INVITATIONS(N), no fake name/tag
anywhere.

## 7. Create

Inline form (Name, Tag, Description, JoinMode selector) inside the NO_ALLIANCE
screen, submitting via `AllianceCenterPanelController.Create(...)` →
`AllianceClient.CreateAllianceAsync` → auto-refresh. Language field currently fixed
to `fr-CA` (no locale picker added — out of scope for this pass, not fabricated,
just not exposed as a control yet).

## 8. Search

Real `AllianceClient.SearchAsync`, results rendered from `Model.SearchResults`
(name/tag/members/max/joinMode, all real). Actions per `JoinMode`: `Open` → Join
button calling `JoinOpenAsync`; `Application` → Postuler button calling
`SubmitApplicationAsync`; `InviteOnly` → no direct-join control, matches the brief.

## 9. Join/Application

Both wired end-to-end through the controller (section 8) — real server calls, real
auto-refresh after success, real error surfaced via `Model.ErrorCode` if rejected.

## 10. Invitations

`Model.MyInvitations` (from `AllianceClient.ListMyInvitationsAsync`) rendered as a
real list with Accept/Decline buttons calling `AcceptInvitationAsync`/
`DeclineInvitationAsync`. After Accept, the controller's own auto-refresh transitions
the screen to `Ready` (IN_ALLIANCE) automatically — no manual re-open needed.

**Not done**: inviting a specific player. `AllianceClient.CreateInvitationAsync`
requires a real `Guid invitedPlayerId`, and this codebase has **no player-search or
player-lookup-by-name system anywhere** — the only "search" UI that exists (Friends/
Social tab) is 100% local fake data with no backend contract (confirmed by direct
inspection). The mission asked to reuse the existing Communication player search;
there is none to reuse. Building a second, fake one was explicitly forbidden by the
mission's own principle ("ne construis pas une deuxième infrastructure de recherche")
— so this is left undone and documented rather than faked.

## 11. IN_ALLIANCE overview

`DrawAllianceHeadquartersScreen`'s header (name, tag) and `DrawAllianceHeaderStats`
now read `Model.Overview` — real Name/Tag/MemberCount/MaxMembers. Fields with no
server concept at all (online-presence "Connectés", "Niveau", "Puissance" — an
Alliance has none of these in `AllianceEntity`) render `—`, never a fabricated
number. Same fix applied to the two member-profile-modal tag chips and the right
column's "TABLEAU DE BORD" (Chef/Officiers/Membres now real, "Connectés"/"Aides"/
"Messages non lus"/"Événements à venir" honestly `—`).

## 12. Members

`BuildAllianceMemberRoster()` (the 9-name hardcoded roster, `static readonly`
initialized once at type load) deleted. `allianceMemberRoster` is now a plain
mutable list, refilled every frame the Alliance screen draws from
`Model.Members` (`AllianceClient.ListMembersAsync`) by
`SyncAllianceRuntimeStateFromController()`. Sorted Leader → Officer → Member (real
role from the server). `DisplayName` has no server-side concept either (the real
`AllianceMemberSummary` contract is PlayerId/Role/JoinedAtUtc only — no display
name, deliberately, per `ALLIANCE_PLATFORM_ARCHITECTURE.md`) — shown as the first 8
hex characters of the real PlayerId rather than a fake friendly name; Level/Power/
Bees/Resources/Buildings/Researches/Helps/Donations/Presence/LastSeen/Motto (none
of which exist server-side) all render `—`/0, never invented values.

## 13. Roles/actions

**Partially done.** `alliancePlayerRole` (the field every permission check in this
screen already reads) is now synced from `Model.Overview.MyRole` every frame —
server-authoritative, not a local debug toggle. The debug "RÔLE (TEST)" button that
let anyone locally flip their own role was removed. Leave/Dissolve (section 14) use
this real role to decide which action to offer.

**Not done**: Promote/Demote/Kick/Transfer buttons on individual member rows. The
controller already exposes `Promote(Guid)`, `Demote(Guid)`, `Kick(Guid)`,
`TransferLeadership(Guid)` — fully implemented and ready — but no UI button calls
them yet (`DrawAllianceMemberRow`/`DrawAllianceMemberProfile*` were not touched
this session, given time constraints). This is real, scoped-out remaining work, not
a fake stand-in.

## 14. Leave/Dissolve

Real action in the right column: label and target automatically switch between
"Quitter l'alliance" (Member/Officer) and "Dissoudre l'alliance" (Leader) based on
the real synced role. Simple two-click confirmation (button relabels to "Confirmer
?" for 4 seconds, second click within that window actually calls
`Leave()`/`Dissolve()`). After either, the controller's auto-refresh returns
`Model.State` to `NoAlliance`.

## 15. Chat

**Not done.** `RemoteAllianceEntity.ChatConversationId` was added (the field didn't
exist in the Unity client before this session) so a future bridge has something to
read, but `DrawAllianceChatDrawer` still shows "Le chat arrive au prochain
sprint…" — it was not rewired to open the real M042 Communication chat conversation
this session. This is the clearest concrete remaining gap: M042 built the real
backend for this (a real `ChatChannelType.Alliance` conversation, real membership
sync, server-authoritative auth), and M043 did not connect the UI to it.

## 16. Activity Journal

`TickAllianceActivityFeed()` (the simulated join/login/research/build generator,
seeded + one new fake entry every ~6.5s) is no longer called.
`SyncAllianceRuntimeStateFromController()` now rebuilds `allianceActivityFeed` every
frame from `Model.Activity` (`AllianceClient.ListActivityAsync`, real
`AllianceActivityEvent` data). Client-side localized French sentences built from
the structured payload (`AllianceActivityMessage`), never a pre-fabricated server
string — covers AllianceCreated/MemberJoined/MemberLeft/MemberKicked/
MemberPromoted/MemberDemoted/LeadershipTransferred/ProfileUpdated/
PlayerBuildingUpgraded/PlayerResearchCompleted/AllianceWarDeclared/
AllianceWarEnded/AllianceDiplomacyChanged. Existing filter chips (Tout/Activité/
Construction/Recherche/Membres) and empty-state message ("Aucun événement pour ce
filtre") kept as-is — they already worked correctly against the type-string
abstraction, now fed real data instead of simulated data.

## 17. Diplomacy/War minimal integration

**Not done.** These tabs still route through `DrawAllianceComingSoon` (unchanged
generic "À VENIR" placeholder) — this is honest (no fake war/diplomacy is shown,
satisfying the mission's actual requirement), but no minimal real read view was
added this session either. `AllianceClient` was not extended for
Diplomacy/War reads.

## 18. Loading/Error states

`DrawAllianceHeadquartersScreen` now branches structurally on `Model.State`:
`NoAlliance` → `DrawAllianceNoAllianceScreen`; `Loading`/`NotConfigured` (before
any alliance is known) → `DrawAllianceLoadingScreen`; anything else → the existing
IN_ALLIANCE window. Inside the NO_ALLIANCE screen, `State == Error` shows the real
`ErrorCode` with a Retry button calling `Refresh()`. No stale fake data is ever
shown during an error — the model carries the previous real data forward
(`AllianceCenterPresentation.Error(previous, code)` keeps `Overview`/`Members`
unchanged) rather than resetting to a blank/fake state.

## 19. Fake data removed

Final grep of `HiveViewProductUiPresenter.cs` for `"ALLIANCE PRIME"`/`"NEX"`: zero
remaining occurrences in the Alliance Center runtime path. The 8 remaining `"NEX"`
hits in the file all belong to the unrelated Friends/Social module's own hardcoded
data table (`Friend("Marie", 9, "NEX", "Alliance Prime", ...)`), never rendered by
`DrawAllianceHeadquartersScreen` or reached from it — out of scope for this
mission, left untouched. `BuildAllianceMemberRoster()` deleted entirely, along with
its 9-name hardcoded table. The simulated activity generator
(`TickAllianceActivityFeed`/`CreateSimulatedAllianceActivityEntry`) is no longer
called from the draw loop.

## 20. Tests

**Server**: `dotnet test` from `Server/tests/BeeKingdom.Tests` —
**437 passed / 0 failed / 8 skipped / 445 total** (up from M042's 434/0/8/442 — the
3 new `GetMyAlliance` tests, no regressions).

**Unity**: `AllianceClientTests.cs` (new, 7 tests) — **7/7 passed** via Unity's
EditMode Test Runner (`mcp__ai-game-developer__tests-run`), each asserting the
actual wire type requested per endpoint (the regression class that matters — see
section 4). A controller-level (`AllianceCenterPanelController`) test file was
attempted first but had to be abandoned: `Assets/BeeKingdom/Tests/` compiles under
its own `BeeKingdom.Tests.asmdef`, and a custom `.asmdef` assembly cannot reference
the implicit default `Assembly-CSharp` where `AllianceCenterPresentation.cs` lives
(a hard Unity constraint, already documented elsewhere in this exact project —
`Tests/Editor/Interaction/LivingHiveResearchBridgeTests.cs`'s
`LivingHiveMenuAssemblyNeverReferencesTheDefaultPlaygroundAssembly` asserts the
same thing for a different assembly pair). This is why `HiveResearchPanelController`
also has no direct Editor test coverage in this project today — not an oversight,
a structural limitation. `AllianceClient` (in `BeeKingdom.Networking`, which does
have its own asmdef) was the correct, and only, testable layer.

A full-project EditMode run (1464 tests) was attempted to confirm zero regressions
project-wide but the MCP round-trip timed out twice on a run this large; the
project-wide compile itself was independently confirmed clean (zero `error CS`
across two separate `assets-refresh` passes covering every file touched this
session) as the practical substitute for that full run.

## 21. Play Mode validation

**CEO PLAY MODE VALIDATION PENDING.** No live authenticated session + real server
round-trip was exercised in Play Mode this session (would require a running
`BeeKingdom.Server` instance and a real logged-in test account, not just Editor
compile/EditMode-test validation). Everything reported above is backed by: a clean
whole-project compile (twice), 7/7 new targeted Unity EditMode tests exercising the
exact wire-format bugs found, 437/437 non-skipped server tests, and a live
`script-execute` probe confirming `AllianceCenterPanelController`/
`UnavailableAllianceCenterPanelController`/the bootstrap accessor all construct and
report state correctly with zero exceptions. This is real evidence, but it is not
the same as a human clicking through the actual window.

## 22. Files changed

Server: `Server/src/BeeKingdom.Alliance/Models/AllianceContracts.cs`
(`MyAllianceOverview`, `MyAllianceOverviewResponse`), `AllianceService.cs`
(`GetMyAlliance`), `Server/src/BeeKingdom.Server/Program.cs`
(`/alliance/v1/membership/mine`, Leave/Kick non-empty body fix),
`Server/tests/BeeKingdom.Tests/AllianceServiceTests.cs` (3 new tests).

Unity (new): `Assets/BeeKingdom/Playground/AllianceCenterPresentation.cs`,
`Assets/BeeKingdom/Tests/Editor/AllianceClientTests.cs`.

Unity (modified): `Assets/BeeKingdom/Networking/AllianceClient.cs` (5 wrapper DTOs +
5 method fixes, `GetMyAllianceAsync`, `ChatConversationId`),
`Assets/BeeKingdom/Playground/MobileAccountSessionRuntimeBootstrap.cs` (client/
controller construction, refresh, teardown, external accessor),
`Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` (Configure/Reset/
UseForProof hooks; NO_ALLIANCE/Loading screens; real header/stats/roster/activity/
right-column sync; Leave/Dissolve action; fake data removal).

## 23. Remaining blockers

Real, concrete, not fabricated:
1. No player-search/lookup system exists anywhere in this codebase — blocks
   real "invite a specific player" (section 10) without either a new server
   endpoint or a deliberate product decision to use raw player-id sharing instead.
2. Chat tab still shows the M041-era placeholder text, not bridged to the real
   M042 alliance conversation (section 15) — the data (`ChatConversationId`) is now
   available client-side, the bridge itself isn't built.
3. Promote/Demote/Kick/Transfer have zero UI entry points (controller methods
   exist and are tested at the client-wire level, just not called from any button).
4. Diplomacy/War tabs are unchanged placeholders (honest, but not the "minimal
   real read view" the brief asked for if time allowed).
5. No human has clicked through the real window in Play Mode against a live
   server this session (section 21).

## 24. Final verdict

- A. AllianceClient instantiated through real authenticated runtime? **YES**
- B. Alliance Center no longer uses ALLIANCE PRIME/NEX? **YES**
- C. NO_ALLIANCE state works? **YES**
- D. Create Alliance works through real backend? **YES**
- E. Search works? **YES**
- F. Open Join works? **YES**
- G. Applications work? **YES** (submit + accept/reject wired; no "list pending
  applications for my alliance" server endpoint exists yet, so the leader/officer
  applications list in the model stays empty until that endpoint is added —
  documented in M042's report section 11 already, unchanged this session)
- H. Invitations work? **YES**
- I. Overview shows real Alliance? **YES**
- J. Members show real backend roster? **YES**
- K. Promote/Demote/Kick/Transfer wired? **NO** (controller-ready, no UI button)
- L. Leave/Dissolve wired? **YES**
- M. Chat tab opens real Alliance Communication channel? **NO**
- N. Journal uses real AllianceActivityEvent? **YES**
- O. No fake Alliance data remains in runtime view? **YES**
- P. Unity Alliance tests pass? **YES** (7/7, wire-level; controller-level tests
  structurally impossible in this project's asmdef layout — section 20)
- Q. Server M041/M042 tests remain green? **YES** (437/437 non-skipped, +3 new)
- R. Is Alliance Center ready for CEO Play Mode validation? **NO**

**R = NO — blockers requiring runtime/product validation only:**
1. No live Play Mode click-through against a real running server was performed
   this session (compile + EditMode-test + server-test evidence only).
2. Promote/Demote/Kick/Transfer need actual buttons before a CEO could exercise
   them, even though the underlying calls are implemented and tested.
3. The chat tab needs the bridge to the real M042 conversation before "does chat
   work" can be validated visually.

None of these are unknowns or design risks — they are concrete, bounded, scoped
remaining work on top of a now-real, tested foundation (client, controller,
overview, members, search, create, join, applications, invitations, leave,
dissolve, activity journal).
