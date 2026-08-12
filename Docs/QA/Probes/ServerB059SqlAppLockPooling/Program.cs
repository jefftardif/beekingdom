using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using BeeKingdom.Persistence.Configuration;
using BeeKingdom.Persistence.Migrations;
using BeeKingdom.Persistence.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

internal static class Program
{
    private const string DataSource = @"(localdb)\MSSQLLocalDB";
    private const string DatabasePrefix = "BeeKingdom_QA_SERVERB059_";
    private const string ApplicationPrefix = "BeeKingdom.QA.SERVERB059.";
    private static readonly List<string> DisposableDatabases = new();
    private static readonly HashSet<string> DisposableFiles = new(StringComparer.OrdinalIgnoreCase);

    private static async Task<int> Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Expected one JSON evidence output path.");
            return 64;
        }

        string outputPath = Path.GetFullPath(args[0]);
        string runId = Guid.NewGuid().ToString("N")[..12];
        Dictionary<string, object?> evidence = new()
        {
            ["proof_id"] = "QA-B-SERVER-B-059-SQL-APPLOCK-POOLING",
            ["generated_utc"] = DateTimeOffset.UtcNow,
            ["scope"] = "LocalDB disposable, local-only, no producer-code mutation, no remote target",
            ["localdb_data_source"] = DataSource,
            ["driver"] = new Dictionary<string, object?>
            {
                ["assembly"] = typeof(SqlConnection).Assembly.GetName().Name,
                ["assembly_version"] = typeof(SqlConnection).Assembly.GetName().Version?.ToString(),
                ["informational_version"] = typeof(SqlConnection).Assembly
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            }
        };

        Exception? fatal = null;
        string phase = "bootstrap";
        try
        {
            await VerifyLocalDbAsync();

            string defaultProbe = $"Server={DataSource};Initial Catalog=master;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=5;Application Name={ApplicationPrefix}{runId}.DefaultKeywordProbe;";
            SqlConnectionStringBuilder defaultBuilder = new(defaultProbe);
            evidence["pooling_default"] = new Dictionary<string, object?>
            {
                ["pooling_keyword_present_in_input"] = defaultProbe.Contains("Pooling=", StringComparison.OrdinalIgnoreCase),
                ["effective_pooling"] = defaultBuilder.Pooling
            };

            phase = "pooling_true_default_session_lock";
            Dictionary<string, object?> pooled = await RunPooledSessionLockScenarioAsync(runId);
            evidence["pooling_true_default_session_lock"] = pooled;

            phase = "pooling_false_session_lock";
            Dictionary<string, object?> unpooled = await RunUnpooledSessionLockScenarioAsync(runId);
            evidence["pooling_false_session_lock"] = unpooled;

            phase = "pooling_true_explicit_release";
            Dictionary<string, object?> explicitRelease = await RunExplicitReleaseScenarioAsync(runId);
            evidence["pooling_true_explicit_release"] = explicitRelease;

            phase = "migration_runner_cross_pool";
            Dictionary<string, object?> runnerCrossPool = await RunMigrationRunnerCrossPoolScenarioAsync(runId);
            evidence["migration_runner_cross_pool"] = runnerCrossPool;

            phase = "migration_runner_concurrent_creation_trials";
            List<Dictionary<string, object?>> concurrentTrials = new();
            for (int index = 1; index <= 3; index++)
            {
                concurrentTrials.Add(await RunConcurrentCreationTrialAsync(runId, index));
            }
            evidence["migration_runner_concurrent_creation_trials"] = concurrentTrials;

            bool directConfirmed = IsTrue(pooled, "independent_pool_blocked_before_owner_reuse")
                && IsTrue(pooled, "same_physical_session_reused")
                && IsTrue(pooled, "independent_pool_succeeds_after_owner_cleanup");
            bool controlsConfirmed = IsTrue(unpooled, "lock_absent_after_dispose")
                && IsTrue(unpooled, "new_session_acquires_immediately")
                && IsTrue(explicitRelease, "lock_absent_after_explicit_release_and_pool_reuse")
                && IsTrue(explicitRelease, "independent_pool_acquires_immediately");
            bool runnerConfirmed = IsTrue(runnerCrossPool, "second_pool_blocked_on_creation_lock")
                && IsTrue(runnerCrossPool, "second_pool_blocked_on_migration_lock_after_creation_pool_cleared")
                && IsTrue(runnerCrossPool, "retry_succeeds_after_all_pools_cleared");
            bool concurrentConfirmed = concurrentTrials.All(trial => IsTrue(trial, "pooling_failure_observed"));

            evidence["blocker_assessment"] = new Dictionary<string, object?>
            {
                ["idle_pooled_session_lock_blocks_independent_pool_confirmed"] = directConfirmed,
                ["pooling_false_and_explicit_release_controls_confirmed"] = controlsConfirmed,
                ["current_runner_cross_pool_block_confirmed"] = runnerConfirmed,
                ["current_runner_concurrent_creation_failure_reproduced_in_all_trials"] = concurrentConfirmed,
                ["confirmed_blocker_for_session_lock_without_explicit_release"] = directConfirmed && controlsConfirmed,
                ["confirmed_blocker"] = directConfirmed && controlsConfirmed
            };
        }
        catch (Exception exception)
        {
            fatal = exception;
            evidence["fatal_phase"] = phase;
            evidence["fatal_error"] = SanitizeException(exception);
        }
        finally
        {
            Dictionary<string, object?> cleanup = await CleanupAsync(runId);
            evidence["cleanup"] = cleanup;
            evidence["completed"] = fatal is null;
            evidence["cleanup_complete"] = IsTrue(cleanup, "zero_database_residue")
                && IsTrue(cleanup, "zero_file_residue")
                && IsTrue(cleanup, "zero_probe_sessions_after_pool_clear");

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            await File.WriteAllTextAsync(
                outputPath,
                JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true }));
        }

        return fatal is null ? 0 : 2;
    }

    private static async Task<Dictionary<string, object?>> RunPooledSessionLockScenarioAsync(string runId)
    {
        string step = "initialize";
        try
        {
            SqlConnection.ClearAllPools();
            string resource = $"BeeKingdom.QA.PoolDefault.{runId}";
            string ownerConnectionString = BuildConnectionString("master", $"{ApplicationPrefix}{runId}.PoolDefault.Owner", null);
            string competitorConnectionString = BuildConnectionString("master", $"{ApplicationPrefix}{runId}.PoolDefault.Competitor", null);

            int firstSpid;
            int acquireResult;
            string initialMode;
            await using (SqlConnection first = new(ownerConnectionString))
            {
                step = "open_initial_owner";
                await first.OpenAsync();
                step = "read_initial_spid";
                firstSpid = await GetSpidAsync(first);
                step = "acquire_initial_lock";
                acquireResult = await AcquireSessionLockAsync(first, resource, 0);
                step = "inspect_initial_lock";
                initialMode = await GetSessionLockModeAsync(first, resource);
            }

            int reusedSpid;
            string modeAfterReuse;
            int blockedResult;
            long blockedElapsedMilliseconds;
            int? competitorReleaseBeforeReuse = null;
            int? ownerReleaseAfterReuse = null;
            int acquireAfterOwnerCleanup;
            int competitorReleaseAfterOwnerCleanup = int.MinValue;

            await using (SqlConnection competitor = new(competitorConnectionString))
            {
                step = "open_competitor_before_owner_reuse";
                await competitor.OpenAsync();
                step = "acquire_competitor_before_owner_reuse";
                Stopwatch blockedWatch = Stopwatch.StartNew();
                blockedResult = await AcquireSessionLockAsync(competitor, resource, 1200);
                blockedWatch.Stop();
                blockedElapsedMilliseconds = blockedWatch.ElapsedMilliseconds;
                if (blockedResult >= 0)
                {
                    step = "release_competitor_before_owner_reuse";
                    competitorReleaseBeforeReuse = await ReleaseSessionLockAsync(competitor, resource);
                }

                await using (SqlConnection reused = new(ownerConnectionString))
                {
                    step = "open_reused_owner_after_competitor_probe";
                    await reused.OpenAsync();
                    step = "read_reused_spid";
                    reusedSpid = await GetSpidAsync(reused);
                    step = "inspect_reused_lock";
                    modeAfterReuse = await GetSessionLockModeAsync(reused, resource);
                    if (IsExclusive(modeAfterReuse))
                    {
                        step = "release_owner_after_reuse";
                        ownerReleaseAfterReuse = await ReleaseSessionLockAsync(reused, resource);
                    }
                }

                step = "acquire_competitor_after_owner_cleanup";
                acquireAfterOwnerCleanup = await AcquireSessionLockAsync(competitor, resource, 2000);
                if (acquireAfterOwnerCleanup >= 0)
                {
                    step = "release_competitor";
                    competitorReleaseAfterOwnerCleanup = await ReleaseSessionLockAsync(competitor, resource);
                }
            }

            SqlConnection.ClearAllPools();
            return new Dictionary<string, object?>
            {
                ["pooling_keyword_explicit"] = false,
                ["effective_pooling"] = new SqlConnectionStringBuilder(ownerConnectionString).Pooling,
                ["initial_acquire_result"] = acquireResult,
                ["initial_lock_mode"] = initialMode,
                ["first_spid"] = firstSpid,
                ["reused_spid"] = reusedSpid,
                ["same_physical_session_reused"] = firstSpid == reusedSpid,
                ["mode_after_close_and_pool_reuse"] = modeAfterReuse,
                ["lock_present_after_close_and_pool_reuse"] = IsExclusive(modeAfterReuse),
                ["independent_pool_acquire_result_before_owner_reuse"] = blockedResult,
                ["independent_pool_wait_before_owner_reuse_milliseconds"] = blockedElapsedMilliseconds,
                ["independent_pool_blocked_before_owner_reuse"] = blockedResult < 0,
                ["competitor_release_before_owner_reuse_result"] = competitorReleaseBeforeReuse,
                ["owner_release_after_reuse_if_still_held_result"] = ownerReleaseAfterReuse,
                ["independent_pool_acquire_result_after_owner_cleanup"] = acquireAfterOwnerCleanup,
                ["independent_pool_succeeds_after_owner_cleanup"] = acquireAfterOwnerCleanup >= 0,
                ["independent_pool_release_after_owner_cleanup_result"] = competitorReleaseAfterOwnerCleanup
            };
        }
        catch (Exception exception)
        {
            throw new ProbeStepException(step, exception);
        }
    }

    private static async Task<Dictionary<string, object?>> RunUnpooledSessionLockScenarioAsync(string runId)
    {
        SqlConnection.ClearAllPools();
        string resource = $"BeeKingdom.QA.PoolFalse.{runId}";
        string connectionString = BuildConnectionString("master", $"{ApplicationPrefix}{runId}.PoolFalse", false);
        int firstSpid;
        int initialAcquire;
        await using (SqlConnection first = new(connectionString))
        {
            await first.OpenAsync();
            firstSpid = await GetSpidAsync(first);
            initialAcquire = await AcquireSessionLockAsync(first, resource, 0);
        }

        int nextSpid;
        string modeBeforeAcquire;
        int nextAcquire;
        long elapsedMilliseconds;
        int releaseResult;
        await using (SqlConnection next = new(connectionString))
        {
            await next.OpenAsync();
            nextSpid = await GetSpidAsync(next);
            modeBeforeAcquire = await GetSessionLockModeAsync(next, resource);
            Stopwatch watch = Stopwatch.StartNew();
            nextAcquire = await AcquireSessionLockAsync(next, resource, 1200);
            watch.Stop();
            elapsedMilliseconds = watch.ElapsedMilliseconds;
            releaseResult = nextAcquire >= 0 ? await ReleaseSessionLockAsync(next, resource) : int.MinValue;
        }

        return new Dictionary<string, object?>
        {
            ["effective_pooling"] = new SqlConnectionStringBuilder(connectionString).Pooling,
            ["initial_acquire_result"] = initialAcquire,
            ["first_spid"] = firstSpid,
            ["next_spid"] = nextSpid,
            ["lock_mode_before_next_acquire"] = modeBeforeAcquire,
            ["lock_absent_after_dispose"] = !IsExclusive(modeBeforeAcquire),
            ["next_acquire_result"] = nextAcquire,
            ["next_acquire_wait_milliseconds"] = elapsedMilliseconds,
            ["new_session_acquires_immediately"] = nextAcquire >= 0 && elapsedMilliseconds < 500,
            ["release_result"] = releaseResult
        };
    }

    private static async Task<Dictionary<string, object?>> RunExplicitReleaseScenarioAsync(string runId)
    {
        SqlConnection.ClearAllPools();
        string resource = $"BeeKingdom.QA.ExplicitRelease.{runId}";
        string ownerConnectionString = BuildConnectionString("master", $"{ApplicationPrefix}{runId}.ExplicitRelease.Owner", null);
        string competitorConnectionString = BuildConnectionString("master", $"{ApplicationPrefix}{runId}.ExplicitRelease.Competitor", null);

        int firstSpid;
        int acquireResult;
        int explicitReleaseResult;
        await using (SqlConnection first = new(ownerConnectionString))
        {
            await first.OpenAsync();
            firstSpid = await GetSpidAsync(first);
            acquireResult = await AcquireSessionLockAsync(first, resource, 0);
            explicitReleaseResult = await ReleaseSessionLockAsync(first, resource);
        }

        int reusedSpid;
        string modeAfterReuse;
        await using (SqlConnection reused = new(ownerConnectionString))
        {
            await reused.OpenAsync();
            reusedSpid = await GetSpidAsync(reused);
            modeAfterReuse = await GetSessionLockModeAsync(reused, resource);
        }

        int competitorAcquire;
        long elapsedMilliseconds;
        int competitorRelease;
        await using (SqlConnection competitor = new(competitorConnectionString))
        {
            await competitor.OpenAsync();
            Stopwatch watch = Stopwatch.StartNew();
            competitorAcquire = await AcquireSessionLockAsync(competitor, resource, 1200);
            watch.Stop();
            elapsedMilliseconds = watch.ElapsedMilliseconds;
            competitorRelease = competitorAcquire >= 0 ? await ReleaseSessionLockAsync(competitor, resource) : int.MinValue;
        }

        SqlConnection.ClearAllPools();
        return new Dictionary<string, object?>
        {
            ["effective_pooling"] = new SqlConnectionStringBuilder(ownerConnectionString).Pooling,
            ["initial_acquire_result"] = acquireResult,
            ["explicit_release_result"] = explicitReleaseResult,
            ["first_spid"] = firstSpid,
            ["reused_spid"] = reusedSpid,
            ["same_physical_session_reused"] = firstSpid == reusedSpid,
            ["mode_after_release_close_and_pool_reuse"] = modeAfterReuse,
            ["lock_absent_after_explicit_release_and_pool_reuse"] = !IsExclusive(modeAfterReuse),
            ["independent_pool_acquire_result"] = competitorAcquire,
            ["independent_pool_wait_milliseconds"] = elapsedMilliseconds,
            ["independent_pool_acquires_immediately"] = competitorAcquire >= 0 && elapsedMilliseconds < 500,
            ["independent_pool_release_result"] = competitorRelease
        };
    }

    private static async Task<Dictionary<string, object?>> RunMigrationRunnerCrossPoolScenarioAsync(string runId)
    {
        SqlConnection.ClearAllPools();
        string databaseName = NewDatabaseName(runId, "CrossPool");
        string ownerTarget = BuildConnectionString(databaseName, $"{ApplicationPrefix}{runId}.Runner.Owner", null);
        string competitorTarget = BuildConnectionString(databaseName, $"{ApplicationPrefix}{runId}.Runner.Competitor", null);
        string ownerMaster = WithDatabase(ownerTarget, "master");
        string competitorMaster = WithDatabase(competitorTarget, "master");
        string creationResource = "BeeKingdom.Database.Create:" + databaseName;
        const string migrationResource = "BeeKingdom.Database.Migrations";

        SqlServerMigrationRunner owner = BuildRunner(ownerTarget, databaseName, 2);
        SqlServerMigrationRunner competitor = BuildRunner(competitorTarget, databaseName, 2);

        Dictionary<string, object?> ownerAttempt = await RunRunnerAttemptAsync(owner);
        Dictionary<string, object?> ownerCreationLock = await InspectLockAsync(ownerMaster, creationResource);
        Dictionary<string, object?> ownerMigrationLock = await InspectLockAsync(ownerTarget, migrationResource);

        Dictionary<string, object?> competitorCreationAttempt = await RunRunnerAttemptAsync(competitor);

        ClearPool(ownerMaster);
        Dictionary<string, object?> competitorMigrationAttempt = await RunRunnerAttemptAsync(competitor);
        Dictionary<string, object?> competitorCreationLockAfterSecondAttempt = await InspectLockAsync(competitorMaster, creationResource);

        SqlConnection.ClearAllPools();
        Dictionary<string, object?> retryAfterClear = await RunRunnerAttemptAsync(competitor);

        return new Dictionary<string, object?>
        {
            ["pooling_default_effective"] = new SqlConnectionStringBuilder(ownerTarget).Pooling,
            ["owner_initial_apply"] = ownerAttempt,
            ["owner_creation_lock_after_runner_return"] = ownerCreationLock,
            ["owner_migration_lock_after_runner_return"] = ownerMigrationLock,
            ["competitor_attempt_before_pool_clear"] = competitorCreationAttempt,
            ["second_pool_blocked_on_creation_lock"] = IsSql51057(competitorCreationAttempt),
            ["competitor_attempt_after_only_owner_master_pool_clear"] = competitorMigrationAttempt,
            ["competitor_creation_lock_after_second_attempt"] = competitorCreationLockAfterSecondAttempt,
            ["second_pool_passed_creation_stage"] = IsExclusiveFromInspection(competitorCreationLockAfterSecondAttempt),
            ["second_pool_blocked_on_migration_lock_after_creation_pool_cleared"] = IsSql51057(competitorMigrationAttempt)
                && IsExclusiveFromInspection(competitorCreationLockAfterSecondAttempt),
            ["retry_after_all_pools_cleared"] = retryAfterClear,
            ["retry_succeeds_after_all_pools_cleared"] = IsTrue(retryAfterClear, "success")
        };
    }

    private static async Task<Dictionary<string, object?>> RunConcurrentCreationTrialAsync(string runId, int trial)
    {
        SqlConnection.ClearAllPools();
        string databaseName = NewDatabaseName(runId, $"Concurrent{trial}");
        string target = BuildConnectionString(databaseName, $"{ApplicationPrefix}{runId}.Concurrent.{trial}", null);
        string master = WithDatabase(target, "master");

        int[] prewarmedSpids;
        await using (SqlConnection warmOne = new(master))
        await using (SqlConnection warmTwo = new(master))
        {
            await Task.WhenAll(warmOne.OpenAsync(), warmTwo.OpenAsync());
            prewarmedSpids = [await GetSpidAsync(warmOne), await GetSpidAsync(warmTwo)];
        }

        SqlServerMigrationRunner first = BuildRunner(target, databaseName, 2);
        SqlServerMigrationRunner second = BuildRunner(target, databaseName, 2);
        Dictionary<string, object?>[] attempts = await Task.WhenAll(
            RunRunnerAttemptAsync(first),
            RunRunnerAttemptAsync(second));

        int successes = attempts.Count(attempt => IsTrue(attempt, "success"));
        int sql51057Failures = attempts.Count(IsSql51057);
        SqlConnection.ClearAllPools();
        Dictionary<string, object?> completionRetry = await RunRunnerAttemptAsync(first);

        return new Dictionary<string, object?>
        {
            ["trial"] = trial,
            ["pooling_default_effective"] = new SqlConnectionStringBuilder(target).Pooling,
            ["prewarmed_master_spids_distinct"] = prewarmedSpids.Distinct().Count() == 2,
            ["attempts"] = attempts,
            ["success_count"] = successes,
            ["sql_51057_failure_count"] = sql51057Failures,
            ["pooling_failure_observed"] = successes == 1 && sql51057Failures == 1,
            ["retry_after_pool_clear"] = completionRetry,
            ["retry_succeeds_after_pool_clear"] = IsTrue(completionRetry, "success")
        };
    }

    private static SqlServerMigrationRunner BuildRunner(string migrationConnectionString, string databaseName, int timeoutSeconds)
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();
        IOptions<SqlServerOptions> options = Options.Create(new SqlServerOptions
        {
            DatabaseName = databaseName,
            ConnectionString = migrationConnectionString,
            MigrationConnectionString = migrationConnectionString,
            RuntimeConnectionString = migrationConnectionString,
            CommandTimeoutSeconds = timeoutSeconds
        });
        SqlConnectionFactory factory = new(configuration, options);
        return new SqlServerMigrationRunner(
            factory,
            options,
            NullLogger<SqlServerMigrationRunner>.Instance,
            new MigrationDiagnostics());
    }

    private static async Task<Dictionary<string, object?>> RunRunnerAttemptAsync(SqlServerMigrationRunner runner)
    {
        Stopwatch watch = Stopwatch.StartNew();
        try
        {
            await runner.ApplyPendingMigrationsAsync();
            watch.Stop();
            return new Dictionary<string, object?>
            {
                ["success"] = true,
                ["elapsed_milliseconds"] = watch.ElapsedMilliseconds,
                ["exception_type"] = null,
                ["sql_error_number"] = null
            };
        }
        catch (Exception exception)
        {
            watch.Stop();
            SqlException? sql = FindSqlException(exception);
            return new Dictionary<string, object?>
            {
                ["success"] = false,
                ["elapsed_milliseconds"] = watch.ElapsedMilliseconds,
                ["exception_type"] = exception.GetType().Name,
                ["sql_error_number"] = sql?.Number
            };
        }
    }

    private static async Task<Dictionary<string, object?>> InspectLockAsync(string connectionString, string resource)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        int spid = await GetSpidAsync(connection);
        string mode = await GetSessionLockModeAsync(connection, resource);
        return new Dictionary<string, object?>
        {
            ["spid"] = spid,
            ["mode"] = mode,
            ["exclusive"] = IsExclusive(mode)
        };
    }

    private static async Task VerifyLocalDbAsync()
    {
        string connectionString = BuildConnectionString("master", ApplicationPrefix + "Bootstrap", false);
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT CONVERT(int, SERVERPROPERTY('IsLocalDB'));";
        int isLocalDb = Convert.ToInt32(await command.ExecuteScalarAsync());
        if (isLocalDb != 1)
        {
            throw new InvalidOperationException("The QA probe did not connect to SQL Server LocalDB.");
        }
    }

    private static async Task<int> AcquireSessionLockAsync(SqlConnection connection, string resource, int timeoutMilliseconds)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandTimeout = Math.Max(5, (timeoutMilliseconds / 1000) + 3);
        command.CommandText = """
            DECLARE @Result int;
            EXEC @Result = sys.sp_getapplock
                @Resource = @Resource,
                @LockMode = N'Exclusive',
                @LockOwner = N'Session',
                @LockTimeout = @LockTimeoutMilliseconds;
            SELECT @Result;
            """;
        command.Parameters.AddWithValue("@Resource", resource);
        command.Parameters.AddWithValue("@LockTimeoutMilliseconds", timeoutMilliseconds);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static async Task<int> ReleaseSessionLockAsync(SqlConnection connection, string resource)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandTimeout = 5;
        command.CommandText = """
            DECLARE @Result int;
            EXEC @Result = sys.sp_releaseapplock
                @Resource = @Resource,
                @LockOwner = N'Session';
            SELECT @Result;
            """;
        command.Parameters.AddWithValue("@Resource", resource);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static async Task<string> GetSessionLockModeAsync(SqlConnection connection, string resource)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandTimeout = 5;
        command.CommandText = "SELECT APPLOCK_MODE(N'public', @Resource, N'Session');";
        command.Parameters.AddWithValue("@Resource", resource);
        return Convert.ToString(await command.ExecuteScalarAsync()) ?? "NoLock";
    }

    private static async Task<int> GetSpidAsync(SqlConnection connection)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT @@SPID;";
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static string BuildConnectionString(string database, string applicationName, bool? pooling)
    {
        string raw = $"Server={DataSource};Initial Catalog={database};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=5;Application Name={applicationName};";
        SqlConnectionStringBuilder builder = new(raw);
        if (pooling.HasValue)
        {
            builder.Pooling = pooling.Value;
        }
        return builder.ConnectionString;
    }

    private static string WithDatabase(string connectionString, string database)
    {
        SqlConnectionStringBuilder builder = new(connectionString) { InitialCatalog = database };
        return builder.ConnectionString;
    }

    private static void ClearPool(string connectionString)
    {
        using SqlConnection marker = new(connectionString);
        SqlConnection.ClearPool(marker);
    }

    private static string NewDatabaseName(string runId, string suffix)
    {
        string name = $"{DatabasePrefix}{runId}_{suffix}";
        DisposableDatabases.Add(name);
        return name;
    }

    private static async Task<Dictionary<string, object?>> CleanupAsync(string runId)
    {
        List<Dictionary<string, object?>> errors = new();
        SqlConnection.ClearAllPools();
        await Task.Delay(250);

        string cleanupConnectionString = BuildConnectionString("master", $"{ApplicationPrefix}{runId}.Cleanup", false);
        foreach (string database in DisposableDatabases.Distinct(StringComparer.Ordinal))
        {
            try
            {
                await CaptureDatabaseFilesAsync(cleanupConnectionString, database);
                await DropDatabaseAsync(cleanupConnectionString, database);
            }
            catch (Exception exception)
            {
                errors.Add(SanitizeException(exception));
            }
        }

        SqlConnection.ClearAllPools();
        await Task.Delay(250);

        int databaseResidue = -1;
        int sessionResidue = -1;
        try
        {
            await using SqlConnection check = new(BuildConnectionString("master", $"{ApplicationPrefix}{runId}.CleanupCheck", false));
            await check.OpenAsync();
            databaseResidue = await ExecuteScalarIntAsync(check, """
                SELECT COUNT(*)
                FROM sys.databases
                WHERE name LIKE N'BeeKingdom[_]QA[_]SERVERB059[_]%';
                """);
            sessionResidue = await ExecuteScalarIntAsync(check, """
                SELECT COUNT(*)
                FROM sys.dm_exec_sessions
                WHERE program_name LIKE N'BeeKingdom.QA.SERVERB059.%'
                  AND session_id <> @@SPID;
                """);
        }
        catch (Exception exception)
        {
            errors.Add(SanitizeException(exception));
        }

        int fileResidue = DisposableFiles.Count(File.Exists);
        SqlConnection.ClearAllPools();

        return new Dictionary<string, object?>
        {
            ["database_names_registered"] = DisposableDatabases.Distinct(StringComparer.Ordinal).Count(),
            ["database_residue_count"] = databaseResidue,
            ["zero_database_residue"] = databaseResidue == 0,
            ["database_file_paths_observed"] = DisposableFiles.Count,
            ["file_residue_count"] = fileResidue,
            ["zero_file_residue"] = fileResidue == 0,
            ["probe_session_residue_count"] = sessionResidue,
            ["zero_probe_sessions_after_pool_clear"] = sessionResidue == 0,
            ["clear_all_pools_called"] = true,
            ["cleanup_error_count"] = errors.Count,
            ["cleanup_errors"] = errors
        };
    }

    private static async Task CaptureDatabaseFilesAsync(string masterConnectionString, string database)
    {
        await using SqlConnection connection = new(masterConnectionString);
        await connection.OpenAsync();
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT physical_name
            FROM sys.master_files
            WHERE database_id = DB_ID(@DatabaseName);
            """;
        command.Parameters.AddWithValue("@DatabaseName", database);
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            DisposableFiles.Add(reader.GetString(0));
        }
    }

    private static async Task DropDatabaseAsync(string masterConnectionString, string database)
    {
        if (!database.StartsWith(DatabasePrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Refusing to drop a database outside the QA disposable prefix.");
        }

        string quoted = "[" + database.Replace("]", "]]", StringComparison.Ordinal) + "]";
        await using SqlConnection connection = new(masterConnectionString);
        await connection.OpenAsync();
        await using SqlCommand command = connection.CreateCommand();
        command.CommandTimeout = 30;
        command.CommandText = $"""
            IF DB_ID(@DatabaseName) IS NOT NULL
            BEGIN
                ALTER DATABASE {quoted} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE {quoted};
            END;
            """;
        command.Parameters.AddWithValue("@DatabaseName", database);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<int> ExecuteScalarIntAsync(SqlConnection connection, string sql)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandTimeout = 10;
        command.CommandText = sql;
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static bool IsExclusive(string mode) => string.Equals(mode, "Exclusive", StringComparison.OrdinalIgnoreCase);

    private static bool IsTrue(IReadOnlyDictionary<string, object?> values, string key)
        => values.TryGetValue(key, out object? value) && value is true;

    private static bool IsSql51057(IReadOnlyDictionary<string, object?> values)
        => values.TryGetValue("sql_error_number", out object? value) && value is int number && number == 51057;

    private static bool IsExclusiveFromInspection(IReadOnlyDictionary<string, object?> values)
        => IsTrue(values, "exclusive");

    private static SqlException? FindSqlException(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException sql)
            {
                return sql;
            }
        }
        return null;
    }

    private static Dictionary<string, object?> SanitizeException(Exception exception)
    {
        SqlException? sql = FindSqlException(exception);
        return new Dictionary<string, object?>
        {
            ["exception_type"] = exception.GetType().Name,
            ["probe_step"] = exception is ProbeStepException probe ? probe.Step : null,
            ["sql_error_number"] = sql?.Number,
            ["sql_error_numbers"] = sql?.Errors.Cast<SqlError>().Select(error => error.Number).ToArray()
        };
    }

    private sealed class ProbeStepException(string step, Exception innerException)
        : Exception("QA probe step failed.", innerException)
    {
        public string Step { get; } = step;
    }
}
