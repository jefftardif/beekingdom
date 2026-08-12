using BeeKingdom.Core.Time;
using BeeKingdom.World;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class BiomeFramework103Tests
    {
        [Test]
        public void RegistersValidBiome()
        {
            BiomeRegistry registry = new BiomeRegistry();

            BiomeValidationResult result = registry.RegisterBiome(BiomeProfile.CreateStandard("meadow", WorldBiomeType.Meadow, WorldClimate.Temperate, 1.2d, 0.9d, 0.8d));

            Assert.That(result.IsValid, Is.True);
            Assert.That(registry.QueryBiome("meadow").BiomeType, Is.EqualTo(WorldBiomeType.Meadow));
        }

        [Test]
        public void RejectsEmptyAndDuplicateIds()
        {
            BiomeRegistry registry = new BiomeRegistry();
            registry.RegisterBiome(BiomeProfile.CreateStandard("prairie", WorldBiomeType.Prairie, WorldClimate.Temperate, 1d, 1d, 1d));

            BiomeValidationResult empty = registry.RegisterBiome(BiomeProfile.CreateStandard("", WorldBiomeType.Forest, WorldClimate.Humid, 1d, 1d, 1d));
            BiomeValidationResult duplicate = registry.RegisterBiome(BiomeProfile.CreateStandard("prairie", WorldBiomeType.Forest, WorldClimate.Humid, 1d, 1d, 1d));

            Assert.That(empty.IsValid, Is.False);
            Assert.That(duplicate.IsValid, Is.False);
        }

        [Test]
        public void ResolvesBiomeFromRegionDefinition()
        {
            BiomeRegistry registry = BiomeRegistry.CreateStandardRegistry();
            BiomeResolver resolver = new BiomeResolver(registry);
            RegionDefinition region = new RegionDefinition("region-1", "world-1", new WorldSeed("seed"), WorldBiomeType.Orchard, WorldWeather.Clear, SimulationSeason.Spring, 20d, 0.5d, 16, 8, 4);

            BiomeProfile profile = resolver.Resolve(region);

            Assert.That(profile.BiomeType, Is.EqualTo(WorldBiomeType.Orchard));
        }

        [Test]
        public void ResolutionIsDeterministicForSameSeed()
        {
            BiomeRegistry registry = BiomeRegistry.CreateStandardRegistry();
            BiomeResolver resolver = new BiomeResolver(registry);
            WorldSeed seed = new WorldSeed("repeatable");

            BiomeProfile first = resolver.ResolveDeterministic(seed, registry.QueryBiomes());
            BiomeProfile second = resolver.ResolveDeterministic(seed, registry.QueryBiomes());

            Assert.That(first.BiomeId, Is.EqualTo(second.BiomeId));
        }

        [Test]
        public void ModifierCollectionsAreImmutableToConsumers()
        {
            BiomeProfile profile = BiomeProfile.CreateStandard("wetland", WorldBiomeType.Wetland, WorldClimate.Humid, 1d, 1d, 1.4d);

            Assert.That(((System.Collections.Generic.ICollection<BiomeResourceRule>)profile.ResourceRules).IsReadOnly, Is.True);
            Assert.That(((System.Collections.Generic.ICollection<BiomeClimateRule>)profile.ClimateRules).IsReadOnly, Is.True);
            Assert.That(profile.Modifiers.WaterMultiplier, Is.EqualTo(1.4d));
        }

        [Test]
        public void WorldDefinitionBiomeCanBeRegisteredInBiomeRegistry()
        {
            WorldDefinition world = new WorldDefinition("world-1", "World", new WorldSeed("seed"));
            BiomeDefinition definition = new BiomeDefinition("prairie", WorldBiomeType.Prairie, WorldClimate.Temperate);
            world.RegisterBiome(definition);
            BiomeRegistry registry = new BiomeRegistry();

            BiomeValidationResult result = registry.RegisterBiome(BiomeProfile.FromDefinition(world.Biomes["prairie"]));

            Assert.That(result.IsValid, Is.True);
            Assert.That(registry.QueryBiome("prairie").Climate, Is.EqualTo(WorldClimate.Temperate));
        }

        [Test]
        public void DiagnosticsRecordsValidAndInvalidDefinitions()
        {
            BiomeDiagnostics diagnostics = new BiomeDiagnostics();
            BiomeRegistry registry = new BiomeRegistry(diagnostics);

            registry.RegisterBiome(BiomeProfile.CreateStandard("forest", WorldBiomeType.Forest, WorldClimate.Humid, 1d, 1d, 1d));
            registry.RegisterBiome(new BiomeProfile("broken", WorldBiomeType.Urban, WorldClimate.Variable, BiomeModifierSet.Identity, new BiomeResourceRule[0], new BiomeClimateRule[0]));

            Assert.That(diagnostics.RegisteredCount, Is.EqualTo(1));
            Assert.That(diagnostics.RejectedCount, Is.EqualTo(1));
            Assert.That(diagnostics.Messages.Count, Is.EqualTo(2));
        }
    }
}
