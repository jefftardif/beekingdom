using System.Data;
using BeeKingdom.Persistence.Sql;
using BeeKingdom.Shared.ValueObjects;
using Microsoft.Data.SqlClient;

namespace BeeKingdom.Alliance.Help;

// Schema: Server/src/BeeKingdom.Database/Scripts/<next>_alliance_help.sql (NOT executed against
// production by this mission - see the M045-CL report for why a migration is genuinely needed and
// exactly what it creates). Two tables: dbo.AllianceHelpRequests (one row per help request) and
// dbo.AllianceHelpContributions (one row per helper, PRIMARY KEY (HelpRequestId, HelperPlayerId) -
// the DB itself is the final backstop against a helper contributing twice, on top of the
// application-level check in AllianceHelpService).
public sealed class SqlAllianceHelpRepository : IAllianceHelpRepository
{
    private readonly SqlConnectionFactory connectionFactory;

    public SqlAllianceHelpRepository(SqlConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<AllianceHelpRequest?> TryCreateAsync(AllianceHelpRequest request, CancellationToken cancellationToken = default)
    {
        using SqlConnection connection = (SqlConnection)connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        using SqlCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.AllianceHelpRequests
                (HelpRequestId, AllianceId, RequestingPlayerId, RequestingHiveId, OperationCategory, OperationTargetId,
                 OperationId, CreatedAtUtc, Status, OriginalDurationSeconds, HelpCount, MaxHelpCount, Revision, ClientRequestId)
            VALUES
                (@HelpRequestId, @AllianceId, @RequestingPlayerId, @RequestingHiveId, @OperationCategory, @OperationTargetId,
                 @OperationId, @CreatedAtUtc, @Status, @OriginalDurationSeconds, @HelpCount, @MaxHelpCount, @Revision, @ClientRequestId);
            """;
        AddRequestParameters(command, request);
        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
            return request;
        }
        catch (SqlException exception) when (IsUniqueViolation(exception))
        {
            // UX_AllianceHelpRequests_Player_Operation_Open: this player already has an OPEN
            // request for this exact operation - the DB is the final authority, not just the
            // service-level GetOpenForPlayerOperationAsync check that runs first.
            return null;
        }
    }

    public async Task<AllianceHelpRequest?> GetAsync(Guid helpRequestId, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = SelectRequestSql("WHERE HelpRequestId = @HelpRequestId");
        Add(command, "@HelpRequestId", helpRequestId);
        using IDataReader reader = command.ExecuteReader();
        return reader.Read() ? ReadRequest(reader) : null;
    }

    public Task<IReadOnlyList<AllianceHelpRequest>> ListOpenForAllianceAsync(Guid allianceId, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = SelectRequestSql("WHERE AllianceId = @AllianceId AND Status = @Status ORDER BY CreatedAtUtc ASC");
        Add(command, "@AllianceId", allianceId);
        Add(command, "@Status", AllianceHelpRequestStatus.Open.ToString());
        using IDataReader reader = command.ExecuteReader();
        List<AllianceHelpRequest> result = [];
        while (reader.Read()) result.Add(ReadRequest(reader));
        return Task.FromResult<IReadOnlyList<AllianceHelpRequest>>(result);
    }

    public async Task<AllianceHelpRequest?> GetOpenForPlayerOperationAsync(Guid requestingPlayerId, string operationCategory, string operationTargetId, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = SelectRequestSql("WHERE RequestingPlayerId = @RequestingPlayerId AND OperationCategory = @OperationCategory AND OperationTargetId = @OperationTargetId AND Status = @Status");
        Add(command, "@RequestingPlayerId", requestingPlayerId);
        Add(command, "@OperationCategory", operationCategory);
        Add(command, "@OperationTargetId", operationTargetId);
        Add(command, "@Status", AllianceHelpRequestStatus.Open.ToString());
        using IDataReader reader = command.ExecuteReader();
        return reader.Read() ? ReadRequest(reader) : null;
    }

    public async Task<AllianceHelpContribution?> GetContributionAsync(Guid helpRequestId, Guid helperPlayerId, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT HelpRequestId, HelperPlayerId, HelpedAtUtc, DurationReductionSeconds, ClientRequestId
            FROM dbo.AllianceHelpContributions
            WHERE HelpRequestId = @HelpRequestId AND HelperPlayerId = @HelperPlayerId;
            """;
        Add(command, "@HelpRequestId", helpRequestId);
        Add(command, "@HelperPlayerId", helperPlayerId);
        using IDataReader reader = command.ExecuteReader();
        return reader.Read() ? ReadContribution(reader) : null;
    }

    public async Task<(bool Applied, string Code, AllianceHelpRequest? Request)> TryContributeAsync(
        Guid helpRequestId, long expectedRevision, AllianceHelpContribution contribution, CancellationToken cancellationToken = default)
    {
        using SqlConnection connection = (SqlConnection)connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        using SqlTransaction transaction = (SqlTransaction)connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            using (SqlCommand insertCommand = connection.CreateCommand())
            {
                insertCommand.Transaction = transaction;
                insertCommand.CommandText = """
                    INSERT INTO dbo.AllianceHelpContributions (HelpRequestId, HelperPlayerId, HelpedAtUtc, DurationReductionSeconds, ClientRequestId)
                    VALUES (@HelpRequestId, @HelperPlayerId, @HelpedAtUtc, @DurationReductionSeconds, @ClientRequestId);
                    """;
                Add(insertCommand, "@HelpRequestId", contribution.HelpRequestId);
                Add(insertCommand, "@HelperPlayerId", contribution.HelperPlayerId.Value);
                Add(insertCommand, "@HelpedAtUtc", contribution.HelpedAtUtc.UtcDateTime);
                Add(insertCommand, "@DurationReductionSeconds", contribution.DurationReductionSeconds);
                Add(insertCommand, "@ClientRequestId", contribution.ClientRequestId);
                try
                {
                    await insertCommand.ExecuteNonQueryAsync(cancellationToken);
                }
                catch (SqlException exception) when (IsUniqueViolation(exception))
                {
                    transaction.Rollback();
                    AllianceHelpRequest? existing = await GetAsync(helpRequestId, cancellationToken);
                    return (false, "already_helped", existing);
                }
            }

            using (SqlCommand updateCommand = connection.CreateCommand())
            {
                updateCommand.Transaction = transaction;
                updateCommand.CommandText = """
                    UPDATE dbo.AllianceHelpRequests
                    SET HelpCount = HelpCount + 1,
                        Revision = Revision + 1,
                        Status = CASE WHEN HelpCount + 1 >= MaxHelpCount THEN @CompletedStatus ELSE Status END
                    WHERE HelpRequestId = @HelpRequestId AND Status = @OpenStatus AND Revision = @ExpectedRevision AND HelpCount < MaxHelpCount;
                    """;
                Add(updateCommand, "@HelpRequestId", helpRequestId);
                Add(updateCommand, "@OpenStatus", AllianceHelpRequestStatus.Open.ToString());
                Add(updateCommand, "@CompletedStatus", AllianceHelpRequestStatus.Completed.ToString());
                Add(updateCommand, "@ExpectedRevision", expectedRevision);
                int affected = await updateCommand.ExecuteNonQueryAsync(cancellationToken);
                if (affected == 0)
                {
                    transaction.Rollback();
                    using IDbCommand readCommand = connection.CreateCommand();
                    readCommand.CommandText = SelectRequestSql("WHERE HelpRequestId = @HelpRequestId");
                    Add(readCommand, "@HelpRequestId", helpRequestId);
                    using IDataReader reader = readCommand.ExecuteReader();
                    if (!reader.Read()) return (false, "not_found", null);
                    AllianceHelpRequest current = ReadRequest(reader);
                    string code = current.Status != AllianceHelpRequestStatus.Open ? "request_not_open"
                        : current.HelpCount >= current.MaxHelpCount ? "help_full"
                        : "revision_conflict";
                    return (false, code, current);
                }
            }

            transaction.Commit();
            AllianceHelpRequest? updated = await GetAsync(helpRequestId, cancellationToken);
            return (true, "help_applied", updated);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<AllianceHelpRequest?> TryUpdateStatusAsync(Guid helpRequestId, long expectedRevision, AllianceHelpRequestStatus status, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            UPDATE dbo.AllianceHelpRequests
            SET Status = @Status, Revision = Revision + 1
            WHERE HelpRequestId = @HelpRequestId AND Revision = @ExpectedRevision;
            """;
        Add(command, "@HelpRequestId", helpRequestId);
        Add(command, "@Status", status.ToString());
        Add(command, "@ExpectedRevision", expectedRevision);
        int affected = command.ExecuteNonQuery();
        return affected > 0 ? await GetAsync(helpRequestId, cancellationToken) : null;
    }

    public async Task CancelOpenRequestsForPlayerAsync(Guid allianceId, Guid playerId, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            UPDATE dbo.AllianceHelpRequests
            SET Status = @Cancelled, Revision = Revision + 1
            WHERE AllianceId = @AllianceId AND RequestingPlayerId = @PlayerId AND Status = @Open;
            """;
        Add(command, "@AllianceId", allianceId);
        Add(command, "@PlayerId", playerId);
        Add(command, "@Cancelled", AllianceHelpRequestStatus.Cancelled.ToString());
        Add(command, "@Open", AllianceHelpRequestStatus.Open.ToString());
        command.ExecuteNonQuery();
    }

    public async Task CancelAllOpenRequestsForAllianceAsync(Guid allianceId, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            UPDATE dbo.AllianceHelpRequests
            SET Status = @Cancelled, Revision = Revision + 1
            WHERE AllianceId = @AllianceId AND Status = @Open;
            """;
        Add(command, "@AllianceId", allianceId);
        Add(command, "@Cancelled", AllianceHelpRequestStatus.Cancelled.ToString());
        Add(command, "@Open", AllianceHelpRequestStatus.Open.ToString());
        command.ExecuteNonQuery();
    }

    private static bool IsUniqueViolation(SqlException exception) => exception.Number is 2627 or 2601;

    private static string SelectRequestSql(string whereClause) => $"""
        SELECT HelpRequestId, AllianceId, RequestingPlayerId, RequestingHiveId, OperationCategory, OperationTargetId,
               OperationId, CreatedAtUtc, Status, OriginalDurationSeconds, HelpCount, MaxHelpCount, Revision, ClientRequestId
        FROM dbo.AllianceHelpRequests
        {whereClause};
        """;

    private static AllianceHelpRequest ReadRequest(IDataReader reader) => new(
        reader.GetGuid(0),
        new AllianceId(reader.GetGuid(1)),
        new PlayerId(reader.GetGuid(2)),
        reader.GetGuid(3),
        reader.GetString(4),
        reader.GetString(5),
        reader.GetGuid(6),
        new DateTimeOffset(reader.GetDateTime(7), TimeSpan.Zero),
        Enum.Parse<AllianceHelpRequestStatus>(reader.GetString(8), ignoreCase: true),
        reader.GetInt64(9),
        reader.GetInt32(10),
        reader.GetInt32(11),
        reader.GetInt64(12),
        reader.GetString(13));

    private static AllianceHelpContribution ReadContribution(IDataReader reader) => new(
        reader.GetGuid(0),
        new PlayerId(reader.GetGuid(1)),
        new DateTimeOffset(reader.GetDateTime(2), TimeSpan.Zero),
        reader.GetInt64(3),
        reader.GetString(4));

    private static void AddRequestParameters(IDbCommand command, AllianceHelpRequest request)
    {
        Add(command, "@HelpRequestId", request.HelpRequestId);
        Add(command, "@AllianceId", request.AllianceId.Value);
        Add(command, "@RequestingPlayerId", request.RequestingPlayerId.Value);
        Add(command, "@RequestingHiveId", request.RequestingHiveId);
        Add(command, "@OperationCategory", request.OperationCategory);
        Add(command, "@OperationTargetId", request.OperationTargetId);
        Add(command, "@OperationId", request.OperationId);
        Add(command, "@CreatedAtUtc", request.CreatedAtUtc.UtcDateTime);
        Add(command, "@Status", request.Status.ToString());
        Add(command, "@OriginalDurationSeconds", request.OriginalDurationSeconds);
        Add(command, "@HelpCount", request.HelpCount);
        Add(command, "@MaxHelpCount", request.MaxHelpCount);
        Add(command, "@Revision", request.Revision);
        Add(command, "@ClientRequestId", request.ClientRequestId);
    }

    private static void Add(IDbCommand command, string name, object value)
    {
        IDbDataParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
