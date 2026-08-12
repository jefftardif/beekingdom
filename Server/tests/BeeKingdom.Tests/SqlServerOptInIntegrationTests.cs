using BeeKingdom.Accounts.DependencyInjection;
using BeeKingdom.Accounts.Models;
using BeeKingdom.Accounts.Repositories;
using BeeKingdom.Authentication.DependencyInjection;
using BeeKingdom.Authentication.Models;
using BeeKingdom.Authentication.Providers;
using BeeKingdom.Authentication.Sessions;
using BeeKingdom.Chat;
using BeeKingdom.Chat.DependencyInjection;
using BeeKingdom.Chat.Models;
using BeeKingdom.Chat.Repositories;
using BeeKingdom.Colony.DependencyInjection;
using BeeKingdom.Colony.Models;
using BeeKingdom.Colony.Repositories;
using BeeKingdom.Colony.Snapshots;
using BeeKingdom.Database;
using BeeKingdom.Infrastructure.DependencyInjection;
using BeeKingdom.HiveOperations;
using BeeKingdom.Persistence.DependencyInjection;
using BeeKingdom.Persistence.Migrations;
using BeeKingdom.Shared.ValueObjects;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BeeKingdom.Tests;

[NonParallelizable]
public sealed class SqlServerOptInIntegrationTests
{
    private const string ConnectionStringVariable = "BEE_SQL_INTEGRATION_CONNECTION_STRING";
    private const string DatabasePrefix = "BeeKingdom_Local_SERVERB057_";
    private const string DatabaseCreationLockPrefix = "BeeKingdom.Database.Create:";
    private const string MigrationLockResource = "BeeKingdom.Database.Migrations";

    private readonly List<string> cleanupFiles = new();
    private readonly List<string> cleanupDatabases = new();
    private ServiceProvider? provider;
    private string? runtimeConnectionString;
    private string? migrationConnectionString;
    private string? masterConnectionString;
    private string? databaseName;

    [SetUp]
    public void SetUp()
    {
        string? configured = Environment.GetEnvironmentVariable(ConnectionStringVariable);
        if (string.IsNullOrWhiteSpace(configured))
        {
            Assert.Ignore($"{ConnectionStringVariable} is not set. Disposable LocalDB integration test skipped by design.");
        }

        SqlConnectionStringBuilder baseBuilder = new(configured);
        if (!baseBuilder.DataSource.StartsWith("(localdb)\\", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Fail($"{ConnectionStringVariable} must target a local SQL Server LocalDB instance. Remote targets are refused.");
        }

        if (!baseBuilder.IntegratedSecurity || !string.IsNullOrWhiteSpace(baseBuilder.UserID) || !string.IsNullOrWhiteSpace(baseBuilder.Password))
        {
            Assert.Fail($"{ConnectionStringVariable} must use Integrated Security and must not contain SQL credentials.");
        }

        databaseName = DatabasePrefix + Guid.NewGuid().ToString("N");
        cleanupDatabases.Add(databaseName);

        SqlConnectionStringBuilder targetBuilder = new(baseBuilder.ConnectionString)
        {
            InitialCatalog = databaseName,
            Pooling = true,
            TrustServerCertificate = true,
            ApplicationName = "BeeKingdom.SERVER-B-061.Tests"
        };
        SqlConnectionStringBuilder masterBuilder = new(targetBuilder.ConnectionString)
        {
            InitialCatalog = "master"
        };

        runtimeConnectionString = targetBuilder.ConnectionString;
        migrationConnectionString = targetBuilder.ConnectionString;
        masterConnectionString = masterBuilder.ConnectionString;
        provider = BuildProvider(runtimeConnectionString, migrationConnectionString, databaseName);
    }

    [TearDown]
    public async Task TearDown()
    {
        provider?.Dispose();
        SqlConnection.ClearAllPools();

        if (!string.IsNullOrWhiteSpace(masterConnectionString))
        {
            foreach (string name in cleanupDatabases.AsEnumerable().Reverse())
            {
                await DropDatabaseAsync(masterConnectionString, name);
            }
        }

        foreach (string path in cleanupFiles)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        cleanupDatabases.Clear();
        cleanupFiles.Clear();
    }

    [Test]
    public async Task SqlHiveStateRepositoryRoundTripsOfflineProductionReceiptAndReplay()
    {
        await EnsureMigratedAsync();
        Guid playerId = Guid.NewGuid();
        Guid hiveId = Guid.NewGuid();
        static PlayerHiveState NewState(Guid player, Guid hive) => new(
            player, hive, HiveStateMigrator.CurrentModelVersion, 0,
            new Dictionary<string, ResourceBalance>
            {
                ["honey"] = new(0, 100), ["wax"] = new(0, 100), ["pollen"] = new(0, 100)
            }, new(), new(), new());
        var repository = new SqlHiveStateRepository(runtimeConnectionString!, NewState);
        HiveOfflineProductionOptions options = new()
        {
            Enabled = true, CatalogVersion = "test-v1", MaxRecognizedDuration = TimeSpan.FromHours(2),
            Catalog =
            [
                new("honey_storage", "honey", 10m, 1_000_000_000),
                new("wax_workshop", "wax", 5m, 1_000_000_000),
                new("warehouse_cells", "pollen", 8m, 1_000_000_000)
            ]
        };
        options.Validate();
        var clock = new SqlOfflineProductionTestClock(DateTimeOffset.UtcNow);
        var service = new HiveOfflineProductionService(repository, clock, options);
        await service.ReadSnapshotAsync(playerId, hiveId);
        clock.Advance(TimeSpan.FromHours(1));
        OfflineProductionCollectionResult first = await service.CollectAsync(playerId, hiveId, "honey_storage", new(0, "sql-offline"));
        Assert.That(first.Succeeded, Is.True);
        Assert.That(first.Response, Is.Not.Null);
        var repository2 = new SqlHiveStateRepository(runtimeConnectionString!, NewState);
        var service2 = new HiveOfflineProductionService(repository2, clock, options);
        PlayerHiveState persisted = (await repository2.ReadAsync(playerId, hiveId))!;
        Assert.That(persisted.ModelVersion, Is.EqualTo(HiveStateMigrator.CurrentModelVersion));
        Assert.That(persisted.OfflineProduction!.Revision, Is.EqualTo(1));
        Assert.That(persisted.OfflineProduction.Receipts.ContainsKey("sql-offline"), Is.True);
        Assert.That(persisted.Resources["honey"].Amount, Is.EqualTo(10));
        OfflineProductionCollectionResult replay = await service2.CollectAsync(playerId, hiveId, "honey_storage", new(0, "sql-offline"));
        Assert.That(replay.Code, Is.EqualTo("game.idempotency_replay"));
        Assert.That(replay.Response, Is.EqualTo(first.Response));
        Assert.That((await repository2.ReadAsync(playerId, hiveId))!.Resources["honey"].Amount, Is.EqualTo(10));
    }

    [Test]
    public async Task SqlServerCreatesDisposableDatabaseAndAppliesMigrationsIdempotently()
    {
        Assert.That(await DatabaseExistsAsync(masterConnectionString!, databaseName!), Is.False);

        IMigrationRunner runner = provider!.GetRequiredService<IMigrationRunner>();
        IReadOnlyList<string> pendingBefore = await runner.GetPendingMigrationsAsync();

        Assert.Multiple(() =>
        {
            Assert.That(pendingBefore, Is.EqualTo(DatabaseCatalog.Migrations.Select(script => script.Name)));
            Assert.That(pendingBefore, Does.Not.Contain("001_create_database.sql"));
        });

        await runner.ApplyPendingMigrationsAsync();
        int appliedAfterFirstRun = await ExecuteScalarAsync<int>(migrationConnectionString!, "SELECT COUNT(*) FROM dbo.SchemaVersion;");
        IReadOnlyList<string> pendingAfterFirstRun = await runner.GetPendingMigrationsAsync();

        await runner.ApplyPendingMigrationsAsync();
        int appliedAfterSecondRun = await ExecuteScalarAsync<int>(migrationConnectionString!, "SELECT COUNT(*) FROM dbo.SchemaVersion;");
        bool databaseExistsAfterMigration = await DatabaseExistsAsync(masterConnectionString!, databaseName!);

        Assert.Multiple(() =>
        {
            Assert.That(databaseExistsAfterMigration, Is.True);
            Assert.That(appliedAfterFirstRun, Is.EqualTo(DatabaseCatalog.Migrations.Count));
            Assert.That(appliedAfterSecondRun, Is.EqualTo(appliedAfterFirstRun));
            Assert.That(pendingAfterFirstRun, Is.Empty);
        });
    }

    [Test]
    public async Task SqlServerRepositoryRoundTripsSyntheticAccountProgression()
    {
        await EnsureMigratedAsync();

        IAccountRepository repository = provider!.GetRequiredService<IAccountRepository>();
        (string Name, AccountProgression Progression)[] roundTrips =
        [
            ("empty", CreateProgression()),
            ("single", CreateProgression(
                achievements: ["first_honey"],
                statistics: new Dictionary<string, double> { ["harvests"] = 1 },
                rewards: ["wax_badge"],
                seasons: ["season-one"],
                purchases: ["starter-pack"])),
            ("multiple", CreateProgression(
                achievements: ["royal_jelly", "first_honey", "builder"],
                statistics: new Dictionary<string, double> { ["harvests"] = 12, ["hives"] = 3 },
                rewards: ["gold_wing", "wax_badge", "amber_crown"],
                seasons: ["season-one", "season-two"],
                purchases: ["starter-pack", "builder-pack"]))
        ];

        foreach ((string name, AccountProgression progression) in roundTrips)
        {
            AccountRecord account = CreateAccountRecord(
                $"sql-progression-{name}-{Guid.NewGuid():N}@bee.test",
                "SQL " + name,
                progression);

            repository.Create(account);
            AccountRecord? fetched = repository.Get(account.Profile.AccountId);

            Assert.That(fetched, Is.Not.Null, name);
            AssertProgression(progression, fetched!.Progression, name);
        }

        AccountRecord duplicatePayloadAccount = CreateAccountRecord(
            $"sql-progression-duplicates-{Guid.NewGuid():N}@bee.test",
            "SQL duplicates");
        repository.Create(duplicatePayloadAccount);
        await UpdateProgressionJsonAsync(duplicatePayloadAccount.Profile.AccountId, """
            {
              "globalAchievements": ["beta", "alpha", "alpha", "beta"],
              "globalStatistics": { "harvests": 4 },
              "permanentRewards": ["wax", "amber", "wax"],
              "seasonHistory": ["season-one", "season-one"],
              "purchaseHistory": []
            }
            """);

        AccountRecord duplicateNormalized = repository.Get(duplicatePayloadAccount.Profile.AccountId)!;
        Assert.Multiple(() =>
        {
            Assert.That(duplicateNormalized.Progression.GlobalAchievements, Is.EquivalentTo(new[] { "alpha", "beta" }));
            Assert.That(duplicateNormalized.Progression.GlobalAchievements, Has.Count.EqualTo(2));
            Assert.That(duplicateNormalized.Progression.PermanentRewards, Is.EquivalentTo(new[] { "amber", "wax" }));
            Assert.That(duplicateNormalized.Progression.PermanentRewards, Has.Count.EqualTo(2));
            Assert.That(duplicateNormalized.Progression.SeasonHistory, Is.EqualTo(new[] { "season-one", "season-one" }), "Ordered history is not a set and must retain duplicates.");
        });

        repository.Save(duplicateNormalized);
        string normalizedJson = await GetProgressionJsonAsync(duplicatePayloadAccount.Profile.AccountId);
        using (JsonDocument document = JsonDocument.Parse(normalizedJson))
        {
            string[] achievements = document.RootElement.GetProperty("globalAchievements")
                .EnumerateArray()
                .Select(item => item.GetString()!)
                .ToArray();
            string[] rewards = document.RootElement.GetProperty("permanentRewards")
                .EnumerateArray()
                .Select(item => item.GetString()!)
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(achievements, Is.EqualTo(new[] { "alpha", "beta" }));
                Assert.That(rewards, Is.EqualTo(new[] { "amber", "wax" }));
            });
        }

        AccountRecord legacyPayloadAccount = CreateAccountRecord(
            $"sql-progression-legacy-{Guid.NewGuid():N}@bee.test",
            "SQL legacy");
        repository.Create(legacyPayloadAccount);
        await UpdateProgressionJsonAsync(legacyPayloadAccount.Profile.AccountId, """
            {
              "GlobalAchievements": ["legacy-achievement"],
              "GlobalStatistics": { "legacy-visits": 3 },
              "PermanentRewards": ["legacy-reward"],
              "SeasonHistory": ["legacy-season"]
            }
            """);

        AccountProgression legacyProgression = repository.Get(legacyPayloadAccount.Profile.AccountId)!.Progression;
        Assert.Multiple(() =>
        {
            Assert.That(legacyProgression.GlobalAchievements, Is.EquivalentTo(new[] { "legacy-achievement" }));
            Assert.That(legacyProgression.GlobalStatistics["legacy-visits"], Is.EqualTo(3));
            Assert.That(legacyProgression.PermanentRewards, Is.EquivalentTo(new[] { "legacy-reward" }));
            Assert.That(legacyProgression.SeasonHistory, Is.EqualTo(new[] { "legacy-season" }));
            Assert.That(legacyProgression.PurchaseHistory, Is.Empty, "A missing legacy property maps to an empty collection.");
        });

        AccountRecord malformedPayloadAccount = CreateAccountRecord(
            $"sql-progression-malformed-{Guid.NewGuid():N}@bee.test",
            "SQL malformed");
        repository.Create(malformedPayloadAccount);
        await UpdateProgressionJsonAsync(malformedPayloadAccount.Profile.AccountId, """
            { "globalAchievements": "not-an-array" }
            """);

        InvalidDataException? malformed = Assert.Throws<InvalidDataException>(
            () => repository.Get(malformedPayloadAccount.Profile.AccountId));
        Assert.Multiple(() =>
        {
            Assert.That(malformed!.Message, Is.EqualTo("Account progression payload is malformed."));
            Assert.That(malformed.InnerException, Is.TypeOf<JsonException>());
        });

        await UpdateProgressionJsonAsync(malformedPayloadAccount.Profile.AccountId, """
            { "globalAchievements": null }
            """);
        InvalidDataException? nullField = Assert.Throws<InvalidDataException>(
            () => repository.Get(malformedPayloadAccount.Profile.AccountId));
        Assert.That(
            nullField!.Message,
            Is.EqualTo("Account progression field 'GlobalAchievements' cannot be null."));
    }

    [Test]
    public async Task SqlServerStoresSyntheticCredentialSessionAndWorldScopedColonies()
    {
        await EnsureMigratedAsync();

        ServiceProvider services = provider!;
        IAccountCredentialStore credentials = services.GetRequiredService<IAccountCredentialStore>();
        IAuthenticationSessionStore sessions = services.GetRequiredService<IAuthenticationSessionStore>();
        IColonyRepository colonies = services.GetRequiredService<IColonyRepository>();

        string email = $"sql-auth-{Guid.NewGuid():N}@bee.test";
        AuthenticationAccount account = credentials.CreateEmailAccount(email, "Strong-Test-Password-1!");
        Assert.That(credentials.TryGetByEmail(email, out AuthenticationAccount fetchedAccount), Is.True);

        AuthenticationSession session = new(
            Guid.NewGuid().ToString("N"),
            account.PlayerId,
            account.AccountId,
            AuthenticationProviderKind.EmailPassword,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddHours(1),
            "1.0.0",
            "127.0.0.1",
            "sql-opt-in-device",
            "local",
            false);
        sessions.Save(session);
        sessions.Save(session);

        WorldId firstWorld = WorldId.New();
        WorldId secondWorld = WorldId.New();
        GameServerId gameServer = GameServerId.New();
        ColonyRecord firstColony = CreateColony(account.PlayerId, firstWorld, "SQL Opt In Hive A");
        ColonyRecord secondColony = CreateColony(account.PlayerId, secondWorld, "SQL Opt In Hive B");
        colonies.Create(firstColony);
        colonies.Create(secondColony);

        ColonySnapshot snapshot = new(
            Guid.NewGuid(),
            firstColony.Profile.ColonyId,
            ColonySnapshotKind.Full,
            0,
            1,
            DateTimeOffset.UtcNow,
            "1.0.0",
            new byte[] { 1, 2, 3 },
            new Dictionary<string, string> { ["source"] = "sql-opt-in" });
        colonies.SaveSnapshot(snapshot);

        int firstWorldCount = await CountColoniesAsync(account.PlayerId, firstWorld);
        int secondWorldCount = await CountColoniesAsync(account.PlayerId, secondWorld);
        int sessionCount = await ExecuteScalarAsync<int>(runtimeConnectionString!,
            "SELECT COUNT(*) FROM dbo.AuthenticationSessions WHERE SessionId = @SessionId;",
            new SqlParameter("@SessionId", session.SessionId));

        Assert.Multiple(() =>
        {
            Assert.That(fetchedAccount.Email, Is.EqualTo(email));
            Assert.That(sessionCount, Is.EqualTo(1), "Saving the same session twice must be idempotent.");
            Assert.That(firstWorldCount, Is.EqualTo(1));
            Assert.That(secondWorldCount, Is.EqualTo(1));
            Assert.That(firstWorld, Is.Not.EqualTo(secondWorld));
            Assert.That(gameServer.Value, Is.Not.EqualTo(Guid.Empty));
            Assert.That(colonies.GetLatestSnapshot(firstColony.Profile.ColonyId)?.Metadata["source"], Is.EqualTo("sql-opt-in"));
        });
    }

    [Test]
    public async Task SqlServerChatRepositoryRoundTripsConversationMessageInboxAndIdempotence()
    {
        await EnsureMigratedAsync();

        ChatManager chat = provider!.GetRequiredService<ChatManager>();
        IChatRepository repository = provider!.GetRequiredService<IChatRepository>();
        PlayerId queen = PlayerId.New();
        PlayerId scout = PlayerId.New();
        Guid gameServerId = Guid.NewGuid();
        Guid worldId = Guid.NewGuid();

        CreateChatConversationResult created = chat.CreateConversation(queen, new CreateChatConversationRequest(
            ChatChannelType.Private,
            gameServerId,
            worldId,
            null,
            "SQL private",
            [scout.Value],
            "sql_private_create_001"));

        SendChatMessageRequest sendRequest = new(
            "sql_send_001",
            "SQL hello with emoji",
            [new ChatContentPart("text", "SQL hello with emoji", null, null, null, null, null)],
            [new ChatMentionInput(scout.Value, "Scout")],
            [new ChatEmoji(":bee:", "bee", "bee")],
            null,
            DateTimeOffset.UtcNow);

        SendChatMessageResult first = await chat.SendMessageAsync(queen, created.Conversation.ConversationId, sendRequest);
        SendChatMessageResult duplicate = await chat.SendMessageAsync(queen, created.Conversation.ConversationId, sendRequest);
        ChatMessagePage page = chat.GetMessages(queen, created.Conversation.ConversationId, 0, 10);
        ChatInboxEntry scoutInbox = repository.GetInbox(scout, created.Conversation.ConversationId)!;
        ChatInboxEntry read = chat.MarkRead(scout, created.Conversation.ConversationId, first.ServerSequence);

        Assert.Multiple(() =>
        {
            Assert.That(repository, Is.TypeOf<SqlChatRepository>());
            Assert.That(first.Deduplicated, Is.False);
            Assert.That(duplicate.Deduplicated, Is.True);
            Assert.That(first.ServerSequence, Is.EqualTo(1));
            Assert.That(duplicate.ServerSequence, Is.EqualTo(first.ServerSequence));
            Assert.That(page.Items, Has.Count.EqualTo(1));
            Assert.That(page.Items[0].Body, Is.EqualTo("SQL hello with emoji"));
            Assert.That(page.Items[0].Mentions[0].PlayerId, Is.EqualTo(scout));
            Assert.That(scoutInbox.UnreadCount, Is.EqualTo(1));
            Assert.That(scoutInbox.MentionCount, Is.EqualTo(1));
            Assert.That(read.ReadCursorSequence, Is.EqualTo(first.ServerSequence));
            Assert.That(read.MentionCount, Is.Zero);
        });
    }

    [Test]
    public async Task SqlServerSerializesMigrationsAndRejectsConcurrentDuplicateAccount()
    {
        Assert.Multiple(() =>
        {
            Assert.That(new SqlConnectionStringBuilder(masterConnectionString!).Pooling, Is.True);
            Assert.That(new SqlConnectionStringBuilder(migrationConnectionString!).Pooling, Is.True);
            Assert.That(new SqlConnectionStringBuilder(runtimeConnectionString!).Pooling, Is.True);
        });

        Task[] migrationAttempts = Enumerable.Range(0, 4)
            .Select(_ => ApplyMigrationsWithIndependentProviderAsync())
            .ToArray();
        await Task.WhenAll(migrationAttempts);

        IMigrationRunner runner = provider!.GetRequiredService<IMigrationRunner>();
        string creationResource = DatabaseCreationLockPrefix + databaseName;
        await AssertRunnerWaitsThenAcquiresAndReleasesAsync(
            runner,
            masterConnectionString!,
            creationResource,
            "database creation");
        await AssertRunnerWaitsThenAcquiresAndReleasesAsync(
            runner,
            migrationConnectionString!,
            MigrationLockResource,
            "migration");
        await AssertRepeatedRunnerAcquisitionsLeaveNoSessionLockCountAsync(runner, creationResource);
        await AssertCreationLockReleasedAfterExceptionAsync();
        await AssertCreationLockAcquisitionCancellationDoesNotLeakAsync(runner, creationResource);
        await AssertMigrationLockReleasedAfterExceptionAsync(runner);
        await AssertMigrationLockReleasedAfterCancellationAsync(runner);

        IAccountRepository repository = provider!.GetRequiredService<IAccountRepository>();
        string email = $"sql-race-{Guid.NewGuid():N}@bee.test";
        AccountRecord first = CreateAccountRecord(email, "Race A");
        AccountRecord second = CreateAccountRecord(email, "Race B");
        Task<bool>[] writes =
        [
            Task.Run(() => TryCreateAccount(repository, first)),
            Task.Run(() => TryCreateAccount(repository, second))
        ];
        bool[] results = await Task.WhenAll(writes);

        int duplicateMigrationRows = await ExecuteScalarAsync<int>(migrationConnectionString!,
            "SELECT COUNT(*) FROM (SELECT ScriptName FROM dbo.SchemaVersion GROUP BY ScriptName HAVING COUNT_BIG(*) > 1) AS duplicates;");
        int accountCount = await ExecuteScalarAsync<int>(runtimeConnectionString!,
            "SELECT COUNT(*) FROM dbo.Accounts WHERE Email = @Email;",
            new SqlParameter("@Email", email));

        Assert.Multiple(() =>
        {
            Assert.That(results.Count(result => result), Is.EqualTo(1));
            Assert.That(accountCount, Is.EqualTo(1));
            Assert.That(duplicateMigrationRows, Is.Zero);
        });
    }

    [Test]
    public async Task WorldSchemaReadinessDraftExecutesAndRollsBackLocally()
    {
        await EnsureMigratedAsync();

        string path = FindRepositoryFile("Server", "ops", "sql-readiness", "world-schema-readiness-dry-run.sql");
        string sql = await File.ReadAllTextAsync(path);
        await ExecuteNonQueryAsync(migrationConnectionString!, sql);

        int remainingDraftTables = await ExecuteScalarAsync<int>(migrationConnectionString!, """
            SELECT COUNT(*)
            FROM sys.tables
            WHERE name IN (N'world_chunks', N'world_hive_nodes', N'world_resource_nodes', N'world_flights');
            """);

        Assert.That(remainingDraftTables, Is.Zero, "The world schema readiness draft must roll back all draft objects.");
    }

    [Test]
    public async Task SqlServerBackupCanBeVerifiedAndRestoredToDisposableDatabase()
    {
        await EnsureMigratedAsync();

        IAccountRepository repository = provider!.GetRequiredService<IAccountRepository>();
        string markerEmail = $"sql-backup-{Guid.NewGuid():N}@bee.test";
        repository.Create(CreateAccountRecord(markerEmail, "Backup Marker"));

        string backupPath = Path.Combine(Path.GetTempPath(), databaseName + ".bak");
        cleanupFiles.Add(backupPath);
        await BackupDatabaseAsync(backupPath);
        await VerifyBackupAsync(backupPath);

        string restoreDatabase = databaseName + "_Restore";
        cleanupDatabases.Add(restoreDatabase);
        IReadOnlyList<BackupFile> files = await ReadBackupFilesAsync(backupPath);
        await RestoreDatabaseAsync(backupPath, restoreDatabase, files);

        SqlConnectionStringBuilder restoredBuilder = new(runtimeConnectionString!)
        {
            InitialCatalog = restoreDatabase,
            Pooling = true
        };
        int restoredMarkerCount = await ExecuteScalarAsync<int>(restoredBuilder.ConnectionString,
            "SELECT COUNT(*) FROM dbo.Accounts WHERE Email = @Email;",
            new SqlParameter("@Email", markerEmail));
        int restoredMigrationCount = await ExecuteScalarAsync<int>(restoredBuilder.ConnectionString,
            "SELECT COUNT(*) FROM dbo.SchemaVersion;");

        Assert.Multiple(() =>
        {
            Assert.That(restoredMarkerCount, Is.EqualTo(1));
            Assert.That(restoredMigrationCount, Is.EqualTo(DatabaseCatalog.Migrations.Count));
        });
    }

    private async Task EnsureMigratedAsync()
    {
        await provider!.GetRequiredService<IMigrationRunner>().ApplyPendingMigrationsAsync();
    }

    private async Task ApplyMigrationsWithIndependentProviderAsync()
    {
        using ServiceProvider independent = BuildProvider(runtimeConnectionString!, migrationConnectionString!, databaseName!);
        await independent.GetRequiredService<IMigrationRunner>().ApplyPendingMigrationsAsync();
    }

    private async Task AssertRunnerWaitsThenAcquiresAndReleasesAsync(
        IMigrationRunner runner,
        string connectionString,
        string resource,
        string scenario)
    {
        await using SqlConnection owner = new(connectionString);
        await using SqlConnection verifier = new(connectionString);
        await owner.OpenAsync();
        await verifier.OpenAsync();

        int ownerSessionId = await GetSessionIdAsync(owner);
        int verifierSessionId = await GetSessionIdAsync(verifier);
        Assert.That(verifierSessionId, Is.Not.EqualTo(ownerSessionId), scenario + " must use distinct physical sessions.");

        bool ownerHoldsLock = false;
        Task<IReadOnlyList<string>>? operation = null;
        try
        {
            int ownerResult = await AcquireSessionAppLockAsync(owner, resource, 2_000);
            Assert.That(ownerResult, Is.GreaterThanOrEqualTo(0), scenario + " owner acquisition");
            ownerHoldsLock = true;

            operation = runner.GetPendingMigrationsAsync();
            await Task.Delay(200);
            Assert.That(operation.IsCompleted, Is.False, scenario + " runner must wait while another session owns the lock.");

            int releaseResult = await ReleaseSessionAppLockAsync(owner, resource);
            Assert.That(releaseResult, Is.GreaterThanOrEqualTo(0), scenario + " owner release");
            ownerHoldsLock = false;

            await operation.WaitAsync(TimeSpan.FromSeconds(15));
            await AssertSessionLockAvailableAsync(verifier, resource, scenario + " runner release");
        }
        finally
        {
            if (ownerHoldsLock)
            {
                await ReleaseSessionAppLockAsync(owner, resource);
            }

            if (operation is not null && !operation.IsCompleted)
            {
                try
                {
                    await operation.WaitAsync(TimeSpan.FromSeconds(15));
                }
                catch
                {
                    // Preserve the primary assertion while ensuring the runner is no longer active.
                }
            }
        }
    }

    private async Task AssertRepeatedRunnerAcquisitionsLeaveNoSessionLockCountAsync(
        IMigrationRunner runner,
        string creationResource)
    {
        await using SqlConnection creationVerifier = new(masterConnectionString!);
        await using SqlConnection migrationVerifier = new(migrationConnectionString!);
        await creationVerifier.OpenAsync();
        await migrationVerifier.OpenAsync();

        for (int attempt = 0; attempt < 3; attempt++)
        {
            IReadOnlyList<string> pending = await runner.GetPendingMigrationsAsync();
            Assert.That(pending, Is.Empty, $"repeated acquisition {attempt + 1}");
        }

        await AssertSessionLockAvailableAsync(creationVerifier, creationResource, "repeated creation acquisitions");
        await AssertSessionLockAvailableAsync(migrationVerifier, MigrationLockResource, "repeated migration acquisitions");
    }

    private async Task AssertCreationLockReleasedAfterExceptionAsync()
    {
        string invalidDatabaseName = DatabasePrefix + new string('X', 129 - DatabasePrefix.Length);
        string resource = DatabaseCreationLockPrefix + invalidDatabaseName;
        SqlConnectionStringBuilder invalidTarget = new(migrationConnectionString!)
        {
            InitialCatalog = invalidDatabaseName,
            Pooling = true
        };

        await using SqlConnection verifier = new(masterConnectionString!);
        await verifier.OpenAsync();
        using ServiceProvider invalidProvider = BuildProvider(
            invalidTarget.ConnectionString,
            invalidTarget.ConnectionString,
            invalidDatabaseName);

        Exception? observed = await CaptureExceptionAsync(
            invalidProvider.GetRequiredService<IMigrationRunner>().GetPendingMigrationsAsync());

        Assert.That(observed, Is.TypeOf<SqlException>(), "The invalid 129-character database name must fail inside the creation critical section.");
        await AssertSessionLockAvailableAsync(verifier, resource, "creation exception release");
        Assert.That(await DatabaseExistsAsync(masterConnectionString!, invalidDatabaseName), Is.False);
    }

    private async Task AssertCreationLockAcquisitionCancellationDoesNotLeakAsync(
        IMigrationRunner runner,
        string resource)
    {
        await using SqlConnection owner = new(masterConnectionString!);
        await using SqlConnection verifier = new(masterConnectionString!);
        await owner.OpenAsync();
        await verifier.OpenAsync();
        Assert.That(await GetSessionIdAsync(verifier), Is.Not.EqualTo(await GetSessionIdAsync(owner)));

        bool ownerHoldsLock = false;
        Task<IReadOnlyList<string>>? operation = null;
        using CancellationTokenSource cancellation = new();
        Exception? observed = null;
        try
        {
            int ownerResult = await AcquireSessionAppLockAsync(owner, resource, 2_000);
            Assert.That(ownerResult, Is.GreaterThanOrEqualTo(0));
            ownerHoldsLock = true;

            operation = runner.GetPendingMigrationsAsync(cancellation.Token);
            await Task.Delay(200);
            Assert.That(operation.IsCompleted, Is.False, "creation acquisition must be waiting before cancellation");

            cancellation.Cancel();
            observed = await CaptureExceptionAsync(operation);
        }
        finally
        {
            if (ownerHoldsLock)
            {
                int releaseResult = await ReleaseSessionAppLockAsync(owner, resource);
                Assert.That(releaseResult, Is.GreaterThanOrEqualTo(0));
            }

            if (operation is not null && !operation.IsCompleted)
            {
                cancellation.Cancel();
                await CaptureExceptionAsync(operation);
            }
        }

        Assert.That(
            IsCancellation(observed, cancellation.IsCancellationRequested),
            Is.True,
            "creation lock acquisition must observe cancellation");
        await AssertSessionLockAvailableAsync(verifier, resource, "creation acquisition cancellation");
    }

    private async Task AssertMigrationLockReleasedAfterExceptionAsync(IMigrationRunner runner)
    {
        const string BackupTable = "SchemaVersion_SERVERB061_Backup";
        await using SqlConnection verifier = new(migrationConnectionString!);
        await verifier.OpenAsync();
        bool schemaAltered = false;

        try
        {
            await ExecuteNonQueryAsync(verifier, $"""
                EXEC sys.sp_rename N'dbo.SchemaVersion', N'{BackupTable}';
                CREATE TABLE dbo.SchemaVersion (UnexpectedColumn int NULL);
                """);
            schemaAltered = true;

            Exception? observed = await CaptureExceptionAsync(runner.GetPendingMigrationsAsync());
            Assert.That(observed, Is.TypeOf<SqlException>(), "invalid SchemaVersion shape must fail after migration lock acquisition");
            Assert.That(((SqlException)observed!).Number, Is.EqualTo(207));
            await AssertSessionLockAvailableAsync(verifier, MigrationLockResource, "migration exception release");
        }
        finally
        {
            if (schemaAltered)
            {
                await ExecuteNonQueryAsync(verifier, $"""
                    DROP TABLE IF EXISTS dbo.SchemaVersion;
                    IF OBJECT_ID(N'dbo.{BackupTable}', N'U') IS NOT NULL
                    BEGIN
                        EXEC sys.sp_rename N'dbo.{BackupTable}', N'SchemaVersion';
                    END;
                    """);
            }
        }
    }

    private async Task AssertMigrationLockReleasedAfterCancellationAsync(IMigrationRunner runner)
    {
        await using SqlConnection tableBlocker = new(migrationConnectionString!);
        await using SqlConnection verifier = new(migrationConnectionString!);
        await tableBlocker.OpenAsync();
        await verifier.OpenAsync();
        Assert.That(await GetSessionIdAsync(verifier), Is.Not.EqualTo(await GetSessionIdAsync(tableBlocker)));

        await using SqlTransaction blockerTransaction = (SqlTransaction)await tableBlocker.BeginTransactionAsync();
        await using (SqlCommand blockerCommand = tableBlocker.CreateCommand())
        {
            blockerCommand.Transaction = blockerTransaction;
            blockerCommand.CommandTimeout = 10;
            blockerCommand.CommandText = "SELECT COUNT_BIG(*) FROM dbo.SchemaVersion WITH (TABLOCKX, HOLDLOCK);";
            await blockerCommand.ExecuteScalarAsync();
        }

        using CancellationTokenSource cancellation = new();
        Task<IReadOnlyList<string>> operation = runner.GetPendingMigrationsAsync(cancellation.Token);
        bool runnerHeldMigrationLock = false;
        Exception? observed = null;
        try
        {
            runnerHeldMigrationLock = await WaitUntilAnotherSessionOwnsLockAsync(
                verifier,
                MigrationLockResource,
                operation,
                TimeSpan.FromSeconds(5));
            cancellation.Cancel();
            observed = await CaptureExceptionAsync(operation);
        }
        finally
        {
            await blockerTransaction.RollbackAsync(CancellationToken.None);
            if (!operation.IsCompleted)
            {
                cancellation.Cancel();
                await CaptureExceptionAsync(operation);
            }
        }

        Assert.That(runnerHeldMigrationLock, Is.True, "runner must hold the migration applock before cancellation");
        Assert.That(
            IsCancellation(observed, cancellation.IsCancellationRequested),
            Is.True,
            "migration operation must observe cancellation inside the critical section");
        await AssertSessionLockAvailableAsync(verifier, MigrationLockResource, "migration cancellation release");
    }

    private async Task<int> CountColoniesAsync(PlayerId playerId, WorldId worldId)
    {
        return await ExecuteScalarAsync<int>(runtimeConnectionString!,
            "SELECT COUNT(*) FROM dbo.Colonies WHERE PlayerId = @PlayerId AND WorldId = @WorldId;",
            new SqlParameter("@PlayerId", playerId.Value),
            new SqlParameter("@WorldId", worldId.Value));
    }

    private async Task UpdateProgressionJsonAsync(Guid accountId, string progressionJson)
    {
        await ExecuteNonQueryAsync(runtimeConnectionString!,
            "UPDATE dbo.Accounts SET ProgressionJson = @ProgressionJson WHERE AccountId = @AccountId;",
            new SqlParameter("@ProgressionJson", progressionJson),
            new SqlParameter("@AccountId", accountId));
    }

    private async Task<string> GetProgressionJsonAsync(Guid accountId)
    {
        return await ExecuteScalarAsync<string>(runtimeConnectionString!,
            "SELECT ProgressionJson FROM dbo.Accounts WHERE AccountId = @AccountId;",
            new SqlParameter("@AccountId", accountId));
    }

    private async Task BackupDatabaseAsync(string backupPath)
    {
        string sql = $"BACKUP DATABASE {QuoteIdentifier(databaseName!)} TO DISK = @BackupPath WITH COPY_ONLY, INIT, CHECKSUM;";
        await ExecuteNonQueryAsync(masterConnectionString!, sql, new SqlParameter("@BackupPath", backupPath));
    }

    private async Task VerifyBackupAsync(string backupPath)
    {
        await ExecuteNonQueryAsync(masterConnectionString!,
            "RESTORE VERIFYONLY FROM DISK = @BackupPath WITH CHECKSUM;",
            new SqlParameter("@BackupPath", backupPath));
    }

    private async Task<IReadOnlyList<BackupFile>> ReadBackupFilesAsync(string backupPath)
    {
        await using SqlConnection connection = new(masterConnectionString!);
        await connection.OpenAsync();
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = "RESTORE FILELISTONLY FROM DISK = @BackupPath;";
        command.Parameters.AddWithValue("@BackupPath", backupPath);
        await using SqlDataReader reader = await command.ExecuteReaderAsync();

        List<BackupFile> files = new();
        int logicalNameOrdinal = reader.GetOrdinal("LogicalName");
        int typeOrdinal = reader.GetOrdinal("Type");
        while (await reader.ReadAsync())
        {
            files.Add(new BackupFile(reader.GetString(logicalNameOrdinal), reader.GetString(typeOrdinal)));
        }

        return files;
    }

    private async Task RestoreDatabaseAsync(string backupPath, string restoreDatabase, IReadOnlyList<BackupFile> files)
    {
        List<string> moves = new();
        for (int index = 0; index < files.Count; index++)
        {
            BackupFile file = files[index];
            string extension = string.Equals(file.Type, "L", StringComparison.OrdinalIgnoreCase) ? ".ldf" : ".mdf";
            string targetPath = Path.Combine(Path.GetTempPath(), $"{restoreDatabase}_{index}{extension}");
            cleanupFiles.Add(targetPath);
            moves.Add($"MOVE N'{EscapeSqlLiteral(file.LogicalName)}' TO N'{EscapeSqlLiteral(targetPath)}'");
        }

        string sql = $"RESTORE DATABASE {QuoteIdentifier(restoreDatabase)} FROM DISK = N'{EscapeSqlLiteral(backupPath)}' WITH {string.Join(", ", moves)}, RECOVERY;";
        await ExecuteNonQueryAsync(masterConnectionString!, sql);
    }

    private static bool TryCreateAccount(IAccountRepository repository, AccountRecord account)
    {
        try
        {
            repository.Create(account);
            return true;
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            return false;
        }
    }

    private static AccountRecord CreateAccountRecord(string email, string displayName, AccountProgression? progression = null)
    {
        return new AccountRecord(
            new AccountProfile(Guid.NewGuid(), PlayerId.New(), displayName, email, "en-US", "UTC", "US", DateTimeOffset.UtcNow, null, AccountStatus.Active),
            new AccountSettings("USD", true, true),
            new AccountPreferences("en-US", true, false, "High", 0.8, true, new Dictionary<string, string>()),
            progression ?? CreateProgression());
    }

    private static AccountProgression CreateProgression(
        IEnumerable<string>? achievements = null,
        IReadOnlyDictionary<string, double>? statistics = null,
        IEnumerable<string>? rewards = null,
        IReadOnlyList<string>? seasons = null,
        IReadOnlyList<string>? purchases = null)
    {
        return new AccountProgression(
            new HashSet<string>(achievements ?? Array.Empty<string>(), StringComparer.Ordinal),
            statistics ?? new Dictionary<string, double>(StringComparer.Ordinal),
            new HashSet<string>(rewards ?? Array.Empty<string>(), StringComparer.Ordinal),
            seasons ?? Array.Empty<string>(),
            purchases ?? Array.Empty<string>());
    }

    private static void AssertProgression(AccountProgression expected, AccountProgression actual, string caseName)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.GlobalAchievements, Is.EquivalentTo(expected.GlobalAchievements), caseName + " achievements");
            Assert.That(actual.GlobalStatistics, Is.EquivalentTo(expected.GlobalStatistics), caseName + " statistics");
            Assert.That(actual.PermanentRewards, Is.EquivalentTo(expected.PermanentRewards), caseName + " rewards");
            Assert.That(actual.SeasonHistory, Is.EqualTo(expected.SeasonHistory), caseName + " seasons");
            Assert.That(actual.PurchaseHistory, Is.EqualTo(expected.PurchaseHistory), caseName + " purchases");
        });
    }

    private static ColonyRecord CreateColony(PlayerId playerId, WorldId worldId, string hiveName)
    {
        return new ColonyRecord(
            new ColonyProfile(ColonyId.New(), playerId, worldId.Value, hiveName, DateTimeOffset.UtcNow, "TestSeason", 12, BeeId.New(), 1, 0, ColonyStatus.Active),
            new ColonyStatistics(12, 2, 3, 1, DateTimeOffset.UtcNow),
            new ColonySettings("Manual", "None", "Semantic"),
            new[] { new ColonyHistoryEntry(DateTimeOffset.UtcNow, "Created", "Synthetic SQL readiness colony.") },
            1);
    }

    private static ServiceProvider BuildProvider(string runtimeConnection, string migrationConnection, string targetDatabaseName)
    {
        Dictionary<string, string?> values = new()
        {
            ["Persistence:Provider"] = "SqlServer",
            ["SqlServer:DatabaseName"] = targetDatabaseName,
            ["SqlServer:ConnectionStringName"] = "BeeKingdomLegacyFallback",
            ["SqlServer:ConnectionString"] = "Server=invalid-fallback;Database=invalid-fallback;Integrated Security=True;",
            ["SqlServer:RuntimeConnectionStringName"] = "BeeKingdomRuntime",
            ["SqlServer:MigrationConnectionStringName"] = "BeeKingdomMigrations",
            ["ConnectionStrings:BeeKingdomRuntime"] = runtimeConnection,
            ["ConnectionStrings:BeeKingdomMigrations"] = migrationConnection,
            ["SqlServer:CommandTimeoutSeconds"] = "30",
            ["Accounts:DefaultLanguage"] = "en-US",
            ["Accounts:DefaultTimeZone"] = "UTC",
            ["Accounts:DefaultCurrency"] = "USD",
            ["Authentication:AccessTokenLifetime"] = "00:15:00",
            ["Authentication:RefreshTokenLifetime"] = "14.00:00:00",
            ["Authentication:MaxSessionsPerAccount"] = "5",
            ["Authentication:MaxFailedAttempts"] = "5",
            ["Colony:MaxSnapshotBytes"] = "1048576",
            ["Colony:AutoSaveInterval"] = "00:05:00",
            ["Colony:RetentionDays"] = "30",
            ["Chat:Enabled"] = "true",
            ["Chat:RealtimeEnabled"] = "false"
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        ServiceCollection services = new();
        services.AddSingleton(configuration);
        services.AddLogging(logging => logging.AddConsole());
        services
            .AddBeeKingdomInfrastructure(configuration)
            .AddBeeKingdomPersistence(configuration)
            .AddBeeKingdomAuthentication(configuration)
            .AddBeeKingdomAccounts(configuration)
            .AddBeeKingdomColony(configuration)
            .AddBeeKingdomChat(configuration);

        return services.BuildServiceProvider(validateScopes: true);
    }

    private static async Task<Exception?> CaptureExceptionAsync(Task operation)
    {
        try
        {
            await operation.WaitAsync(TimeSpan.FromSeconds(10));
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private static bool IsCancellation(Exception? exception, bool cancellationRequested)
    {
        return exception is OperationCanceledException
            || cancellationRequested && exception is SqlException;
    }

    private static async Task<bool> WaitUntilAnotherSessionOwnsLockAsync(
        SqlConnection verifier,
        string resource,
        Task operation,
        TimeSpan timeout)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(timeout);
        while (!operation.IsCompleted && DateTimeOffset.UtcNow < deadline)
        {
            int acquisitionResult = await AcquireSessionAppLockAsync(verifier, resource, 0);
            if (acquisitionResult == -1)
            {
                return true;
            }

            Assert.That(acquisitionResult, Is.GreaterThanOrEqualTo(0), "unexpected applock probe result");
            int releaseResult = await ReleaseSessionAppLockAsync(verifier, resource);
            Assert.That(releaseResult, Is.GreaterThanOrEqualTo(0), "applock probe release");
            await Task.Delay(50);
        }

        return false;
    }

    private static async Task AssertSessionLockAvailableAsync(
        SqlConnection verifier,
        string resource,
        string scenario)
    {
        int acquisitionResult = await AcquireSessionAppLockAsync(verifier, resource, 2_000);
        int? releaseResult = null;
        if (acquisitionResult >= 0)
        {
            releaseResult = await ReleaseSessionAppLockAsync(verifier, resource);
        }

        Assert.Multiple(() =>
        {
            Assert.That(acquisitionResult, Is.GreaterThanOrEqualTo(0), scenario + " acquisition");
            Assert.That(releaseResult, Is.GreaterThanOrEqualTo(0), scenario + " release");
        });
    }

    private static async Task<int> AcquireSessionAppLockAsync(
        SqlConnection connection,
        string resource,
        int lockTimeoutMilliseconds)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandTimeout = 10;
        command.CommandText = """
            DECLARE @LockResult int;
            EXEC @LockResult = sys.sp_getapplock
                @Resource = @Resource,
                @LockMode = N'Exclusive',
                @LockOwner = N'Session',
                @LockTimeout = @LockTimeoutMilliseconds;
            SELECT @LockResult;
            """;
        command.Parameters.AddWithValue("@Resource", resource);
        command.Parameters.AddWithValue("@LockTimeoutMilliseconds", lockTimeoutMilliseconds);
        object? result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    private static async Task<int> ReleaseSessionAppLockAsync(SqlConnection connection, string resource)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandTimeout = 10;
        command.CommandText = """
            DECLARE @LockResult int;
            EXEC @LockResult = sys.sp_releaseapplock
                @Resource = @Resource,
                @LockOwner = N'Session';
            SELECT @LockResult;
            """;
        command.Parameters.AddWithValue("@Resource", resource);
        object? result = await command.ExecuteScalarAsync(CancellationToken.None);
        return Convert.ToInt32(result);
    }

    private static async Task<int> GetSessionIdAsync(SqlConnection connection)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT @@SPID;";
        object? result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    private static async Task ExecuteNonQueryAsync(
        SqlConnection connection,
        string sql,
        params SqlParameter[] parameters)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandTimeout = 60;
        command.CommandText = sql;
        command.Parameters.AddRange(parameters);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<bool> DatabaseExistsAsync(string connectionString, string name)
    {
        int count = await ExecuteScalarAsync<int>(connectionString,
            "SELECT COUNT(*) FROM sys.databases WHERE name = @DatabaseName;",
            new SqlParameter("@DatabaseName", name));
        return count == 1;
    }

    private static async Task DropDatabaseAsync(string connectionString, string name)
    {
        if (!name.StartsWith(DatabasePrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Refusing to drop a database outside the disposable SERVER-B-057 prefix.");
        }

        string identifier = QuoteIdentifier(name);
        string sql = $"IF DB_ID(@DatabaseName) IS NOT NULL BEGIN ALTER DATABASE {identifier} SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE {identifier}; END;";
        await ExecuteNonQueryAsync(connectionString, sql, new SqlParameter("@DatabaseName", name));
    }

    private static async Task<T> ExecuteScalarAsync<T>(string connectionString, string sql, params SqlParameter[] parameters)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using SqlCommand command = connection.CreateCommand();
        command.CommandTimeout = 30;
        command.CommandText = sql;
        command.Parameters.AddRange(parameters);
        object? value = await command.ExecuteScalarAsync();
        return (T)Convert.ChangeType(value!, typeof(T));
    }

    private static async Task ExecuteNonQueryAsync(string connectionString, string sql, params SqlParameter[] parameters)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using SqlCommand command = connection.CreateCommand();
        command.CommandTimeout = 60;
        command.CommandText = sql;
        command.Parameters.AddRange(parameters);
        await command.ExecuteNonQueryAsync();
    }

    private static string QuoteIdentifier(string value) => "[" + value.Replace("]", "]]", StringComparison.Ordinal) + "]";
    private static string EscapeSqlLiteral(string value) => value.Replace("'", "''", StringComparison.Ordinal);

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

    private sealed class SqlOfflineProductionTestClock(DateTimeOffset now) : BeeKingdom.HiveOperations.IServerClock
    {
        public DateTimeOffset UtcNow { get; private set; } = now;
        public void Advance(TimeSpan amount) => UtcNow = UtcNow.Add(amount);
    }

    private sealed record BackupFile(string LogicalName, string Type);
}
