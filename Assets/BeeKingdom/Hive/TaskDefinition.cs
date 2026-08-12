namespace BeeKingdom.Hive
{
    public sealed class TaskDefinition
    {
        public string DefinitionId { get; }
        public ColonyTaskType TaskType { get; }
        public TaskPriority Priority { get; }
        public double EstimatedCost { get; }
        public double EstimatedDurationSeconds { get; }
        public BeeLifecycleRole PreferredRole { get; }

        public TaskDefinition(string definitionId, ColonyTaskType taskType, TaskPriority priority, double estimatedCost, double estimatedDurationSeconds, BeeLifecycleRole preferredRole)
        {
            DefinitionId = string.IsNullOrWhiteSpace(definitionId) ? taskType.ToString() : definitionId;
            TaskType = taskType;
            Priority = priority;
            EstimatedCost = estimatedCost < 0d ? 0d : estimatedCost;
            EstimatedDurationSeconds = estimatedDurationSeconds < 0d ? 0d : estimatedDurationSeconds;
            PreferredRole = preferredRole;
        }
    }
}
