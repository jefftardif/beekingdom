namespace BeeKingdom.HiveOperations;

public sealed record HiveProgressionSnapshot(
    Guid PlayerId, Guid HiveId, Guid WorldId, Guid GameServerId,
    long BuildingRevision, long ArmyRevision, string CatalogVersion,
    IReadOnlyDictionary<string, int> BuildingLevels,
    IReadOnlyDictionary<string, long> TroopCounts);

public static class HiveProgressionSnapshotFactory
{
    public static HiveProgressionSnapshot FromAuthoritativeState(PlayerHiveState state, Guid worldId, Guid gameServerId, long armyRevision, IReadOnlyDictionary<string, long>? troopCounts = null, string catalogVersion = "server-v1")
    {
        if (state is null || state.PlayerId == Guid.Empty || state.HiveId == Guid.Empty || state.Revision < 0 || worldId == Guid.Empty || gameServerId == Guid.Empty || armyRevision < 0 || string.IsNullOrWhiteSpace(catalogVersion)) throw new ArgumentException("Invalid authoritative scope");
        if (state.BuildingLevels is null || state.BuildingLevels.Any(x => string.IsNullOrWhiteSpace(x.Key) || x.Value < 0) || troopCounts?.Any(x => string.IsNullOrWhiteSpace(x.Key) || x.Value < 0) == true) throw new InvalidDataException("Invalid progression value");
        return new(state.PlayerId, state.HiveId, worldId, gameServerId, state.Revision, armyRevision,
            catalogVersion, new Dictionary<string, int>(state.BuildingLevels, StringComparer.Ordinal),
            new Dictionary<string, long>(troopCounts ?? new Dictionary<string, long>(), StringComparer.Ordinal));
    }
}

public sealed class HiveProgressionSnapshotOptions
{
    public const string SectionName = "HiveProgressionSnapshot";
    public bool Enabled { get; set; }
}
