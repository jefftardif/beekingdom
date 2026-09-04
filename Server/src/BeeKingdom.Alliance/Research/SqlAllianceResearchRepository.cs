using System.Data;
using System.Text.Json;
using BeeKingdom.Persistence.Sql;
using Microsoft.Data.SqlClient;

namespace BeeKingdom.Alliance.Research;

// Schema: Server/src/BeeKingdom.Database/Scripts/<next>_alliance_research.sql (NOT executed against
// production by this mission - see the M051-CL report). One row per Alliance, the whole
// AllianceResearchState serialized as JSON (same shape as dbo.HivePlayerStates/SqlHiveStateRepository
// - the codebase's own proven pattern for a single-aggregate-per-key durable document), guarded by a
// real exclusive sys.sp_getapplock per AllianceId for the duration of the mutation transaction so
// concurrent donations to the same Alliance always serialize - no optimistic-retry loop needed.
public sealed class SqlAllianceResearchRepository : IAllianceResearchRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly SqlConnectionFactory connectionFactory;

    public SqlAllianceResearchRepository(SqlConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<AllianceResearchState> ExecuteAtomicallyAsync(Guid allianceId, Func<AllianceResearchState, AllianceResearchState> mutation, CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        await AcquireLockAsync(connection, transaction, allianceId, cancellationToken);
        AllianceResearchState current = await ReadLockedAsync(connection, transaction, allianceId, cancellationToken) ?? AllianceResearchState.Empty(allianceId);
        AllianceResearchState updated = mutation(current);
        await UpsertAsync(connection, transaction, updated, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return updated;
    }

    public async Task<AllianceResearchState?> ReadAsync(Guid allianceId, CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT StateJson FROM dbo.AllianceResearch WHERE AllianceId=@allianceId";
        command.Parameters.Add(new SqlParameter("@allianceId", SqlDbType.UniqueIdentifier) { Value = allianceId });
        object? json = await command.ExecuteScalarAsync(cancellationToken);
        return json is string value ? JsonSerializer.Deserialize<AllianceResearchState>(value, JsonOptions) : null;
    }

    private static async Task AcquireLockAsync(SqlConnection connection, SqlTransaction transaction, Guid allianceId, CancellationToken ct)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DECLARE @result int; EXEC @result=sys.sp_getapplock @Resource=@resource,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000; IF @result<0 THROW 51071,'Alliance research lock unavailable.',1;";
        command.Parameters.Add(new SqlParameter("@resource", SqlDbType.NVarChar, 255) { Value = $"allianceresearch:{allianceId:N}" });
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task<AllianceResearchState?> ReadLockedAsync(SqlConnection connection, SqlTransaction transaction, Guid allianceId, CancellationToken ct)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT StateJson FROM dbo.AllianceResearch WITH (UPDLOCK,HOLDLOCK) WHERE AllianceId=@allianceId";
        command.Parameters.Add(new SqlParameter("@allianceId", SqlDbType.UniqueIdentifier) { Value = allianceId });
        object? json = await command.ExecuteScalarAsync(ct);
        return json is string value ? JsonSerializer.Deserialize<AllianceResearchState>(value, JsonOptions) : null;
    }

    private static async Task UpsertAsync(SqlConnection connection, SqlTransaction transaction, AllianceResearchState state, CancellationToken ct)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "UPDATE dbo.AllianceResearch SET ModelVersion=@version,Revision=@revision,StateJson=@json,UpdatedAtUtc=SYSUTCDATETIME() WHERE AllianceId=@allianceId; IF @@ROWCOUNT=0 INSERT dbo.AllianceResearch(AllianceId,ModelVersion,Revision,StateJson) VALUES(@allianceId,@version,@revision,@json);";
        command.Parameters.Add(new SqlParameter("@allianceId", SqlDbType.UniqueIdentifier) { Value = state.AllianceId });
        command.Parameters.Add(new SqlParameter("@version", SqlDbType.Int) { Value = state.ModelVersion });
        command.Parameters.Add(new SqlParameter("@revision", SqlDbType.BigInt) { Value = state.Revision });
        command.Parameters.Add(new SqlParameter("@json", SqlDbType.NVarChar, -1) { Value = JsonSerializer.Serialize(state, JsonOptions) });
        await command.ExecuteNonQueryAsync(ct);
    }
}
