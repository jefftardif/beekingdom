using BeeKingdom.Core.Events;

namespace BeeKingdom.Core.Effects
{
    public readonly struct EffectRegistered : IGameplayEvent { public string EffectId { get; } public EffectRegistered(string effectId) { EffectId = effectId; } }
    public readonly struct EffectApplied : IGameplayEvent { public GameplayEffectHandle Handle { get; } public EffectApplied(GameplayEffectHandle handle) { Handle = handle; } }
    public readonly struct EffectRefreshed : IGameplayEvent { public GameplayEffectHandle Handle { get; } public EffectRefreshed(GameplayEffectHandle handle) { Handle = handle; } }
    public readonly struct EffectExpired : IGameplayEvent { public GameplayEffectHandle Handle { get; } public EffectExpired(GameplayEffectHandle handle) { Handle = handle; } }
    public readonly struct EffectRemoved : IGameplayEvent { public GameplayEffectHandle Handle { get; } public EffectRemoved(GameplayEffectHandle handle) { Handle = handle; } }
    public readonly struct EffectSuspended : IGameplayEvent { public GameplayEffectHandle Handle { get; } public EffectSuspended(GameplayEffectHandle handle) { Handle = handle; } }
    public readonly struct EffectResumed : IGameplayEvent { public GameplayEffectHandle Handle { get; } public EffectResumed(GameplayEffectHandle handle) { Handle = handle; } }
}
