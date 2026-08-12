using System.Data;
using System.Text.Json;
using BeeKingdom.Colony.Models;
using BeeKingdom.Colony.Snapshots;
using BeeKingdom.Persistence.Sql;
using BeeKingdom.Shared.Serialization;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Colony.Repositories;

public sealed class SqlColonyRepository : IColonyRepository
{
    private static readonly JsonSerializerOptions JsonOptions = BeeJson.CreateDefaultOptions();
    private readonly SqlConnectionFactory connectionFactory;

    public SqlColonyRepository(SqlConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public ColonyRecord Create(ColonyRecord colony)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.Colonies
            (ColonyId, PlayerId, WorldId, HiveName, QueenId, CurrentSeason, CurrentPopulation, ColonyLevel, PrestigeLevel,
             Status, SavePolicy, CompressionPolicy, VersioningStrategy, StatisticsJson, HistoryJson, Revision, CreatedAtUtc)
            VALUES
            (@ColonyId, @PlayerId, @WorldId, @HiveName, @QueenId, @CurrentSeason, @CurrentPopulation, @ColonyLevel, @PrestigeLevel,
             @Status, @SavePolicy, @CompressionPolicy, @VersioningStrategy, @StatisticsJson, @HistoryJson, @Revision, @CreatedAtUtc);
            """;
        AddColonyParameters(command, colony);
        command.ExecuteNonQuery();
        return colony;
    }

    public ColonyRecord? Get(ColonyId colonyId)
    {
        return QuerySingle("WHERE ColonyId = @ColonyId", command => Add(command, "@ColonyId", colonyId.Value));
    }

    public ColonyRecord Save(ColonyRecord colony)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            UPDATE dbo.Colonies
            SET PlayerId = @PlayerId,
                WorldId = @WorldId,
                HiveName = @HiveName,
                QueenId = @QueenId,
                CurrentSeason = @CurrentSeason,
                CurrentPopulation = @CurrentPopulation,
                ColonyLevel = @ColonyLevel,
                PrestigeLevel = @PrestigeLevel,
                Status = @Status,
                SavePolicy = @SavePolicy,
                CompressionPolicy = @CompressionPolicy,
                VersioningStrategy = @VersioningStrategy,
                StatisticsJson = @StatisticsJson,
                HistoryJson = @HistoryJson,
                Revision = @Revision,
                CreatedAtUtc = @CreatedAtUtc,
                UpdatedAtUtc = SYSUTCDATETIME()
            WHERE ColonyId = @ColonyId;
            """;
        AddColonyParameters(command, colony);
        command.ExecuteNonQuery();
        return colony;
    }

    public IReadOnlyList<ColonyRecord> Query(ColonyQuery query)
    {
        List<string> filters = new();
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();

        if (query.PlayerId.HasValue)
        {
            filters.Add("PlayerId = @PlayerId");
            Add(command, "@PlayerId", query.PlayerId.Value.Value);
        }

        if (query.Status.HasValue)
        {
            filters.Add("Status = @Status");
            Add(command, "@Status", (int)query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.HiveNameContains))
        {
            filters.Add("HiveName LIKE @HiveNameContains");
            Add(command, "@HiveNameContains", $"%{query.HiveNameContains.Trim()}%");
        }

        string where = filters.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", filters);
        command.CommandText = SelectSql(where);
        using IDataReader reader = command.ExecuteReader();
        return ReadColonies(reader).OrderBy(colony => colony.Profile.CreationDate).ToArray();
    }

    public ColonySnapshot SaveSnapshot(ColonySnapshot snapshot)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.ColonySnapshots
            (SnapshotId, ColonyId, Kind, BaseRevision, Revision, CreatedAtUtc, Version, Payload, MetadataJson)
            VALUES
            (@SnapshotId, @ColonyId, @Kind, @BaseRevision, @Revision, @CreatedAtUtc, @Version, @Payload, @MetadataJson);
            """;
        AddSnapshotParameters(command, snapshot);
        command.ExecuteNonQuery();
        return snapshot;
    }

    public ColonySnapshot? GetLatestSnapshot(ColonyId colonyId)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP (1) SnapshotId, ColonyId, Kind, BaseRevision, Revision, CreatedAtUtc, Version, Payload, MetadataJson
            FROM dbo.ColonySnapshots
            WHERE ColonyId = @ColonyId
            ORDER BY Revision DESC;
            """;
        Add(command, "@ColonyId", colonyId.Value);
        using IDataReader reader = command.ExecuteReader();
        return reader.Read() ? ReadSnapshot(reader) : null;
    }

    private ColonyRecord? QuerySingle(string whereClause, Action<IDbCommand> configure)
    {
        using IDbConnection connection = connectionFactory.CreateConnection();
        connection.Open();
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = SelectSql(whereClause);
        configure(command);
        using IDataReader reader = command.ExecuteReader();
        return ReadColonies(reader).FirstOrDefault();
    }

    private static string SelectSql(string whereClause)
    {
        return $"""
            SELECT ColonyId, PlayerId, WorldId, HiveName, QueenId, CurrentSeason, CurrentPopulation, ColonyLevel, PrestigeLevel,
                   Status, SavePolicy, CompressionPolicy, VersioningStrategy, StatisticsJson, HistoryJson, Revision, CreatedAtUtc
            FROM dbo.Colonies
            {whereClause};
            """;
    }

    private static IReadOnlyList<ColonyRecord> ReadColonies(IDataReader reader)
    {
        List<ColonyRecord> colonies = new();
        while (reader.Read())
        {
            ColonyStatistics statistics = JsonSerializer.Deserialize<ColonyStatistics>(reader.GetString(13), JsonOptions)
                ?? throw new InvalidOperationException("Invalid colony statistics payload.");
            IReadOnlyList<ColonyHistoryEntry> history = JsonSerializer.Deserialize<IReadOnlyList<ColonyHistoryEntry>>(reader.GetString(14), JsonOptions)
                ?? throw new InvalidOperationException("Invalid colony history payload.");

            ColonyProfile profile = new(
                new ColonyId(reader.GetGuid(0)),
                new PlayerId(reader.GetGuid(1)),
                reader.GetGuid(2),
                reader.GetString(3),
                AsUtc(reader.GetDateTime(16)),
                reader.GetString(5),
                reader.GetInt32(6),
                new BeeId(reader.GetGuid(4)),
                reader.GetInt32(7),
                reader.GetInt32(8),
                (ColonyStatus)reader.GetInt32(9));
            ColonySettings settings = new(reader.GetString(10), reader.GetString(11), reader.GetString(12));
            colonies.Add(new ColonyRecord(profile, statistics, settings, history, reader.GetInt64(15)));
        }

        return colonies;
    }

    private static ColonySnapshot ReadSnapshot(IDataReader reader)
    {
        IReadOnlyDictionary<string, string> metadata = JsonSerializer.Deserialize<IReadOnlyDictionary<string, string>>(reader.GetString(8), JsonOptions)
            ?? throw new InvalidOperationException("Invalid snapshot metadata payload.");

        return new ColonySnapshot(
            reader.GetGuid(0),
            new ColonyId(reader.GetGuid(1)),
            (ColonySnapshotKind)reader.GetInt32(2),
            reader.GetInt64(3),
            reader.GetInt64(4),
            AsUtc(reader.GetDateTime(5)),
            reader.GetString(6),
            (byte[])reader.GetValue(7),
            metadata);
    }

    private static void AddColonyParameters(IDbCommand command, ColonyRecord colony)
    {
        Add(command, "@ColonyId", colony.Profile.ColonyId.Value);
        Add(command, "@PlayerId", colony.Profile.PlayerId.Value);
        Add(command, "@WorldId", colony.Profile.WorldId);
        Add(command, "@HiveName", colony.Profile.HiveName);
        Add(command, "@QueenId", colony.Profile.QueenId.Value);
        Add(command, "@CurrentSeason", colony.Profile.CurrentSeason);
        Add(command, "@CurrentPopulation", colony.Profile.CurrentPopulation);
        Add(command, "@ColonyLevel", colony.Profile.ColonyLevel);
        Add(command, "@PrestigeLevel", colony.Profile.PrestigeLevel);
        Add(command, "@Status", (int)colony.Profile.Status);
        Add(command, "@SavePolicy", colony.Settings.SavePolicy);
        Add(command, "@CompressionPolicy", colony.Settings.CompressionPolicy);
        Add(command, "@VersioningStrategy", colony.Settings.VersioningStrategy);
        Add(command, "@StatisticsJson", JsonSerializer.Serialize(colony.Statistics, JsonOptions));
        Add(command, "@HistoryJson", JsonSerializer.Serialize(colony.History, JsonOptions));
        Add(command, "@Revision", colony.Revision);
        Add(command, "@CreatedAtUtc", colony.Profile.CreationDate.UtcDateTime);
    }

    private static void AddSnapshotParameters(IDbCommand command, ColonySnapshot snapshot)
    {
        Add(command, "@SnapshotId", snapshot.SnapshotId);
        Add(command, "@ColonyId", snapshot.ColonyId.Value);
        Add(command, "@Kind", (int)snapshot.Kind);
        Add(command, "@BaseRevision", snapshot.BaseRevision);
        Add(command, "@Revision", snapshot.Revision);
        Add(command, "@CreatedAtUtc", snapshot.CreatedAtUtc.UtcDateTime);
        Add(command, "@Version", snapshot.Version);
        Add(command, "@Payload", snapshot.Payload);
        Add(command, "@MetadataJson", JsonSerializer.Serialize(snapshot.Metadata, JsonOptions));
    }

    private static DateTimeOffset AsUtc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static void Add(IDbCommand command, string name, object value)
    {
        IDbDataParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
