using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BeeKingdom.Core.Events;

namespace BeeKingdom.World
{
    public interface IWorldSnapshotPackageBuilder
    {
        WorldSnapshotPackage Build(WorldSnapshot worldSnapshot, IEnumerable<RegionSnapshotEntry> regions, IEnumerable<BiomeSnapshotReference> biomes, IEnumerable<RegionalWeatherSnapshotEntry> weather, IEnumerable<RegionalResourceDistributionEntry> resources);
    }

    public interface IWorldSnapshotCompatibilityValidator
    {
        WorldSnapshotCompatibilityResult Validate(WorldSnapshotPackage package);
    }

    public interface IWorldSnapshotChecksumBuilder
    {
        string Calculate(WorldSnapshotPackage package);
    }

    public sealed class WorldSnapshotPackageBuilder : IWorldSnapshotPackageBuilder
    {
        private readonly IWorldSnapshotChecksumBuilder checksumBuilder;

        public WorldSnapshotPackageBuilder(IWorldSnapshotChecksumBuilder checksumBuilder = null)
        {
            this.checksumBuilder = checksumBuilder ?? new WorldSnapshotChecksumBuilder();
        }

        public WorldSnapshotPackage Build(WorldSnapshot worldSnapshot, IEnumerable<RegionSnapshotEntry> regions, IEnumerable<BiomeSnapshotReference> biomes, IEnumerable<RegionalWeatherSnapshotEntry> weather, IEnumerable<RegionalResourceDistributionEntry> resources)
        {
            if (worldSnapshot == null) throw new ArgumentNullException(nameof(worldSnapshot));

            List<RegionSnapshotEntry> orderedRegions = (regions ?? Array.Empty<RegionSnapshotEntry>()).OrderBy(entry => entry.RegionId, StringComparer.Ordinal).ToList();
            List<BiomeSnapshotReference> orderedBiomes = (biomes ?? Array.Empty<BiomeSnapshotReference>()).OrderBy(entry => entry.RegionId, StringComparer.Ordinal).ToList();
            List<RegionalWeatherSnapshotEntry> orderedWeather = (weather ?? Array.Empty<RegionalWeatherSnapshotEntry>()).OrderBy(entry => entry.RegionId, StringComparer.Ordinal).ToList();
            List<RegionalResourceDistributionEntry> orderedResources = (resources ?? Array.Empty<RegionalResourceDistributionEntry>()).OrderBy(entry => entry.RegionId, StringComparer.Ordinal).ThenBy(entry => entry.NodeId, StringComparer.Ordinal).ToList();
            WorldSnapshotManifest manifest = new WorldSnapshotManifest(1, worldSnapshot.WorldId, orderedRegions.Count, orderedBiomes.Count, orderedWeather.Count, orderedResources.Count);
            WorldSnapshotPackage package = new WorldSnapshotPackage(manifest, worldSnapshot, orderedRegions, orderedBiomes, orderedWeather, orderedResources, string.Empty);
            return package.WithChecksum(checksumBuilder.Calculate(package));
        }
    }

    public sealed class WorldSnapshotPackage
    {
        public WorldSnapshotManifest Manifest { get; }
        public WorldSnapshot World { get; }
        public IReadOnlyList<RegionSnapshotEntry> Regions { get; }
        public IReadOnlyList<BiomeSnapshotReference> Biomes { get; }
        public IReadOnlyList<RegionalWeatherSnapshotEntry> RegionalWeather { get; }
        public IReadOnlyList<RegionalResourceDistributionEntry> ResourceDistributions { get; }
        public string Checksum { get; }

        public WorldSnapshotPackage(WorldSnapshotManifest manifest, WorldSnapshot world, IReadOnlyList<RegionSnapshotEntry> regions, IReadOnlyList<BiomeSnapshotReference> biomes, IReadOnlyList<RegionalWeatherSnapshotEntry> regionalWeather, IReadOnlyList<RegionalResourceDistributionEntry> resourceDistributions, string checksum)
        {
            Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
            World = world ?? throw new ArgumentNullException(nameof(world));
            Regions = new List<RegionSnapshotEntry>(regions ?? Array.Empty<RegionSnapshotEntry>()).AsReadOnly();
            Biomes = new List<BiomeSnapshotReference>(biomes ?? Array.Empty<BiomeSnapshotReference>()).AsReadOnly();
            RegionalWeather = new List<RegionalWeatherSnapshotEntry>(regionalWeather ?? Array.Empty<RegionalWeatherSnapshotEntry>()).AsReadOnly();
            ResourceDistributions = new List<RegionalResourceDistributionEntry>(resourceDistributions ?? Array.Empty<RegionalResourceDistributionEntry>()).AsReadOnly();
            Checksum = checksum ?? string.Empty;
        }

        public WorldSnapshotPackage WithChecksum(string checksum)
        {
            return new WorldSnapshotPackage(Manifest, World, Regions, Biomes, RegionalWeather, ResourceDistributions, checksum);
        }

        public string ToStablePayload()
        {
            StringBuilder builder = new StringBuilder(1024);
            builder.Append("manifest|").Append(Manifest.SchemaVersion).Append('|').Append(Manifest.WorldId).Append('|').Append(Manifest.RegionCount).Append('|').Append(Manifest.BiomeReferenceCount).Append('|').Append(Manifest.RegionalWeatherCount).Append('|').Append(Manifest.ResourceDistributionCount).Append('\n');
            builder.Append("world|").Append(World.WorldId).Append('|').Append(World.Seed.Value).Append('|').Append(World.CurrentSeason).Append('|').Append(World.CurrentWeather).Append('|').Append(World.ActiveColonies).Append('|').Append(World.ActiveEventCount).Append('\n');
            foreach (RegionSnapshotEntry entry in Regions) entry.AppendTo(builder);
            foreach (BiomeSnapshotReference entry in Biomes) entry.AppendTo(builder);
            foreach (RegionalWeatherSnapshotEntry entry in RegionalWeather) entry.AppendTo(builder);
            foreach (RegionalResourceDistributionEntry entry in ResourceDistributions) entry.AppendTo(builder);
            return builder.ToString();
        }
    }

    public sealed class WorldSnapshotManifest
    {
        public int SchemaVersion { get; }
        public string WorldId { get; }
        public int RegionCount { get; }
        public int BiomeReferenceCount { get; }
        public int RegionalWeatherCount { get; }
        public int ResourceDistributionCount { get; }

        public WorldSnapshotManifest(int schemaVersion, string worldId, int regionCount, int biomeReferenceCount, int regionalWeatherCount, int resourceDistributionCount)
        {
            SchemaVersion = schemaVersion;
            WorldId = string.IsNullOrWhiteSpace(worldId) ? throw new ArgumentException("WorldId is required.") : worldId;
            RegionCount = Math.Max(0, regionCount);
            BiomeReferenceCount = Math.Max(0, biomeReferenceCount);
            RegionalWeatherCount = Math.Max(0, regionalWeatherCount);
            ResourceDistributionCount = Math.Max(0, resourceDistributionCount);
        }
    }

    public sealed class RegionSnapshotEntry
    {
        public string RegionId { get; }
        public string WorldId { get; }
        public WorldBiomeType Biome { get; }
        public WorldWeather Weather { get; }
        public BeeKingdom.Core.Time.SimulationSeason Season { get; }
        public double Temperature { get; }
        public double Humidity { get; }
        public RegionSimulationState State { get; }

        public RegionSnapshotEntry(RegionSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            RegionId = snapshot.RegionId;
            WorldId = snapshot.WorldId;
            Biome = snapshot.Biome;
            Weather = snapshot.Weather;
            Season = snapshot.Season;
            Temperature = snapshot.Temperature;
            Humidity = snapshot.Humidity;
            State = snapshot.State;
        }

        public void AppendTo(StringBuilder builder)
        {
            builder.Append("region|").Append(RegionId).Append('|').Append(WorldId).Append('|').Append(Biome).Append('|').Append(Weather).Append('|').Append(Season).Append('|').Append(Temperature).Append('|').Append(Humidity).Append('|').Append(State).Append('\n');
        }
    }

    public sealed class BiomeSnapshotReference
    {
        public string RegionId { get; }
        public string BiomeId { get; }
        public WorldBiomeType BiomeType { get; }

        public BiomeSnapshotReference(string regionId, BiomeProfile biome)
        {
            if (biome == null) throw new ArgumentNullException(nameof(biome));
            RegionId = string.IsNullOrWhiteSpace(regionId) ? throw new ArgumentException("RegionId is required.") : regionId;
            BiomeId = biome.BiomeId;
            BiomeType = biome.BiomeType;
        }

        public void AppendTo(StringBuilder builder)
        {
            builder.Append("biome|").Append(RegionId).Append('|').Append(BiomeId).Append('|').Append(BiomeType).Append('\n');
        }
    }

    public sealed class RegionalWeatherSnapshotEntry
    {
        public string RegionId { get; }
        public WorldWeather Weather { get; }
        public int WeatherStep { get; }
        public double Temperature { get; }
        public double Humidity { get; }

        public RegionalWeatherSnapshotEntry(RegionalWeatherSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            RegionId = snapshot.RegionId;
            Weather = snapshot.Weather;
            WeatherStep = snapshot.WeatherStep;
            Temperature = snapshot.Temperature;
            Humidity = snapshot.Humidity;
        }

        public void AppendTo(StringBuilder builder)
        {
            builder.Append("weather|").Append(RegionId).Append('|').Append(Weather).Append('|').Append(WeatherStep).Append('|').Append(Temperature).Append('|').Append(Humidity).Append('\n');
        }
    }

    public sealed class RegionalResourceDistributionEntry
    {
        public string RegionId { get; }
        public string NodeId { get; }
        public BeeKingdom.Economy.ResourceType ResourceType { get; }
        public double Capacity { get; }
        public double InitialAmount { get; }

        public RegionalResourceDistributionEntry(RegionalResourceNodePlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            RegionId = plan.RegionId;
            NodeId = plan.NodeId;
            ResourceType = plan.ResourceType;
            Capacity = plan.Capacity;
            InitialAmount = plan.InitialAmount;
        }

        public void AppendTo(StringBuilder builder)
        {
            builder.Append("resource|").Append(RegionId).Append('|').Append(NodeId).Append('|').Append(ResourceType).Append('|').Append(Capacity).Append('|').Append(InitialAmount).Append('\n');
        }
    }

    public sealed class WorldSnapshotCompatibilityValidator : IWorldSnapshotCompatibilityValidator
    {
        public WorldSnapshotCompatibilityResult Validate(WorldSnapshotPackage package)
        {
            List<string> errors = new List<string>();
            if (package == null)
            {
                errors.Add("Package is required.");
                return new WorldSnapshotCompatibilityResult(errors);
            }

            if (package.Manifest.SchemaVersion <= 0) errors.Add("Schema version is invalid.");
            HashSet<string> regions = new HashSet<string>(package.Regions.Select(entry => entry.RegionId));
            foreach (RegionSnapshotEntry region in package.Regions)
            {
                if (!package.Biomes.Any(biome => biome.RegionId == region.RegionId)) errors.Add("Missing biome for region " + region.RegionId + ".");
                if (!package.RegionalWeather.Any(weather => weather.RegionId == region.RegionId)) errors.Add("Missing regional weather for region " + region.RegionId + ".");
            }

            foreach (RegionalResourceDistributionEntry resource in package.ResourceDistributions)
            {
                if (!regions.Contains(resource.RegionId)) errors.Add("Resource distribution references unknown region " + resource.RegionId + ".");
            }

            return new WorldSnapshotCompatibilityResult(errors);
        }
    }

    public sealed class WorldSnapshotCompatibilityResult
    {
        public IReadOnlyList<string> Errors { get; }
        public bool IsValid => Errors.Count == 0;
        public WorldSnapshotCompatibilityResult(IReadOnlyList<string> errors) { Errors = new List<string>(errors ?? Array.Empty<string>()).AsReadOnly(); }
    }

    public sealed class WorldSnapshotChecksumBuilder : IWorldSnapshotChecksumBuilder
    {
        public string Calculate(WorldSnapshotPackage package)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(package.ToStablePayload());
                byte[] hash = sha.ComputeHash(bytes);
                StringBuilder builder = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++) builder.Append(hash[i].ToString("x2"));
                return builder.ToString();
            }
        }
    }

    public sealed class WorldSnapshotCompatibilityDiagnostics
    {
        private readonly List<string> messages = new List<string>();
        public int PackageCount { get; private set; }
        public int FailureCount { get; private set; }
        public int ChecksumChangeCount { get; private set; }
        public IReadOnlyList<string> Messages => messages.AsReadOnly();
        public void RecordPackage(string worldId) { PackageCount++; messages.Add("Package:" + worldId); }
        public void RecordFailure(string reason) { FailureCount++; messages.Add("Failed:" + reason); }
        public void RecordChecksumChanged(string checksum) { ChecksumChangeCount++; messages.Add("ChecksumChanged:" + checksum); }
    }

    public readonly struct WorldSnapshotPackageCreated : IGameplayEvent
    {
        public string WorldId { get; }
        public string Checksum { get; }
        public WorldSnapshotPackageCreated(string worldId, string checksum) { WorldId = worldId; Checksum = checksum; }
    }

    public readonly struct WorldSnapshotCompatibilityFailed : IGameplayEvent
    {
        public string WorldId { get; }
        public string Reason { get; }
        public WorldSnapshotCompatibilityFailed(string worldId, string reason) { WorldId = worldId; Reason = reason; }
    }

    public readonly struct WorldSnapshotChecksumChanged : IGameplayEvent
    {
        public string WorldId { get; }
        public string Checksum { get; }
        public WorldSnapshotChecksumChanged(string worldId, string checksum) { WorldId = worldId; Checksum = checksum; }
    }
}
