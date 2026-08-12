namespace BeeKingdom.Core.Workflows
{
    public enum WorkflowState { Requested, Validated, Queued, Reserved, Executing, ApplyingEffects, UpdatingAttributes, Completed, Cancelled, Interrupted, Suspended, Retrying, Failed }
    public enum WorkflowPriority { Immediate, Critical, High, Normal, Low, Background }
    public enum WorkflowQueueType { Player, BeeAI, World, Simulation, Backend, LiveOps }
}
