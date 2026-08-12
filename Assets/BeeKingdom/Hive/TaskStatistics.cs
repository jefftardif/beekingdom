namespace BeeKingdom.Hive
{
    public readonly struct TaskStatistics
    {
        public int TotalTasks { get; }
        public int QueuedTasks { get; }
        public int AssignedTasks { get; }
        public int CompletedTasks { get; }
        public int CancelledTasks { get; }
        public int FailedTasks { get; }

        public TaskStatistics(int totalTasks, int queuedTasks, int assignedTasks, int completedTasks, int cancelledTasks, int failedTasks)
        {
            TotalTasks = totalTasks;
            QueuedTasks = queuedTasks;
            AssignedTasks = assignedTasks;
            CompletedTasks = completedTasks;
            CancelledTasks = cancelledTasks;
            FailedTasks = failedTasks;
        }
    }
}
