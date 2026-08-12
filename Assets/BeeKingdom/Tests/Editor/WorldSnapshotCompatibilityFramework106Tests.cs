using System;
using System.Linq;
using BeeKingdom.Core.Save;
using BeeKingdom.Core.Time;
using BeeKingdom.Economy;
using BeeKingdom.World;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class WorldSnapshotCompatibilityFramework106Tests
    {
        [Test]
        public void SameDataProducesSameChecksum()
        {
            WorldSnapshotPackage first = BuildPackage(WorldWeather.Clear);
            WorldSnapshotPackage second = BuildPackage(WorldWeather.Clear);

            Assert.That(first.Checksum, Is.EqualTo(second.Checksum));
        }

        [Test]
        public void WeatherChangeChangesChecksum()
        {
            WorldSnapshotPackage first = BuildPackage(WorldWeather.Clear);
            WorldSnapshotPackage second = BuildPackage(WorldWeather.Rain);

            Assert.That(first.Checksum, Is.Not.EqualTo(second.Checksum));
        }

        [Test]
        public void MissingBiomeProducesCompatibilityError()
        {
            WorldSnapshot world = World();
            RegionSnapshot region = Region("b", WorldWeather.Clear);
            WorldSnapshotPackage package = new WorldSnapshotPackageBuilder().Build(world, new[] { new RegionSnapshotEntry(region) }, Array.Empty<BiomeSnapshotReference>(), new[] { new RegionalWeatherSnapshotEntry(Weather(region, WorldWeather.Clear)) }, Array.Empty<RegionalResourceDistributionEntry>());

            WorldSnapshotCompatibilityResult result = new WorldSnapshotCompatibilityValidator().Validate(package);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0], Does.Contain("Missing biome"));
        }

        [Test]
        public void RegionInputOrderDoesNotChangePackage()
        {
            WorldSnapshot world = World();
            RegionSnapshot a = Region("a", WorldWeather.Clear);
            RegionSnapshot b = Region("b", WorldWeather.Rain);
            BiomeProfile biome = BiomeProfile.CreateStandard("prairie", WorldBiomeType.Prairie, WorldClimate.Temperate, 1d, 1d, 1d);
            WorldSnapshotPackageBuilder builder = new WorldSnapshotPackageBuilder();

            WorldSnapshotPackage first = builder.Build(world, new[] { new RegionSnapshotEntry(b), new RegionSnapshotEntry(a) }, new[] { new BiomeSnapshotReference("b", biome), new BiomeSnapshotReference("a", biome) }, new[] { new RegionalWeatherSnapshotEntry(Weather(b, WorldWeather.Rain)), new RegionalWeatherSnapshotEntry(Weather(a, WorldWeather.Clear)) }, Array.Empty<RegionalResourceDistributionEntry>());
            WorldSnapshotPackage second = builder.Build(world, new[] { new RegionSnapshotEntry(a), new RegionSnapshotEntry(b) }, new[] { new BiomeSnapshotReference("a", biome), new BiomeSnapshotReference("b", biome) }, new[] { new RegionalWeatherSnapshotEntry(Weather(a, WorldWeather.Clear)), new RegionalWeatherSnapshotEntry(Weather(b, WorldWeather.Rain)) }, Array.Empty<RegionalResourceDistributionEntry>());

            Assert.That(first.ToStablePayload(), Is.EqualTo(second.ToStablePayload()));
            Assert.That(first.Checksum, Is.EqualTo(second.Checksum));
        }

        [Test]
        public void PackagePayloadCanBeInsertedIntoSaveSnapshot()
        {
            WorldSnapshotPackage package = BuildPackage(WorldWeather.Clear);
            SaveSnapshot snapshot = new SaveSnapshot(1, "test", DateTime.UnixEpoch, DateTime.UnixEpoch, string.Empty, package.ToStablePayload());

            Assert.That(snapshot.Payload, Does.Contain("world|world-1"));
        }

        private static WorldSnapshotPackage BuildPackage(WorldWeather weather)
        {
            WorldSnapshot world = World();
            RegionSnapshot region = Region("region-1", weather);
            BiomeProfile biome = BiomeProfile.CreateStandard("prairie", WorldBiomeType.Prairie, WorldClimate.Temperate, 1d, 1d, 1d);
            RegionalResourceNodePlan plan = new RegionalResourceNodePlan("region-1-nectar-00", "region-1", new HexCoordinates(0, 0), ResourceType.Nectar, RegionalResourceCategory.Floral, 100d, 50d, 1d, 10);
            return new WorldSnapshotPackageBuilder().Build(world, new[] { new RegionSnapshotEntry(region) }, new[] { new BiomeSnapshotReference(region.RegionId, biome) }, new[] { new RegionalWeatherSnapshotEntry(Weather(region, weather)) }, new[] { new RegionalResourceDistributionEntry(plan) });
        }

        private static WorldSnapshot World()
        {
            return new WorldSnapshot("world-1", new WorldSeed("seed"), SimulationSeason.Spring, WorldWeather.Clear, 1, 0);
        }

        private static RegionSnapshot Region(string regionId, WorldWeather weather)
        {
            return new RegionSnapshot(regionId, "world-1", new WorldSeed("seed"), WorldBiomeType.Prairie, weather, SimulationSeason.Spring, 20d, 0.5d, 0, 0, RegionSimulationState.Active);
        }

        private static RegionalWeatherSnapshot Weather(RegionSnapshot region, WorldWeather weather)
        {
            return new RegionalWeatherSnapshot(region.WorldId, region.RegionId, region.Biome, region.Season, weather, region.Temperature, region.Humidity, 1, "weather");
        }
    }
}
