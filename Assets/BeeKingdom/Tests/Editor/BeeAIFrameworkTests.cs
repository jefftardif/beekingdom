using BeeKingdom.AI;
using BeeKingdom.Hive;
using BeeKingdom.Services;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class BeeAIFrameworkTests
    {
        [Test]
        public void AssignTaskChangesBeeState()
        {
            BeeAIManager ai = new BeeAIManager();
            ai.CreateBrain("bee-1", 100, 100);

            ai.AssignTask("bee-1", Task("task-1", ColonyTaskType.HarvestNectar));

            Assert.That(ai.GetCurrentState("bee-1"), Is.EqualTo(BeeBehaviorState.Harvesting));
        }

        [Test]
        public void UpdateBehaviorCompletesTask()
        {
            EventBus eventBus = new EventBus();
            int completed = 0;
            eventBus.Subscribe<BeeTaskCompleted>(_ => completed++);
            BeeAIManager ai = new BeeAIManager(eventBus);
            ai.CreateBrain("bee-1", 100, 100);
            ai.AssignTask("bee-1", Task("task-1", ColonyTaskType.BuildCell));

            bool result = ai.UpdateBehavior("bee-1", 10d);

            Assert.That(result, Is.True);
            Assert.That(completed, Is.EqualTo(1));
            Assert.That(ai.GetCurrentState("bee-1"), Is.EqualTo(BeeBehaviorState.Idle));
        }

        [Test]
        public void InterruptAndResumeRestoresTaskState()
        {
            BeeAIManager ai = new BeeAIManager();
            ai.CreateBrain("bee-1", 100, 100);
            ai.AssignTask("bee-1", Task("task-1", ColonyTaskType.DefendHive));

            ai.Interrupt("bee-1");
            Assert.That(ai.GetCurrentState("bee-1"), Is.EqualTo(BeeBehaviorState.Waiting));

            ai.Resume("bee-1");
            Assert.That(ai.GetCurrentState("bee-1"), Is.EqualTo(BeeBehaviorState.Guarding));
        }

        [Test]
        public void HandlesLargeBrainSet()
        {
            BeeAIManager ai = new BeeAIManager(null, 512);
            for (int i = 0; i < 50000; i++)
            {
                ai.CreateBrain("bee-" + i, 100, 100);
            }

            Assert.That(ai.GetStatistics().BrainCount, Is.EqualTo(50000));
        }

        [Test]
        public void SameTaskProducesDeterministicState()
        {
            BeeAIManager first = new BeeAIManager();
            BeeAIManager second = new BeeAIManager();
            first.CreateBrain("bee-1", 100, 100);
            second.CreateBrain("bee-1", 100, 100);

            TaskInstance task = Task("task-1", ColonyTaskType.RepairHive);
            first.AssignTask("bee-1", task);
            second.AssignTask("bee-1", task);

            Assert.That(first.GetCurrentState("bee-1"), Is.EqualTo(second.GetCurrentState("bee-1")));
        }

        private static TaskInstance Task(string taskId, ColonyTaskType type)
        {
            TaskDefinition definition = new TaskDefinition(type.ToString(), type, new TaskPriority(10, 0, 0), 1d, 1d, BeeLifecycleRole.Worker);
            TaskInstance task = new TaskInstance(taskId, definition, 0d, 100d);
            task.ChangeState(TaskLifecycleState.Queued);
            task.Reserve(new TaskReservation(taskId, "bee-1", 10d));
            task.Assign("bee-1");
            return task;
        }
    }
}
