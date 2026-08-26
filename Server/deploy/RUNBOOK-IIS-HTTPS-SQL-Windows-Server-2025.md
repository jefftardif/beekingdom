# Bee Kingdom Server - IIS, HTTPS and SQL Runtime Runbook

Date: 2026-07-10
Scope: Windows Server 2025 runtime for Bee Kingdom server only.

## Objective

Deploy Bee Kingdom Server behind IIS with HTTPS and enable SQL Server runtime persistence when the environment is explicitly configured for it.

This runbook does not activate MMO-final gameplay features. It only covers server hosting, migrations, persistence provider selection and rollback.

## Runtime Modes

Default local mode:

- `Persistence:Provider=InMemory`
- No SQL connection is required.
- Repositories use in-memory stores for accounts, credentials, sessions, colonies and snapshots.

SQL runtime mode:

- `Persistence:Provider=SqlServer`
- `SqlServer:ConnectionStringName=BeeKingdomDb`
- `ConnectionStrings:BeeKingdomDb=<secured SQL Server connection string>`
- Repositories use SQL Server tables created by the migration catalog.
- Runtime and migration identities can be separated with `SqlServer:RuntimeConnectionStringName` and `SqlServer:MigrationConnectionStringName`.

## IIS Prerequisites

1. Install IIS with ASP.NET Core Hosting Bundle for .NET 8.
2. Install URL Rewrite only if required by the fronting network topology.
3. Create a Windows service account dedicated to Bee Kingdom Server.
4. Grant the app pool identity read/execute access to the published server directory.
5. Grant the app pool identity database permissions only when SQL runtime mode is selected.

## HTTPS Binding

1. Import the production certificate into the Local Machine certificate store.
2. Bind the IIS site to `https` on port `443`.
3. Select the Bee Kingdom certificate.
4. Keep `http` either disabled or redirected to `https` at the IIS edge.
5. Confirm `/health` returns `Healthy` over HTTPS before exposing traffic.

## Environment Configuration

Set environment variables at the IIS application level:

```powershell
ASPNETCORE_ENVIRONMENT=Production
Persistence__Provider=SqlServer
SqlServer__ConnectionStringName=BeeKingdomDb
ConnectionStrings__BeeKingdomDb=Server=<sql-host>;Database=BeeKingdom;User Id=<user>;Password=<secret>;TrustServerCertificate=False;Encrypt=True;
Ops__RequireAdminKey=true
Ops__AdminKey=<long-random-secret>
Ops__RequireMigrationApplyKey=true
Ops__MigrationApplyKey=<different-long-random-secret>
ServerIdentity__GameServerId=<stable-guid-for-this-server>
ServerIdentity__DefaultWorldId=<stable-guid-for-default-world>
ServerIdentity__ShardName=production-preparation
```

Required secret rules:

- `ConnectionStrings__BeeKingdomDb`, `ConnectionStrings__BeeKingdomRuntime` and `ConnectionStrings__BeeKingdomMigrations` must be stored outside source control.
- `Ops__AdminKey` must be a long random secret.
- `Ops__MigrationApplyKey` must be a different long random secret.
- Prefer `Ops__AdminKeySha256` and `Ops__MigrationApplyKeySha256` over plaintext keys when the deployment channel supports hashed secrets.
- `Ops__MigrationApplyKey` must not equal `Ops__AdminKey`; the server fails closed if both are required and identical.
- Prefer `Encrypt=True;TrustServerCertificate=False;` for Windows Server 2025 production SQL connections.
- `ServerIdentity__GameServerId`, `ServerIdentity__DefaultWorldId` and `ServerIdentity__ShardName` are public routing identifiers, not secrets.
- Keep server identity values stable across package restarts. Change them only through an explicit operations decision.

Hashed operations key mode:

```powershell
$adminKey = "<long-random-secret>"
$migrationKey = "<different-long-random-secret>"
$adminHash = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData([System.Text.Encoding]::UTF8.GetBytes($adminKey))).ToLowerInvariant()
$migrationHash = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData([System.Text.Encoding]::UTF8.GetBytes($migrationKey))).ToLowerInvariant()

Ops__RequireAdminKey=true
Ops__AdminKey=
Ops__AdminKeySha256=$adminHash
Ops__RequireMigrationApplyKey=true
Ops__MigrationApplyKey=
Ops__MigrationApplyKeySha256=$migrationHash
```

Do not write `$adminKey` or `$migrationKey` to source control, runbooks, reports or logs.

For separated runtime and migration identities:

```powershell
SqlServer__RuntimeConnectionStringName=BeeKingdomRuntime
SqlServer__MigrationConnectionStringName=BeeKingdomMigrations
ConnectionStrings__BeeKingdomRuntime=Server=<sql-host>;Database=BeeKingdom;User Id=bee_runtime;Password=<secret>;TrustServerCertificate=False;Encrypt=True;
ConnectionStrings__BeeKingdomMigrations=Server=<sql-host>;Database=BeeKingdom;User Id=bee_migration;Password=<secret>;TrustServerCertificate=False;Encrypt=True;
```

For local validation without SQL, keep:

```powershell
ASPNETCORE_ENVIRONMENT=Production
Persistence__Provider=InMemory
```

## SQL Migration Runner

Migration commands are available through `BeeKingdom.Tools`:

```powershell
dotnet BeeKingdom.Tools.dll diagnostics
dotnet BeeKingdom.Tools.dll migrate
```

`diagnostics` lists pending migrations. `migrate` applies pending migrations when `Persistence__Provider=SqlServer`.

The HTTP endpoint `POST /ops/migrations/apply` also applies migrations through the configured runner. Restrict this endpoint at the network/IIS layer until an admin authorization policy is introduced.

## Admin Operations Security

All `/ops` endpoints are admin-only when `Ops__RequireAdminKey=true`.

Required header:

```powershell
X-BeeKingdom-Admin-Key: <long-random-secret>
```

Protected endpoints:

- `GET /ops/migrations/pending`
- `POST /ops/migrations/apply`
- `GET /ops/migrations/rollback-plan`
- `GET /ops/monitoring`
- `GET /ops/readiness`
- `GET /ops/sql-production-dry-run`

If `Ops__RequireAdminKey=true` and `Ops__AdminKey` is missing, `/ops` fails closed with HTTP 503.

`Ops__AdminKeySha256` can be used instead of `Ops__AdminKey`. The request header still sends the original secret value:

```powershell
X-BeeKingdom-Admin-Key: <long-random-secret>
```

`POST /ops/migrations/apply` has an additional migration key when `Ops__RequireMigrationApplyKey=true`:

```powershell
X-BeeKingdom-Migration-Key: <different-long-random-secret>
```

If the migration key is required and missing, migration apply returns HTTP 401. If the key is required but not configured, it fails closed with HTTP 503.

`Ops__MigrationApplyKeySha256` can be used instead of `Ops__MigrationApplyKey`.

## Operations Readiness

`GET /ops/readiness` is a non-destructive readiness endpoint for Windows Server 2025 operations.

It reports:

- hosting model and target operating system;
- whether SQL Server mode is enabled;
- configured runtime and migration connection string names;
- whether runtime and migration connection strings are present;
- whether runtime and migration identities are separated;
- whether `/ops/monitoring`, `/ops/migrations/rollback-plan` and `/ops/migrations/apply` are secured;
- blocker messages when required production settings are missing.

It does not return SQL connection strings, passwords, admin keys or migration keys.

Example:

```powershell
Invoke-RestMethod `
  -Uri https://<host>/ops/readiness `
  -Headers @{ "X-BeeKingdom-Admin-Key" = "<long-random-secret>" }
```

The readiness endpoint is operational evidence only. It does not apply migrations, does not execute rollback, and does not claim gameplay readiness.

## SQL Production Dry Run Readiness

`GET /ops/sql-production-dry-run` is a non-destructive gate for production SQL dry run planning.

It verifies:

- SQL Server provider is explicitly selected;
- runtime and migration connection string names are configured;
- runtime and migration identities are separated;
- admin and migration operations keys are configured and distinct;
- a verified backup evidence reference is configured;
- a maintenance window reference is configured;
- the rollback plan has been acknowledged;
- account, credential, session, colony and snapshot tables are present in the migration catalog.

It never applies migrations, never executes rollback, never publishes deployment and never returns SQL connection strings, passwords or operations secrets.

Recommended environment variables:

```powershell
SqlProductionDryRun__TargetHost=104.129.128.136
SqlProductionDryRun__RequireBackupEvidence=true
SqlProductionDryRun__BackupEvidenceReference=<external-backup-evidence-id>
SqlProductionDryRun__RequireMaintenanceWindow=true
SqlProductionDryRun__MaintenanceWindowReference=<external-maintenance-window-id>
SqlProductionDryRun__RollbackPlanAcknowledged=true
```

Do not store backup locations containing secrets or credentials in source control, runbooks, reports or logs.

Example:

```powershell
Invoke-RestMethod `
  -Uri https://<host>/ops/sql-production-dry-run `
  -Headers @{ "X-BeeKingdom-Admin-Key" = "<long-random-secret>" }
```

`readyForDryRun=true` only means that the non-destructive dry run preconditions are configured. It does not authorize deployment, migration apply, rollback execution, live accounts, live sessions, gameplay synchronization or MMO readiness.

## SQL Permissions

The migration identity requires:

- create database permission when the Bee Kingdom database does not exist;
- create table/index permission during first migration;
- insert permission on `dbo.SchemaVersion`.

The runtime identity requires:

- read/write permissions on `dbo.Accounts`;
- read/write permissions on `dbo.AuthenticationAccounts`;
- read/write permissions on `dbo.AuthenticationSessions`;
- read/write permissions on `dbo.Colonies`;
- read/write permissions on `dbo.ColonySnapshots`;
- read permission on `dbo.SchemaVersion`.

Prefer separate migration and runtime identities in production.

## Smoke Tests

After deployment:

```powershell
.\Test-BeeKingdomServer.ps1 -BaseUrl https://<host>
```

Manual checks:

- `GET /health`
- `POST /protocol/ping`
- `POST /runtime/handshake`
- `GET /runtime/server-first-readiness`
- `GET /ops/readiness` with `X-BeeKingdom-Admin-Key`
- `GET /ops/sql-production-dry-run` with `X-BeeKingdom-Admin-Key`
- `GET /ops/migrations/pending` with `X-BeeKingdom-Admin-Key`
- `GET /ops/migrations/rollback-plan` with `X-BeeKingdom-Admin-Key`
- `GET /ops/monitoring` with `X-BeeKingdom-Admin-Key`
- `POST /accounts`
- `POST /colonies`

Accounts and colonies checks validate the backend service path only. They do not prove final MMO gameplay readiness.

## Runtime Handshake Boundary

`POST /runtime/handshake` is a public, non-gameplay handshake for client readiness.

It accepts only:

- client build;
- client environment;
- supported protocol major/minor.

It returns only:

- server name;
- server time;
- environment;
- game server id;
- default world id;
- shard name;
- current protocol version;
- protocol compatibility;
- public availability;
- maintenance message;
- fallback mode;
- explicit false live claims.

It does not authenticate a player, does not create a session, does not expose a profile, does not carry colony state, does not carry resources, does not carry bees, does not mutate persistence and does not grant gameplay authority.

Example:

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri https://<host>/runtime/handshake `
  -ContentType application/json `
  -Body '{ "clientBuild": "1.0.0", "clientEnvironment": "Unity", "supportedProtocolMajor": 1, "supportedProtocolMinor": 0 }'
```

Configuration:

```powershell
RuntimeHandshake__Availability=ServerInPreparation
RuntimeHandshake__MaintenanceMessage="Serveur Bee Kingdom en preparation."
RuntimeHandshake__FallbackMode=LocalOnly
ServerIdentity__GameServerId=<stable-guid-for-this-server>
ServerIdentity__DefaultWorldId=<stable-guid-for-default-world>
ServerIdentity__ShardName=production-preparation
```

Do not present this endpoint as MMO login, save sync, official progression or live account readiness. `gameServerId`, `defaultWorldId` and `shardName` identify the runtime route only; they do not prove that a live world, account session or synchronized Unity gameplay is available.

## Server-First Public Readiness

`GET /runtime/server-first-readiness` is a public, non-secret status contract for the server-first entry shell.

It returns:

- production target;
- handshake path;
- game server id;
- default world id;
- shard name;
- official server requirement;
- production route proof status;
- offline consultation mode;
- account/session/read model status;
- backup and rollback approval requirements;
- explicit forbidden claims.

It does not authenticate a player, does not create an account, does not create a session, does not expose ops state, does not expose SQL details and does not mutate gameplay state.

Default production-safe configuration:

```powershell
ServerFirstReadiness__ProductionTarget=104.129.128.136
ServerFirstReadiness__HandshakePath=/runtime/handshake
ServerFirstReadiness__ProductionRouteProven=false
ServerFirstReadiness__ProductionRouteStatus=NotRouted
ServerFirstReadiness__OfflineMode=ConsultationOnly
ServerFirstReadiness__AccountStatus=NotLive
ServerFirstReadiness__SessionStatus=NotLive
ServerFirstReadiness__ColonyReadModelStatus=PreparationOnly
ServerIdentity__GameServerId=<stable-guid-for-this-server>
ServerIdentity__DefaultWorldId=<stable-guid-for-default-world>
ServerIdentity__ShardName=production-preparation
AccountSessionReadiness__ProductionTarget=104.129.128.136
AccountSessionReadiness__AccountStatus=NotLive
AccountSessionReadiness__SessionStatus=NotLive
AccountSessionReadiness__CredentialStatus=PreparationOnly
AccountSessionReadiness__ColonyReadModelStatus=PreparationOnly
AccountSessionReadiness__AccountCreationAllowed=false
AccountSessionReadiness__SessionCreationAllowed=false
AccountSessionReadiness__TokenIssuanceAllowed=false
AccountSessionReadiness__OfficialPersistenceClaimAllowed=false
```

Do not set `ProductionRouteProven=true` until Bee Kingdom Server is actually routed on production and `/runtime/handshake` returns a Bee Kingdom response over HTTPS.

## Account And Session Public Readiness

`GET /runtime/account-session-readiness` is a public, non-secret status contract for account/session preparation.

It returns:

- game server id;
- default world id;
- shard name;
- production target;
- selected persistence provider;
- whether SQL runtime and migration connection names are configured;
- whether account, credential and session stores are wired in the server;
- explicit account/session/credential/read model statuses;
- explicit false live claims;
- blockers that prevent account/session production activation.

It does not create an account, does not authenticate a player, does not create a session, does not issue access or refresh tokens, does not expose credentials, does not expose SQL connection strings and does not mutate gameplay state.

Example:

```powershell
Invoke-RestMethod `
  -Uri https://<host>/runtime/account-session-readiness
```

Do not present this endpoint as login readiness, saved progression, official persistence, server account activation or synchronized Unity gameplay until a dedicated SERVER authorizes those claims and production route evidence exists.

## World Identity Public Readiness

`GET /runtime/world-identity-readiness` is a public, non-secret status contract for `GameServerId` / `WorldId` consistency.

It returns:

- game server id;
- default world id;
- shard name;
- GUID validity flags;
- whether the two identifiers are distinct;
- required future scopes for accounts, colonies, world map, alliances, chat and rankings;
- blockers that prevent live world identity claims.

It does not assign an account to a world, does not select a live server, does not expose player identifiers and does not unlock official progression.

Example:

```powershell
Invoke-RestMethod `
  -Uri https://<host>/runtime/world-identity-readiness
```

Do not present this endpoint as live world selection, account-world assignment, saved progression, ranking, matchmaking or synchronized Unity gameplay.

## World Registry Public Readiness

`GET /runtime/world-registry-readiness` is a public, non-secret status contract for future multi-world growth.

It returns:

- game server id;
- default world id;
- shard name;
- production target;
- registry status;
- one default world entry in preparation;
- explicit false flags for live world selection, creation, transfer, merge and population metrics;
- blockers that prevent live world-registry claims.

Default production-safe configuration:

```powershell
WorldRegistryReadiness__ProductionTarget=104.129.128.136
WorldRegistryReadiness__RegistryStatus=PreparationOnly
WorldRegistryReadiness__DefaultWorldDisplayName="Bee Kingdom 1"
WorldRegistryReadiness__DefaultWorldStatus=PreparationOnly
WorldRegistryReadiness__DefaultWorldRegion=Unassigned
WorldRegistryReadiness__DefaultWorldLocale=und
WorldRegistryReadiness__ProductionRouteProven=false
WorldRegistryReadiness__WorldSelectionEnabled=false
WorldRegistryReadiness__WorldCreationEnabled=false
WorldRegistryReadiness__WorldTransferEnabled=false
WorldRegistryReadiness__WorldMergeEnabled=false
WorldRegistryReadiness__LivePopulationEnabled=false
```

It does not assign a player to a world, does not list live capacity, does not expose population, does not create worlds, does not transfer accounts and does not merge worlds.

Example:

```powershell
Invoke-RestMethod `
  -Uri https://<host>/runtime/world-registry-readiness
```

Do not present this endpoint as live world selection, server list availability, player join readiness, population count, ranking, matchmaking, official progression or cross-server gameplay.

## World Map Public Readiness

`GET /runtime/world-map-readiness` is a public, non-secret status contract for the world-map foundation.

It returns:

- game server id;
- default world id;
- shard name;
- production target;
- world-map status and boundary;
- read-only draft node models;
- explicit false live flags for map gameplay, territory, alliance, scouting, war, economy, synchronization and progression;
- blockers that prevent any live world-map claim.

Default production-safe configuration:

```powershell
WorldMapReadiness__ProductionTarget=104.129.128.136
WorldMapReadiness__WorldMapStatus=PreparationOnly
WorldMapReadiness__WorldMapBoundary=ReadOnlyNonLiveFoundation
WorldMapReadiness__ProductionRouteProven=false
WorldMapReadiness__MapGameplayEnabled=false
WorldMapReadiness__LiveTerritoryEnabled=false
WorldMapReadiness__LiveAllianceEnabled=false
WorldMapReadiness__LiveScoutingEnabled=false
WorldMapReadiness__LiveWarEnabled=false
WorldMapReadiness__LiveEconomyEnabled=false
WorldMapReadiness__RealTimeSynchronizationEnabled=false
WorldMapReadiness__OfficialProgressionEnabled=false
```

It does not return live map nodes, player positions, territory ownership, alliance membership, scouting reports, resource amounts, war state, ranking or matchmaking data.

Example:

```powershell
Invoke-RestMethod `
  -Uri https://<host>/runtime/world-map-readiness
```

Do not present this endpoint as a live world map, territory system, alliance map, scouting feed, route execution, PvP surface, economy surface, ranking, matchmaking or synchronized Unity gameplay.

## Internal Support Subdomain

`internal-support.beekingdomgame.com` (Cloudflare DNS record created 2026-08-26) is a second
hostname binding on the same already-deployed BeeKingdom.Server IIS site that serves
`api-ops.beekingdomgame.com`. It is not a separate process or deployment - it exposes
`/admin/ui` (see `AdminUiPage.cs`) under a friendlier internal hostname, gated by the same
`AdminSupport` shared-secret described in the Admin Operations Security section, using a
different key from `Ops__AdminKey`.

Setup (run on the Windows Server 2025 box):

1. In Cloudflare, set SSL/TLS mode to "Full (strict)" for the zone.
2. In Cloudflare, generate an Origin Certificate for `internal-support.beekingdomgame.com`
   (SSL/TLS > Origin Server > Create Certificate), download the certificate and private key,
   and combine them into a `.pfx`:
   ```powershell
   openssl pkcs12 -export -out internal-support-origin.pfx -inkey origin.key -in origin.pem
   ```
3. Run:
   ```powershell
   .\Server\deploy\Add-InternalSupportSubdomain.ps1 -CertPfxPath C:\certs\internal-support-origin.pfx
   ```
   This finds the existing IIS site, imports the origin certificate, adds an HTTPS SNI
   binding for the new hostname, generates a random `AdminSupport` key, stores only its
   SHA-256 in the site's `web.config` (`AdminSupport__KeySha256`), and recycles the app pool.
   The plaintext key is printed once to the console - store it in a password manager, it is
   never written to disk or logged.

Durability: this relies entirely on the existing IIS hosting for `api-ops.beekingdomgame.com`
(W3SVC auto-starts on server reboot, ASP.NET Core Module auto-restarts the worker process on
crash). No separate Windows Service or scheduled task is introduced.

Access: `https://internal-support.beekingdomgame.com/admin/ui`, header
`X-BeeKingdom-Support-Key: <key>`.

Do not reuse the `Ops__AdminKey`/`Ops__MigrationApplyKey` value for `AdminSupport__Key`; keep
these secrets distinct so they can be rotated independently (see `AdminSupportOptions.cs`).

## Rollback Strategy

Package rollback:

```powershell
.\Rollback-BeeKingdomServer.ps1 -BackupPath <previous-package-path> -TargetPath <current-iis-site-path>
```

SQL rollback:

- Use `DatabaseRollbackCatalog` scripts in reverse dependency order.
- Follow `RUNBOOK-SQL-ROLLBACK-OPERATIONS.md`.
- Take a SQL backup before running any rollback script.
- Apply rollback only during a controlled maintenance window.
- Record rollback script names and timestamps in the operational incident log.
- Re-run `dotnet BeeKingdom.Tools.dll diagnostics` after rollback to confirm expected migration state.

Rollback scripts currently remove the initial SERVER-018/SERVER-019 tables:

- `050_rollback_colony_snapshots.sql`
- `040_rollback_colonies.sql`
- `030_rollback_authentication_sessions.sql`
- `020_rollback_accounts.sql`

## Operational Limits

- SQL integration is runtime-effective but still minimal.
- `/ops` uses a shared admin key; replace with a stronger admin auth policy when governance requires it.
- Real SQL integration tests are opt-in through `BEE_SQL_INTEGRATION_CONNECTION_STRING`.
- No live economy, combat, alliance, matchmaking, ranking or monetization feature is activated by this runbook.
- Unity remains outside this scope.
