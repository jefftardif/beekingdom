using BeeKingdom.Core.Time;
using BeeKingdom.Gameplay;
using BeeKingdom.World;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class FlowerEcosystemTests
    {
        [Test]
        public void PatchBloomsAfterGrowthDuration()
        {
            FlowerManager manager = CreateManager();
            FlowerPatch patch = manager.CreatePatch("patch-1", "clover", "region", new HexCoordinates(0, 0));

            manager.Execute(SimulationContextFactory.Create(11d));

            Assert.That(patch.Stage, Is.EqualTo(FlowerGrowthStage.Blooming));
            Assert.That(manager.Diagnostics.BloomingCount, Is.EqualTo(1));
        }

        [Test]
        public void BloomingPatchProducesNectarAndPollen()
        {
            FlowerManager manager = CreateManager();
            FlowerPatch patch = manager.CreatePatch("patch-1", "clover", "region", new HexCoordinates(0, 0));

            manager.Execute(SimulationContextFactory.Create(20d));

            Assert.That(patch.Nectar, Is.GreaterThan(0d));
            Assert.That(patch.Pollen, Is.GreaterThan(0d));
        }

        [Test]
        public void HarvestCanDepletePatch()
        {
            FlowerManager manager = CreateManager();
            FlowerPatch patch = manager.CreatePatch("patch-1", "clover", "region", new HexCoordinates(0, 0));
            manager.Execute(SimulationContextFactory.Create(20d));

            FlowerHarvestResult result = manager.Harvest(patch.PatchId, 100d, 100d);

            Assert.That(result.IsDepleted, Is.True);
            Assert.That(manager.Diagnostics.DepletedCount, Is.EqualTo(1));
        }

        [Test]
        public void RainySpringRegeneratesFasterThanStormyWinter()
        {
            FlowerManager spring = CreateManager();
            FlowerPatch springPatch = spring.CreatePatch("spring", "clover", "region", new HexCoordinates(0, 0));
            spring.SetEnvironment(SimulationSeason.Spring, WorldWeather.Rain);
            spring.Execute(SimulationContextFactory.Create(20d));

            FlowerManager winter = CreateManager();
            FlowerPatch winterPatch = winter.CreatePatch("winter", "clover", "region", new HexCoordinates(0, 0));
            winter.SetEnvironment(SimulationSeason.Winter, WorldWeather.Storm);
            winter.Execute(SimulationContextFactory.Create(20d));

            Assert.That(springPatch.Nectar, Is.GreaterThan(winterPatch.Nectar));
        }

        [Test]
        public void SeedFromRegionCreatesPatchesOnHexCells()
        {
            WorldManager worldManager = new WorldManager();
            WorldState world = worldManager.CreateWorld(new WorldSeed("flowers"), WorldGenerationProfile.CreateDefault(WorldGenerationProfileType.Tutorial));
            HexGrid grid = HexGrid.FromWorld(world);
            FlowerManager flowers = new FlowerManager();

            flowers.SeedFromRegion(world.Regions["region-0-0"], grid);

            Assert.That(flowers.Diagnostics.PatchCount, Is.EqualTo(7));
        }

        private static FlowerManager CreateManager()
        {
            FlowerManager manager = new FlowerManager();
            manager.RegisterSpecies(new FlowerSpecies("clover", "Clover", 10d, 8d, new BloomCycle(10d, 20d, 10d, 10d), PollinationRules.CreateDefault()));
            return manager;
        }
    }
}
