using System.Collections.Generic;

namespace BeeKingdom.Core.Workflows
{
    public sealed class GameplayWorkflowDefinition
    {
        public string WorkflowId { get; }
        public string AbilityId { get; }
        public WorkflowQueueType QueueType { get; }
        public WorkflowPriority Priority { get; }
        public IReadOnlyList<string> RequiredReservations { get; }
        public IReadOnlyList<string> EffectIds { get; }

        public GameplayWorkflowDefinition(string workflowId, string abilityId, WorkflowQueueType queueType, WorkflowPriority priority, IReadOnlyList<string> requiredReservations = null, IReadOnlyList<string> effectIds = null)
        {
            WorkflowId = string.IsNullOrWhiteSpace(workflowId) ? throw new System.ArgumentException("Workflow id is required.", nameof(workflowId)) : workflowId;
            AbilityId = abilityId ?? string.Empty;
            QueueType = queueType;
            Priority = priority;
            RequiredReservations = requiredReservations ?? new string[0];
            EffectIds = effectIds ?? new string[0];
        }
    }
}
