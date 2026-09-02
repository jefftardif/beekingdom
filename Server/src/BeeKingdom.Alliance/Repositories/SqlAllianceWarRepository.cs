using System.Data;
using BeeKingdom.Alliance.Models;
using BeeKingdom.Persistence.Sql;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Alliance.Repositories;

public sealed class SqlAllianceWarRepository : IAllianceWarRepository
{
    private readonly SqlConnectionFactory connectionFactory;

    public SqlAllianceWarRepository(SqlConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public AllianceWar Save(AllianceWar war)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            MERGE dbo.AllianceWars AS target
            USING (SELECT @WarId AS WarId) AS source
            ON target.WarId = source.WarId
            WHEN MATCHED THEN
                UPDATE SET AttackerAllianceId = @AttackerAllianceId, DefenderAllianceId = @DefenderAllianceId,
                           Status = @Status, DeclaredAtUtc = @DeclaredAtUtc, StartedAtUtc = @StartedAtUtc,
                           EndedAtUtc = @EndedAtUtc, WinnerAllianceId = @WinnerAllianceId, Revision = @Revision
            WHEN NOT MATCHED THEN
                INSERT (WarId, AttackerAllianceId, DefenderAllianceId, Status, DeclaredAtUtc, StartedAtUtc,
                        EndedAtUtc, WinnerAllianceId, Revision)
                VALUES (@WarId, @AttackerAllianceId, @DefenderAllianceId, @Status, @DeclaredAtUtc, @StartedAtUtc,
                        @EndedAtUtc, @WinnerAllianceId, @Revision);
            """;
        Add(command, "@WarId", war.WarId);
        Add(command, "@AttackerAllianceId", war.AttackerAllianceId.Value);
        Add(command, "@DefenderAllianceId", war.DefenderAllianceId.Value);
        Add(command, "@Status", war.Status.ToString());
        Add(command, "@DeclaredAtUtc", war.DeclaredAtUtc.UtcDateTime);
        Add(command, "@StartedAtUtc", war.StartedAtUtc.HasValue ? war.StartedAtUtc.Value.UtcDateTime : DBNull.Value);
        Add(command, "@EndedAtUtc", war.EndedAtUtc.HasValue ? war.EndedAtUtc.Value.UtcDateTime : DBNull.Value);
        Add(command, "@WinnerAllianceId", war.WinnerAllianceId.HasValue ? war.WinnerAllianceId.Value.Value : DBNull.Value);
        Add(command, "@Revision", war.Revision);
        command.ExecuteNonQuery();
        return war;
    }

    public AllianceWar? Get(Guid warId)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM dbo.AllianceWars WHERE WarId = @WarId;";
        Add(command, "@WarId", warId);
        using IDataReader reader = command.ExecuteReader();
        return reader.Read() ? ReadWar(reader) : null;
    }

    public IReadOnlyList<AllianceWar> ListActiveForAlliance(AllianceId allianceId)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT * FROM dbo.AllianceWars
            WHERE (AttackerAllianceId = @AllianceId OR DefenderAllianceId = @AllianceId)
              AND Status IN (@Declared, @Active)
            ORDER BY DeclaredAtUtc DESC;
            """;
        Add(command, "@AllianceId", allianceId.Value);
        Add(command, "@Declared", AllianceWarStatus.Declared.ToString());
        Add(command, "@Active", AllianceWarStatus.Active.ToString());
        using IDataReader reader = command.ExecuteReader();
        List<AllianceWar> results = new();
        while (reader.Read())
        {
            results.Add(ReadWar(reader));
        }
        return results;
    }

    public bool HasActiveWarBetween(AllianceId allianceA, AllianceId allianceB)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*) FROM dbo.AllianceWars
            WHERE Status IN (@Declared, @Active)
              AND ((AttackerAllianceId = @AllianceA AND DefenderAllianceId = @AllianceB)
                OR (AttackerAllianceId = @AllianceB AND DefenderAllianceId = @AllianceA));
            """;
        Add(command, "@Declared", AllianceWarStatus.Declared.ToString());
        Add(command, "@Active", AllianceWarStatus.Active.ToString());
        Add(command, "@AllianceA", allianceA.Value);
        Add(command, "@AllianceB", allianceB.Value);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    public Guid? GetDeclareReceipt(PlayerId actorPlayerId, string clientRequestId)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = "SELECT WarId FROM dbo.AllianceWarDeclareReceipts WHERE PlayerId = @PlayerId AND ClientRequestId = @ClientRequestId;";
        Add(command, "@PlayerId", actorPlayerId.Value);
        Add(command, "@ClientRequestId", clientRequestId);
        object? result = command.ExecuteScalar();
        return result is Guid guid ? guid : null;
    }

    public void SaveDeclareReceipt(PlayerId actorPlayerId, string clientRequestId, Guid warId)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            MERGE dbo.AllianceWarDeclareReceipts AS target
            USING (SELECT @PlayerId AS PlayerId, @ClientRequestId AS ClientRequestId) AS source
            ON target.PlayerId = source.PlayerId AND target.ClientRequestId = source.ClientRequestId
            WHEN MATCHED THEN UPDATE SET WarId = @WarId
            WHEN NOT MATCHED THEN INSERT (PlayerId, ClientRequestId, WarId) VALUES (@PlayerId, @ClientRequestId, @WarId);
            """;
        Add(command, "@PlayerId", actorPlayerId.Value);
        Add(command, "@ClientRequestId", clientRequestId);
        Add(command, "@WarId", warId);
        command.ExecuteNonQuery();
    }

    private static AllianceWar ReadWar(IDataReader reader) => new()
    {
        WarId = (Guid)reader["WarId"],
        AttackerAllianceId = new AllianceId((Guid)reader["AttackerAllianceId"]),
        DefenderAllianceId = new AllianceId((Guid)reader["DefenderAllianceId"]),
        Status = Enum.Parse<AllianceWarStatus>((string)reader["Status"]),
        DeclaredAtUtc = AsUtc((DateTime)reader["DeclaredAtUtc"]),
        StartedAtUtc = reader["StartedAtUtc"] is DBNull ? null : AsUtc((DateTime)reader["StartedAtUtc"]),
        EndedAtUtc = reader["EndedAtUtc"] is DBNull ? null : AsUtc((DateTime)reader["EndedAtUtc"]),
        WinnerAllianceId = reader["WinnerAllianceId"] is DBNull ? null : new AllianceId((Guid)reader["WinnerAllianceId"]),
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
