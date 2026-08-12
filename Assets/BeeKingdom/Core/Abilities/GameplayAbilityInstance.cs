namespace BeeKingdom.Core.Abilities
{
    public sealed class GameplayAbilityInstance
    {
        public GameplayAbilityHandle Handle { get; }
        public GameplayAbilityDefinition Definition { get; }
        public GameplayAbilityContext Context { get; }
        public GameplayAbilityState State { get; private set; }

        public GameplayAbilityInstance(GameplayAbilityHandle handle, GameplayAbilityDefinition definition, GameplayAbilityContext context)
        {
            Handle = handle;
            Definition = definition;
            Context = context;
            State = GameplayAbilityState.Requested;
        }

        public bool ChangeState(GameplayAbilityState next)
        {
            if (!CanTransition(State, next))
            {
                return false;
            }

            State = next;
            return true;
        }

        private static bool CanTransition(GameplayAbilityState current, GameplayAbilityState next)
        {
            if (current == next) return true;
            if (current == GameplayAbilityState.Completed || current == GameplayAbilityState.Cancelled || current == GameplayAbilityState.Interrupted || current == GameplayAbilityState.Failed) return false;
            if (next == GameplayAbilityState.Cancelled || next == GameplayAbilityState.Interrupted || next == GameplayAbilityState.Failed) return true;
            return current == GameplayAbilityState.Requested && next == GameplayAbilityState.Validated ||
                current == GameplayAbilityState.Validated && next == GameplayAbilityState.Activated ||
                current == GameplayAbilityState.Activated && next == GameplayAbilityState.Executing ||
                current == GameplayAbilityState.Executing && next == GameplayAbilityState.Completed;
        }
    }
}
