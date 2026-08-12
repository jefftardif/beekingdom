using BeeKingdom.Buildings;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class ConstructionWorkflowEngineTests
    {
        [Test]
        public void StartConstructionCreatesRunningWorkflow()
        {
            ConstructionWorkflowManager manager = CreateManager();

            ConstructionWorkflowInstance instance = manager.StartConstruction("nursery-workflow", "building-1", buildersAvailable: 2);

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance.State, Is.EqualTo(ConstructionWorkflowState.UnderConstruction));
            Assert.That(instance.BuilderCount, Is.EqualTo(2));
        }

        [Test]
        public void AdvanceConstructionCompletesAllPhases()
        {
            ConstructionWorkflowManager manager = CreateManager();
            ConstructionWorkflowInstance instance = manager.StartConstruction("nursery-workflow", "building-1");

            manager.AdvanceConstruction(instance.InstanceId, 1d);
            Assert.That(instance.PhaseIndex, Is.EqualTo(1));
            manager.AdvanceConstruction(instance.InstanceId, 1d);

            Assert.That(instance.State, Is.EqualTo(ConstructionWorkflowState.Operational));
            Assert.That(instance.GetProgress().TotalProgress, Is.EqualTo(1d));
            Assert.That(manager.Diagnostics.Completed, Is.EqualTo(1));
        }

        [Test]
        public void PauseResumeAndCancelAreTracked()
        {
            ConstructionWorkflowManager manager = CreateManager();
            ConstructionWorkflowInstance instance = manager.StartConstruction("nursery-workflow", "building-1");

            Assert.That(manager.PauseConstruction(instance.InstanceId), Is.True);
            Assert.That(manager.ResumeConstruction(instance.InstanceId), Is.True);
            Assert.That(manager.CancelConstruction(instance.InstanceId), Is.True);

            Assert.That(instance.State, Is.EqualTo(ConstructionWorkflowState.Cancelled));
            Assert.That(manager.Diagnostics.Paused, Is.EqualTo(1));
            Assert.That(manager.Diagnostics.Resumed, Is.EqualTo(1));
            Assert.That(manager.Diagnostics.Cancelled, Is.EqualTo(1));
        }

        [Test]
        public void InsufficientResourcesWaitsBeforeConstruction()
        {
            ConstructionWorkflowManager manager = CreateManager();

            ConstructionWorkflowInstance instance = manager.StartConstruction("nursery-workflow", "building-1", resourcesAvailable: false);

            Assert.That(instance.State, Is.EqualTo(ConstructionWorkflowState.WaitingForResources));
            Assert.That(manager.AdvanceConstruction(instance.InstanceId, 10d), Is.True);
            Assert.That(instance.GetProgress().TotalProgress, Is.EqualTo(0d));
        }

        private static ConstructionWorkflowManager CreateManager()
        {
            ConstructionWorkflowManager manager = new ConstructionWorkflowManager();
            manager.RegisterDefinition(new ConstructionWorkflowDefinition(
                "nursery-workflow",
                "nursery",
                new[] { new ConstructionPhase("excavation", 1d), new ConstructionPhase("wax", 1d) }));
            return manager;
        }
    }
}
