# Bee Kingdom Account Architecture

## Scope

`BeeKingdom.Accounts` owns persistent player identity data. A Bee Kingdom account represents a person; a colony represents a game state. The Account Service never owns colony, inventory, world, or simulation data.

## Components

| Component | Responsibility |
| --- | --- |
| `AccountManager` | Public facade for account operations. |
| `AccountService` | Validates transitions, coordinates repository, defaults, diagnostics, and events. |
| `IAccountRepository` | Persistence boundary with O(1) lookup targets by account id, player id, and email. |
| `InMemoryAccountRepository` | Initial testable repository implementation. |
| `AccountProfile` | Persistent identity fields. |
| `AccountSettings` | Durable account-level settings. |
| `AccountPreferences` | Extensible user preferences. |
| `AccountProgression` | Global non-colony progression history. |
| `AccountDiagnostics` | Account counts, status counts, daily creations, modifications, processing ticks. |

## Account Data

`AccountProfile` contains:

* `AccountId`
* `PlayerId`
* `DisplayName`
* `Email`
* `Language`
* `TimeZone`
* `Country`
* `CreationDate`
* `LastLogin`
* `Status`

## Status Transitions

Supported statuses:

* `PendingVerification`
* `Active`
* `Suspended`
* `Banned`
* `Deleted`

Deleted accounts are terminal. Transitions are validated by `AccountService`.

## Preferences

Preferences include language, notifications, privacy, graphics, audio, social preferences, and an extension dictionary for future settings without reshaping the core model.

## Persistence

The current repository is in-memory for deterministic tests. A SQL-backed repository should replace `InMemoryAccountRepository` once persistence specifications define schemas and migrations.
