using BeeKingdom.Buildings;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class ConstructionPriorityEngineTests
    {
        [Test]
        public void EvaluatePriorityUsesWeightedRules()
        {
            ConstructionPriorityManager manager = CreateManager();

            PriorityResult result = manager.EvaluatePriority("default", new PriorityContext("a", colonyEmergency: 3d));

            Assert.That(result.Level, Is.EqualTo(ConstructionPriorityLevel.Critical));
            Assert.That(manager.Diagnostics.Calculated, Is.EqualTo(1));
        }

        [Test]
        public void OverridePromoteAndDemoteAreApplied()
        {
            ConstructionPriorityManager manager = CreateManager();
            manager.EvaluatePriority("default", new PriorityContext("a"));

            manager.PromoteConstruction("a");
            PriorityResult promoted = manager.EvaluatePriority("default", new PriorityContext("a"));
            Assert.That(promoted.Level, Is.EqualTo(ConstructionPriorityLevel.High));

            manager.DemoteConstruction("a");
            PriorityResult demoted = manager.EvaluatePriority("default", new PriorityContext("a"));
            Assert.That(demoted.Level, Is.EqualTo(ConstructionPriorityLevel.Normal));
        }

        [Test]
        public void RecalculateOrdersByScoreThenId()
        {
            ConstructionPriorityManager manager = CreateManager();
            var results = manager.RecalculatePriorities("default", new[]
            {
                new PriorityContext("b", colonyEmergency: 1d),
                new PriorityContext("a", colonyEmergency: 1d),
                new PriorityContext("c", colonyEmergency: 2d)
            });

            Assert.That(results[0].ConstructionId, Is.EqualTo("c"));
            Assert.That(results[1].ConstructionId, Is.EqualTo("a"));
        }

        private static ConstructionPriorityManager CreateManager()
        {
            ConstructionPriorityManager manager = new ConstructionPriorityManager();
            manager.RegisterDefinition(new ConstructionPriorityDefinition("default", ConstructionPriorityLevel.Normal, new[]
            {
                new PriorityRule("emergency", 700d, context => context.ColonyEmergency)
            }));
            return manager;
        }
    }
}
