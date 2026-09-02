using System.Data;
using System.Text.Json;
using BeeKingdom.Alliance.Models;
using BeeKingdom.Persistence.Sql;
using BeeKingdom.Shared.Serialization;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Alliance.Repositories;

public sealed class SqlAllianceActivityRepository : IAllianceActivityRepository
{
    private static readonly JsonSerializerOptions JsonOptions = BeeJson.CreateDefaultOptions();
    private readonly SqlConnectionFactory connectionFactory;

    public SqlAllianceActivityRepository(SqlConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public AllianceActivityEvent Append(AllianceActivityEvent activity)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbTransaction transaction = connection.BeginTransaction();
        long sequence = NextSequence(connection, transaction, activity.AllianceId.Value);
        AllianceActivityEvent stored = activity with { Sequence = sequence };
        InsertEvent(connection, transaction, stored);
        transaction.Commit();
        return stored;
    }

    public AllianceActivityEvent AppendIdempotent(AllianceActivityEvent activity, string dedupeKey)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbTransaction transaction = connection.BeginTransaction();

        string key = $"{activity.AllianceId.Value:N}:{activity.Type}:{dedupeKey}";
        using IDbCommand lookup = connection.CreateCommand();
        lookup.Transaction = transaction;
        lookup.CommandText = "SELECT ActivityId FROM dbo.AllianceActivityDedupe WHERE DedupeKey = @DedupeKey;";
        Add(lookup, "@DedupeKey", key);
        object? existing = lookup.ExecuteScalar();
        if (existing is Guid existingId)
        {
            using IDbCommand fetch = connection.CreateCommand();
            fetch.Transaction = transaction;
            fetch.CommandText = "SELECT * FROM dbo.AllianceActivityEvents WHERE ActivityId = @ActivityId;";
            Add(fetch, "@ActivityId", existingId);
            using IDataReader reader = fetch.ExecuteReader();
            reader.Read();
            AllianceActivityEvent found = ReadEvent(reader);
            transaction.Commit();
            return found;
        }

        long sequence = NextSequence(connection, transaction, activity.AllianceId.Value);
        AllianceActivityEvent stored = activity with { Sequence = sequence };
        InsertEvent(connection, transaction, stored);

        using IDbCommand dedupeInsert = connection.CreateCommand();
        dedupeInsert.Transaction = transaction;
        dedupeInsert.CommandText = "INSERT INTO dbo.AllianceActivityDedupe (DedupeKey, ActivityId) VALUES (@DedupeKey, @ActivityId);";
        Add(dedupeInsert, "@DedupeKey", key);
        Add(dedupeInsert, "@ActivityId", stored.ActivityId);
        dedupeInsert.ExecuteNonQuery();

        transaction.Commit();
        return stored;
    }

    public AllianceActivityPage ListForAlliance(AllianceId allianceId, long? beforeSequence, int limit, AllianceActivityVisibility maxVisibility)
    {
        string[] allowed = Enum.GetValues<AllianceActivityVisibility>()
            .Where(v => (int)v <= (int)maxVisibility)
            .Select(v => v.ToString())
            .ToArray();
        return Query(allianceId, beforeSequence, limit, "AND Visibility IN (" + string.Join(",", allowed.Select((_, i) => $"@Visibility{i}")) + ")",
            command =>
            {
                for (int i = 0; i < allowed.Length; i++) Add(command, $"@Visibility{i}", allowed[i]);
            });
    }

    public AllianceActivityPage ListPublicForAlliance(AllianceId allianceId, long? beforeSequence, int limit)
        => Query(allianceId, beforeSequence, limit, "AND Visibility = @Visibility",
            command => Add(command, "@Visibility", AllianceActivityVisibility.Public.ToString()));

    private AllianceActivityPage Query(AllianceId allianceId, long? beforeSequence, int limit, string extraFilter, Action<IDbCommand> bindExtra)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        string sequenceFilter = beforeSequence.HasValue ? "AND Sequence < @BeforeSequence" : string.Empty;
        command.CommandText = $"""
            SELECT TOP (@Limit) * FROM dbo.AllianceActivityEvents
            WHERE AllianceId = @AllianceId {sequenceFilter} {extraFilter}
            ORDER BY Sequence DESC;
            """;
        Add(command, "@AllianceId", allianceId.Value);
        Add(command, "@Limit", Math.Clamp(limit, 1, 200));
        if (beforeSequence.HasValue) Add(command, "@BeforeSequence", beforeSequence.Value);
        bindExtra(command);
        using IDataReader reader = command.ExecuteReader();
        List<AllianceActivityEvent> items = new();
        while (reader.Read())
        {
            items.Add(ReadEvent(reader));
        }
        long? next = items.Count > 0 ? items[^1].Sequence : null;
        return new AllianceActivityPage(items, next);
    }

    private static long NextSequence(IDbConnection connection, IDbTransaction transaction, Guid allianceId)
    {
        using IDbCommand ensure = connection.CreateCommand();
        ensure.Transaction = transaction;
        ensure.CommandText = """
            IF NOT EXISTS (SELECT 1 FROM dbo.AllianceActivitySequences WHERE AllianceId = @AllianceId)
            BEGIN
                INSERT INTO dbo.AllianceActivitySequences (AllianceId, NextSequence) VALUES (@AllianceId, 1);
            END
            """;
        Add(ensure, "@AllianceId", allianceId);
        ensure.ExecuteNonQuery();

        using IDbCommand claim = connection.CreateCommand();
        claim.Transaction = transaction;
        claim.CommandText = """
            DECLARE @Sequence bigint;
            SELECT @Sequence = NextSequence FROM dbo.AllianceActivitySequences WITH (UPDLOCK, HOLDLOCK) WHERE AllianceId = @AllianceId;
            UPDATE dbo.AllianceActivitySequences SET NextSequence = @Sequence + 1 WHERE AllianceId = @AllianceId;
            SELECT @Sequence;
            """;
        Add(claim, "@AllianceId", allianceId);
        return (long)claim.ExecuteScalar()!;
    }

    private static void InsertEvent(IDbConnection connection, IDbTransaction transaction, AllianceActivityEvent activity)
    {
        using IDbCommand insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = """
            INSERT INTO dbo.AllianceActivityEvents
                (ActivityId, AllianceId, Type, OccurredAtUtc, ActorPlayerId, TargetPlayerId, RelatedAllianceId,
                 RelatedEntityId, Visibility, PayloadJson, Sequence)
            VALUES
                (@ActivityId, @AllianceId, @Type, @OccurredAtUtc, @ActorPlayerId, @TargetPlayerId, @RelatedAllianceId,
                 @RelatedEntityId, @Visibility, @PayloadJson, @Sequence);
            """;
        Add(insert, "@ActivityId", activity.ActivityId);
        Add(insert, "@AllianceId", activity.AllianceId.Value);
        Add(insert, "@Type", activity.Type.ToString());
        Add(insert, "@OccurredAtUtc", activity.OccurredAtUtc.UtcDateTime);
        Add(insert, "@ActorPlayerId", activity.ActorPlayerId.HasValue ? activity.ActorPlayerId.Value.Value : DBNull.Value);
        Add(insert, "@TargetPlayerId", activity.TargetPlayerId.HasValue ? activity.TargetPlayerId.Value.Value : DBNull.Value);
        Add(insert, "@RelatedAllianceId", activity.RelatedAllianceId.HasValue ? activity.RelatedAllianceId.Value.Value : DBNull.Value);
        Add(insert, "@RelatedEntityId", activity.RelatedEntityId.HasValue ? activity.RelatedEntityId.Value : DBNull.Value);
        Add(insert, "@Visibility", activity.Visibility.ToString());
        Add(insert, "@PayloadJson", activity.Payload is null ? DBNull.Value : JsonSerializer.Serialize(activity.Payload, JsonOptions));
        Add(insert, "@Sequence", activity.Sequence);
        insert.ExecuteNonQuery();
    }

    private static AllianceActivityEvent ReadEvent(IDataReader reader) => new()
    {
        ActivityId = (Guid)reader["ActivityId"],
        AllianceId = new AllianceId((Guid)reader["AllianceId"]),
        Type = Enum.Parse<AllianceActivityType>((string)reader["Type"]),
        OccurredAtUtc = AsUtc((DateTime)reader["OccurredAtUtc"]),
        ActorPlayerId = reader["ActorPlayerId"] is DBNull ? null : new PlayerId((Guid)reader["ActorPlayerId"]),
        TargetPlayerId = reader["TargetPlayerId"] is DBNull ? null : new PlayerId((Guid)reader["TargetPlayerId"]),
        RelatedAllianceId = reader["RelatedAllianceId"] is DBNull ? null : new AllianceId((Guid)reader["RelatedAllianceId"]),
        RelatedEntityId = reader["RelatedEntityId"] is DBNull ? null : (Guid)reader["RelatedEntityId"],
        Visibility = Enum.Parse<AllianceActivityVisibility>((string)reader["Visibility"]),
        Payload = reader["PayloadJson"] is DBNull ? null : JsonSerializer.Deserialize<AllianceActivityPayload>((string)reader["PayloadJson"], JsonOptions),
        Sequence = (long)reader["Sequence"]
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
