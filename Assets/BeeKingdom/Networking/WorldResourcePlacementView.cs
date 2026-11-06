using System;
using System.Collections.Generic;
using System.Linq;
using BeeKingdom.MapPlacement;

namespace BeeKingdom.Networking
{
    // A single committed snapshot supplies world positions, click bounds and saved geometry.
    public sealed class WorldResourcePlacementView
    {
        private Dictionary<string,MapPlacementSnapshot> placements=new Dictionary<string,MapPlacementSnapshot>(StringComparer.Ordinal);
        private HashSet<string> catalogueIds=new HashSet<string>(StringComparer.Ordinal);
        public long Revision { get; private set; }
        public bool HasAcceptedSnapshot => Revision>0;
        public bool TryInstall(IReadOnlyList<RemoteWorldResourceNode> nodes,string world,string surface,string unit,out string reason)
        {
            reason="resource_placements_invalid";
            if(nodes==null||nodes.Count==0||nodes.Count>10000)return false;
            var next=new Dictionary<string,MapPlacementSnapshot>(StringComparer.Ordinal);long revision=0;string terrain=null;
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var node in nodes)
            {
                if(node==null || string.IsNullOrWhiteSpace(node.NodeId) || !seen.Add(node.NodeId))return false;
                if(node.PlacementStatus=="unplaced")
                {
                    var scope=node.PlacementScope;
                    if(node.Placement!=null || node.CanLaunch || scope==null || scope.WorldId!=world || scope.SurfaceId!=surface || scope.CoordinateUnit!=unit
                        || scope.OccupationRevision<=0 || string.IsNullOrWhiteSpace(scope.TerrainRevision))return false;
                    if(revision!=0&&(revision!=scope.OccupationRevision||terrain!=scope.TerrainRevision))return false;
                    revision=scope.OccupationRevision;terrain=scope.TerrainRevision;continue;
                }
                if(node.PlacementStatus!=null && node.PlacementStatus!="legacy" && node.PlacementStatus!="placed")return false;
                if(MapPlacementRules.Validate(node.Placement,"resource",node.NodeId,world,surface,unit)!="verified")return false;
                var p=node.Placement;
                if(revision!=0&&(revision!=p.OccupationRevision||terrain!=p.TerrainRevision))return false;
                if(node.PlacementScope is ResourcePlacementScope provided && (provided.WorldId!=world||provided.SurfaceId!=surface||provided.CoordinateUnit!=unit
                    ||provided.OccupationRevision!=p.OccupationRevision||provided.TerrainRevision!=p.TerrainRevision))return false;
                revision=p.OccupationRevision;terrain=p.TerrainRevision;
                next.Add(node.NodeId,MapPlacementRules.Copy(p));
            }
            if(!catalogueIds.IsSubsetOf(seen)){reason="resource_catalogue_incomplete";return false;}
            if(revision<Revision){reason="resource_placements_stale";return false;}
            if(revision==Revision&&Revision!=0)
            {
                if(!catalogueIds.SetEquals(seen)||next.Count!=placements.Count||next.Any(x=>!placements.TryGetValue(x.Key,out var old)||!Equal(old,x.Value)))
                {reason="resource_placements_revision_conflict";return false;}
                reason="unchanged";return true;
            }
            placements=next;catalogueIds=seen;Revision=revision;reason="installed";return true;
        }
        private static bool Equal(MapPlacementSnapshot a,MapPlacementSnapshot b)=>a.WorldId==b.WorldId&&a.SurfaceId==b.SurfaceId&&a.CoordinateUnit==b.CoordinateUnit&&a.PlacementId==b.PlacementId&&a.Revision==b.Revision&&a.TerrainRevision==b.TerrainRevision&&a.X==b.X&&a.Y==b.Y&&a.Left==b.Left&&a.Top==b.Top&&a.Right==b.Right&&a.Bottom==b.Bottom;
        public bool TryGet(string id,out MapPlacementSnapshot placement)
        {if(placements.TryGetValue(id,out var p)){placement=MapPlacementRules.Copy(p);return true;}placement=null;return false;}
        public MapPlacementSnapshot[] Save()=>placements.Values.OrderBy(p=>p.EntityId,StringComparer.Ordinal).Select(MapPlacementRules.Copy).ToArray();
        public bool Contains(string id)=>placements.ContainsKey(id);
    }
}
