# Bee Kingdom Authentication Architecture

## Scope

`BeeKingdom.Authentication` is the official identity entry point for Bee Kingdom. It authenticates players, creates sessions, validates and rotates tokens, revokes access, logs out sessions, and records security events.

It contains no gameplay, colony, inventory, world, or simulation logic.

## Components

| Component | Responsibility |
| --- | --- |
| `AuthenticationManager` | Facade exposing Authenticate, RefreshToken, ValidateToken, RevokeToken, Logout, LogoutAllSessions, QuerySession. |
| `AuthenticationService` | Coordinates providers, sessions, tokens, diagnostics, and events. |
| `IAuthenticationProvider` | Provider extension point for email/password, Google, Apple, Steam, Epic, or guest accounts. |
| `EmailPasswordAuthenticationProvider` | Initial email/password provider. |
| `AuthenticationTokenManager` | Creates opaque access/refresh tokens, hashes stored tokens, rotates refresh tokens, revokes tokens. |
| `AuthenticationSessionValidator` | Validates access token, session state, revocation, and expiration. |
| `AuthenticationDiagnostics` | Tracks successes, refusals, invalid attempts, active sessions, expired sessions, and average auth ticks. |

## Security Choices

* Passwords are hashed with PBKDF2-SHA256 and random salts.
* Access and refresh tokens are opaque random values.
* Stored token values are SHA-256 hashes, not plaintext tokens.
* Refresh token rotation revokes the previous refresh token.
* Lockout settings, token lifetimes, max sessions, max attempts, and minimum client version are configurable through `AuthenticationOptions`.
* Provider-specific secrets are not stored in code.

## Events

Authentication publishes internal security events through `IAuthenticationEventSink`:

* `PlayerAuthenticated`
* `AuthenticationFailed`
* `SessionCreated`
* `SessionExpired`
* `SessionRevoked`
* `PlayerLoggedOut`

## Current Persistence

The initial implementation uses in-memory stores for accounts, sessions, and tokens so the service is fully testable. SQL-backed implementations should replace these stores in a later persistence specification without changing the public authentication API.
