namespace BeeKingdom.Alliance.Research;

public interface IAllianceResearchRepository
{
    // Real exclusive lock per AllianceId for the duration of the mutation - two members donating
    // "at nearly the same time" (the mission's own concurrency example) simply serialize through
    // this, never racing on a shared read-modify-write. Mirrors IHiveStateRepository.ExecuteAtomicallyAsync
    // exactly (same proven pattern for this codebase's highest-contention state).
    Task<AllianceResearchState> ExecuteAtomicallyAsync(Guid allianceId, Func<AllianceResearchState, AllianceResearchState> mutation, CancellationToken cancellationToken = default);

    Task<AllianceResearchState?> ReadAsync(Guid allianceId, CancellationToken cancellationToken = default);

    // M054-CL: enumeration surface for the one-time Royal Seals wallet migration
    // (RoyalSealsMigrationService) - every other method here is scoped to a single already-known
    // AllianceId, but migrating legacy AllianceCurrencyBalance values into the player wallet
    // requires visiting every Alliance's research row at least once.
    Task<IReadOnlyList<Guid>> ListAllAllianceIdsAsync(CancellationToken cancellationToken = default);
}
