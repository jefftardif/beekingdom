using BeeKingdom.Population;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class CollectiveIntelligenceFrameworkTests
    {
        [Test]
        public void CalculatePrioritiesReflectsThreat()
        {
            CollectiveIntelligenceManager manager = CreateManager();

            var priorities = manager.CalculateColonyPriorities(new ColonyStateContext(threatPressure: 0.9d));

            Assert.That(priorities[ColonyPriorityType.Defend], Is.EqualTo(0.9d));
        }

        [Test]
        public void BroadcastSignalUpdatesSwarmState()
        {
            CollectiveIntelligenceManager manager = CreateManager();

            manager.BroadcastSignal(new SwarmSignal("alarm", SwarmSignalType.AlarmPheromone, 1d, 5d, 1d, 0.5d, 1d));

            Assert.That(manager.QuerySwarmState().ActiveSignals.Count, Is.EqualTo(1));
        }

        [Test]
        public void EmergencyDefenseActivatesFromThreat()
        {
            CollectiveIntelligenceManager manager = CreateManager();

            CollectiveBehaviorType behavior = manager.EvaluateColonyIntent(new ColonyStateContext(threatPressure: 0.95d));

            Assert.That(behavior, Is.EqualTo(CollectiveBehaviorType.EmergencyDefense));
            Assert.That(manager.Diagnostics.EmergencyProtocols, Is.EqualTo(1));
        }

        [Test]
        public void CooperationScoreIsDeterministic()
        {
            CollectiveIntelligenceManager first = CreateManager();
            CollectiveIntelligenceManager second = CreateManager();
            first.CalculateColonyPriorities(new ColonyStateContext(resourcePressure: 0.5d));
            second.CalculateColonyPriorities(new ColonyStateContext(resourcePressure: 0.5d));

            Assert.That(first.QueryCooperationScore(), Is.EqualTo(second.QueryCooperationScore()));
        }

        private static CollectiveIntelligenceManager CreateManager()
        {
            CollectiveIntelligenceManager manager = new CollectiveIntelligenceManager();
            manager.RegisterCollectiveBehavior(new CollectiveBehaviorDefinition("food", CollectiveBehaviorType.FoodGathering, ColonyPriorityType.Produce, 0.2d));
            manager.RegisterCollectiveBehavior(new CollectiveBehaviorDefinition("defense", CollectiveBehaviorType.EmergencyDefense, ColonyPriorityType.Defend, 0.8d));
            return manager;
        }
    }
}
