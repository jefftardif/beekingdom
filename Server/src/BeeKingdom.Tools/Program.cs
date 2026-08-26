using System.Data;
using System.Text.Json;
using BeeKingdom.HiveOperations;
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

    case "repair-squad-reservation":
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: repair-squad-reservation <hive-id-guid> [--apply]");
            Environment.ExitCode = 2;
            break;
        }
        await RepairSquadReservationAsync(host.Services.GetRequiredService<SqlConnectionFactory>(), args[1], args.Contains("--apply"));
        break;

    case "grant-resources":
        if (args.Length < 4 || !long.TryParse(args[3], out long resourceDelta))
        {
            Console.Error.WriteLine("Usage: grant-resources <account-email-substring> <resource-key: honey|pollen|wax> <delta> [--apply]");
            Environment.ExitCode = 2;
            break;
        }
        await GrantResourcesAsync(host.Services.GetRequiredService<SqlConnectionFactory>(), args[1], args[2], resourceDelta, args.Contains("--apply"));
        break;

    case "set-building-level":
        if (args.Length < 4 || !int.TryParse(args[3], out int buildingLevel))
        {
            Console.Error.WriteLine("Usage: set-building-level <account-email-substring> <building-key> <level> [--apply]");
            Environment.ExitCode = 2;
            break;
        }
        await SetBuildingLevelAsync(host.Services.GetRequiredService<SqlConnectionFactory>(), args[1], args[2], buildingLevel, args.Contains("--apply"));
        break;

    case "grant-recall-tokens":
        if (args.Length < 3 || !long.TryParse(args[2], out long recallDelta))
        {
            Console.Error.WriteLine("Usage: grant-recall-tokens <account-email-substring> <delta> [--apply]");
            Environment.ExitCode = 2;
            break;
        }
        await GrantRecallTokensAsync(host.Services.GetRequiredService<SqlConnectionFactory>(), args[1], recallDelta, args.Contains("--apply"));
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

// Fixe une ruche dont SquadReservation.Reserved depasse DoctrineRoster.Counts - invariant
// verifie par HiveStateMigrator.ToCurrent (ligne 46-47), viole par une session de tests
// automatises trop rapide le 2026-08-25 (Lancer/Reclamer en rafale sur un CombatPatrol reel).
// Lit le JSON BRUT (sans passer par ToCurrent, qui jetterait avant meme de pouvoir lire),
// ramene chaque famille reservee a ne pas depasser l'effectif reel, et efface la reservation
// (ReservationId=null) si tout retombe a 0 - seule combinaison valide selon le meme invariant.
// Sans --apply : affiche ce qui serait corrige, n'ecrit rien.
static async Task RepairSquadReservationAsync(SqlConnectionFactory connectionFactory, string hiveIdRaw, bool apply)
{
    if (!Guid.TryParse(hiveIdRaw, out Guid hiveId))
    {
        Console.Error.WriteLine("Invalid hive id: " + hiveIdRaw);
        Environment.ExitCode = 2;
        return;
    }

    JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);
    await using SqlConnection connection = connectionFactory.CreateConnection();
    await connection.OpenAsync();

    string? json = null;
    Guid playerId = Guid.Empty;
    await using (SqlCommand read = connection.CreateCommand())
    {
        read.CommandText = "SELECT PlayerId, StateJson FROM dbo.HivePlayerStates WHERE HiveId=@hiveId";
        read.Parameters.Add(new SqlParameter("@hiveId", SqlDbType.UniqueIdentifier) { Value = hiveId });
        await using SqlDataReader reader = await read.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            Console.Error.WriteLine("No HivePlayerStates row found for HiveId=" + hiveId);
            Environment.ExitCode = 1;
            return;
        }
        playerId = reader.GetGuid(0);
        json = reader.GetString(1);
    }

    PlayerHiveState state = JsonSerializer.Deserialize<PlayerHiveState>(json, jsonOptions)!;
    if (state.SquadReservation is null)
    {
        Console.WriteLine("No SquadReservation on this hive - nothing to repair.");
        return;
    }

    Dictionary<string, long> rosterCounts = state.DoctrineRoster?.Counts ?? new Dictionary<string, long>();
    Dictionary<string, long> reserved = state.SquadReservation.Reserved;
    var corrected = new Dictionary<string, long>(reserved.Comparer);
    bool changed = false;
    foreach ((string family, long count) in reserved)
    {
        long cap = rosterCounts.GetValueOrDefault(family);
        long clamped = Math.Min(count, cap);
        corrected[family] = clamped;
        if (clamped != count) changed = true;
        Console.WriteLine($"  {family}: reserved={count} roster={cap} -> {clamped}");
    }

    if (!changed)
    {
        Console.WriteLine("Reserved already within roster bounds - nothing to repair (the invalid state may be elsewhere).");
        return;
    }

    long correctedTotal = corrected.Values.Sum();
    string? correctedReservationId = correctedTotal > 0 ? state.SquadReservation.ReservationId : null;
    SquadReservationState correctedReservation = state.SquadReservation with { Reserved = corrected, ReservationId = correctedReservationId };
    PlayerHiveState correctedState = state with { SquadReservation = correctedReservation };

    // Sanity check locally before writing anything: the corrected state must actually pass the
    // same validation that is currently rejecting the stored one.
    try { HiveStateMigrator.ToCurrent(correctedState); }
    catch (Exception validationError)
    {
        Console.Error.WriteLine("Corrected state still fails validation, aborting without writing: " + validationError.Message);
        Environment.ExitCode = 1;
        return;
    }

    Console.WriteLine($"PlayerId={playerId} HiveId={hiveId}");
    Console.WriteLine(correctedReservationId is null
        ? "Reservation total dropped to 0 -> clearing ReservationId as well (required by the same invariant)."
        : "Reservation kept (total still > 0) with clamped per-family counts.");

    if (!apply)
    {
        Console.WriteLine("Dry run only - no write performed. Re-run with --apply to write this correction.");
        return;
    }

    string correctedJson = JsonSerializer.Serialize(correctedState, jsonOptions);
    await using (SqlCommand write = connection.CreateCommand())
    {
        write.CommandText = "UPDATE dbo.HivePlayerStates SET StateJson=@json, UpdatedAtUtc=SYSUTCDATETIME() WHERE PlayerId=@playerId AND HiveId=@hiveId";
        write.Parameters.Add(new SqlParameter("@json", SqlDbType.NVarChar, -1) { Value = correctedJson });
        write.Parameters.Add(new SqlParameter("@playerId", SqlDbType.UniqueIdentifier) { Value = playerId });
        write.Parameters.Add(new SqlParameter("@hiveId", SqlDbType.UniqueIdentifier) { Value = hiveId });
        int rows = await write.ExecuteNonQueryAsync();
        Console.WriteLine(rows == 1 ? "Applied: 1 row updated." : "Unexpected row count updated: " + rows);
    }
}

// Fixe le niveau d'un batiment (demande de Jeff, 2026-08-26 - augmenter drastiquement la capacite
// de population pour tester la Caserne passe par le niveau du batiment "nursery_cluster", seul
// levier reel qui gouverne CombatRecruitmentService.ComputePopulationCapacity ; aucun champ de
// capacite separe n'existe). Meme modele que les autres commandes grant-* : trouve le compte par
// email, lit le JSON BRUT, fixe le niveau (valeur absolue, pas un delta), valide via
// HiveStateMigrator.ToCurrent avant d'ecrire.
static async Task SetBuildingLevelAsync(SqlConnectionFactory connectionFactory, string emailMatch, string buildingKey, int level, bool apply)
{
    if (level < 0) { Console.Error.WriteLine("Level must be >= 0."); Environment.ExitCode = 2; return; }
    buildingKey = buildingKey.Trim();

    JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);
    await using SqlConnection connection = connectionFactory.CreateConnection();
    await connection.OpenAsync();

    Guid playerId = Guid.Empty;
    await using (SqlCommand find = connection.CreateCommand())
    {
        find.CommandText = "SELECT TOP 2 a.PlayerId, a.Email FROM dbo.AuthenticationAccounts a WHERE a.Email LIKE @Match;";
        find.Parameters.Add(new SqlParameter("@Match", "%" + emailMatch + "%"));
        var matches = new List<(Guid PlayerId, string Email)>();
        await using (SqlDataReader reader = await find.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync()) matches.Add((reader.GetGuid(0), reader.GetString(1)));
        }
        if (matches.Count == 0) { Console.Error.WriteLine("No matching account found."); Environment.ExitCode = 1; return; }
        if (matches.Count > 1) { Console.Error.WriteLine("More than one account matches - be more specific. Matches: " + string.Join(", ", matches.Select(m => m.Email))); Environment.ExitCode = 1; return; }
        playerId = matches[0].PlayerId;
        Console.WriteLine("Matched account: " + playerId + " " + matches[0].Email);
    }

    Guid hiveId = Guid.Empty;
    string? json = null;
    await using (SqlCommand read = connection.CreateCommand())
    {
        read.CommandText = "SELECT TOP 2 HiveId, StateJson FROM dbo.HivePlayerStates WHERE PlayerId=@playerId ORDER BY UpdatedAtUtc DESC;";
        read.Parameters.Add(new SqlParameter("@playerId", SqlDbType.UniqueIdentifier) { Value = playerId });
        var rows = new List<(Guid HiveId, string Json)>();
        await using (SqlDataReader reader = await read.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync()) rows.Add((reader.GetGuid(0), reader.GetString(1)));
        }
        if (rows.Count == 0) { Console.Error.WriteLine("No HivePlayerStates row found for PlayerId=" + playerId); Environment.ExitCode = 1; return; }
        if (rows.Count > 1) Console.WriteLine("Warning: multiple hives found for this player - using the most recently updated one.");
        hiveId = rows[0].HiveId;
        json = rows[0].Json;
    }

    PlayerHiveState state = JsonSerializer.Deserialize<PlayerHiveState>(json, jsonOptions)!;
    var buildingLevels = new Dictionary<string, int>(state.BuildingLevels ?? new Dictionary<string, int>(StringComparer.Ordinal), StringComparer.Ordinal);
    int before = buildingLevels.GetValueOrDefault(buildingKey);
    buildingLevels[buildingKey] = level;
    PlayerHiveState correctedState = state with { BuildingLevels = buildingLevels };

    try { HiveStateMigrator.ToCurrent(correctedState); }
    catch (Exception validationError)
    {
        Console.Error.WriteLine("Corrected state fails validation, aborting without writing: " + validationError.Message);
        Environment.ExitCode = 1;
        return;
    }

    Console.WriteLine($"PlayerId={playerId} HiveId={hiveId}");
    Console.WriteLine($"  {buildingKey}: level {before} -> {level}");

    if (!apply)
    {
        Console.WriteLine("Dry run only - no write performed. Re-run with --apply to write this change.");
        return;
    }

    string correctedJson = JsonSerializer.Serialize(correctedState, jsonOptions);
    await using (SqlCommand write = connection.CreateCommand())
    {
        write.CommandText = "UPDATE dbo.HivePlayerStates SET StateJson=@json, UpdatedAtUtc=SYSUTCDATETIME() WHERE PlayerId=@playerId AND HiveId=@hiveId";
        write.Parameters.Add(new SqlParameter("@json", SqlDbType.NVarChar, -1) { Value = correctedJson });
        write.Parameters.Add(new SqlParameter("@playerId", SqlDbType.UniqueIdentifier) { Value = playerId });
        write.Parameters.Add(new SqlParameter("@hiveId", SqlDbType.UniqueIdentifier) { Value = hiveId });
        int rows = await write.ExecuteNonQueryAsync();
        Console.WriteLine(rows == 1 ? "Applied: 1 row updated." : "Unexpected row count updated: " + rows);
    }
}

// Octroi manuel de miel/pollen/cire (demande de Jeff, 2026-08-26 - tester la formation de marche
// mixte necessite d'entrainer des Voltigeuses/Lanceuses, ce qui coute des ressources). Meme modele
// que grant-recall-tokens : trouve le compte par email, lit le JSON BRUT, ajoute le delta au solde
// (borne a la capacite existante, jamais negatif), valide via HiveStateMigrator.ToCurrent avant
// d'ecrire. Sans --apply : affiche ce qui serait fait, n'ecrit rien.
static async Task GrantResourcesAsync(SqlConnectionFactory connectionFactory, string emailMatch, string resourceKey, long delta, bool apply)
{
    resourceKey = resourceKey.Trim().ToLowerInvariant();
    if (resourceKey is not ("honey" or "pollen" or "wax"))
    {
        Console.Error.WriteLine("Unknown resource key: " + resourceKey + " (expected honey, pollen, or wax)");
        Environment.ExitCode = 2;
        return;
    }

    JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);
    await using SqlConnection connection = connectionFactory.CreateConnection();
    await connection.OpenAsync();

    Guid playerId = Guid.Empty;
    await using (SqlCommand find = connection.CreateCommand())
    {
        find.CommandText = "SELECT TOP 2 a.PlayerId, a.Email FROM dbo.AuthenticationAccounts a WHERE a.Email LIKE @Match;";
        find.Parameters.Add(new SqlParameter("@Match", "%" + emailMatch + "%"));
        var matches = new List<(Guid PlayerId, string Email)>();
        await using (SqlDataReader reader = await find.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync()) matches.Add((reader.GetGuid(0), reader.GetString(1)));
        }
        if (matches.Count == 0) { Console.Error.WriteLine("No matching account found."); Environment.ExitCode = 1; return; }
        if (matches.Count > 1) { Console.Error.WriteLine("More than one account matches - be more specific. Matches: " + string.Join(", ", matches.Select(m => m.Email))); Environment.ExitCode = 1; return; }
        playerId = matches[0].PlayerId;
        Console.WriteLine("Matched account: " + playerId + " " + matches[0].Email);
    }

    Guid hiveId = Guid.Empty;
    string? json = null;
    await using (SqlCommand read = connection.CreateCommand())
    {
        read.CommandText = "SELECT TOP 2 HiveId, StateJson FROM dbo.HivePlayerStates WHERE PlayerId=@playerId ORDER BY UpdatedAtUtc DESC;";
        read.Parameters.Add(new SqlParameter("@playerId", SqlDbType.UniqueIdentifier) { Value = playerId });
        var rows = new List<(Guid HiveId, string Json)>();
        await using (SqlDataReader reader = await read.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync()) rows.Add((reader.GetGuid(0), reader.GetString(1)));
        }
        if (rows.Count == 0) { Console.Error.WriteLine("No HivePlayerStates row found for PlayerId=" + playerId); Environment.ExitCode = 1; return; }
        if (rows.Count > 1) Console.WriteLine("Warning: multiple hives found for this player - using the most recently updated one.");
        hiveId = rows[0].HiveId;
        json = rows[0].Json;
    }

    PlayerHiveState state = JsonSerializer.Deserialize<PlayerHiveState>(json, jsonOptions)!;
    if (!state.Resources.TryGetValue(resourceKey, out ResourceBalance? balance))
    {
        Console.Error.WriteLine("Hive has no '" + resourceKey + "' balance entry - aborting.");
        Environment.ExitCode = 1;
        return;
    }
    long before = balance.Amount;
    long after = Math.Max(0, Math.Min(balance.Capacity, before + delta));
    var resources = new Dictionary<string, ResourceBalance>(state.Resources, StringComparer.Ordinal) { [resourceKey] = balance with { Amount = after } };
    PlayerHiveState correctedState = state with { Resources = resources };

    try { HiveStateMigrator.ToCurrent(correctedState); }
    catch (Exception validationError)
    {
        Console.Error.WriteLine("Corrected state fails validation, aborting without writing: " + validationError.Message);
        Environment.ExitCode = 1;
        return;
    }

    Console.WriteLine($"PlayerId={playerId} HiveId={hiveId}");
    Console.WriteLine($"  {resourceKey}: {before} -> {after} (delta {delta}, capacity {balance.Capacity})");

    if (!apply)
    {
        Console.WriteLine("Dry run only - no write performed. Re-run with --apply to write this change.");
        return;
    }

    string correctedJson = JsonSerializer.Serialize(correctedState, jsonOptions);
    await using (SqlCommand write = connection.CreateCommand())
    {
        write.CommandText = "UPDATE dbo.HivePlayerStates SET StateJson=@json, UpdatedAtUtc=SYSUTCDATETIME() WHERE PlayerId=@playerId AND HiveId=@hiveId";
        write.Parameters.Add(new SqlParameter("@json", SqlDbType.NVarChar, -1) { Value = correctedJson });
        write.Parameters.Add(new SqlParameter("@playerId", SqlDbType.UniqueIdentifier) { Value = playerId });
        write.Parameters.Add(new SqlParameter("@hiveId", SqlDbType.UniqueIdentifier) { Value = hiveId });
        int rows = await write.ExecuteNonQueryAsync();
        Console.WriteLine(rows == 1 ? "Applied: 1 row updated." : "Unexpected row count updated: " + rows);
    }
}

// Octroi manuel de jetons de rappel de patrouille (demande de Jeff, 2026-08-26) - seul moyen d'en
// obtenir en l'absence de boutique/systeme de quetes branche. Trouve le compte par email (comme
// revoke-google-sessions), lit le JSON BRUT de sa ruche, ajoute le delta au meme dictionnaire
// generique deja utilise par CombatPatrolService.RecallItemId (state.SpeedUps), valide via
// HiveStateMigrator.ToCurrent avant d'ecrire. Sans --apply : affiche ce qui serait fait, n'ecrit rien.
static async Task GrantRecallTokensAsync(SqlConnectionFactory connectionFactory, string emailMatch, long delta, bool apply)
{
    JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);
    await using SqlConnection connection = connectionFactory.CreateConnection();
    await connection.OpenAsync();

    Guid playerId = Guid.Empty;
    await using (SqlCommand find = connection.CreateCommand())
    {
        find.CommandText = "SELECT TOP 2 a.PlayerId, a.Email FROM dbo.AuthenticationAccounts a WHERE a.Email LIKE @Match;";
        find.Parameters.Add(new SqlParameter("@Match", "%" + emailMatch + "%"));
        var matches = new List<(Guid PlayerId, string Email)>();
        await using (SqlDataReader reader = await find.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync()) matches.Add((reader.GetGuid(0), reader.GetString(1)));
        }
        if (matches.Count == 0) { Console.Error.WriteLine("No matching account found."); Environment.ExitCode = 1; return; }
        if (matches.Count > 1) { Console.Error.WriteLine("More than one account matches - be more specific. Matches: " + string.Join(", ", matches.Select(m => m.Email))); Environment.ExitCode = 1; return; }
        playerId = matches[0].PlayerId;
        Console.WriteLine("Matched account: " + playerId + " " + matches[0].Email);
    }

    Guid hiveId = Guid.Empty;
    string? json = null;
    await using (SqlCommand read = connection.CreateCommand())
    {
        read.CommandText = "SELECT TOP 2 HiveId, StateJson FROM dbo.HivePlayerStates WHERE PlayerId=@playerId ORDER BY UpdatedAtUtc DESC;";
        read.Parameters.Add(new SqlParameter("@playerId", SqlDbType.UniqueIdentifier) { Value = playerId });
        var rows = new List<(Guid HiveId, string Json)>();
        await using (SqlDataReader reader = await read.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync()) rows.Add((reader.GetGuid(0), reader.GetString(1)));
        }
        if (rows.Count == 0) { Console.Error.WriteLine("No HivePlayerStates row found for PlayerId=" + playerId); Environment.ExitCode = 1; return; }
        if (rows.Count > 1) Console.WriteLine("Warning: multiple hives found for this player - using the most recently updated one.");
        hiveId = rows[0].HiveId;
        json = rows[0].Json;
    }

    PlayerHiveState state = JsonSerializer.Deserialize<PlayerHiveState>(json, jsonOptions)!;
    var items = new Dictionary<string, int>(state.SpeedUps ?? new Dictionary<string, int>(StringComparer.Ordinal), StringComparer.Ordinal);
    long before = items.GetValueOrDefault(CombatPatrolService.RecallItemId);
    long after = Math.Max(0, before + delta);
    items[CombatPatrolService.RecallItemId] = (int)Math.Min(after, int.MaxValue);
    PlayerHiveState correctedState = state with { SpeedUps = items };

    try { HiveStateMigrator.ToCurrent(correctedState); }
    catch (Exception validationError)
    {
        Console.Error.WriteLine("Corrected state fails validation, aborting without writing: " + validationError.Message);
        Environment.ExitCode = 1;
        return;
    }

    Console.WriteLine($"PlayerId={playerId} HiveId={hiveId}");
    Console.WriteLine($"  recall tokens: {before} -> {after} (delta {delta})");

    if (!apply)
    {
        Console.WriteLine("Dry run only - no write performed. Re-run with --apply to write this change.");
        return;
    }

    string correctedJson = JsonSerializer.Serialize(correctedState, jsonOptions);
    await using (SqlCommand write = connection.CreateCommand())
    {
        write.CommandText = "UPDATE dbo.HivePlayerStates SET StateJson=@json, UpdatedAtUtc=SYSUTCDATETIME() WHERE PlayerId=@playerId AND HiveId=@hiveId";
        write.Parameters.Add(new SqlParameter("@json", SqlDbType.NVarChar, -1) { Value = correctedJson });
        write.Parameters.Add(new SqlParameter("@playerId", SqlDbType.UniqueIdentifier) { Value = playerId });
        write.Parameters.Add(new SqlParameter("@hiveId", SqlDbType.UniqueIdentifier) { Value = hiveId });
        int rows = await write.ExecuteNonQueryAsync();
        Console.WriteLine(rows == 1 ? "Applied: 1 row updated." : "Unexpected row count updated: " + rows);
    }
}
