using BeeKingdom.Core.Events;

namespace BeeKingdom.Hive
{
    public readonly struct TaskCreated : IGameplayEvent { public string TaskId { get; } public TaskCreated(string taskId) { TaskId = taskId; } }
    public readonly struct TaskAssigned : IGameplayEvent { public string TaskId { get; } public string BeeId { get; } public TaskAssigned(string taskId, string beeId) { TaskId = taskId; BeeId = beeId; } }
    public readonly struct TaskCompleted : IGameplayEvent { public string TaskId { get; } public TaskCompleted(string taskId) { TaskId = taskId; } }
    public readonly struct TaskCancelled : IGameplayEvent { public string TaskId { get; } public TaskCancelled(string taskId) { TaskId = taskId; } }
    public readonly struct TaskFailed : IGameplayEvent { public string TaskId { get; } public TaskFailed(string taskId) { TaskId = taskId; } }
    public readonly struct TaskPriorityChanged : IGameplayEvent { public string TaskId { get; } public TaskPriority Priority { get; } public TaskPriorityChanged(string taskId, TaskPriority priority) { TaskId = taskId; Priority = priority; } }
}
