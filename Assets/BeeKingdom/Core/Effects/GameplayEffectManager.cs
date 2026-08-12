using System.Collections.Generic;
using BeeKingdom.Core.Abilities;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Core.Effects
{
    public sealed class GameplayEffectManager
    {
        private readonly GameplayEffectRegistry registry;
        private readonly GameplayEffectFactory factory;
        private readonly Dictionary<GameplayEffectHandle, GameplayEffectInstance> instances = new Dictionary<GameplayEffectHandle, GameplayEffectInstance>();
        private readonly IEventBus eventBus;

        public GameplayEffectDiagnostics Diagnostics { get; } = new GameplayEffectDiagnostics();

        public GameplayEffectManager(IEventBus eventBus = null)
            : this(new GameplayEffectRegistry(), new GameplayEffectFactory(), eventBus)
        {
        }

        public GameplayEffectManager(GameplayEffectRegistry registry, GameplayEffectFactory factory, IEventBus eventBus = null)
        {
            this.registry = registry;
            this.factory = factory;
            this.eventBus = eventBus;
        }

        public bool RegisterEffect(GameplayEffectDefinition definition)
        {
            bool registered = registry.RegisterEffect(definition);
            if (registered)
            {
                Diagnostics.RecordRegistered(registry.Count);
                eventBus?.Publish(new EffectRegistered(definition.EffectId));
            }
            return registered;
        }

        public GameplayEffectResult ApplyEffect(string effectId, GameplayEffectContext context, out GameplayEffectHandle handle)
        {
            handle = default;
            if (!registry.TryGet(effectId, out GameplayEffectDefinition definition))
            {
                return GameplayEffectResult.Fail(GameplayEffectState.Failed, "Effect is not registered.");
            }

            GameplayEffectInstance instance = factory.Create(definition, context);
            instances.Add(instance.Handle, instance);
            handle = instance.Handle;
            instance.ChangeState(GameplayEffectState.Applied);
            instance.ChangeState(GameplayEffectState.Active);
            Diagnostics.RecordApplied();
            eventBus?.Publish(new EffectApplied(handle));
            return GameplayEffectResult.Ok(instance.State);
        }

        public GameplayEffectResult RefreshEffect(GameplayEffectHandle handle)
        {
            GameplayEffectInstance instance = GetInstance(handle);
            if (!instance.ChangeState(GameplayEffectState.Refreshing) || !instance.ChangeState(GameplayEffectState.Active))
            {
                return GameplayEffectResult.Fail(instance.State, "Effect cannot refresh.");
            }
            eventBus?.Publish(new EffectRefreshed(handle));
            return GameplayEffectResult.Ok(instance.State);
        }

        public GameplayEffectResult RemoveEffect(GameplayEffectHandle handle)
        {
            GameplayEffectInstance instance = GetInstance(handle);
            if (!instance.ChangeState(GameplayEffectState.Removed)) return GameplayEffectResult.Fail(instance.State, "Effect cannot be removed.");
            Diagnostics.RecordRemoved();
            eventBus?.Publish(new EffectRemoved(handle));
            return GameplayEffectResult.Ok(instance.State);
        }

        public GameplayEffectResult SuspendEffect(GameplayEffectHandle handle)
        {
            GameplayEffectInstance instance = GetInstance(handle);
            if (!instance.ChangeState(GameplayEffectState.Suspended)) return GameplayEffectResult.Fail(instance.State, "Effect cannot be suspended.");
            Diagnostics.RecordSuspended();
            eventBus?.Publish(new EffectSuspended(handle));
            return GameplayEffectResult.Ok(instance.State);
        }

        public GameplayEffectResult ResumeEffect(GameplayEffectHandle handle)
        {
            GameplayEffectInstance instance = GetInstance(handle);
            if (!instance.ChangeState(GameplayEffectState.Active)) return GameplayEffectResult.Fail(instance.State, "Effect cannot resume.");
            eventBus?.Publish(new EffectResumed(handle));
            return GameplayEffectResult.Ok(instance.State);
        }

        public void Tick(double deltaSeconds)
        {
            foreach (GameplayEffectInstance instance in instances.Values)
            {
                if (instance.Advance(deltaSeconds))
                {
                    Diagnostics.RecordExpired();
                    eventBus?.Publish(new EffectExpired(instance.Handle));
                }
            }
        }

        public IReadOnlyList<GameplayEffectDefinition> QueryEffects(GameplayAbilityTag tag) => registry.QueryEffects(tag);
        public GameplayEffectInstance GetInstance(GameplayEffectHandle handle) => instances[handle];
        public GameplayEffectSnapshot CreateSnapshot(GameplayEffectHandle handle)
        {
            GameplayEffectInstance instance = GetInstance(handle);
            return new GameplayEffectSnapshot(instance.Handle.Value, instance.Definition.EffectId, instance.State, instance.ElapsedSeconds);
        }
    }

    public readonly struct GameplayEffectSnapshot
    {
        public long Handle { get; }
        public string EffectId { get; }
        public GameplayEffectState State { get; }
        public double ElapsedSeconds { get; }

        public GameplayEffectSnapshot(long handle, string effectId, GameplayEffectState state, double elapsedSeconds)
        {
            Handle = handle;
            EffectId = effectId;
            State = state;
            ElapsedSeconds = elapsedSeconds;
        }
    }
}
