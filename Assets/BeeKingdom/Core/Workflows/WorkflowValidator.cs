namespace BeeKingdom.Core.Workflows
{
    public sealed class WorkflowValidator
    {
        public bool Validate(GameplayWorkflowInstance instance)
        {
            return instance != null && !string.IsNullOrWhiteSpace(instance.Definition.WorkflowId);
        }
    }
}
