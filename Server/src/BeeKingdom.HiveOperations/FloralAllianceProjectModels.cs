namespace BeeKingdom.HiveOperations;

/// <summary>
/// Pilot-only cooperative goal tuning. No gameplay claim until the founder
/// approves final thresholds; disabled by default in every environment.
/// </summary>
public sealed class FloralAllianceProjectOptions
{
    public const string SectionName = "FloralAllianceProjects";
    public bool Enabled { get; set; }
    /// <summary>
    /// No new alliance projects are created while disabled, but already
    /// launched civil flights may still complete their cooperative deliveries
    /// and grant the earned project XP in one durable SQL transaction.
    /// Explicitly OFF by default. Keep the project SQL schema and membership.
    /// </summary>
    public bool DrainExistingDeliveries { get; set; }
    public bool CanApplyDeliveries => Enabled || DrainExistingDeliveries;

    public long GoalPoints { get; set; } = 100;
    public int RequiredDistinctMembers { get; set; } = 2;
    public long MinimumPointsPerMember { get; set; } = 5;
    public long LevelTwoExperience { get; set; } = 100;
    public long LevelThreeExperience { get; set; } = 300;

    public void Validate()
    {
        if (GoalPoints is < 2 or > 1_000_000_000
            || RequiredDistinctMembers is < 2 or > 200
            || MinimumPointsPerMember is < 1 or > 1_000_000_000
            || LevelTwoExperience < 1
            || LevelThreeExperience <= LevelTwoExperience
            || LevelThreeExperience > 1_000_000_000_000)
            throw new InvalidDataException("Invalid floral alliance project settings.");
    }

    public int Level(long experience) =>
        experience >= LevelThreeExperience ? 3
        : experience >= LevelTwoExperience ? 2 : 1;

    public int ActiveSlotsForLevel(int level) => level >= 2 ? 2 : 1;
}

public sealed record FloralAllianceProgression(
    Guid AllianceId, long Revision, int Level, long Experience,
    long CompletedProjects);

public sealed record FloralAllianceProject(
    Guid ProjectId, Guid AllianceId, string WorldId, string SiteId,
    Guid CreatedByPlayerId, DateTimeOffset CreatedAtUtc,
    long Revision, long GoalPoints, int RequiredDistinctMembers,
    long MinimumPointsPerMember, long Points, int QualifiedMembers,
    string Status, DateTimeOffset? CompletedAtUtc);

public sealed record FloralAllianceProjectSnapshot(
    string ContractVersion, DateTimeOffset ServerTimeUtc,
    FloralAllianceProject Project,
    FloralAllianceProgression Progression, bool IsCompleted);

public sealed record CreateFloralAllianceProjectRequest(
    string SiteId, long ExpectedProgressionRevision, string IdempotencyKey);

public sealed record FloralAllianceProjectCreateResult(
    bool Succeeded, string Code, FloralAllianceProjectSnapshot? Snapshot);

public interface IFloralAllianceProjectService
{
    Task<FloralAllianceProjectCreateResult> CreateAsync(
        Guid playerId, Guid allianceId, CreateFloralAllianceProjectRequest request,
        CancellationToken ct = default);
    Task<FloralAllianceProjectSnapshot> ReadAsync(
        Guid playerId, Guid allianceId, Guid projectId,
        CancellationToken ct = default);
    Task<IReadOnlyList<FloralAllianceProjectSnapshot>> ListAsync(
        Guid playerId, Guid allianceId, CancellationToken ct = default);
}
