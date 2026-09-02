using System.Data;
using BeeKingdom.Alliance.Models;
using BeeKingdom.Persistence.Sql;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Alliance.Repositories;

public sealed class SqlAllianceDiplomacyRepository : IAllianceDiplomacyRepository
{
    private readonly SqlConnectionFactory connectionFactory;

    public SqlAllianceDiplomacyRepository(SqlConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public AllianceDiplomaticRelation Save(AllianceDiplomaticRelation relation)
    {
        (Guid a, Guid b) = InMemoryAllianceDiplomacyRepository.CanonicalKey(relation.AllianceIdA, relation.AllianceIdB);
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            MERGE dbo.AllianceDiplomaticRelations AS target
            USING (SELECT @AllianceIdA AS AllianceIdA, @AllianceIdB AS AllianceIdB) AS source
            ON target.AllianceIdA = source.AllianceIdA AND target.AllianceIdB = source.AllianceIdB
            WHEN MATCHED THEN
                UPDATE SET RelationId = @RelationId, RelationType = @RelationType, Status = @Status,
                           CreatedAtUtc = @CreatedAtUtc, UpdatedAtUtc = @UpdatedAtUtc,
                           InitiatedByAllianceId = @InitiatedByAllianceId, Revision = @Revision
            WHEN NOT MATCHED THEN
                INSERT (RelationId, AllianceIdA, AllianceIdB, RelationType, Status, CreatedAtUtc, UpdatedAtUtc,
                        InitiatedByAllianceId, Revision)
                VALUES (@RelationId, @AllianceIdA, @AllianceIdB, @RelationType, @Status, @CreatedAtUtc, @UpdatedAtUtc,
                        @InitiatedByAllianceId, @Revision);
            """;
        Add(command, "@RelationId", relation.RelationId);
        Add(command, "@AllianceIdA", a);
        Add(command, "@AllianceIdB", b);
        Add(command, "@RelationType", relation.RelationType.ToString());
        Add(command, "@Status", relation.Status.ToString());
        Add(command, "@CreatedAtUtc", relation.CreatedAtUtc.UtcDateTime);
        Add(command, "@UpdatedAtUtc", relation.UpdatedAtUtc.UtcDateTime);
        Add(command, "@InitiatedByAllianceId", relation.InitiatedByAllianceId.Value);
        Add(command, "@Revision", relation.Revision);
        command.ExecuteNonQuery();
        return relation;
    }

    public AllianceDiplomaticRelation? GetRelation(AllianceId allianceA, AllianceId allianceB)
    {
        (Guid a, Guid b) = InMemoryAllianceDiplomacyRepository.CanonicalKey(allianceA, allianceB);
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM dbo.AllianceDiplomaticRelations WHERE AllianceIdA = @AllianceIdA AND AllianceIdB = @AllianceIdB;";
        Add(command, "@AllianceIdA", a);
        Add(command, "@AllianceIdB", b);
        using IDataReader reader = command.ExecuteReader();
        return reader.Read() ? ReadRelation(reader) : null;
    }

    public IReadOnlyList<AllianceDiplomaticRelation> ListForAlliance(AllianceId allianceId)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT * FROM dbo.AllianceDiplomaticRelations
            WHERE AllianceIdA = @AllianceId OR AllianceIdB = @AllianceId
            ORDER BY UpdatedAtUtc DESC;
            """;
        Add(command, "@AllianceId", allianceId.Value);
        using IDataReader reader = command.ExecuteReader();
        List<AllianceDiplomaticRelation> results = new();
        while (reader.Read())
        {
            results.Add(ReadRelation(reader));
        }
        return results;
    }

    public Guid? GetProposalReceipt(PlayerId actorPlayerId, string clientRequestId)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = "SELECT RelationId FROM dbo.AllianceDiplomacyProposalReceipts WHERE PlayerId = @PlayerId AND ClientRequestId = @ClientRequestId;";
        Add(command, "@PlayerId", actorPlayerId.Value);
        Add(command, "@ClientRequestId", clientRequestId);
        object? result = command.ExecuteScalar();
        return result is Guid guid ? guid : null;
    }

    public void SaveProposalReceipt(PlayerId actorPlayerId, string clientRequestId, Guid relationId)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            MERGE dbo.AllianceDiplomacyProposalReceipts AS target
            USING (SELECT @PlayerId AS PlayerId, @ClientRequestId AS ClientRequestId) AS source
            ON target.PlayerId = source.PlayerId AND target.ClientRequestId = source.ClientRequestId
            WHEN MATCHED THEN UPDATE SET RelationId = @RelationId
            WHEN NOT MATCHED THEN INSERT (PlayerId, ClientRequestId, RelationId) VALUES (@PlayerId, @ClientRequestId, @RelationId);
            """;
        Add(command, "@PlayerId", actorPlayerId.Value);
        Add(command, "@ClientRequestId", clientRequestId);
        Add(command, "@RelationId", relationId);
        command.ExecuteNonQuery();
    }

    private static AllianceDiplomaticRelation ReadRelation(IDataReader reader) => new()
    {
        RelationId = (Guid)reader["RelationId"],
        AllianceIdA = new AllianceId((Guid)reader["AllianceIdA"]),
        AllianceIdB = new AllianceId((Guid)reader["AllianceIdB"]),
        RelationType = Enum.Parse<AllianceRelationType>((string)reader["RelationType"]),
        Status = Enum.Parse<AllianceRelationStatus>((string)reader["Status"]),
        CreatedAtUtc = AsUtc((DateTime)reader["CreatedAtUtc"]),
        UpdatedAtUtc = AsUtc((DateTime)reader["UpdatedAtUtc"]),
        InitiatedByAllianceId = new AllianceId((Guid)reader["InitiatedByAllianceId"]),
        Revision = (long)reader["Revision"]
    };

    private static DateTimeOffset AsUtc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static void Add(IDbCommand command, string name, object value)
    {
        IDbDataParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
