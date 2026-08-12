namespace BeeKingdom.Core.Effects
{
    public sealed class GameplayEffectInstance
    {
        public GameplayEffectHandle Handle { get; }
        public GameplayEffectDefinition Definition { get; }
        public GameplayEffectContext Context { get; }
        public GameplayEffectState State { get; private set; }
        public double ElapsedSeconds { get; private set; }

        public GameplayEffectInstance(GameplayEffectHandle handle, GameplayEffectDefinition definition, GameplayEffectContext context)
        {
            Handle = handle;
            Definition = definition;
            Context = context;
            State = GameplayEffectState.Pending;
        }

        public bool ChangeState(GameplayEffectState next)
        {
            if (!CanTransition(State, next)) return false;
            State = next;
            return true;
        }

        public bool Advance(double deltaSeconds)
        {
            if (State != GameplayEffectState.Active || Definition.EffectType == GameplayEffectType.Infinite || Definition.EffectType == GameplayEffectType.Global)
            {
                return false;
            }

            ElapsedSeconds += deltaSeconds < 0d ? 0d : deltaSeconds;
            if (Definition.DurationSeconds > 0d && ElapsedSeconds >= Definition.DurationSeconds)
            {
                State = GameplayEffectState.Expired;
                return true;
            }

            return false;
        }

        private static bool CanTransition(GameplayEffectState current, GameplayEffectState next)
        {
            if (current == next) return true;
            if (current == GameplayEffectState.Expired || current == GameplayEffectState.Removed || current == GameplayEffectState.Cancelled || current == GameplayEffectState.Failed) return false;
            if (next == GameplayEffectState.Cancelled || next == GameplayEffectState.Failed || next == GameplayEffectState.Removed) return true;
            if (current == GameplayEffectState.Suspended && next == GameplayEffectState.Active) return true;
            return current == GameplayEffectState.Pending && next == GameplayEffectState.Applied ||
                current == GameplayEffectState.Applied && next == GameplayEffectState.Active ||
                current == GameplayEffectState.Active && next == GameplayEffectState.Refreshing ||
                current == GameplayEffectState.Refreshing && next == GameplayEffectState.Active ||
                current == GameplayEffectState.Active && next == GameplayEffectState.Expired ||
                current == GameplayEffectState.Active && next == GameplayEffectState.Suspended;
        }
    }
}
