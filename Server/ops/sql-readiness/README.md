# SERVER-B-057 SQL readiness

This folder is local-only preparation for a future persistent staging wave.
Nothing here enables `Persistence=SqlServer`, publishes a server, or contacts a
remote SQL instance.

## Automated local proof

Requirements:

- .NET 8 SDK;
- SQL Server LocalDB;
- `sqllocaldb` and `sqlcmd` on `PATH`;
- Windows Integrated Security.

Run from the repository root:

```powershell
.\Server\ops\sql-readiness\Invoke-LocalSqlReadiness.ps1 -NoRestore
```

The runner accepts only a `(localdb)\...` instance. Each SQL test creates a
unique `BeeKingdom_Local_SERVERB057_*` database and drops it in teardown. The
suite covers bootstrap, migration replay, synthetic account/session/colony
records, world scoping, minimal concurrency, the world-schema draft, and a
backup/verify/side-by-side restore drill.

Without the environment variable set by this runner, SQL integration tests are
ignored by design. Static migration and configuration tests still run in the
normal .NET suite.

## Files

- `world-schema-readiness-dry-run.sql`: unregistered transactional DDL draft;
- `Schema-Inventory-And-Gaps.md`: current schema and deferred MMO tables;
- `Backup-Restore-Staging-Runbook.md`: future staging operator procedure;
- `External-Secrets-Checklist.md`: names and handling rules, without values.

These artifacts are readiness evidence only. They are not production migration
or deployment tooling.
