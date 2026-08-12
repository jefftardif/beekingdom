using BeeKingdom.Colony.Models;
using BeeKingdom.Colony.Snapshots;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Colony.Repositories;

public sealed class InMemoryColonyRepository : IColonyRepository
{
    private readonly Dictionary<ColonyId, ColonyRecord> coloniesById = new();
    private readonly Dictionary<ColonyId, List<ColonySnapshot>> snapshotsByColonyId = new();
    private readonly object sync = new();

    public ColonyRecord Create(ColonyRecord colony)
    {
        lock (sync)
        {
            coloniesById[colony.Profile.ColonyId] = colony;
            return colony;
        }
    }

    public ColonyRecord? Get(ColonyId colonyId)
    {
        lock (sync)
        {
            return coloniesById.TryGetValue(colonyId, out ColonyRecord? colony) ? colony : null;
        }
    }

    public ColonyRecord Save(ColonyRecord colony)
    {
        lock (sync)
        {
            coloniesById[colony.Profile.ColonyId] = colony;
            return colony;
        }
    }

    public IReadOnlyList<ColonyRecord> Query(ColonyQuery query)
    {
        lock (sync)
        {
            IEnumerable<ColonyRecord> values = coloniesById.Values;
            if (query.PlayerId.HasValue)
            {
                values = values.Where(colony => colony.Profile.PlayerId == query.PlayerId.Value);
            }

            if (query.Status.HasValue)
            {
                values = values.Where(colony => colony.Profile.Status == query.Status.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.HiveNameContains))
            {
                values = values.Where(colony => colony.Profile.HiveName.Contains(query.HiveNameContains, StringComparison.OrdinalIgnoreCase));
            }

            return values.OrderBy(colony => colony.Profile.CreationDate).ToArray();
        }
    }

    public ColonySnapshot SaveSnapshot(ColonySnapshot snapshot)
    {
        lock (sync)
        {
            if (!snapshotsByColonyId.TryGetValue(snapshot.ColonyId, out List<ColonySnapshot>? snapshots))
            {
                snapshots = new List<ColonySnapshot>();
                snapshotsByColonyId[snapshot.ColonyId] = snapshots;
            }

            snapshots.Add(snapshot);
            return snapshot;
        }
    }

    public ColonySnapshot? GetLatestSnapshot(ColonyId colonyId)
    {
        lock (sync)
        {
            return snapshotsByColonyId.TryGetValue(colonyId, out List<ColonySnapshot>? snapshots)
                ? snapshots.OrderByDescending(snapshot => snapshot.Revision).FirstOrDefault()
                : null;
        }
    }
}
