using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.World
{
    public interface IBiomeRegistry
    {
        IReadOnlyList<BiomeProfile> QueryBiomes();
        BiomeProfile QueryBiome(string biomeId);
        bool TryGetBiome(string biomeId, out BiomeProfile profile);
    }

    public interface IBiomeResolver
    {
        BiomeProfile Resolve(WorldBiomeType biomeType);
        BiomeProfile Resolve(RegionDefinition region);
        BiomeProfile Resolve(RegionSnapshot snapshot);
    }

    public interface IBiomeDiagnostics
    {
        IReadOnlyList<string> Messages { get; }
    }

    public sealed class BiomeRegistry : IBiomeRegistry
    {
        private readonly Dictionary<string, BiomeProfile> byId = new Dictionary<string, BiomeProfile>();
        private readonly Dictionary<WorldBiomeType, string> byType = new Dictionary<WorldBiomeType, string>();
        private readonly BiomeDiagnostics diagnostics;

        public BiomeRegistry(BiomeDiagnostics diagnostics = null)
        {
            this.diagnostics = diagnostics ?? new BiomeDiagnostics();
        }

        public BiomeValidationResult RegisterBiome(BiomeProfile profile)
        {
            BiomeValidationResult result = Validate(profile);
            if (!result.IsValid)
            {
                diagnostics.RecordRejected(profile == null ? string.Empty : profile.BiomeId, result);
                return result;
            }

            byId.Add(profile.BiomeId, profile);
            byType[profile.BiomeType] = profile.BiomeId;
            diagnostics.RecordRegistered(profile.BiomeId);
            return result;
        }

        public IReadOnlyList<BiomeProfile> QueryBiomes()
        {
            return byId.Values.OrderBy(profile => profile.BiomeId, StringComparer.Ordinal).ToList();
        }

        public BiomeProfile QueryBiome(string biomeId)
        {
            if (!TryGetBiome(biomeId, out BiomeProfile profile))
            {
                throw new KeyNotFoundException(biomeId);
            }

            return profile;
        }

        public bool TryGetBiome(string biomeId, out BiomeProfile profile)
        {
            return byId.TryGetValue(biomeId, out profile);
        }

        public bool TryGetBiome(WorldBiomeType biomeType, out BiomeProfile profile)
        {
            profile = null;
            return byType.TryGetValue(biomeType, out string biomeId) && byId.TryGetValue(biomeId, out profile);
        }

        public BiomeValidationResult Validate(BiomeProfile profile)
        {
            List<string> errors = new List<string>();
            List<string> warnings = new List<string>();
            if (profile == null)
            {
                errors.Add("Biome profile is required.");
                return new BiomeValidationResult(string.Empty, errors, warnings);
            }

            if (string.IsNullOrWhiteSpace(profile.BiomeId))
            {
                errors.Add("BiomeId is required.");
            }

            if (!string.IsNullOrWhiteSpace(profile.BiomeId) && byId.ContainsKey(profile.BiomeId))
            {
                errors.Add("BiomeId must be unique.");
            }

            if (profile.ResourceRules.Count == 0)
            {
                errors.Add("At least one resource rule is required.");
            }

            if (profile.ClimateRules.Count == 0)
            {
                errors.Add("At least one climate rule is required.");
            }

            if (byType.TryGetValue(profile.BiomeType, out string existingId) && existingId != profile.BiomeId)
            {
                warnings.Add("Biome type already has a registered profile; resolver will use the latest registered type mapping.");
            }

            return new BiomeValidationResult(profile.BiomeId, errors, warnings);
        }

        public static BiomeRegistry CreateStandardRegistry(BiomeDiagnostics diagnostics = null)
        {
            BiomeRegistry registry = new BiomeRegistry(diagnostics);
            registry.RegisterBiome(BiomeProfile.CreateStandard("prairie", WorldBiomeType.Prairie, WorldClimate.Temperate, 1.1d, 1d, 0.8d));
            registry.RegisterBiome(BiomeProfile.CreateStandard("forest", WorldBiomeType.Forest, WorldClimate.Humid, 0.95d, 1.15d, 1.1d));
            registry.RegisterBiome(BiomeProfile.CreateStandard("mountain", WorldBiomeType.Mountain, WorldClimate.Cold, 0.65d, 1.35d, 0.55d));
            registry.RegisterBiome(BiomeProfile.CreateStandard("river", WorldBiomeType.River, WorldClimate.Humid, 1.15d, 1d, 1.35d));
            registry.RegisterBiome(BiomeProfile.CreateStandard("marsh", WorldBiomeType.Marsh, WorldClimate.Variable, 0.85d, 1.25d, 1.4d));
            registry.RegisterBiome(BiomeProfile.CreateStandard("flower-fields", WorldBiomeType.FlowerFields, WorldClimate.Temperate, 1.6d, 0.8d, 0.9d));
            registry.RegisterBiome(BiomeProfile.CreateStandard("wetland", WorldBiomeType.Wetland, WorldClimate.Humid, 1.05d, 1.15d, 1.5d));
            registry.RegisterBiome(BiomeProfile.CreateStandard("meadow", WorldBiomeType.Meadow, WorldClimate.Temperate, 1.25d, 0.9d, 0.85d));
            registry.RegisterBiome(BiomeProfile.CreateStandard("orchard", WorldBiomeType.Orchard, WorldClimate.Temperate, 1.35d, 0.95d, 0.75d));
            registry.RegisterBiome(BiomeProfile.CreateStandard("farmland", WorldBiomeType.Farmland, WorldClimate.Dry, 1.05d, 1.05d, 0.55d));
            registry.RegisterBiome(BiomeProfile.CreateStandard("urban", WorldBiomeType.Urban, WorldClimate.Variable, 0.55d, 1.4d, 0.35d));
            registry.RegisterBiome(BiomeProfile.CreateStandard("special-event-area", WorldBiomeType.SpecialEventArea, WorldClimate.Variable, 1.2d, 1.2d, 1d));
            return registry;
        }
    }

    public sealed class BiomeResolver : IBiomeResolver
    {
        private readonly BiomeRegistry registry;
        private readonly BiomeDiagnostics diagnostics;

        public BiomeResolver(BiomeRegistry registry, BiomeDiagnostics diagnostics = null)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
            this.diagnostics = diagnostics ?? new BiomeDiagnostics();
        }

        public BiomeProfile Resolve(WorldBiomeType biomeType)
        {
            if (!registry.TryGetBiome(biomeType, out BiomeProfile profile))
            {
                throw new KeyNotFoundException("No biome profile registered for " + biomeType + ".");
            }

            diagnostics.RecordResolved(profile.BiomeId);
            return profile;
        }

        public BiomeProfile Resolve(RegionDefinition region)
        {
            if (region == null) throw new ArgumentNullException(nameof(region));
            return Resolve(region.Biome);
        }

        public BiomeProfile Resolve(RegionSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            return Resolve(snapshot.Biome);
        }

        public BiomeProfile ResolveDeterministic(WorldSeed seed, IReadOnlyList<BiomeProfile> candidates)
        {
            if (candidates == null || candidates.Count == 0)
            {
                throw new ArgumentException("At least one biome candidate is required.");
            }

            List<BiomeProfile> ordered = candidates.OrderBy(candidate => candidate.BiomeId, StringComparer.Ordinal).ToList();
            int index = Math.Abs(seed.Hash) % ordered.Count;
            BiomeProfile profile = ordered[index];
            diagnostics.RecordResolved(profile.BiomeId);
            return profile;
        }
    }

    public sealed class BiomeProfile
    {
        private readonly List<BiomeResourceRule> resourceRules;
        private readonly List<BiomeClimateRule> climateRules;

        public string BiomeId { get; }
        public WorldBiomeType BiomeType { get; }
        public WorldClimate Climate { get; }
        public BiomeModifierSet Modifiers { get; }
        public IReadOnlyList<BiomeResourceRule> ResourceRules => resourceRules.AsReadOnly();
        public IReadOnlyList<BiomeClimateRule> ClimateRules => climateRules.AsReadOnly();

        public BiomeProfile(string biomeId, WorldBiomeType biomeType, WorldClimate climate, BiomeModifierSet modifiers, IReadOnlyList<BiomeResourceRule> resourceRules, IReadOnlyList<BiomeClimateRule> climateRules)
        {
            BiomeId = biomeId;
            BiomeType = biomeType;
            Climate = climate;
            Modifiers = modifiers ?? BiomeModifierSet.Identity;
            this.resourceRules = new List<BiomeResourceRule>(resourceRules ?? Array.Empty<BiomeResourceRule>());
            this.climateRules = new List<BiomeClimateRule>(climateRules ?? Array.Empty<BiomeClimateRule>());
        }

        public static BiomeProfile FromDefinition(BiomeDefinition definition, BiomeModifierSet modifiers = null)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            BiomeModifierSet resolvedModifiers = modifiers ?? BiomeModifierSet.Identity;
            return new BiomeProfile(
                definition.BiomeId,
                definition.Type,
                definition.Climate,
                resolvedModifiers,
                new[] { new BiomeResourceRule("nectar", resolvedModifiers.NectarMultiplier, 1d), new BiomeResourceRule("pollen", resolvedModifiers.PollenMultiplier, 1d) },
                new[] { BiomeClimateRule.FromClimate(definition.Climate) });
        }

        public static BiomeProfile FromWorldBiome(string biomeId, WorldBiome biome)
        {
            if (biome == null) throw new ArgumentNullException(nameof(biome));
            List<BiomeResourceRule> resourceRules = biome.ResourceWeights.Select(pair => new BiomeResourceRule(pair.Key, pair.Value, 1d)).ToList();
            return new BiomeProfile(
                biomeId,
                biome.BiomeType,
                biome.Climate,
                new BiomeModifierSet(biome.RichnessMultiplier, biome.RichnessMultiplier * 0.8d, 1d, 1d, 1d, biome.DifficultyMultiplier, biome.RichnessMultiplier),
                resourceRules,
                new[] { BiomeClimateRule.FromClimate(biome.Climate) });
        }

        public static BiomeProfile CreateStandard(string biomeId, WorldBiomeType type, WorldClimate climate, double nectar, double travelCost, double water)
        {
            return new BiomeProfile(
                biomeId,
                type,
                climate,
                new BiomeModifierSet(nectar, nectar * 0.8d, water, 1d, 1d, travelCost, nectar),
                new[] { new BiomeResourceRule("nectar", nectar, 1d), new BiomeResourceRule("pollen", nectar * 0.8d, 1d), new BiomeResourceRule("water", water, 1d) },
                new[] { BiomeClimateRule.FromClimate(climate) });
        }
    }

    public sealed class BiomeModifierSet
    {
        public static readonly BiomeModifierSet Identity = new BiomeModifierSet(1d, 1d, 1d, 1d, 1d, 1d, 1d);
        public double NectarMultiplier { get; }
        public double PollenMultiplier { get; }
        public double WaterMultiplier { get; }
        public double TemperatureMultiplier { get; }
        public double HumidityMultiplier { get; }
        public double TravelCostMultiplier { get; }
        public double ForagingEfficiencyMultiplier { get; }

        public BiomeModifierSet(double nectarMultiplier, double pollenMultiplier, double waterMultiplier, double temperatureMultiplier, double humidityMultiplier, double travelCostMultiplier, double foragingEfficiencyMultiplier)
        {
            NectarMultiplier = Clamp(nectarMultiplier);
            PollenMultiplier = Clamp(pollenMultiplier);
            WaterMultiplier = Clamp(waterMultiplier);
            TemperatureMultiplier = Clamp(temperatureMultiplier);
            HumidityMultiplier = Clamp(humidityMultiplier);
            TravelCostMultiplier = Clamp(travelCostMultiplier);
            ForagingEfficiencyMultiplier = Clamp(foragingEfficiencyMultiplier);
        }

        private static double Clamp(double value)
        {
            return value < 0d ? 0d : value;
        }
    }

    public sealed class BiomeResourceRule
    {
        public string ResourceId { get; }
        public double Weight { get; }
        public double RegenerationMultiplier { get; }

        public BiomeResourceRule(string resourceId, double weight, double regenerationMultiplier)
        {
            if (string.IsNullOrWhiteSpace(resourceId)) throw new ArgumentException("ResourceId is required.");
            ResourceId = resourceId;
            Weight = weight < 0d ? 0d : weight;
            RegenerationMultiplier = regenerationMultiplier < 0d ? 0d : regenerationMultiplier;
        }
    }

    public sealed class BiomeClimateRule
    {
        public WorldClimate Climate { get; }
        public double MinimumTemperature { get; }
        public double MaximumTemperature { get; }
        public double Humidity { get; }
        public IReadOnlyList<WorldWeather> AllowedWeather { get; }

        public BiomeClimateRule(WorldClimate climate, double minimumTemperature, double maximumTemperature, double humidity, IReadOnlyList<WorldWeather> allowedWeather)
        {
            Climate = climate;
            MinimumTemperature = minimumTemperature;
            MaximumTemperature = maximumTemperature;
            Humidity = humidity < 0d ? 0d : humidity;
            AllowedWeather = new List<WorldWeather>(allowedWeather ?? Array.Empty<WorldWeather>());
        }

        public static BiomeClimateRule FromClimate(WorldClimate climate)
        {
            switch (climate)
            {
                case WorldClimate.Humid:
                    return new BiomeClimateRule(climate, 8d, 32d, 0.75d, new[] { WorldWeather.Clear, WorldWeather.Cloudy, WorldWeather.Rain, WorldWeather.Storm });
                case WorldClimate.Dry:
                    return new BiomeClimateRule(climate, 12d, 38d, 0.3d, new[] { WorldWeather.Clear, WorldWeather.Cloudy, WorldWeather.Wind });
                case WorldClimate.Cold:
                    return new BiomeClimateRule(climate, -10d, 18d, 0.45d, new[] { WorldWeather.Clear, WorldWeather.Cloudy, WorldWeather.Wind, WorldWeather.Storm });
                case WorldClimate.Variable:
                    return new BiomeClimateRule(climate, 0d, 35d, 0.55d, new[] { WorldWeather.Clear, WorldWeather.Cloudy, WorldWeather.Rain, WorldWeather.Wind, WorldWeather.Storm });
                default:
                    return new BiomeClimateRule(climate, 5d, 30d, 0.5d, new[] { WorldWeather.Clear, WorldWeather.Cloudy, WorldWeather.Rain });
            }
        }
    }

    public sealed class BiomeValidationResult
    {
        public string BiomeId { get; }
        public IReadOnlyList<string> Errors { get; }
        public IReadOnlyList<string> Warnings { get; }
        public bool IsValid => Errors.Count == 0;

        public BiomeValidationResult(string biomeId, IReadOnlyList<string> errors, IReadOnlyList<string> warnings)
        {
            BiomeId = biomeId ?? string.Empty;
            Errors = new List<string>(errors ?? Array.Empty<string>());
            Warnings = new List<string>(warnings ?? Array.Empty<string>());
        }
    }

    public sealed class BiomeDiagnostics : IBiomeDiagnostics
    {
        private readonly List<string> messages = new List<string>();
        public int RegisteredCount { get; private set; }
        public int ResolvedCount { get; private set; }
        public int RejectedCount { get; private set; }
        public IReadOnlyList<string> Messages => messages.AsReadOnly();

        public void RecordRegistered(string biomeId)
        {
            RegisteredCount++;
            messages.Add("Registered:" + biomeId);
        }

        public void RecordResolved(string biomeId)
        {
            ResolvedCount++;
            messages.Add("Resolved:" + biomeId);
        }

        public void RecordRejected(string biomeId, BiomeValidationResult result)
        {
            RejectedCount++;
            messages.Add("Rejected:" + biomeId + ":" + string.Join("|", result.Errors));
        }
    }
}
