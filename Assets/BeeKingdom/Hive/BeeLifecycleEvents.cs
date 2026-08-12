using BeeKingdom.Core.Events;

namespace BeeKingdom.Hive
{
    public readonly struct BeeBorn : IBeeEvent
    {
        public string BeeId { get; }
        public string HiveId { get; }
        public BeeBorn(string beeId, string hiveId) { BeeId = beeId; HiveId = hiveId; }
    }

    public readonly struct BeeStageChanged : IBeeEvent
    {
        public string BeeId { get; }
        public BeeLifecycleStage Stage { get; }
        public BeeStageChanged(string beeId, BeeLifecycleStage stage) { BeeId = beeId; Stage = stage; }
    }

    public readonly struct BeeRoleChanged : IBeeEvent
    {
        public string BeeId { get; }
        public BeeLifecycleRole Role { get; }
        public BeeRoleChanged(string beeId, BeeLifecycleRole role) { BeeId = beeId; Role = role; }
    }

    public readonly struct BeeAged : IBeeEvent
    {
        public string BeeId { get; }
        public double AgeSeconds { get; }
        public BeeAged(string beeId, double ageSeconds) { BeeId = beeId; AgeSeconds = ageSeconds; }
    }

    public readonly struct BeeDied : IBeeEvent
    {
        public string BeeId { get; }
        public BeeMortalityCause Cause { get; }
        public BeeDied(string beeId, BeeMortalityCause cause) { BeeId = beeId; Cause = cause; }
    }
}
