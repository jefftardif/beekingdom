namespace BeeKingdom.AI
{
    public sealed class BeeBehaviorStateMachine
    {
        public bool CanTransition(BeeBehaviorState current, BeeBehaviorState next)
        {
            if (current == next) return true;
            if (current == BeeBehaviorState.Dead) return false;
            if (next == BeeBehaviorState.Dead) return true;
            return true;
        }
    }
}
