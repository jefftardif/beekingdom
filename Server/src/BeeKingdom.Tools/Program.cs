using System.Data;
using BeeKingdom.Infrastructure.DependencyInjection;
using BeeKingdom.Persistence.DependencyInjection;
using BeeKingdom.Persistence.Migrations;
using BeeKingdom.Persistence.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddBeeKingdomInfrastructure(builder.Configuration)
    .AddBeeKingdomPersistence(builder.Configuration);

using IHost host = builder.Build();

string command = args.Length > 0 ? args[0].ToLowerInvariant() : "diagnostics";

switch (command)
{
    case "migrate":
        await host.Services.GetRequiredService<IMigrationRunner>().ApplyPendingMigrationsAsync();
        Console.WriteLine("Migration scripts registered.");
        break;

    case "diagnostics":
        IReadOnlyList<string> pending = await host.Services.GetRequiredService<IMigrationRunner>().GetPendingMigrationsAsync();
        Console.WriteLine($"Bee Kingdom server tools ready. Registered migrations: {pending.Count}.");
        foreach (string name in pending) Console.WriteLine("  pending: " + name);
        break;

    case "probe-google-account":
        await ProbeGoogleAccountAsync(host.Services.GetRequiredService<SqlConnectionFactory>());
        break;

    case "list-schema-version":
        await ListSchemaVersionAsync(host.Services.GetRequiredService<SqlConnectionFactory>());
        break;

    case "check-role-column":
        await CheckRoleColumnAsync(host.Services.GetRequiredService<SqlConnectionFactory>());
        break;

    case "revoke-google-sessions":
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: revoke-google-sessions <account-email-or-google-subject-substring>");
            Environment.ExitCode = 2;
            break;
        }
        await RevokeGoogleSessionsAsync(host.Services.GetRequiredService<SqlConnectionFactory>(), args[1]);
        break;

    default:
        Console.Error.WriteLine($"Unknown command: {command}");
        Environment.ExitCode = 2;
        break;
}

static async Task ListSchemaVersionAsync(SqlConnectionFactory connectionFactory)
{
    try
    {
        Console.WriteLine("RuntimeConnectionString: " + connectionFactory.GetRuntimeConnectionString());
        Console.WriteLine("MigrationConnectionString: " + connectionFactory.GetMigrationConnectionString());
        await using SqlConnection connection = connectionFactory.CreateMigrationConnection();
        await connection.OpenAsync();
        Console.WriteLine("Connected. Database=" + connection.Database + " DataSource=" + connection.DataSource);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT ScriptName, AppliedAtUtc FROM dbo.SchemaVersion ORDER BY Id;";
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        int i = 0;
        while (await reader.ReadAsync())
        {
            Console.WriteLine($"  [{i}] {reader.GetString(0)} applied={reader.GetDateTime(1):O}");
            i++;
        }
        Console.WriteLine("Total applied rows: " + i);
    }
    catch (Exception exception)
    {
        Console.WriteLine("FAILED: " + exception);
    }
}

static async Task RevokeGoogleSessionsAsync(SqlConnectionFactory connectionFactory, string emailMatch)
{
    try
    {
        await using SqlConnection connection = connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using SqlCommand find = connection.CreateCommand();
        find.CommandText = "SELECT AccountId, Email FROM dbo.AuthenticationAccounts WHERE Email LIKE @Match;";
        find.Parameters.Add(new SqlParameter("@Match", "%" + emailMatch + "%"));
        var accountIds = new List<Guid>();
        await using (SqlDataReader reader = await find.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                accountIds.Add(reader.GetGuid(0));
                Console.WriteLine("Matched account: " + reader.GetGuid(0) + " " + reader.GetString(1));
            }
        }

        if (accountIds.Count == 0)
        {
            Console.WriteLine("No matching account found.");
            return;
        }

        foreach (Guid accountId in accountIds)
        {
            await using SqlCommand revoke = connection.CreateCommand();
            revoke.CommandText = "UPDATE dbo.AuthenticationSessions SET IsRevoked = 1 WHERE AccountId = @AccountId AND IsRevoked = 0;";
            revoke.Parameters.Add(new SqlParameter("@AccountId", accountId));
            int affected = await revoke.ExecuteNonQueryAsync();
            Console.WriteLine($"Revoked {affected} active session(s) for account {accountId}.");
        }
    }
    catch (Exception exception)
    {
        Console.WriteLine("FAILED: " + exception);
    }
}

static async Task CheckRoleColumnAsync(SqlConnectionFactory connectionFactory)
{
    try
    {
        await using SqlConnection connection = connectionFactory.CreateConnection();
        await connection.OpenAsync();
        Console.WriteLine("Connected via RUNTIME connection. Database=" + connection.Database + " DataSource=" + connection.DataSource);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.AuthenticationAccounts') ORDER BY column_id;";
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            Console.WriteLine("  column: " + reader.GetString(0));
        }
    }
    catch (Exception exception)
    {
        Console.WriteLine("FAILED: " + exception);
    }
}

// Diagnostic-only: exercises the exact INSERT/MERGE statements used by the Google
// login path (SqlAccountCredentialStore.Insert + SqlAuthenticationSessionStore.Save)
// against the real production schema, inside a transaction that is ALWAYS rolled
// back, so it never leaves any trace in the database regardless of outcome. Used to
// isolate whether a SQL-layer defect (missing column, constraint, etc.) is the cause
// of the "auth.rejected" failure seen after a real Google sign-in, without needing to
// complete another live OAuth round-trip.
static async Task ProbeGoogleAccountAsync(SqlConnectionFactory connectionFactory)
{
    await using SqlConnection connection = connectionFactory.CreateConnection();
    await connection.OpenAsync();
    await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted);
    try
    {
        Guid accountId = Guid.NewGuid();
        Guid playerId = Guid.NewGuid();
        string googleSubjectId = "diagnostic-probe-" + Guid.NewGuid().ToString("N");
        string email = googleSubjectId + "@diagnostic.invalid";

        await using (SqlCommand insertAccount = connection.CreateCommand())
        {
            insertAccount.Transaction = transaction;
            insertAccount.CommandText = """
                INSERT INTO dbo.AuthenticationAccounts
                (AccountId, PlayerId, Email, PasswordHash, SecurityState, FailedAttempts, LockedUntilUtc, GoogleSubjectId, DisplayName, IsOnboarded, Role)
                VALUES
                (@AccountId, @PlayerId, @Email, @PasswordHash, @SecurityState, @FailedAttempts, @LockedUntilUtc, @GoogleSubjectId, @DisplayName, @IsOnboarded, @Role);
                """;
            Add(insertAccount, "@AccountId", accountId);
            Add(insertAccount, "@PlayerId", playerId);
            Add(insertAccount, "@Email", email);
            Add(insertAccount, "@PasswordHash", DBNull.Value);
            Add(insertAccount, "@SecurityState", 0);
            Add(insertAccount, "@FailedAttempts", 0);
            Add(insertAccount, "@LockedUntilUtc", DBNull.Value);
            Add(insertAccount, "@GoogleSubjectId", googleSubjectId);
            Add(insertAccount, "@DisplayName", DBNull.Value);
            Add(insertAccount, "@IsOnboarded", false);
            Add(insertAccount, "@Role", 0);
            await insertAccount.ExecuteNonQueryAsync();
        }
        Console.WriteLine("Account insert: OK.");

        await using (SqlCommand insertSession = connection.CreateCommand())
        {
            insertSession.Transaction = transaction;
            insertSession.CommandText = """
                MERGE dbo.AuthenticationSessions AS target
                USING (SELECT @SessionId AS SessionId) AS source
                ON target.SessionId = source.SessionId
                WHEN MATCHED THEN
                    UPDATE SET AccountId = @AccountId,
                               PlayerId = @PlayerId,
                               AuthenticationProvider = @AuthenticationProvider,
                               LoginUtc = @LoginUtc,
                               LastActivityUtc = @LastActivityUtc,
                               ExpirationUtc = @ExpirationUtc,
                               ClientVersion = @ClientVersion,
                               IpAddress = @IpAddress,
                               DeviceIdentifier = @DeviceIdentifier,
                               Region = @Region,
                               IsRevoked = @IsRevoked
                WHEN NOT MATCHED THEN
                    INSERT (SessionId, AccountId, PlayerId, AuthenticationProvider, LoginUtc, LastActivityUtc, ExpirationUtc,
                            ClientVersion, IpAddress, DeviceIdentifier, Region, IsRevoked)
                    VALUES (@SessionId, @AccountId, @PlayerId, @AuthenticationProvider, @LoginUtc, @LastActivityUtc, @ExpirationUtc,
                            @ClientVersion, @IpAddress, @DeviceIdentifier, @Region, @IsRevoked);
                """;
            DateTime now = DateTime.UtcNow;
            Add(insertSession, "@SessionId", Guid.NewGuid().ToString("N"));
            Add(insertSession, "@AccountId", accountId);
            Add(insertSession, "@PlayerId", playerId);
            Add(insertSession, "@AuthenticationProvider", 1);
            Add(insertSession, "@LoginUtc", now);
            Add(insertSession, "@LastActivityUtc", now);
            Add(insertSession, "@ExpirationUtc", now.AddDays(30));
            Add(insertSession, "@ClientVersion", "diagnostic-probe");
            Add(insertSession, "@IpAddress", "127.0.0.1");
            Add(insertSession, "@DeviceIdentifier", "diagnostic-probe-device");
            Add(insertSession, "@Region", "ca-east");
            Add(insertSession, "@IsRevoked", false);
            await insertSession.ExecuteNonQueryAsync();
        }
        Console.WriteLine("Session insert: OK.");
        Console.WriteLine("PROBE PASSED: both writes succeeded against the real schema. Rolling back (no data kept).");
    }
    catch (Exception exception)
    {
        Console.WriteLine("PROBE FAILED: " + exception.GetType().FullName + ": " + exception.Message);
        Console.WriteLine(exception.ToString());
    }
    finally
    {
        await transaction.RollbackAsync();
    }

    static void Add(SqlCommand command, string name, object value)
    {
        SqlParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
