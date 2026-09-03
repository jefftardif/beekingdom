# M043U-CL — Real Alliance Member Gets Chat `not_a_member` After Invitation Acceptance

## Summary

A live production repro (Stara, real account, real accepted invitation) showed
`Alliance Center → Chat` displaying `"Accès au chat de l'alliance perdu
(not_a_member)."` immediately after a genuine, server-confirmed Alliance
membership acceptance (Jeff Leader, Stara Member, member count 2/100, real
`MemberJoined` activity).

Read-only production SQL, run by the CEO against the live database, proved
the server-side data was **already 100% correct** at the moment of
investigation:

```
dbo.Alliances:
  AllianceId 5FEAFC8C-365B-43EA-A5A7-0818419F9261
  ChatConversationId C2B28689-D74C-4936-A1BA-A573C408B4D4

dbo.AllianceMemberships:
  DA420F03-...  Leader  RemovedAtUtc NULL   (Jeff)
  77510147-...  Member  RemovedAtUtc NULL   (Stara)

dbo.ChatConversationParticipants (ConversationId C2B28689-...):
  DA420F03-...  Leader  RemovedAtUtc NULL  CanRead 1  CanWrite 1  (Jeff)
  77510147-...  Member  RemovedAtUtc NULL  CanRead 1  CanWrite 1  (Stara)
```

`ChatService.RequireRead`'s exact predicate is:

```csharp
ChatConversationParticipant? participant = repository.GetParticipant(conversationId, playerId);
if (participant == null || participant.RemovedAtUtc != null || !participant.CanRead)
    throw new UnauthorizedAccessException("forbidden");
```

Stara's row satisfies all three conditions for allow. Server-side, this
request could not legitimately return `forbidden` at the time the data was
read. **The membership → chat participant sync worked correctly for this
acceptance; nothing here needed repair.**

Shortly after, the CEO reproduced the flow again in the same Play Mode
session (no code change had shipped yet) and it worked — Stara opened
Alliance Chat successfully. The failure did not reproduce a second time, and
the only trace of the first failure (a `Debug.LogWarning` with the real
`errorCode`) was already gone once Play Mode had restarted in between.

## What this actually is

Given the data was proven correct, the most likely explanation is a
**transient condition** at the exact moment Stara first opened the chat tab —
plausibly a timing/race window immediately after `AcceptInvitation` (client
opening the chat screen before its own session/model state had settled), a
one-off transport hiccup, or a stale local chat-provider state left over from
before she was a member. It was not a data corruption and not a permanent
membership/participant desync.

Two real, independent problems were found and fixed regardless, because they
made this exact class of failure **undiagnosable** and would make any future
recurrence just as hard to prove:

### 1. Silent failure swallowing in `AllianceService`

`SyncChatParticipantAdded`, `SyncChatParticipantRemoved`, and
`CreateOrLinkAllianceChat` all catch `Exception` and continue silently by
design (chat sync must never block or roll back a real membership change –
correct behavior, unchanged). But **none of them logged anything**, so a real
SQL failure on any of these paths would be indistinguishable from "nothing
went wrong" — exactly the situation this mission started in. Fixed: added an
optional `ILogger<AllianceService>? logger = null` constructor parameter
(same optional/nullable/backward-compatible pattern already used for
`chatManager`/`chatRepository`/`playerDirectory` in this class — every
existing `AllianceServiceTests` constructor call keeps compiling unchanged),
and every one of those three catch blocks now logs a warning with the real
exception before continuing. DI (`AddBeeKingdomAlliance`) needs no change —
ASP.NET Core's container resolves `ILogger<AllianceService>` automatically.

### 2. The on-screen error message was hardcoded, not the real code

`HiveViewProductUiPresenter.EnsureAllianceChatOpenedAndSelected` always set
`allianceChatAccessDeniedReason = "not_a_member"` whenever the chat status
came back `AuthenticationRequired` **or** `Error` — a generic catch-all for
two different underlying `LivingHiveChatStatus` values covering several
different `RemoteChatError` cases (`Unauthorized`, `LocalAccountMismatch`, or
literally anything unclassified). The **real** server/transport code was
already captured (`afterSelect.ErrorCode`) but only ever reached a
`Debug.LogWarning` — invisible outside the Unity console, and gone the moment
Play Mode restarts (exactly what happened here: the CEO had already
restarted before this mission could inspect the console). Fixed: the on-screen
label now shows the real `afterSelect.ErrorCode` when one is present,
falling back to `"not_a_member"` only when it's empty. A future occurrence
is now provable from a screenshot alone, without needing to catch it live in
the Editor console.

## Answering the mission's questions directly

- **Was this a membership → chat sync bug?** No evidence of one at the data
  level. The one production sample available was fully correct.
- **Was it a PlayerId identity mismatch (AccountId vs PlayerId, legacy
  Accounts, etc.)?** Not provably — Stara's `AllianceMemberships.PlayerId`
  and `ChatConversationParticipants.PlayerId` are byte-for-byte identical
  (`77510147-CC80-4922-9BDE-AA8A296CDD68`), and the chat opened successfully
  on retry using the same account/session. If her live token had resolved to
  a *different* PlayerId, the retry would have failed identically — it
  didn't.
- **Was it the malformed backslash accept route from M043S's note?**
  Verified not present anywhere in `Program.cs` — already correct
  (`/alliance/v1/invitations/{invitationId:guid}/accept`), confirmed by
  M043T-CL. Unrelated to this bug regardless (accept succeeded; this bug is
  about reading the chat afterward).
- **Was `AcceptInvitation_AddsRealChatParticipant` (M043T) representative of
  production?** Re-verified: yes for what it tests (the call happens,
  `UpsertParticipant` is invoked with the right arguments), and production
  SQL now additionally proves the *SQL-backed* write actually persisted
  correctly for this real acceptance — the test's in-memory repository choice
  was not masking a persistence-layer bug in this instance.

## Files touched

- `Server/src/BeeKingdom.Alliance/AllianceService.cs` — optional
  `ILogger<AllianceService>` + logging in the three previously-silent catch
  blocks.
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` —
  `allianceChatAccessDeniedReason` now shows the real error code instead of a
  hardcoded label; alliance chat message bubbles now align right (green) for
  the current player's own messages and left (blue) for everyone else's,
  matching the convention Jeff asked for while retesting this fix (own vs.
  others' messages were previously all pinned left, indistinguishable).

No production data was mutated. No new invitation, no leave/rejoin, no second
Alliance, no second Chat conversation, no Unity-only bypass of the real
server-authoritative check.

## Verdict

| # | Criterion | Result |
|---|---|---|
| A | Stara Alliance membership valid? | ✅ YES |
| B | Stara Chat participant existed before fix? | ✅ YES (proven by SQL, at time of investigation) |
| C | Exact `not_a_member` predicate proven? | ✅ YES (`ChatService.RequireRead`) |
| D | Canonical PlayerId consistent? | ✅ YES (identical across Membership/Participant, retry succeeded on same session) |
| E | Correct existing ChatConversationId used? | ✅ YES (`C2B28689-...`, same conversation, Jeff's history intact) |
| F | Production/test discrepancy explained? | ⚠️ PARTIAL — no discrepancy found; the one production data point matched the test's expectation. Root cause of the *original* failure is most likely transient/timing, not proven with certainty since it didn't reproduce for direct capture |
| G | Root cause proven? | ⚠️ PARTIAL — server-side correctness proven; the original failure's exact trigger is not, because it stopped reproducing before it could be captured live |
| H | Architecture fixed at authoritative boundary? | ✅ YES — silent-failure gap in `AllianceService`'s chat sync closed (logging added); misleading fixed on-screen label replaced with the real code, so any future occurrence is self-diagnosing |
| I | Existing Alliance Test preserved? | ✅ YES — untouched |
| J | Existing Chat history preserved? | ✅ YES — Jeff's message intact, same conversation |
| K | Existing Stara membership recoverable without rejoin? | ✅ YES — never needed touching, still Member |
| L | All Alliance join/leave paths consistent? | ✅ YES — `SyncChatParticipantAdded`/`Removed` are the single shared sync point for every path (create/join/accept/application-accept/kick/leave/dissolve/promote/demote/transfer); not modified beyond adding logging |
| M | Tests green? | ✅ YES — `AllianceServiceTests` + `AllianceChatIntegrationTests`: 62/62. Full server build: 0 errors |
| N | Server deployment required? | **YES** — see below |
| O | Production reconciliation required? | ❌ NO — data already correct, nothing to reconcile |
| P | READY FOR CEO STARA CHAT RETEST? | ✅ YES — already retested live during this mission and confirmed working |

## SERVER DEPLOYMENT REQUIRED — YES

File: `Server/src/BeeKingdom.Alliance/AllianceService.cs` (logging-only
change — no behavior change to any existing success/failure path, adds
observability for the next time a chat-sync exception is actually thrown).
Not deployed by this session; CEO to authorize separately.

## Next test for the CEO

Nothing further required on the Stara/Alliance Test flow — already
confirmed working live. If `"Accès au chat de l'alliance perdu (...)"` is
ever seen again, the parenthesized code will now be the real server/transport
error instead of a hardcoded placeholder — screenshot it directly, no need to
catch it live in the Editor console this time.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
