# External secrets checklist

No value from this checklist belongs in source control, an appsettings file, a
package manifest, test output or a readiness report.

## Future staging values

- [ ] `ConnectionStrings__BeeKingdomRuntime` supplied by the staging secret store.
- [ ] `ConnectionStrings__BeeKingdomMigrations` supplied separately.
- [ ] `SqlServer__RuntimeConnectionStringName=BeeKingdomRuntime` supplied as configuration.
- [ ] `SqlServer__MigrationConnectionStringName=BeeKingdomMigrations` supplied as configuration.
- [ ] `Persistence__Provider=SqlServer` set only in the explicitly approved private staging environment.
- [ ] Ops admin key/hash supplied externally.
- [ ] Migration-apply key/hash supplied externally and distinct from the admin key.
- [ ] Backup destination credentials supplied to the backup operator, not the application runtime.
- [ ] Encryption certificate trust and SQL host identity validated outside source control.

## Identity and permission checks

- [ ] Runtime and migration identities are distinct principals.
- [ ] Runtime principal has only required DML/execute permissions.
- [ ] Runtime principal cannot create/alter/drop databases or run backup/restore.
- [ ] Migration principal can apply approved DDL and acquire the migration application lock.
- [ ] Backup operator is separate from the runtime identity.
- [ ] No shared developer credential is used by staging.
- [ ] Connection strings do not embed credentials when Windows service identities can be used.

## Handling and rotation

- [ ] Values are injected after package installation through the approved secret channel.
- [ ] Logs and health/readiness responses expose booleans or names, never connection values.
- [ ] Evidence is redacted before leaving the staging host.
- [ ] Rotation owner, expiry and emergency revoke procedure are recorded externally.
- [ ] Old values are revoked after rotation and rollback validation.
- [ ] Local test variables target only LocalDB and contain no SQL password.

`appsettings.Production.json` must remain `Persistence.Provider=InMemory` until a
future staging change is explicitly approved. This checklist does not authorize
that change.
