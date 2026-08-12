using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Chambers
{
    public enum LayoutRecommendationType { NewChamber, NewCorridor, Upgrade, FutureMove, NewSector, ReduceCongestion }

    public sealed class ColonyLayoutDefinition
    {
        public double LogisticsWeight { get; }
        public double ProductionWeight { get; }
        public double PopulationWeight { get; }
        public double ExpansionWeight { get; }
        public double DefenseWeight { get; }
        public double AccessibilityWeight { get; }

        public ColonyLayoutDefinition(double logisticsWeight = 1d, double productionWeight = 1d, double populationWeight = 1d, double expansionWeight = 1d, double defenseWeight = 1d, double accessibilityWeight = 1d)
        {
            LogisticsWeight = Math.Max(0d, logisticsWeight);
            ProductionWeight = Math.Max(0d, productionWeight);
            PopulationWeight = Math.Max(0d, populationWeight);
            ExpansionWeight = Math.Max(0d, expansionWeight);
            DefenseWeight = Math.Max(0d, defenseWeight);
            AccessibilityWeight = Math.Max(0d, accessibilityWeight);
        }
    }

    public readonly struct LayoutScore
    {
        public double LogisticsScore { get; }
        public double ProductionScore { get; }
        public double PopulationScore { get; }
        public double ExpansionScore { get; }
        public double DefenseScore { get; }
        public double AccessibilityScore { get; }
        public double OverallScore { get; }

        public LayoutScore(double logisticsScore, double productionScore, double populationScore, double expansionScore, double defenseScore, double accessibilityScore, double overallScore)
        {
            LogisticsScore = logisticsScore;
            ProductionScore = productionScore;
            PopulationScore = populationScore;
            ExpansionScore = expansionScore;
            DefenseScore = defenseScore;
            AccessibilityScore = accessibilityScore;
            OverallScore = overallScore;
        }
    }

    public sealed class LayoutRecommendation
    {
        public LayoutRecommendationType Type { get; }
        public string TargetId { get; }
        public string Reason { get; }

        public LayoutRecommendation(LayoutRecommendationType type, string targetId, string reason)
        {
            Type = type;
            TargetId = targetId ?? string.Empty;
            Reason = reason ?? string.Empty;
        }
    }

    public sealed class LayoutSector
    {
        public string SectorId { get; }
        public IReadOnlyList<string> ChamberIds { get; }

        public LayoutSector(string sectorId, IReadOnlyList<string> chamberIds)
        {
            SectorId = sectorId ?? string.Empty;
            ChamberIds = chamberIds ?? Array.Empty<string>();
        }
    }

    public sealed class ColonyLayoutSnapshot
    {
        public int Version { get; }
        public LayoutScore Score { get; }
        public IReadOnlyList<LayoutRecommendation> Recommendations { get; }
        public IReadOnlyList<LayoutSector> Sectors { get; }

        public ColonyLayoutSnapshot(int version, LayoutScore score, IReadOnlyList<LayoutRecommendation> recommendations, IReadOnlyList<LayoutSector> sectors)
        {
            Version = version;
            Score = score;
            Recommendations = recommendations ?? Array.Empty<LayoutRecommendation>();
            Sectors = sectors ?? Array.Empty<LayoutSector>();
        }
    }

    public sealed class LayoutScoreCalculator
    {
        public LayoutScore CalculateLayoutScore(ColonyLayoutDefinition definition, int chamberCount, int corridorCount, int congestedCorridors, int sectorCount)
        {
            double accessibility = corridorCount <= 0 && chamberCount > 1 ? 0d : 100d;
            double congestionPenalty = congestedCorridors * 15d;
            double logistics = Clamp(100d - congestionPenalty);
            double production = Clamp(chamberCount * 10d);
            double population = Clamp(chamberCount * 8d);
            double expansion = Clamp(100d - chamberCount * 2d);
            double defense = Clamp(sectorCount * 12d);
            double weighted = logistics * definition.LogisticsWeight + production * definition.ProductionWeight + population * definition.PopulationWeight + expansion * definition.ExpansionWeight + defense * definition.DefenseWeight + accessibility * definition.AccessibilityWeight;
            double weights = definition.LogisticsWeight + definition.ProductionWeight + definition.PopulationWeight + definition.ExpansionWeight + definition.DefenseWeight + definition.AccessibilityWeight;
            return new LayoutScore(logistics, production, population, expansion, defense, accessibility, weights <= 0d ? 0d : weighted / weights);
        }

        private static double Clamp(double value) => value < 0d ? 0d : value > 100d ? 100d : value;
    }

    public sealed class ColonyLayoutAnalyzer
    {
        public IReadOnlyList<LayoutRecommendation> AnalyzeRecommendations(int chamberCount, int corridorCount, int congestedCorridors)
        {
            List<LayoutRecommendation> recommendations = new List<LayoutRecommendation>();
            if (chamberCount > 1 && corridorCount == 0) recommendations.Add(new LayoutRecommendation(LayoutRecommendationType.NewCorridor, "corridor", "No corridor between chambers."));
            if (congestedCorridors > 0) recommendations.Add(new LayoutRecommendation(LayoutRecommendationType.ReduceCongestion, "corridor", "Congested corridor detected."));
            if (chamberCount < 3) recommendations.Add(new LayoutRecommendation(LayoutRecommendationType.NewChamber, "expansion", "Colony has few chambers."));
            return recommendations;
        }
    }

    public sealed class ColonyLayoutDiagnostics
    {
        public int Analyses { get; private set; }
        public int ScoreChanges { get; private set; }
        public int Bottlenecks { get; private set; }
        public int Recommendations { get; private set; }
        public int Sectors { get; private set; }
        public void RecordAnalysis() => Analyses++;
        public void RecordScoreChange() => ScoreChanges++;
        public void RecordBottlenecks(int count) => Bottlenecks += count;
        public void RecordRecommendations(int count) => Recommendations += count;
        public void RecordSectors(int count) => Sectors = count;
    }

    public sealed class ColonyLayoutManager
    {
        private const int SnapshotVersion = 1;
        private readonly ColonyLayoutDefinition definition;
        private readonly LayoutScoreCalculator scoreCalculator = new LayoutScoreCalculator();
        private readonly ColonyLayoutAnalyzer analyzer = new ColonyLayoutAnalyzer();
        private readonly Dictionary<string, LayoutSector> sectors = new Dictionary<string, LayoutSector>();
        private readonly IEventBus eventBus;
        private LayoutScore lastScore;
        private IReadOnlyList<LayoutRecommendation> lastRecommendations = Array.Empty<LayoutRecommendation>();

        public ColonyLayoutDiagnostics Diagnostics { get; } = new ColonyLayoutDiagnostics();

        public ColonyLayoutManager(ColonyLayoutDefinition definition = null, IEventBus eventBus = null)
        {
            this.definition = definition ?? new ColonyLayoutDefinition();
            this.eventBus = eventBus;
        }

        public ColonyLayoutSnapshot AnalyzeLayout(int chamberCount, int corridorCount, int congestedCorridors)
        {
            LayoutScore score = CalculateLayoutScore(chamberCount, corridorCount, congestedCorridors);
            lastRecommendations = analyzer.AnalyzeRecommendations(chamberCount, corridorCount, congestedCorridors);
            Diagnostics.RecordAnalysis();
            Diagnostics.RecordRecommendations(lastRecommendations.Count);
            if (!score.OverallScore.Equals(lastScore.OverallScore))
            {
                Diagnostics.RecordScoreChange();
                eventBus?.Publish(new LayoutScoreChanged(score.OverallScore));
            }
            if (congestedCorridors > 0)
            {
                Diagnostics.RecordBottlenecks(congestedCorridors);
                eventBus?.Publish(new BottleneckDetected(congestedCorridors));
            }
            lastScore = score;
            eventBus?.Publish(new LayoutAnalyzed(score.OverallScore));
            return GenerateLayoutSnapshot();
        }

        public LayoutScore CalculateLayoutScore(int chamberCount, int corridorCount, int congestedCorridors)
        {
            return scoreCalculator.CalculateLayoutScore(definition, chamberCount, corridorCount, congestedCorridors, sectors.Count);
        }

        public IReadOnlyList<LayoutRecommendation> QueryRecommendations() => lastRecommendations;

        public LayoutSector QuerySector(string sectorId)
        {
            sectors.TryGetValue(sectorId, out LayoutSector sector);
            return sector;
        }

        public IReadOnlyList<string> DetectBottlenecks(IReadOnlyList<CorridorInstance> corridors)
        {
            List<string> result = new List<string>();
            for (int i = 0; i < corridors.Count; i++)
            {
                if (corridors[i].State == CorridorState.Congested) result.Add(corridors[i].EntityId);
            }
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        public void CreateSector(string sectorId, IReadOnlyList<string> chamberIds)
        {
            sectors[sectorId] = new LayoutSector(sectorId, chamberIds);
            Diagnostics.RecordSectors(sectors.Count);
            eventBus?.Publish(new SectorCreated(sectorId));
        }

        public ColonyLayoutSnapshot GenerateLayoutSnapshot()
        {
            List<LayoutSector> sectorList = new List<LayoutSector>(sectors.Values);
            sectorList.Sort((left, right) => string.CompareOrdinal(left.SectorId, right.SectorId));
            return new ColonyLayoutSnapshot(SnapshotVersion, lastScore, lastRecommendations, sectorList);
        }
    }

    public readonly struct LayoutAnalyzed : IGameplayEvent, IBuildingEvent { public double Score { get; } public LayoutAnalyzed(double score) { Score = score; } }
    public readonly struct LayoutScoreChanged : IGameplayEvent, IBuildingEvent { public double Score { get; } public LayoutScoreChanged(double score) { Score = score; } }
    public readonly struct BottleneckDetected : IGameplayEvent, IBuildingEvent { public int Count { get; } public BottleneckDetected(int count) { Count = count; } }
    public readonly struct RecommendationGenerated : IGameplayEvent, IBuildingEvent { public string TargetId { get; } public RecommendationGenerated(string targetId) { TargetId = targetId; } }
    public readonly struct SectorCreated : IGameplayEvent, IBuildingEvent { public string SectorId { get; } public SectorCreated(string sectorId) { SectorId = sectorId; } }
}
