using BeeKingdom.Colony.Diagnostics;
using BeeKingdom.Colony.Models;
using BeeKingdom.Colony.Snapshots;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Colony;

public sealed class ColonyManager
{
    private readonly IColonyService service;

    public ColonyManager(IColonyService service)
    {
        this.service = service;
    }

    public ColonyDiagnostics Diagnostics => service.Diagnostics;
    public ColonyRecord CreateColony(CreateColonyRequest request) => service.CreateColony(request);
    public ColonyRecord LoadColony(ColonyId colonyId) => service.LoadColony(colonyId);
    public ColonySnapshot SaveColony(ColonyId colonyId, ColonySnapshotKind kind = ColonySnapshotKind.Full) => service.SaveColony(colonyId, kind);
    public ColonyRecord DeleteColony(ColonyId colonyId) => service.DeleteColony(colonyId);
    public IReadOnlyList<ColonyRecord> QueryColony(ColonyQuery query) => service.QueryColony(query);
    public ColonyRecord RenameColony(ColonyId colonyId, string hiveName) => service.RenameColony(colonyId, hiveName);
    public ColonyRecord SetColonyStatus(ColonyId colonyId, ColonyStatus status) => service.SetColonyStatus(colonyId, status);
    public ColonyStatistics GetColonyStatistics(ColonyId colonyId) => service.GetColonyStatistics(colonyId);
}
