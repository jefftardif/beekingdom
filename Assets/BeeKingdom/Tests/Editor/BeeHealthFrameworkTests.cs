using BeeKingdom.Population;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class BeeHealthFrameworkTests
    {
        [Test]
        public void ApplyDamageCreatesInjuredState()
        {
            BeeHealthManager manager = CreateManager();

            Assert.That(manager.ApplyDamage("bee-1", 30d, new InjuryRecord("scratch", InjuryKind.Minor, 0.3d, 2d, 1d)), Is.True);

            Assert.That(manager.QueryHealth("bee-1").State, Is.EqualTo(HealthState.Injured));
        }

        [Test]
        public void HealBeeRestoresHealth()
        {
            BeeHealthManager manager = CreateManager();
            manager.ApplyDamage("bee-1", 30d);

            manager.HealBee("bee-1", 20d);

            Assert.That(manager.QueryHealth("bee-1").CurrentHealth, Is.EqualTo(90d));
        }

        [Test]
        public void DiseaseCanBeAppliedAndCured()
        {
            BeeHealthManager manager = CreateManager();

            manager.ApplyDisease("bee-1", new DiseaseRecord("infection", DiseaseKind.Infection, 0.8d, 0.2d));
            Assert.That(manager.QueryHealth("bee-1").State, Is.EqualTo(HealthState.Sick));

            Assert.That(manager.CureDisease("bee-1", "infection"), Is.True);
            Assert.That(manager.EvaluateHealth(new HealthEvaluationContext("bee-1")), Is.EqualTo(HealthState.Perfect));
        }

        [Test]
        public void CriticalAndDeadStatesAreDeterministic()
        {
            BeeHealthManager manager = CreateManager();

            manager.ApplyDamage("bee-1", 95d);
            Assert.That(manager.QueryHealth("bee-1").State, Is.EqualTo(HealthState.Critical));

            manager.ApplyDamage("bee-1", 5d);
            Assert.That(manager.QueryHealth("bee-1").State, Is.EqualTo(HealthState.Dead));
        }

        private static BeeHealthManager CreateManager()
        {
            BeeHealthManager manager = new BeeHealthManager();
            manager.RegisterHealthDefinition(new HealthDefinition("worker-health", 100d, 10d, 10d, 60d, 0.5d));
            manager.CreateHealthRecord("bee-1", "worker-health");
            return manager;
        }
    }
}
