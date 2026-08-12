using BeeKingdom.Core.Simulation;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class SimulationStatisticsEngineTests
    {
        [Test]
        public void RecordsAndAggregatesMetricsDeterministically()
        {
            SimulationStatisticsEngine engine = new SimulationStatisticsEngine();
            engine.RegisterMetric(new SimulationMetricDefinition("population", SimulationMetricAggregation.Last));
            engine.RegisterMetric(new SimulationMetricDefinition("nectar", SimulationMetricAggregation.Sum));

            engine.RecordSample("population", 1d, 10d);
            engine.RecordSample("population", 2d, 12d);
            engine.RecordSample("nectar", 1d, 3d);
            engine.RecordSample("nectar", 2d, 4d);

            Assert.That(engine.TryGetValue("population", out SimulationMetricValue population), Is.True);
            Assert.That(population.Value, Is.EqualTo(12d));
            Assert.That(engine.TryGetValue("nectar", out SimulationMetricValue nectar), Is.True);
            Assert.That(nectar.Value, Is.EqualTo(7d));
        }

        [Test]
        public void SnapshotOrdersMetricsByIdentifier()
        {
            SimulationStatisticsEngine engine = new SimulationStatisticsEngine();
            engine.RegisterMetric(new SimulationMetricDefinition("z", SimulationMetricAggregation.Max));
            engine.RegisterMetric(new SimulationMetricDefinition("a", SimulationMetricAggregation.Min));
            engine.RecordSample("z", 1d, 3d);
            engine.RecordSample("a", 1d, 2d);

            SimulationStatisticsSnapshot snapshot = engine.CreateSnapshot(42d);

            Assert.That(snapshot.Version, Is.EqualTo(1));
            Assert.That(snapshot.SimulationSeconds, Is.EqualTo(42d));
            Assert.That(snapshot.Metrics[0].MetricId, Is.EqualTo("a"));
            Assert.That(snapshot.Metrics[1].MetricId, Is.EqualTo("z"));
        }

        [Test]
        public void RejectsUnknownOrInvalidSamples()
        {
            SimulationStatisticsEngine engine = new SimulationStatisticsEngine();
            engine.RegisterMetric(new SimulationMetricDefinition("load", SimulationMetricAggregation.Average));

            Assert.That(engine.RecordSample("missing", 0d, 1d), Is.False);
            Assert.That(engine.RecordSample("load", 0d, double.NaN), Is.False);
            Assert.That(engine.Diagnostics.RejectedSamples, Is.EqualTo(2));
        }
    }
}
