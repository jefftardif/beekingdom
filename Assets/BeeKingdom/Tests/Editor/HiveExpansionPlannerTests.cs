using BeeKingdom.Buildings;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class HiveExpansionPlannerTests
    {
        [Test]
        public void GeneratePlanCreatesCostAndRecommendation()
        {
            HiveExpansionManager manager = new HiveExpansionManager();
            ExpansionPlan plan = manager.GenerateExpansionPlan(ExpansionGoal.PopulationGrowth, 9, 10, 80d);

            Assert.That(plan.Priority, Is.GreaterThan(0));
            Assert.That(plan.RecommendedOrder.Count, Is.EqualTo(1));
            Assert.That(manager.RecommendNextConstruction().PlanId, Is.EqualTo(plan.PlanId));
        }

        [Test]
        public void PredictCapacityDetectsNearSaturation()
        {
            HiveExpansionManager manager = new HiveExpansionManager();

            ExpansionForecast forecast = manager.PredictCapacity(9, 10, 1d);

            Assert.That(forecast.SaturationDays, Is.EqualTo(1d));
            Assert.That(forecast.GrowthRisk, Is.EqualTo(1d));
        }

        [Test]
        public void QueryPlansOrdersByPriority()
        {
            HiveExpansionManager manager = new HiveExpansionManager();
            manager.GenerateExpansionPlan(ExpansionGoal.FoodProduction, 1, 20, 90d);
            manager.GenerateExpansionPlan(ExpansionGoal.Logistics, 1, 20, 30d);

            Assert.That(manager.QueryExpansionPlans()[0].Goal, Is.EqualTo(ExpansionGoal.Logistics));
        }
    }
}
