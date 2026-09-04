namespace BeeKingdom.Alliance.Research;

public interface IAllianceResearchRepository
{
    // Real exclusive lock per AllianceId for the duration of the mutation - two members donating
    // "at nearly the same time" (the mission's own concurrency example) simply serialize through
    // this, never racing on a shared read-modify-write. Mirrors IHiveStateRepository.ExecuteAtomicallyAsync
    // exactly (same proven pattern for this codebase's highest-contention state).
    Task<AllianceResearchState> ExecuteAtomicallyAsync(Guid allianceId, Func<AllianceResearchState, AllianceResearchState> mutation, CancellationToken cancellationToken = default);

    Task<AllianceResearchState?> ReadAsync(Guid allianceId, CancellationToken cancellationToken = default);
}
