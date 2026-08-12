using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Population
{
    public sealed class CasteDefinition
    {
        public string DefinitionId { get; }
        public BeeCaste Caste { get; }
        public double TargetRatio { get; }
        public int MinimumPopulation { get; }

        public CasteDefinition(string definitionId, BeeCaste caste, double targetRatio, int minimumPopulation)
        {
            DefinitionId = string.IsNullOrWhiteSpace(definitionId) ? throw new ArgumentException("Definition id is required.", nameof(definitionId)) : definitionId;
            Caste = caste;
            TargetRatio = Clamp01(targetRatio);
            MinimumPopulation = Math.Max(0, minimumPopulation);
        }

        private static double Clamp01(double value) => value < 0d ? 0d : value > 1d ? 1d : value;
    }

    public sealed class CasteAssignmentRule
    {
        public string RuleId { get; }
        public BeeCaste TargetCaste { get; }
        public string GeneticTraitId { get; }
        public double GeneticWeight { get; }
        public double AgeWeight { get; }
        public double HealthWeight { get; }
        public double ColonyNeedWeight { get; }
        public double StrategyWeight { get; }

        public CasteAssignmentRule(string ruleId, BeeCaste targetCaste, string geneticTraitId, double geneticWeight, double ageWeight, double healthWeight, double colonyNeedWeight, double strategyWeight)
        {
            RuleId = string.IsNullOrWhiteSpace(ruleId) ? throw new ArgumentException("Rule id is required.", nameof(ruleId)) : ruleId;
            TargetCaste = targetCaste;
            GeneticTraitId = geneticTraitId ?? string.Empty;
            GeneticWeight = Clamp01(geneticWeight);
            AgeWeight = Clamp01(ageWeight);
            HealthWeight = Clamp01(healthWeight);
            ColonyNeedWeight = Clamp01(colonyNeedWeight);
            StrategyWeight = Clamp01(strategyWeight);
        }

        private static double Clamp01(double value) => value < 0d ? 0d : value > 1d ? 1d : value;
    }

    public readonly struct CasteAssignmentContext
    {
        public string BeeId { get; }
        public string GenomeId { get; }
        public double AgeDays { get; }
        public double HealthFactor { get; }
        public double SeasonFactor { get; }
        public double ResourceFactor { get; }
        public double StrategyFactor { get; }

        public CasteAssignmentContext(string beeId, string genomeId, double ageDays, double healthFactor = 1d, double seasonFactor = 1d, double resourceFactor = 1d, double strategyFactor = 1d)
        {
            BeeId = beeId ?? string.Empty;
            GenomeId = genomeId ?? string.Empty;
            AgeDays = Math.Max(0d, ageDays);
            HealthFactor = Clamp01(healthFactor);
            SeasonFactor = Clamp01(seasonFactor);
            ResourceFactor = Clamp01(resourceFactor);
            StrategyFactor = Clamp01(strategyFactor);
        }

        private static double Clamp01(double value) => value < 0d ? 0d : value > 1d ? 1d : value;
    }

    public sealed class PopulationBalance
    {
        public bool IsBalanced { get; }
        public BeeCaste MostNeededCaste { get; }
        public BeeCaste MostExcessiveCaste { get; }
        public double BalanceScore { get; }

        public PopulationBalance(bool isBalanced, BeeCaste mostNeededCaste, BeeCaste mostExcessiveCaste, double balanceScore)
        {
            IsBalanced = isBalanced;
            MostNeededCaste = mostNeededCaste;
            MostExcessiveCaste = mostExcessiveCaste;
            BalanceScore = balanceScore;
        }
    }

    public sealed class CasteStatistics
    {
        public int Assignments { get; }
        public int Reassignments { get; }
        public IReadOnlyDictionary<BeeCaste, int> PopulationByCaste { get; }
        public PopulationBalance Balance { get; }

        public CasteStatistics(int assignments, int reassignments, IReadOnlyDictionary<BeeCaste, int> populationByCaste, PopulationBalance balance)
        {
            Assignments = assignments;
            Reassignments = reassignments;
            PopulationByCaste = populationByCaste;
            Balance = balance;
        }
    }

    public sealed class CasteDiagnostics
    {
        public int DefinitionsRegistered { get; private set; }
        public int RulesRegistered { get; private set; }
        public int Assignments { get; private set; }
        public int Reassignments { get; private set; }
        public int Imbalances { get; private set; }

        public void RecordDefinitions(int count) => DefinitionsRegistered = count;
        public void RecordRules(int count) => RulesRegistered = count;
        public void RecordAssignment() => Assignments++;
        public void RecordReassignment() => Reassignments++;
        public void RecordImbalance() => Imbalances++;
    }

    public sealed class CasteAssignmentEngine
    {
        public BeeCaste SelectCaste(IReadOnlyList<CasteAssignmentRule> rules, IReadOnlyDictionary<string, double> traits, PopulationBalance balance, CasteAssignmentContext context)
        {
            double bestScore = double.MinValue;
            BeeCaste selected = rules.Count == 0 ? BeeCaste.Worker : rules[0].TargetCaste;
            for (int i = 0; i < rules.Count; i++)
            {
                CasteAssignmentRule rule = rules[i];
                traits.TryGetValue(rule.GeneticTraitId, out double genetic);
                double need = balance.MostNeededCaste == rule.TargetCaste ? 1d : 0d;
                double age = Math.Min(1d, context.AgeDays / 30d);
                double score =
                    genetic * rule.GeneticWeight +
                    age * rule.AgeWeight +
                    context.HealthFactor * rule.HealthWeight +
                    need * rule.ColonyNeedWeight +
                    context.StrategyFactor * context.SeasonFactor * context.ResourceFactor * rule.StrategyWeight;
                if (score > bestScore)
                {
                    bestScore = score;
                    selected = rule.TargetCaste;
                }
            }
            return selected;
        }

        public PopulationBalance EvaluatePopulationNeeds(IReadOnlyList<CasteDefinition> definitions, IReadOnlyDictionary<BeeCaste, int> counts, int totalPopulation)
        {
            double largestDeficit = double.MinValue;
            double largestExcess = double.MinValue;
            BeeCaste needed = BeeCaste.Worker;
            BeeCaste excessive = BeeCaste.Worker;
            double totalDeviation = 0d;

            for (int i = 0; i < definitions.Count; i++)
            {
                CasteDefinition definition = definitions[i];
                counts.TryGetValue(definition.Caste, out int current);
                double target = Math.Max(definition.MinimumPopulation, totalPopulation * definition.TargetRatio);
                double deficit = target - current;
                double excess = current - target;
                totalDeviation += Math.Abs(deficit);
                if (deficit > largestDeficit) { largestDeficit = deficit; needed = definition.Caste; }
                if (excess > largestExcess) { largestExcess = excess; excessive = definition.Caste; }
            }

            double score = totalPopulation == 0 ? 1d : Math.Max(0d, 1d - totalDeviation / Math.Max(1d, totalPopulation));
            return new PopulationBalance(score >= 0.8d, needed, excessive, score);
        }
    }

    public sealed class CasteAssignmentManager
    {
        private readonly Dictionary<BeeCaste, CasteDefinition> definitions = new Dictionary<BeeCaste, CasteDefinition>();
        private readonly List<CasteAssignmentRule> rules = new List<CasteAssignmentRule>();
        private readonly CasteAssignmentEngine engine = new CasteAssignmentEngine();
        private readonly PopulationManager populationManager;
        private readonly GeneticsManager geneticsManager;
        private readonly IEventBus eventBus;

        public CasteDiagnostics Diagnostics { get; } = new CasteDiagnostics();

        public CasteAssignmentManager(PopulationManager populationManager, GeneticsManager geneticsManager = null, IEventBus eventBus = null)
        {
            this.populationManager = populationManager ?? throw new ArgumentNullException(nameof(populationManager));
            this.geneticsManager = geneticsManager;
            this.eventBus = eventBus;
        }

        public bool RegisterCasteDefinition(CasteDefinition definition)
        {
            if (definition == null || definitions.ContainsKey(definition.Caste)) return false;
            definitions.Add(definition.Caste, definition);
            Diagnostics.RecordDefinitions(definitions.Count);
            return true;
        }

        public bool RegisterAssignmentRule(CasteAssignmentRule rule)
        {
            if (rule == null) return false;
            rules.Add(rule);
            Diagnostics.RecordRules(rules.Count);
            return true;
        }

        public BeeCaste AssignCaste(CasteAssignmentContext context)
        {
            PopulationBalance balance = EvaluatePopulationNeeds();
            IReadOnlyDictionary<string, double> traits = geneticsManager?.CalculateTraits(context.GenomeId) ?? new Dictionary<string, double>();
            BeeCaste caste = engine.SelectCaste(rules, traits, balance, context);
            populationManager.ChangeBeeCaste(context.BeeId, caste);
            Diagnostics.RecordAssignment();
            eventBus?.Publish(new CasteAssigned(context.BeeId, caste));
            eventBus?.Publish(new AssignmentRuleTriggered(context.BeeId, caste));
            return caste;
        }

        public bool ReassignCaste(string beeId, BeeCaste caste)
        {
            bool changed = populationManager.ChangeBeeCaste(beeId, caste);
            if (!changed) return false;
            Diagnostics.RecordReassignment();
            eventBus?.Publish(new CasteChangedByAssignment(beeId, caste));
            return true;
        }

        public PopulationBalance EvaluatePopulationNeeds()
        {
            PopulationStatistics statistics = populationManager.QueryStatistics();
            List<CasteDefinition> definitionList = new List<CasteDefinition>(definitions.Values);
            definitionList.Sort((left, right) => left.Caste.CompareTo(right.Caste));
            PopulationBalance balance = engine.EvaluatePopulationNeeds(definitionList, statistics.PopulationByCaste, statistics.TotalPopulation);
            if (!balance.IsBalanced)
            {
                Diagnostics.RecordImbalance();
                eventBus?.Publish(new PopulationImbalanceDetected(balance.MostNeededCaste));
            }
            else
            {
                eventBus?.Publish(new PopulationBalanced());
            }
            return balance;
        }

        public PopulationBalance QueryPopulationBalance() => EvaluatePopulationNeeds();

        public CasteStatistics QueryCasteStatistics()
        {
            PopulationStatistics statistics = populationManager.QueryStatistics();
            return new CasteStatistics(Diagnostics.Assignments, Diagnostics.Reassignments, statistics.PopulationByCaste, EvaluatePopulationNeeds());
        }
    }

    public readonly struct CasteAssigned : IGameplayEvent, IBeeEvent { public string BeeId { get; } public BeeCaste Caste { get; } public CasteAssigned(string beeId, BeeCaste caste) { BeeId = beeId; Caste = caste; } }
    public readonly struct CasteChangedByAssignment : IGameplayEvent, IBeeEvent { public string BeeId { get; } public BeeCaste Caste { get; } public CasteChangedByAssignment(string beeId, BeeCaste caste) { BeeId = beeId; Caste = caste; } }
    public readonly struct PopulationImbalanceDetected : IGameplayEvent, IBeeEvent { public BeeCaste NeededCaste { get; } public PopulationImbalanceDetected(BeeCaste neededCaste) { NeededCaste = neededCaste; } }
    public readonly struct PopulationBalanced : IGameplayEvent, IBeeEvent { }
    public readonly struct AssignmentRuleTriggered : IGameplayEvent, IBeeEvent { public string BeeId { get; } public BeeCaste Caste { get; } public AssignmentRuleTriggered(string beeId, BeeCaste caste) { BeeId = beeId; Caste = caste; } }
}
