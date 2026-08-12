# Bee Kingdom SQL Rollback Operations Runbook

Date: 2026-07-10
Scope: Bee Kingdom Server SQL rollback operations.

## Objective

Provide an operational rollback procedure for Bee Kingdom SQL runtime tables introduced by SERVER-018 and SERVER-019.

This runbook does not claim Hive View persistence, synchronization, official economy, construction, reward, progression or gameplay readiness.

## Safety Rules

1. Rollback must be approved during a controlled maintenance window.
2. Take a verified SQL backup before any rollback command.
3. Stop write traffic to Bee Kingdom Server before destructive rollback.
4. Use a migration identity, not the runtime identity.
5. Record every rollback script name, operator, timestamp and backup path.
6. Re-run diagnostics after rollback.

## Identity Separation

Runtime identity:

- reads/writes runtime tables only;
- configured through `SqlServer:RuntimeConnectionStringName` or `ConnectionStrings:BeeKingdomRuntime`;
- should not create/drop tables.

Migration identity:

- applies migrations and rollback scripts;
- configured through `SqlServer:MigrationConnectionStringName` or `ConnectionStrings:BeeKingdomMigrations`;
- may create/drop tables only during controlled operations.

Recommended environment variables:

```powershell
Persistence__Provider=SqlServer
SqlServer__RuntimeConnectionStringName=BeeKingdomRuntime
SqlServer__MigrationConnectionStringName=BeeKingdomMigrations
ConnectionStrings__BeeKingdomRuntime="Server=<sql>;Database=BeeKingdom;User Id=bee_runtime;Password=<secret>;Encrypt=True;TrustServerCertificate=False;"
ConnectionStrings__BeeKingdomMigrations="Server=<sql>;Database=BeeKingdom;User Id=bee_migration;Password=<secret>;Encrypt=True;TrustServerCertificate=False;"
```

## Admin Operations Keys

`POST /ops/migrations/apply` requires both keys when enabled:

```powershell
X-BeeKingdom-Admin-Key: <ops-admin-key>
X-BeeKingdom-Migration-Key: <migration-apply-key>
```

Production defaults:

```powershell
Ops__RequireAdminKey=true
Ops__RequireMigrationApplyKey=true
```

Prefer hashed key storage when possible:

```powershell
Ops__AdminKeySha256=<sha256-of-admin-key>
Ops__MigrationApplyKeySha256=<sha256-of-migration-key>
```

The actual admin and migration secrets are still sent in headers by the operator, but are not stored in plaintext server configuration.

## Rollback Order

Use the rollback catalog in reverse dependency order:

1. `050_rollback_colony_snapshots.sql`
2. `040_rollback_colonies.sql`
3. `030_rollback_authentication_sessions.sql`
4. `020_rollback_accounts.sql`

## Manual SQL Rollback Procedure

1. Confirm backup exists and restore test has been verified.
2. Stop public traffic to Bee Kingdom Server.
3. Call `GET /ops/sql-production-dry-run` and confirm it reports backup evidence, maintenance window and rollback acknowledgement.
4. Confirm `/ops/monitoring` migration failures and pending checks are captured.
5. Call `GET /ops/migrations/rollback-plan` and confirm `executableByEndpoint=false`.
6. Connect with the migration identity.
7. Execute rollback scripts in the documented order.
8. Record each script execution.
9. Restart Bee Kingdom Server.
10. Call `/health`.
11. Call `/ops/monitoring`.
12. Decide whether to keep service offline, reapply migrations, or restore backup.

## Post-Rollback Verification

Minimum checks:

- `GET /health`
- `GET /ops/readiness`
- `GET /ops/sql-production-dry-run`
- `GET /ops/monitoring`
- `GET /ops/migrations/rollback-plan`
- `GET /ops/migrations/pending`
- SQL table existence check for expected rollback state
- log review for SQL exceptions

## Recovery Paths

If rollback is intentional:

- keep rollback state;
- document the active migration baseline;
- do not claim production readiness until a new validation cycle passes.

If rollback was accidental or partial:

- restore SQL backup; or
- reapply migrations with the migration identity;
- repeat smoke tests.

## Operational Limits

- Rollback scripts are destructive.
- Rollback is not a gameplay feature.
- Rollback does not validate official Hive View persistence or synchronization.
- Rollback must not be used as a substitute for migration dry-run validation.
