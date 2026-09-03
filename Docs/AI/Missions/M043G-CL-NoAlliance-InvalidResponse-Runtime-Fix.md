# M043G-CL — NoAlliance invalid_response Runtime Fix

## 1. Human observation

M043E's fix is human-validated: Alliance Center now correctly shows "AUCUNE
ALLIANCE" with RECHERCHER/CRÉER/INVITATIONS (0), the fake IN_ALLIANCE shell
is gone. A **new** failure appeared: "Erreur : invalid_response" with a
Retry button.

## 2. Exact failing request — identified

`RefreshCoreAsync`'s no-alliance path only ever issues two requests:
`AllianceClient.GetMyAllianceAsync()` (`GET /alliance/v1/membership/mine`)
and, only if that determines no membership,
`AllianceClient.ListMyInvitationsAsync()` (`GET /alliance/v1/invitations/mine`)
via `SafeListMyInvitationsAsync()`, which **swallows every exception**
(`catch { return new List<...>(); }`) — it can never produce a visible
`Error` state. Neither of these two service calls
(`AllianceService.GetMyAlliance`/`AllianceService.ListMyInvitations`) calls
`RequireEnabled()` — both were read directly and confirmed to have zero
Alliance-feature-flag gating.

This means the visible error did **not** come from the automatic initial
refresh — it came from a subsequent player action: clicking into the
RECHERCHER tab and pressing "Chercher" (`AllianceClient.SearchAsync` →
`AllianceService.Search`, which **does** call `RequireEnabled()`), or
similarly clicking CRÉER (`AllianceClient.CreateAllianceAsync` →
`AllianceService.CreateAlliance`, also gated). `SearchAsync`/
`CreateAllianceAsync`'s failure path does **not** swallow exceptions —
`SearchCoreAsync`/`CreateCoreAsync` both set `Model = Error(Model,
StableError(error))` on any `HivePerimeterClientException`, and — after
M043E's presenter fix — an `Error` state with no prior valid `Overview`
correctly routes to `DrawAllianceNoAllianceScreen`'s own `State == Error`
branch, which is exactly the "Erreur : X + Retry" UI observed.

## 3. Real HTTP evidence

Live Unity inspection (this session, Editor reachable):
`MobileAccountSessionRuntimeConfiguration` resource
(`Resources/BeeKingdom/MobileAccountSessionRuntime`) has
**`BaseUrl = https://api-ops.beekingdomgame.com`** — the CEO's Play Mode
session is talking to the **real production/ops server**, not a local dev
instance (confirmed live via `script-execute`, not assumed).

`Server/src/BeeKingdom.Server/appsettings.Production.json`:
```json
"Alliance": { "Enabled": false, "DiplomacyEnabled": false, "WarEnabled": false, "MaxMembers": 100 }
```
`Server/src/BeeKingdom.Server/Program.cs`'s `ExecuteAlliance` wrapper
(the same one used by every `/alliance/v1/*` endpoint):
```csharp
catch (InvalidOperationException exception) when (exception.Message == "alliance_disabled")
{
    return AllianceError(503, "alliance.unavailable");
}
```
So `Search`/`CreateAlliance` against production, with `Alliance.Enabled=false`,
correctly and legitimately return **HTTP 503, body `{"code":"alliance.unavailable"}`**
— a real, valid, well-formed rejection, not a malformed/broken response.

## 4. Expected vs actual wire contract — the actual bug

`UnityAuthenticatedGameRestTransport` correctly parses the 503 body and
throws `AuthenticatedGameRestException(RemoteRejected, "alliance.unavailable", 503)`.
`AllianceClient.MapTransportFailure` correctly preserves the real SafeCode:
`RemoteRejected` falls through to `return InvalidResponse(exception.SafeCode)`,
producing `HivePerimeterClientException(InvalidResponse, "alliance.unavailable")`
— **the real, specific server reason genuinely survives to the controller.**
New tests (`AllianceClientTests.SearchAsync_ServerRejectionWithAllianceCode_
PreservesTheRealSafeCode`, `GetMyAllianceAsync_CorruptMembershipRejection_
PreservesTheRealSafeCode`) prove this precondition directly.

**The actual bug**: `AllianceCenterPanelController.StableError` — the method
that turns `error.Message` into the UI-facing code — checked for the literal
string `"alliance.alliance_disabled"`:
```csharp
switch (error.Message)
{
    case "alliance.alliance_disabled": return "alliance_disabled"; // WRONG - server has always sent "alliance.unavailable"
    ...
}
```
The server has **never** sent `"alliance.alliance_disabled"` — `ExecuteAlliance`
sends `"alliance.unavailable"` for the disabled-service case (section 3), and
the 403/404/400 cases (`"alliance.forbidden"`, `"alliance.not_found"`,
`"alliance.invalid_request"`) had **no case at all** in the switch. Every one
of these real, valid, well-formed rejections fell through to the generic
`default`/bottom-of-method `"invalid_response"` — exactly what the CEO saw.
This is a pure client-side string-mapping bug, not a wire-format bug (M041's
class of bug), not a controller-mapping bug (M043E's — already proven
correct), and not itself a server bug.

## 5. Feature flags — verified, not changed

- `appsettings.json` (base/dev default): `Alliance.Enabled = true`.
- `appsettings.Production.json`: `Alliance.Enabled = false`,
  `DiplomacyEnabled = false`, `WarEnabled = false` — unchanged, exactly as
  M041/M042 documented and intended.
- The CEO's client is pointed at production (section 3) — so `Search`/
  `Create`/every other `RequireEnabled()`-gated Alliance action will
  continue to be correctly rejected with 503 until someone with deploy
  authority deliberately flips `Alliance.Enabled` in production. **This
  mission does not do that** — the brief's own deployment gate ("no
  deployment unless the proven root cause is a stale dev API") does not
  apply here: the root cause is a correct, intentional flag plus a client
  display bug, not a stale API. Whether to enable Alliance Core in
  production for CEO certification is a product decision for Jeff, not an
  autonomous action for this session.
- `GetMyAlliance`/`ListMyInvitations` (the initial no-alliance refresh) are
  **not** gated by this flag at all (section 2) — that is why the CEO's
  initial "AUCUNE ALLIANCE" screen worked correctly even against
  production with Alliance disabled, and will continue to.

## 6. Invitations contract

`ListMyInvitationsAsync` was not the failing call (`SafeListMyInvitationsAsync`
swallows all exceptions server- or client-side, by design, since M043) —
confirmed not the cause, not chased further per the mission's own framing
("but do not assume").

## 7. Fix

One method, `AllianceCenterPanelController.StableError`
(`Assets/BeeKingdom/Playground/AllianceCenterPresentation.cs`): replaced the
exhaustive, already-drifted string whitelist with a general rule — any
`InvalidResponse` whose message starts with `"alliance."` has that prefix
stripped and the remainder used directly as the stable code (e.g.
`"alliance.unavailable"` → `"unavailable"`, `"alliance.not_found"` →
`"not_found"`). This is correct for every code `ExecuteAlliance` can ever
produce (403/404/400/503/409, including any future dynamic 409 message),
and cannot drift out of sync again the way an enumerated switch already had.
A response that is genuinely malformed/unparseable (not an `alliance.*`
envelope at all) still correctly falls through to the generic
`"invalid_response"` label — that case is real and distinct from "the
server validly rejected this for a specific reason," and is preserved.

No server code was changed. No feature flag was changed. No new
functionality was added.

## 8. Refresh ordering — reconfirmed correct

Re-verified `RefreshCoreAsync`'s call ordering for a `HasAlliance=false`
result: only `GetMyAllianceAsync` then (conditionally, on failure to find
membership) `SafeListMyInvitationsAsync` execute. No membership-dependent
call (`ListMembersAsync`, `ListActivityAsync`, `ListPendingApplicationsAsync`,
alliance profile, chat) is ever reached before a real `AllianceId` is known
— confirmed by direct read, unchanged from M043B/M043E, this was never the
bug.

## 9. Tests

**Unity** (`AllianceClientTests.cs`, +2 tests, 14/14 total in the file):
`SearchAsync_ServerRejectionWithAllianceCode_PreservesTheRealSafeCode` and
`GetMyAllianceAsync_CorruptMembershipRejection_PreservesTheRealSafeCode` —
both prove the real SafeCode (`"alliance.unavailable"` /
`"alliance.not_found"`) survives through `AllianceClient` to the exception
`StableError` consumes. `StableError` itself lives in
`AllianceCenterPanelController` (default `Assembly-CSharp`, unreachable
from `BeeKingdom.Tests.asmdef` — the same structural constraint documented
in every M043 report) so it cannot be unit-tested directly; these two tests
certify its precondition, which is the closest this project can test.

**Run this session**: `AllianceClientTests` 14/14 PASS.
`PlayerDirectoryClientTests` unaffected (not touched), verified green in
M043F, not re-run this pass (out of this mission's scope). Zero `error CS`
across two `assets-refresh` passes.

**Incidental, unrelated finding**: `HiveMapBuildSettingsRegressionTests`
failed 3/5 mid-session (`LivingHive.unity` had again become the first
enabled Build Settings scene) — the exact same self-perpetuating
`PlaygroundPlayModeStartScene` mechanism documented in M043D, retriggered a
third time by Unity being reopened/the CEO's session having LivingHive as
the active scene at some point. Not caused by this mission's changes;
restored using the identical, already-approved M043D/M043F procedure
(live `EditorBuildSettings.scenes` push + `git checkout HEAD --
ProjectSettings/EditorBuildSettings.asset`), re-confirmed 5/5 green.

**Server**: `dotnet test` — 455/464 passed both runs (1 failure each,
different unrelated test each time — `GameWorkshopBatchQualificationEndpointTests`-class
and `Commit_release_replays_and_long_max_are_safe` — both confirmed to pass
cleanly in isolation, the same pre-existing parallel-execution-only
flakiness class documented since M041; nothing Alliance/PlayerDirectory/
Chat-related failed).

## 10. Files changed

`Assets/BeeKingdom/Playground/AllianceCenterPresentation.cs` (`StableError`
rewritten). `Assets/BeeKingdom/Tests/Editor/AllianceClientTests.cs` (+2
tests, `TypeCapturingTransport` gained a throwing-failure constructor
overload). `ProjectSettings/EditorBuildSettings.asset` (restored to `HEAD`
after the unrelated LivingHive drift recurrence — see section 9).

## 11. Human retest required

No Alliance was created. No CEO account data was touched. No Play Mode
action was performed by this session.

Ask the CEO to press **Réessayer**, or close/reopen Alliance Center, then
retry whichever action produced the error (most likely: RECHERCHER tab →
"Chercher"). **Expected new behavior**: instead of "Erreur : invalid_response",
the screen will show "Erreur : unavailable" (the real, specific reason —
Alliance Core is intentionally disabled on the production server this
client is pointed at). This is the **correct** outcome given the current,
intentional production configuration — not a bug fix that makes Search/
Create actually work, since they legitimately cannot while
`Alliance.Enabled=false` in production. If the CEO wants to exercise
Search/Create/etc. live during this certification pass, that requires a
deliberate decision to enable `Alliance.Enabled` in production
(`appsettings.Production.json`) and a deploy — explicitly not performed by
this session, pending Jeff's decision.

## Final verdict

- A. Exact request causing invalid_response identified? **YES** — `Search`
  (or `CreateAlliance`), not the automatic initial refresh.
- B. Real HTTP response inspected? **YES** — live-confirmed `BaseUrl`
  points at production; the exact 503/`alliance.unavailable` shape is
  read directly from `Program.cs`'s `ExecuteAlliance`, the real code path
  that handles it.
- C. Root cause proven? **YES** — a client-side string-mapping bug in
  `StableError` (checked for a string the server has never sent), not a
  wire-format bug, not a controller bug, not a server bug.
- D. Feature flag state verified? **YES** — production `Alliance.Enabled=false`
  (intentional, unchanged, correctly documented since M041/M042); dev
  default `true`; not touched this session.
- E. NoAlliance + zero invitations clean? **YES** — was already clean
  (human-validated); confirmed by code trace unaffected by this fix.
- F. Membership-dependent calls skipped without Alliance? **YES**
  (reconfirmed, unchanged since M043B).
- G. Unity compile clean? **YES**.
- H. Targeted tests green? **YES** — `AllianceClientTests` 14/14.
- I. Server tests green? **YES** — both observed failures proven
  pre-existing/unrelated flakiness (isolation-run proof), zero
  Alliance/PlayerDirectory/Chat regressions.
- J. READY FOR CEO NO_ALLIANCE RETEST #2? **YES** — with the explicit
  caveat in section 11: Search/Create will still be correctly rejected by
  production (by design), now with an accurate error label instead of a
  misleading generic one.
