using BeeKingdom.Database;

namespace BeeKingdom.Tests;

public sealed class DatabaseMigrationTests
{
    [Test]
    public void DatabaseBootstrapIsNotATransactionalMigration()
    {
        string[] bootstrapNames = DatabaseCatalog.BootstrapScripts.Select(script => script.Name).ToArray();
        string[] migrationNames = DatabaseCatalog.Migrations.Select(script => script.Name).ToArray();
        string migrationSql = string.Join(Environment.NewLine, DatabaseCatalog.Migrations.Select(script => script.Sql));

        Assert.Multiple(() =>
        {
            Assert.That(bootstrapNames, Is.EqualTo(new[] { "001_create_database.sql" }));
            Assert.That(migrationNames, Does.Not.Contain("001_create_database.sql"));
            Assert.That(migrationSql, Does.Not.Contain("CREATE DATABASE"));
        });
    }

    [Test]
    public void CatalogContainsAccountSessionAndColonyMigrations()
    {
        string[] names = DatabaseCatalog.Migrations.Select(script => script.Name).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(names, Does.Contain("020_accounts.sql"));
            Assert.That(names, Does.Contain("030_authentication_sessions.sql"));
            Assert.That(names, Does.Contain("040_colonies.sql"));
            Assert.That(names, Does.Contain("050_colony_snapshots.sql"));
            Assert.That(names, Does.Contain("060_chat_messaging.sql"));
        });
    }

    [Test]
    public void SchemaVersionScriptsEnforceUniqueMigrationNames()
    {
        string schemaSql = DatabaseCatalog.Migrations.Single(script => script.Name == "010_schema_version.sql").Sql;
        string hardeningSql = DatabaseCatalog.Migrations.Single(script => script.Name == "011_schema_version_uniqueness.sql").Sql;

        Assert.Multiple(() =>
        {
            Assert.That(schemaSql, Does.Contain("UQ_SchemaVersion_ScriptName UNIQUE"));
            Assert.That(hardeningSql, Does.Contain("HAVING COUNT_BIG(*) > 1"));
            Assert.That(hardeningSql, Does.Contain("CREATE UNIQUE INDEX UX_SchemaVersion_ScriptName"));
        });
    }

    [Test]
    public void CatalogSqlMatchesCheckedInScriptFiles()
    {
        DatabaseScript[] scripts = DatabaseCatalog.BootstrapScripts.Concat(DatabaseCatalog.Migrations).ToArray();

        Assert.Multiple(() =>
        {
            foreach (DatabaseScript script in scripts)
            {
                string path = FindRepositoryFile("Server", "src", "BeeKingdom.Database", "Scripts", script.Name);
                Assert.That(NormalizeSql(File.ReadAllText(path)), Is.EqualTo(NormalizeSql(script.Sql)), script.Name);
            }
        });
    }

    [Test]
    public void MigrationsCreateExpectedTables()
    {
        string sql = string.Join(Environment.NewLine, DatabaseCatalog.Migrations.Select(script => script.Sql));

        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Contain("dbo.Accounts"));
            Assert.That(sql, Does.Contain("dbo.AuthenticationAccounts"));
            Assert.That(sql, Does.Contain("dbo.AuthenticationSessions"));
            Assert.That(sql, Does.Contain("dbo.Colonies"));
            Assert.That(sql, Does.Contain("dbo.ColonySnapshots"));
            Assert.That(sql, Does.Contain("dbo.ChatConversations"));
            Assert.That(sql, Does.Contain("dbo.ChatMessages"));
            Assert.That(sql, Does.Contain("dbo.ChatInbox"));
        });
    }

    [Test]
    public void RollbackCatalogDropsTablesInReverseDependencyOrder()
    {
        string[] names = DatabaseRollbackCatalog.Rollbacks.Select(script => script.Name).ToArray();
        string sql = string.Join(Environment.NewLine, DatabaseRollbackCatalog.Rollbacks.Select(script => script.Sql));

        Assert.Multiple(() =>
        {
            Assert.That(names, Is.EqualTo(new[]
            {
                "070_rollback_hive_operations.sql",
                "064_rollback_chat_contract_bounds.sql",
                "063_rollback_chat_moderation_idempotency.sql",
                "062_rollback_chat_creation_idempotency.sql",
                "061_rollback_chat_translations.sql",
                "060_rollback_chat_messaging.sql",
                "050_rollback_colony_snapshots.sql",
                "040_rollback_colonies.sql",
                "030_rollback_authentication_sessions.sql",
                "020_rollback_accounts.sql"
            }));
            Assert.That(sql, Does.Contain("DROP TABLE dbo.ChatMessages"));
            Assert.That(sql, Does.Contain("DROP TABLE dbo.ChatConversations"));
            Assert.That(sql, Does.Contain("DROP TABLE dbo.ColonySnapshots"));
            Assert.That(sql, Does.Contain("DROP TABLE dbo.AuthenticationSessions"));
            Assert.That(sql, Does.Contain("DROP TABLE dbo.AuthenticationAccounts"));
            Assert.That(sql, Does.Contain("DROP TABLE dbo.Accounts"));
        });
    }

    [Test]
    public void HiveLoopDryRunPlanIsLocalOptInAndNotRegisteredAsProductionMigration()
    {
        string dryRunPath = FindRepositoryFile("Server", "src", "BeeKingdom.Database", "DryRuns", "SERVER-039_hive_loop_local_opt_in_dry_run.sql");
        string sql = File.ReadAllText(dryRunPath);
        string[] migrationNames = DatabaseCatalog.Migrations.Select(script => script.Name).ToArray();
        string migrationSql = string.Join(Environment.NewLine, DatabaseCatalog.Migrations.Select(script => script.Sql));

        Assert.Multiple(() =>
        {
            Assert.That(migrationNames, Does.Not.Contain("SERVER-039_hive_loop_local_opt_in_dry_run.sql"));
            Assert.That(migrationSql, Does.Not.Contain("dbo.player_resources"));
            Assert.That(sql, Does.Contain("LOCAL SQL OPT-IN DRY RUN ONLY"));
            Assert.That(sql, Does.Contain("DB_NAME() NOT LIKE N'BeeKingdom[_]Local%'"));
            Assert.That(sql, Does.Contain("BEGIN TRANSACTION SERVER_039_HIVE_LOOP_DRY_RUN"));
            Assert.That(sql, Does.Contain("ROLLBACK TRANSACTION SERVER_039_HIVE_LOOP_DRY_RUN"));
            Assert.That(sql, Does.Contain("Do not run against 104.129.128.136"));
        });
    }

    [Test]
    public void HiveLoopDryRunPlanMapsFutureTablesIndexesAndConstraints()
    {
        string dryRunPath = FindRepositoryFile("Server", "src", "BeeKingdom.Database", "DryRuns", "SERVER-039_hive_loop_local_opt_in_dry_run.sql");
        string sql = File.ReadAllText(dryRunPath);

        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Contain("dbo.player_resources"));
            Assert.That(sql, Does.Contain("dbo.hive_buildings"));
            Assert.That(sql, Does.Contain("dbo.construction_queue"));
            Assert.That(sql, Does.Contain("dbo.troop_counts"));
            Assert.That(sql, Does.Contain("dbo.training_queue"));
            Assert.That(sql, Does.Contain("dbo.idempotency_records"));
            Assert.That(sql, Does.Contain("PlayerId uniqueidentifier NOT NULL"));
            Assert.That(sql, Does.Contain("WorldId uniqueidentifier NOT NULL"));
            Assert.That(sql, Does.Contain("GameServerId uniqueidentifier NOT NULL"));
            Assert.That(sql, Does.Contain("ExpectedResourceRevision bigint NOT NULL"));
            Assert.That(sql, Does.Contain("ExpectedBuildingRevision bigint NOT NULL"));
            Assert.That(sql, Does.Contain("ExpectedArmyRevision bigint NOT NULL"));
            Assert.That(sql, Does.Contain("PK_idempotency_records PRIMARY KEY (PlayerId, WorldId, GameServerId, IdempotencyKeyHash)"));
            Assert.That(sql, Does.Contain("IX_construction_queue_scope_status_time"));
            Assert.That(sql, Does.Contain("IX_training_queue_scope_status_time"));
            Assert.That(sql, Does.Contain("IX_idempotency_records_expiration"));
        });
    }

    [Test]
    public void WorldSchemaReadinessDraftIsLocalRollbackOnlyAndNotRegistered()
    {
        string path = FindRepositoryFile("Server", "ops", "sql-readiness", "world-schema-readiness-dry-run.sql");
        string sql = File.ReadAllText(path);
        string migrationSql = string.Join(Environment.NewLine, DatabaseCatalog.Migrations.Select(script => script.Sql));

        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Contain("DB_NAME() NOT LIKE N'BeeKingdom[_]Local[_]SERVERB057[_]%'"));
            Assert.That(sql, Does.Contain("BEGIN TRANSACTION SERVERB057_WORLD_DRYRUN"));
            Assert.That(sql, Does.Contain("ROLLBACK TRANSACTION SERVERB057_WORLD_DRYRUN"));
            Assert.That(sql, Does.Contain("dbo.world_chunks"));
            Assert.That(sql, Does.Contain("dbo.world_hive_nodes"));
            Assert.That(sql, Does.Contain("dbo.world_resource_nodes"));
            Assert.That(sql, Does.Contain("dbo.world_flights"));
            Assert.That(sql, Does.Contain("WorldId uniqueidentifier NOT NULL"));
            Assert.That(sql, Does.Contain("GameServerId uniqueidentifier NOT NULL"));
            Assert.That(migrationSql, Does.Not.Contain("dbo.world_chunks"));
            Assert.That(DatabaseCatalog.Migrations.Select(script => script.Name), Does.Not.Contain("world-schema-readiness-dry-run.sql"));
        });
    }

    [Test]
    public void ProductionConfigurationRemainsInMemoryAndContainsNoSqlConnectionValue()
    {
        string path = FindRepositoryFile("Server", "src", "BeeKingdom.Server", "appsettings.Production.json");
        using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
        System.Text.Json.JsonElement root = document.RootElement;
        System.Text.Json.JsonElement sqlServer = root.GetProperty("SqlServer");

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("Persistence").GetProperty("Provider").GetString(), Is.EqualTo("InMemory"));
            Assert.That(sqlServer.TryGetProperty("ConnectionString", out _), Is.False);
            Assert.That(sqlServer.TryGetProperty("RuntimeConnectionString", out _), Is.False);
            Assert.That(sqlServer.TryGetProperty("MigrationConnectionString", out _), Is.False);
        });
    }

    [Test]
    public void HiveLoopRepositoryContractsDoNotRegisterProductionMigration()
    {
        string[] migrationNames = DatabaseCatalog.Migrations.Select(script => script.Name).ToArray();
        string migrationSql = string.Join(Environment.NewLine, DatabaseCatalog.Migrations.Select(script => script.Sql));

        Assert.Multiple(() =>
        {
            Assert.That(migrationNames, Does.Not.Contain("SERVER-040_hive_loop_repository_contracts.sql"));
            Assert.That(migrationSql, Does.Not.Contain("IHiveLoopReadinessRepository"));
            Assert.That(migrationSql, Does.Not.Contain("TryReserveUpgrade"));
            Assert.That(migrationSql, Does.Not.Contain("TryReserveTraining"));
            Assert.That(migrationSql, Does.Not.Contain("CompleteDueQueues"));
            Assert.That(migrationSql, Does.Not.Contain("RecordIdempotencyResult"));
        });
    }

    [Test]
    public void HiveActionLoopDevOnlyBridgeDoesNotRegisterProductionMigration()
    {
        string[] migrationNames = DatabaseCatalog.Migrations.Select(script => script.Name).ToArray();
        string migrationSql = string.Join(Environment.NewLine, DatabaseCatalog.Migrations.Select(script => script.Sql));

        Assert.Multiple(() =>
        {
            Assert.That(migrationNames, Does.Not.Contain("SERVER-042_hive_action_loop_dev_only_bridge.sql"));
            Assert.That(migrationNames, Does.Not.Contain("SERVER-043_hive_action_loop_dev_only_contracts_snapshot_prep.sql"));
            Assert.That(migrationNames, Does.Not.Contain("SERVER-044_future_official_persistence_idempotency_reconciliation.sql"));
            Assert.That(migrationNames, Does.Not.Contain("SERVER-045_hive_non_claim_idempotency_snapshot_evidence_prep.sql"));
            Assert.That(migrationNames, Does.Not.Contain("SERVER-046_hive_app_readiness_non_claim_evidence_carry_forward.sql"));
            Assert.That(migrationNames, Does.Not.Contain("SERVER-047_bee958_hive_product_non_claim_support.sql"));
            Assert.That(migrationNames, Does.Not.Contain("SERVER-048_bee975_official_server_claim_boundary.sql"));
            Assert.That(migrationNames, Does.Not.Contain("SERVER-049_bee997_server_live_claim_visual_guard.sql"));
            Assert.That(migrationNames, Does.Not.Contain("SERVER-052_official_auth_foundation.sql"));
            Assert.That(migrationSql, Does.Not.Contain("HiveActionLoopDevOnlyBridge"));
            Assert.That(migrationSql, Does.Not.Contain("OfficialSaveFuturePreparation"));
            Assert.That(migrationSql, Does.Not.Contain("HiveResourceTickDevOnlyContract"));
            Assert.That(migrationSql, Does.Not.Contain("HiveActionLoopSnapshotEnvelopeDevOnly"));
            Assert.That(migrationSql, Does.Not.Contain("HiveLocalServerReconciliationDevOnlyContract"));
            Assert.That(migrationSql, Does.Not.Contain("HiveOfficialPersistenceRequirementsInventory"));
            Assert.That(migrationSql, Does.Not.Contain("HiveFutureIdempotencyReplaySafetyPolicy"));
            Assert.That(migrationSql, Does.Not.Contain("HiveSnapshotDeltaAuditDevOnlyContract"));
            Assert.That(migrationSql, Does.Not.Contain("HiveFutureAuthoritativeActionHandlerHandoff"));
            Assert.That(migrationSql, Does.Not.Contain("HiveOfficialPersistenceNonClaimGuard"));
            Assert.That(migrationSql, Does.Not.Contain("HiveIdempotencyReplayEvidenceFieldSet"));
            Assert.That(migrationSql, Does.Not.Contain("HiveSnapshotDeltaReconciliationEvidenceFieldSet"));
            Assert.That(migrationSql, Does.Not.Contain("HiveNonClaimEvidenceCarryForward"));
            Assert.That(migrationSql, Does.Not.Contain("HiveIdempotencySnapshotEvidenceContinuity"));
            Assert.That(migrationSql, Does.Not.Contain("HivePreviewDemoLiveStateMatrix"));
            Assert.That(migrationSql, Does.Not.Contain("HiveServerFutureSupportNonClaimManifest"));
            Assert.That(migrationSql, Does.Not.Contain("HiveServerFutureSupportQaLiveClaimChecklist"));
            Assert.That(migrationSql, Does.Not.Contain("HiveOfficialServerClaimBoundary"));
            Assert.That(migrationSql, Does.Not.Contain("HiveOfficialServerClaimBoundaryQaCriteria"));
            Assert.That(migrationSql, Does.Not.Contain("HiveServerLiveClaimVisualGuard"));
            Assert.That(migrationSql, Does.Not.Contain("HiveServerLiveClaimVisualGuardQaCriteria"));
            Assert.That(migrationSql, Does.Not.Contain("OfficialAuthFoundationDescriptor"));
            Assert.That(migrationSql, Does.Not.Contain("OfficialAuthEndpointPlan"));
        });
    }

    private static string FindRepositoryFile(params string[] segments)
    {
        DirectoryInfo? directory = new(TestContext.CurrentContext.TestDirectory);

        while (directory is not null)
        {
            string candidate = Path.Combine(new[] { directory.FullName }.Concat(segments).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find repository file '{Path.Combine(segments)}'.");
    }

    private static string NormalizeSql(string sql)
    {
        return sql.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
    }
}
