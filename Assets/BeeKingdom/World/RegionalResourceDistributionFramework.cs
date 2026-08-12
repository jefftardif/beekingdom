using System;
using System.Collections.Generic;
using System.Linq;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Time;
using BeeKingdom.Economy;

namespace BeeKingdom.World
{
    public enum RegionalResourceCategory
    {
        Floral,
        Water,
        Generic
    }

    public interface IRegionalResourceDistributor
    {
        RegionalResourceDistributionSnapshot BuildDistribution(RegionDefinition region, BiomeProfile biome, RegionalWeatherSnapshot weatherSnapshot);
    }

    public interface IRegionalResourceDistributionSnapshotProvider
    {
        RegionalResourceDistributionSnapshot QueryDistributionSnapshot(string regionId);
    }

    public interface IRegionalResourceDiagnostics
    {
        IReadOnlyList<string> Messages { get; }
    }

    public sealed class RegionalResourceDistributor : IRegionalResourceDistributor
    {
        private readonly WorldSeed seed;
        private readonly RegionalResourceDiagnostics diagnostics;

        public RegionalResourceDistributor(WorldSeed seed, RegionalResourceDiagnostics diagnostics = null)
        {
            this.seed = seed;
            this.diagnostics = diagnostics ?? new RegionalResourceDiagnostics();
        }

        public RegionalResourceDistributionSnapshot BuildDistribution(RegionDefinition region, BiomeProfile biome, RegionalWeatherSnapshot weatherSnapshot)
        {
            if (region == null) throw new ArgumentNullException(nameof(region));
            if (biome == null) throw new ArgumentNullException(nameof(biome));
            if (weatherSnapshot == null) throw new ArgumentNullException(nameof(weatherSnapshot));

            List<RegionalResourceNodePlan> plans = new List<RegionalResourceNodePlan>();
            foreach (BiomeResourceRule biomeRule in biome.ResourceRules.OrderBy(rule => rule.ResourceId, StringComparer.Ordinal))
            {
                if (!TryCreateRule(biomeRule, weatherSnapshot, out RegionalResourceRule rule))
                {
                    diagnostics.RecordRejected(region.RegionId, biomeRule.ResourceId);
                    continue;
                }

                int nodeCount = CalculateNodeCount(rule);
                for (int i = 0; i < nodeCount; i++)
                {
                    string nodeId = BuildNodeId(region.RegionId, rule.ResourceType, i);
                    double capacity = Math.Max(0d, rule.BaseCapacity * rule.BiomeMultiplier * rule.WeatherMultiplier * rule.SeasonMultiplier);
                    double initial = capacity * 0.5d;
                    double regeneration = rule.RegenerationMultiplier * rule.WeatherMultiplier * rule.SeasonMultiplier;
                    HexCoordinates coordinates = CreateCoordinates(region.RegionId, nodeId, region.ChunkSize);
                    plans.Add(new RegionalResourceNodePlan(nodeId, region.RegionId, coordinates, rule.ResourceType, rule.Category, capacity, initial, regeneration, rule.Priority));
                    diagnostics.RecordNodePlanned(region.RegionId, nodeId);
                }
            }

            List<RegionalResourceNodePlan> ordered = plans
                .OrderBy(plan => plan.RegionId, StringComparer.Ordinal)
                .ThenBy(plan => plan.NodeId, StringComparer.Ordinal)
                .ToList();

            diagnostics.RecordBuilt(region.RegionId, ordered.Count);
            return new RegionalResourceDistributionSnapshot(region.WorldId, region.RegionId, weatherSnapshot.WeatherStep, weatherSnapshot.Season, weatherSnapshot.Weather, ordered);
        }

        private bool TryCreateRule(BiomeResourceRule biomeRule, RegionalWeatherSnapshot weatherSnapshot, out RegionalResourceRule rule)
        {
            rule = null;
            if (biomeRule == null || string.IsNullOrWhiteSpace(biomeRule.ResourceId))
            {
                return false;
            }

            if (!TryMapResource(biomeRule.ResourceId, out ResourceType resourceType))
            {
                return false;
            }

            double weatherMultiplier = WeatherMultiplier(resourceType, weatherSnapshot.Weather);
            double seasonMultiplier = SeasonMultiplier(resourceType, weatherSnapshot.Season);
            RegionalResourceCategory category = Category(resourceType);
            rule = new RegionalResourceRule(resourceType, category, biomeRule.Weight, 100d, biomeRule.Weight, weatherMultiplier, seasonMultiplier, biomeRule.RegenerationMultiplier, Priority(resourceType, biomeRule.Weight));
            return true;
        }

        private int CalculateNodeCount(RegionalResourceRule rule)
        {
            return Math.Max(1, (int)Math.Ceiling(rule.Weight));
        }

        private string BuildNodeId(string regionId, ResourceType resourceType, int index)
        {
            return regionId + "-" + resourceType.ToString().ToLowerInvariant() + "-" + index.ToString("D2");
        }

        private HexCoordinates CreateCoordinates(string regionId, string nodeId, int chunkSize)
        {
            int hash = StableHash(seed.Hash + ":" + regionId + ":" + nodeId);
            int size = Math.Max(1, chunkSize);
            int q = Math.Abs(hash % size);
            int r = Math.Abs((hash / size) % size);
            return new HexCoordinates(q, r);
        }

        private static bool TryMapResource(string resourceId, out ResourceType resourceType)
        {
            switch (resourceId.Trim().ToLowerInvariant())
            {
                case "nectar":
                    resourceType = ResourceType.Nectar;
                    return true;
                case "pollen":
                    resourceType = ResourceType.Pollen;
                    return true;
                case "water":
                    resourceType = ResourceType.Water;
                    return true;
                case "wax":
                    resourceType = ResourceType.Wax;
                    return true;
                case "honey":
                    resourceType = ResourceType.Honey;
                    return true;
                case "royaljelly":
                case "royal-jelly":
                    resourceType = ResourceType.RoyalJelly;
                    return true;
                case "propolis":
                    resourceType = ResourceType.Propolis;
                    return true;
                default:
                    resourceType = default;
                    return false;
            }
        }

        private static double WeatherMultiplier(ResourceType resourceType, WorldWeather weather)
        {
            if (resourceType == ResourceType.Water && (weather == WorldWeather.Rain || weather == WorldWeather.Storm)) return 1.5d;
            if ((resourceType == ResourceType.Nectar || resourceType == ResourceType.Pollen) && weather == WorldWeather.Storm) return 0.6d;
            if (weather == WorldWeather.Wind) return 0.85d;
            return 1d;
        }

        private static double SeasonMultiplier(ResourceType resourceType, SimulationSeason season)
        {
            if (season == SimulationSeason.Spring && (resourceType == ResourceType.Nectar || resourceType == ResourceType.Pollen)) return 1.25d;
            if (season == SimulationSeason.Winter && resourceType != ResourceType.Water) return 0.45d;
            return 1d;
        }

        private static RegionalResourceCategory Category(ResourceType resourceType)
        {
            if (resourceType == ResourceType.Nectar || resourceType == ResourceType.Pollen) return RegionalResourceCategory.Floral;
            if (resourceType == ResourceType.Water) return RegionalResourceCategory.Water;
            return RegionalResourceCategory.Generic;
        }

        private static int Priority(ResourceType resourceType, double weight)
        {
            int basePriority = resourceType == ResourceType.Water ? 20 : resourceType == ResourceType.Nectar || resourceType == ResourceType.Pollen ? 10 : 5;
            return basePriority + (int)Math.Round(weight * 10d);
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                const int offset = (int)2166136261;
                const int prime = 16777619;
                int hash = offset;
                for (int i = 0; i < value.Length; i++)
                {
                    hash ^= value[i];
                    hash *= prime;
                }

                return hash;
            }
        }
    }

    public sealed class RegionalResourceProfile
    {
        private readonly List<RegionalResourceRule> rules;
        public string RegionId { get; }
        public IReadOnlyList<RegionalResourceRule> Rules => rules.AsReadOnly();

        public RegionalResourceProfile(string regionId, IReadOnlyList<RegionalResourceRule> rules)
        {
            RegionId = string.IsNullOrWhiteSpace(regionId) ? throw new ArgumentException("RegionId is required.") : regionId;
            this.rules = new List<RegionalResourceRule>(rules ?? Array.Empty<RegionalResourceRule>());
        }
    }

    public sealed class RegionalResourceRule
    {
        public ResourceType ResourceType { get; }
        public RegionalResourceCategory Category { get; }
        public double Weight { get; }
        public double BaseCapacity { get; }
        public double BiomeMultiplier { get; }
        public double WeatherMultiplier { get; }
        public double SeasonMultiplier { get; }
        public double RegenerationMultiplier { get; }
        public int Priority { get; }

        public RegionalResourceRule(ResourceType resourceType, RegionalResourceCategory category, double weight, double baseCapacity, double biomeMultiplier, double weatherMultiplier, double seasonMultiplier, double regenerationMultiplier, int priority)
        {
            ResourceType = resourceType;
            Category = category;
            Weight = weight < 0d ? 0d : weight;
            BaseCapacity = baseCapacity < 0d ? 0d : baseCapacity;
            BiomeMultiplier = biomeMultiplier < 0d ? 0d : biomeMultiplier;
            WeatherMultiplier = weatherMultiplier < 0d ? 0d : weatherMultiplier;
            SeasonMultiplier = seasonMultiplier < 0d ? 0d : seasonMultiplier;
            RegenerationMultiplier = regenerationMultiplier < 0d ? 0d : regenerationMultiplier;
            Priority = priority;
        }
    }

    public sealed class RegionalResourceNodePlan : IEquatable<RegionalResourceNodePlan>
    {
        public string NodeId { get; }
        public string RegionId { get; }
        public HexCoordinates Coordinates { get; }
        public ResourceType ResourceType { get; }
        public RegionalResourceCategory Category { get; }
        public double Capacity { get; }
        public double InitialAmount { get; }
        public double InitialRegenerationPerSecond { get; }
        public int Priority { get; }

        public RegionalResourceNodePlan(string nodeId, string regionId, HexCoordinates coordinates, ResourceType resourceType, RegionalResourceCategory category, double capacity, double initialAmount, double initialRegenerationPerSecond, int priority)
        {
            NodeId = string.IsNullOrWhiteSpace(nodeId) ? throw new ArgumentException("NodeId is required.") : nodeId;
            RegionId = string.IsNullOrWhiteSpace(regionId) ? throw new ArgumentException("RegionId is required.") : regionId;
            Coordinates = coordinates;
            ResourceType = resourceType;
            Category = category;
            Capacity = capacity < 0d ? 0d : capacity;
            InitialAmount = initialAmount < 0d ? 0d : Math.Min(initialAmount, Capacity);
            InitialRegenerationPerSecond = initialRegenerationPerSecond < 0d ? 0d : initialRegenerationPerSecond;
            Priority = priority;
        }

        public bool Equals(RegionalResourceNodePlan other)
        {
            return other != null &&
                   NodeId == other.NodeId &&
                   RegionId == other.RegionId &&
                   Coordinates.Equals(other.Coordinates) &&
                   ResourceType == other.ResourceType &&
                   Category == other.Category &&
                   Capacity.Equals(other.Capacity) &&
                   InitialAmount.Equals(other.InitialAmount) &&
                   InitialRegenerationPerSecond.Equals(other.InitialRegenerationPerSecond) &&
                   Priority == other.Priority;
        }

        public override bool Equals(object obj) { return Equals(obj as RegionalResourceNodePlan); }
        public override int GetHashCode() { return NodeId.GetHashCode(); }
    }

    public sealed class RegionalResourceDistributionSnapshot : IEquatable<RegionalResourceDistributionSnapshot>
    {
        private readonly List<RegionalResourceNodePlan> plans;
        public string WorldId { get; }
        public string RegionId { get; }
        public int WeatherStep { get; }
        public SimulationSeason Season { get; }
        public WorldWeather Weather { get; }
        public IReadOnlyList<RegionalResourceNodePlan> Plans => plans.AsReadOnly();

        public RegionalResourceDistributionSnapshot(string worldId, string regionId, int weatherStep, SimulationSeason season, WorldWeather weather, IReadOnlyList<RegionalResourceNodePlan> plans)
        {
            WorldId = string.IsNullOrWhiteSpace(worldId) ? throw new ArgumentException("WorldId is required.") : worldId;
            RegionId = string.IsNullOrWhiteSpace(regionId) ? throw new ArgumentException("RegionId is required.") : regionId;
            WeatherStep = weatherStep;
            Season = season;
            Weather = weather;
            this.plans = new List<RegionalResourceNodePlan>(plans ?? Array.Empty<RegionalResourceNodePlan>());
        }

        public bool Equals(RegionalResourceDistributionSnapshot other)
        {
            if (other == null || WorldId != other.WorldId || RegionId != other.RegionId || WeatherStep != other.WeatherStep || Season != other.Season || Weather != other.Weather || Plans.Count != other.Plans.Count)
            {
                return false;
            }

            for (int i = 0; i < Plans.Count; i++)
            {
                if (!Plans[i].Equals(other.Plans[i])) return false;
            }

            return true;
        }

        public override bool Equals(object obj) { return Equals(obj as RegionalResourceDistributionSnapshot); }
        public override int GetHashCode() { return RegionId.GetHashCode() ^ WeatherStep; }
    }

    public sealed class RegionalResourceDistributionRegistry : IRegionalResourceDistributionSnapshotProvider
    {
        private readonly Dictionary<string, RegionalResourceDistributionSnapshot> snapshots = new Dictionary<string, RegionalResourceDistributionSnapshot>();
        public void Apply(RegionalResourceDistributionSnapshot snapshot) { snapshots[snapshot.RegionId] = snapshot; }
        public RegionalResourceDistributionSnapshot QueryDistributionSnapshot(string regionId) { return snapshots.TryGetValue(regionId, out RegionalResourceDistributionSnapshot snapshot) ? snapshot : null; }
    }

    public sealed class RegionalResourceDiagnostics : IRegionalResourceDiagnostics
    {
        private readonly List<string> messages = new List<string>();
        public int BuiltCount { get; private set; }
        public int PlannedNodeCount { get; private set; }
        public int RejectedRuleCount { get; private set; }
        public int CorrectedCapacityCount { get; private set; }
        public IReadOnlyList<string> Messages => messages.AsReadOnly();

        public void RecordBuilt(string regionId, int nodes)
        {
            BuiltCount++;
            messages.Add("Built:" + regionId + ":" + nodes);
        }

        public void RecordNodePlanned(string regionId, string nodeId)
        {
            PlannedNodeCount++;
            messages.Add("Planned:" + regionId + ":" + nodeId);
        }

        public void RecordRejected(string regionId, string resourceId)
        {
            RejectedRuleCount++;
            messages.Add("Rejected:" + regionId + ":" + resourceId);
        }

        public void RecordCapacityCorrected(string regionId, string nodeId)
        {
            CorrectedCapacityCount++;
            messages.Add("CorrectedCapacity:" + regionId + ":" + nodeId);
        }
    }

    public readonly struct RegionalResourceDistributionBuilt : IGameplayEvent
    {
        public string WorldId { get; }
        public string RegionId { get; }
        public int NodeCount { get; }
        public RegionalResourceDistributionBuilt(string worldId, string regionId, int nodeCount) { WorldId = worldId; RegionId = regionId; NodeCount = nodeCount; }
    }

    public readonly struct RegionalResourceNodePlanned : IGameplayEvent
    {
        public string RegionId { get; }
        public string NodeId { get; }
        public ResourceType ResourceType { get; }
        public RegionalResourceNodePlanned(string regionId, string nodeId, ResourceType resourceType) { RegionId = regionId; NodeId = nodeId; ResourceType = resourceType; }
    }

    public readonly struct RegionalResourceRuleRejected : IGameplayEvent
    {
        public string RegionId { get; }
        public string ResourceId { get; }
        public RegionalResourceRuleRejected(string regionId, string resourceId) { RegionId = regionId; ResourceId = resourceId; }
    }
}
