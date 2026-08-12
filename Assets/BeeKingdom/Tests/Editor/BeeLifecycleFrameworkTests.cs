using BeeKingdom.Population;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class BeeLifecycleFrameworkTests
    {
        [Test]
        public void BeeTransitionsThroughLifecycle()
        {
            BeeLifecycleManager manager = CreateManager();
            manager.RegisterBee("bee", "worker-life");

            manager.AdvanceLifecycle("bee", 4d);

            Assert.That(manager.QueryStage("bee"), Is.EqualTo(LifecycleStage.Adult));
            Assert.That(manager.Diagnostics.StageChanges, Is.GreaterThan(0));
        }

        [Test]
        public void BiologicalAgeUsesMultiplier()
        {
            BeeLifecycleManager manager = CreateManager();

            Assert.That(manager.CalculateBiologicalAge(10d, 0.5d), Is.EqualTo(5d));
        }

        [Test]
        public void LongevityCausesDeath()
        {
            BeeLifecycleManager manager = CreateManager();
            manager.RegisterBee("bee", "worker-life");

            manager.AdvanceLifecycle("bee", 20d);

            Assert.That(manager.QueryStage("bee"), Is.EqualTo(LifecycleStage.Death));
            Assert.That(manager.Diagnostics.Deaths, Is.EqualTo(1));
        }

        private static BeeLifecycleManager CreateManager()
        {
            BeeLifecycleManager manager = new BeeLifecycleManager();
            manager.RegisterLifecycleDefinition(new LifecycleDefinition("worker-life", new[]
            {
                new LifecycleTransition(LifecycleStage.Egg, LifecycleStage.Larva, 1d),
                new LifecycleTransition(LifecycleStage.Larva, LifecycleStage.Pupa, 2d),
                new LifecycleTransition(LifecycleStage.Pupa, LifecycleStage.YoungAdult, 3d),
                new LifecycleTransition(LifecycleStage.YoungAdult, LifecycleStage.Adult, 4d)
            }, 10d));
            return manager;
        }
    }
}
