using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace BeeKingdom.HiveOperations;

public sealed class SqlHiveStateRepository(string connectionString, Func<Guid, Guid, PlayerHiveState> newStateFactory, int commandTimeoutSeconds = 15) : IHiveStateRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<PlayerHiveState> ExecuteAtomicallyAsync(Guid playerId, Guid hiveId, Func<PlayerHiveState, PlayerHiveState> mutation, CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await AcquireLockAsync(connection, transaction, playerId, hiveId, cancellationToken);
        PlayerHiveState state = await ReadLockedAsync(connection, transaction, playerId, hiveId, cancellationToken) ?? newStateFactory(playerId, hiveId);
        PlayerHiveState updated = mutation(HiveStateMigrator.ToCurrent(state));
        await UpsertAsync(connection, transaction, updated, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return updated;
    }

    public async Task<PlayerHiveState?> ReadAsync(Guid playerId, Guid hiveId, CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandTimeout = commandTimeoutSeconds;
        command.CommandText = "SELECT StateJson FROM dbo.HivePlayerStates WHERE PlayerId=@playerId AND HiveId=@hiveId";
        command.Parameters.Add(new SqlParameter("@playerId", SqlDbType.UniqueIdentifier) { Value = playerId });
        command.Parameters.Add(new SqlParameter("@hiveId", SqlDbType.UniqueIdentifier) { Value = hiveId });
        object? json = await command.ExecuteScalarAsync(cancellationToken);
        return json is string value ? HiveStateMigrator.ToCurrent(JsonSerializer.Deserialize<PlayerHiveState>(value, JsonOptions)!) : null;
    }

    public async Task<IReadOnlyList<Guid>> ListHiveIdsAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandTimeout = commandTimeoutSeconds;
        command.CommandText = "SELECT HiveId FROM dbo.HivePlayerStates WHERE PlayerId=@playerId";
        command.Parameters.Add(new SqlParameter("@playerId", SqlDbType.UniqueIdentifier) { Value = playerId });
        var hiveIds = new List<Guid>();
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) hiveIds.Add(reader.GetGuid(0));
        return hiveIds;
    }

    public async Task<IReadOnlyList<PlayerHiveState>> ListRecentlyActiveAsync(int limit, CancellationToken cancellationToken = default)
    {
        if (limit <= 0) return Array.Empty<PlayerHiveState>();
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandTimeout = commandTimeoutSeconds;
        command.CommandText = "SELECT TOP (@limit) StateJson FROM dbo.HivePlayerStates ORDER BY UpdatedAtUtc DESC";
        command.Parameters.Add(new SqlParameter("@limit", SqlDbType.Int) { Value = limit });
        var states = new List<PlayerHiveState>();
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            states.Add(HiveStateMigrator.ToCurrent(JsonSerializer.Deserialize<PlayerHiveState>(reader.GetString(0), JsonOptions)!));
        return states;
    }

    private async Task AcquireLockAsync(SqlConnection connection, SqlTransaction transaction, Guid playerId, Guid hiveId, CancellationToken ct)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandTimeout = commandTimeoutSeconds;
        command.CommandText = "DECLARE @result int; EXEC @result=sys.sp_getapplock @Resource=@resource,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=@timeout; IF @result<0 THROW 51070,'Hive state lock unavailable.',1;";
        command.Parameters.Add(new SqlParameter("@resource", SqlDbType.NVarChar, 255) { Value = $"hive:{playerId:N}:{hiveId:N}" });
        command.Parameters.Add(new SqlParameter("@timeout", SqlDbType.Int) { Value = commandTimeoutSeconds * 1000 });
        await command.ExecuteNonQueryAsync(ct);
    }

    private async Task<PlayerHiveState?> ReadLockedAsync(SqlConnection connection, SqlTransaction transaction, Guid playerId, Guid hiveId, CancellationToken ct)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandTimeout = commandTimeoutSeconds;
        command.CommandText = "SELECT StateJson FROM dbo.HivePlayerStates WITH (UPDLOCK,HOLDLOCK) WHERE PlayerId=@playerId AND HiveId=@hiveId";
        command.Parameters.Add(new SqlParameter("@playerId", SqlDbType.UniqueIdentifier) { Value = playerId });
        command.Parameters.Add(new SqlParameter("@hiveId", SqlDbType.UniqueIdentifier) { Value = hiveId });
        object? json = await command.ExecuteScalarAsync(ct);
        return json is string value ? JsonSerializer.Deserialize<PlayerHiveState>(value, JsonOptions) : null;
    }

    private async Task UpsertAsync(SqlConnection connection, SqlTransaction transaction, PlayerHiveState state, CancellationToken ct)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandTimeout = commandTimeoutSeconds;
        command.CommandText = "UPDATE dbo.HivePlayerStates SET ModelVersion=@version,Revision=@revision,StateJson=@json,UpdatedAtUtc=SYSUTCDATETIME() WHERE PlayerId=@playerId AND HiveId=@hiveId; IF @@ROWCOUNT=0 INSERT dbo.HivePlayerStates(PlayerId,HiveId,ModelVersion,Revision,StateJson) VALUES(@playerId,@hiveId,@version,@revision,@json);";
        command.Parameters.Add(new SqlParameter("@playerId", SqlDbType.UniqueIdentifier) { Value = state.PlayerId });
        command.Parameters.Add(new SqlParameter("@hiveId", SqlDbType.UniqueIdentifier) { Value = state.HiveId });
        command.Parameters.Add(new SqlParameter("@version", SqlDbType.Int) { Value = state.ModelVersion });
        command.Parameters.Add(new SqlParameter("@revision", SqlDbType.BigInt) { Value = state.Revision });
        command.Parameters.Add(new SqlParameter("@json", SqlDbType.NVarChar, -1) { Value = JsonSerializer.Serialize(state, JsonOptions) });
        await command.ExecuteNonQueryAsync(ct);
    }
}
