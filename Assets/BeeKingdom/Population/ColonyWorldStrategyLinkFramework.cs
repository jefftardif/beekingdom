using System;
using System.Collections.Generic;
using System.Linq;
using BeeKingdom.Core.Events;
using BeeKingdom.World;

namespace BeeKingdom.Population
{
    public interface IColonyWorldSignalCollector
    {
        WorldAwareStrategyContext Collect(ColonyWorldSignalInput input, ColonyWorldSignalWeights weights);
    }

    public interface IColonyWorldStrategyAdapter
    {
        StrategyContext ToStrategyContext(WorldAwareStrategyContext context);
    }

    public interface IColonyWorldStrategySnapshotProvider
    {
        ColonyWorldStrategySnapshot QuerySnapshot(string colonyId);
    }

    public sealed class WorldAwareStrategyContext
    {
        public string ColonyId { get; }
        public double WeatherRisk { get; }
        public double FoodPressure { get; }
        public double ExploreOpportunity { get; }
        public double WorldThreatPressure { get; }
        public double EcologyPressure { get; }

        public WorldAwareStrategyContext(string colonyId, double weatherRisk, double foodPressure, double exploreOpportunity, double worldThreatPressure, double ecologyPressure)
        {
            ColonyId = string.IsNullOrWhiteSpace(colonyId) ? throw new ArgumentException("ColonyId is required.") : colonyId;
            WeatherRisk = Clamp01(weatherRisk);
            FoodPressure = Clamp01(foodPressure);
            ExploreOpportunity = Clamp01(exploreOpportunity);
            WorldThreatPressure = Clamp01(worldThreatPressure);
            EcologyPressure = Clamp01(ecologyPressure);
        }

        private static double Clamp01(double value) { return value < 0d ? 0d : value > 1d ? 1d : value; }
    }

    public sealed class ColonyWorldSignalInput
    {
        public string ColonyId { get; }
        public WorldExplorationVisibilitySnapshot Visibility { get; }
        public IReadOnlyList<RegionalWeatherSnapshot> WeatherSnapshots { get; }
        public IReadOnlyList<RegionalEcologySnapshot> EcologySnapshots { get; }
        public IReadOnlyList<RegionalResourceDistributionSnapshot> ResourceSnapshots { get; }
        public IReadOnlyList<RegionalEventPropagationSnapshot> EventSnapshots { get; }

        public ColonyWorldSignalInput(string colonyId, WorldExplorationVisibilitySnapshot visibility, IReadOnlyList<RegionalWeatherSnapshot> weatherSnapshots, IReadOnlyList<RegionalEcologySnapshot> ecologySnapshots, IReadOnlyList<RegionalResourceDistributionSnapshot> resourceSnapshots, IReadOnlyList<RegionalEventPropagationSnapshot> eventSnapshots)
        {
            ColonyId = string.IsNullOrWhiteSpace(colonyId) ? throw new ArgumentException("ColonyId is required.") : colonyId;
            Visibility = visibility ?? new WorldExplorationVisibilitySnapshot(Array.Empty<RegionVisibilityRecord>());
            WeatherSnapshots = weatherSnapshots ?? Array.Empty<RegionalWeatherSnapshot>();
            EcologySnapshots = ecologySnapshots ?? Array.Empty<RegionalEcologySnapshot>();
            ResourceSnapshots = resourceSnapshots ?? Array.Empty<RegionalResourceDistributionSnapshot>();
            EventSnapshots = eventSnapshots ?? Array.Empty<RegionalEventPropagationSnapshot>();
        }
    }

    public sealed class ColonyWorldSignalWeights
    {
        public double WeatherWeight { get; }
        public double FoodWeight { get; }
        public double ExplorationWeight { get; }
        public double ThreatWeight { get; }
        public double EcologyWeight { get; }

        public ColonyWorldSignalWeights(double weatherWeight = 1d, double foodWeight = 1d, double explorationWeight = 1d, double threatWeight = 1d, double ecologyWeight = 1d)
        {
            WeatherWeight = Clamp(weight: weatherWeight);
            FoodWeight = Clamp(weight: foodWeight);
            ExplorationWeight = Clamp(weight: explorationWeight);
            ThreatWeight = Clamp(weight: threatWeight);
            EcologyWeight = Clamp(weight: ecologyWeight);
        }

        private static double Clamp(double weight) { return weight < 0d ? 0d : weight; }
    }

    public sealed class ColonyWorldSignalCollector : IColonyWorldSignalCollector
    {
        private readonly ColonyWorldStrategyDiagnostics diagnostics;

        public ColonyWorldSignalCollector(ColonyWorldStrategyDiagnostics diagnostics = null)
        {
            this.diagnostics = diagnostics ?? new ColonyWorldStrategyDiagnostics();
        }

        public WorldAwareStrategyContext Collect(ColonyWorldSignalInput input, ColonyWorldSignalWeights weights)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            weights = weights ?? new ColonyWorldSignalWeights();
            HashSet<string> visibleRegions = new HashSet<string>(
                input.Visibility.Records
                    .Where(record => record.ColonyId == input.ColonyId && record.State != ExplorationVisibilityState.Unknown && record.State != ExplorationVisibilityState.Stale)
                    .Select(record => record.RegionId));

            if (visibleRegions.Count == 0)
            {
                diagnostics.RecordMissing(input.ColonyId, "No visible regions.");
                return new WorldAwareStrategyContext(input.ColonyId, 0d, 0d, 1d, 0d, 0d);
            }

            double weatherRisk = Average(input.WeatherSnapshots.Where(snapshot => visibleRegions.Contains(snapshot.RegionId)).Select(WeatherRisk)) * weights.WeatherWeight;
            double foodPressure = CalculateFoodPressure(input.ResourceSnapshots.Where(snapshot => visibleRegions.Contains(snapshot.RegionId))) * weights.FoodWeight;
            double ecologyPressure = Average(input.EcologySnapshots.Where(snapshot => visibleRegions.Contains(snapshot.RegionId)).Select(snapshot => snapshot.PressureScore)) * weights.EcologyWeight;
            double threatPressure = CalculateThreat(input.EventSnapshots, visibleRegions) * weights.ThreatWeight;
            double exploreOpportunity = Math.Max(0d, 1d - (visibleRegions.Count / Math.Max(1d, input.Visibility.Records.Count))) * weights.ExplorationWeight;
            diagnostics.RecordCollected(input.ColonyId);
            return new WorldAwareStrategyContext(input.ColonyId, weatherRisk, foodPressure, exploreOpportunity, threatPressure, ecologyPressure);
        }

        private static double WeatherRisk(RegionalWeatherSnapshot snapshot)
        {
            if (snapshot.Weather == WorldWeather.Storm) return 1d;
            if (snapshot.Weather == WorldWeather.Wind) return 0.6d;
            if (snapshot.Weather == WorldWeather.Rain) return 0.3d;
            return 0d;
        }

        private static double CalculateFoodPressure(IEnumerable<RegionalResourceDistributionSnapshot> snapshots)
        {
            List<RegionalResourceNodePlan> plans = snapshots.SelectMany(snapshot => snapshot.Plans).ToList();
            if (plans.Count == 0) return 1d;
            double ratio = plans.Average(plan => plan.Capacity <= 0d ? 0d : plan.InitialAmount / plan.Capacity);
            return 1d - Clamp01(ratio);
        }

        private static double CalculateThreat(IEnumerable<RegionalEventPropagationSnapshot> events, HashSet<string> visibleRegions)
        {
            double threat = 0d;
            foreach (RegionalEventPropagationSnapshot snapshot in events)
            {
                foreach (RegionalEventAffectedRegion affected in snapshot.AffectedRegions)
                {
                    if (visibleRegions.Contains(affected.RegionId))
                    {
                        threat = Math.Max(threat, affected.Intensity);
                    }
                }
            }

            return threat;
        }

        private static double Average(IEnumerable<double> values)
        {
            List<double> list = values.ToList();
            return list.Count == 0 ? 0d : Clamp01(list.Average());
        }

        private static double Clamp01(double value) { return value < 0d ? 0d : value > 1d ? 1d : value; }
    }

    public sealed class ColonyWorldStrategyAdapter : IColonyWorldStrategyAdapter, IColonyWorldStrategySnapshotProvider
    {
        private readonly Dictionary<string, ColonyWorldStrategySnapshot> snapshots = new Dictionary<string, ColonyWorldStrategySnapshot>();

        public StrategyContext ToStrategyContext(WorldAwareStrategyContext context)
        {
            StrategyContext strategy = new StrategyContext(
                foodPressure: context.FoodPressure,
                weatherRisk: context.WeatherRisk,
                seasonPressure: context.EcologyPressure,
                growthPressure: context.ExploreOpportunity,
                threatPressure: context.WorldThreatPressure,
                playerGoalWeight: context.ExploreOpportunity);
            snapshots[context.ColonyId] = new ColonyWorldStrategySnapshot(context.ColonyId, context, strategy);
            return strategy;
        }

        public ColonyWorldStrategySnapshot QuerySnapshot(string colonyId)
        {
            return snapshots.TryGetValue(colonyId, out ColonyWorldStrategySnapshot snapshot) ? snapshot : null;
        }
    }

    public sealed class ColonyWorldStrategySnapshot
    {
        public string ColonyId { get; }
        public WorldAwareStrategyContext WorldContext { get; }
        public StrategyContext StrategyContext { get; }
        public ColonyWorldStrategySnapshot(string colonyId, WorldAwareStrategyContext worldContext, StrategyContext strategyContext) { ColonyId = colonyId; WorldContext = worldContext; StrategyContext = strategyContext; }
    }

    public sealed class ColonyWorldStrategyDiagnostics
    {
        private readonly List<string> messages = new List<string>();
        public int CollectedCount { get; private set; }
        public int MissingSignalCount { get; private set; }
        public IReadOnlyList<string> Messages => messages.AsReadOnly();
        public void RecordCollected(string colonyId) { CollectedCount++; messages.Add("Collected:" + colonyId); }
        public void RecordMissing(string colonyId, string signal) { MissingSignalCount++; messages.Add("Missing:" + colonyId + ":" + signal); }
    }

    public readonly struct ColonyWorldStrategyContextUpdated : IGameplayEvent, IBeeEvent
    {
        public string ColonyId { get; }
        public double WeatherRisk { get; }
        public double FoodPressure { get; }
        public double WorldThreatPressure { get; }
        public ColonyWorldStrategyContextUpdated(string colonyId, double weatherRisk, double foodPressure, double worldThreatPressure) { ColonyId = colonyId; WeatherRisk = weatherRisk; FoodPressure = foodPressure; WorldThreatPressure = worldThreatPressure; }
    }

    public readonly struct ColonyWorldSignalMissing : IGameplayEvent, IBeeEvent
    {
        public string ColonyId { get; }
        public string Signal { get; }
        public ColonyWorldSignalMissing(string colonyId, string signal) { ColonyId = colonyId; Signal = signal; }
    }
}
