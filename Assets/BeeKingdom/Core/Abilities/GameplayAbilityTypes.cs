namespace BeeKingdom.Core.Abilities
{
    public enum GameplayAbilityState
    {
        Registered,
        Available,
        Requested,
        Validated,
        Activated,
        Executing,
        Completed,
        Cancelled,
        Interrupted,
        Failed
    }

    public enum GameplayAbilityActivationSource
    {
        Local,
        Server,
        LiveOps,
        WorldEvent
    }
}
