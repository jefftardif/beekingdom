using System;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Networking
{
    public sealed class RemoteTerritoryPoint { public decimal X {get;set;} public decimal Y {get;set;} }
    public sealed class RemoteTerritoryBounds { public decimal MinX {get;set;} public decimal MinY {get;set;} public decimal MaxX {get;set;} public decimal MaxY {get;set;} }
    public sealed class RemoteTerritoryOutline
    {
        public Guid TerritoryId {get;set;} public Guid AllianceId {get;set;}
        public string WorldId {get;set;} public string SurfaceId {get;set;} public string CoordinateUnit {get;set;}
        public long Revision {get;set;} public RemoteTerritoryBounds Bounds {get;set;}
        public RemoteTerritoryPoint[] Ring {get;set;} public Guid HeadquartersId {get;set;}
        public RemoteTerritoryPoint HeadquartersPosition {get;set;}
        public int AllianceLevel {get;set;} public int HeadquartersLevel {get;set;}
        public bool HeadquartersOperational {get;set;}
    }
    public sealed class RemoteTerritoryMapView
    {
        public bool Available {get;set;} public string Code {get;set;}
        public long? Revision {get;set;} public RemoteTerritoryOutline Outline {get;set;}
    }
    public sealed class RemoteTerritoryResponse
    {
        public string ContractVersion {get;set;} public Guid PlayerId {get;set;} public Guid AllianceId {get;set;}
        public string WorldId {get;set;} public string SurfaceId {get;set;}
        public long MembershipRevision {get;set;} public DateTimeOffset ServerTimeUtc {get;set;}
        public RemoteTerritoryMapView View {get;set;}
    }

}
