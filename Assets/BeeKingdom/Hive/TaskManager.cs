using System;
using System.Collections.Generic;
using BeeKingdom.Core.Services;
using BeeKingdom.Core.Simulation;

namespace BeeKingdom.Hive
{
    public sealed class TaskManager : ISimulationSystem
    {
        private readonly Dictionary<string, TaskInstance> tasksById = new Dictionary<string, TaskInstance>();
        private readonly TaskQueue queue = new TaskQueue();
        private readonly TaskAllocator allocator = new TaskAllocator();
        private readonly IEventBus eventBus;

        public Type SystemType => typeof(TaskManager);
        public string Name => nameof(TaskManager);
        public SimulationPhase Phase => SimulationPhase.PostSimulation;
        public int Priority => 200;
        public IReadOnlyList<Type> RunsAfter => new[] { typeof(BeeLifecycleManager) };
        public IReadOnlyList<Type> RunsBefore => Array.Empty<Type>();
        public TaskDiagnostics Diagnostics { get; } = new TaskDiagnostics();

        public TaskManager(IEventBus eventBus = null)
        {
            this.eventBus = eventBus;
        }

        public TaskInstance CreateTask(string taskId, TaskDefinition definition, double nowSeconds, double expiresAtSeconds)
        {
            TaskInstance task = new TaskInstance(taskId, definition, nowSeconds, expiresAtSeconds);
            task.ChangeState(TaskLifecycleState.Queued);
            tasksById.Add(taskId, task);
            queue.Enqueue(task);
            eventBus?.Publish(new TaskCreated(taskId));
            Record();
            return task;
        }

        public bool CancelTask(string taskId)
        {
            TaskInstance task = GetTask(taskId);
            if (!task.ChangeState(TaskLifecycleState.Cancelled))
            {
                return false;
            }

            eventBus?.Publish(new TaskCancelled(taskId));
            Record();
            return true;
        }

        public bool CompleteTask(string taskId)
        {
            TaskInstance task = GetTask(taskId);
            if (task.State == TaskLifecycleState.Assigned)
            {
                task.ChangeState(TaskLifecycleState.Executing);
            }

            if (!task.ChangeState(TaskLifecycleState.Completed))
            {
                return false;
            }

            eventBus?.Publish(new TaskCompleted(taskId));
            Record();
            return true;
        }

        public TaskReservation ReserveTask(string taskId, string beeId, double nowSeconds, double durationSeconds)
        {
            TaskInstance task = GetTask(taskId);
            TaskReservation reservation = new TaskReservation(taskId, beeId, nowSeconds + durationSeconds);
            if (!task.Reserve(reservation))
            {
                return default;
            }

            Diagnostics.RecordReservation();
            Record();
            return reservation;
        }

        public bool AssignTask(string taskId, string beeId)
        {
            TaskInstance task = GetTask(taskId);
            if (!task.Assign(beeId))
            {
                return false;
            }

            Diagnostics.RecordAssignment();
            eventBus?.Publish(new TaskAssigned(taskId, beeId));
            Record();
            return true;
        }

        public bool TryAutoAssign(IReadOnlyList<BeeTaskCandidate> candidates, double nowSeconds, double reservationSeconds, out TaskInstance task, out string beeId)
        {
            task = queue.GetBestAvailable();
            beeId = null;
            if (task == null || !allocator.TrySelectBee(task, candidates, out beeId))
            {
                return false;
            }

            TaskReservation reservation = ReserveTask(task.TaskId, beeId, nowSeconds, reservationSeconds);
            return reservation.IsValid && AssignTask(task.TaskId, beeId);
        }

        public IReadOnlyList<TaskInstance> GetAvailableTasks()
        {
            return queue.GetAll();
        }

        public TaskStatistics GetStatistics()
        {
            int queued = 0;
            int assigned = 0;
            int completed = 0;
            int cancelled = 0;
            int failed = 0;
            foreach (TaskInstance task in tasksById.Values)
            {
                if (task.State == TaskLifecycleState.Queued) queued++;
                else if (task.State == TaskLifecycleState.Assigned || task.State == TaskLifecycleState.Executing) assigned++;
                else if (task.State == TaskLifecycleState.Completed) completed++;
                else if (task.State == TaskLifecycleState.Cancelled) cancelled++;
                else if (task.State == TaskLifecycleState.Failed) failed++;
            }

            return new TaskStatistics(tasksById.Count, queued, assigned, completed, cancelled, failed);
        }

        public void ChangePriority(string taskId, TaskPriority priority)
        {
            TaskInstance task = GetTask(taskId);
            task.SetPriority(priority);
            eventBus?.Publish(new TaskPriorityChanged(taskId, priority));
            Record();
        }

        public void Execute(in SimulationExecutionContext context)
        {
            Record();
        }

        private TaskInstance GetTask(string taskId)
        {
            if (tasksById.TryGetValue(taskId, out TaskInstance task))
            {
                return task;
            }

            throw new KeyNotFoundException($"Task {taskId} was not found.");
        }

        private void Record()
        {
            Diagnostics.RecordStatistics(GetStatistics());
        }
    }
}
