using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Buildings
{
    public enum ConstructionPriorityLevel { Deferred, Low, Normal, High, Critical, Emergency }

    public sealed class ConstructionPriorityDefinition
    {
        public string DefinitionId { get; }
        public ConstructionPriorityLevel BasePriority { get; }
        public IReadOnlyList<PriorityRule> Rules { get; }

        public ConstructionPriorityDefinition(string definitionId, ConstructionPriorityLevel basePriority, IReadOnlyList<PriorityRule> rules = null)
        {
            DefinitionId = string.IsNullOrWhiteSpace(definitionId) ? throw new ArgumentException("Definition id is required.", nameof(definitionId)) : definitionId;
            BasePriority = basePriority;
            Rules = rules ?? Array.Empty<PriorityRule>();
        }
    }

    public sealed class PriorityContext
    {
        public string ConstructionId { get; }
        public double ColonyEmergency { get; }
        public double ResourceAvailability { get; }
        public double BuilderAvailability { get; }
        public double DependencyReadiness { get; }
        public double Risk { get; }
        public double LiveOpsModifier { get; }

        public PriorityContext(string constructionId, double colonyEmergency = 0d, double resourceAvailability = 1d, double builderAvailability = 1d, double dependencyReadiness = 1d, double risk = 0d, double liveOpsModifier = 0d)
        {
            ConstructionId = constructionId ?? string.Empty;
            ColonyEmergency = colonyEmergency;
            ResourceAvailability = resourceAvailability;
            BuilderAvailability = builderAvailability;
            DependencyReadiness = dependencyReadiness;
            Risk = risk;
            LiveOpsModifier = liveOpsModifier;
        }
    }

    public sealed class PriorityRule
    {
        public string RuleId { get; }
        public double Weight { get; }
        public Func<PriorityContext, double> Evaluator { get; }

        public PriorityRule(string ruleId, double weight, Func<PriorityContext, double> evaluator)
        {
            RuleId = string.IsNullOrWhiteSpace(ruleId) ? throw new ArgumentException("Rule id is required.", nameof(ruleId)) : ruleId;
            Weight = weight;
            Evaluator = evaluator ?? (_ => 0d);
        }

        public double Evaluate(PriorityContext context) => Evaluator(context) * Weight;
    }

    public readonly struct PriorityResult
    {
        public string ConstructionId { get; }
        public double Score { get; }
        public ConstructionPriorityLevel Level { get; }

        public PriorityResult(string constructionId, double score, ConstructionPriorityLevel level)
        {
            ConstructionId = constructionId;
            Score = score;
            Level = level;
        }
    }

    public sealed class PriorityResolver
    {
        public PriorityResult Resolve(ConstructionPriorityDefinition definition, PriorityContext context, ConstructionPriorityLevel? overrideLevel)
        {
            double score = BaseScore(overrideLevel ?? definition.BasePriority);
            for (int i = 0; i < definition.Rules.Count; i++)
            {
                score += definition.Rules[i].Evaluate(context);
            }

            ConstructionPriorityLevel level = ScoreToLevel(score);
            return new PriorityResult(context.ConstructionId, score, level);
        }

        private static double BaseScore(ConstructionPriorityLevel level) => (int)level * 1000d;

        private static ConstructionPriorityLevel ScoreToLevel(double score)
        {
            if (score >= 5000d) return ConstructionPriorityLevel.Emergency;
            if (score >= 4000d) return ConstructionPriorityLevel.Critical;
            if (score >= 3000d) return ConstructionPriorityLevel.High;
            if (score >= 2000d) return ConstructionPriorityLevel.Normal;
            if (score >= 1000d) return ConstructionPriorityLevel.Low;
            return ConstructionPriorityLevel.Deferred;
        }
    }

    public sealed class PriorityDiagnostics
    {
        public int Calculated { get; private set; }
        public int Changed { get; private set; }
        public int Promoted { get; private set; }
        public int Demoted { get; private set; }
        public int Emergencies { get; private set; }
        public void RecordCalculated(PriorityResult result) { Calculated++; if (result.Level == ConstructionPriorityLevel.Emergency) Emergencies++; }
        public void RecordChanged() => Changed++;
        public void RecordPromoted() => Promoted++;
        public void RecordDemoted() => Demoted++;
    }

    public sealed class ConstructionPriorityEngine
    {
        private readonly Dictionary<string, ConstructionPriorityDefinition> definitions = new Dictionary<string, ConstructionPriorityDefinition>();
        private readonly Dictionary<string, ConstructionPriorityLevel> overrides = new Dictionary<string, ConstructionPriorityLevel>();
        private readonly PriorityResolver resolver = new PriorityResolver();

        public bool RegisterDefinition(ConstructionPriorityDefinition definition)
        {
            if (definition == null || definitions.ContainsKey(definition.DefinitionId)) return false;
            definitions.Add(definition.DefinitionId, definition);
            return true;
        }

        public PriorityResult EvaluatePriority(string definitionId, PriorityContext context)
        {
            if (!definitions.TryGetValue(definitionId, out ConstructionPriorityDefinition definition))
            {
                definition = new ConstructionPriorityDefinition(definitionId, ConstructionPriorityLevel.Normal);
            }
            overrides.TryGetValue(context.ConstructionId, out ConstructionPriorityLevel overrideLevel);
            return resolver.Resolve(definition, context, overrides.ContainsKey(context.ConstructionId) ? overrideLevel : null);
        }

        public void OverridePriority(string constructionId, ConstructionPriorityLevel level) => overrides[constructionId] = level;
        public void ClearOverride(string constructionId) => overrides.Remove(constructionId);
    }

    public sealed class ConstructionPriorityManager
    {
        private readonly ConstructionPriorityEngine engine = new ConstructionPriorityEngine();
        private readonly Dictionary<string, PriorityResult> results = new Dictionary<string, PriorityResult>();
        private readonly IEventBus eventBus;

        public PriorityDiagnostics Diagnostics { get; } = new PriorityDiagnostics();

        public ConstructionPriorityManager(IEventBus eventBus = null)
        {
            this.eventBus = eventBus;
        }

        public bool RegisterDefinition(ConstructionPriorityDefinition definition) => engine.RegisterDefinition(definition);

        public PriorityResult EvaluatePriority(string definitionId, PriorityContext context)
        {
            PriorityResult result = engine.EvaluatePriority(definitionId, context);
            if (results.TryGetValue(context.ConstructionId, out PriorityResult previous) && previous.Level != result.Level)
            {
                Diagnostics.RecordChanged();
                eventBus?.Publish(new PriorityChanged(context.ConstructionId, result.Level));
            }

            results[context.ConstructionId] = result;
            Diagnostics.RecordCalculated(result);
            eventBus?.Publish(new PriorityCalculated(context.ConstructionId, result.Score));
            if (result.Level == ConstructionPriorityLevel.Emergency) eventBus?.Publish(new EmergencyPriorityActivated(context.ConstructionId));
            return result;
        }

        public IReadOnlyList<PriorityResult> RecalculatePriorities(string definitionId, IReadOnlyList<PriorityContext> contexts)
        {
            List<PriorityResult> recalculated = new List<PriorityResult>();
            for (int i = 0; i < contexts.Count; i++)
            {
                recalculated.Add(EvaluatePriority(definitionId, contexts[i]));
            }
            recalculated.Sort((left, right) =>
            {
                int scoreCompare = right.Score.CompareTo(left.Score);
                return scoreCompare != 0 ? scoreCompare : string.CompareOrdinal(left.ConstructionId, right.ConstructionId);
            });
            return recalculated;
        }

        public void PromoteConstruction(string constructionId)
        {
            PriorityResult current = results.TryGetValue(constructionId, out PriorityResult result) ? result : new PriorityResult(constructionId, 2000d, ConstructionPriorityLevel.Normal);
            OverridePriority(constructionId, (ConstructionPriorityLevel)Math.Min((int)ConstructionPriorityLevel.Emergency, (int)current.Level + 1));
            Diagnostics.RecordPromoted();
            eventBus?.Publish(new ConstructionPromoted(constructionId));
        }

        public void DemoteConstruction(string constructionId)
        {
            PriorityResult current = results.TryGetValue(constructionId, out PriorityResult result) ? result : new PriorityResult(constructionId, 2000d, ConstructionPriorityLevel.Normal);
            OverridePriority(constructionId, (ConstructionPriorityLevel)Math.Max((int)ConstructionPriorityLevel.Deferred, (int)current.Level - 1));
            Diagnostics.RecordDemoted();
            eventBus?.Publish(new ConstructionDemoted(constructionId));
        }

        public void OverridePriority(string constructionId, ConstructionPriorityLevel level) => engine.OverridePriority(constructionId, level);
        public void ClearOverride(string constructionId) => engine.ClearOverride(constructionId);

        public IReadOnlyList<PriorityResult> QueryPriorities()
        {
            List<PriorityResult> list = new List<PriorityResult>(results.Values);
            list.Sort((left, right) => string.CompareOrdinal(left.ConstructionId, right.ConstructionId));
            return list;
        }
    }

    public readonly struct PriorityCalculated : IGameplayEvent, IBuildingEvent { public string ConstructionId { get; } public double Score { get; } public PriorityCalculated(string constructionId, double score) { ConstructionId = constructionId; Score = score; } }
    public readonly struct PriorityChanged : IGameplayEvent, IBuildingEvent { public string ConstructionId { get; } public ConstructionPriorityLevel Level { get; } public PriorityChanged(string constructionId, ConstructionPriorityLevel level) { ConstructionId = constructionId; Level = level; } }
    public readonly struct ConstructionPromoted : IGameplayEvent, IBuildingEvent { public string ConstructionId { get; } public ConstructionPromoted(string constructionId) { ConstructionId = constructionId; } }
    public readonly struct ConstructionDemoted : IGameplayEvent, IBuildingEvent { public string ConstructionId { get; } public ConstructionDemoted(string constructionId) { ConstructionId = constructionId; } }
    public readonly struct EmergencyPriorityActivated : IGameplayEvent, IBuildingEvent { public string ConstructionId { get; } public EmergencyPriorityActivated(string constructionId) { ConstructionId = constructionId; } }
}
