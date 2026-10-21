using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace BeeKingdom.HiveOperations;

/// <summary>
/// Only supported mutation backend. Mirrors the canonical SQL hive lock
/// ("hive:{player:N}:{hive:N}") and floral lock ("floral:{world}:{site}")
/// before modifying ANY economy, civilian or ecological balance. All writes
/// and the unique contribution ledger share one SERIALIZABLE SQL transaction.
/// </summary>
public sealed class SqlCivilExpeditionUnitOfWork(
    string connectionString, int commandTimeoutSeconds = 15)
    : ICivilExpeditionUnitOfWork, ICivilExpeditionDueIndex
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public async Task<CivilExpeditionMutation> ExecuteAsync(
        Guid playerId, Guid hiveId, FloralSiteDefinition definition,
        string catalogVersion,
        Func<PlayerHiveState, FloralSiteState, CivilExpeditionMutation> change,
        CancellationToken ct = default)
    {
        if (playerId == Guid.Empty || hiveId == Guid.Empty || definition is null
            || !FloralSiteOptions.ValidId(definition.SiteId) || change is null)
            throw new ArgumentException("game.invalid_request");

        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(ct);
        await using SqlTransaction tx = (SqlTransaction)
            await connection.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        // LOCK ORDER: always hive, then floral. Existing HiveRepository only
        // locks hive, and SQL FloraRepository only locks floral, so this cannot
        // form a resource lock cycle with the existing writers.
        await LockAsync(connection, tx,
            $"hive:{playerId:N}:{hiveId:N}", 51070, ct);
        PlayerHiveState hive = await ReadHiveAsync(connection, tx,
            playerId, hiveId, ct)
            ?? throw new KeyNotFoundException("game.not_found");
        if (hive.PlayerId != playerId || hive.HiveId != hiveId)
            throw new UnauthorizedAccessException("game.hive_forbidden");

        await LockAsync(connection, tx,
            $"floral:{definition.WorldId}:{definition.SiteId}", 51071, ct);
        FloralSiteState? storedSite = await ReadSiteAsync(connection, tx,
            definition.WorldId, definition.SiteId, ct);
        bool siteExists = storedSite is not null;
        FloralSiteState site = storedSite ?? FloralSiteService.InitialState(
            definition, catalogVersion, DateTimeOffset.UtcNow);
        FloralSiteService.ValidateStored(site);
        if (site.WorldId != definition.WorldId
            || site.RegionId != definition.RegionId
            || site.SiteId != definition.SiteId
            || site.FamilyId != definition.FamilyId)
            throw new InvalidDataException("Persisted floral identity mismatch.");

        // Prevent launching flights on a DB with only the P2 migration (096)
        // because those flights would later be impossible to settle safely.
        await CheckLedgerSchemaAsync(connection, tx, ct);
        CivilExpeditionMutation result = change(hive, site);
        if (result is null || result.Hive.PlayerId != playerId
            || result.Hive.HiveId != hiveId
            || result.Site.WorldId != definition.WorldId
            || result.Site.SiteId != definition.SiteId)
            throw new InvalidDataException("Cross-aggregate identity invalid.");
        if (result.Hive.Revision < hive.Revision
            || result.Site.Revision < site.Revision)
            throw new InvalidDataException("Cross-aggregate revision decreased.");
        FloralSiteService.ValidateStored(result.Site);
        CivilExpeditionRules.ValidateStored(result.Hive.CivilExpeditions
            ?? CivilExpeditionRules.Empty(), result.Hive.CivilPopulation);

        // Referential audit row, unique by mission ID. Failure rolls back
        // BOTH balances and every civilian reservation without partial payout.
        if (result.Contribution is { } contribution)
        {
            if (contribution.MissionId == Guid.Empty
                || contribution.PlayerId != playerId
                || contribution.HiveId != hiveId
                || contribution.WorldId != definition.WorldId
                || contribution.SiteId != definition.SiteId
                || contribution.PollenRemoved <= 0
                || contribution.PollenRemoved != contribution.PollenCredited
                || contribution.PollinationPoints != contribution.PollenRemoved)
                throw new InvalidDataException("Invalid contribution ledger row.");
            await InsertContributionAsync(connection, tx, contribution, ct);
        }
        CivilExpeditionMission? beforeFlight = hive.CivilExpeditions?.Active;
        CivilExpeditionMission? afterFlight = result.Hive.CivilExpeditions?.Active;
        if (beforeFlight is null && afterFlight is not null)
            await InsertDueAsync(connection, tx, afterFlight, playerId, hiveId, ct);
        else if (beforeFlight is not null && afterFlight is null)
            await DeleteDueAsync(connection, tx, beforeFlight.MissionId, ct);
        else if (beforeFlight is not null && afterFlight is not null
            && beforeFlight.MissionId != afterFlight.MissionId)
            throw new InvalidDataException("Civil mission replaced without settlement.");
        if (!ReferenceEquals(result.Hive, hive))
            await UpdateHiveAsync(connection, tx, hive.Revision, result.Hive, ct);

        // A brand-new floral site might be inserted even if the first start
        // fails. It is still a legitimate public-site seed from the server
        // catalogue; no resources are credited from such an initialization.
        if (!siteExists)
            await InsertSiteAsync(connection, tx, result.Site, ct);
        else if (!ReferenceEquals(result.Site, site))
            await UpdateSiteAsync(connection, tx, site.Revision, result.Site, ct);
        await tx.CommitAsync(ct);
        return result;
    }

    public async Task<IReadOnlyList<CivilDueExpedition>> DueAsync(
        DateTimeOffset nowUtc, int limit, CancellationToken ct = default)
    {
        if (limit is < 1 or > 200)
            throw new ArgumentOutOfRangeException(nameof(limit));
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandTimeout = commandTimeoutSeconds;
        command.CommandText =
            "SET TRANSACTION ISOLATION LEVEL READ COMMITTED; "
            + "SELECT TOP (@limit) MissionId,PlayerId,HiveId,ReturnsAtUtc "
            + "FROM dbo.FloralCivilActiveMissions "
            + "WHERE ReturnsAtUtc<=@now "
            + "ORDER BY ReturnsAtUtc,MissionId";
        command.Parameters.Add(new SqlParameter("@limit", SqlDbType.Int)
        { Value = limit });
        command.Parameters.Add(new SqlParameter("@now", SqlDbType.DateTime2)
        { Value = nowUtc.UtcDateTime });
        var result = new List<CivilDueExpedition>();
        await using SqlDataReader reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            result.Add(new(reader.GetGuid(0), reader.GetGuid(1),
                reader.GetGuid(2), new DateTimeOffset(
                    DateTime.SpecifyKind(reader.GetDateTime(3), DateTimeKind.Utc))));
        return result;
    }

    private async Task InsertDueAsync(SqlConnection connection, SqlTransaction tx,
        CivilExpeditionMission flight, Guid playerId, Guid hiveId, CancellationToken ct)
    {
        await using var command = NewCommand(connection, tx);
        command.CommandText =
            "INSERT dbo.FloralCivilActiveMissions "
            + "(MissionId,PlayerId,HiveId,WorldId,SiteId,ReturnsAtUtc) "
            + "VALUES(@mission,@player,@hive,@world,@site,@returns)";
        AddPlayerHive(command, playerId, hiveId);
        AddSite(command, flight.WorldId, flight.SiteId);
        command.Parameters.Add(new SqlParameter("@mission", SqlDbType.UniqueIdentifier)
        { Value = flight.MissionId });
        command.Parameters.Add(new SqlParameter("@returns", SqlDbType.DateTime2)
        { Value = flight.ReturnsAtUtc.UtcDateTime });
        await command.ExecuteNonQueryAsync(ct);
    }

    private async Task DeleteDueAsync(SqlConnection connection, SqlTransaction tx,
        Guid missionId, CancellationToken ct)
    {
        await using var command = NewCommand(connection, tx);
        command.CommandText =
            "DELETE FROM dbo.FloralCivilActiveMissions WHERE MissionId=@mission; "
            + "IF @@ROWCOUNT<>1 THROW 51075,'Civil mission due index is missing.',1;";
        command.Parameters.Add(new SqlParameter("@mission", SqlDbType.UniqueIdentifier)
        { Value = missionId });
        await command.ExecuteNonQueryAsync(ct);
    }
    private async Task LockAsync(SqlConnection connection, SqlTransaction tx,
        string key, int errorCode, CancellationToken ct)
    {
        await using var command = NewCommand(connection, tx);
        command.CommandText =
            "DECLARE @result int; EXEC @result=sys.sp_getapplock "
            + "@Resource=@resource,@LockMode='Exclusive',"
            + "@LockOwner='Transaction',@LockTimeout=@timeout; "
            + $"IF @result<0 THROW {errorCode},'Aggregate lock unavailable.',1;";
        command.Parameters.Add(new SqlParameter("@resource", SqlDbType.NVarChar, 255)
        { Value = key });
        command.Parameters.Add(new SqlParameter("@timeout", SqlDbType.Int)
        { Value = commandTimeoutSeconds * 1000 });
        await command.ExecuteNonQueryAsync(ct);
    }

    private async Task<PlayerHiveState?> ReadHiveAsync(SqlConnection connection,
        SqlTransaction tx, Guid playerId, Guid hiveId, CancellationToken ct)
    {
        await using var command = NewCommand(connection, tx);
        command.CommandText =
            "SELECT StateJson FROM dbo.HivePlayerStates WITH (UPDLOCK,HOLDLOCK) "
            + "WHERE PlayerId=@player AND HiveId=@hive";
        AddPlayerHive(command, playerId, hiveId);
        object? raw = await command.ExecuteScalarAsync(ct);
        return raw is string value
            ? HiveStateMigrator.ToCurrent(
                JsonSerializer.Deserialize<PlayerHiveState>(value, JsonOptions)
                ?? throw new InvalidDataException("Null stored hive document."))
            : null;
    }

    private async Task<FloralSiteState?> ReadSiteAsync(SqlConnection connection,
        SqlTransaction tx, string worldId, string siteId, CancellationToken ct)
    {
        await using var command = NewCommand(connection, tx);
        command.CommandText =
            "SELECT StateJson FROM dbo.FloralSiteStates WITH (UPDLOCK,HOLDLOCK) "
            + "WHERE WorldId=@world AND SiteId=@site";
        AddSite(command, worldId, siteId);
        object? raw = await command.ExecuteScalarAsync(ct);
        return raw is string value
            ? JsonSerializer.Deserialize<FloralSiteState>(value, JsonOptions)
                ?? throw new InvalidDataException("Null stored floral document.")
            : null;
    }

    private async Task CheckLedgerSchemaAsync(SqlConnection connection,
        SqlTransaction tx, CancellationToken ct)
    {
        await using var command = NewCommand(connection, tx);
        command.CommandText =
            "IF OBJECT_ID(N'dbo.FloralCivilContributions',N'U') IS NULL OR OBJECT_ID(N'dbo.FloralCivilActiveMissions',N'U') IS NULL "
            + "THROW 51072,'Floral contribution schema 097 is missing.',1;";
        await command.ExecuteNonQueryAsync(ct);
    }

    private async Task UpdateHiveAsync(SqlConnection connection,
        SqlTransaction tx, long before, PlayerHiveState state, CancellationToken ct)
    {
        await using var command = NewCommand(connection, tx);
        command.CommandText =
            "UPDATE dbo.HivePlayerStates SET Revision=@revision, "
            + "ModelVersion=@version, StateJson=@json, UpdatedAtUtc=SYSUTCDATETIME() "
            + "WHERE PlayerId=@player AND HiveId=@hive AND Revision=@before; "
            + "IF @@ROWCOUNT<>1 THROW 51073,'Hive revision changed.',1;";
        AddPlayerHive(command, state.PlayerId, state.HiveId);
        command.Parameters.Add(new SqlParameter("@version", SqlDbType.Int)
        { Value = state.ModelVersion });
        command.Parameters.Add(new SqlParameter("@revision", SqlDbType.BigInt)
        { Value = state.Revision });
        command.Parameters.Add(new SqlParameter("@before", SqlDbType.BigInt)
        { Value = before });
        command.Parameters.Add(new SqlParameter("@json", SqlDbType.NVarChar, -1)
        { Value = JsonSerializer.Serialize(state, JsonOptions) });
        await command.ExecuteNonQueryAsync(ct);
    }

    private async Task InsertSiteAsync(SqlConnection connection,
        SqlTransaction tx, FloralSiteState state, CancellationToken ct)
    {
        await using var command = NewCommand(connection, tx);
        command.CommandText =
            "INSERT dbo.FloralSiteStates(WorldId,SiteId,Revision,StateJson) "
            + "VALUES(@world,@site,@revision,@json);";
        AddSite(command, state.WorldId, state.SiteId);
        AddSiteValues(command, state);
        await command.ExecuteNonQueryAsync(ct);
    }

    private async Task UpdateSiteAsync(SqlConnection connection,
        SqlTransaction tx, long before, FloralSiteState state, CancellationToken ct)
    {
        await using var command = NewCommand(connection, tx);
        command.CommandText =
            "UPDATE dbo.FloralSiteStates SET Revision=@revision, "
            + "StateJson=@json,UpdatedAtUtc=SYSUTCDATETIME() "
            + "WHERE WorldId=@world AND SiteId=@site AND Revision=@before; "
            + "IF @@ROWCOUNT<>1 THROW 51074,'Floral revision changed.',1;";
        AddSite(command, state.WorldId, state.SiteId);
        AddSiteValues(command, state);
        command.Parameters.Add(new SqlParameter("@before", SqlDbType.BigInt)
        { Value = before });
        await command.ExecuteNonQueryAsync(ct);
    }

    private async Task InsertContributionAsync(SqlConnection connection,
        SqlTransaction tx, CivilExpeditionContribution row, CancellationToken ct)
    {
        await using var command = NewCommand(connection, tx);
        command.CommandText =
            "INSERT dbo.FloralCivilContributions "
            + "(MissionId,PlayerId,HiveId,WorldId,SiteId,"
            + "PollenRemoved,PollenCredited,PollinationPoints,CompletedAtUtc) "
            + "VALUES(@mission,@player,@hive,@world,@site,"
            + "@removed,@credited,@points,@completed)";
        AddPlayerHive(command, row.PlayerId, row.HiveId);
        AddSite(command, row.WorldId, row.SiteId);
        command.Parameters.Add(new SqlParameter("@mission", SqlDbType.UniqueIdentifier)
        { Value = row.MissionId });
        command.Parameters.Add(new SqlParameter("@removed", SqlDbType.BigInt)
        { Value = row.PollenRemoved });
        command.Parameters.Add(new SqlParameter("@credited", SqlDbType.BigInt)
        { Value = row.PollenCredited });
        command.Parameters.Add(new SqlParameter("@points", SqlDbType.BigInt)
        { Value = row.PollinationPoints });
        command.Parameters.Add(new SqlParameter("@completed", SqlDbType.DateTime2)
        { Value = row.AtUtc.UtcDateTime });
        await command.ExecuteNonQueryAsync(ct);
    }

    private SqlCommand NewCommand(SqlConnection connection, SqlTransaction tx) =>
        new() { Connection = connection, Transaction = tx,
            CommandTimeout = commandTimeoutSeconds };

    private static void AddPlayerHive(SqlCommand command, Guid playerId, Guid hiveId)
    {
        command.Parameters.Add(new SqlParameter("@player", SqlDbType.UniqueIdentifier)
        { Value = playerId });
        command.Parameters.Add(new SqlParameter("@hive", SqlDbType.UniqueIdentifier)
        { Value = hiveId });
    }
    private static void AddSite(SqlCommand command, string worldId, string siteId)
    {
        command.Parameters.Add(new SqlParameter("@world", SqlDbType.NVarChar, 64)
        { Value = worldId });
        command.Parameters.Add(new SqlParameter("@site", SqlDbType.NVarChar, 64)
        { Value = siteId });
    }
    private static void AddSiteValues(SqlCommand command, FloralSiteState state)
    {
        command.Parameters.Add(new SqlParameter("@revision", SqlDbType.BigInt)
        { Value = state.Revision });
        command.Parameters.Add(new SqlParameter("@json", SqlDbType.NVarChar, -1)
        { Value = JsonSerializer.Serialize(state, JsonOptions) });
    }
}

public sealed class UnavailableCivilExpeditionUnitOfWork : ICivilExpeditionUnitOfWork
{
    public Task<CivilExpeditionMutation> ExecuteAsync(
        Guid playerId, Guid hiveId, FloralSiteDefinition site,
        string catalogVersion,
        Func<PlayerHiveState, FloralSiteState, CivilExpeditionMutation> change,
        CancellationToken ct = default)
        => throw new NotSupportedException(
            "Civil expedition writes require one shared SQL database.");
}
