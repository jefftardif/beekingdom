using System;

namespace BeeKingdom.Hive
{
    public sealed class TaskInstance
    {
        public string TaskId { get; }
        public TaskDefinition Definition { get; }
        public TaskLifecycleState State { get; private set; }
        public TaskPriority Priority { get; private set; }
        public double CreatedAtSeconds { get; }
        public double ExpiresAtSeconds { get; }
        public string ReservedByBeeId { get; private set; }
        public string AssignedBeeId { get; private set; }

        public TaskInstance(string taskId, TaskDefinition definition, double createdAtSeconds, double expiresAtSeconds)
        {
            TaskId = string.IsNullOrWhiteSpace(taskId) ? throw new ArgumentException("Task id is required.", nameof(taskId)) : taskId;
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            State = TaskLifecycleState.Created;
            Priority = definition.Priority;
            CreatedAtSeconds = createdAtSeconds;
            ExpiresAtSeconds = expiresAtSeconds;
        }

        public bool ChangeState(TaskLifecycleState next)
        {
            if (!CanTransition(State, next))
            {
                return false;
            }

            State = next;
            return true;
        }

        public bool Reserve(TaskReservation reservation)
        {
            if (State != TaskLifecycleState.Queued || !reservation.IsValid)
            {
                return false;
            }

            ReservedByBeeId = reservation.BeeId;
            return ChangeState(TaskLifecycleState.Reserved);
        }

        public bool Assign(string beeId)
        {
            if (State != TaskLifecycleState.Reserved || ReservedByBeeId != beeId)
            {
                return false;
            }

            AssignedBeeId = beeId;
            return ChangeState(TaskLifecycleState.Assigned);
        }

        public void SetPriority(TaskPriority priority)
        {
            Priority = priority;
        }

        private static bool CanTransition(TaskLifecycleState current, TaskLifecycleState next)
        {
            if (current == next) return true;
            if (current == TaskLifecycleState.Completed || current == TaskLifecycleState.Cancelled || current == TaskLifecycleState.Failed) return false;
            if (next == TaskLifecycleState.Cancelled || next == TaskLifecycleState.Failed) return true;
            return current == TaskLifecycleState.Created && next == TaskLifecycleState.Queued ||
                current == TaskLifecycleState.Queued && next == TaskLifecycleState.Reserved ||
                current == TaskLifecycleState.Reserved && next == TaskLifecycleState.Assigned ||
                current == TaskLifecycleState.Assigned && next == TaskLifecycleState.Executing ||
                current == TaskLifecycleState.Executing && next == TaskLifecycleState.Completed;
        }
    }
}
