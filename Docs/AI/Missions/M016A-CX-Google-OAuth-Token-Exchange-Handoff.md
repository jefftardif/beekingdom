# M016A-CX GOOGLE OAUTH TOKEN EXCHANGE RESULT

Owner: CX  
Reviewer: GPT  
Status: Implementation complete for CEO retest; not committed.

## Handoff Summary

OC had already proven that HiveMap reaches `http://localhost:5067`, readiness can naturally become `ConnectionTruthState.Ready` with explicit local Live environment variables, the Google browser flow opens, the callback receives an authorization response, and one clean CEO login attempt results in one server-side `POST https://oauth2.googleapis.com/token`.

CX did not restart the investigation. The remaining work focused on the server-side token request and safe diagnostics.

## Actual Google Error

The real CEO Google rejection is now known:

```text
[BeeKingdom.Auth] Google token exchange failed:
HTTP 400 invalid_request:
Could not determine client ID from request.
```

This is the authoritative root-cause evidence for the final M016A correction.

M016A keeps the rejection observable safely in the server console for any non-success Google token response:

```text
[BeeKingdom.Auth] Google token exchange failed: HTTP <status> error=<google_error> error_description=<google_error_description>
```

Only these safe fields are captured:

- HTTP status;
- `error`;
- `error_description`.

The implementation does not log authorization code, code verifier, access token, refresh token, ID token, client secret, authorization headers, or raw response body.

Targeted tests prove safe extraction with representative Google errors:

- `invalid_request`;
- `invalid_client`;
- `invalid_grant`.

The next CEO retest should no longer report `Could not determine client ID from request`; the server now logs token request metadata proving `client_id` presence before calling Google.

## Confirmed Google Error — Missing Client ID

Safe Google error:

- HTTP status: `400`;
- error: `invalid_request`;
- error_description: `Could not determine client ID from request.`

Why `ClientId` was absent:

- Unity had the public OAuth client ID and used it to build the Google authorization URL.
- The Unity-to-server `GoogleLoginRequest` did not include that public client ID.
- BeeKingdom.Server therefore depended only on its own `GoogleOAuthOptions.ClientId`.
- In the actual runtime used for CEO validation, the server-side token exchange reached Google with no valid `client_id` in the form.

Exact configuration/DI/data-flow defect:

- `GoogleOAuthOptions` is bound from the `GoogleOAuth` config section.
- The development JSON now contains that section, and a targeted test proves it binds the expected public client ID.
- However, relying only on server config leaves a data-flow gap: the client ID used in Unity authorization was not carried into `/auth/login/google`, so the token exchange could be out of parity with the actual authorization request.

Exact code fix:

- `GoogleLoginRequest` now carries `OAuthClientId`.
- `UnityMobileAccountSessionRestTransport` serializes `oauthClientId` into `/auth/login/google`.
- `MobileAccountSessionRuntimeBootstrap` passes `configuration.GoogleOAuthClientId`, the same value used for `GoogleOAuthLoginFlow.RunAsync`.
- `GoogleLoginHttpRequest` and `GoogleAuthenticationRequest` now carry `OAuthClientId`.
- `GoogleOAuthIdentityExchanger` resolves an effective client ID from server config and the Unity request:
  - if both are present, they must match;
  - if server config is empty, the Unity request client ID is used;
  - if both are absent, the exchange fails before contacting Google;
  - the outgoing token form always includes `client_id=<effectiveClientId>`.

Outgoing-form test result:

- Targeted M016A tests pass: 6 passed, 0 failed.
- They prove:
  - configured `ClientId` is present in outgoing form;
  - outgoing `client_id` matches the configured value;
  - Unity/request `OAuthClientId` is used when server config is empty;
  - mismatched configured/request client IDs fail before Google;
  - empty/unconfigured `client_secret` is omitted;
  - redirect URI and PKCE verifier fields remain unchanged.

Final runtime ClientId parity proof:

- Unity `MobileAccountSessionRuntime.asset`: `209838375708-kfr4t9k99s620ndq602jkprddvu3mvsq.apps.googleusercontent.com`.
- Server development `GoogleOAuth:ClientId`: `209838375708-kfr4t9k99s620ndq602jkprddvu3mvsq.apps.googleusercontent.com`.
- Development-safe token request metadata logs now report:
  - exact public `client_id`;
  - whether server configured client ID is present;
  - whether Unity/request client ID is present;
  - whether a client secret is configured;
  - redirect URI;
  - code verifier presence and length only.

No authorization code, verifier value, token, bearer header, client secret, or raw body is logged.

## Root Cause

The local repo/server configuration and token request were not in parity with the Unity authorization request:

1. Unity authorized with the public Google client ID stored in `MobileAccountSessionRuntime.asset`.
2. The Unity-to-server Google login DTO did not carry that OAuth client ID.
3. The server token exchange used only `GoogleOAuthOptions.ClientId`.
4. The actual CEO runtime token exchange reached Google without a valid `client_id`.
5. The server also previously sent a `client_secret` field even when the configured value was empty.

That combination produced the Google rejection `invalid_request: Could not determine client ID from request.` The fix makes the authorization request client ID flow explicitly to the server and into the token form.

## Authorization Request

Unity authorization request source:

- client ID: `Assets/BeeKingdom/Playground/Resources/BeeKingdom/MobileAccountSessionRuntime.asset`;
- authorization endpoint: `https://accounts.google.com/o/oauth2/v2/auth`;
- redirect URI: `http://127.0.0.1:53682/oauth/callback`;
- response type: `code`;
- scopes: `openid email profile`;
- PKCE challenge method: `S256`;
- browser callback listener prefix: `http://127.0.0.1:53682/oauth/callback/`.

The authorization request path was not changed in M016A-CX.

## Token Request

Server token request now sends:

- `grant_type=authorization_code`;
- `client_id`;
- `redirect_uri`;
- `code`;
- `code_verifier`;
- `client_secret` only when explicitly configured and non-empty.

The request remains `application/x-www-form-urlencoded` through `FormUrlEncodedContent`.

`Server/src/BeeKingdom.Server/appsettings.Development.json` now configures the same public Google client ID used by Unity. No client secret is committed.

## Redirect URI Analysis

Exact redirect URI path:

- authorization request: `http://127.0.0.1:53682/oauth/callback`;
- local callback result returned by `GoogleOAuthLoginFlow`: `http://127.0.0.1:53682/oauth/callback`;
- Unity to BeeKingdom.Server request: `redirectUri` plus `oauthClientId`;
- BeeKingdom.Server to Google token request: same `redirectUri` value passed by Unity.

No normalization, rewriting, slash insertion, or substitution was added.

Google documentation requires the token request `redirect_uri` to be one of the redirect URIs configured for the OAuth client and to match the flow. The project uses the desktop/installed-app loopback pattern, not a production web callback.

## PKCE Analysis

Current Unity flow:

- code verifier generated by `GenerateUrlSafeToken(32)`;
- resulting verifier length is 43 Base64URL characters;
- generated characters are URL-safe Base64 without padding;
- challenge is `BASE64URL(SHA256(ASCII(code_verifier)))`;
- `code_challenge_method=S256`;
- exact verifier is returned from the callback result and sent to BeeKingdom.Server;
- server sends that verifier to Google as `code_verifier`.

M016A-CX did not log verifier contents.

## OAuth Client Type Analysis

The repository implementation is an installed/desktop-app style OAuth flow:

- system browser;
- local loopback redirect;
- PKCE S256;
- no committed client secret.

Google documentation states that installed apps cannot keep secrets confidential and describes loopback redirect URIs and PKCE for mobile/desktop apps. Google also lists `client_secret` as optional in the installed/mobile/desktop token exchange.

Therefore the BeeKingdom local development flow expects a Google OAuth client compatible with a desktop/installed application loopback redirect, with the public client ID configured consistently in Unity and BeeKingdom.Server.

If the CEO Google Cloud client is actually a Web application client that requires a client secret, or if its authorized redirect URI does not allow `http://127.0.0.1:53682/oauth/callback`, the next retest will still fail with a safe Google error. In that case, the required change is in Google Cloud configuration, not a BeeKingdom bypass:

- use a Desktop app OAuth client for the local loopback PKCE flow, or
- securely configure the matching Web client secret outside source control and ensure the exact redirect URI is registered.

## Changes

- Added safe structured Google token-exchange diagnostics in `GoogleOAuthIdentityExchanger`.
- Added `GoogleOAuthTokenExchangeException` carrying only status, `error`, and `error_description`.
- Changed token form construction so `client_secret` is emitted only when non-empty.
- Added a fast fail when no effective OAuth client ID is available from either server config or the Unity request.
- Added the public Google OAuth client ID to development server config to match Unity.
- Added `OAuthClientId` to the Unity login request and server Google auth request.
- Added server-side client ID parity enforcement when both Unity request and server config provide a value.
- Added development-safe token request metadata showing public client ID parity before Google `/token`.
- Removed OC temporary Unity logs that printed HTTP response bodies.
- Verified/restored `/auth/login/google` behavior so Unity continues to receive generic `auth.google_sign_in_failed` instead of Google diagnostic detail.
- Added targeted server tests for:
  - form-urlencoded token request construction;
  - redirect URI preservation;
  - PKCE verifier field propagation;
  - omission of empty client secret;
  - inclusion of non-empty configured client secret;
  - fallback to Unity/request OAuth client ID when server config is empty;
  - mismatch rejection before Google;
  - development config binding of the expected public client ID;
  - safe Google error parsing without raw-body leakage.

## Files Changed

- `Assets/BeeKingdom/Networking/MobileAccountSessionClient.cs`
- `Assets/BeeKingdom/Networking/UnityMobileAccountSessionRestTransport.cs`
- `Assets/BeeKingdom/Playground/Editor/MobileAccountSessionUiTests.cs`
- `Assets/BeeKingdom/Playground/MobileAccountSessionRuntimeBootstrap.cs`
- `Assets/BeeKingdom/Playground/Resources/BeeKingdom/MobileAccountSessionRuntime.asset`
- `Assets/BeeKingdom/Tests/Editor/MobileAccountSessionClientTests.cs`
- `Server/src/BeeKingdom.Authentication/AuthenticationService.cs`
- `Server/src/BeeKingdom.Authentication/Models/AuthenticationModels.cs`
- `Server/src/BeeKingdom.Authentication/Providers/GoogleOAuthIdentityExchanger.cs`
- `Server/src/BeeKingdom.Authentication/Providers/IGoogleIdentityExchanger.cs`
- `Server/src/BeeKingdom.Server/Program.cs`
- `Server/src/BeeKingdom.Server/appsettings.Development.json`
- `Server/tests/BeeKingdom.Tests/GoogleOAuthIdentityExchangerTests.cs`
- `Docs/AI/Missions/M016A-CX-Google-OAuth-Token-Exchange-Handoff.md`

## OC Changes Retained/Reverted

Retained:

- Unity local base URL corrected to `http://localhost:5067`.
- `appsettings.Development.json` cleanup that removes broad duplicated server sections and keeps explicit local readiness as environment-variable driven.
- OC report left untouched.

Reverted/removed:

- Unity debug logs that printed readiness/HTTP response bodies.
- Program-level exception wrapper that could surface Google diagnostic text back to Unity.

Refined:

- OC safe error extraction became a typed exception with bounded safe fields and tests.
- The final CEO-proven missing-client-ID defect was fixed by carrying the Unity OAuth client ID through the authenticated login handoff.

## Security Review

- PKCE remains enabled.
- Google identity verification remains through `GoogleJsonWebSignature.ValidateAsync`.
- No unsigned identity is accepted.
- No fake Google response is accepted.
- No token, authorization code, code verifier, client secret, bearer header, or raw body is logged.
- Token request metadata logs include the public client ID, redirect URI, boolean secret presence, and verifier length only.
- Production readiness guardrails remain unchanged.
- Unity still receives generic auth failure on Google exchange failure.
- No secret is committed. The committed Google OAuth value is the already-public client ID also present in the Unity runtime asset.

## Validation

Server build:

- `dotnet build Server/src/BeeKingdom.Authentication/BeeKingdom.Authentication.csproj --no-restore -v:minimal /clp:ErrorsOnly /nr:false`
  - 0 errors
- `dotnet build Server/src/BeeKingdom.Server/BeeKingdom.Server.csproj -c Release --no-restore -v:minimal /clp:ErrorsOnly /nr:false`
  - 0 errors

Server tests:

- Targeted:
  - `dotnet test Server/tests/BeeKingdom.Tests/BeeKingdom.Tests.csproj -c Release --no-restore --filter GoogleOAuthIdentityExchangerTests --logger "trx;LogFileName=m016a_google_oauth_identity_clientid_release.trx" /nr:false`
  - 6 passed, 0 failed, 0 skipped
- Full:
  - `dotnet test Server/tests/BeeKingdom.Tests/BeeKingdom.Tests.csproj -c Release --no-restore --logger "trx;LogFileName=m016a_full_server_clientid_serial_release.trx" /nr:false -- NUnit.NumberOfTestWorkers=1`
  - 391 passed, 8 skipped, 0 failed
  - This is the previous 385/8/0 baseline plus 6 M016A tests.

Unity compile:

- `dotnet build BeeKingdom.Networking.csproj --no-restore -v:minimal /clp:ErrorsOnly /nr:false`
  - 0 errors
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal /clp:ErrorsOnly /nr:false`
  - 0 errors
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal /clp:ErrorsOnly /nr:false`
  - 0 errors

Notes:

- A Debug `dotnet test` run was blocked by an already-running `BeeKingdom.Server.exe` locking Debug output files. It was not treated as a code failure; validation was rerun in Release.
- One parallel full-suite Release run hit an unrelated file move/access collision in `CombatSquadReservationEndpointTests`; the suite was rerun serially with 391/8/0.
- Unity Editor batchmode was not run in this M016A pass.

## CEO Manual Validation Required

1. Start BeeKingdom.Server with the explicit local Live readiness environment variables already documented by OC.
2. Confirm server uses `http://localhost:5067`.
3. Confirm Unity `MobileAccountSessionRuntime.asset` uses `http://localhost:5067`.
4. Open `Environment2D5D_HiveMap_Test` in Unity Play Mode.
5. Open `Connexion`.
6. Verify `Se connecter avec Google` appears only when readiness is `Ready`.
7. Click `Se connecter avec Google`.
8. Complete Google auth in the browser.
9. Confirm the server console prints token request metadata with:
   - `client_id=209838375708-kfr4t9k99s620ndq602jkprddvu3mvsq.apps.googleusercontent.com`;
   - `request_client_id_present=true`;
   - no code/verifier/token values.
10. If it fails, copy only the server console line:
   - HTTP status;
   - `error`;
   - `error_description`.
11. Expected success path:
    - Google token exchange succeeds;
    - BeeKingdom session is established;
    - HiveMap loads authenticated;
    - M016B can validate Activities with real server state.

## Remaining Issues

- CEO retest still required because CX did not authenticate as CEO.
- The previously confirmed live error was `invalid_request: Could not determine client ID from request`; the next retest should prove that error is gone.
- If the retest reports `invalid_client` or a client-secret requirement, Google Cloud configuration must be corrected or a secret must be provided securely outside source control.
- If the retest reports `redirect_uri_mismatch`, Google Cloud must register the exact loopback redirect URI or the project must move to the configured desktop redirect mechanism.
- Existing untracked OC/test artifacts remain outside this report and were not committed.

## Confidence

MEDIUM-HIGH.

The confirmed missing-client-ID defect is corrected and tested through the Unity-to-server-to-Google form path. Final confidence becomes HIGH only after CEO retest confirms the live Google exchange succeeds.

## References

- Google OAuth 2.0 for Mobile and Desktop Apps: https://developers.google.com/youtube/reporting/guides/authorization/installed-apps
- Google OAuth 2.0 overview / installed applications: https://developers.google.cn/identity/protocols/oauth2
