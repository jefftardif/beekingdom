using BeeKingdom.Colony.Models;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Colony.Registry;

public sealed class ColonyRegistry
{
    private readonly Dictionary<ColonyId, ColonyRecord> loaded = new();
    private readonly object sync = new();

    public void Register(ColonyRecord colony)
    {
        lock (sync)
        {
            loaded[colony.Profile.ColonyId] = colony;
        }
    }

    public bool TryGet(ColonyId colonyId, out ColonyRecord colony)
    {
        lock (sync)
        {
            return loaded.TryGetValue(colonyId, out colony!);
        }
    }

    public IReadOnlyList<ColonyRecord> GetLoaded()
    {
        lock (sync)
        {
            return loaded.Values.ToArray();
        }
    }
}
