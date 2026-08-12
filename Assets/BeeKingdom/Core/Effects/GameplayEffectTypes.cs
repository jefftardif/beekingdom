namespace BeeKingdom.Core.Effects
{
    public enum GameplayEffectType
    {
        Instant,
        Duration,
        Infinite,
        Periodic,
        Conditional,
        Aura,
        Global
    }

    public enum GameplayEffectState
    {
        Registered,
        Pending,
        Applied,
        Active,
        Refreshing,
        Expired,
        Removed,
        Cancelled,
        Suspended,
        Failed
    }
}
