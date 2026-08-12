using BeeKingdom.Core.Events;

namespace BeeKingdom.World
{
    public readonly struct WaterCollected : IGameplayEvent
    {
        public string SourceId { get; }
        public double Amount { get; }
        public WaterCollected(string sourceId, double amount) { SourceId = sourceId; Amount = amount; }
    }

    public readonly struct WaterSourceDepleted : IGameplayEvent
    {
        public string SourceId { get; }
        public WaterSourceDepleted(string sourceId) { SourceId = sourceId; }
    }

    public readonly struct HydrationDemandUpdated : IGameplayEvent
    {
        public string HiveId { get; }
        public double DailyDemand { get; }
        public HydrationDemandUpdated(string hiveId, double dailyDemand) { HiveId = hiveId; DailyDemand = dailyDemand; }
    }
}
