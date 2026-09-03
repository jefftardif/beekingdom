# M043B-CL — Alliance Center Functional Closeout

## 1. Executive verdict

All three functional gaps M043 left open are closed server-side and client-side
in source: Promote/Demote/Kick/Transfer Leadership are real buttons with
server-mirrored permission visibility and two-click confirmation; a genuinely
generic `PlayerDirectoryService`/`PlayerDirectoryClient` now exists (there was
none before this mission — M043 confirmed the gap), and the Alliance Invite
flow uses it instead of a raw GUID; the Alliance chat tab now bridges into the
real M042 Communication backend (`LivingHiveChatRuntime`) instead of showing
"Le chat arrive au prochain sprint…". The application-review gap M043 also
flagged (no way for a Leader/Officer to list pending applications) is closed
with a new server-authoritative endpoint and a real Unity list with real
DisplayNames.

**Update (same day, after Jeff reopened the Editor)**: the Unity Editor MCP
connection was unresponsive when the section below was first written (every
call — `assets-refresh`, `script-execute`, even `console-get-logs` — failed
after 10 retries, matching a known pattern already recorded in this project's
memory: "Unity hangs after bulk script writes"). Jeff reopened Unity and
asked to rerun `assets-refresh`. **Confirmed clean**: `assets-refresh`
returned "AssetDatabase refreshed successfully" with zero `error CS` entries
in the console; a live `script-execute` probe resolved
`UnavailableAllianceCenterPanelController`, `PlayerDirectoryClient`, and
`IAllianceClient.ListPendingApplicationsAsync` with no exceptions; and the
existing `AllianceClientTests.cs` (M043's wire-contract suite) ran **7/7
green** via the real EditMode Test Runner, confirming no regression. The
Unity compile-confirmation blocker from earlier in this session is closed.

## 2. Member management UI

`Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`: the member
profile modal's second action row (previously two placeholder buttons,
"Promouvoir"/"Exclure", both showing "sera disponible avec les rôles
serveur.") is replaced by `DrawAllianceMemberAdminActionsRow`, which computes
the real permitted action set per member (`AllianceComputeMemberAdminActions`)
and lays out 0–3 real buttons dynamically. No new logic was built into the
controller — `Promote(Guid)`, `Demote(Guid)`, `Kick(Guid)`,
`TransferLeadership(Guid)` already existed on `AllianceCenterPanelController`
since M043 and are called directly.

## 3. Permission visibility

Mirrors `AlliancePermissionPolicy` exactly (`Server/src/BeeKingdom.Alliance/
Models/AllianceModels.cs`):
- Leader viewing a Member → Promote, Kick, Transfer.
- Leader viewing an Officer → Demote, Kick, Transfer.
- Officer viewing a Member → Kick only (`CanKickTarget`'s asymmetric rule).
- Officer viewing an Officer/Leader, or a plain Member viewing anyone → no
  admin actions at all.
- Never offered against yourself (`member.PlayerId == overview.MyPlayerId`
  check) — required adding `MyPlayerId` to `AllianceOverviewModel`, which
  didn't exist in M043 (only `MyRole`/`MyJoinedAtUtc`).

The server remains the final authority — a rejected mutation still surfaces
through `Model.ErrorCode` exactly like every other controller action; this is
purely about which buttons are worth showing.

## 4. Player Public Identity

New `Server/src/BeeKingdom.Shared/ValueObjects/PlayerPublicIdentity.cs`:
`PlayerPublicIdentity(PlayerId PlayerId, string DisplayName)` — deliberately
minimal and deliberately placed in `BeeKingdom.Shared` (not `BeeKingdom.
Accounts` or `BeeKingdom.Alliance`) so any future domain can depend on it
without a cross-domain reference to Accounts. `AccountProfile.DisplayName`
already existed (`BeeKingdom.Accounts.Models.AccountModels.cs`) — this is not
a new source of truth, just the first privacy-safe projection of it.

## 5. Player Directory architecture

`Server/src/BeeKingdom.Accounts/PlayerDirectoryService.cs` (new,
`IPlayerDirectoryService`): `Search(string, offset, limit)`,
`GetByPlayerId(PlayerId)`, `GetByPlayerIds(IReadOnlyCollection<PlayerId>)`
(batch). Registered in `AccountServiceCollectionExtensions` alongside
`AccountManager`. Required adding `IAccountService.GetAccountByPlayerId`
(the repository already had `GetByPlayerId`, the service just never exposed
it) and a small `BeeKingdom.Alliance → BeeKingdom.Accounts` project reference
(no circular risk — `BeeKingdom.Accounts` has zero references back).

Unity mirror: `Assets/BeeKingdom/Networking/PlayerDirectoryClient.cs` (new),
same session/transport/single-refresh-on-401 plumbing as `AllianceClient`/
`HiveResearchClient`, entirely generic — `AllianceCenterPanelController`
depends on it via `IPlayerDirectoryClient`, never duplicates search logic
inside `AllianceClient`. This is the reusable seam Communication/Friends/mail
recipient selection can build on later, per the mission's explicit ask.

## 6. Player search endpoint

`GET /game/v1/players/search?q=...&offset=...&limit=...`
(`Server/src/BeeKingdom.Server/Program.cs`, placed before the Alliance block).
Auth-required (`AuthenticateGameRequest`, 401 `game.session_required` if
missing). Query validation lives in `PlayerDirectoryService.Search`: rejects
blank/too-short (<2 chars) or too-long (>64 chars) queries with 400
`game.invalid_request` — **a blank `q=""` can never extract the whole player
base**, exactly the brief's privacy requirement. Only `Active`-status accounts
are searchable. Results are `PlayerPublicIdentity[]` — structurally incapable
of carrying email/status/auth-provider-id (see the reflection-based test in
section 13.1 that asserts this).

## 7. Alliance Invite flow

`Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`:
`DrawAllianceInvitePlayerBody` replaces the "invite" quick-action panel's
placeholder text with a real debounced search field (350ms after the player
stops typing, plus an explicit "Chercher" button), a real results list
(`allianceCenterController.InvitePlayerSearchResults`, real DisplayName), and
a real "Inviter" button per result calling `InvitePlayer(result.PlayerId)` —
never a pasted GUID. Also fixed a pre-existing bug found while wiring this:
`AllianceActionAllowed("invite")` previously returned `true` for a plain
Member (any role could open the invite panel), which never matched the real
server rule (`AlliancePermissionPolicy.CanInvite` — Officer/Leader only); now
gated correctly.

## 8. Application review endpoint

`AllianceService.ListPendingApplicationsForMyAlliance(PlayerId actorPlayerId)`
— the gap M043 explicitly documented (repository already had
`ListPendingApplications(AllianceId)`, the service never called it). AllianceId
is **always** derived server-side from the actor's own real membership
(`RequireAllianceIdForPlayer`) — there is no client-supplied AllianceId
parameter on this call at all, so a non-member cannot enumerate any alliance's
applications, not even by guessing an id (there's no id to guess). Requires
`AlliancePermissionPolicy.CanApproveApplication` (Officer/Leader) or throws
`UnauthorizedAccessException` → 403. New endpoint:
`GET /alliance/v1/applications/pending`. Results are enriched with real
`DisplayName` via the same batch-resolution pattern as section 10, returned as
a new `AllianceApplicationView` record (not the raw `AllianceApplication`
domain model, which has no DisplayName field).

## 9. Application UI

`AllianceCenterPanelController.SafeListApplicationsAsync` (a documented stub
returning an empty list in M043) now calls the real
`AllianceClient.ListPendingApplicationsAsync()`. Displayed in
`DrawAllianceMembersList` — appended below the real member roster inside the
same scroll view (no new tab plumbing needed), visible only when
`Overview.IsOfficerOrLeader` and there are pending applications: real
DisplayName, submission date, Accept/Reject buttons calling
`AcceptApplication`/`RejectApplication`. Accept/Reject both trigger the
controller's existing auto-refresh (Applications + Members + Activity all
reload together, same mechanism as every other mutation since M043).

## 10. Member DisplayNames

`AllianceMemberSummary` (server) gained a `DisplayName` field (was `PlayerId,
Role, JoinedAtUtc` only). `AllianceService.ListMembers` batch-resolves all
member DisplayNames in one `PlayerDirectoryService.GetByPlayerIds` call — one
server-side pass, not one lookup per member, and critically not N+1 HTTP
calls from Unity (the brief's explicit requirement). Also fixed
`GetPublicProfile`'s Leader summary, which in M041/M042/M043 literally
returned `alliance.LeaderPlayerId.ToString()` as the "DisplayName" (a raw
GUID string) — now resolves the real name the same way.

Unity: `RemoteAllianceMemberSummary.DisplayName` (new field),
`AllianceMemberModel.ResolvedDisplayName` (real name, GUID-prefix fallback
**only** when the server genuinely has no account record — not the normal
path). `SyncAllianceRuntimeStateFromController` now populates the roster's
`Name` field from `ResolvedDisplayName` instead of the M043-era 8-hex-char
fallback.

## 11. Alliance Chat bridge

`DrawAllianceChatDrawer`'s fake 3-line hardcoded conversation and "Le chat
arrive au prochain sprint…" composer are replaced entirely by
`DrawAllianceChatRealBody`, which bridges into the real M042 Communication
backend via `BeeKingdom.Gameplay.Communication.LivingHiveChatRuntime` (already
used by `MobileAccountSessionRuntimeBootstrap.cs` elsewhere in this project,
confirming the cross-assembly reference direction is valid: Playground has no
`.asmdef` — compiles into the implicit `Assembly-CSharp` — while `BeeKingdom.
Gameplay` does have its own `.asmdef`; the implicit default assembly compiles
last and can reference explicit assemblies, never the reverse). No second
chat system was built — `ServerChatProvider`/`LivingHiveChatController` do all
the real work exactly as they already did for the rest of Communication.

Flow: `EnsureAllianceChatOpenedAndSelected(conversationKey)` calls
`LivingHiveChatRuntime.OpenAsync()` (loads every conversation the player is
actually a real participant of, per M042's `SyncChatParticipantAdded`) then
`SelectAsync(conversationKey)` — the conversation identity is **exactly**
`Model.Overview.ChatConversationId`, read fresh each time, never a locally
cached or guessed id. Once selected, messages render from
`LivingHiveChatRuntime.Snapshot.Messages` (real `SenderDisplayName`,
`VisibleBody`) and Send calls `LivingHiveChatRuntime.SendAsync(body)` — the
same optimistic-append/confirm pipeline every other Communication screen uses.
Background polling/realtime updates are `LivingHiveChatController`'s own
existing responsibility (`EnsureLiveUpdates`, already running since M042) —
this screen doesn't add its own polling loop.

## 12. Chat membership/security

No local membership check exists anywhere in this new code — `SelectAsync`
either succeeds (server, via the real M042 `IAllianceMembershipResolver` chain,
confirmed the player is a real participant) or throws `ArgumentException`
(the conversation isn't in the player's real accessible-conversations list),
which is caught and surfaces as "Accès au chat de l'alliance perdu
(not_a_member)." — this is the exact mechanism that makes kicked/left members
lose access, since M042's `AllianceService.Kick`/`Leave` already call
`SyncChatParticipantRemoved`, which the real chat server enforces on the next
`OpenAsync`/`SelectAsync` round-trip. Nothing client-side ever decides who can
read the alliance conversation.

## 12b. No ChatConversationId handling

If `Model.Overview.ChatConversationId` is null/empty (an old or invalid
alliance state), the body renders "Chat indisponible pour cette alliance."
and logs a `Debug.LogWarning` diagnostic exactly once (deduped via
`allianceChatAccessDeniedReason`, not spammed every frame) — no crash, and
critically, no second chat conversation is ever created client-side to work
around it.

## 13. Tests server

`dotnet test` from `Server/tests/BeeKingdom.Tests`:
- `PlayerDirectoryServiceTests.cs` (new, 6 tests): search finds real
  DisplayName case-insensitively, blank/too-short/too-long query rejected,
  a **reflection-based structural test** asserting `PlayerPublicIdentity` has
  exactly `{PlayerId, DisplayName}` properties (so no future edit can
  accidentally add email/status without this test catching it), pagination/
  limit, batch resolution, inactive accounts excluded from search.
- `PlayerDirectoryEndpointTests.cs` (new, 3 tests): full HTTP round-trip —
  unauthenticated → 401, blank/too-short query → 400, authenticated real
  query → 200 with a JSON array.
- `AllianceServiceTests.cs` additions (7 tests): pending-applications leader
  allowed, officer allowed (via real promotion), member denied
  (`UnauthorizedAccessException`), non-member denied (`InvalidOperationException`
  — no AllianceId parameter exists to guess), member roster resolves real
  DisplayNames via a fake `IPlayerDirectoryService`, empty-directory fallback
  is `string.Empty` not a fabricated name, public profile resolves the real
  Leader DisplayName.

**Result**: 452–453 passed / 1 failed (flaky, non-deterministic — see below) /
8 skipped / 461 total, run 3 times. The single failure
(`GameWorkshopBatchQualificationEndpointTests.
Qualification_returns_incorrect_then_advances_and_replays`) is unrelated to
this mission (Workshop Batch Qualification domain, untouched this session),
passes 100% of the time in isolation, and fails only under the full parallel
run — the exact same class of pre-existing flakiness M041/M042 already
documented (previously 2 named tests; this run surfaced this one instead,
consistent with "flaky under parallel execution", not a fixed always-failing
test). Not silently ignored: documented here per the mission's explicit
instruction, not chased further into a test-framework investigation (out of
scope, per the same instruction). **461 total tests is up from the M043
baseline of 445** (+16: 6 PlayerDirectoryServiceTests + 3
PlayerDirectoryEndpointTests + 7 AllianceServiceTests additions).

## 14. Tests Unity

**Not added this session.** The plan was to extend `AllianceClientTests.cs`
(M043's wire-contract test file, which lives in `BeeKingdom.Networking` — a
real, referenceable asmdef, unlike `AllianceCenterPanelController`) with
`PlayerDirectoryClient` search/malformed-response/auth-gate tests and an
`AllianceClient.ListPendingApplicationsAsync`/member-DisplayName wire-contract
test, mirroring the exact pattern that worked in M043. This was deferred when
the Unity Editor MCP connection became unresponsive (section 1) before these
could be written and verified. The existing `AllianceClientTests.cs` suite
(M043's 7 tests) was re-run after the Editor recovered and stayed green — no
regression — but the NEW wire contracts from this mission
(`PlayerDirectoryClient`, `ListPendingApplicationsAsync`, member `DisplayName`
parsing) still have no dedicated Unity test coverage. This remains the single
most concrete piece of unfinished work in this mission.

## 15. Files changed

Server (new): `BeeKingdom.Shared/ValueObjects/PlayerPublicIdentity.cs`,
`BeeKingdom.Accounts/PlayerDirectoryService.cs`,
`Server/tests/.../PlayerDirectoryServiceTests.cs`,
`Server/tests/.../PlayerDirectoryEndpointTests.cs`.

Server (modified): `BeeKingdom.Accounts/AccountService.cs`
(`GetAccountByPlayerId`), `AccountManager.cs` (same),
`DependencyInjection/AccountServiceCollectionExtensions.cs`
(`IPlayerDirectoryService` registration), `BeeKingdom.Alliance/Models/
AllianceContracts.cs` (`AllianceMemberSummary.DisplayName`,
`AllianceApplicationView`), `BeeKingdom.Alliance/AllianceService.cs`
(`playerDirectory` dependency, `ListMembers`/`GetPublicProfile` DisplayName
resolution, `ListPendingApplicationsForMyAlliance`),
`BeeKingdom.Alliance.csproj` (Accounts project reference), `BeeKingdom.Server/
Program.cs` (`/game/v1/players/search`, `/alliance/v1/applications/pending`),
`Server/tests/.../AllianceServiceTests.cs` (7 new tests).

Unity (new): `Assets/BeeKingdom/Networking/PlayerDirectoryClient.cs`.

Unity (modified, **compile confirmed** — section 1):
`Assets/BeeKingdom/Networking/AllianceClient.cs`
(`RemoteAllianceMemberSummary.DisplayName`, `RemoteAllianceApplicationView`,
`ListPendingApplicationsAsync`), `Assets/BeeKingdom/Playground/
AllianceCenterPresentation.cs` (`PlayerSearchResultModel`,
`MyPlayerId`/`ResolvedDisplayName` additions,
`SearchPlayersForInvite`/`InvitePlayerSearchResults`,
`IPlayerDirectoryClient` dependency), `Assets/BeeKingdom/Playground/
MobileAccountSessionRuntimeBootstrap.cs` (`PlayerDirectoryClient`
construction), `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`
(member admin actions row, invite search body, real applications list, real
chat bridge, `AllianceActionAllowed("invite")` permission fix).

## 16. Remaining gaps

Real, concrete, not fabricated:
1. No Unity-side (`BeeKingdom.Networking`-reachable) tests were added for the
   new `PlayerDirectoryClient`/`ListPendingApplicationsAsync`/DisplayName wire
   contracts (section 14) — this mission's own new code has no dedicated
   Unity test coverage yet, even though it now compiles and the pre-existing
   M043 tests confirm no regression.
2. No Play Mode click-through against a live server (never attempted this
   session either — was already the case in M043).
3. `AllianceOptions.MaxMembers`/language picker/emblem selection and every
   item on M043's own remaining-gaps list not touched by this mission (Part
   9's explicit do-not-expand-scope list: Diplomacy/War UI, territory,
   Alliance tech/buildings, rally, war combat, gameplay activity ingestion,
   Web Alliance page, SQL, FTUE) remain exactly as M043 left them.

## 17. CEO certification readiness

**Functionally ready; not yet Play-Mode-verified.** The Unity compile
blocker is closed (section 1) — everything described in sections 2–12 is
real, compiles, and, where testable outside Unity (all server-side logic and
the pre-existing M043 wire-contract tests), is green. What's left before a
genuine CEO certification is a real Play Mode session against a running
server (never done, for either M043 or M043B) and the new Unity test
coverage from item 1 above. No further "the backend exists but the button
isn't there" gaps remain in the areas this mission targeted.

## 18. Final verdict

- A. Promote button wired? **YES** (compile-confirmed)
- B. Demote wired? **YES** (compile-confirmed)
- C. Kick wired with confirmation? **YES** (two-click, compile-confirmed)
- D. Transfer Leadership wired with confirmation? **YES** (two-click, compile-confirmed)
- E. Real generic Player Directory exists? **YES** (server tested; Unity client compile-confirmed)
- F. Search returns real public player identity? **YES**
- G. Search protects private account data? **YES** (structurally — `PlayerPublicIdentity` cannot carry more fields; tested)
- H. Alliance Invite uses Player Directory? **YES** (compile-confirmed)
- I. Leader/Officer can list real pending applications? **YES** (tested server-side)
- J. Applications show real DisplayName? **YES**
- K. Alliance member roster shows real DisplayName? **YES** (tested server-side)
- L. Alliance Chat tab uses real M042 conversation? **YES** (compile-confirmed)
- M. Real messages can be read? **YES** (compile-confirmed, via `LivingHiveChatRuntime.Snapshot`)
- N. Real messages can be sent? **YES** (compile-confirmed, via `LivingHiveChatRuntime.SendAsync`)
- O. Chat access remains server-authoritative? **YES** (no local membership check anywhere in the new code)
- P. No Alliance Center fake player/chat data remains? **YES** (grep-confirmed, section 11/12b of this doc)
- Q. Server tests green? **YES** (452–453/453 non-flaky-affected, 1 pre-existing unrelated flake documented, not ignored)
- R. Unity targeted tests green? **PARTIAL** (M043's 7 pre-existing `AllianceClientTests` re-confirmed green, no regression; this mission's own new wire contracts have no dedicated tests yet — section 14)
- S. READY FOR CEO PLAY MODE CERTIFICATION? **NO**

**S = NO — concrete remaining blockers only:**
1. No Unity-side automated tests exist yet for `PlayerDirectoryClient`/the
   new application-listing and DisplayName wire contracts (Unity compile
   itself is confirmed clean, and existing tests show no regression).
2. No real Play Mode session against a running server has ever been done
   for the Alliance Center (M043 or M043B).

Both are mechanical, bounded next steps on top of a complete, compiling
implementation — neither is a design gap or an unknown.
