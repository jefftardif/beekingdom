# SQL schema inventory and gaps

Status: SERVER-B-057 local readiness only. No world-map draft below is a
registered migration.

## Registered bootstrap

| Script | Purpose | Execution boundary |
| --- | --- | --- |
| `001_create_database.sql` | Legacy/default database creation | Bootstrap only; excluded from transactional migrations |

The runtime migration runner creates the configured database from its migration
connection and does not execute this bootstrap script inside a transaction.

## Registered migrations

| Script | Tables or change | Important constraints/indexes |
| --- | --- | --- |
| `010_schema_version.sql` | `dbo.SchemaVersion` | primary key and unique `ScriptName` |
| `011_schema_version_uniqueness.sql` | upgrade guard for `SchemaVersion` | refuses duplicates, then creates unique index if needed |
| `020_accounts.sql` | `dbo.Accounts` | unique player and email; status index |
| `030_authentication_sessions.sql` | `dbo.AuthenticationAccounts`, `dbo.AuthenticationSessions` | unique auth player/email; account/player/expiration session indexes |
| `040_colonies.sql` | `dbo.Colonies` | player, status and `WorldId` indexes |
| `050_colony_snapshots.sql` | `dbo.ColonySnapshots` | colony/revision and creation-time indexes |

Current SQL repositories cover accounts, credentials, sessions, colonies and
colony snapshots. They do not yet provide an official atomic account +
credential + colony onboarding transaction. `IUnitOfWorkFactory` and
`IBackupService` are still non-SQL placeholders.

## Runtime versus migration identity

`SqlConnectionFactory` exposes separate runtime and migration connections.
Dedicated names/values take precedence over the legacy fallback. Repositories
use the runtime connection; `SqlServerMigrationRunner` and database creation use
the migration connection.

The local proof uses one Windows identity because LocalDB is per-user. A future
staging wave must prove two distinct least-privilege identities before enabling
SQL persistence.

## Deferred MMO schemas

The following authoritative tables are missing and intentionally remain
unregistered:

| Area | Minimum future table | Required scope/constraints |
| --- | --- | --- |
| Chunk ownership | `world_chunks` | `(WorldId, ChunkX, ChunkY)` key, `GameServerId`, `RegionKey`, revision |
| World hives | `world_hive_nodes` | world-scoped colony and position uniqueness, chunk ownership FK |
| Resource fields | `world_resource_nodes` | world-scoped position, server-only amount, occupancy/regeneration indexes |
| Flights/marches | `world_flights` | world owner, due-state index, server times, world-scoped idempotency |

Companion schemas still requiring separate design approval include flight troop
manifests, cargo, map events/outbox, projection revisions, visibility snapshots,
spawn reservations, sector leases, alliance territories, wonders, hostile
nests, retention, partitioning and archival.

`world-schema-readiness-dry-run.sql` checks only the four core relational shapes,
foreign keys and cross-world key behavior inside a rolled-back local
transaction. It does not authorize gameplay commands, endpoints or MMO live
claims.
