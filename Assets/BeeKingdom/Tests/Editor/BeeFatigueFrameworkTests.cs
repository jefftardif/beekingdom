using BeeKingdom.Population;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class BeeFatigueFrameworkTests
    {
        [Test]
        public void IncreaseFatigueChangesState()
        {
            BeeFatigueManager manager = CreateManager();

            Assert.That(manager.IncreaseFatigue("bee-1", FatigueSource.Construction, 45d), Is.True);

            Assert.That(manager.GetFatigueState("bee-1"), Is.EqualTo(FatigueState.Tired));
        }

        [Test]
        public void RecoveryReducesFatigue()
        {
            BeeFatigueManager manager = CreateManager();
            manager.IncreaseFatigue("bee-1", FatigueSource.Harvesting, 50d);

            manager.RecoverFatigue(new FatigueContext("bee-1"), 2d);

            Assert.That(manager.QueryFatigue("bee-1").CurrentFatigue, Is.EqualTo(30d));
        }

        [Test]
        public void BurnoutDoesNotRemoveBee()
        {
            BeeFatigueManager manager = CreateManager();

            manager.IncreaseFatigue("bee-1", FatigueSource.Combat, 70d);

            Assert.That(manager.GetFatigueState("bee-1"), Is.EqualTo(FatigueState.Burnout));
            Assert.That(manager.QueryFatigue("bee-1"), Is.Not.Null);
        }

        [Test]
        public void PerformanceModifierIsDeterministic()
        {
            BeeFatigueManager first = CreateManager();
            BeeFatigueManager second = CreateManager();

            first.IncreaseFatigue("bee-1", FatigueSource.Movement, 25d);
            second.IncreaseFatigue("bee-1", FatigueSource.Movement, 25d);

            Assert.That(first.QueryFatigue("bee-1").PerformanceModifier, Is.EqualTo(second.QueryFatigue("bee-1").PerformanceModifier));
        }

        private static BeeFatigueManager CreateManager()
        {
            BeeFatigueManager manager = new BeeFatigueManager();
            manager.RegisterFatigueDefinition(new FatigueDefinition("worker-fatigue", 100d, 10d, 40d, 70d, 90d, 0.8d));
            manager.CreateFatigueRecord("bee-1", "worker-fatigue");
            return manager;
        }
    }
}
