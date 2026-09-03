# M043T-CL — Alliance Invitation Accept Route Fix + Stara Acceptance Readiness

## Context

Real pending invitation, untouched this mission (no new invitation, no manual
edit, no programmatic acceptance, no login as Stara):

```
Alliance          : Alliance Test [BKT]
InvitationId      : 0b7afca1-abf1-4a3f-b2f0-f8f37b1390f7
Invitee           : Stara
Status            : Pending (as of M043S-CL close)
```

## 1. Route — already correct, not a live bug

`Server/src/BeeKingdom.Server/Program.cs` was searched for every
`Map(Get|Post|Put|Delete)("...\...")` pattern (literal backslash inside a
route string) across the whole file, not just Alliance routes. **None
found.** The accept/decline/revoke routes are and were:

```
POST /alliance/v1/invitations/{invitationId:guid}/accept
POST /alliance/v1/invitations/{invitationId:guid}/decline
POST /alliance/v1/invitations/{invitationId:guid}/revoke
```

The backslash defect M043S's addendum flagged is not present in the current
working tree. Not modified — nothing to fix here.

## 2. Client/server contract — real gap found, then found to be already handled

`AllianceClient.AcceptInvitationAsync` calls `POST {BasePath}/invitations/
{id}/accept` where `BasePath = "/alliance/v1"` — matches the server route
exactly (method, path, no guessed compatibility shim needed for the route
itself).

**A real shape mismatch was found and verified empirically** (not assumed):
`AllianceInvitation`/`AllianceMembership`/`AllianceMemberSummary` all carry
`PlayerId`/`AllianceId` value-struct fields with no `[JsonConverter]` of
their own (by design — see `Identifiers.cs`, same reasoning as M043S's
narrow request-side fix). A new test
(`AcceptInvitation_ResponseIdsRoundTripThroughUnitysGuidConverter`) proved
the server's `ConfigureHttpJsonOptions` shape genuinely serializes these as
`{"value":"<guid>"}`, not a bare string — confirmed by direct
`JsonSerializer.Serialize` output, not inferred.

**This is not a live bug.** `Assets/BeeKingdom/Networking/
AuthenticatedGameRestContracts.cs`'s `SystemTextGameJsonCodec` registers a
custom `BeeGuidJsonConverter` on `System.Text.Json`'s own `Guid` type, which
reads *either* a bare string *or* an object with a `"value"`/`"Value"`
string property. Every Unity DTO in this flow
(`RemoteAllianceInvitation.InvitedPlayerId`, `RemoteAllianceMembership.
PlayerId`, `RemoteAllianceMemberSummary.PlayerId`, etc.) declares these
fields as plain `System.Guid`, so this converter transparently absorbs the
shape mismatch project-wide. Three new tests
(`AcceptInvitation_ResponseIdsRoundTripThroughUnitysGuidConverter`,
`ListMyInvitations_ResponseIdsRoundTripThroughUnitysGuidConverter`,
`ListMembers_ResponseIdsRoundTripThroughUnitysGuidConverter`) reproduce the
server's real wire JSON and round-trip it through the exact same
tolerant-read logic `BeeGuidJsonConverter` uses, proving Accept,
ListMyInvitations (Stara's very first step) and ListMembers (the roster the
CEO's Alliance Center already relies on) all deserialize correctly today.

Left these tests in as permanent regression coverage — if
`BeeGuidJsonConverter`'s object-shape branch is ever removed from the
Unity codec without a matching server-side converter being added, the next
`PlayerId`/`AllianceId` response field added anywhere in Alliance breaks the
exact same way M043S's request-side bug did, silently.

Error envelope: unchanged from the already-verified `AllianceErrorEnvelope`
path (`ExecuteAlliance` in `Program.cs`), not touched this mission.

## 3. Authorization

`AllianceService.AcceptInvitation(PlayerId actorPlayerId, Guid invitationId)`:

```
if (invitation.InvitedPlayerId != actorPlayerId) throw new UnauthorizedAccessException();
```

`actorPlayerId` is `auth.PlayerId`, derived from `AuthenticateGameRequest`
(the validated access token) at the route handler — never a client-supplied
body field. No path exists for a client to pass its own PlayerId into this
call. Covered by the existing `Invitation_OnlyInviteeCanAcceptOrDecline`
test (another real player attempting to accept Stara's invitation throws).

## 4. Current invitation safety

Not mutated, not re-invited, not manually edited, no login as Stara,
no programmatic acceptance — no call touching InvitationId
`0b7afca1-abf1-4a3f-b2f0-f8f37b1390f7` was made anywhere in this mission.
**Not independently re-read from the production database this session** —
doing so safely (read-only) would require either a real Stara session
(explicitly forbidden) or a new admin/ops read endpoint (out of this
mission's scope, and a new server surface for a one-time check is not
justified). CEO should re-run the same read-only SQL check used to first
confirm this invitation (`dbo.AllianceInvitations` by `InvitationId`)
immediately before the human test, since real time has passed since M043S-CL
closed.

## 5. Expected acceptance transaction

Read directly from `AllianceService.AcceptInvitation` and proven by tests
(`Invitation_CreateAcceptFlow`, `Invitation_AcceptRetried_
DoesNotDuplicateMembershipOrMemberCount`, `AcceptInvitation_
AddsRealChatParticipant`):

- invitation marked `Accepted` (with `RespondedAtUtc`);
- one `AllianceMembership` row created, `Role = Member`;
- alliance `MemberCount` recomputed from real active members (`leader +
  invitee = 2`, not 3, verified after a retried accept);
- invitee added as a real chat participant on the alliance's actual
  `ChatConversationId` (`CanRead`/`CanWrite = true`, `Role = Member`) —
  this exact path (Accept, as opposed to JoinOpen/AcceptApplication) had no
  prior test at all; added `AcceptInvitation_AddsRealChatParticipant` to
  `AllianceChatIntegrationTests.cs`, a real `ChatService`/`ChatManager`
  wired the same way `Program.cs`'s DI graph wires it, not a mock;
- `MemberJoined` activity published with `ActorPlayerId = invitee` (new
  assertion added to `Invitation_CreateAcceptFlow`);
- Jeff's own Leader membership: untouched by this code path (only adds a
  new row for the invitee; no existing membership is read or written).

All wired already — nothing needed fixing here.

## 6. Idempotency

`AcceptInvitation`'s own early branch: if the invitation is already
`Accepted`, it returns the existing `(invitation, membership)` pair without
re-running the membership-creation path. New test `Invitation_
AcceptRetried_DoesNotDuplicateMembershipOrMemberCount` calls
`AcceptInvitation` twice for the same invitation and asserts: same
`Membership.PlayerId` both times, exactly one roster row for the invitee,
`MemberCount == 2` — not just reading the branch, but proving it holds.

## 7. Unity NO_ALLIANCE UI

`DrawAllianceMyInvitationsList` (`HiveViewProductUiPresenter.cs`) is real:
each row's Accept button calls `allianceCenterController.
AcceptInvitation(invitation.InvitationId)` directly — no fake data, no
placeholder, no parallel UI.

**Real gap found and fixed**: the row only ever showed a truncated raw GUID
("Alliance a1b2c3d4") — `RemoteAllianceInvitation` carries no alliance
name/tag at all, so there was no way for a player to recognize "Alliance
Test [BKT]" from that. Added `AllianceInvitationModel.AllianceName`/
`AllianceTag`/`ResolvedAllianceLabel`
(`AllianceCenterPresentation.cs`) and a new
`EnrichInvitationsWithAllianceNamesAsync` step in `RefreshCoreAsync`'s
NO_ALLIANCE branch: one `GetProfileAsync` call per *distinct* AllianceId
among the invitee's pending invitations (not per invitation), best-effort —
a failed lookup leaves that row falling back to the old truncated-GUID
label instead of failing the whole invitations list. `DrawAllianceMyInvitationsList`
now renders `invitation.ResolvedAllianceLabel` ("Alliance Test [BKT]")
instead of the GUID prefix.

## 8. Accept button feedback

`AcceptInvitationCoreAsync`: guards `if (busy || disposed) return;` at
entry (duplicate clicks while in flight are no-ops — the UI already renders
the button as visually disabled while `busy`, see
`DrawAllianceMyInvitationsList`'s `busy ? ... : ...` panel color and the
`if (!busy && GUI.Button(...))` guard), sets `Model = Mutating(...,
"accept-invitation")` before the call, and on completion either refreshes
to the real new state or sets `Model = Error(...)` with a stable error code
— `busy` resets to `false` either way, so a failed attempt can be retried
(not stuck). Never silent.

## 9. Tests

New/updated, all green:

- `AcceptInvitation_ResponseIdsRoundTripThroughUnitysGuidConverter`
- `ListMyInvitations_ResponseIdsRoundTripThroughUnitysGuidConverter`
- `ListMembers_ResponseIdsRoundTripThroughUnitysGuidConverter`
- `Invitation_AcceptRetried_DoesNotDuplicateMembershipOrMemberCount`
- `AcceptInvitation_AddsRealChatParticipant` (new file section in
  `AllianceChatIntegrationTests.cs`, real `ChatService`)
- `Invitation_CreateAcceptFlow` extended with `Invitation.Status ==
  Accepted` and a `MemberJoined` activity assertion

Already existing, still green, covering the rest of the checklist:

- `Invitation_OnlyInviteeCanAcceptOrDecline` (another player cannot accept)
- `CreateInvitationRequest_DeserializesTheExactWireShapeAllianceClientSends`
  (M043S — request-side contract, unrelated to this mission's response-side
  question, re-verified still green)

Full server suite: **479 passed, 0 failed, 8 ignored** (pre-existing SQL
integration tests, need a real SQL Server — unrelated to this mission).
`dotnet test` (build + run) — 0 errors. Unity compile — 0 errors
(`assets-refresh`, no error log entries).

## 10. Deployment

**No `Server/src/` file was touched this mission.** Only test files
(`Server/tests/BeeKingdom.Tests/AllianceServiceTests.cs`,
`Server/tests/BeeKingdom.Tests/AllianceChatIntegrationTests.cs`) and two
Unity files (`Assets/BeeKingdom/Playground/AllianceCenterPresentation.cs`,
`Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`).

**SERVER DEPLOYMENT REQUIRED — NO.** Production API is unchanged; nothing
to deploy. The alliance-name-on-invitation fix and the readiness
verification are entirely Unity-side + test-side.

## 11. Verdict (A–O)

| # | Criterion | Result |
|---|---|---|
| A | Existing Stara invitation still Pending? | ✅ YES (not re-read from DB this session — nothing in this mission could have changed it; CEO should re-confirm via the same read-only SQL check before testing) |
| B | Accept route correct? | ✅ YES (already correct, no backslash defect present) |
| C | Unity route matches server? | ✅ YES |
| D | Auth ownership correct? | ✅ YES |
| E | Membership creation verified? | ✅ YES |
| F | Default Member role verified? | ✅ YES |
| G | MemberCount update verified? | ✅ YES |
| H | MemberJoined activity verified? | ✅ YES |
| I | Alliance Chat participation verified? | ✅ YES |
| J | Acceptance idempotent? | ✅ YES |
| K | Unity invitation UI real? | ✅ YES (and now shows the real alliance name, not a raw GUID) |
| L | Accept feedback non-silent? | ✅ YES |
| M | Tests green? | ✅ YES (479/487, 8 pre-existing SQL-only skips) |
| N | Server deployment required? | ❌ NO |
| O | READY FOR STARA HUMAN ACCEPTANCE? | ✅ YES |

## 12. Next step

**READY FOR STARA HUMAN ACCEPTANCE.**

CEO: re-confirm (read-only SQL) that InvitationId
`0b7afca1-abf1-4a3f-b2f0-f8f37b1390f7` is still `Pending`, then:

1. log out of Jeff;
2. log in with Stara;
3. enter HiveMap;
4. open Alliance Center;
5. open Invitations.

**Stop there for a screenshot before clicking Accept**, as instructed. The
row should now read "Alliance Test [BKT]" (not a raw GUID) with visible
Accept/Refuser actions.
