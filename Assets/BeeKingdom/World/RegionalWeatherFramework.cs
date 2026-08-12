using System;
using System.Collections.Generic;
using System.Linq;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Time;

namespace BeeKingdom.World
{
    public interface IRegionalWeatherResolver
    {
        RegionalWeatherSnapshot Resolve(RegionDefinition region, BiomeProfile biome, SimulationSeason season, WorldWeather baseWeather, int weatherStep);
        RegionalWeatherSnapshot Resolve(RegionSnapshot snapshot, BiomeProfile biome, WorldWeather baseWeather, int weatherStep);
    }

    public interface IRegionalWeatherSnapshotProvider
    {
        RegionalWeatherSnapshot QueryWeatherSnapshot(string regionId);
    }

    public interface IRegionalWeatherDiagnostics
    {
        IReadOnlyList<string> Messages { get; }
    }

    public sealed class RegionalWeatherResolver : IRegionalWeatherResolver
    {
        private readonly WorldSeed seed;
        private readonly RegionalWeatherDiagnostics diagnostics;
        private readonly string profileId;

        public RegionalWeatherResolver(WorldSeed seed, RegionalWeatherDiagnostics diagnostics = null, string profileId = "regional-weather")
        {
            this.seed = seed;
            this.diagnostics = diagnostics ?? new RegionalWeatherDiagnostics();
            this.profileId = string.IsNullOrWhiteSpace(profileId) ? "regional-weather" : profileId;
        }

        public RegionalWeatherSnapshot Resolve(RegionDefinition region, BiomeProfile biome, SimulationSeason season, WorldWeather baseWeather, int weatherStep)
        {
            if (region == null) throw new ArgumentNullException(nameof(region));
            if (biome == null)
            {
                diagnostics.RecordRejected(region.RegionId, "Biome could not be resolved.");
                throw new ArgumentNullException(nameof(biome));
            }

            BiomeClimateRule climateRule = SelectClimateRule(biome);
            WorldWeather resolvedWeather = ResolveWeather(region.RegionId, biome, climateRule, baseWeather, weatherStep);
            double temperature = Clamp(region.Temperature * biome.Modifiers.TemperatureMultiplier, climateRule.MinimumTemperature, climateRule.MaximumTemperature);
            double humidity = Math.Max(0d, ((region.Humidity + climateRule.Humidity) * 0.5d) * biome.Modifiers.HumidityMultiplier);
            diagnostics.RecordResolved(region.RegionId, resolvedWeather);
            return new RegionalWeatherSnapshot(region.WorldId, region.RegionId, biome.BiomeType, season, resolvedWeather, temperature, humidity, weatherStep, profileId);
        }

        public RegionalWeatherSnapshot Resolve(RegionSnapshot snapshot, BiomeProfile biome, WorldWeather baseWeather, int weatherStep)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (biome == null)
            {
                diagnostics.RecordRejected(snapshot.RegionId, "Biome could not be resolved.");
                throw new ArgumentNullException(nameof(biome));
            }

            BiomeClimateRule climateRule = SelectClimateRule(biome);
            WorldWeather resolvedWeather = ResolveWeather(snapshot.RegionId, biome, climateRule, baseWeather, weatherStep);
            double temperature = Clamp(snapshot.Temperature * biome.Modifiers.TemperatureMultiplier, climateRule.MinimumTemperature, climateRule.MaximumTemperature);
            double humidity = Math.Max(0d, ((snapshot.Humidity + climateRule.Humidity) * 0.5d) * biome.Modifiers.HumidityMultiplier);
            diagnostics.RecordResolved(snapshot.RegionId, resolvedWeather);
            return new RegionalWeatherSnapshot(snapshot.WorldId, snapshot.RegionId, biome.BiomeType, snapshot.Season, resolvedWeather, temperature, humidity, weatherStep, profileId);
        }

        private WorldWeather ResolveWeather(string regionId, BiomeProfile biome, BiomeClimateRule climateRule, WorldWeather baseWeather, int weatherStep)
        {
            IReadOnlyList<WorldWeather> allowed = climateRule.AllowedWeather;
            if (allowed.Count == 0)
            {
                diagnostics.RecordRejected(regionId, "Biome climate rule has no allowed weather.");
                throw new InvalidOperationException("Biome climate rule has no allowed weather.");
            }

            if (allowed.Contains(baseWeather))
            {
                return baseWeather;
            }

            List<WorldWeather> ordered = allowed.OrderBy(weather => weather.ToString(), StringComparer.Ordinal).ToList();
            int index = StableIndex(regionId, biome.BiomeId, weatherStep, ordered.Count);
            WorldWeather corrected = ordered[index];
            diagnostics.RecordCorrection(regionId, baseWeather, corrected);
            return corrected;
        }

        private BiomeClimateRule SelectClimateRule(BiomeProfile biome)
        {
            BiomeClimateRule rule = biome.ClimateRules.FirstOrDefault(item => item.Climate == biome.Climate);
            return rule ?? biome.ClimateRules.FirstOrDefault();
        }

        private int StableIndex(string regionId, string biomeId, int weatherStep, int count)
        {
            unchecked
            {
                int hash = seed.Hash;
                hash = (hash * 397) ^ regionId.GetHashCode();
                hash = (hash * 397) ^ biomeId.GetHashCode();
                hash = (hash * 397) ^ weatherStep;
                return Math.Abs(hash) % count;
            }
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            if (value < minimum) return minimum;
            if (value > maximum) return maximum;
            return value;
        }
    }

    public sealed class RegionalWeatherProfile
    {
        public string ProfileId { get; }
        public IReadOnlyDictionary<WorldWeather, double> WeatherWeights { get; }

        public RegionalWeatherProfile(string profileId, IReadOnlyDictionary<WorldWeather, double> weatherWeights)
        {
            ProfileId = string.IsNullOrWhiteSpace(profileId) ? "regional-weather" : profileId;
            WeatherWeights = new Dictionary<WorldWeather, double>(weatherWeights ?? new Dictionary<WorldWeather, double>());
        }

        public static RegionalWeatherProfile FromBiome(BiomeProfile biome)
        {
            if (biome == null) throw new ArgumentNullException(nameof(biome));
            Dictionary<WorldWeather, double> weights = new Dictionary<WorldWeather, double>();
            foreach (BiomeClimateRule rule in biome.ClimateRules)
            {
                foreach (WorldWeather weather in rule.AllowedWeather)
                {
                    weights[weather] = weights.TryGetValue(weather, out double existing) ? existing + 1d : 1d;
                }
            }

            return new RegionalWeatherProfile(biome.BiomeId, weights);
        }
    }

    public sealed class RegionalWeatherState
    {
        public string RegionId { get; }
        public RegionalWeatherSnapshot Snapshot { get; private set; }
        public int LastWeatherStep { get; private set; }

        public RegionalWeatherState(string regionId)
        {
            RegionId = string.IsNullOrWhiteSpace(regionId) ? throw new ArgumentException("RegionId is required.") : regionId;
            LastWeatherStep = -1;
        }

        public bool Apply(RegionalWeatherSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            bool changed = Snapshot == null || Snapshot.Weather != snapshot.Weather;
            Snapshot = snapshot;
            LastWeatherStep = snapshot.WeatherStep;
            return changed;
        }
    }

    public sealed class RegionalWeatherSnapshot : IEquatable<RegionalWeatherSnapshot>
    {
        public string WorldId { get; }
        public string RegionId { get; }
        public WorldBiomeType BiomeType { get; }
        public SimulationSeason Season { get; }
        public WorldWeather Weather { get; }
        public double Temperature { get; }
        public double Humidity { get; }
        public int WeatherStep { get; }
        public string ProfileId { get; }

        public RegionalWeatherSnapshot(string worldId, string regionId, WorldBiomeType biomeType, SimulationSeason season, WorldWeather weather, double temperature, double humidity, int weatherStep, string profileId)
        {
            WorldId = string.IsNullOrWhiteSpace(worldId) ? throw new ArgumentException("WorldId is required.") : worldId;
            RegionId = string.IsNullOrWhiteSpace(regionId) ? throw new ArgumentException("RegionId is required.") : regionId;
            BiomeType = biomeType;
            Season = season;
            Weather = weather;
            Temperature = temperature;
            Humidity = humidity < 0d ? 0d : humidity;
            WeatherStep = weatherStep;
            ProfileId = string.IsNullOrWhiteSpace(profileId) ? "regional-weather" : profileId;
        }

        public bool Equals(RegionalWeatherSnapshot other)
        {
            return other != null &&
                   WorldId == other.WorldId &&
                   RegionId == other.RegionId &&
                   BiomeType == other.BiomeType &&
                   Season == other.Season &&
                   Weather == other.Weather &&
                   Temperature.Equals(other.Temperature) &&
                   Humidity.Equals(other.Humidity) &&
                   WeatherStep == other.WeatherStep &&
                   ProfileId == other.ProfileId;
        }

        public override bool Equals(object obj) { return Equals(obj as RegionalWeatherSnapshot); }
        public override int GetHashCode() { unchecked { return (((WorldId.GetHashCode() * 397) ^ RegionId.GetHashCode()) * 397) ^ WeatherStep; } }
    }

    public sealed class RegionalWeatherSnapshotRegistry : IRegionalWeatherSnapshotProvider
    {
        private readonly Dictionary<string, RegionalWeatherState> states = new Dictionary<string, RegionalWeatherState>();

        public bool Apply(RegionalWeatherSnapshot snapshot)
        {
            if (!states.TryGetValue(snapshot.RegionId, out RegionalWeatherState state))
            {
                state = new RegionalWeatherState(snapshot.RegionId);
                states.Add(snapshot.RegionId, state);
            }

            return state.Apply(snapshot);
        }

        public RegionalWeatherSnapshot QueryWeatherSnapshot(string regionId)
        {
            return states.TryGetValue(regionId, out RegionalWeatherState state) ? state.Snapshot : null;
        }
    }

    public sealed class RegionalWeatherDiagnostics : IRegionalWeatherDiagnostics
    {
        private readonly List<string> messages = new List<string>();
        public int ResolutionCount { get; private set; }
        public int RejectionCount { get; private set; }
        public int CorrectionCount { get; private set; }
        public int MissingBiomeCount { get; private set; }
        public IReadOnlyList<string> Messages => messages.AsReadOnly();

        public void RecordResolved(string regionId, WorldWeather weather)
        {
            ResolutionCount++;
            messages.Add("Resolved:" + regionId + ":" + weather);
        }

        public void RecordCorrection(string regionId, WorldWeather from, WorldWeather to)
        {
            CorrectionCount++;
            messages.Add("Corrected:" + regionId + ":" + from + "->" + to);
        }

        public void RecordRejected(string regionId, string reason)
        {
            RejectionCount++;
            if (reason.IndexOf("Biome", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                MissingBiomeCount++;
            }

            messages.Add("Rejected:" + regionId + ":" + reason);
        }
    }

    public readonly struct RegionalWeatherChanged : IGameplayEvent
    {
        public string WorldId { get; }
        public string RegionId { get; }
        public WorldWeather PreviousWeather { get; }
        public WorldWeather NewWeather { get; }
        public SimulationSeason Season { get; }
        public int WeatherStep { get; }
        public WorldBiomeType BiomeType { get; }

        public RegionalWeatherChanged(string worldId, string regionId, WorldWeather previousWeather, WorldWeather newWeather, SimulationSeason season, int weatherStep, WorldBiomeType biomeType)
        {
            WorldId = worldId;
            RegionId = regionId;
            PreviousWeather = previousWeather;
            NewWeather = newWeather;
            Season = season;
            WeatherStep = weatherStep;
            BiomeType = biomeType;
        }
    }

    public readonly struct RegionalWeatherCorrected : IGameplayEvent
    {
        public string WorldId { get; }
        public string RegionId { get; }
        public WorldWeather PreviousWeather { get; }
        public WorldWeather NewWeather { get; }
        public SimulationSeason Season { get; }
        public int WeatherStep { get; }
        public WorldBiomeType BiomeType { get; }

        public RegionalWeatherCorrected(string worldId, string regionId, WorldWeather previousWeather, WorldWeather newWeather, SimulationSeason season, int weatherStep, WorldBiomeType biomeType)
        {
            WorldId = worldId;
            RegionId = regionId;
            PreviousWeather = previousWeather;
            NewWeather = newWeather;
            Season = season;
            WeatherStep = weatherStep;
            BiomeType = biomeType;
        }
    }

    public readonly struct RegionalWeatherRejected : IGameplayEvent
    {
        public string WorldId { get; }
        public string RegionId { get; }
        public string Reason { get; }

        public RegionalWeatherRejected(string worldId, string regionId, string reason)
        {
            WorldId = worldId;
            RegionId = regionId;
            Reason = reason;
        }
    }
}
