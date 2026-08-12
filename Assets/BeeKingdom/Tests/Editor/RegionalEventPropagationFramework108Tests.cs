using System.Linq;
using BeeKingdom.Core.Time;
using BeeKingdom.World;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class RegionalEventPropagationFramework108Tests
    {
        [Test]
        public void PropagatesToDirectNeighbor()
        {
            RegionalEventPropagationEngine engine = CreateEngine();
            RegionalEventInstance instance = engine.StartEvent("storm-front", "a", 1d, 1);

            RegionalEventPropagationSnapshot snapshot = engine.Propagate(instance.EventId, 1);

            Assert.That(snapshot.AffectedRegions.Any(region => region.RegionId == "b" && region.Depth == 1), Is.True);
        }

        [Test]
        public void AttenuatesAtDepthTwo()
        {
            RegionalEventPropagationEngine engine = CreateEngine();
            RegionalEventInstance instance = engine.StartEvent("storm-front", "a", 1d, 1);

            RegionalEventAffectedRegion c = engine.Propagate(instance.EventId, 1).AffectedRegions.First(region => region.RegionId == "c");

            Assert.That(c.Depth, Is.EqualTo(2));
            Assert.That(c.Intensity, Is.EqualTo(0.25d).Within(0.0001d));
        }

        [Test]
        public void PreventsLoop()
        {
            RegionalEventPropagationEngine engine = CreateEngine();
            RegionalEventInstance instance = engine.StartEvent("storm-front", "a", 1d, 1);

            RegionalEventPropagationSnapshot snapshot = engine.Propagate(instance.EventId, 1);

            Assert.That(snapshot.AffectedRegions.Count(region => region.RegionId == "a"), Is.EqualTo(1));
        }

        [Test]
        public void AffectedRegionsAreStable()
        {
            RegionalEventPropagationEngine engine = CreateEngine();
            RegionalEventInstance instance = engine.StartEvent("storm-front", "a", 1d, 1);

            string[] ids = engine.Propagate(instance.EventId, 1).AffectedRegions.Select(region => region.RegionId).ToArray();

            Assert.That(ids, Is.EqualTo(ids.OrderBy(id => id).ToArray()));
        }

        [Test]
        public void ExpirationIsDeterministic()
        {
            RegionalEventPropagationEngine engine = CreateEngine();
            RegionalEventInstance instance = engine.StartEvent("storm-front", "a", 1d, 1);

            RegionalEventPropagationSnapshot snapshot = engine.Propagate(instance.EventId, 6);

            Assert.That(snapshot.Expired, Is.True);
        }

        [Test]
        public void UnknownSourceIsBlocked()
        {
            RegionalEventPropagationDiagnostics diagnostics = new RegionalEventPropagationDiagnostics();
            RegionalEventPropagationEngine engine = CreateEngine(diagnostics);

            Assert.That(engine.StartEvent("storm-front", "unknown", 1d, 1), Is.Null);
            Assert.That(diagnostics.BlockedCount, Is.EqualTo(1));
        }

        private static RegionalEventPropagationEngine CreateEngine(RegionalEventPropagationDiagnostics diagnostics = null)
        {
            RegionalEventPropagationEngine engine = new RegionalEventPropagationEngine(diagnostics);
            engine.RegisterDefinition(new RegionalEventDefinition("storm-front", "weather", new RegionalEventPropagationRule(2, 0.5d, 0.1d), 5));
            engine.RegisterRegion(Region("a", "b"));
            engine.RegisterRegion(Region("b", "a", "c"));
            engine.RegisterRegion(Region("c", "b"));
            return engine;
        }

        private static RegionDefinition Region(string id, params string[] neighbors)
        {
            return new RegionDefinition(id, "world-1", new WorldSeed("seed"), WorldBiomeType.Prairie, WorldWeather.Clear, SimulationSeason.Spring, 20d, 0.5d, 16, 8, 4, neighbors);
        }
    }
}
