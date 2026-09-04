using System.Collections.Concurrent;

namespace BeeKingdom.Alliance.Research;

// Non-SQL local/dev environments only - production runs Persistence:Provider=SqlServer (see
// SqlAllianceResearchRepository's class comment). Not durable across a process restart. A single
// per-alliance SemaphoreSlim stands in for the real SQL app-lock, giving the exact same "concurrent
// donations serialize" guarantee for tests/dev.
public sealed class InMemoryAllianceResearchRepository : IAllianceResearchRepository
{
    private readonly ConcurrentDictionary<Guid, AllianceResearchState> states = new();
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> locks = new();

    public async Task<AllianceResearchState> ExecuteAtomicallyAsync(Guid allianceId, Func<AllianceResearchState, AllianceResearchState> mutation, CancellationToken cancellationToken = default)
    {
        SemaphoreSlim gate = locks.GetOrAdd(allianceId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            AllianceResearchState current = states.GetValueOrDefault(allianceId) ?? AllianceResearchState.Empty(allianceId);
            AllianceResearchState updated = mutation(current);
            states[allianceId] = updated;
            return updated;
        }
        finally
        {
            gate.Release();
        }
    }

    public Task<AllianceResearchState?> ReadAsync(Guid allianceId, CancellationToken cancellationToken = default)
        => Task.FromResult(states.GetValueOrDefault(allianceId));
}
