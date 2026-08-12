using BeeKingdom.Hive;
using BeeKingdom.Services;
using NUnit.Framework;
using System.Collections.Generic;

namespace BeeKingdom.Tests.Editor
{
    public sealed class TaskSystemTests
    {
        [Test]
        public void CreateTaskQueuesTaskAndPublishesEvent()
        {
            EventBus eventBus = new EventBus();
            int created = 0;
            eventBus.Subscribe<TaskCreated>(_ => created++);
            TaskManager manager = new TaskManager(eventBus);

            TaskInstance task = manager.CreateTask("task-1", Definition(ColonyTaskType.HarvestNectar, 10), 0d, 100d);

            Assert.That(task.State, Is.EqualTo(TaskLifecycleState.Queued));
            Assert.That(created, Is.EqualTo(1));
        }

        [Test]
        public void QueueReturnsHighestPriorityTask()
        {
            TaskManager manager = new TaskManager();
            manager.CreateTask("low", Definition(ColonyTaskType.Idle, 1), 0d, 100d);
            manager.CreateTask("high", Definition(ColonyTaskType.DefendHive, 100), 0d, 100d);

            IReadOnlyList<BeeTaskCandidate> candidates = new[]
            {
                new BeeTaskCandidate("bee-1", BeeLifecycleRole.Soldier, 100d, 100, 0, 100, true)
            };

            Assert.That(manager.TryAutoAssign(candidates, 0d, 10d, out TaskInstance task, out _), Is.True);
            Assert.That(task.TaskId, Is.EqualTo("high"));
        }

        [Test]
        public void ReservedTaskCannotBeReservedTwice()
        {
            TaskManager manager = new TaskManager();
            manager.CreateTask("task-1", Definition(ColonyTaskType.HarvestNectar, 10), 0d, 100d);

            TaskReservation first = manager.ReserveTask("task-1", "bee-1", 0d, 10d);
            TaskReservation second = manager.ReserveTask("task-1", "bee-2", 0d, 10d);

            Assert.That(first.IsValid, Is.True);
            Assert.That(second.IsValid, Is.False);
        }

        [Test]
        public void AssignTaskRequiresMatchingReservation()
        {
            TaskManager manager = new TaskManager();
            manager.CreateTask("task-1", Definition(ColonyTaskType.HarvestNectar, 10), 0d, 100d);
            manager.ReserveTask("task-1", "bee-1", 0d, 10d);

            Assert.That(manager.AssignTask("task-1", "bee-2"), Is.False);
            Assert.That(manager.AssignTask("task-1", "bee-1"), Is.True);
        }

        [Test]
        public void CancelTaskMovesToCancelled()
        {
            TaskManager manager = new TaskManager();
            manager.CreateTask("task-1", Definition(ColonyTaskType.HarvestNectar, 10), 0d, 100d);

            Assert.That(manager.CancelTask("task-1"), Is.True);
            Assert.That(manager.GetStatistics().CancelledTasks, Is.EqualTo(1));
        }

        [Test]
        public void HandlesLargeTaskSet()
        {
            TaskManager manager = new TaskManager();
            for (int i = 0; i < 100000; i++)
            {
                manager.CreateTask("task-" + i, Definition(ColonyTaskType.Idle, i % 100), 0d, 100d);
            }

            Assert.That(manager.GetStatistics().TotalTasks, Is.EqualTo(100000));
            Assert.That(manager.GetAvailableTasks().Count, Is.EqualTo(100000));
        }

        private static TaskDefinition Definition(ColonyTaskType type, int priority)
        {
            BeeLifecycleRole role = type == ColonyTaskType.DefendHive ? BeeLifecycleRole.Soldier : BeeLifecycleRole.Worker;
            return new TaskDefinition(type.ToString(), type, new TaskPriority(priority, 0, 0), 1d, 1d, role);
        }
    }
}
