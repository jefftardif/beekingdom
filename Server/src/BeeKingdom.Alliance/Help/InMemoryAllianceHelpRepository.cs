namespace BeeKingdom.Alliance.Help;

// Used by the test suite and by non-SQL local dev environments (same role InMemoryChatRepository/
// InMemoryAllianceRepository play elsewhere in this codebase). Production runs Persistence:Provider
// =SqlServer (confirmed by the M043I-CL incident report) and uses SqlAllianceHelpRepository - this
// implementation does not persist across a process restart, which is fine for tests but means it is
// NOT the durable store certification requirement #19 (survives server restart) exercises; that
// requirement is proven against SqlAllianceHelpRepository/dbo.AllianceHelpRequests instead.
public sealed class InMemoryAllianceHelpRepository : IAllianceHelpRepository
{
    private readonly object gate = new();
    private readonly Dictionary<Guid, AllianceHelpRequest> requests = new();
    private readonly Dictionary<Guid, List<AllianceHelpContribution>> contributionsByRequest = new();

    public Task<AllianceHelpRequest?> TryCreateAsync(AllianceHelpRequest request, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            bool alreadyOpenForOperation = requests.Values.Any(existing =>
                existing.Status == AllianceHelpRequestStatus.Open
                && existing.RequestingPlayerId == request.RequestingPlayerId
                && string.Equals(existing.OperationCategory, request.OperationCategory, StringComparison.Ordinal)
                && string.Equals(existing.OperationTargetId, request.OperationTargetId, StringComparison.Ordinal));
            if (alreadyOpenForOperation) return Task.FromResult<AllianceHelpRequest?>(null);

            requests[request.HelpRequestId] = request;
            contributionsByRequest[request.HelpRequestId] = new List<AllianceHelpContribution>();
            return Task.FromResult<AllianceHelpRequest?>(request);
        }
    }

    public Task<AllianceHelpRequest?> GetAsync(Guid helpRequestId, CancellationToken cancellationToken = default)
    {
        lock (gate) return Task.FromResult(requests.TryGetValue(helpRequestId, out AllianceHelpRequest? value) ? value : null);
    }

    public Task<IReadOnlyList<AllianceHelpRequest>> ListOpenForAllianceAsync(Guid allianceId, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            IReadOnlyList<AllianceHelpRequest> result = requests.Values
                .Where(request => request.AllianceId.Value == allianceId && request.Status == AllianceHelpRequestStatus.Open)
                .OrderBy(request => request.CreatedAtUtc)
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task<AllianceHelpRequest?> GetOpenForPlayerOperationAsync(Guid requestingPlayerId, string operationCategory, string operationTargetId, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            AllianceHelpRequest? match = requests.Values.FirstOrDefault(request =>
                request.Status == AllianceHelpRequestStatus.Open
                && request.RequestingPlayerId.Value == requestingPlayerId
                && string.Equals(request.OperationCategory, operationCategory, StringComparison.Ordinal)
                && string.Equals(request.OperationTargetId, operationTargetId, StringComparison.Ordinal));
            return Task.FromResult(match);
        }
    }

    public Task<AllianceHelpContribution?> GetContributionAsync(Guid helpRequestId, Guid helperPlayerId, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            AllianceHelpContribution? match = contributionsByRequest.TryGetValue(helpRequestId, out List<AllianceHelpContribution>? list)
                ? list.FirstOrDefault(contribution => contribution.HelperPlayerId.Value == helperPlayerId)
                : null;
            return Task.FromResult(match);
        }
    }

    public Task<(bool Applied, string Code, AllianceHelpRequest? Request)> TryContributeAsync(
        Guid helpRequestId, long expectedRevision, AllianceHelpContribution contribution, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            if (!requests.TryGetValue(helpRequestId, out AllianceHelpRequest? request))
                return Task.FromResult((false, "not_found", (AllianceHelpRequest?)null));
            if (request.Status != AllianceHelpRequestStatus.Open)
                return Task.FromResult((false, "request_not_open", (AllianceHelpRequest?)request));
            if (request.Revision != expectedRevision)
                return Task.FromResult((false, "revision_conflict", (AllianceHelpRequest?)request));
            if (request.HelpCount >= request.MaxHelpCount)
                return Task.FromResult((false, "help_full", (AllianceHelpRequest?)request));

            List<AllianceHelpContribution> list = contributionsByRequest[helpRequestId];
            if (list.Any(existing => existing.HelperPlayerId.Value == contribution.HelperPlayerId.Value))
                return Task.FromResult((false, "already_helped", (AllianceHelpRequest?)request));

            list.Add(contribution);
            int newHelpCount = request.HelpCount + 1;
            AllianceHelpRequest updated = request with
            {
                HelpCount = newHelpCount,
                Revision = request.Revision + 1,
                Status = newHelpCount >= request.MaxHelpCount ? AllianceHelpRequestStatus.Completed : request.Status
            };
            requests[helpRequestId] = updated;
            return Task.FromResult((true, "help_applied", (AllianceHelpRequest?)updated));
        }
    }

    public Task<AllianceHelpRequest?> TryUpdateStatusAsync(Guid helpRequestId, long expectedRevision, AllianceHelpRequestStatus status, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            if (!requests.TryGetValue(helpRequestId, out AllianceHelpRequest? request) || request.Revision != expectedRevision)
                return Task.FromResult<AllianceHelpRequest?>(null);
            AllianceHelpRequest updated = request with { Status = status, Revision = request.Revision + 1 };
            requests[helpRequestId] = updated;
            return Task.FromResult<AllianceHelpRequest?>(updated);
        }
    }

    public Task CancelOpenRequestsForPlayerAsync(Guid allianceId, Guid playerId, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            foreach (Guid id in requests.Keys.ToList())
            {
                AllianceHelpRequest request = requests[id];
                if (request.AllianceId.Value == allianceId && request.RequestingPlayerId.Value == playerId && request.Status == AllianceHelpRequestStatus.Open)
                    requests[id] = request with { Status = AllianceHelpRequestStatus.Cancelled, Revision = request.Revision + 1 };
            }
            return Task.CompletedTask;
        }
    }

    public Task CancelAllOpenRequestsForAllianceAsync(Guid allianceId, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            foreach (Guid id in requests.Keys.ToList())
            {
                AllianceHelpRequest request = requests[id];
                if (request.AllianceId.Value == allianceId && request.Status == AllianceHelpRequestStatus.Open)
                    requests[id] = request with { Status = AllianceHelpRequestStatus.Cancelled, Revision = request.Revision + 1 };
            }
            return Task.CompletedTask;
        }
    }
}
