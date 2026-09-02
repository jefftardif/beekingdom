using BeeKingdom.Alliance.Models;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Alliance.Repositories;

public sealed class InMemoryAllianceActivityRepository : IAllianceActivityRepository
{
    private readonly Dictionary<Guid, List<AllianceActivityEvent>> byAlliance = new();
    private readonly Dictionary<Guid, long> nextSequenceByAlliance = new();
    private readonly Dictionary<string, Guid> dedupeIndex = new(StringComparer.Ordinal);
    private readonly object sync = new();

    public AllianceActivityEvent Append(AllianceActivityEvent activity)
    {
        lock (sync)
        {
            long sequence = NextSequenceLocked(activity.AllianceId.Value);
            AllianceActivityEvent stored = activity with { Sequence = sequence };
            ListLocked(activity.AllianceId.Value).Add(stored);
            return stored;
        }
    }

    public AllianceActivityEvent AppendIdempotent(AllianceActivityEvent activity, string dedupeKey)
    {
        lock (sync)
        {
            string key = $"{activity.AllianceId.Value:N}:{activity.Type}:{dedupeKey}";
            if (dedupeIndex.TryGetValue(key, out Guid existingId))
            {
                return ListLocked(activity.AllianceId.Value).First(e => e.ActivityId == existingId);
            }

            long sequence = NextSequenceLocked(activity.AllianceId.Value);
            AllianceActivityEvent stored = activity with { Sequence = sequence };
            ListLocked(activity.AllianceId.Value).Add(stored);
            dedupeIndex[key] = stored.ActivityId;
            return stored;
        }
    }

    public AllianceActivityPage ListForAlliance(AllianceId allianceId, long? beforeSequence, int limit, AllianceActivityVisibility maxVisibility)
    {
        lock (sync)
        {
            IEnumerable<AllianceActivityEvent> query = ListLocked(allianceId.Value)
                .Where(e => (int)e.Visibility <= (int)maxVisibility);
            return Page(query, beforeSequence, limit);
        }
    }

    public AllianceActivityPage ListPublicForAlliance(AllianceId allianceId, long? beforeSequence, int limit)
    {
        lock (sync)
        {
            IEnumerable<AllianceActivityEvent> query = ListLocked(allianceId.Value)
                .Where(e => e.Visibility == AllianceActivityVisibility.Public);
            return Page(query, beforeSequence, limit);
        }
    }

    private static AllianceActivityPage Page(IEnumerable<AllianceActivityEvent> query, long? beforeSequence, int limit)
    {
        if (beforeSequence.HasValue)
        {
            query = query.Where(e => e.Sequence < beforeSequence.Value);
        }
        AllianceActivityEvent[] items = query.OrderByDescending(e => e.Sequence).Take(Math.Clamp(limit, 1, 200)).ToArray();
        long? next = items.Length > 0 ? items[^1].Sequence : null;
        return new AllianceActivityPage(items, next);
    }

    private List<AllianceActivityEvent> ListLocked(Guid allianceId)
    {
        if (!byAlliance.TryGetValue(allianceId, out List<AllianceActivityEvent>? list))
        {
            list = new List<AllianceActivityEvent>();
            byAlliance[allianceId] = list;
        }
        return list;
    }

    private long NextSequenceLocked(Guid allianceId)
    {
        long sequence = nextSequenceByAlliance.TryGetValue(allianceId, out long current) ? current : 1;
        nextSequenceByAlliance[allianceId] = sequence + 1;
        return sequence;
    }

    // M042-CL: see the identical note in InMemoryAllianceRepository - internal-only dump/restore
    // surface for DurableJsonAllianceActivityRepository. DumpAll returns every visibility level
    // (unlike the public List* methods, which filter), ordered by Sequence, so a restore replays
    // events in their original append order and NextSequenceLocked resumes correctly afterward.
    internal IReadOnlyList<AllianceActivityEvent> DumpAll(Guid allianceId)
    {
        lock (sync) return ListLocked(allianceId).OrderBy(e => e.Sequence).ToArray();
    }

    // Restore bypasses Append's sequence assignment - the event already carries its real,
    // previously-assigned Sequence from disk, and NextSequenceLocked is advanced to stay after it.
    internal void RestoreEvent(AllianceActivityEvent activity, string? dedupeKey)
    {
        lock (sync)
        {
            ListLocked(activity.AllianceId.Value).Add(activity);
            long next = nextSequenceByAlliance.TryGetValue(activity.AllianceId.Value, out long current) ? current : 1;
            if (activity.Sequence >= next) nextSequenceByAlliance[activity.AllianceId.Value] = activity.Sequence + 1;
            if (!string.IsNullOrEmpty(dedupeKey))
                dedupeIndex[$"{activity.AllianceId.Value:N}:{activity.Type}:{dedupeKey}"] = activity.ActivityId;
        }
    }
}
