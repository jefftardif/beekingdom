namespace BeeKingdom.HiveOperations;

public sealed record CivilExpeditionDueBatch(
    int Inspected, int Settled, IReadOnlyList<Guid> RetryMissionIds);

/// <summary>
/// Bounded sweep of SQL-indexed due missions. Faults are isolated per mission,
/// and a failed settlement remains on the durable due index for later retry.
/// Both HTTP GET and this processor reuse CivilExpeditionService.ReadAsync.
/// </summary>
public sealed class CivilExpeditionDueProcessor(
    ICivilExpeditionDueIndex due,
    CivilExpeditionService expeditions,
    IServerClock clock)
{
    public async Task<CivilExpeditionDueBatch> ProcessAsync(
        int limit = 100, CancellationToken ct = default)
    {
        if (limit is < 1 or > 200)
            throw new ArgumentOutOfRangeException(nameof(limit));
        IReadOnlyList<CivilDueExpedition> flights = await due.DueAsync(
            clock.UtcNow.ToUniversalTime(), limit, ct);
        int settled = 0;
        var retries = new List<Guid>();
        foreach (CivilDueExpedition flight in flights)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                CivilExpeditionSnapshot snapshot = await expeditions.ReadAsync(
                    flight.PlayerId, flight.HiveId, ct);
                if (snapshot.Active is null) settled++;
                else retries.Add(flight.MissionId);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                retries.Add(flight.MissionId);
            }
        }
        return new(flights.Count, settled, retries);
    }
}
