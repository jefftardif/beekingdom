using BeeKingdom.Alliance.Models;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Alliance.Repositories;

// M042-CL: same pattern as DurableJsonAllianceRepository - delegates to an inner
// InMemoryAllianceActivityRepository, persists one JSON file per alliance
// ({root}/{allianceId:N}.json). The dedupeKey used by AppendIdempotent isn't part of
// AllianceActivityEvent itself, so it's carried alongside each event on disk (ActivityRecord)
// specifically so a restored event still participates in dedupe after a restart - otherwise a
// retried gameplay-event publish right after a restart could double-append.
public sealed class DurableJsonAllianceActivityRepository : IAllianceActivityRepository
{
    private sealed class ActivityRecord
    {
        public AllianceActivityEvent? Event { get; set; }
        public string? DedupeKey { get; set; }
    }

    private readonly InMemoryAllianceActivityRepository inner = new();
    private readonly string root;
    private readonly object writeLock = new();

    // Tracks the dedupeKey used for each ActivityId in this process so a later persist can
    // include it - AllianceActivityEvent itself doesn't carry it (by design, it's not part of
    // the domain event's public shape).
    private readonly Dictionary<Guid, string> dedupeKeyByActivityId = new();

    public DurableJsonAllianceActivityRepository(string rootDirectory)
    {
        root = rootDirectory;
        LoadAll();
    }

    private void LoadAll()
    {
        foreach (string file in DurableJsonFileIo.EnumerateJsonFiles(root))
        {
            List<ActivityRecord>? records = DurableJsonFileIo.ReadIfExists<List<ActivityRecord>>(file);
            if (records == null) continue;
            foreach (ActivityRecord record in records.OrderBy(r => r.Event?.Sequence ?? 0))
            {
                if (record.Event == null) continue;
                inner.RestoreEvent(record.Event, record.DedupeKey);
                if (!string.IsNullOrEmpty(record.DedupeKey)) dedupeKeyByActivityId[record.Event.ActivityId] = record.DedupeKey;
            }
        }
    }

    private void Persist(Guid allianceId)
    {
        lock (writeLock)
        {
            List<ActivityRecord> records = inner.DumpAll(allianceId)
                .Select(e => new ActivityRecord { Event = e, DedupeKey = dedupeKeyByActivityId.GetValueOrDefault(e.ActivityId) })
                .ToList();
            DurableJsonFileIo.WriteAtomic(Path.Combine(root, allianceId.ToString("N") + ".json"), records);
        }
    }

    public AllianceActivityEvent Append(AllianceActivityEvent activity)
    {
        AllianceActivityEvent result = inner.Append(activity);
        Persist(activity.AllianceId.Value);
        return result;
    }

    public AllianceActivityEvent AppendIdempotent(AllianceActivityEvent activity, string dedupeKey)
    {
        AllianceActivityEvent result = inner.AppendIdempotent(activity, dedupeKey);
        lock (writeLock) dedupeKeyByActivityId[result.ActivityId] = dedupeKey;
        Persist(activity.AllianceId.Value);
        return result;
    }

    public AllianceActivityPage ListForAlliance(AllianceId allianceId, long? beforeSequence, int limit, AllianceActivityVisibility maxVisibility)
        => inner.ListForAlliance(allianceId, beforeSequence, limit, maxVisibility);

    public AllianceActivityPage ListPublicForAlliance(AllianceId allianceId, long? beforeSequence, int limit)
        => inner.ListPublicForAlliance(allianceId, beforeSequence, limit);
}
