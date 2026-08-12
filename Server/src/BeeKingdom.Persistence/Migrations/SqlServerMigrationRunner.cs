using System.Data;
using BeeKingdom.Database;
using BeeKingdom.Persistence.Configuration;
using BeeKingdom.Persistence.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Persistence.Migrations;

public sealed class SqlServerMigrationRunner : IMigrationRunner
{
    private const string SchemaVersionTable = "dbo.SchemaVersion";
    private const string MigrationLockResource = "BeeKingdom.Database.Migrations";
    private const string DatabaseCreationLockPrefix = "BeeKingdom.Database.Create:";

    private readonly SqlConnectionFactory connectionFactory;
    private readonly SqlServerOptions options;
    private readonly ILogger<SqlServerMigrationRunner> logger;
    private readonly MigrationDiagnostics diagnostics;

    public SqlServerMigrationRunner(
        SqlConnectionFactory connectionFactory,
        IOptions<SqlServerOptions> options,
        ILogger<SqlServerMigrationRunner> logger,
        MigrationDiagnostics diagnostics)
    {
        this.connectionFactory = connectionFactory;
        this.options = options.Value;
        this.logger = logger;
        this.diagnostics = diagnostics;
    }

    public async Task<IReadOnlyList<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default)
    {
        diagnostics.RecordPendingCheck();
        try
        {
            await EnsureDatabaseAsync(cancellationToken);
            await using SqlConnection connection = connectionFactory.CreateMigrationConnection();
            await connection.OpenAsync(cancellationToken);
            return await ExecuteWithSessionLockAsync(
                connection,
                MigrationLockResource,
                async token =>
                {
                    await EnsureSchemaVersionTableAsync(connection, token);
                    HashSet<string> applied = await GetAppliedMigrationsAsync(connection, token);
                    return (IReadOnlyList<string>)DatabaseCatalog.Migrations
                        .Where(script => !applied.Contains(script.Name))
                        .Select(script => script.Name)
                        .ToArray();
                },
                cancellationToken);
        }
        catch (Exception exception) when (exception is SqlException or InvalidOperationException)
        {
            diagnostics.RecordFailure(exception);
            throw;
        }
    }

    public async Task ApplyPendingMigrationsAsync(CancellationToken cancellationToken = default)
    {
        diagnostics.RecordApplyAttempt();
        try
        {
            await EnsureDatabaseAsync(cancellationToken);
            await using SqlConnection connection = connectionFactory.CreateMigrationConnection();
            await connection.OpenAsync(cancellationToken);
            await ExecuteWithSessionLockAsync(
                connection,
                MigrationLockResource,
                async token =>
                {
                    await EnsureSchemaVersionTableAsync(connection, token);

                    HashSet<string> applied = await GetAppliedMigrationsAsync(connection, token);
                    foreach (DatabaseScript script in DatabaseCatalog.Migrations.Where(script => !applied.Contains(script.Name)))
                    {
                        token.ThrowIfCancellationRequested();
                        logger.LogInformation("Applying SQL migration {ScriptName}", script.Name);

                        await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, token);
                        await ExecuteAsync(connection, transaction, script.Sql, token);
                        await MarkAppliedAsync(connection, transaction, script.Name, token);
                        await transaction.CommitAsync(token);
                        diagnostics.RecordScriptApplied(script.Name);
                    }

                    return true;
                },
                cancellationToken);
        }
        catch (Exception exception) when (exception is SqlException or InvalidOperationException)
        {
            diagnostics.RecordFailure(exception);
            throw;
        }
    }

    private async Task EnsureDatabaseAsync(CancellationToken cancellationToken)
    {
        string databaseName = connectionFactory.GetDatabaseName();
        await using SqlConnection connection = connectionFactory.CreateMasterConnection();
        await connection.OpenAsync(cancellationToken);
        await ExecuteWithSessionLockAsync(
            connection,
            DatabaseCreationLockPrefix + databaseName,
            async token =>
            {
                string escapedDatabaseName = databaseName.Replace("]", "]]", StringComparison.Ordinal);
                string escapedLiteral = databaseName.Replace("'", "''", StringComparison.Ordinal);
                await using SqlCommand command = connection.CreateCommand();
                command.CommandTimeout = options.CommandTimeoutSeconds;
                command.CommandText = $"""
                    IF DB_ID(N'{escapedLiteral}') IS NULL
                    BEGIN
                        CREATE DATABASE [{escapedDatabaseName}];
                    END
                    """;

                await command.ExecuteNonQueryAsync(token);
                return true;
            },
            cancellationToken);
    }

    private async Task EnsureSchemaVersionTableAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        DatabaseScript script = DatabaseCatalog.Migrations.First(migration => migration.Name == "010_schema_version.sql");
        await ExecuteAsync(connection, null, script.Sql, cancellationToken);
    }

    private async Task<TResult> ExecuteWithSessionLockAsync<TResult>(
        SqlConnection connection,
        string resource,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        try
        {
            await AcquireSessionLockAsync(connection, resource, cancellationToken);
        }
        catch (Exception exception)
        {
            await DiscardPooledConnectionAsync(connection, resource, "acquisition", exception);
            throw;
        }

        Exception? operationException = null;
        try
        {
            return await operation(cancellationToken);
        }
        catch (Exception exception)
        {
            operationException = exception;
            throw;
        }
        finally
        {
            try
            {
                await ReleaseSessionLockAsync(connection, resource);
            }
            catch (Exception releaseException)
            {
                await DiscardPooledConnectionAsync(connection, resource, "release", releaseException);
                if (operationException is null)
                {
                    throw;
                }

                logger.LogError(
                    releaseException,
                    "SQL session lock {Resource} could not be released while handling {OperationExceptionType}; the physical connection was discarded.",
                    resource,
                    operationException.GetType().Name);
            }
        }
    }

    private async Task AcquireSessionLockAsync(SqlConnection connection, string resource, CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandTimeout = options.CommandTimeoutSeconds;
        command.CommandText = """
            DECLARE @LockResult int;
            EXEC @LockResult = sys.sp_getapplock
                @Resource = @Resource,
                @LockMode = N'Exclusive',
                @LockOwner = N'Session',
                @LockTimeout = @LockTimeoutMilliseconds;

            IF @LockResult < 0
            BEGIN
                THROW 51057, 'Could not acquire the Bee Kingdom SQL session lock.', 1;
            END;
            """;
        command.Parameters.AddWithValue("@Resource", resource);
        command.Parameters.AddWithValue("@LockTimeoutMilliseconds", options.CommandTimeoutSeconds * 1000);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task ReleaseSessionLockAsync(SqlConnection connection, string resource)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandTimeout = options.CommandTimeoutSeconds;
        command.CommandText = """
            DECLARE @LockResult int;
            EXEC @LockResult = sys.sp_releaseapplock
                @Resource = @Resource,
                @LockOwner = N'Session';

            IF @LockResult < 0
            BEGIN
                THROW 51058, 'Could not release the Bee Kingdom SQL session lock.', 1;
            END;
            """;
        command.Parameters.AddWithValue("@Resource", resource);
        await command.ExecuteNonQueryAsync(CancellationToken.None);
    }

    private async Task DiscardPooledConnectionAsync(
        SqlConnection connection,
        string resource,
        string phase,
        Exception cause)
    {
        bool poolCleared = false;
        try
        {
            SqlConnection.ClearPool(connection);
            poolCleared = true;
        }
        catch (Exception clearException)
        {
            logger.LogError(clearException, "Could not clear the SQL pool after session lock {Phase} failure for {Resource}.", phase, resource);
        }

        if (!poolCleared)
        {
            try
            {
                SqlConnection.ClearAllPools();
            }
            catch (Exception clearAllException)
            {
                logger.LogError(clearAllException, "Could not clear all SQL pools after session lock {Phase} failure for {Resource}.", phase, resource);
            }
        }

        try
        {
            await connection.CloseAsync();
        }
        catch (Exception closeException)
        {
            logger.LogError(closeException, "Could not close the SQL connection after session lock {Phase} failure for {Resource}.", phase, resource);
        }

        logger.LogWarning(cause, "Discarded a pooled SQL connection after session lock {Phase} failure for {Resource}.", phase, resource);
    }

    private async Task<HashSet<string>> GetAppliedMigrationsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandTimeout = options.CommandTimeoutSeconds;
        command.CommandText = $"SELECT ScriptName FROM {SchemaVersionTable};";

        HashSet<string> applied = new(StringComparer.OrdinalIgnoreCase);
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            applied.Add(reader.GetString(0));
        }

        return applied;
    }

    private async Task ExecuteAsync(SqlConnection connection, SqlTransaction? transaction, string sql, CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandTimeout = options.CommandTimeoutSeconds;
        command.Transaction = transaction;
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task MarkAppliedAsync(SqlConnection connection, SqlTransaction transaction, string scriptName, CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandTimeout = options.CommandTimeoutSeconds;
        command.Transaction = transaction;
        command.CommandText = $"INSERT INTO {SchemaVersionTable} (ScriptName) VALUES (@ScriptName);";
        command.Parameters.AddWithValue("@ScriptName", scriptName);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
