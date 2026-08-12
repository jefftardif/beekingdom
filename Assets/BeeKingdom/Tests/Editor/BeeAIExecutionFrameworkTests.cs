using BeeKingdom.AI;
using BeeKingdom.Population;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class BeeAIExecutionFrameworkTests
    {
        [Test]
        public void ExecuteBehaviorStartsRegisteredBehavior()
        {
            BeeAIManager manager = CreateManager();

            BehaviorContext context = manager.ExecuteBehavior("bee-1", BeeIntent.Build, "cell-1");

            Assert.That(context, Is.Not.Null);
            Assert.That(context.State, Is.EqualTo(BehaviorExecutionState.Working));
        }

        [Test]
        public void BehaviorCompletesAfterDuration()
        {
            BeeAIManager manager = CreateManager();
            manager.CreateBrain("bee-1", 10, 10);
            manager.ExecuteBehavior("bee-1", BeeIntent.Build, "cell-1");

            Assert.That(manager.UpdateBehavior("bee-1", 2d), Is.True);
            Assert.That(manager.QueryBehavior("bee-1"), Is.Null);
        }

        [Test]
        public void InterruptAndResumeBehavior()
        {
            BeeAIManager manager = CreateManager();
            manager.ExecuteBehavior("bee-1", BeeIntent.Build, "cell-1");

            Assert.That(manager.InterruptBehavior("bee-1", "danger"), Is.True);
            Assert.That(manager.QueryBehavior("bee-1").State, Is.EqualTo(BehaviorExecutionState.Interrupted));
            Assert.That(manager.ResumeBehavior("bee-1"), Is.True);
        }

        [Test]
        public void CancelBehaviorRemovesContext()
        {
            BeeAIManager manager = CreateManager();
            manager.ExecuteBehavior("bee-1", BeeIntent.Build, "cell-1");

            Assert.That(manager.CancelBehavior("bee-1"), Is.True);
            Assert.That(manager.QueryBehavior("bee-1"), Is.Null);
        }

        private static BeeAIManager CreateManager()
        {
            BeeAIManager manager = new BeeAIManager();
            manager.RegisterBehavior(new BehaviorDefinition("build", BeeIntent.Build, BehaviorActionType.Build, 1d));
            return manager;
        }
    }
}
