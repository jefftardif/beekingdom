using BeeKingdom.Core.Events;

namespace BeeKingdom.Hive
{
    public readonly struct HiveCreated : IHiveEvent
    {
        public string HiveId { get; }
        public HiveCreated(string hiveId) { HiveId = hiveId; }
    }

    public readonly struct HiveLoaded : IHiveEvent
    {
        public string HiveId { get; }
        public HiveLoaded(string hiveId) { HiveId = hiveId; }
    }

    public readonly struct BeeAdded : IBeeEvent
    {
        public string HiveId { get; }
        public string BeeId { get; }
        public BeeAdded(string hiveId, string beeId) { HiveId = hiveId; BeeId = beeId; }
    }

    public readonly struct BeeRemoved : IBeeEvent
    {
        public string HiveId { get; }
        public string BeeId { get; }
        public BeeRemoved(string hiveId, string beeId) { HiveId = hiveId; BeeId = beeId; }
    }

    public readonly struct BuildingRegistered : IBuildingEvent
    {
        public string HiveId { get; }
        public string BuildingId { get; }
        public BuildingRegistered(string hiveId, string buildingId) { HiveId = hiveId; BuildingId = buildingId; }
    }

    public readonly struct HiveValidated : IHiveEvent
    {
        public string HiveId { get; }
        public bool IsValid { get; }
        public HiveValidated(string hiveId, bool isValid) { HiveId = hiveId; IsValid = isValid; }
    }
}
