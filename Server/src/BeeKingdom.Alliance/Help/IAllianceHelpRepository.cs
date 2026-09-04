namespace BeeKingdom.Alliance.Help;

public interface IAllianceHelpRepository
{
    // Fails (returns null) if the requesting player already has an OPEN request for the exact
    // same (category, targetId) - enforces "no repeated request button for the same active
    // operation" server-side, not just as a client-side UI convenience.
    Task<AllianceHelpRequest?> TryCreateAsync(AllianceHelpRequest request, CancellationToken cancellationToken = default);

    Task<AllianceHelpRequest?> GetAsync(Guid helpRequestId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AllianceHelpRequest>> ListOpenForAllianceAsync(Guid allianceId, CancellationToken cancellationToken = default);

    Task<AllianceHelpRequest?> GetOpenForPlayerOperationAsync(Guid requestingPlayerId, string operationCategory, string operationTargetId, CancellationToken cancellationToken = default);

    Task<AllianceHelpContribution?> GetContributionAsync(Guid helpRequestId, Guid helperPlayerId, CancellationToken cancellationToken = default);

    // Atomically: rejects if the request isn't Open, is already at MaxHelpCount, this helper
    // already has a contribution row, or the given expectedRevision is stale - all inside one
    // transaction/lock so concurrent "Aider" clicks from different members can never both succeed
    // past MaxHelpCount and a retried request for the same ClientRequestId can never double-insert
    // a contribution. Returns the updated request on success.
    Task<(bool Applied, string Code, AllianceHelpRequest? Request)> TryContributeAsync(
        Guid helpRequestId, long expectedRevision, AllianceHelpContribution contribution, CancellationToken cancellationToken = default);

    Task<AllianceHelpRequest?> TryUpdateStatusAsync(Guid helpRequestId, long expectedRevision, AllianceHelpRequestStatus status, CancellationToken cancellationToken = default);

    // Membership-lifecycle sweep (invariants 10/11): marks every OPEN request the player owns
    // within this alliance as Cancelled - called on leave/kick. History (the request row and any
    // contributions already recorded) is preserved, never deleted.
    Task CancelOpenRequestsForPlayerAsync(Guid allianceId, Guid playerId, CancellationToken cancellationToken = default);

    // Called on Alliance dissolve.
    Task CancelAllOpenRequestsForAllianceAsync(Guid allianceId, CancellationToken cancellationToken = default);
}
