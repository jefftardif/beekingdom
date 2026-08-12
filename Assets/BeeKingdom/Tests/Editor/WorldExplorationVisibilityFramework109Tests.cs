using System.Linq;
using BeeKingdom.World;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class WorldExplorationVisibilityFramework109Tests
    {
        [Test]
        public void UnknownRegionBecomesDiscovered()
        {
            WorldExplorationVisibilityMap map = new WorldExplorationVisibilityMap();

            map.MarkDiscovered("colony-1", "world-1", "region-1", 1);

            Assert.That(map.QueryKnowledge("colony-1").QueryRegion("region-1").State, Is.EqualTo(ExplorationVisibilityState.Discovered));
        }

        [Test]
        public void VisibleExpiresToStale()
        {
            WorldExplorationVisibilityMap map = new WorldExplorationVisibilityMap();
            map.MarkVisible("colony-1", "world-1", "region-1", 1, 5);

            map.UpdateExpirations(6);

            Assert.That(map.QueryKnowledge("colony-1").QueryRegion("region-1").State, Is.EqualTo(ExplorationVisibilityState.Stale));
        }

        [Test]
        public void ColoniesHaveDistinctMaps()
        {
            WorldExplorationVisibilityMap map = new WorldExplorationVisibilityMap();
            map.MarkVisible("colony-1", "world-1", "region-1", 1, 10);
            map.MarkDiscovered("colony-2", "world-1", "region-1", 1);

            Assert.That(map.QueryKnowledge("colony-1").QueryRegion("region-1").State, Is.EqualTo(ExplorationVisibilityState.Visible));
            Assert.That(map.QueryKnowledge("colony-2").QueryRegion("region-1").State, Is.EqualTo(ExplorationVisibilityState.Discovered));
        }

        [Test]
        public void SnapshotIsSortedByColonyThenRegion()
        {
            WorldExplorationVisibilityMap map = new WorldExplorationVisibilityMap();
            map.MarkDiscovered("colony-b", "world-1", "region-b", 1);
            map.MarkDiscovered("colony-a", "world-1", "region-b", 1);
            map.MarkDiscovered("colony-a", "world-1", "region-a", 1);

            string[] order = map.CreateSnapshot().Records.Select(record => record.ColonyId + ":" + record.RegionId).ToArray();

            Assert.That(order, Is.EqualTo(order.OrderBy(value => value).ToArray()));
        }

        [Test]
        public void DoesNotRequireUnityObjects()
        {
            WorldExplorationVisibilityMap map = new WorldExplorationVisibilityMap();
            Assert.That(map.CreateSnapshot().Records.Count, Is.EqualTo(0));
        }
    }
}
