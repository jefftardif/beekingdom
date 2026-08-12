using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Buildings
{
    public enum DependencyEntityType { Building, Chamber, Technology, Population, Resource, Queen, Colony, Category, WorldState, Event }
    public enum DependencyType { BuildingRequired, ChamberRequired, TechnologyRequired, PopulationRequired, ResourceRequired, QueenLevelRequired, ColonyLevelRequired, CategoryRequired, WorldStateRequired, EventRequired }
    public enum DependencyUnlockState { Locked, Unlocked }

    public sealed class DependencyNode
    {
        public string NodeId { get; }
        public DependencyEntityType EntityType { get; }
        public string DefinitionId { get; }
        public string CurrentState { get; private set; }
        public DependencyUnlockState UnlockState { get; private set; }

        public DependencyNode(string nodeId, DependencyEntityType entityType, string definitionId, string currentState = "")
        {
            NodeId = string.IsNullOrWhiteSpace(nodeId) ? throw new ArgumentException("Node id is required.", nameof(nodeId)) : nodeId;
            EntityType = entityType;
            DefinitionId = definitionId ?? string.Empty;
            CurrentState = currentState ?? string.Empty;
            UnlockState = DependencyUnlockState.Locked;
        }

        public void SetUnlocked(bool unlocked) => UnlockState = unlocked ? DependencyUnlockState.Unlocked : DependencyUnlockState.Locked;
    }

    public sealed class DependencyEdge
    {
        public string Source { get; }
        public string Destination { get; }
        public DependencyType DependencyType { get; }
        public int Priority { get; }
        public bool Optional { get; }
        public string ValidationRule { get; }

        public DependencyEdge(string source, string destination, DependencyType dependencyType, int priority = 0, bool optional = false, string validationRule = "")
        {
            Source = source ?? string.Empty;
            Destination = destination ?? string.Empty;
            DependencyType = dependencyType;
            Priority = priority;
            Optional = optional;
            ValidationRule = validationRule ?? string.Empty;
        }
    }

    public sealed class BuildingDependencyGraphModel
    {
        private readonly Dictionary<string, DependencyNode> nodes = new Dictionary<string, DependencyNode>();
        private readonly List<DependencyEdge> edges = new List<DependencyEdge>();

        public IReadOnlyDictionary<string, DependencyNode> Nodes => nodes;
        public IReadOnlyList<DependencyEdge> Edges => edges;

        public void BuildGraph(IReadOnlyList<DependencyNode> sourceNodes, IReadOnlyList<DependencyEdge> sourceEdges)
        {
            nodes.Clear();
            edges.Clear();
            for (int i = 0; i < sourceNodes.Count; i++) nodes[sourceNodes[i].NodeId] = sourceNodes[i];
            for (int i = 0; i < sourceEdges.Count; i++) edges.Add(sourceEdges[i]);
            edges.Sort((left, right) =>
            {
                int destinationCompare = string.CompareOrdinal(left.Destination, right.Destination);
                return destinationCompare != 0 ? destinationCompare : left.Priority.CompareTo(right.Priority);
            });
        }

        public IReadOnlyList<DependencyEdge> Incoming(string nodeId)
        {
            List<DependencyEdge> result = new List<DependencyEdge>();
            for (int i = 0; i < edges.Count; i++) if (edges[i].Destination == nodeId) result.Add(edges[i]);
            return result;
        }
    }

    public sealed class DependencyResolver
    {
        public bool ValidateDependencies(BuildingDependencyGraphModel graph, string nodeId, HashSet<string> satisfied)
        {
            IReadOnlyList<DependencyEdge> incoming = graph.Incoming(nodeId);
            for (int i = 0; i < incoming.Count; i++)
            {
                if (!incoming[i].Optional && !satisfied.Contains(incoming[i].Source)) return false;
            }
            return true;
        }

        public IReadOnlyList<string> GetMissingDependencies(BuildingDependencyGraphModel graph, string nodeId, HashSet<string> satisfied)
        {
            List<string> missing = new List<string>();
            IReadOnlyList<DependencyEdge> incoming = graph.Incoming(nodeId);
            for (int i = 0; i < incoming.Count; i++)
            {
                if (!incoming[i].Optional && !satisfied.Contains(incoming[i].Source)) missing.Add(incoming[i].Source);
            }
            missing.Sort(StringComparer.Ordinal);
            return missing;
        }

        public bool DetectCycles(BuildingDependencyGraphModel graph)
        {
            HashSet<string> visiting = new HashSet<string>();
            HashSet<string> visited = new HashSet<string>();
            foreach (string node in graph.Nodes.Keys)
            {
                if (Visit(node, graph, visiting, visited)) return true;
            }
            return false;
        }

        private static bool Visit(string node, BuildingDependencyGraphModel graph, HashSet<string> visiting, HashSet<string> visited)
        {
            if (visited.Contains(node)) return false;
            if (!visiting.Add(node)) return true;
            for (int i = 0; i < graph.Edges.Count; i++)
            {
                if (graph.Edges[i].Source == node && Visit(graph.Edges[i].Destination, graph, visiting, visited)) return true;
            }
            visiting.Remove(node);
            visited.Add(node);
            return false;
        }
    }

    public sealed class DependencyDiagnostics
    {
        public int GraphBuilds { get; private set; }
        public int Validations { get; private set; }
        public int Unlocks { get; private set; }
        public int Locks { get; private set; }
        public int CyclesDetected { get; private set; }
        public void RecordBuild() => GraphBuilds++;
        public void RecordValidation() => Validations++;
        public void RecordUnlock() => Unlocks++;
        public void RecordLock() => Locks++;
        public void RecordCycle() => CyclesDetected++;
    }

    public sealed class BuildingDependencyManager
    {
        private readonly BuildingDependencyGraphModel graph = new BuildingDependencyGraphModel();
        private readonly DependencyResolver resolver = new DependencyResolver();
        private readonly HashSet<string> satisfied = new HashSet<string>();
        private readonly IEventBus eventBus;

        public DependencyDiagnostics Diagnostics { get; } = new DependencyDiagnostics();

        public BuildingDependencyManager(IEventBus eventBus = null) { this.eventBus = eventBus; }

        public void BuildGraph(IReadOnlyList<DependencyNode> nodes, IReadOnlyList<DependencyEdge> edges)
        {
            graph.BuildGraph(nodes ?? Array.Empty<DependencyNode>(), edges ?? Array.Empty<DependencyEdge>());
            Diagnostics.RecordBuild();
            eventBus?.Publish(new DependencyGraphUpdated(graph.Nodes.Count));
        }

        public void MarkSatisfied(string nodeId)
        {
            satisfied.Add(nodeId);
            eventBus?.Publish(new DependencySatisfied(nodeId));
            RecalculateGraph();
        }

        public bool ValidateDependencies(string nodeId)
        {
            Diagnostics.RecordValidation();
            bool valid = resolver.ValidateDependencies(graph, nodeId, satisfied);
            if (!valid) eventBus?.Publish(new DependencyBroken(nodeId));
            return valid;
        }

        public IReadOnlyList<string> GetMissingDependencies(string nodeId) => resolver.GetMissingDependencies(graph, nodeId, satisfied);

        public IReadOnlyList<string> GetUnlockedBuildings()
        {
            List<string> result = new List<string>();
            foreach (DependencyNode node in graph.Nodes.Values)
            {
                if (node.UnlockState == DependencyUnlockState.Unlocked) result.Add(node.NodeId);
            }
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        public IReadOnlyList<string> GetLockedBuildings()
        {
            List<string> result = new List<string>();
            foreach (DependencyNode node in graph.Nodes.Values)
            {
                if (node.UnlockState == DependencyUnlockState.Locked) result.Add(node.NodeId);
            }
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        public void RecalculateGraph()
        {
            foreach (DependencyNode node in graph.Nodes.Values)
            {
                bool unlocked = ValidateDependencies(node.NodeId);
                bool changed = node.UnlockState == DependencyUnlockState.Unlocked != unlocked;
                node.SetUnlocked(unlocked);
                if (!changed) continue;
                if (unlocked) { Diagnostics.RecordUnlock(); eventBus?.Publish(new DependencyUnlocked(node.NodeId)); }
                else { Diagnostics.RecordLock(); eventBus?.Publish(new DependencyLocked(node.NodeId)); }
            }
            eventBus?.Publish(new DependencyGraphUpdated(graph.Nodes.Count));
        }

        public bool DetectCycles()
        {
            bool cycles = resolver.DetectCycles(graph);
            if (cycles) Diagnostics.RecordCycle();
            return cycles;
        }
    }

    public readonly struct DependencyUnlocked : IGameplayEvent, IBuildingEvent { public string NodeId { get; } public DependencyUnlocked(string nodeId) { NodeId = nodeId; } }
    public readonly struct DependencyLocked : IGameplayEvent, IBuildingEvent { public string NodeId { get; } public DependencyLocked(string nodeId) { NodeId = nodeId; } }
    public readonly struct DependencySatisfied : IGameplayEvent, IBuildingEvent { public string NodeId { get; } public DependencySatisfied(string nodeId) { NodeId = nodeId; } }
    public readonly struct DependencyBroken : IGameplayEvent, IBuildingEvent { public string NodeId { get; } public DependencyBroken(string nodeId) { NodeId = nodeId; } }
    public readonly struct DependencyGraphUpdated : IGameplayEvent, IBuildingEvent { public int NodeCount { get; } public DependencyGraphUpdated(int nodeCount) { NodeCount = nodeCount; } }
}
