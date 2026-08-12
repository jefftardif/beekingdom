namespace BeeKingdom.Core.Workflows
{
    public sealed class WorkflowExecutor
    {
        public bool Execute(GameplayWorkflowInstance instance)
        {
            return instance.ChangeState(WorkflowState.Executing) &&
                instance.ChangeState(WorkflowState.ApplyingEffects) &&
                instance.ChangeState(WorkflowState.UpdatingAttributes) &&
                instance.ChangeState(WorkflowState.Completed);
        }
    }
}
