using BeeKingdom.Core.Events;

namespace BeeKingdom.Core.Abilities
{
    public readonly struct AbilityRegistered : IGameplayEvent { public string AbilityId { get; } public AbilityRegistered(string abilityId) { AbilityId = abilityId; } }
    public readonly struct AbilityRequested : IGameplayEvent { public GameplayAbilityHandle Handle { get; } public AbilityRequested(GameplayAbilityHandle handle) { Handle = handle; } }
    public readonly struct AbilityValidated : IGameplayEvent { public GameplayAbilityHandle Handle { get; } public AbilityValidated(GameplayAbilityHandle handle) { Handle = handle; } }
    public readonly struct AbilityActivated : IGameplayEvent { public GameplayAbilityHandle Handle { get; } public AbilityActivated(GameplayAbilityHandle handle) { Handle = handle; } }
    public readonly struct AbilityCompleted : IGameplayEvent { public GameplayAbilityHandle Handle { get; } public AbilityCompleted(GameplayAbilityHandle handle) { Handle = handle; } }
    public readonly struct AbilityCancelled : IGameplayEvent { public GameplayAbilityHandle Handle { get; } public AbilityCancelled(GameplayAbilityHandle handle) { Handle = handle; } }
    public readonly struct AbilityInterrupted : IGameplayEvent { public GameplayAbilityHandle Handle { get; } public AbilityInterrupted(GameplayAbilityHandle handle) { Handle = handle; } }
    public readonly struct AbilityFailed : IGameplayEvent { public GameplayAbilityHandle Handle { get; } public string Reason { get; } public AbilityFailed(GameplayAbilityHandle handle, string reason) { Handle = handle; Reason = reason; } }
}
