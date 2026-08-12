using BeeKingdom.Colony.Models;
using BeeKingdom.Colony.Snapshots;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Colony.Repositories;

public interface IColonyRepository
{
    ColonyRecord Create(ColonyRecord colony);
    ColonyRecord? Get(ColonyId colonyId);
    ColonyRecord Save(ColonyRecord colony);
    IReadOnlyList<ColonyRecord> Query(ColonyQuery query);
    ColonySnapshot SaveSnapshot(ColonySnapshot snapshot);
    ColonySnapshot? GetLatestSnapshot(ColonyId colonyId);
}
