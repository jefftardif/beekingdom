using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using BeeKingdom.Persistence.Configuration;
using BeeKingdom.Persistence.Migrations;
using BeeKingdom.Persistence.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

internal static class Program
{
    private const string DataSource = @"(localdb)\MSSQLLocalDB";
    private const string ApplicationPrefix = "BeeKingdom.QA.SERVERB061.";
    private static readonly MethodInfo ExecuteWithSessionLockMethod = typeof(SqlServerMigrationRunner)
        .GetMethod("ExecuteWithSessionLockAsync", BindingFlags.Instance | BindingFlags.NonPublic)
        ?.MakeGenericMethod(typeof(bool))
        ?? throw new InvalidOperationException("ExecuteWithSessionLockAsync was not found.");

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
            ["proof_id"] = "QA-B-SERVER-B-061-SQL-APPLOCK-LIFECYCLE",
            ["generated_utc"] = DateTimeOffset.UtcNow,
            ["scope"] = "LocalDB only; no producer-code mutation; no remote target",
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

            phase = "distinct_sessions_success_release";
            Dictionary<string, object?> success = await RunSuccessfulLifecycleAsync(runId);
            evidence[phase] = success;

            phase = "ambiguous_acquisition_eviction";
            Dictionary<string, object?> acquisition = await RunAmbiguousAcquisitionAsync(runId);
            evidence[phase] = acquisition;

            phase = "ambiguous_release_primary_error";
            Dictionary<string, object?> releasePrimary = await RunAmbiguousReleaseAsync(runId, operationThrows: false);
            evidence[phase] = releasePrimary;

            phase = "ambiguous_release_preserves_operation_error";
            Dictionary<string, object?> releaseSecondary = await RunAmbiguousReleaseAsync(runId, operationThrows: true);
            evidence[phase] = releaseSecondary;

            bool pass = IsTrue(success, "pass")
                && IsTrue(acquisition, "pass")
                && IsTrue(releasePrimary, "pass")
                && IsTrue(releaseSecondary, "pass");
            evidence["assessment"] = new Dictionary<string, object?>
            {
                ["same_resource_two_distinct_spids_confirmed"] = IsTrue(success, "distinct_spids"),
                ["second_session_acquires_after_release"] = IsTrue(success, "acquires_after_release"),
                ["no_lock_after_close_and_pool_reuse"] = IsTrue(success, "no_lock_after_close_and_pool_reuse"),
                ["acquisition_failure_pool_evicted"] = IsTrue(acquisition, "pool_evicted"),
                ["release_failure_pool_evicted"] = IsTrue(releasePrimary, "pool_evicted"),
                ["release_error_propagated_when_primary"] = IsTrue(releasePrimary, "release_error_propagated"),
                ["operation_error_preserved_when_release_also_fails"] = IsTrue(releaseSecondary, "operation_error_preserved"),
                ["pass"] = pass
            };
        }
        catch (Exception exception)
        {
            fatal = exception;
            evidence["fatal_phase"] = phase;
            evidence["fatal_error"] = ExceptionEvidence(exception);
        }
        finally
        {
            evidence["cleanup"] = await CleanupAsync();
            evidence["completed"] = fatal is null;
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(
                evidence,
                new JsonSerializerOptions { WriteIndented = true }));
        }

        return fatal is null ? 0 : 2;
    }

    private static async Task<Dictionary<string, object?>> RunSuccessfulLifecycleAsync(string runId)
    {
        SqlConnection.ClearAllPools();
        string resource = $"BeeKingdom.QA.SERVERB061.Success.{runId}";
        string ownerConnectionString = BuildConnectionString($"{runId}.Success.Owner");
        string verifierConnectionString = BuildConnectionString($"{runId}.Success.Verifier");

        await using SqlConnection owner = new(ownerConnectionString);
        await using SqlConnection verifier = new(verifierConnectionString);
        await Task.WhenAll(owner.OpenAsync(), verifier.OpenAsync());

        int ownerSpid = await GetSpidAsync(owner);
        int verifierSpid = await GetSpidAsync(verifier);
        Guid ownerConnectionId = owner.ClientConnectionId;
        int ownerAcquire = await AcquireAsync(owner, resource, 0);
        int verifierWhileHeld = await AcquireAsync(verifier, resource, 0);
        int ownerRelease = await ReleaseAsync(owner, resource);

        Stopwatch afterReleaseWatch = Stopwatch.StartNew();
        int verifierAfterRelease = await AcquireAsync(verifier, resource, 2_000);
        afterReleaseWatch.Stop();
        int verifierRelease = verifierAfterRelease >= 0 ? await ReleaseAsync(verifier, resource) : int.MinValue;

        await owner.CloseAsync();
        await using SqlConnection reusedOwner = new(ownerConnectionString);
        await reusedOwner.OpenAsync();
        int reusedOwnerSpid = await GetSpidAsync(reusedOwner);
        Guid reusedConnectionId = reusedOwner.ClientConnectionId;
        string modeAfterReuse = await GetModeAsync(reusedOwner, resource);
        await reusedOwner.CloseAsync();

        int verifierAfterReuse = await AcquireAsync(verifier, resource, 2_000);
        int verifierAfterReuseRelease = verifierAfterReuse >= 0
            ? await ReleaseAsync(verifier, resource)
            : int.MinValue;

        bool distinctSpids = ownerSpid != verifierSpid;
        bool acquiresAfterRelease = verifierAfterRelease >= 0 && afterReleaseWatch.ElapsedMilliseconds < 1_000;
        bool noLockAfterReuse = !string.Equals(modeAfterReuse, "Exclusive", StringComparison.OrdinalIgnoreCase)
            && verifierAfterReuse >= 0;

        return new Dictionary<string, object?>
        {
            ["pooling_effective"] = new SqlConnectionStringBuilder(ownerConnectionString).Pooling,
            ["owner_spid"] = ownerSpid,
            ["verifier_spid"] = verifierSpid,
            ["distinct_spids"] = distinctSpids,
            ["owner_acquire_result"] = ownerAcquire,
            ["verifier_while_held_result"] = verifierWhileHeld,
            ["owner_release_result"] = ownerRelease,
            ["verifier_after_release_result"] = verifierAfterRelease,
            ["verifier_after_release_wait_milliseconds"] = afterReleaseWatch.ElapsedMilliseconds,
            ["verifier_after_release_release_result"] = verifierRelease,
            ["owner_connection_id"] = ownerConnectionId,
            ["reused_owner_spid"] = reusedOwnerSpid,
            ["reused_owner_connection_id"] = reusedConnectionId,
            ["same_physical_connection_reused"] = ownerConnectionId == reusedConnectionId,
            ["mode_after_close_and_pool_reuse"] = modeAfterReuse,
            ["verifier_after_pool_reuse_result"] = verifierAfterReuse,
            ["verifier_after_pool_reuse_release_result"] = verifierAfterReuseRelease,
            ["acquires_after_release"] = acquiresAfterRelease,
            ["no_lock_after_close_and_pool_reuse"] = noLockAfterReuse,
            ["pass"] = new SqlConnectionStringBuilder(ownerConnectionString).Pooling
                && distinctSpids
                && ownerAcquire >= 0
                && verifierWhileHeld == -1
                && ownerRelease >= 0
                && acquiresAfterRelease
                && verifierRelease >= 0
                && noLockAfterReuse
                && verifierAfterReuseRelease >= 0
        };
    }

    private static async Task<Dictionary<string, object?>> RunAmbiguousAcquisitionAsync(string runId)
    {
        SqlConnection.ClearAllPools();
        string resource = $"BeeKingdom.QA.SERVERB061.AcquireAmbiguous.{runId}";
        string ownerConnectionString = BuildConnectionString($"{runId}.Acquire.Owner");
        string victimConnectionString = BuildConnectionString($"{runId}.Acquire.Victim");
        string verifierConnectionString = BuildConnectionString($"{runId}.Acquire.Verifier");
        CaptureLogger logger = new();
        SqlServerMigrationRunner runner = BuildRunner(victimConnectionString, logger);

        await using SqlConnection owner = new(ownerConnectionString);
        await using SqlConnection victim = new(victimConnectionString);
        await using SqlConnection verifier = new(verifierConnectionString);
        await Task.WhenAll(owner.OpenAsync(), victim.OpenAsync(), verifier.OpenAsync());

        int ownerSpid = await GetSpidAsync(owner);
        int victimSpid = await GetSpidAsync(victim);
        int verifierSpid = await GetSpidAsync(verifier);
        Guid victimConnectionId = victim.ClientConnectionId;
        int ownerAcquire = await AcquireAsync(owner, resource, 0);

        using CancellationTokenSource cancellation = new();
        Task<bool> operation = InvokeWithSessionLockAsync(
            runner,
            victim,
            resource,
            _ => Task.FromResult(true),
            cancellation.Token);
        await Task.Delay(200);
        bool waitingBeforeCancellation = !operation.IsCompleted;
        cancellation.Cancel();
        Exception? observed = await CaptureExceptionAsync(operation);
        int ownerRelease = await ReleaseAsync(owner, resource);

        Stopwatch verifierWatch = Stopwatch.StartNew();
        int verifierAcquire = await AcquireAsync(verifier, resource, 2_000);
        verifierWatch.Stop();
        int verifierRelease = verifierAcquire >= 0 ? await ReleaseAsync(verifier, resource) : int.MinValue;

        await using SqlConnection replacement = new(victimConnectionString);
        await replacement.OpenAsync();
        Guid replacementConnectionId = replacement.ClientConnectionId;
        int replacementSpid = await GetSpidAsync(replacement);

        bool discardLogged = logger.Contains("acquisition", resource);
        bool poolEvicted = victim.State == System.Data.ConnectionState.Closed
            && victimConnectionId != replacementConnectionId
            && discardLogged;

        return new Dictionary<string, object?>
        {
            ["pooling_effective"] = new SqlConnectionStringBuilder(victimConnectionString).Pooling,
            ["owner_spid"] = ownerSpid,
            ["victim_spid"] = victimSpid,
            ["verifier_spid"] = verifierSpid,
            ["all_spids_distinct"] = new[] { ownerSpid, victimSpid, verifierSpid }.Distinct().Count() == 3,
            ["owner_acquire_result"] = ownerAcquire,
            ["waiting_before_cancellation"] = waitingBeforeCancellation,
            ["observed_error"] = ExceptionEvidence(observed),
            ["owner_release_result"] = ownerRelease,
            ["victim_state_after_failure"] = victim.State.ToString(),
            ["victim_connection_id"] = victimConnectionId,
            ["replacement_connection_id"] = replacementConnectionId,
            ["replacement_spid"] = replacementSpid,
            ["discard_path_logged"] = discardLogged,
            ["pool_evicted"] = poolEvicted,
            ["verifier_acquire_after_failure_result"] = verifierAcquire,
            ["verifier_wait_milliseconds"] = verifierWatch.ElapsedMilliseconds,
            ["verifier_release_result"] = verifierRelease,
            ["pass"] = ownerAcquire >= 0
                && waitingBeforeCancellation
                && observed is not null
                && ownerRelease >= 0
                && poolEvicted
                && verifierAcquire >= 0
                && verifierRelease >= 0
        };
    }

    private static async Task<Dictionary<string, object?>> RunAmbiguousReleaseAsync(
        string runId,
        bool operationThrows)
    {
        SqlConnection.ClearAllPools();
        string suffix = operationThrows ? "Secondary" : "Primary";
        string resource = $"BeeKingdom.QA.SERVERB061.Release{suffix}.{runId}";
        string victimConnectionString = BuildConnectionString($"{runId}.Release{suffix}.Victim");
        string verifierConnectionString = BuildConnectionString($"{runId}.Release{suffix}.Verifier");
        CaptureLogger logger = new();
        SqlServerMigrationRunner runner = BuildRunner(victimConnectionString, logger);

        await using SqlConnection victim = new(victimConnectionString);
        await using SqlConnection verifier = new(verifierConnectionString);
        await Task.WhenAll(victim.OpenAsync(), verifier.OpenAsync());
        int victimSpid = await GetSpidAsync(victim);
        int verifierSpid = await GetSpidAsync(verifier);
        Guid victimConnectionId = victim.ClientConnectionId;
        const string Sentinel = "QA-B operation sentinel";

        Task<bool> operation = InvokeWithSessionLockAsync(
            runner,
            victim,
            resource,
            async _ =>
            {
                await victim.CloseAsync();
                if (operationThrows)
                {
                    throw new ProbeOperationException(Sentinel);
                }

                return true;
            },
            CancellationToken.None);
        Exception? observed = await CaptureExceptionAsync(operation);

        Stopwatch verifierWatch = Stopwatch.StartNew();
        int verifierAcquire = await AcquireAsync(verifier, resource, 2_000);
        verifierWatch.Stop();
        int verifierRelease = verifierAcquire >= 0 ? await ReleaseAsync(verifier, resource) : int.MinValue;

        await using SqlConnection replacement = new(victimConnectionString);
        await replacement.OpenAsync();
        Guid replacementConnectionId = replacement.ClientConnectionId;
        int replacementSpid = await GetSpidAsync(replacement);

        bool releaseDiscardLogged = logger.Contains("release", resource);
        bool poolEvicted = victim.State == System.Data.ConnectionState.Closed
            && victimConnectionId != replacementConnectionId
            && releaseDiscardLogged;
        bool operationErrorPreserved = operationThrows
            && observed is ProbeOperationException
            && string.Equals(observed.Message, Sentinel, StringComparison.Ordinal);
        bool releaseErrorPropagated = !operationThrows
            && observed is not null
            && observed is not ProbeOperationException;

        return new Dictionary<string, object?>
        {
            ["operation_throws"] = operationThrows,
            ["pooling_effective"] = new SqlConnectionStringBuilder(victimConnectionString).Pooling,
            ["victim_spid"] = victimSpid,
            ["verifier_spid"] = verifierSpid,
            ["distinct_spids"] = victimSpid != verifierSpid,
            ["victim_connection_id"] = victimConnectionId,
            ["victim_state_after_failure"] = victim.State.ToString(),
            ["observed_error"] = ExceptionEvidence(observed),
            ["release_discard_path_logged"] = releaseDiscardLogged,
            ["verifier_acquire_before_owner_pool_reuse_result"] = verifierAcquire,
            ["verifier_wait_milliseconds"] = verifierWatch.ElapsedMilliseconds,
            ["verifier_release_result"] = verifierRelease,
            ["replacement_connection_id"] = replacementConnectionId,
            ["replacement_spid"] = replacementSpid,
            ["pool_evicted"] = poolEvicted,
            ["release_error_propagated"] = releaseErrorPropagated,
            ["operation_error_preserved"] = operationErrorPreserved,
            ["pass"] = victimSpid != verifierSpid
                && releaseDiscardLogged
                && poolEvicted
                && verifierAcquire >= 0
                && verifierRelease >= 0
                && (operationThrows ? operationErrorPreserved : releaseErrorPropagated)
        };
    }

    private static SqlServerMigrationRunner BuildRunner(string connectionString, CaptureLogger logger)
    {
        SqlServerOptions settings = new()
        {
            DatabaseName = "master",
            ConnectionString = connectionString,
            RuntimeConnectionString = connectionString,
            MigrationConnectionString = connectionString,
            CommandTimeoutSeconds = 5
        };
        IOptions<SqlServerOptions> options = Options.Create(settings);
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        SqlConnectionFactory factory = new(configuration, options);
        return new SqlServerMigrationRunner(factory, options, logger, new MigrationDiagnostics());
    }

    private static Task<bool> InvokeWithSessionLockAsync(
        SqlServerMigrationRunner runner,
        SqlConnection connection,
        string resource,
        Func<CancellationToken, Task<bool>> operation,
        CancellationToken cancellationToken)
    {
        try
        {
            return (Task<bool>)ExecuteWithSessionLockMethod.Invoke(
                runner,
                new object?[] { connection, resource, operation, cancellationToken })!;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            return Task.FromException<bool>(exception.InnerException);
        }
    }

    private static string BuildConnectionString(string applicationSuffix, bool pooling = true)
    {
        SqlConnectionStringBuilder builder = new()
        {
            DataSource = DataSource,
            InitialCatalog = "master",
            IntegratedSecurity = true,
            TrustServerCertificate = true,
            ConnectTimeout = 5,
            Pooling = pooling,
            ApplicationName = ApplicationPrefix + applicationSuffix
        };
        return builder.ConnectionString;
    }

    private static async Task VerifyLocalDbAsync()
    {
        await using SqlConnection connection = new(BuildConnectionString("Bootstrap", pooling: false));
        await connection.OpenAsync();
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT CONVERT(int, SERVERPROPERTY('IsLocalDB'));";
        if (Convert.ToInt32(await command.ExecuteScalarAsync()) != 1)
        {
            throw new InvalidOperationException("The QA probe did not connect to LocalDB.");
        }
    }

    private static async Task<int> AcquireAsync(SqlConnection connection, string resource, int timeoutMilliseconds)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandTimeout = 10;
        command.CommandText = """
            DECLARE @Result int;
            EXEC @Result = sys.sp_getapplock
                @Resource = @Resource,
                @LockMode = N'Exclusive',
                @LockOwner = N'Session',
                @LockTimeout = @Timeout;
            SELECT @Result;
            """;
        command.Parameters.AddWithValue("@Resource", resource);
        command.Parameters.AddWithValue("@Timeout", timeoutMilliseconds);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static async Task<int> ReleaseAsync(SqlConnection connection, string resource)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandTimeout = 10;
        command.CommandText = """
            DECLARE @Result int;
            EXEC @Result = sys.sp_releaseapplock
                @Resource = @Resource,
                @LockOwner = N'Session';
            SELECT @Result;
            """;
        command.Parameters.AddWithValue("@Resource", resource);
        return Convert.ToInt32(await command.ExecuteScalarAsync(CancellationToken.None));
    }

    private static async Task<int> GetSpidAsync(SqlConnection connection)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT @@SPID;";
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static async Task<string> GetModeAsync(SqlConnection connection, string resource)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT APPLOCK_MODE(N'public', @Resource, N'Session');";
        command.Parameters.AddWithValue("@Resource", resource);
        return Convert.ToString(await command.ExecuteScalarAsync()) ?? string.Empty;
    }

    private static async Task<Exception?> CaptureExceptionAsync(Task operation)
    {
        try
        {
            await operation.WaitAsync(TimeSpan.FromSeconds(12));
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private static async Task<Dictionary<string, object?>> CleanupAsync()
    {
        SqlConnection.ClearAllPools();
        await Task.Delay(100);
        int sessionResidue;
        await using (SqlConnection check = new(BuildConnectionString("Cleanup", pooling: false)))
        {
            await check.OpenAsync();
            await using SqlCommand command = check.CreateCommand();
            command.CommandText = """
                SELECT COUNT(*)
                FROM sys.dm_exec_sessions
                WHERE program_name LIKE N'BeeKingdom.QA.SERVERB061.%'
                  AND session_id <> @@SPID;
                """;
            sessionResidue = Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        SqlConnection.ClearAllPools();
        return new Dictionary<string, object?>
        {
            ["clear_all_pools_called"] = true,
            ["database_objects_created"] = 0,
            ["session_residue_count"] = sessionResidue,
            ["zero_session_residue"] = sessionResidue == 0
        };
    }

    private static Dictionary<string, object?>? ExceptionEvidence(Exception? exception)
    {
        if (exception is null)
        {
            return null;
        }

        return new Dictionary<string, object?>
        {
            ["type"] = exception.GetType().FullName,
            ["sql_error_number"] = exception is SqlException sqlException ? sqlException.Number : null,
            ["is_operation_sentinel"] = exception is ProbeOperationException
        };
    }

    private static bool IsTrue(IReadOnlyDictionary<string, object?> values, string key)
    {
        return values.TryGetValue(key, out object? value) && value is true;
    }

    private sealed class ProbeOperationException(string message) : Exception(message);

    private sealed class CaptureLogger : ILogger<SqlServerMigrationRunner>
    {
        private readonly List<string> messages = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            messages.Add(formatter(state, exception));
        }

        public bool Contains(string phase, string resource)
        {
            return messages.Any(message =>
                message.Contains(phase, StringComparison.OrdinalIgnoreCase)
                && message.Contains(resource, StringComparison.Ordinal));
        }
    }
}
