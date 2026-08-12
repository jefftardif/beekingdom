using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Population
{
    public enum ColonyStrategyType { Survival, Expansion, EconomicGrowth, PopulationGrowth, FoodAccumulation, Defense, Research, Exploration, SwarmPreparation, EmergencyRecovery }
    public enum StrategyMode { Balanced, Defensive, Aggressive, Expansionist, Economic, Scientific, Emergency }

    public sealed class ColonyStrategyDefinition
    {
        public string StrategyId { get; }
        public ColonyStrategyType Type { get; }
        public StrategyMode Mode { get; }
        public double ActivationWeight { get; }
        public ColonyStrategyDefinition(string strategyId, ColonyStrategyType type, StrategyMode mode, double activationWeight)
        {
            StrategyId = string.IsNullOrWhiteSpace(strategyId) ? throw new ArgumentException("Strategy id is required.", nameof(strategyId)) : strategyId;
            Type = type;
            Mode = mode;
            ActivationWeight = Math.Max(0d, activationWeight);
        }
    }

    public sealed class ColonyGoal
    {
        public string GoalId { get; }
        public string Description { get; }
        public double Priority { get; }
        public bool Completed { get; private set; }
        public ColonyGoal(string goalId, string description, double priority) { GoalId = goalId; Description = description ?? string.Empty; Priority = Math.Max(0d, priority); }
        public void Complete() => Completed = true;
    }

    public readonly struct StrategyContext
    {
        public double FoodPressure { get; }
        public double HealthRisk { get; }
        public double StructuralRisk { get; }
        public double WeatherRisk { get; }
        public double SeasonPressure { get; }
        public double GrowthPressure { get; }
        public double ThreatPressure { get; }
        public double PlayerGoalWeight { get; }
        public StrategyContext(double foodPressure = 0d, double healthRisk = 0d, double structuralRisk = 0d, double weatherRisk = 0d, double seasonPressure = 0d, double growthPressure = 0d, double threatPressure = 0d, double playerGoalWeight = 0d)
        { FoodPressure = C(foodPressure); HealthRisk = C(healthRisk); StructuralRisk = C(structuralRisk); WeatherRisk = C(weatherRisk); SeasonPressure = C(seasonPressure); GrowthPressure = C(growthPressure); ThreatPressure = C(threatPressure); PlayerGoalWeight = C(playerGoalWeight); }
        private static double C(double v) => v < 0d ? 0d : v > 1d ? 1d : v;
    }

    public sealed class StrategyEvaluator
    {
        public double Evaluate(ColonyStrategyDefinition definition, StrategyContext context)
        {
            double emergency = Math.Max(Math.Max(context.ThreatPressure, context.HealthRisk), context.StructuralRisk);
            switch (definition.Type)
            {
                case ColonyStrategyType.Survival: return emergency * definition.ActivationWeight;
                case ColonyStrategyType.Defense: return context.ThreatPressure * definition.ActivationWeight;
                case ColonyStrategyType.FoodAccumulation: return context.FoodPressure * definition.ActivationWeight;
                case ColonyStrategyType.Expansion: return context.GrowthPressure * definition.ActivationWeight;
                case ColonyStrategyType.EmergencyRecovery: return Math.Max(emergency, context.WeatherRisk) * definition.ActivationWeight;
                default: return Math.Max(context.PlayerGoalWeight, context.SeasonPressure) * definition.ActivationWeight;
            }
        }
    }

    public sealed class ColonyStrategyEngine
    {
        private readonly StrategyEvaluator evaluator = new StrategyEvaluator();
        public ColonyStrategyDefinition EvaluateStrategy(IReadOnlyList<ColonyStrategyDefinition> definitions, StrategyContext context)
        {
            ColonyStrategyDefinition best = null; double score = double.MinValue;
            for (int i = 0; i < definitions.Count; i++) { double s = evaluator.Evaluate(definitions[i], context); if (best == null || s > score) { best = definitions[i]; score = s; } }
            return best;
        }
        public IReadOnlyList<ColonyGoal> GenerateGoals(ColonyStrategyDefinition strategy)
        {
            return new[] { new ColonyGoal(strategy.StrategyId + "-goal", strategy.Type.ToString(), strategy.ActivationWeight) };
        }
    }

    public sealed class StrategyDiagnostics { public int Evaluations { get; private set; } public int Changes { get; private set; } public int Goals { get; private set; } public void RecordEvaluation() => Evaluations++; public void RecordChange() => Changes++; public void RecordGoals(int count) => Goals += count; }

    public sealed class ColonyStrategyManager
    {
        private readonly List<ColonyStrategyDefinition> strategies = new List<ColonyStrategyDefinition>();
        private readonly List<ColonyGoal> goals = new List<ColonyGoal>();
        private readonly ColonyStrategyEngine engine = new ColonyStrategyEngine();
        private readonly IEventBus eventBus;
        private ColonyStrategyDefinition current;
        public StrategyDiagnostics Diagnostics { get; } = new StrategyDiagnostics();
        public ColonyStrategyManager(IEventBus eventBus = null) { this.eventBus = eventBus; }
        public bool RegisterStrategy(ColonyStrategyDefinition strategy) { if (strategy == null) return false; strategies.Add(strategy); return true; }
        public ColonyStrategyDefinition EvaluateStrategy(StrategyContext context)
        {
            ColonyStrategyDefinition next = engine.EvaluateStrategy(strategies, context);
            Diagnostics.RecordEvaluation();
            if (next != null && (current == null || current.StrategyId != next.StrategyId)) { current = next; Diagnostics.RecordChange(); eventBus?.Publish(new StrategyChanged(next.StrategyId)); if (next.Mode == StrategyMode.Emergency) eventBus?.Publish(new EmergencyStrategyActivated(next.StrategyId)); }
            return current;
        }
        public IReadOnlyList<ColonyGoal> GenerateGoals()
        {
            goals.Clear();
            if (current == null) return goals;
            goals.AddRange(engine.GenerateGoals(current));
            Diagnostics.RecordGoals(goals.Count);
            for (int i = 0; i < goals.Count; i++) eventBus?.Publish(new GoalCreated(goals[i].GoalId));
            return goals;
        }
        public ColonyStrategyDefinition UpdateStrategy(StrategyContext context) { ColonyStrategyDefinition strategy = EvaluateStrategy(context); GenerateGoals(); return strategy; }
        public ColonyStrategyDefinition QueryCurrentStrategy() => current;
        public IReadOnlyList<ColonyGoal> QueryGoals() => goals;
        public bool CompleteGoal(string goalId) { for (int i = 0; i < goals.Count; i++) if (goals[i].GoalId == goalId) { goals[i].Complete(); eventBus?.Publish(new GoalCompleted(goalId)); return true; } return false; }
        public bool AbandonGoal(string goalId) { for (int i = 0; i < goals.Count; i++) if (goals[i].GoalId == goalId) { goals.RemoveAt(i); eventBus?.Publish(new GoalAbandoned(goalId)); return true; } return false; }
    }

    public readonly struct StrategyChanged : IGameplayEvent, IBeeEvent { public string StrategyId { get; } public StrategyChanged(string strategyId) { StrategyId = strategyId; } }
    public readonly struct GoalCreated : IGameplayEvent, IBeeEvent { public string GoalId { get; } public GoalCreated(string goalId) { GoalId = goalId; } }
    public readonly struct GoalCompleted : IGameplayEvent, IBeeEvent { public string GoalId { get; } public GoalCompleted(string goalId) { GoalId = goalId; } }
    public readonly struct GoalAbandoned : IGameplayEvent, IBeeEvent { public string GoalId { get; } public GoalAbandoned(string goalId) { GoalId = goalId; } }
    public readonly struct EmergencyStrategyActivated : IGameplayEvent, IBeeEvent { public string StrategyId { get; } public EmergencyStrategyActivated(string strategyId) { StrategyId = strategyId; } }
}
