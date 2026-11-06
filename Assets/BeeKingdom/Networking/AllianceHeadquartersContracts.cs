using System;

namespace BeeKingdom.Networking
{
    public sealed class RemoteHeadquartersRevision
    {
        public Guid AllianceId { get; set; }
        public Guid BuildingId { get; set; }
        public string WorldId { get; set; }
        public string SurfaceId { get; set; }
        public long IdentityGeneration { get; set; }
        public long Membership { get; set; }
        public long Scope { get; set; }
        public long Progression { get; set; }
        public long Placement { get; set; }
        public long Territory { get; set; }
        public long Headquarters { get; set; }
        public long WorkReceipts { get; set; }
        public long WorkUnits { get; set; }
        public bool Cancelled { get; set; }

        public bool AtLeast(RemoteHeadquartersRevision floor)
        {
            return AllianceId == floor.AllianceId && WorldId == floor.WorldId && SurfaceId == floor.SurfaceId
                && IdentityGeneration >= floor.IdentityGeneration && Membership >= floor.Membership
                && Scope >= floor.Scope && Progression >= floor.Progression
                && (IdentityGeneration > floor.IdentityGeneration || (BuildingId == floor.BuildingId
                    && Placement >= floor.Placement && Territory >= floor.Territory && Headquarters >= floor.Headquarters
                    && WorkReceipts >= floor.WorkReceipts && WorkUnits >= floor.WorkUnits && (!floor.Cancelled || Cancelled)));
        }

        public RemoteHeadquartersRevision Copy() => (RemoteHeadquartersRevision)MemberwiseClone();
    }

    public sealed class RemoteHeadquartersConstruction
    {
        public string State { get; set; }
        public Guid BuildingId { get; set; }
        public int TargetLevel { get; set; }
        public string PolicyVersion { get; set; }
        public long CompletedWork { get; set; }
        public long RequiredWork { get; set; }
    }

    public sealed class RemoteHeadquartersDetails
    {
        public int? HeadquartersLevel { get; set; }
        public bool? HeadquartersOperational { get; set; }
        public bool Available { get; set; }
        public string Code { get; set; }
        public RemoteHeadquartersRevision Revision { get; set; }
        public string WorldId { get; set; }
        public string SurfaceId { get; set; }
        public string AreaUnit { get; set; }
        public decimal? OccupiedArea { get; set; }
        public decimal? MaximumArea { get; set; }
        public decimal? RemainingArea { get; set; }
        public int? MaximumFloralInstallations { get; set; }
        public RemoteHeadquartersConstruction Construction { get; set; }
    }

    public sealed class RemoteHeadquartersResponse
    {
        public const string Version = "bk-alliance-headquarters-v2-candidate";
        public string ContractVersion { get; set; }
        public Guid PlayerId { get; set; }
        public DateTimeOffset ServerTimeUtc { get; set; }
        public RemoteHeadquartersDetails Details { get; set; }
    }
}
