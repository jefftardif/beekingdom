using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Population
{
    public enum OptimizationDomain { Population, Construction, ResourceFlow, Logistics, Pathfinding, Production, Defense, Health, Fatigue, Storage, ChamberOccupation, Movement }
    public enum OptimizationRecommendationType { NewChamber, MoveBuilding, ReassignCastes, ChangeStrategy, IncreaseProduction, UpgradeBuilding, OpenCorridor, RedistributeResources }

    public sealed class OptimizationRule
    {
        public string RuleId { get; }
        public OptimizationDomain Domain { get; }
        public double Threshold { get; }
        public OptimizationRecommendationType RecommendationType { get; }
        public OptimizationRule(string ruleId, OptimizationDomain domain, double threshold, OptimizationRecommendationType recommendationType)
        {
            RuleId = string.IsNullOrWhiteSpace(ruleId) ? throw new ArgumentException("Rule id is required.", nameof(ruleId)) : ruleId;
            Domain = domain;
            Threshold = threshold < 0d ? 0d : threshold > 1d ? 1d : threshold;
            RecommendationType = recommendationType;
        }
    }

    public sealed class OptimizationScore
    {
        public double LogisticsScore { get; }
        public double ProductionScore { get; }
        public double PopulationScore { get; }
        public double ConstructionScore { get; }
        public double ResourceScore { get; }
        public double DefenseScore { get; }
        public double ColonyEfficiencyScore { get; }
        public OptimizationScore(double logistics, double production, double population, double construction, double resource, double defense)
        {
            LogisticsScore = C(logistics); ProductionScore = C(production); PopulationScore = C(population); ConstructionScore = C(construction); ResourceScore = C(resource); DefenseScore = C(defense);
            ColonyEfficiencyScore = (LogisticsScore + ProductionScore + PopulationScore + ConstructionScore + ResourceScore + DefenseScore) / 6d;
        }
        private static double C(double v) => v < 0d ? 0d : v > 1d ? 1d : v;
    }

    public sealed class OptimizationRecommendation
    {
        public string RecommendationId { get; }
        public OptimizationRecommendationType Type { get; }
        public OptimizationDomain Domain { get; }
        public double Priority { get; }
        public OptimizationRecommendation(string recommendationId, OptimizationRecommendationType type, OptimizationDomain domain, double priority)
        { RecommendationId = recommendationId; Type = type; Domain = domain; Priority = Math.Max(0d, priority); }
    }

    public sealed class OptimizationReport
    {
        public OptimizationScore Score { get; }
        public IReadOnlyList<OptimizationRecommendation> Recommendations { get; }
        public bool RegressionDetected { get; }
        public OptimizationReport(OptimizationScore score, IReadOnlyList<OptimizationRecommendation> recommendations, bool regressionDetected)
        { Score = score; Recommendations = recommendations; RegressionDetected = regressionDetected; }
    }

    public sealed class OptimizationAnalyzer
    {
        public OptimizationScore CalculateScores(double idleTime, double congestion, double resourceWaste, double travelWaste, double populationImbalance, double defenseRisk)
        {
            return new OptimizationScore(1d - congestion, 1d - idleTime, 1d - populationImbalance, 1d - congestion, 1d - resourceWaste, 1d - defenseRisk);
        }
    }

    public sealed class ColonyOptimizationEngine
    {
        private readonly OptimizationAnalyzer analyzer = new OptimizationAnalyzer();
        public OptimizationScore CalculateScores(double idleTime, double congestion, double resourceWaste, double travelWaste, double populationImbalance, double defenseRisk) => analyzer.CalculateScores(idleTime, congestion, resourceWaste, travelWaste, populationImbalance, defenseRisk);
        public IReadOnlyList<OptimizationRecommendation> GenerateRecommendations(IReadOnlyList<OptimizationRule> rules, OptimizationScore score)
        {
            List<OptimizationRecommendation> result = new List<OptimizationRecommendation>();
            for (int i = 0; i < rules.Count; i++)
            {
                double domainScore = DomainScore(rules[i].Domain, score);
                if (domainScore >= rules[i].Threshold) continue;
                result.Add(new OptimizationRecommendation("recommendation-" + (i + 1).ToString("D3"), rules[i].RecommendationType, rules[i].Domain, 1d - domainScore));
            }
            return result;
        }
        private static double DomainScore(OptimizationDomain domain, OptimizationScore score)
        {
            switch (domain)
            {
                case OptimizationDomain.Population: return score.PopulationScore;
                case OptimizationDomain.Construction: return score.ConstructionScore;
                case OptimizationDomain.ResourceFlow:
                case OptimizationDomain.Storage: return score.ResourceScore;
                case OptimizationDomain.Defense: return score.DefenseScore;
                case OptimizationDomain.Production: return score.ProductionScore;
                default: return score.LogisticsScore;
            }
        }
    }

    public sealed class OptimizationDiagnostics { public int Analyses { get; private set; } public int Recommendations { get; private set; } public int Regressions { get; private set; } public void RecordAnalysis() => Analyses++; public void RecordRecommendations(int count) => Recommendations += count; public void RecordRegression() => Regressions++; }

    public sealed class ColonyOptimizationManager
    {
        private readonly List<OptimizationRule> rules = new List<OptimizationRule>();
        private readonly List<OptimizationScore> history = new List<OptimizationScore>();
        private readonly ColonyOptimizationEngine engine = new ColonyOptimizationEngine();
        private readonly IEventBus eventBus;
        private OptimizationReport lastReport;
        public OptimizationDiagnostics Diagnostics { get; } = new OptimizationDiagnostics();
        public ColonyOptimizationManager(IEventBus eventBus = null) { this.eventBus = eventBus; }
        public bool RegisterOptimizationRule(OptimizationRule rule) { if (rule == null) return false; rules.Add(rule); return true; }
        public OptimizationReport AnalyzeColony(double idleTime, double congestion, double resourceWaste, double travelWaste, double populationImbalance, double defenseRisk)
        {
            OptimizationScore score = CalculateScores(idleTime, congestion, resourceWaste, travelWaste, populationImbalance, defenseRisk);
            IReadOnlyList<OptimizationRecommendation> recommendations = GenerateRecommendations(score);
            bool regression = history.Count > 0 && score.ColonyEfficiencyScore < history[history.Count - 1].ColonyEfficiencyScore;
            history.Add(score);
            lastReport = new OptimizationReport(score, recommendations, regression);
            Diagnostics.RecordAnalysis();
            Diagnostics.RecordRecommendations(recommendations.Count);
            eventBus?.Publish(new ColonyAnalyzed(score.ColonyEfficiencyScore));
            for (int i = 0; i < recommendations.Count; i++) eventBus?.Publish(new RecommendationGenerated(recommendations[i].RecommendationId));
            if (regression) { Diagnostics.RecordRegression(); eventBus?.Publish(new PerformanceRegressionDetected(score.ColonyEfficiencyScore)); }
            eventBus?.Publish(new OptimizationScoreChanged(score.ColonyEfficiencyScore));
            return lastReport;
        }
        public IReadOnlyList<OptimizationRecommendation> GenerateRecommendations(OptimizationScore score) => engine.GenerateRecommendations(rules, score);
        public OptimizationScore CalculateScores(double idleTime, double congestion, double resourceWaste, double travelWaste, double populationImbalance, double defenseRisk) => engine.CalculateScores(idleTime, congestion, resourceWaste, travelWaste, populationImbalance, defenseRisk);
        public OptimizationReport QueryOptimizationReport() => lastReport;
        public IReadOnlyList<OptimizationRecommendation> QueryRecommendations() => lastReport?.Recommendations ?? Array.Empty<OptimizationRecommendation>();
        public void MarkOptimized() => eventBus?.Publish(new ColonyOptimized());
    }

    public readonly struct ColonyAnalyzed : IGameplayEvent, IBeeEvent { public double Score { get; } public ColonyAnalyzed(double score) { Score = score; } }
    public readonly struct RecommendationGenerated : IGameplayEvent, IBeeEvent { public string RecommendationId { get; } public RecommendationGenerated(string recommendationId) { RecommendationId = recommendationId; } }
    public readonly struct OptimizationScoreChanged : IGameplayEvent, IBeeEvent { public double Score { get; } public OptimizationScoreChanged(double score) { Score = score; } }
    public readonly struct ColonyOptimized : IGameplayEvent, IBeeEvent { }
    public readonly struct PerformanceRegressionDetected : IGameplayEvent, IBeeEvent { public double Score { get; } public PerformanceRegressionDetected(double score) { Score = score; } }
}
