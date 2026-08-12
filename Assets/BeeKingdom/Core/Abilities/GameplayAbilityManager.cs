using System.Collections.Generic;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Core.Abilities
{
    public sealed class GameplayAbilityManager
    {
        private readonly GameplayAbilityRegistry registry;
        private readonly GameplayAbilityFactory factory;
        private readonly Dictionary<GameplayAbilityHandle, GameplayAbilityInstance> instances = new Dictionary<GameplayAbilityHandle, GameplayAbilityInstance>();
        private readonly IEventBus eventBus;

        public GameplayAbilityDiagnostics Diagnostics { get; } = new GameplayAbilityDiagnostics();

        public GameplayAbilityManager(IEventBus eventBus = null)
            : this(new GameplayAbilityRegistry(), new GameplayAbilityFactory(), eventBus)
        {
        }

        public GameplayAbilityManager(GameplayAbilityRegistry registry, GameplayAbilityFactory factory, IEventBus eventBus = null)
        {
            this.registry = registry;
            this.factory = factory;
            this.eventBus = eventBus;
        }

        public bool RegisterAbility(GameplayAbilityDefinition definition)
        {
            bool registered = registry.RegisterAbility(definition);
            if (registered)
            {
                Diagnostics.RecordRegistered(registry.Count);
                eventBus?.Publish(new AbilityRegistered(definition.AbilityId));
            }

            return registered;
        }

        public bool UnregisterAbility(string abilityId)
        {
            bool removed = registry.UnregisterAbility(abilityId);
            if (removed)
            {
                Diagnostics.RecordRegistered(registry.Count);
            }

            return removed;
        }

        public GameplayAbilityResult RequestActivation(string abilityId, GameplayAbilityContext context, out GameplayAbilityHandle handle)
        {
            handle = default;
            if (!registry.TryGet(abilityId, out GameplayAbilityDefinition definition))
            {
                Diagnostics.RecordFailed();
                return GameplayAbilityResult.Fail(GameplayAbilityState.Failed, "Ability is not registered.");
            }

            GameplayAbilityInstance instance = factory.Create(definition, context);
            instances.Add(instance.Handle, instance);
            handle = instance.Handle;
            Diagnostics.RecordRequested();
            eventBus?.Publish(new AbilityRequested(handle));
            return GameplayAbilityResult.Ok(instance.State);
        }

        public GameplayAbilityResult Validate(GameplayAbilityHandle handle)
        {
            return Change(handle, GameplayAbilityState.Validated, new AbilityValidated(handle));
        }

        public GameplayAbilityResult Activate(GameplayAbilityHandle handle)
        {
            GameplayAbilityResult validated = Change(handle, GameplayAbilityState.Activated, new AbilityActivated(handle));
            if (validated.Success)
            {
                Change(handle, GameplayAbilityState.Executing, default(AbilityActivated), false);
            }

            return validated;
        }

        public GameplayAbilityResult Complete(GameplayAbilityHandle handle)
        {
            GameplayAbilityResult result = Change(handle, GameplayAbilityState.Completed, new AbilityCompleted(handle));
            if (result.Success) Diagnostics.RecordCompleted();
            return result;
        }

        public GameplayAbilityResult Cancel(GameplayAbilityHandle handle)
        {
            GameplayAbilityResult result = Change(handle, GameplayAbilityState.Cancelled, new AbilityCancelled(handle));
            if (result.Success) Diagnostics.RecordCancelled();
            return result;
        }

        public GameplayAbilityResult Interrupt(GameplayAbilityHandle handle)
        {
            GameplayAbilityResult result = Change(handle, GameplayAbilityState.Interrupted, new AbilityInterrupted(handle));
            if (result.Success) Diagnostics.RecordInterrupted();
            return result;
        }

        public IReadOnlyList<GameplayAbilityDefinition> QueryAbilities(GameplayAbilityTag tag)
        {
            return registry.QueryAbilities(tag);
        }

        public GameplayAbilityInstance GetInstance(GameplayAbilityHandle handle)
        {
            return instances[handle];
        }

        public GameplayAbilitySnapshot CreateSnapshot(GameplayAbilityHandle handle)
        {
            GameplayAbilityInstance instance = GetInstance(handle);
            return new GameplayAbilitySnapshot(instance.Handle.Value, instance.Definition.AbilityId, instance.State, instance.Context.SimulationTimeSeconds);
        }

        private GameplayAbilityResult Change<TEvent>(GameplayAbilityHandle handle, GameplayAbilityState state, TEvent eventData, bool publish = true)
        {
            if (!instances.TryGetValue(handle, out GameplayAbilityInstance instance))
            {
                Diagnostics.RecordFailed();
                return GameplayAbilityResult.Fail(GameplayAbilityState.Failed, "Ability instance was not found.");
            }

            if (!instance.ChangeState(state))
            {
                Diagnostics.RecordFailed();
                eventBus?.Publish(new AbilityFailed(handle, "Invalid state transition."));
                return GameplayAbilityResult.Fail(GameplayAbilityState.Failed, "Invalid state transition.");
            }

            if (publish)
            {
                eventBus?.Publish(eventData);
            }

            return GameplayAbilityResult.Ok(instance.State);
        }
    }

    public readonly struct GameplayAbilitySnapshot
    {
        public long Handle { get; }
        public string AbilityId { get; }
        public GameplayAbilityState State { get; }
        public double SimulationTimeSeconds { get; }

        public GameplayAbilitySnapshot(long handle, string abilityId, GameplayAbilityState state, double simulationTimeSeconds)
        {
            Handle = handle;
            AbilityId = abilityId;
            State = state;
            SimulationTimeSeconds = simulationTimeSeconds;
        }
    }
}
