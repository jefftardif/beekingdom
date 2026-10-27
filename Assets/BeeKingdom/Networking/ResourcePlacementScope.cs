namespace BeeKingdom.MapPlacement
{
    // Scope/version also accompanies an explicitly unplaced catalogue node.
    // No coordinates are invented for withdrawals.
    public sealed class ResourcePlacementScope
    {
        public string WorldId {get;set;} = string.Empty; public string SurfaceId {get;set;} = string.Empty;
        public string CoordinateUnit {get;set;} = string.Empty; public string TerrainRevision {get;set;} = string.Empty;
        public long OccupationRevision {get;set;}
    }
}
