using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Population
{
    public enum CollectiveBehaviorType { FoodGathering, ColonyExpansion, EmergencyDefense, FireResponse, FloodResponse, Swarming, PopulationRebalancing, ResourceRedistribution, EmergencyRepair, SeasonalPreparation, Custom }
    public enum SwarmSignalType { RoyalPheromone, AlarmPheromone, FoodPheromone, ConstructionSignal, DefenseSignal, RecruitmentSignal, SwarmSignal }
    public enum ColonyPriorityType { Survive, Produce, Build, Defend, Explore, Conserve, Swarm }

    public sealed class CollectiveBehaviorDefinition
    {
        public string BehaviorId { get; }
        public CollectiveBehaviorType Type { get; }
        public ColonyPriorityType Priority { get; }
        public double ActivationThreshold { get; }

        public CollectiveBehaviorDefinition(string behaviorId, CollectiveBehaviorType type, ColonyPriorityType priority, double activationThreshold)
        {
            BehaviorId = string.IsNullOrWhiteSpace(behaviorId) ? throw new ArgumentException("Behavior id is required.", nameof(behaviorId)) : behaviorId;
            Type = type;
            Priority = priority;
            ActivationThreshold = activationThreshold < 0d ? 0d : activationThreshold > 1d ? 1d : activationThreshold;
        }
    }

    public sealed class SwarmSignal
    {
        public string SignalId { get; }
        public SwarmSignalType Type { get; }
        public double Intensity { get; }
        public double Radius { get; }
        public double DurationDays { get; }
        public double Propagation { get; }
        public double Priority { get; }

        public SwarmSignal(string signalId, SwarmSignalType type, double intensity, double radius, double durationDays, double propagation, double priority)
        {
            SignalId = string.IsNullOrWhiteSpace(signalId) ? throw new ArgumentException("Signal id is required.", nameof(signalId)) : signalId;
            Type = type;
            Intensity = Clamp01(intensity);
            Radius = Math.Max(0d, radius);
            DurationDays = Math.Max(0d, durationDays);
            Propagation = Clamp01(propagation);
            Priority = Clamp01(priority);
        }

        private static double Clamp01(double value) => value < 0d ? 0d : value > 1d ? 1d : value;
    }

    public readonly struct ColonyStateContext
    {
        public double SeasonPressure { get; }
        public double WeatherRisk { get; }
        public double PopulationPressure { get; }
        public double ResourcePressure { get; }
        public double HealthPressure { get; }
        public double ThreatPressure { get; }
        public double PlayerGoalPressure { get; }

        public ColonyStateContext(double seasonPressure = 0d, double weatherRisk = 0d, double populationPressure = 0d, double resourcePressure = 0d, double healthPressure = 0d, double threatPressure = 0d, double playerGoalPressure = 0d)
        {
            SeasonPressure = Clamp01(seasonPressure);
            WeatherRisk = Clamp01(weatherRisk);
            PopulationPressure = Clamp01(populationPressure);
            ResourcePressure = Clamp01(resourcePressure);
            HealthPressure = Clamp01(healthPressure);
            ThreatPressure = Clamp01(threatPressure);
            PlayerGoalPressure = Clamp01(playerGoalPressure);
        }

        private static double Clamp01(double value) => value < 0d ? 0d : value > 1d ? 1d : value;
    }

    public sealed class SwarmState
    {
        public IReadOnlyList<SwarmSignal> ActiveSignals { get; }
        public IReadOnlyDictionary<ColonyPriorityType, double> Priorities { get; }
        public CollectiveBehaviorType ActiveBehavior { get; }

        public SwarmState(IReadOnlyList<SwarmSignal> activeSignals, IReadOnlyDictionary<ColonyPriorityType, double> priorities, CollectiveBehaviorType activeBehavior)
        {
            ActiveSignals = activeSignals;
            Priorities = priorities;
            ActiveBehavior = activeBehavior;
        }
    }

    public sealed class CollectiveStatistics
    {
        public int SignalsBroadcast { get; }
        public int BehaviorsRegistered { get; }
        public int BehaviorsActivated { get; }
        public int EmergencyProtocols { get; }

        public CollectiveStatistics(int signalsBroadcast, int behaviorsRegistered, int behaviorsActivated, int emergencyProtocols)
        {
            SignalsBroadcast = signalsBroadcast;
            BehaviorsRegistered = behaviorsRegistered;
            BehaviorsActivated = behaviorsActivated;
            EmergencyProtocols = emergencyProtocols;
        }
    }

    public sealed class CollectiveBehaviorRegistry
    {
        private readonly List<CollectiveBehaviorDefinition> behaviors = new List<CollectiveBehaviorDefinition>();
        public int Count => behaviors.Count;
        public bool Register(CollectiveBehaviorDefinition behavior)
        {
            if (behavior == null) return false;
            behaviors.Add(behavior);
            return true;
        }
        public IReadOnlyList<CollectiveBehaviorDefinition> Query() => behaviors;
    }

    public sealed class SwarmSignalManager
    {
        private readonly List<SwarmSignal> signals = new List<SwarmSignal>();
        public int BroadcastCount { get; private set; }
        public void Broadcast(SwarmSignal signal) { if (signal != null) { signals.Add(signal); BroadcastCount++; } }
        public IReadOnlyList<SwarmSignal> QuerySignals() => signals;
    }

    public sealed class ColonyIntentEngine
    {
        public IReadOnlyDictionary<ColonyPriorityType, double> CalculateColonyPriorities(ColonyStateContext context)
        {
            Dictionary<ColonyPriorityType, double> priorities = new Dictionary<ColonyPriorityType, double>
            {
                { ColonyPriorityType.Survive, Math.Max(context.HealthPressure, context.ThreatPressure) },
                { ColonyPriorityType.Produce, Math.Max(0d, 1d - context.ResourcePressure) },
                { ColonyPriorityType.Build, Math.Max(context.PopulationPressure, context.PlayerGoalPressure) },
                { ColonyPriorityType.Defend, context.ThreatPressure },
                { ColonyPriorityType.Explore, Math.Max(0d, 1d - context.WeatherRisk) * context.PlayerGoalPressure },
                { ColonyPriorityType.Conserve, Math.Max(context.SeasonPressure, context.ResourcePressure) },
                { ColonyPriorityType.Swarm, context.PopulationPressure }
            };
            return priorities;
        }

        public CollectiveBehaviorType EvaluateColonyIntent(IReadOnlyList<CollectiveBehaviorDefinition> behaviors, IReadOnlyDictionary<ColonyPriorityType, double> priorities)
        {
            CollectiveBehaviorType selected = CollectiveBehaviorType.FoodGathering;
            double best = double.MinValue;
            for (int i = 0; i < behaviors.Count; i++)
            {
                priorities.TryGetValue(behaviors[i].Priority, out double score);
                if (score < behaviors[i].ActivationThreshold) continue;
                if (score > best) { best = score; selected = behaviors[i].Type; }
            }
            return selected;
        }
    }

    public sealed class SwarmCoordinator
    {
        public double CalculateCooperationScore(IReadOnlyDictionary<ColonyPriorityType, double> priorities, IReadOnlyList<SwarmSignal> signals)
        {
            double total = 0d;
            foreach (double value in priorities.Values) total += value;
            for (int i = 0; i < signals.Count; i++) total += signals[i].Intensity * signals[i].Priority;
            return Math.Min(1d, total / Math.Max(1d, priorities.Count + signals.Count));
        }
    }

    public sealed class CollectiveDiagnostics
    {
        public int PriorityChanges { get; private set; }
        public int BehaviorsActivated { get; private set; }
        public int EmergencyProtocols { get; private set; }
        public void RecordPriorityChange() => PriorityChanges++;
        public void RecordBehaviorActivated() => BehaviorsActivated++;
        public void RecordEmergency() => EmergencyProtocols++;
    }

    public sealed class CollectiveIntelligenceManager
    {
        private readonly CollectiveBehaviorRegistry behaviorRegistry = new CollectiveBehaviorRegistry();
        private readonly SwarmSignalManager signalManager = new SwarmSignalManager();
        private readonly ColonyIntentEngine intentEngine = new ColonyIntentEngine();
        private readonly SwarmCoordinator coordinator = new SwarmCoordinator();
        private readonly IEventBus eventBus;
        private IReadOnlyDictionary<ColonyPriorityType, double> lastPriorities = new Dictionary<ColonyPriorityType, double>();
        private CollectiveBehaviorType activeBehavior = CollectiveBehaviorType.FoodGathering;

        public CollectiveDiagnostics Diagnostics { get; } = new CollectiveDiagnostics();

        public CollectiveIntelligenceManager(IEventBus eventBus = null) { this.eventBus = eventBus; }

        public bool RegisterCollectiveBehavior(CollectiveBehaviorDefinition behavior) => behaviorRegistry.Register(behavior);

        public CollectiveBehaviorType EvaluateColonyIntent(ColonyStateContext context)
        {
            IReadOnlyDictionary<ColonyPriorityType, double> priorities = CalculateColonyPriorities(context);
            CollectiveBehaviorType next = intentEngine.EvaluateColonyIntent(behaviorRegistry.Query(), priorities);
            if (next != activeBehavior)
            {
                activeBehavior = next;
                Diagnostics.RecordBehaviorActivated();
                eventBus?.Publish(new CollectiveBehaviorActivated(next));
            }
            if (context.ThreatPressure >= 0.8d)
            {
                Diagnostics.RecordEmergency();
                eventBus?.Publish(new EmergencyProtocolActivated(CollectiveBehaviorType.EmergencyDefense));
            }
            return activeBehavior;
        }

        public void BroadcastSignal(SwarmSignal signal)
        {
            signalManager.Broadcast(signal);
            eventBus?.Publish(new SwarmSignalBroadcast(signal.Type, signal.Intensity));
        }

        public SwarmState QuerySwarmState() => new SwarmState(signalManager.QuerySignals(), lastPriorities, activeBehavior);

        public IReadOnlyDictionary<ColonyPriorityType, double> CalculateColonyPriorities(ColonyStateContext context)
        {
            lastPriorities = intentEngine.CalculateColonyPriorities(context);
            Diagnostics.RecordPriorityChange();
            eventBus?.Publish(new ColonyPriorityChanged());
            return lastPriorities;
        }

        public CollectiveStatistics QueryCollectiveStatistics() => new CollectiveStatistics(signalManager.BroadcastCount, behaviorRegistry.Count, Diagnostics.BehaviorsActivated, Diagnostics.EmergencyProtocols);
        public double QueryCooperationScore() => coordinator.CalculateCooperationScore(lastPriorities, signalManager.QuerySignals());
        public void CompleteActiveBehavior() => eventBus?.Publish(new CollectiveBehaviorCompleted(activeBehavior));
    }

    public readonly struct ColonyPriorityChanged : IGameplayEvent, IBeeEvent { }
    public readonly struct SwarmSignalBroadcast : IGameplayEvent, IBeeEvent { public SwarmSignalType Type { get; } public double Intensity { get; } public SwarmSignalBroadcast(SwarmSignalType type, double intensity) { Type = type; Intensity = intensity; } }
    public readonly struct CollectiveBehaviorActivated : IGameplayEvent, IBeeEvent { public CollectiveBehaviorType Type { get; } public CollectiveBehaviorActivated(CollectiveBehaviorType type) { Type = type; } }
    public readonly struct CollectiveBehaviorCompleted : IGameplayEvent, IBeeEvent { public CollectiveBehaviorType Type { get; } public CollectiveBehaviorCompleted(CollectiveBehaviorType type) { Type = type; } }
    public readonly struct EmergencyProtocolActivated : IGameplayEvent, IBeeEvent { public CollectiveBehaviorType Type { get; } public EmergencyProtocolActivated(CollectiveBehaviorType type) { Type = type; } }
}
