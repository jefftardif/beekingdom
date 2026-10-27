using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.MapPlacement
{
    public sealed class MapPlacementSnapshot
    {
        public string ContractVersion { get; set; } = "bk-map-placement-v1-candidate";
        public string PlacementId { get; set; }
        public string EntityId { get; set; }
        public string Kind { get; set; }
        public string WorldId { get; set; }
        public string SurfaceId { get; set; }
        public string CoordinateUnit { get; set; }
        public long Revision { get; set; }
        public string TerrainRevision { get; set; }
        public long OccupationRevision { get; set; }
        public double? X { get; set; }
        public double? Y { get; set; }
        public double? Left { get; set; }
        public double? Top { get; set; }
        public double? Right { get; set; }
        public double? Bottom { get; set; }
    }

    public readonly struct PlacementRect
    {
        public readonly double Left, Top, Right, Bottom;
        public PlacementRect(double left, double top, double right, double bottom)
        { Left = left; Top = top; Right = right; Bottom = bottom; }
        public bool Valid => MapPlacementRules.Finite(Left) && MapPlacementRules.Finite(Top)
            && MapPlacementRules.Finite(Right) && MapPlacementRules.Finite(Bottom) && Left < Right && Top < Bottom;
        public bool Contains(PlacementRect r) => r.Left >= Left && r.Top >= Top && r.Right <= Right && r.Bottom <= Bottom;
        public bool Contains(double x, double y) => x >= Left && x <= Right && y >= Top && y <= Bottom;
        public PlacementRect Expand(double margin) => new PlacementRect(Left-margin,Top-margin,Right+margin,Bottom+margin);
        public bool Conflicts(PlacementRect r, double gap) => !(Right+gap < r.Left || r.Right+gap < Left || Bottom+gap < r.Top || r.Bottom+gap < Top);
    }

    public static class MapPlacementRules
    {
        // Candidate numerical domain, not a new gameplay or ownership rule.
        public const double MaximumCoordinate = 1000000;
        public static bool Finite(double x) => !double.IsNaN(x) && !double.IsInfinity(x) && Math.Abs(x) <= MaximumCoordinate;
        private static bool Value(double? x) => x.HasValue && Finite(x.Value);
        public static string Validate(MapPlacementSnapshot p, string kind, string entity, string world, string surface, string unit)
        {
            if (p == null) return "placement_missing";
            if (p.ContractVersion != "bk-map-placement-v1-candidate" || string.IsNullOrWhiteSpace(p.PlacementId)
                || string.IsNullOrWhiteSpace(kind) || string.IsNullOrWhiteSpace(entity) || string.IsNullOrWhiteSpace(world)
                || string.IsNullOrWhiteSpace(surface) || string.IsNullOrWhiteSpace(unit)
                || p.Kind != kind || p.EntityId != entity || p.WorldId != world || p.SurfaceId != surface || p.CoordinateUnit != unit)
                return "placement_identity_or_scope_mismatch";
            if (p.Revision <= 0 || p.OccupationRevision <= 0 || string.IsNullOrWhiteSpace(p.TerrainRevision)) return "placement_revision_missing";
            if (!Value(p.X) || !Value(p.Y) || !Value(p.Left) || !Value(p.Top) || !Value(p.Right) || !Value(p.Bottom)) return "placement_geometry_missing_or_outside_domain";
            var r = Bounds(p);
            return r.Valid && r.Contains(p.X.Value,p.Y.Value) ? "verified" : "placement_bounds_invalid";
        }
        public static PlacementRect Bounds(MapPlacementSnapshot p) => new PlacementRect(p.Left.Value,p.Top.Value,p.Right.Value,p.Bottom.Value);
        // A single projection supplies both the drawn rectangle and its click target.
        public static PlacementRect Project(MapPlacementSnapshot p, double centerX, double centerY, double zoom, double width, double height)
        {
            if (!Finite(centerX) || !Finite(centerY) || !Finite(zoom) || zoom <= 0 || !Finite(width) || !Finite(height) || width <= 0 || height <= 0)
                throw new ArgumentException("projection_invalid");
            var r=Bounds(p);
            return new PlacementRect(width/2+(r.Left-centerX)*zoom,height/2+(r.Top-centerY)*zoom,
                width/2+(r.Right-centerX)*zoom,height/2+(r.Bottom-centerY)*zoom);
        }
        public static MapPlacementSnapshot Copy(MapPlacementSnapshot p) => new MapPlacementSnapshot {
            ContractVersion=p.ContractVersion,PlacementId=p.PlacementId,EntityId=p.EntityId,Kind=p.Kind,
            WorldId=p.WorldId,SurfaceId=p.SurfaceId,CoordinateUnit=p.CoordinateUnit,Revision=p.Revision,
            TerrainRevision=p.TerrainRevision,OccupationRevision=p.OccupationRevision,X=p.X,Y=p.Y,
            Left=p.Left,Top=p.Top,Right=p.Right,Bottom=p.Bottom };
    }

    public sealed class TerrainPlacementCoverage
    {
        public string WorldId { get; set; }
        public string SurfaceId { get; set; }
        public string CoordinateUnit { get; set; }
        public string Revision { get; set; }
        public bool VerifiedGeometry { get; set; }
        public PlacementRect WorldBounds { get; set; }
        // Per-family regions: water sites and flowers must not share a guessed admissibility rule.
        public IDictionary<string, IReadOnlyList<PlacementRect>> AllowedRegions { get; set; }
        public IDictionary<string,string> EntityTerrainFamilies { get; set; }
        public string TerrainFamily(string kind,string entity)
        {string family;return EntityTerrainFamilies!=null && EntityTerrainFamilies.TryGetValue(kind+":"+entity,out family)?family:kind;}
    }
    public sealed class PlacementRequest
    {
        public string EntityId { get; set; }
        public string Kind { get; set; }
        public IReadOnlyList<MapPlacementSnapshot> Candidates { get; set; }
    }
    public sealed class PlacementPlan
    {
        public string Code { get; }
        public string FailureEntityId { get; }
        private readonly MapPlacementSnapshot[] placements;
        public IReadOnlyList<MapPlacementSnapshot> Placements => Array.AsReadOnly(placements.Select(MapPlacementRules.Copy).ToArray());
        internal PlacementPlan(string code,IReadOnlyList<MapPlacementSnapshot> values,string failedEntity=null)
        {Code=code;FailureEntityId=failedEntity;placements=values.Select(MapPlacementRules.Copy).ToArray();}
    }
    public static class SharedPlacementPlanner
    {
        private static PlacementPlan Fail(string code,string entity=null) => new PlacementPlan(code,Array.Empty<MapPlacementSnapshot>(),entity);
        public static PlacementPlan Build(TerrainPlacementCoverage coverage,IReadOnlyList<MapPlacementSnapshot> fixedPlacements,
            IReadOnlyList<PlacementRequest> requests,double gap,long occupationRevision,IReadOnlyCollection<string> historicalFixedIdentities=null,bool packRegionEdges=false,Action<string,MapPlacementSnapshot,int> diagnostic=null,int packingOrder=0)
        {
            if (packingOrder<0 || packingOrder>3)return Fail("packing_order_invalid");
            if (coverage == null || fixedPlacements == null || requests == null || !MapPlacementRules.Finite(gap) || gap < 0 || occupationRevision <= 0) return Fail("invalid_input");
            if (!coverage.VerifiedGeometry || !coverage.WorldBounds.Valid || coverage.AllowedRegions == null || string.IsNullOrWhiteSpace(coverage.Revision)) return Fail("terrain_coverage_unknown");
            if (fixedPlacements.Count+requests.Count > 10000) return Fail("placement_budget_exceeded");
            var result = new List<MapPlacementSnapshot>(); var ids=new HashSet<string>(StringComparer.Ordinal);
            var placementIds=new HashSet<string>(StringComparer.Ordinal);
            void Trace(string code,MapPlacementSnapshot p){diagnostic?.Invoke(code,p==null?null:MapPlacementRules.Copy(p),result.Count);}
            bool Admit(MapPlacementSnapshot p,string kind,string entity,bool historicalFixed=false)
            {
                bool Reject(string code){Trace(code,p);return false;}
                if (MapPlacementRules.Validate(p,kind,entity,coverage.WorldId,coverage.SurfaceId,coverage.CoordinateUnit) != "verified"
                    || p.TerrainRevision != coverage.Revision || p.OccupationRevision != occupationRevision) return Reject("identity_revision_or_geometry");
                var rect=MapPlacementRules.Bounds(p); var clearance=rect.Expand(gap);
                IReadOnlyList<PlacementRect> regions;
                if (!coverage.WorldBounds.Contains(clearance)) return Reject("world_boundary");
                // Preserving a known occupied position does not certify its terrain for new objects.
                if (!historicalFixed && (!coverage.AllowedRegions.TryGetValue(coverage.TerrainFamily(kind,entity),out regions)
                    || regions == null || !regions.Any(r=>r.Valid && r.Contains(rect)))) return Reject("terrain_region");
                if(result.Any(other=>rect.Conflicts(MapPlacementRules.Bounds(other),gap)))return Reject("occupied");
                Trace("admitted",p);return true;
            }
            foreach(var p in fixedPlacements)
            {
                if(p==null || !ids.Add(p.Kind+":"+p.EntityId) || !placementIds.Add(p.PlacementId) || !Admit(p,p.Kind,p.EntityId,historicalFixedIdentities!=null && historicalFixedIdentities.Contains(p.Kind+":"+p.EntityId))) return Fail("fixed_occupation_conflict_or_unverified");
                result.Add(MapPlacementRules.Copy(p));
            }
            // Stable ordering independent of loaded chunk order. Failure commits nothing.
            // Place larger envelopes first to avoid consuming every large clearing with tiny icons.
            // Identity tie-breaker retains determinism across producer/chunk enumeration order.
            double Area(PlacementRequest request)
            {
                var p=request?.Candidates?.FirstOrDefault();
                if(p==null || !p.Left.HasValue || !p.Right.HasValue || !p.Top.HasValue || !p.Bottom.HasValue)return 0;
                double area=(p.Right.Value-p.Left.Value)*(p.Bottom.Value-p.Top.Value);
                return double.IsNaN(area)||double.IsInfinity(area)?0:Math.Max(0,area);
            }
            double Dimension(PlacementRequest r,bool minimum)
            {
                var p=r?.Candidates?.FirstOrDefault();if(p==null||!p.Left.HasValue||!p.Right.HasValue||!p.Top.HasValue||!p.Bottom.HasValue)return 0;
                double w=p.Right.Value-p.Left.Value,h=p.Bottom.Value-p.Top.Value;return minimum?Math.Min(w,h):Math.Max(w,h);
            }
            int FitCount(PlacementRequest r)
            {
                var p=r?.Candidates?.FirstOrDefault();if(p==null||!p.Left.HasValue||!p.Right.HasValue||!p.Top.HasValue||!p.Bottom.HasValue)return 0;
                if(!coverage.AllowedRegions.TryGetValue(coverage.TerrainFamily(r.Kind,r.EntityId),out var rs)||rs==null)return 0;
                double w=p.Right.Value-p.Left.Value+2*gap,h=p.Bottom.Value-p.Top.Value+2*gap;
                return rs.Count(b=>b.Valid && b.Right-b.Left>=w && b.Bottom-b.Top>=h);
            }
            foreach(var request in requests.OrderBy(r=>packingOrder==3?FitCount(r):0)
                .ThenByDescending(r=>packingOrder==1?Dimension(r,true):packingOrder==2?Dimension(r,false):Area(r))
                .ThenBy(r=>r==null?"":r.Kind+":"+r.EntityId,StringComparer.Ordinal))
            {
                if(request==null || string.IsNullOrWhiteSpace(request.EntityId) || string.IsNullOrWhiteSpace(request.Kind)
                    || !ids.Add(request.Kind+":"+request.EntityId) || request.Candidates==null || request.Candidates.Count>256) return Fail("request_invalid_or_duplicate");
                MapPlacementSnapshot selected=null;
                Trace("request_begin",request.Candidates.FirstOrDefault());
                IEnumerable<MapPlacementSnapshot> EdgeCandidates()
                {
                    var basis=request.Candidates.FirstOrDefault();
                    if(basis==null || MapPlacementRules.Validate(basis,request.Kind,request.EntityId,coverage.WorldId,coverage.SurfaceId,coverage.CoordinateUnit)!="verified")yield break;
                    if(!coverage.AllowedRegions.TryGetValue(coverage.TerrainFamily(request.Kind,request.EntityId),out var regions)||regions==null)yield break;
                    var b=MapPlacementRules.Bounds(basis);double w=b.Right-b.Left,h=b.Bottom-b.Top;int budget=0;
                    foreach(var region in regions.OrderBy(r=>r.Top).ThenBy(r=>r.Left))
                    {
                        var xs=new[]{region.Left+gap}.Concat(result.Select(p=>p.Right.Value+gap+.25)).Where(x=>x>=region.Left+gap && x+w+gap<=region.Right).Distinct().OrderBy(x=>x).Take(128).ToArray();
                        var ys=new[]{region.Top+gap}.Concat(result.Select(p=>p.Bottom.Value+gap+.25)).Where(y=>y>=region.Top+gap && y+h+gap<=region.Bottom).Distinct().OrderBy(y=>y).Take(128).ToArray();
                        foreach(var y in ys)foreach(var x in xs)
                        {
                            if(++budget>4096)yield break;
                            var p=MapPlacementRules.Copy(basis);p.X=basis.X+x-b.Left;p.Y=basis.Y+y-b.Top;
                            p.Left=x;p.Top=y;p.Right=x+w;p.Bottom=y+h;yield return p;
                        }
                    }
                }
                var proposals=packRegionEdges ? EdgeCandidates().Concat(request.Candidates) : request.Candidates;
                foreach(var candidate in proposals)
                    if(candidate!=null && !placementIds.Contains(candidate.PlacementId) && Admit(candidate,request.Kind,request.EntityId)) {selected=candidate;break;}
                if(selected==null){Trace("exhausted",request.Candidates.FirstOrDefault());return Fail("no_admissible_distinct_placement",request.EntityId);}
                placementIds.Add(selected.PlacementId); result.Add(MapPlacementRules.Copy(selected));
            }
            return new PlacementPlan("planned",result.AsReadOnly());
        }
    }
}
