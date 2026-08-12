using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Chambers
{
    public enum StructuralSupportType { NaturalSoil, WaxStructure, ReinforcedWax, RoyalStructure, RockSupport, ArtificialSupport }
    public enum StructuralIntegrityState { Stable, Optimal, Warning, Critical, FailureRisk }

    public sealed class StructuralNode
    {
        public string NodeId { get; }
        public StructuralSupportType SupportType { get; }
        public double Load { get; }
        public double Support { get; }
        public int Depth { get; }

        public StructuralNode(string nodeId, StructuralSupportType supportType, double load, double support, int depth)
        {
            NodeId = string.IsNullOrWhiteSpace(nodeId) ? throw new ArgumentException("Node id is required.", nameof(nodeId)) : nodeId;
            SupportType = supportType;
            Load = load < 0d ? 0d : load;
            Support = support <= 0d ? 1d : support;
            Depth = depth < 0 ? 0 : depth;
        }
    }

    public sealed class StructuralSupportGraph
    {
        private readonly Dictionary<string, StructuralNode> nodes = new Dictionary<string, StructuralNode>();
        public IReadOnlyDictionary<string, StructuralNode> Nodes => nodes;
        public void Build(IReadOnlyList<StructuralNode> sourceNodes)
        {
            nodes.Clear();
            for (int i = 0; i < sourceNodes.Count; i++) nodes[sourceNodes[i].NodeId] = sourceNodes[i];
        }
    }

    public readonly struct StructuralIntegrityResult
    {
        public double Score { get; }
        public StructuralIntegrityState State { get; }
        public StructuralIntegrityResult(double score, StructuralIntegrityState state) { Score = score; State = state; }
    }

    public sealed class StructuralAnalyzer
    {
        public StructuralIntegrityResult Analyze(StructuralSupportGraph graph)
        {
            if (graph.Nodes.Count == 0) return new StructuralIntegrityResult(100d, StructuralIntegrityState.Optimal);
            double score = 0d;
            foreach (StructuralNode node in graph.Nodes.Values)
            {
                double local = Math.Max(0d, Math.Min(100d, node.Support / Math.Max(1d, node.Load + node.Depth) * 100d));
                score += local;
            }
            score /= graph.Nodes.Count;
            return new StructuralIntegrityResult(score, ToState(score));
        }

        public IReadOnlyList<string> QueryWeakZones(StructuralSupportGraph graph)
        {
            List<string> weak = new List<string>();
            foreach (StructuralNode node in graph.Nodes.Values)
            {
                if (node.Support < node.Load + node.Depth) weak.Add(node.NodeId);
            }
            weak.Sort(StringComparer.Ordinal);
            return weak;
        }

        private static StructuralIntegrityState ToState(double score)
        {
            if (score >= 95d) return StructuralIntegrityState.Optimal;
            if (score >= 75d) return StructuralIntegrityState.Stable;
            if (score >= 50d) return StructuralIntegrityState.Warning;
            if (score >= 25d) return StructuralIntegrityState.Critical;
            return StructuralIntegrityState.FailureRisk;
        }
    }

    public sealed class StructuralDiagnostics
    {
        public int Analyses { get; private set; }
        public int WeakZones { get; private set; }
        public int Recommendations { get; private set; }
        public int ExpansionValidations { get; private set; }
        public void RecordAnalysis() => Analyses++;
        public void RecordWeakZones(int count) => WeakZones += count;
        public void RecordRecommendations(int count) => Recommendations += count;
        public void RecordExpansionValidation() => ExpansionValidations++;
    }

    public sealed class StructuralIntegrityEngine
    {
        private readonly StructuralSupportGraph graph = new StructuralSupportGraph();
        private readonly StructuralAnalyzer analyzer = new StructuralAnalyzer();

        public void BuildGraph(IReadOnlyList<StructuralNode> nodes) => graph.Build(nodes ?? Array.Empty<StructuralNode>());
        public StructuralIntegrityResult AnalyzeIntegrity() => analyzer.Analyze(graph);
        public double CalculateIntegrityScore() => AnalyzeIntegrity().Score;
        public IReadOnlyList<string> QueryWeakZones() => analyzer.QueryWeakZones(graph);
        public StructuralSupportGraph QuerySupportGraph() => graph;
        public IReadOnlyList<string> RecommendReinforcements() => QueryWeakZones();
        public bool ValidateExpansion(StructuralNode proposedNode) => proposedNode != null && proposedNode.Support >= proposedNode.Load + proposedNode.Depth;
    }

    public sealed class StructuralIntegrityManager
    {
        private readonly StructuralIntegrityEngine engine = new StructuralIntegrityEngine();
        private readonly IEventBus eventBus;

        public StructuralDiagnostics Diagnostics { get; } = new StructuralDiagnostics();
        public StructuralIntegrityManager(IEventBus eventBus = null) { this.eventBus = eventBus; }

        public void BuildGraph(IReadOnlyList<StructuralNode> nodes) => engine.BuildGraph(nodes);

        public StructuralIntegrityResult AnalyzeIntegrity()
        {
            StructuralIntegrityResult result = engine.AnalyzeIntegrity();
            IReadOnlyList<string> weak = engine.QueryWeakZones();
            Diagnostics.RecordAnalysis();
            Diagnostics.RecordWeakZones(weak.Count);
            eventBus?.Publish(new IntegrityUpdated(result.Score));
            if (weak.Count > 0) eventBus?.Publish(new WeakZoneDetected(weak[0]));
            if (result.State >= StructuralIntegrityState.Critical) eventBus?.Publish(new StructuralWarning(result.State));
            return result;
        }

        public double CalculateIntegrityScore() => engine.CalculateIntegrityScore();
        public IReadOnlyList<string> QueryWeakZones() => engine.QueryWeakZones();
        public StructuralSupportGraph QuerySupportGraph() => engine.QuerySupportGraph();

        public IReadOnlyList<string> RecommendReinforcements()
        {
            IReadOnlyList<string> recommendations = engine.RecommendReinforcements();
            Diagnostics.RecordRecommendations(recommendations.Count);
            if (recommendations.Count > 0) eventBus?.Publish(new ReinforcementRecommended(recommendations[0]));
            return recommendations;
        }

        public bool ValidateExpansion(StructuralNode proposedNode)
        {
            bool valid = engine.ValidateExpansion(proposedNode);
            Diagnostics.RecordExpansionValidation();
            if (!valid) eventBus?.Publish(new StructuralFailurePrevented(proposedNode?.NodeId ?? string.Empty));
            return valid;
        }
    }

    public readonly struct IntegrityUpdated : IGameplayEvent, IBuildingEvent { public double Score { get; } public IntegrityUpdated(double score) { Score = score; } }
    public readonly struct WeakZoneDetected : IGameplayEvent, IBuildingEvent { public string NodeId { get; } public WeakZoneDetected(string nodeId) { NodeId = nodeId; } }
    public readonly struct ReinforcementRecommended : IGameplayEvent, IBuildingEvent { public string NodeId { get; } public ReinforcementRecommended(string nodeId) { NodeId = nodeId; } }
    public readonly struct StructuralWarning : IGameplayEvent, IBuildingEvent { public StructuralIntegrityState State { get; } public StructuralWarning(StructuralIntegrityState state) { State = state; } }
    public readonly struct StructuralFailurePrevented : IGameplayEvent, IBuildingEvent { public string NodeId { get; } public StructuralFailurePrevented(string nodeId) { NodeId = nodeId; } }
}
