using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Alliance.Territories;

public sealed record TerritoryMapOutline(Guid TerritoryId, Guid AllianceId, string WorldId,
    string SurfaceId, string CoordinateUnit, long Revision, TerritoryMapBounds Bounds,
    IReadOnlyList<TerritoryPoint> Ring, Guid HeadquartersId, TerritoryPoint HeadquartersPosition,
    int AllianceLevel, int HeadquartersLevel, bool HeadquartersOperational);
public sealed record TerritoryMapView(bool Available, string Code, long? Revision, TerritoryMapOutline? Outline);

/// <summary>
/// Read-only map projection from committed authority, ready for a future authorized
/// API/client adapter. No source => no outline. An inactive record is distinct from
/// an unavailable source. A non-operational building does not fabricate loss of land.
/// CoordinateUnit is the declared area unit's coordinate basis and MUST be supplied
/// separately by the map adapter; this projector does not infer it from area units.
/// </summary>
public sealed class TerritoryMapReader(DurableTerritoryAuthorityStore store)
{
    public TerritoryMapView Read(TerritoryCapacityReadRequest request, string coordinateUnit)
    {
        if (string.IsNullOrWhiteSpace(coordinateUnit)) return new(false,"coordinate_mapping_unavailable",null,null);
        var state = store.ReadState(request);
        return Project(state, request.MinimumRevision, coordinateUnit);
    }

    public static TerritoryMapView Project(TerritoryAuthorityState? state, long minimumRevision, string coordinateUnit)
    {
        if (string.IsNullOrWhiteSpace(coordinateUnit)) return new(false,"coordinate_mapping_unavailable",null,null);
        if (state is null) return new(false,"territory_unavailable",null,null);
        DurableTerritoryAuthorityStore.Validate(state);
        if (minimumRevision < 0 || state.Revision < minimumRevision)
            return new(false,"snapshot_older_than_requested",null,null);
        if (!state.Active) return new(true,"territory_released",state.Revision,null);
        var ring = state.Ring.ToArray();
        if (ring.Length > 1 && ring[0] == ring[^1]) ring = ring[..^1];
        return new(true,"territory_outline_available",state.Revision,
            new(state.TerritoryId,state.AllianceId,state.WorldId,state.SurfaceId,coordinateUnit,state.Revision,
                state.Bounds,Array.AsReadOnly(ring),state.Headquarters.BuildingId,state.Headquarters.Position,
                state.TerritorialLevel,state.Headquarters.Level,state.Headquarters.Operational));
    }
}
