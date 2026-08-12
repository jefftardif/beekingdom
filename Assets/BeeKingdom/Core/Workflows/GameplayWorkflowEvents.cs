using BeeKingdom.Core.Events;

namespace BeeKingdom.Core.Workflows
{
    public readonly struct WorkflowRequested : IGameplayEvent { public long Handle { get; } public WorkflowRequested(long handle) { Handle = handle; } }
    public readonly struct WorkflowValidated : IGameplayEvent { public long Handle { get; } public WorkflowValidated(long handle) { Handle = handle; } }
    public readonly struct WorkflowQueued : IGameplayEvent { public long Handle { get; } public WorkflowQueued(long handle) { Handle = handle; } }
    public readonly struct WorkflowStarted : IGameplayEvent { public long Handle { get; } public WorkflowStarted(long handle) { Handle = handle; } }
    public readonly struct WorkflowCompleted : IGameplayEvent { public long Handle { get; } public WorkflowCompleted(long handle) { Handle = handle; } }
    public readonly struct WorkflowCancelled : IGameplayEvent { public long Handle { get; } public WorkflowCancelled(long handle) { Handle = handle; } }
    public readonly struct WorkflowInterrupted : IGameplayEvent { public long Handle { get; } public WorkflowInterrupted(long handle) { Handle = handle; } }
    public readonly struct WorkflowFailed : IGameplayEvent { public long Handle { get; } public WorkflowFailed(long handle) { Handle = handle; } }
}
