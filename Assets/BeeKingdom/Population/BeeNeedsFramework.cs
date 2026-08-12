using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Population
{
    public enum NeedKind { Hunger, Energy, Rest, Hydration, Health, Cleanliness, Safety, Temperature, Social, ColonyDuty, Custom }

    public sealed class NeedDefinition
    {
        public string NeedId { get; }
        public NeedKind Kind { get; }
        public double MaximumValue { get; }
        public double GrowthRate { get; }
        public double CriticalThreshold { get; }
        public double SatisfactionThreshold { get; }
        public double PriorityWeight { get; }

        public NeedDefinition(string needId, NeedKind kind, double maximumValue, double growthRate, double criticalThreshold, double satisfactionThreshold, double priorityWeight)
        {
            NeedId = string.IsNullOrWhiteSpace(needId) ? throw new ArgumentException("Need id is required.", nameof(needId)) : needId;
            Kind = kind;
            MaximumValue = maximumValue <= 0d ? 1d : maximumValue;
            GrowthRate = Math.Max(0d, growthRate);
            CriticalThreshold = Clamp(criticalThreshold, 0d, MaximumValue);
            SatisfactionThreshold = Clamp(satisfactionThreshold, 0d, MaximumValue);
            PriorityWeight = Math.Max(0d, priorityWeight);
        }

        private static double Clamp(double value, double minimum, double maximum) => value < minimum ? minimum : value > maximum ? maximum : value;
    }

    public sealed class NeedInstance
    {
        public string BeeId { get; }
        public string NeedId { get; }
        public NeedKind Kind { get; }
        public double CurrentValue { get; private set; }
        public double PriorityScore { get; private set; }
        public bool IsCritical { get; private set; }

        public NeedInstance(string beeId, NeedDefinition definition, double currentValue = 0d)
        {
            BeeId = beeId ?? string.Empty;
            NeedId = definition.NeedId;
            Kind = definition.Kind;
            CurrentValue = Clamp(currentValue, 0d, definition.MaximumValue);
        }

        public void Increase(double amount, double maximum) => CurrentValue = Clamp(CurrentValue + Math.Max(0d, amount), 0d, maximum);
        public void Satisfy(double amount) => CurrentValue = Math.Max(0d, CurrentValue - Math.Max(0d, amount));
        public void SetEvaluation(double priorityScore, bool isCritical)
        {
            PriorityScore = Math.Max(0d, priorityScore);
            IsCritical = isCritical;
        }

        private static double Clamp(double value, double minimum, double maximum) => value < minimum ? minimum : value > maximum ? maximum : value;
    }

    public readonly struct BeeNeedsContext
    {
        public string BeeId { get; }
        public string GenomeId { get; }
        public double AgeFactor { get; }
        public double CasteFactor { get; }
        public double HealthFactor { get; }
        public double SeasonFactor { get; }
        public double EnvironmentFactor { get; }
        public double GameplayEffectFactor { get; }

        public BeeNeedsContext(string beeId, string genomeId = "", double ageFactor = 1d, double casteFactor = 1d, double healthFactor = 1d, double seasonFactor = 1d, double environmentFactor = 1d, double gameplayEffectFactor = 1d)
        {
            BeeId = beeId ?? string.Empty;
            GenomeId = genomeId ?? string.Empty;
            AgeFactor = Clamp01(ageFactor);
            CasteFactor = Clamp01(casteFactor);
            HealthFactor = Clamp01(healthFactor);
            SeasonFactor = Clamp01(seasonFactor);
            EnvironmentFactor = Clamp01(environmentFactor);
            GameplayEffectFactor = Clamp01(gameplayEffectFactor);
        }

        private static double Clamp01(double value) => value < 0d ? 0d : value > 1d ? 1d : value;
    }

    public sealed class NeedEvaluator
    {
        public double CalculatePriority(NeedDefinition definition, NeedInstance instance, BeeNeedsContext context, IReadOnlyDictionary<string, double> traits)
        {
            traits.TryGetValue(definition.NeedId, out double geneticInfluence);
            double normalized = definition.MaximumValue <= 0d ? 0d : instance.CurrentValue / definition.MaximumValue;
            double contextFactor = (context.AgeFactor + context.CasteFactor + context.HealthFactor + context.SeasonFactor + context.EnvironmentFactor + context.GameplayEffectFactor) / 6d;
            return normalized * definition.PriorityWeight * (1d + geneticInfluence) * contextFactor;
        }
    }

    public sealed class BeeNeedsEngine
    {
        private readonly NeedEvaluator evaluator = new NeedEvaluator();

        public void UpdateNeed(NeedDefinition definition, NeedInstance instance, double days, BeeNeedsContext context)
        {
            double modifier = (context.SeasonFactor + context.EnvironmentFactor + context.GameplayEffectFactor) / 3d;
            instance.Increase(definition.GrowthRate * Math.Max(0d, days) * modifier, definition.MaximumValue);
        }

        public void EvaluateNeed(NeedDefinition definition, NeedInstance instance, BeeNeedsContext context, IReadOnlyDictionary<string, double> traits)
        {
            double priority = evaluator.CalculatePriority(definition, instance, context, traits);
            instance.SetEvaluation(priority, instance.CurrentValue >= definition.CriticalThreshold);
        }
    }

    public sealed class BeeNeedsDiagnostics
    {
        public int DefinitionsRegistered { get; private set; }
        public int BeesInitialized { get; private set; }
        public int Updates { get; private set; }
        public int Evaluations { get; private set; }
        public int Satisfactions { get; private set; }
        public int CriticalEvents { get; private set; }

        public void RecordDefinitions(int count) => DefinitionsRegistered = count;
        public void RecordBeeInitialized() => BeesInitialized++;
        public void RecordUpdate() => Updates++;
        public void RecordEvaluation() => Evaluations++;
        public void RecordSatisfaction() => Satisfactions++;
        public void RecordCritical() => CriticalEvents++;
    }

    public sealed class BeeNeedsManager
    {
        private readonly Dictionary<string, NeedDefinition> definitions = new Dictionary<string, NeedDefinition>();
        private readonly Dictionary<string, Dictionary<string, NeedInstance>> needsByBee = new Dictionary<string, Dictionary<string, NeedInstance>>();
        private readonly BeeNeedsEngine engine = new BeeNeedsEngine();
        private readonly GeneticsManager geneticsManager;
        private readonly IEventBus eventBus;

        public BeeNeedsDiagnostics Diagnostics { get; } = new BeeNeedsDiagnostics();

        public BeeNeedsManager(GeneticsManager geneticsManager = null, IEventBus eventBus = null)
        {
            this.geneticsManager = geneticsManager;
            this.eventBus = eventBus;
        }

        public bool RegisterNeedDefinition(NeedDefinition definition)
        {
            if (definition == null || definitions.ContainsKey(definition.NeedId)) return false;
            definitions.Add(definition.NeedId, definition);
            Diagnostics.RecordDefinitions(definitions.Count);
            return true;
        }

        public IReadOnlyList<NeedInstance> QueryNeeds(string beeId)
        {
            Dictionary<string, NeedInstance> needs = EnsureBeeNeeds(beeId);
            List<NeedInstance> result = new List<NeedInstance>(needs.Values);
            result.Sort((left, right) => string.CompareOrdinal(left.NeedId, right.NeedId));
            return result;
        }

        public IReadOnlyList<NeedInstance> UpdateNeeds(BeeNeedsContext context, double days)
        {
            Dictionary<string, NeedInstance> needs = EnsureBeeNeeds(context.BeeId);
            foreach (NeedInstance need in needs.Values)
            {
                NeedDefinition definition = definitions[need.NeedId];
                engine.UpdateNeed(definition, need, days, context);
                eventBus?.Publish(new NeedChanged(context.BeeId, need.NeedId, need.CurrentValue));
            }
            Diagnostics.RecordUpdate();
            return EvaluateNeeds(context);
        }

        public IReadOnlyList<NeedInstance> EvaluateNeeds(BeeNeedsContext context)
        {
            Dictionary<string, NeedInstance> needs = EnsureBeeNeeds(context.BeeId);
            IReadOnlyDictionary<string, double> traits = geneticsManager?.CalculateTraits(context.GenomeId) ?? new Dictionary<string, double>();
            foreach (NeedInstance need in needs.Values)
            {
                NeedDefinition definition = definitions[need.NeedId];
                bool wasCritical = need.IsCritical;
                engine.EvaluateNeed(definition, need, context, traits);
                if (need.IsCritical && !wasCritical)
                {
                    Diagnostics.RecordCritical();
                    eventBus?.Publish(new NeedCritical(context.BeeId, need.NeedId));
                }
                if (!need.IsCritical && wasCritical) eventBus?.Publish(new NeedRecovered(context.BeeId, need.NeedId));
            }
            Diagnostics.RecordEvaluation();
            eventBus?.Publish(new BeeNeedsUpdated(context.BeeId));
            return QueryNeeds(context.BeeId);
        }

        public NeedInstance GetHighestPriorityNeed(string beeId)
        {
            IReadOnlyList<NeedInstance> needs = QueryNeeds(beeId);
            NeedInstance best = null;
            for (int i = 0; i < needs.Count; i++)
            {
                if (best == null || needs[i].PriorityScore > best.PriorityScore) best = needs[i];
            }
            return best;
        }

        public bool SatisfyNeed(string beeId, string needId, double amount)
        {
            Dictionary<string, NeedInstance> needs = EnsureBeeNeeds(beeId);
            if (!needs.TryGetValue(needId, out NeedInstance need)) return false;
            bool wasCritical = need.IsCritical;
            need.Satisfy(amount);
            Diagnostics.RecordSatisfaction();
            eventBus?.Publish(new NeedSatisfied(beeId, needId));
            if (wasCritical) eventBus?.Publish(new NeedRecovered(beeId, needId));
            return true;
        }

        private Dictionary<string, NeedInstance> EnsureBeeNeeds(string beeId)
        {
            if (needsByBee.TryGetValue(beeId ?? string.Empty, out Dictionary<string, NeedInstance> needs)) return needs;
            needs = new Dictionary<string, NeedInstance>();
            foreach (NeedDefinition definition in definitions.Values) needs.Add(definition.NeedId, new NeedInstance(beeId, definition));
            needsByBee[beeId ?? string.Empty] = needs;
            Diagnostics.RecordBeeInitialized();
            return needs;
        }
    }

    public readonly struct NeedChanged : IGameplayEvent, IBeeEvent { public string BeeId { get; } public string NeedId { get; } public double Value { get; } public NeedChanged(string beeId, string needId, double value) { BeeId = beeId; NeedId = needId; Value = value; } }
    public readonly struct NeedCritical : IGameplayEvent, IBeeEvent { public string BeeId { get; } public string NeedId { get; } public NeedCritical(string beeId, string needId) { BeeId = beeId; NeedId = needId; } }
    public readonly struct NeedSatisfied : IGameplayEvent, IBeeEvent { public string BeeId { get; } public string NeedId { get; } public NeedSatisfied(string beeId, string needId) { BeeId = beeId; NeedId = needId; } }
    public readonly struct NeedRecovered : IGameplayEvent, IBeeEvent { public string BeeId { get; } public string NeedId { get; } public NeedRecovered(string beeId, string needId) { BeeId = beeId; NeedId = needId; } }
    public readonly struct BeeNeedsUpdated : IGameplayEvent, IBeeEvent { public string BeeId { get; } public BeeNeedsUpdated(string beeId) { BeeId = beeId; } }
}
