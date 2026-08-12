namespace BeeKingdom.HiveOperations;

public sealed record CombatFormationReadinessSnapshot(
    Guid PlayerId, Guid HiveId, string ContractVersion, string DoctrineCatalogVersion,
    long Revision, string AvailabilityStatus,
    IReadOnlyDictionary<string, long?> Families,
    IReadOnlyList<string> UnclassifiedLegacyRoles);

public sealed class CombatFormationReadinessService
{
    public const string ContractVersion = "phase4-combat-formation-readiness-v1";

    public CombatFormationReadinessSnapshot FromAuthoritativeState(PlayerHiveState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.PlayerId == Guid.Empty || state.HiveId == Guid.Empty || state.Revision < 0)
            throw new ArgumentException("Invalid authoritative hive scope", nameof(state));

        // Null means not recorded; a present state is a recorded roster. Legacy roles are
        // deliberately left unclassified and are never converted implicitly.
        if (state.DoctrineRoster is { } roster)
        {
            var counts = CombatDoctrineService.Families.ToDictionary(f => f, f => (long?)roster.Counts.GetValueOrDefault(f), StringComparer.Ordinal);
            return new(state.PlayerId, state.HiveId, ContractVersion, CombatDoctrineService.CatalogVersion, roster.Revision, "recorded", counts,
                ["Soldats", "Gardiennes", "Eclaireuses"]);
        }
        return new(state.PlayerId, state.HiveId, ContractVersion, CombatDoctrineService.CatalogVersion,
            state.Revision, "not_recorded", new Dictionary<string, long?>(),
            ["Soldats", "Gardiennes", "Eclaireuses"]);
    }
}

public sealed class CombatFormationReadinessOptions
{
    public const string SectionName = "CombatFormationReadiness";
    public bool Enabled { get; set; }
}
