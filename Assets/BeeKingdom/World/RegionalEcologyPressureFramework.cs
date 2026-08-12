using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;

namespace BeeKingdom.World
{
    public enum RegionalEcologyStateKind
    {
        Healthy,
        Fragile,
        Degraded,
        Critical
    }

    public interface IRegionalEcologyEvaluator
    {
        RegionalEcologySnapshot Evaluate(RegionalEcologyInput input);
    }

    public interface IRegionalEcologySnapshotProvider
    {
        RegionalEcologySnapshot QueryEcologySnapshot(string regionId);
    }

    public sealed class RegionalEcologyEvaluator : IRegionalEcologyEvaluator
    {
        private readonly RegionalEcologyDiagnostics diagnostics;

        public RegionalEcologyEvaluator(RegionalEcologyDiagnostics diagnostics = null)
        {
            this.diagnostics = diagnostics ?? new RegionalEcologyDiagnostics();
        }

        public RegionalEcologySnapshot Evaluate(RegionalEcologyInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            double climatePressure = input.WeatherSnapshot.Weather == WorldWeather.Storm ? 0.25d : input.WeatherSnapshot.Weather == WorldWeather.Wind ? 0.12d : 0d;
            double depletionPressure = CalculateDepletion(input.ResourceDistribution);
            double waterPressure = 1d - Clamp01(input.WaterAvailability);
            double pollinationPressure = 1d - Clamp01(input.PollinationFactor);
            double biomePressure = 1d - Clamp01(input.BiomeFactor);
            double score = Clamp01((depletionPressure * 0.35d) + (waterPressure * 0.25d) + (climatePressure * 0.2d) + (pollinationPressure * 0.15d) + (biomePressure * 0.05d));
            RegionalEcologyStateKind state = ResolveState(score);
            if (state == RegionalEcologyStateKind.Critical)
            {
                diagnostics.RecordCritical(input.RegionId, score);
            }

            diagnostics.RecordEvaluation(input.RegionId, score);
            return new RegionalEcologySnapshot(input.WorldId, input.RegionId, score, state, input.WeatherSnapshot.Weather, input.WeatherSnapshot.WeatherStep, depletionPressure, waterPressure, climatePressure);
        }

        private static double CalculateDepletion(RegionalResourceDistributionSnapshot distribution)
        {
            if (distribution == null || distribution.Plans.Count == 0) return 1d;
            double ratio = 0d;
            for (int i = 0; i < distribution.Plans.Count; i++)
            {
                RegionalResourceNodePlan plan = distribution.Plans[i];
                ratio += plan.Capacity <= 0d ? 1d : plan.InitialAmount / plan.Capacity;
            }

            return 1d - Clamp01(ratio / distribution.Plans.Count);
        }

        private static RegionalEcologyStateKind ResolveState(double score)
        {
            if (score >= 0.75d) return RegionalEcologyStateKind.Critical;
            if (score >= 0.5d) return RegionalEcologyStateKind.Degraded;
            if (score >= 0.25d) return RegionalEcologyStateKind.Fragile;
            return RegionalEcologyStateKind.Healthy;
        }

        private static double Clamp01(double value)
        {
            if (value < 0d) return 0d;
            if (value > 1d) return 1d;
            return value;
        }
    }

    public sealed class RegionalEcologyInput
    {
        public string WorldId { get; }
        public string RegionId { get; }
        public RegionalWeatherSnapshot WeatherSnapshot { get; }
        public RegionalResourceDistributionSnapshot ResourceDistribution { get; }
        public double PollinationFactor { get; }
        public double BiomeFactor { get; }
        public double WaterAvailability { get; }

        public RegionalEcologyInput(string worldId, string regionId, RegionalWeatherSnapshot weatherSnapshot, RegionalResourceDistributionSnapshot resourceDistribution, double pollinationFactor, double biomeFactor, double waterAvailability)
        {
            WorldId = string.IsNullOrWhiteSpace(worldId) ? throw new ArgumentException("WorldId is required.") : worldId;
            RegionId = string.IsNullOrWhiteSpace(regionId) ? throw new ArgumentException("RegionId is required.") : regionId;
            WeatherSnapshot = weatherSnapshot ?? throw new ArgumentNullException(nameof(weatherSnapshot));
            ResourceDistribution = resourceDistribution;
            PollinationFactor = pollinationFactor < 0d ? 0d : pollinationFactor;
            BiomeFactor = biomeFactor < 0d ? 0d : biomeFactor;
            WaterAvailability = waterAvailability < 0d ? 0d : waterAvailability;
        }
    }

    public sealed class RegionalEcologyPressure
    {
        public double Score { get; }
        public RegionalEcologyStateKind State { get; }
        public RegionalEcologyPressure(double score, RegionalEcologyStateKind state) { Score = score < 0d ? 0d : score > 1d ? 1d : score; State = state; }
    }

    public sealed class RegionalEcologyState : IRegionalEcologySnapshotProvider
    {
        private readonly Dictionary<string, RegionalEcologySnapshot> snapshots = new Dictionary<string, RegionalEcologySnapshot>();
        private readonly HashSet<string> thresholdRegions = new HashSet<string>();

        public bool Apply(RegionalEcologySnapshot snapshot)
        {
            bool crossed = snapshot.State == RegionalEcologyStateKind.Degraded || snapshot.State == RegionalEcologyStateKind.Critical;
            bool publish = crossed && thresholdRegions.Add(snapshot.RegionId);
            snapshots[snapshot.RegionId] = snapshot;
            return publish;
        }

        public RegionalEcologySnapshot QueryEcologySnapshot(string regionId)
        {
            return snapshots.TryGetValue(regionId, out RegionalEcologySnapshot snapshot) ? snapshot : null;
        }
    }

    public sealed class RegionalEcologySnapshot : IEquatable<RegionalEcologySnapshot>
    {
        public string WorldId { get; }
        public string RegionId { get; }
        public double PressureScore { get; }
        public RegionalEcologyStateKind State { get; }
        public WorldWeather Weather { get; }
        public int WeatherStep { get; }
        public double DepletionPressure { get; }
        public double WaterPressure { get; }
        public double ClimatePressure { get; }

        public RegionalEcologySnapshot(string worldId, string regionId, double pressureScore, RegionalEcologyStateKind state, WorldWeather weather, int weatherStep, double depletionPressure, double waterPressure, double climatePressure)
        {
            WorldId = worldId;
            RegionId = regionId;
            PressureScore = pressureScore;
            State = state;
            Weather = weather;
            WeatherStep = weatherStep;
            DepletionPressure = depletionPressure;
            WaterPressure = waterPressure;
            ClimatePressure = climatePressure;
        }

        public bool Equals(RegionalEcologySnapshot other)
        {
            return other != null && WorldId == other.WorldId && RegionId == other.RegionId && PressureScore.Equals(other.PressureScore) && State == other.State && Weather == other.Weather && WeatherStep == other.WeatherStep;
        }

        public override bool Equals(object obj) { return Equals(obj as RegionalEcologySnapshot); }
        public override int GetHashCode() { return RegionId.GetHashCode() ^ WeatherStep; }
    }

    public sealed class RegionalEcologyDiagnostics
    {
        private readonly List<string> messages = new List<string>();
        public int EvaluationCount { get; private set; }
        public int CriticalCount { get; private set; }
        public IReadOnlyList<string> Messages => messages.AsReadOnly();
        public void RecordEvaluation(string regionId, double score) { EvaluationCount++; messages.Add("Evaluated:" + regionId + ":" + score); }
        public void RecordCritical(string regionId, double score) { CriticalCount++; messages.Add("Critical:" + regionId + ":" + score); }
    }

    public readonly struct RegionalEcologyPressureChanged : IGameplayEvent
    {
        public string WorldId { get; }
        public string RegionId { get; }
        public double PreviousScore { get; }
        public double NewScore { get; }
        public RegionalEcologyStateKind State { get; }
        public RegionalEcologyPressureChanged(string worldId, string regionId, double previousScore, double newScore, RegionalEcologyStateKind state) { WorldId = worldId; RegionId = regionId; PreviousScore = previousScore; NewScore = newScore; State = state; }
    }

    public readonly struct RegionalEcologyThresholdReached : IGameplayEvent
    {
        public string WorldId { get; }
        public string RegionId { get; }
        public double Score { get; }
        public RegionalEcologyStateKind State { get; }
        public RegionalEcologyThresholdReached(string worldId, string regionId, double score, RegionalEcologyStateKind state) { WorldId = worldId; RegionId = regionId; Score = score; State = state; }
    }
}
