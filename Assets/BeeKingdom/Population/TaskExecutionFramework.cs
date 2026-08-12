using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Population
{
    public enum TaskWorkType { Build, Upgrade, Gather, Harvest, Transport, FeedLarva, ProduceWax, ProduceHoney, Nurse, Clean, Repair, Patrol, Guard, Explore, Attack, Defend, Heal, Rest, Research, Custom }
    public enum TaskExecutionState { Created, Waiting, Reserved, Assigned, Executing, Paused, Resumed, Completed, Cancelled, Failed, Expired }
    public enum TaskPriority { Emergency, Critical, High, Normal, Low, Background }

    public sealed class TaskDefinition
    {
        public string DefinitionId { get; }
        public TaskWorkType WorkType { get; }
        public TaskPriority Priority { get; }
        public double DurationSeconds { get; }
        public int RequiredBees { get; }

        public TaskDefinition(string definitionId, TaskWorkType workType, TaskPriority priority, double durationSeconds, int requiredBees = 1)
        {
            DefinitionId = string.IsNullOrWhiteSpace(definitionId) ? throw new ArgumentException("Definition id is required.", nameof(definitionId)) : definitionId;
            WorkType = workType;
            Priority = priority;
            DurationSeconds = durationSeconds <= 0d ? 0.1d : durationSeconds;
            RequiredBees = Math.Max(1, requiredBees);
        }
    }

    public sealed class TaskInstance
    {
        private readonly List<string> assignedBees = new List<string>();
        private readonly List<string> dependencies = new List<string>();

        public string TaskId { get; }
        public string DefinitionId { get; }
        public string TargetId { get; }
        public TaskExecutionState State { get; private set; }
        public double ProgressSeconds { get; private set; }
        public IReadOnlyList<string> AssignedBees => assignedBees;
        public IReadOnlyList<string> Dependencies => dependencies;

        public TaskInstance(string taskId, TaskDefinition definition, string targetId)
        {
            TaskId = string.IsNullOrWhiteSpace(taskId) ? throw new ArgumentException("Task id is required.", nameof(taskId)) : taskId;
            DefinitionId = definition.DefinitionId;
            TargetId = targetId ?? string.Empty;
            State = TaskExecutionState.Created;
        }

        public void SetState(TaskExecutionState state) => State = state;
        public void AddBee(string beeId) { if (!assignedBees.Contains(beeId)) assignedBees.Add(beeId); }
        public void AddDependency(string dependencyId) { if (!string.IsNullOrWhiteSpace(dependencyId)) dependencies.Add(dependencyId); }
        public void Advance(double seconds) => ProgressSeconds += Math.Max(0d, seconds);
    }

    public sealed class TaskScheduler
    {
        public void Schedule(TaskInstance task) => task.SetState(TaskExecutionState.Waiting);
        public void Assign(TaskInstance task, string beeId) { task.AddBee(beeId); task.SetState(TaskExecutionState.Assigned); }
        public void Reserve(TaskInstance task) => task.SetState(TaskExecutionState.Reserved);
    }

    public sealed class TaskExecutor
    {
        public TaskExecutionState Execute(TaskInstance task, TaskDefinition definition, double deltaSeconds)
        {
            task.SetState(TaskExecutionState.Executing);
            task.Advance(deltaSeconds);
            if (task.ProgressSeconds >= definition.DurationSeconds) task.SetState(TaskExecutionState.Completed);
            return task.State;
        }
    }

    public sealed class TaskExecutionEngine
    {
        private readonly TaskScheduler scheduler = new TaskScheduler();
        private readonly TaskExecutor executor = new TaskExecutor();
        public void Schedule(TaskInstance task) => scheduler.Schedule(task);
        public void Reserve(TaskInstance task) => scheduler.Reserve(task);
        public void Assign(TaskInstance task, string beeId) => scheduler.Assign(task, beeId);
        public TaskExecutionState Execute(TaskInstance task, TaskDefinition definition, double deltaSeconds) => executor.Execute(task, definition, deltaSeconds);
    }

    public sealed class TaskDiagnostics
    {
        public int Created { get; private set; }
        public int Reserved { get; private set; }
        public int Assigned { get; private set; }
        public int Completed { get; private set; }
        public int Cancelled { get; private set; }
        public int Failed { get; private set; }
        public void RecordCreated() => Created++;
        public void RecordReserved() => Reserved++;
        public void RecordAssigned() => Assigned++;
        public void RecordCompleted() => Completed++;
        public void RecordCancelled() => Cancelled++;
        public void RecordFailed() => Failed++;
    }

    public sealed class TaskExecutionManager
    {
        private readonly Dictionary<string, TaskDefinition> definitions = new Dictionary<string, TaskDefinition>();
        private readonly Dictionary<string, TaskInstance> tasks = new Dictionary<string, TaskInstance>();
        private readonly TaskExecutionEngine engine = new TaskExecutionEngine();
        private readonly IEventBus eventBus;
        private int sequence;

        public TaskDiagnostics Diagnostics { get; } = new TaskDiagnostics();
        public TaskExecutionManager(IEventBus eventBus = null) { this.eventBus = eventBus; }

        public bool RegisterTaskDefinition(TaskDefinition definition)
        {
            if (definition == null || definitions.ContainsKey(definition.DefinitionId)) return false;
            definitions.Add(definition.DefinitionId, definition);
            return true;
        }

        public TaskInstance CreateTask(string definitionId, string targetId)
        {
            if (!definitions.TryGetValue(definitionId, out TaskDefinition definition)) return null;
            TaskInstance task = new TaskInstance("task-" + (++sequence).ToString("D6"), definition, targetId);
            tasks.Add(task.TaskId, task);
            Diagnostics.RecordCreated();
            eventBus?.Publish(new TaskCreated(task.TaskId));
            engine.Schedule(task);
            return task;
        }

        public bool ReserveTask(string taskId)
        {
            if (!tasks.TryGetValue(taskId, out TaskInstance task)) return false;
            engine.Reserve(task);
            Diagnostics.RecordReserved();
            return true;
        }

        public bool AssignTask(string taskId, string beeId)
        {
            if (!tasks.TryGetValue(taskId, out TaskInstance task)) return false;
            engine.Assign(task, beeId);
            Diagnostics.RecordAssigned();
            eventBus?.Publish(new TaskAssigned(taskId, beeId));
            return true;
        }

        public TaskExecutionState ExecuteTask(string taskId, double deltaSeconds)
        {
            if (!tasks.TryGetValue(taskId, out TaskInstance task) || !definitions.TryGetValue(task.DefinitionId, out TaskDefinition definition)) return TaskExecutionState.Failed;
            TaskExecutionState state = engine.Execute(task, definition, deltaSeconds);
            if (state == TaskExecutionState.Completed)
            {
                Diagnostics.RecordCompleted();
                eventBus?.Publish(new TaskCompleted(taskId));
            }
            else
            {
                eventBus?.Publish(new TaskStarted(taskId));
            }
            return state;
        }

        public bool PauseTask(string taskId) => SetState(taskId, TaskExecutionState.Paused, () => eventBus?.Publish(new TaskPaused(taskId)));
        public bool ResumeTask(string taskId) => SetState(taskId, TaskExecutionState.Resumed, () => eventBus?.Publish(new TaskResumed(taskId)));
        public bool CancelTask(string taskId) { bool ok = SetState(taskId, TaskExecutionState.Cancelled, () => eventBus?.Publish(new TaskCancelled(taskId))); if (ok) Diagnostics.RecordCancelled(); return ok; }
        public bool CompleteTask(string taskId) { bool ok = SetState(taskId, TaskExecutionState.Completed, () => eventBus?.Publish(new TaskCompleted(taskId))); if (ok) Diagnostics.RecordCompleted(); return ok; }
        public IReadOnlyList<TaskInstance> QueryTasks() { List<TaskInstance> result = new List<TaskInstance>(tasks.Values); result.Sort((a, b) => string.CompareOrdinal(a.TaskId, b.TaskId)); return result; }

        private bool SetState(string taskId, TaskExecutionState state, Action publish)
        {
            if (!tasks.TryGetValue(taskId, out TaskInstance task)) return false;
            task.SetState(state);
            publish?.Invoke();
            return true;
        }
    }

    public readonly struct TaskCreated : IGameplayEvent, IBeeEvent { public string TaskId { get; } public TaskCreated(string taskId) { TaskId = taskId; } }
    public readonly struct TaskAssigned : IGameplayEvent, IBeeEvent { public string TaskId { get; } public string BeeId { get; } public TaskAssigned(string taskId, string beeId) { TaskId = taskId; BeeId = beeId; } }
    public readonly struct TaskStarted : IGameplayEvent, IBeeEvent { public string TaskId { get; } public TaskStarted(string taskId) { TaskId = taskId; } }
    public readonly struct TaskPaused : IGameplayEvent, IBeeEvent { public string TaskId { get; } public TaskPaused(string taskId) { TaskId = taskId; } }
    public readonly struct TaskResumed : IGameplayEvent, IBeeEvent { public string TaskId { get; } public TaskResumed(string taskId) { TaskId = taskId; } }
    public readonly struct TaskCompleted : IGameplayEvent, IBeeEvent { public string TaskId { get; } public TaskCompleted(string taskId) { TaskId = taskId; } }
    public readonly struct TaskCancelled : IGameplayEvent, IBeeEvent { public string TaskId { get; } public TaskCancelled(string taskId) { TaskId = taskId; } }
    public readonly struct TaskFailed : IGameplayEvent, IBeeEvent { public string TaskId { get; } public TaskFailed(string taskId) { TaskId = taskId; } }
}
