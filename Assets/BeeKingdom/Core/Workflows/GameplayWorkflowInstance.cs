namespace BeeKingdom.Core.Workflows
{
    public sealed class GameplayWorkflowInstance
    {
        public long Handle { get; }
        public GameplayWorkflowDefinition Definition { get; }
        public WorkflowState State { get; private set; }
        public long Sequence { get; }

        public GameplayWorkflowInstance(long handle, GameplayWorkflowDefinition definition, long sequence)
        {
            Handle = handle;
            Definition = definition;
            Sequence = sequence;
            State = WorkflowState.Requested;
        }

        public bool ChangeState(WorkflowState next)
        {
            if (!CanTransition(State, next)) return false;
            State = next;
            return true;
        }

        private static bool CanTransition(WorkflowState current, WorkflowState next)
        {
            if (current == next) return true;
            if (current == WorkflowState.Completed || current == WorkflowState.Cancelled || current == WorkflowState.Failed) return false;
            if (next == WorkflowState.Cancelled || next == WorkflowState.Interrupted || next == WorkflowState.Suspended || next == WorkflowState.Failed) return true;
            if (current == WorkflowState.Suspended && next == WorkflowState.Queued) return true;
            if (current == WorkflowState.Interrupted && next == WorkflowState.Retrying) return true;
            if (current == WorkflowState.Retrying && next == WorkflowState.Queued) return true;
            return current == WorkflowState.Requested && next == WorkflowState.Validated ||
                current == WorkflowState.Validated && next == WorkflowState.Queued ||
                current == WorkflowState.Queued && next == WorkflowState.Reserved ||
                current == WorkflowState.Reserved && next == WorkflowState.Executing ||
                current == WorkflowState.Executing && next == WorkflowState.ApplyingEffects ||
                current == WorkflowState.ApplyingEffects && next == WorkflowState.UpdatingAttributes ||
                current == WorkflowState.UpdatingAttributes && next == WorkflowState.Completed;
        }
    }
}
