using System;

namespace BeeKingdom.Networking
{
    // Exact decimal predicates within the same explicit six-decimal candidate domain
    // as the server. No Unity dependency; malformed responses never become map contours.
    public static class TerritoryOutlineValidation
    {
        public static bool ValidResponse(RemoteTerritoryResponse r, Guid player, string world, string surface, string unit)
        {
            if (r==null || r.ContractVersion!="bk-alliance-territory-v1-candidate" || r.PlayerId!=player
                || player==Guid.Empty || r.AllianceId==Guid.Empty || r.WorldId!=world || r.SurfaceId!=surface || r.MembershipRevision<0
                || r.ServerTimeUtc==default(DateTimeOffset) || r.ServerTimeUtc.Offset!=TimeSpan.Zero || r.View==null) return false;
            var view=r.View;
            if (!view.Available) return view.Outline==null;
            if (view.Code=="territory_released") return view.Outline==null && view.Revision>=1;
            var o=view.Outline;
            if (view.Code!="territory_outline_available" || o==null || o.TerritoryId==Guid.Empty || o.AllianceId!=r.AllianceId
                || o.HeadquartersId==Guid.Empty || o.WorldId!=world || o.SurfaceId!=surface || o.CoordinateUnit!=unit
                || o.Revision<1 || view.Revision!=o.Revision || o.AllianceLevel<1 || o.HeadquartersLevel<1) return false;
            return ValidRing(o.Ring,o.Bounds) && PointSupported(o.HeadquartersPosition) && Contains(o.Ring,o.HeadquartersPosition);
        }
        public static bool ValidRing(RemoteTerritoryPoint[] ring,RemoteTerritoryBounds bounds)
        {
            if (ring==null || ring.Length<3 || ring.Length>1024 || bounds==null
                || bounds.MinX>=bounds.MaxX || bounds.MinY>=bounds.MaxY
                || !Coordinate(bounds.MinX) || !Coordinate(bounds.MinY) || !Coordinate(bounds.MaxX) || !Coordinate(bounds.MaxY)) return false;
            for(int i=0;i<ring.Length;i++)
            {
                var p=ring[i];
                if (!PointSupported(p) || p.X<bounds.MinX || p.X>bounds.MaxX || p.Y<bounds.MinY || p.Y>bounds.MaxY) return false;
                for(int j=0;j<i;j++) if(p.X==ring[j].X && p.Y==ring[j].Y) return false;
            }
            decimal area=0;
            for(int i=1;i<ring.Length-1;i++) area+=Cross(ring[0],ring[i],ring[i+1]);
            if(area==0) return false;
            for(int i=0;i<ring.Length;i++)
            {
                var a=ring[i];var b=ring[(i+1)%ring.Length];var c=ring[(i+2)%ring.Length];
                if(Cross(a,b,c)==0 && On(a,c,b)) return false;
                for(int j=i+1;j<ring.Length;j++)
                {
                    if(j==i+1 || (i==0 && j==ring.Length-1)) continue;
                    if(Intersects(a,b,ring[j],ring[(j+1)%ring.Length])) return false;
                }
            }
            return true;
        }
        private static bool Coordinate(decimal p) => Math.Abs(p)<=1000000m && decimal.Truncate(p*1000000m)==p*1000000m;
        private static bool PointSupported(RemoteTerritoryPoint p) => p!=null && Coordinate(p.X) && Coordinate(p.Y);
        private static decimal Cross(RemoteTerritoryPoint a,RemoteTerritoryPoint b,RemoteTerritoryPoint c) => (b.X-a.X)*(c.Y-a.Y)-(b.Y-a.Y)*(c.X-a.X);
        private static bool On(RemoteTerritoryPoint a,RemoteTerritoryPoint p,RemoteTerritoryPoint b) => p.X>=Math.Min(a.X,b.X)&&p.X<=Math.Max(a.X,b.X)&&p.Y>=Math.Min(a.Y,b.Y)&&p.Y<=Math.Max(a.Y,b.Y);
        private static bool Intersects(RemoteTerritoryPoint a,RemoteTerritoryPoint b,RemoteTerritoryPoint c,RemoteTerritoryPoint d)
        {
            decimal x=Cross(a,b,c),y=Cross(a,b,d),z=Cross(c,d,a),w=Cross(c,d,b);
            return (x==0&&On(a,c,b))||(y==0&&On(a,d,b))||(z==0&&On(c,a,d))||(w==0&&On(c,b,d))
                || (((x<0&&y>0)||(x>0&&y<0))&&((z<0&&w>0)||(z>0&&w<0)));
        }
        private static bool Contains(RemoteTerritoryPoint[] ring,RemoteTerritoryPoint p)
        {
            int winding=0;
            for(int i=0;i<ring.Length;i++)
            {
                var a=ring[i];var b=ring[(i+1)%ring.Length];decimal c=Cross(a,b,p);
                if(c==0&&On(a,p,b)) return true;
                if(a.Y<=p.Y&&b.Y>p.Y&&c>0) winding++; else if(a.Y>p.Y&&b.Y<=p.Y&&c<0) winding--;
            }
            return winding!=0;
        }
    }
}
