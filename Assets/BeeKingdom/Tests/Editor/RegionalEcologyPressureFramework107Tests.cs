using BeeKingdom.Core.Time;
using BeeKingdom.Economy;
using BeeKingdom.World;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class RegionalEcologyPressureFramework107Tests
    {
        [Test]
        public void RichHumidRegionHasLowPressure()
        {
            RegionalEcologySnapshot snapshot = new RegionalEcologyEvaluator().Evaluate(Input(WorldWeather.Rain, 1d, 1d, 1d, 100d, 90d));

            Assert.That(snapshot.State, Is.EqualTo(RegionalEcologyStateKind.Healthy));
            Assert.That(snapshot.PressureScore, Is.LessThan(0.25d));
        }

        [Test]
        public void StrongDepletionIncreasesPressure()
        {
            RegionalEcologySnapshot snapshot = new RegionalEcologyEvaluator().Evaluate(Input(WorldWeather.Clear, 1d, 1d, 1d, 100d, 0d));

            Assert.That(snapshot.State, Is.Not.EqualTo(RegionalEcologyStateKind.Healthy));
            Assert.That(snapshot.DepletionPressure, Is.EqualTo(1d));
        }

        [Test]
        public void StormIncreasesClimateRisk()
        {
            RegionalEcologySnapshot clear = new RegionalEcologyEvaluator().Evaluate(Input(WorldWeather.Clear, 1d, 1d, 1d, 100d, 50d));
            RegionalEcologySnapshot storm = new RegionalEcologyEvaluator().Evaluate(Input(WorldWeather.Storm, 1d, 1d, 1d, 100d, 50d));

            Assert.That(storm.PressureScore, Is.GreaterThan(clear.PressureScore));
        }

        [Test]
        public void SameInputProducesSameSnapshot()
        {
            RegionalEcologyEvaluator evaluator = new RegionalEcologyEvaluator();
            RegionalEcologyInput input = Input(WorldWeather.Clear, 0.8d, 0.9d, 0.7d, 100d, 30d);

            Assert.That(evaluator.Evaluate(input).Equals(evaluator.Evaluate(input)), Is.True);
        }

        [Test]
        public void ThresholdIsPublishedOnceByState()
        {
            RegionalEcologyState state = new RegionalEcologyState();
            RegionalEcologySnapshot critical = new RegionalEcologyEvaluator().Evaluate(Input(WorldWeather.Storm, 0d, 0d, 0d, 100d, 0d));

            Assert.That(state.Apply(critical), Is.True);
            Assert.That(state.Apply(critical), Is.False);
        }

        private static RegionalEcologyInput Input(WorldWeather weather, double pollination, double biome, double water, double capacity, double amount)
        {
            RegionalWeatherSnapshot weatherSnapshot = new RegionalWeatherSnapshot("world-1", "region-1", WorldBiomeType.Prairie, SimulationSeason.Spring, weather, 20d, 0.5d, 1, "weather");
            RegionalResourceNodePlan plan = new RegionalResourceNodePlan("node-1", "region-1", new HexCoordinates(0, 0), ResourceType.Nectar, RegionalResourceCategory.Floral, capacity, amount, 1d, 1);
            RegionalResourceDistributionSnapshot resources = new RegionalResourceDistributionSnapshot("world-1", "region-1", 1, SimulationSeason.Spring, weather, new[] { plan });
            return new RegionalEcologyInput("world-1", "region-1", weatherSnapshot, resources, pollination, biome, water);
        }
    }
}
