# Backup and restore runbook for a future private staging wave

Status: operator procedure only. Do not execute against production. Do not use
until the staging database, maintenance window, identities and backup location
have been explicitly approved.

All values below are placeholders. Never paste a real secret into this file or
the command history captured as evidence.

## Preconditions

- Private staging remains non-public and is not an official persistence claim.
- Runtime writes are stopped or drained for the backup/restore validation window.
- `<STAGING_SQL_INSTANCE>`, `<STAGING_DB>` and `<BACKUP_PATH>` are approved.
- Runtime, migration and backup identities are separate and supplied externally.
- Free space covers the source database, backup and side-by-side restore.
- The previous known-good package, configuration references and database backup are retained.

## Backup

Connect with the approved backup operator, not the application runtime identity:

```sql
BACKUP DATABASE [<STAGING_DB>]
TO DISK = N'<BACKUP_PATH>\<STAGING_DB>-<UTC_TIMESTAMP>.bak'
WITH COPY_ONLY, INIT, CHECKSUM, STATS = 10;
```

Verify the media immediately:

```sql
RESTORE VERIFYONLY
FROM DISK = N'<BACKUP_PATH>\<STAGING_DB>-<UTC_TIMESTAMP>.bak'
WITH CHECKSUM;
```

Record only redacted evidence: UTC time, database name alias, backup size,
checksum/verify result and operator/change reference. Do not record connection
strings, tokens, passwords or filesystem credentials.

## Side-by-side restore drill

Inspect logical files first:

```sql
RESTORE FILELISTONLY
FROM DISK = N'<BACKUP_PATH>\<STAGING_DB>-<UTC_TIMESTAMP>.bak';
```

Restore to a disposable validation database and distinct files. Never overwrite
the active staging database during a drill:

```sql
RESTORE DATABASE [<STAGING_DB>_RestoreValidation_<UTC_TIMESTAMP>]
FROM DISK = N'<BACKUP_PATH>\<STAGING_DB>-<UTC_TIMESTAMP>.bak'
WITH
    MOVE N'<LOGICAL_DATA_NAME>' TO N'<VALIDATION_DATA_PATH>.mdf',
    MOVE N'<LOGICAL_LOG_NAME>' TO N'<VALIDATION_LOG_PATH>.ldf',
    RECOVERY,
    STATS = 10;
```

Validate with the migration/read-only validation identity:

```sql
DBCC CHECKDB ([<STAGING_DB>_RestoreValidation_<UTC_TIMESTAMP>]) WITH NO_INFOMSGS;

SELECT ScriptName, AppliedAtUtc
FROM [<STAGING_DB>_RestoreValidation_<UTC_TIMESTAMP>].dbo.SchemaVersion
ORDER BY Id;
```

Then run approved synthetic read checks for Accounts, AuthenticationSessions,
Colonies and ColonySnapshots. Do not copy or inspect real player rows as
readiness evidence.

After evidence is accepted, disconnect validation clients and drop only the
explicitly named restore-validation database. Retain or purge the `.bak` file
according to the approved staging retention policy.

## Failed migration rollback

1. Keep staging writes stopped and preserve logs/evidence.
2. Do not run rollback DDL automatically against the failed database.
3. Restore the last verified backup to a new side-by-side database name.
4. Run `DBCC CHECKDB`, schema-version checks and synthetic application reads.
5. Change the external staging connection reference only after approval.
6. Start the previous known-good package in private loopback mode.
7. Verify health, readiness non-claims and synthetic persistence behavior.
8. Preserve the failed database read-only until reconciliation is approved.

## Go/no-go evidence

- Backup completed with checksum.
- `RESTORE VERIFYONLY` succeeded.
- Side-by-side restore completed.
- `DBCC CHECKDB` returned no errors.
- SchemaVersion is unique and complete.
- Synthetic repository reads succeeded.
- Runtime and migration connection references remained separate.
- Rollback target and owner were recorded.
- No real secret or player data entered the report.

Failure of any item is a staging no-go. This runbook does not authorize changing
`appsettings.Production.json`, deploying SERVER-056 scripts, opening a network
port or declaring persistence live.
