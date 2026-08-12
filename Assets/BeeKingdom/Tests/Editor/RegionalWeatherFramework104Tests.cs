using System;
using BeeKingdom.Core.Time;
using BeeKingdom.World;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class RegionalWeatherFramework104Tests
    {
        [Test]
        public void PrairieAcceptsAllowedWeather()
        {
            RegionalWeatherResolver resolver = new RegionalWeatherResolver(new WorldSeed("weather"));
            BiomeProfile prairie = BiomeProfile.CreateStandard("prairie", WorldBiomeType.Prairie, WorldClimate.Temperate, 1d, 1d, 1d);
            RegionDefinition region = Region(WorldBiomeType.Prairie, WorldWeather.Clear, 20d, 0.5d);

            RegionalWeatherSnapshot snapshot = resolver.Resolve(region, prairie, SimulationSeason.Spring, WorldWeather.Rain, 1);

            Assert.That(snapshot.Weather, Is.EqualTo(WorldWeather.Rain));
        }

        [Test]
        public void DryBiomeCorrectsStormDeterministically()
        {
            RegionalWeatherDiagnostics diagnostics = new RegionalWeatherDiagnostics();
            RegionalWeatherResolver resolver = new RegionalWeatherResolver(new WorldSeed("weather"), diagnostics);
            BiomeProfile farmland = BiomeProfile.CreateStandard("farmland", WorldBiomeType.Farmland, WorldClimate.Dry, 1d, 1d, 1d);
            RegionDefinition region = Region(WorldBiomeType.Farmland, WorldWeather.Storm, 28d, 0.3d);

            RegionalWeatherSnapshot first = resolver.Resolve(region, farmland, SimulationSeason.Summer, WorldWeather.Storm, 4);
            RegionalWeatherSnapshot second = resolver.Resolve(region, farmland, SimulationSeason.Summer, WorldWeather.Storm, 4);

            Assert.That(first.Weather, Is.Not.EqualTo(WorldWeather.Storm));
            Assert.That(first.Equals(second), Is.True);
            Assert.That(diagnostics.CorrectionCount, Is.EqualTo(2));
        }

        [Test]
        public void DifferentBiomesCanResolveDifferentWeatherFromSameBase()
        {
            RegionalWeatherResolver resolver = new RegionalWeatherResolver(new WorldSeed("weather"));
            BiomeProfile wet = BiomeProfile.CreateStandard("wetland", WorldBiomeType.Wetland, WorldClimate.Humid, 1d, 1d, 1d);
            BiomeProfile dry = BiomeProfile.CreateStandard("farmland", WorldBiomeType.Farmland, WorldClimate.Dry, 1d, 1d, 1d);

            RegionalWeatherSnapshot wetSnapshot = resolver.Resolve(Region(WorldBiomeType.Wetland, WorldWeather.Storm, 20d, 0.8d), wet, SimulationSeason.Spring, WorldWeather.Storm, 9);
            RegionalWeatherSnapshot drySnapshot = resolver.Resolve(Region(WorldBiomeType.Farmland, WorldWeather.Storm, 20d, 0.3d), dry, SimulationSeason.Spring, WorldWeather.Storm, 9);

            Assert.That(wetSnapshot.Weather, Is.EqualTo(WorldWeather.Storm));
            Assert.That(drySnapshot.Weather, Is.Not.EqualTo(WorldWeather.Storm));
        }

        [Test]
        public void MissingBiomeProducesRejectionDiagnostic()
        {
            RegionalWeatherDiagnostics diagnostics = new RegionalWeatherDiagnostics();
            RegionalWeatherResolver resolver = new RegionalWeatherResolver(new WorldSeed("weather"), diagnostics);

            Assert.Throws<ArgumentNullException>(() => resolver.Resolve(Region(WorldBiomeType.Prairie, WorldWeather.Clear, 20d, 0.5d), null, SimulationSeason.Spring, WorldWeather.Clear, 1));
            Assert.That(diagnostics.RejectionCount, Is.EqualTo(1));
            Assert.That(diagnostics.MissingBiomeCount, Is.EqualTo(1));
        }

        [Test]
        public void SnapshotRegistryReportsChangesOnlyWhenWeatherChanges()
        {
            RegionalWeatherSnapshotRegistry registry = new RegionalWeatherSnapshotRegistry();
            RegionalWeatherSnapshot first = new RegionalWeatherSnapshot("world-1", "region-1", WorldBiomeType.Prairie, SimulationSeason.Spring, WorldWeather.Clear, 20d, 0.5d, 1, "profile");
            RegionalWeatherSnapshot sameWeather = new RegionalWeatherSnapshot("world-1", "region-1", WorldBiomeType.Prairie, SimulationSeason.Spring, WorldWeather.Clear, 21d, 0.6d, 2, "profile");

            Assert.That(registry.Apply(first), Is.True);
            Assert.That(registry.Apply(sameWeather), Is.False);
        }

        [Test]
        public void SnapshotContainsRequiredFields()
        {
            RegionalWeatherResolver resolver = new RegionalWeatherResolver(new WorldSeed("weather"));
            BiomeProfile prairie = BiomeProfile.CreateStandard("prairie", WorldBiomeType.Prairie, WorldClimate.Temperate, 1d, 1d, 1d);

            RegionalWeatherSnapshot snapshot = resolver.Resolve(Region(WorldBiomeType.Prairie, WorldWeather.Cloudy, 19d, 0.4d), prairie, SimulationSeason.Autumn, WorldWeather.Cloudy, 12);

            Assert.That(snapshot.WorldId, Is.EqualTo("world-1"));
            Assert.That(snapshot.RegionId, Is.EqualTo("region-1"));
            Assert.That(snapshot.Season, Is.EqualTo(SimulationSeason.Autumn));
            Assert.That(snapshot.WeatherStep, Is.EqualTo(12));
            Assert.That(snapshot.Temperature, Is.EqualTo(19d));
            Assert.That(snapshot.Humidity, Is.GreaterThan(0d));
        }

        [Test]
        public void ResolverDoesNotModifyWeatherManager()
        {
            WeatherManager weatherManager = new WeatherManager(new WorldSeed("weather"));
            weatherManager.SetWeather(WorldWeather.Storm);
            RegionalWeatherResolver resolver = new RegionalWeatherResolver(new WorldSeed("weather"));
            BiomeProfile dry = BiomeProfile.CreateStandard("farmland", WorldBiomeType.Farmland, WorldClimate.Dry, 1d, 1d, 1d);

            resolver.Resolve(Region(WorldBiomeType.Farmland, WorldWeather.Storm, 25d, 0.3d), dry, SimulationSeason.Summer, weatherManager.CurrentWeather, 3);

            Assert.That(weatherManager.CurrentWeather, Is.EqualTo(WorldWeather.Storm));
        }

        private static RegionDefinition Region(WorldBiomeType biome, WorldWeather weather, double temperature, double humidity)
        {
            return new RegionDefinition("region-1", "world-1", new WorldSeed("region"), biome, weather, SimulationSeason.Spring, temperature, humidity, 16, 8, 4);
        }
    }
}
