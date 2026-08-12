using BeeKingdom.Population;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class DynamicTaskAllocationFrameworkTests
    {
        [Test]
        public void AllocateTaskSelectsBestWorker()
        {
            DynamicTaskAllocationManager manager = CreateManager();
            TaskAssignment assignment = manager.AllocateTask("task-1", "hybrid", TaskPriority.Normal, new[]
            {
                new WorkerCandidate("bee-1", BeeCaste.Builder, 0.3d, 0.1d, 1d, 1d, 0),
                new WorkerCandidate("bee-2", BeeCaste.Builder, 0.9d, 0.1d, 1d, 1d, 0)
            });

            Assert.That(assignment.BeeId, Is.EqualTo("bee-2"));
        }

        [Test]
        public void ReleaseTaskRemovesAssignment()
        {
            DynamicTaskAllocationManager manager = CreateManager();
            manager.AllocateTask("task-1", "hybrid", TaskPriority.Normal, new[] { new WorkerCandidate("bee-1", BeeCaste.Worker, 1d, 0d, 1d, 0d, 0) });

            Assert.That(manager.ReleaseTask("task-1"), Is.True);
            Assert.That(manager.QueryAssignments().Count, Is.EqualTo(0));
        }

        private static DynamicTaskAllocationManager CreateManager()
        {
            DynamicTaskAllocationManager manager = new DynamicTaskAllocationManager();
            manager.RegisterAllocationPolicy(new AllocationPolicy("hybrid", AllocationPolicyType.HybridStrategy, 1d, 1d, 0.5d, 0.5d));
            return manager;
        }
    }
}
