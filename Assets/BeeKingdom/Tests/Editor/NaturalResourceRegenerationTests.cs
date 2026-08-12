using BeeKingdom.Economy;
using BeeKingdom.Gameplay;
using BeeKingdom.World;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class NaturalResourceRegenerationTests
    {
        [Test]
        public void NodeRegeneratesTowardCapacity()
        {
            RegenerationManager manager = new RegenerationManager();
            NaturalResourceNode node = new NaturalResourceNode("nectar", "region", new HexCoordinates(0, 0), ResourceType.Nectar, 100d, 0d, new ResourceNodeLifecycle(10d, 0.25d));
            manager.RegisterNode(node);

            manager.Execute(SimulationContextFactory.Create(5d));

            Assert.That(node.Amount, Is.EqualTo(50d));
            Assert.That(node.State, Is.EqualTo(ResourceNodeState.Available));
        }

        [Test]
        public void HarvestDepletesNode()
        {
            RegenerationManager manager = new RegenerationManager();
            NaturalResourceNode node = new NaturalResourceNode("pollen", "region", new HexCoordinates(0, 0), ResourceType.Pollen, 20d, 10d, new ResourceNodeLifecycle(1d, 0.25d));
            manager.RegisterNode(node);

            double harvested = manager.Harvest("pollen", 10d);

            Assert.That(harvested, Is.EqualTo(10d));
            Assert.That(node.State, Is.EqualTo(ResourceNodeState.Depleted));
            Assert.That(manager.Diagnostics.DepletedNodes, Is.EqualTo(1));
        }

        [Test]
        public void EcologicalBalanceChangesRegenerationSpeed()
        {
            NaturalResourceNode slow = new NaturalResourceNode("slow", "region", new HexCoordinates(0, 0), ResourceType.Nectar, 100d, 0d, new ResourceNodeLifecycle(1d, 0.25d));
            slow.Regenerate(10d, new EcologicalBalance(0.5d, 1d, 1d));

            NaturalResourceNode fast = new NaturalResourceNode("fast", "region", new HexCoordinates(0, 0), ResourceType.Nectar, 100d, 0d, new ResourceNodeLifecycle(1d, 0.25d));
            fast.Regenerate(10d, new EcologicalBalance(2d, 1d, 1d));

            Assert.That(fast.Amount, Is.GreaterThan(slow.Amount));
        }

        [Test]
        public void SeedFromRegionCreatesResourceNodes()
        {
            WorldManager worldManager = new WorldManager();
            WorldState world = worldManager.CreateWorld(new WorldSeed("regen"), WorldGenerationProfile.CreateDefault(WorldGenerationProfileType.Tutorial));
            HexGrid grid = HexGrid.FromWorld(world);
            RegenerationManager manager = new RegenerationManager();

            manager.SeedFromRegion(world.Regions["region-0-0"], grid);

            Assert.That(manager.Diagnostics.NodeCount, Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void RegenerationCapsAtCapacity()
        {
            NaturalResourceNode node = new NaturalResourceNode("water", "region", new HexCoordinates(0, 0), ResourceType.Water, 10d, 9d, new ResourceNodeLifecycle(10d, 0.25d));

            node.Regenerate(10d, new EcologicalBalance());

            Assert.That(node.Amount, Is.EqualTo(10d));
        }
    }
}
