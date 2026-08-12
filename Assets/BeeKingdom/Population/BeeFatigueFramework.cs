using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Population
{
    public enum FatigueState { FullyRested, Rested, Active, Tired, Exhausted, Burnout }
    public enum FatigueSource { Movement, Construction, Harvesting, Combat, Care, Transport, ExtremeTemperature, Disease, SleepDeprivation, Custom }

    public sealed class FatigueDefinition
    {
        public string DefinitionId { get; }
        public double MaximumFatigue { get; }
        public double BaseRecoveryRate { get; }
        public double TiredThreshold { get; }
        public double ExhaustedThreshold { get; }
        public double BurnoutThreshold { get; }
        public double PerformancePenaltyWeight { get; }

        public FatigueDefinition(string definitionId, double maximumFatigue, double baseRecoveryRate, double tiredThreshold, double exhaustedThreshold, double burnoutThreshold, double performancePenaltyWeight)
        {
            DefinitionId = string.IsNullOrWhiteSpace(definitionId) ? throw new ArgumentException("Definition id is required.", nameof(definitionId)) : definitionId;
            MaximumFatigue = maximumFatigue <= 0d ? 1d : maximumFatigue;
            BaseRecoveryRate = Math.Max(0d, baseRecoveryRate);
            TiredThreshold = Clamp(tiredThreshold, 0d, MaximumFatigue);
            ExhaustedThreshold = Clamp(exhaustedThreshold, 0d, MaximumFatigue);
            BurnoutThreshold = Clamp(burnoutThreshold, 0d, MaximumFatigue);
            PerformancePenaltyWeight = Clamp01(performancePenaltyWeight);
        }

        private static double Clamp(double value, double minimum, double maximum) => value < minimum ? minimum : value > maximum ? maximum : value;
        private static double Clamp01(double value) => value < 0d ? 0d : value > 1d ? 1d : value;
    }

    public sealed class FatigueRecord
    {
        public string BeeId { get; }
        public string DefinitionId { get; }
        public double CurrentFatigue { get; private set; }
        public FatigueState State { get; private set; }
        public double PerformanceModifier { get; private set; }

        public FatigueRecord(string beeId, FatigueDefinition definition)
        {
            BeeId = beeId ?? string.Empty;
            DefinitionId = definition.DefinitionId;
            CurrentFatigue = 0d;
            State = FatigueState.FullyRested;
            PerformanceModifier = 1d;
        }

        public void Increase(double amount, double maximum) => CurrentFatigue = Math.Min(maximum, CurrentFatigue + Math.Max(0d, amount));
        public void Recover(double amount) => CurrentFatigue = Math.Max(0d, CurrentFatigue - Math.Max(0d, amount));
        public void SetEvaluation(FatigueState state, double performanceModifier)
        {
            State = state;
            PerformanceModifier = performanceModifier < 0d ? 0d : performanceModifier > 1d ? 1d : performanceModifier;
        }
    }

    public readonly struct FatigueContext
    {
        public string BeeId { get; }
        public string GenomeId { get; }
        public double RestQuality { get; }
        public double FoodFactor { get; }
        public double TemperatureFactor { get; }
        public double HealthFactor { get; }
        public double ColonyBonusFactor { get; }

        public FatigueContext(string beeId, string genomeId = "", double restQuality = 1d, double foodFactor = 1d, double temperatureFactor = 1d, double healthFactor = 1d, double colonyBonusFactor = 1d)
        {
            BeeId = beeId ?? string.Empty;
            GenomeId = genomeId ?? string.Empty;
            RestQuality = Clamp01(restQuality);
            FoodFactor = Clamp01(foodFactor);
            TemperatureFactor = Clamp01(temperatureFactor);
            HealthFactor = Clamp01(healthFactor);
            ColonyBonusFactor = Clamp01(colonyBonusFactor);
        }

        private static double Clamp01(double value) => value < 0d ? 0d : value > 1d ? 1d : value;
    }

    public sealed class FatigueEvaluator
    {
        public FatigueState EvaluateState(FatigueDefinition definition, FatigueRecord record)
        {
            if (record.CurrentFatigue >= definition.BurnoutThreshold) return FatigueState.Burnout;
            if (record.CurrentFatigue >= definition.ExhaustedThreshold) return FatigueState.Exhausted;
            if (record.CurrentFatigue >= definition.TiredThreshold) return FatigueState.Tired;
            if (record.CurrentFatigue > definition.TiredThreshold * 0.5d) return FatigueState.Active;
            if (record.CurrentFatigue > 0d) return FatigueState.Rested;
            return FatigueState.FullyRested;
        }

        public double CalculatePerformanceModifier(FatigueDefinition definition, FatigueRecord record)
        {
            double normalized = definition.MaximumFatigue <= 0d ? 0d : record.CurrentFatigue / definition.MaximumFatigue;
            return Math.Max(0d, 1d - normalized * definition.PerformancePenaltyWeight);
        }
    }

    public sealed class BeeFatigueEngine
    {
        private readonly FatigueEvaluator evaluator = new FatigueEvaluator();

        public void IncreaseFatigue(FatigueDefinition definition, FatigueRecord record, FatigueSource source, double amount)
        {
            record.Increase(amount * SourceMultiplier(source), definition.MaximumFatigue);
            EvaluateFatigue(definition, record);
        }

        public void RecoverFatigue(FatigueDefinition definition, FatigueRecord record, FatigueContext context, double days)
        {
            double recoveryFactor = (context.RestQuality + context.FoodFactor + context.TemperatureFactor + context.HealthFactor + context.ColonyBonusFactor) / 5d;
            record.Recover(definition.BaseRecoveryRate * Math.Max(0d, days) * recoveryFactor);
            EvaluateFatigue(definition, record);
        }

        public FatigueState EvaluateFatigue(FatigueDefinition definition, FatigueRecord record)
        {
            FatigueState state = evaluator.EvaluateState(definition, record);
            double modifier = evaluator.CalculatePerformanceModifier(definition, record);
            record.SetEvaluation(state, modifier);
            return state;
        }

        private static double SourceMultiplier(FatigueSource source)
        {
            switch (source)
            {
                case FatigueSource.Combat: return 1.5d;
                case FatigueSource.ExtremeTemperature:
                case FatigueSource.Disease:
                case FatigueSource.SleepDeprivation: return 1.25d;
                default: return 1d;
            }
        }
    }

    public sealed class BeeFatigueDiagnostics
    {
        public int DefinitionsRegistered { get; private set; }
        public int RecordsCreated { get; private set; }
        public int Increases { get; private set; }
        public int Recoveries { get; private set; }
        public int Evaluations { get; private set; }
        public int Burnouts { get; private set; }

        public void RecordDefinitions(int count) => DefinitionsRegistered = count;
        public void RecordCreated() => RecordsCreated++;
        public void RecordIncrease() => Increases++;
        public void RecordRecovery() => Recoveries++;
        public void RecordEvaluation() => Evaluations++;
        public void RecordBurnout() => Burnouts++;
    }

    public sealed class BeeFatigueManager
    {
        private readonly Dictionary<string, FatigueDefinition> definitions = new Dictionary<string, FatigueDefinition>();
        private readonly Dictionary<string, FatigueRecord> records = new Dictionary<string, FatigueRecord>();
        private readonly BeeFatigueEngine engine = new BeeFatigueEngine();
        private readonly IEventBus eventBus;

        public BeeFatigueDiagnostics Diagnostics { get; } = new BeeFatigueDiagnostics();

        public BeeFatigueManager(IEventBus eventBus = null)
        {
            this.eventBus = eventBus;
        }

        public bool RegisterFatigueDefinition(FatigueDefinition definition)
        {
            if (definition == null || definitions.ContainsKey(definition.DefinitionId)) return false;
            definitions.Add(definition.DefinitionId, definition);
            Diagnostics.RecordDefinitions(definitions.Count);
            return true;
        }

        public FatigueRecord CreateFatigueRecord(string beeId, string definitionId)
        {
            if (!definitions.TryGetValue(definitionId, out FatigueDefinition definition)) return null;
            FatigueRecord record = new FatigueRecord(beeId, definition);
            records[beeId ?? string.Empty] = record;
            Diagnostics.RecordCreated();
            return record;
        }

        public FatigueRecord QueryFatigue(string beeId)
        {
            return records.TryGetValue(beeId ?? string.Empty, out FatigueRecord record) ? record : null;
        }

        public FatigueState GetFatigueState(string beeId)
        {
            return QueryFatigue(beeId)?.State ?? FatigueState.Burnout;
        }

        public FatigueState EvaluateFatigue(string beeId)
        {
            FatigueRecord record = QueryFatigue(beeId);
            if (record == null || !definitions.TryGetValue(record.DefinitionId, out FatigueDefinition definition)) return FatigueState.Burnout;
            FatigueState previous = record.State;
            FatigueState state = engine.EvaluateFatigue(definition, record);
            Diagnostics.RecordEvaluation();
            if (previous != state) eventBus?.Publish(new FatigueStateChanged(beeId, state));
            if (state == FatigueState.Exhausted) eventBus?.Publish(new BeeExhausted(beeId));
            if (state == FatigueState.Burnout && previous != FatigueState.Burnout) { Diagnostics.RecordBurnout(); eventBus?.Publish(new BurnoutReached(beeId)); }
            return state;
        }

        public bool IncreaseFatigue(string beeId, FatigueSource source, double amount)
        {
            FatigueRecord record = QueryFatigue(beeId);
            if (record == null || !definitions.TryGetValue(record.DefinitionId, out FatigueDefinition definition)) return false;
            engine.IncreaseFatigue(definition, record, source, amount);
            Diagnostics.RecordIncrease();
            eventBus?.Publish(new FatigueIncreased(beeId, source, amount));
            EvaluateFatigue(beeId);
            return true;
        }

        public bool RecoverFatigue(FatigueContext context, double days)
        {
            FatigueRecord record = QueryFatigue(context.BeeId);
            if (record == null || !definitions.TryGetValue(record.DefinitionId, out FatigueDefinition definition)) return false;
            engine.RecoverFatigue(definition, record, context, days);
            Diagnostics.RecordRecovery();
            eventBus?.Publish(new FatigueRecovered(context.BeeId));
            EvaluateFatigue(context.BeeId);
            return true;
        }
    }

    public readonly struct FatigueIncreased : IGameplayEvent, IBeeEvent { public string BeeId { get; } public FatigueSource Source { get; } public double Amount { get; } public FatigueIncreased(string beeId, FatigueSource source, double amount) { BeeId = beeId; Source = source; Amount = amount; } }
    public readonly struct FatigueRecovered : IGameplayEvent, IBeeEvent { public string BeeId { get; } public FatigueRecovered(string beeId) { BeeId = beeId; } }
    public readonly struct BeeExhausted : IGameplayEvent, IBeeEvent { public string BeeId { get; } public BeeExhausted(string beeId) { BeeId = beeId; } }
    public readonly struct BurnoutReached : IGameplayEvent, IBeeEvent { public string BeeId { get; } public BurnoutReached(string beeId) { BeeId = beeId; } }
    public readonly struct FatigueStateChanged : IGameplayEvent, IBeeEvent { public string BeeId { get; } public FatigueState State { get; } public FatigueStateChanged(string beeId, FatigueState state) { BeeId = beeId; State = state; } }
}
