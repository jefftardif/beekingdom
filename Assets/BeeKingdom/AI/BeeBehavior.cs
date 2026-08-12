namespace BeeKingdom.AI
{
    public sealed class BeeBehavior
    {
        public bool Execute(BeeBrain brain, in BeeDecisionContext context)
        {
            if (brain.Blackboard.State == BeeBehaviorState.Dead)
            {
                return false;
            }

            if (context.Task == null)
            {
                brain.ChangeState(BeeBehaviorState.Waiting);
                return false;
            }

            return context.DeltaSeconds >= context.Task.Definition.EstimatedDurationSeconds;
        }
    }
}
