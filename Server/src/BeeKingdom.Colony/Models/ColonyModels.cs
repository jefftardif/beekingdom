using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Colony.Models;

public enum ColonyStatus
{
    Creating = 0,
    Active = 1,
    Sleeping = 2,
    Migrating = 3,
    Locked = 4,
    Deleted = 5
}

public enum ColonySnapshotKind
{
    Full = 0,
    Incremental = 1
}

public sealed record ColonyProfile(
    ColonyId ColonyId,
    PlayerId PlayerId,
    Guid WorldId,
    string HiveName,
    DateTimeOffset CreationDate,
    string CurrentSeason,
    int CurrentPopulation,
    BeeId QueenId,
    int ColonyLevel,
    int PrestigeLevel,
    ColonyStatus Status);

public sealed record ColonyStatistics(int Population, int Buildings, int Chambers, long Revision, DateTimeOffset UpdatedAtUtc);

public sealed record ColonySettings(string SavePolicy, string CompressionPolicy, string VersioningStrategy);

public sealed record ColonyHistoryEntry(DateTimeOffset OccurredAtUtc, string EventType, string Description);

public sealed record ColonyRecord(
    ColonyProfile Profile,
    ColonyStatistics Statistics,
    ColonySettings Settings,
    IReadOnlyList<ColonyHistoryEntry> History,
    long Revision);

public sealed record CreateColonyRequest(PlayerId PlayerId, Guid WorldId, string HiveName, BeeId QueenId);

public sealed record ColonyQuery(PlayerId? PlayerId = null, ColonyStatus? Status = null, string? HiveNameContains = null);
