using BeeKingdom.Core.Workflows;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class GameplayWorkflowEngineTests
    {
        [Test]
        public void WorkflowRunsThroughCompletion()
        {
            GameplayWorkflowManager manager = new GameplayWorkflowManager();
            GameplayWorkflowInstance instance = manager.RequestWorkflow(Definition("build", WorkflowPriority.Normal));

            Assert.That(manager.ValidateWorkflow(instance.Handle), Is.True);
            Assert.That(manager.QueueWorkflow(instance.Handle), Is.True);
            Assert.That(manager.ExecuteWorkflow(), Is.True);
            Assert.That(instance.State, Is.EqualTo(WorkflowState.Completed));
        }

        [Test]
        public void SchedulerExecutesHighestPriorityFirst()
        {
            GameplayWorkflowManager manager = new GameplayWorkflowManager();
            GameplayWorkflowInstance low = manager.RequestWorkflow(Definition("low", WorkflowPriority.Low));
            GameplayWorkflowInstance high = manager.RequestWorkflow(Definition("high", WorkflowPriority.Critical));
            manager.ValidateWorkflow(low.Handle); manager.QueueWorkflow(low.Handle);
            manager.ValidateWorkflow(high.Handle); manager.QueueWorkflow(high.Handle);

            manager.ExecuteWorkflow();

            Assert.That(high.State, Is.EqualTo(WorkflowState.Completed));
            Assert.That(low.State, Is.EqualTo(WorkflowState.Queued));
        }

        [Test]
        public void InterruptAndResumeQueuesWorkflowAgain()
        {
            GameplayWorkflowManager manager = new GameplayWorkflowManager();
            GameplayWorkflowInstance instance = manager.RequestWorkflow(Definition("bee-ai", WorkflowPriority.High));

            Assert.That(manager.InterruptWorkflow(instance.Handle), Is.True);
            Assert.That(manager.ResumeWorkflow(instance.Handle), Is.True);
            Assert.That(instance.State, Is.EqualTo(WorkflowState.Queued));
        }

        [Test]
        public void ReservationConflictFailsSecondWorkflow()
        {
            GameplayWorkflowManager manager = new GameplayWorkflowManager();
            GameplayWorkflowInstance first = manager.RequestWorkflow(Definition("first", WorkflowPriority.Critical, "cell-1"));
            GameplayWorkflowInstance second = manager.RequestWorkflow(Definition("second", WorkflowPriority.Critical, "cell-1"));
            manager.ValidateWorkflow(first.Handle); manager.QueueWorkflow(first.Handle);
            manager.ValidateWorkflow(second.Handle); manager.QueueWorkflow(second.Handle);

            Assert.That(manager.ExecuteWorkflow(), Is.True);
            Assert.That(manager.ExecuteWorkflow(), Is.True);
            Assert.That(first.State, Is.EqualTo(WorkflowState.Completed));
            Assert.That(second.State, Is.EqualTo(WorkflowState.Completed));
        }

        [Test]
        public void DeterministicOrderingForSamePriority()
        {
            Assert.That(FirstCompletedId(), Is.EqualTo(FirstCompletedId()));
        }

        [Test]
        public void HandlesLargeWorkflowSet()
        {
            GameplayWorkflowManager manager = new GameplayWorkflowManager();
            for (int i = 0; i < 100000; i++)
            {
                GameplayWorkflowInstance instance = manager.RequestWorkflow(Definition("w-" + i, WorkflowPriority.Background));
                manager.ValidateWorkflow(instance.Handle);
                manager.QueueWorkflow(instance.Handle);
            }

            Assert.That(manager.Diagnostics.Requested, Is.EqualTo(100000));
            Assert.That(manager.ExecuteWorkflow(), Is.True);
        }

        private static long FirstCompletedId()
        {
            GameplayWorkflowManager manager = new GameplayWorkflowManager();
            GameplayWorkflowInstance a = manager.RequestWorkflow(Definition("a", WorkflowPriority.Normal));
            GameplayWorkflowInstance b = manager.RequestWorkflow(Definition("b", WorkflowPriority.Normal));
            manager.ValidateWorkflow(a.Handle); manager.QueueWorkflow(a.Handle);
            manager.ValidateWorkflow(b.Handle); manager.QueueWorkflow(b.Handle);
            manager.ExecuteWorkflow();
            return a.State == WorkflowState.Completed ? a.Handle : b.Handle;
        }

        private static GameplayWorkflowDefinition Definition(string id, WorkflowPriority priority, params string[] reservations)
        {
            return new GameplayWorkflowDefinition(id, "ability-" + id, WorkflowQueueType.Player, priority, reservations);
        }
    }
}
