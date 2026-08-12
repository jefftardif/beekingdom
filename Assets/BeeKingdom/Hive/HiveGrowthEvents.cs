using BeeKingdom.Core.Events;

namespace BeeKingdom.Hive
{
    public readonly struct ChamberPlanned : IHiveEvent
    {
        public string ChamberId { get; }
        public ChamberPlanned(string chamberId) { ChamberId = chamberId; }
    }

    public readonly struct ChamberConstructionStarted : IHiveEvent
    {
        public string ChamberId { get; }
        public string SiteId { get; }
        public ChamberConstructionStarted(string chamberId, string siteId) { ChamberId = chamberId; SiteId = siteId; }
    }

    public readonly struct ChamberCompleted : IHiveEvent
    {
        public string ChamberId { get; }
        public ChamberCompleted(string chamberId) { ChamberId = chamberId; }
    }

    public readonly struct HiveExpanded : IHiveEvent
    {
        public string ChamberId { get; }
        public HiveExpanded(string chamberId) { ChamberId = chamberId; }
    }

    public readonly struct TopologyChanged : IHiveEvent
    {
        public int Revision { get; }
        public TopologyChanged(int revision) { Revision = revision; }
    }
}
