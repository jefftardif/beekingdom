using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Buildings
{
    public enum SpecializationType { Production, Storage, Logistics, Population, Defense, Royal, Research, Economy, Support, Utility }

    public sealed class SpecializationDefinition
    {
        public string SpecializationId { get; }
        public SpecializationType Type { get; }
        public int RequiredLevel { get; }
        public IReadOnlyList<string> ExclusiveWith { get; }
        public string GameplayEffectId { get; }

        public SpecializationDefinition(string specializationId, SpecializationType type, int requiredLevel = 0, IReadOnlyList<string> exclusiveWith = null, string gameplayEffectId = "")
        {
            SpecializationId = string.IsNullOrWhiteSpace(specializationId) ? throw new ArgumentException("Specialization id is required.", nameof(specializationId)) : specializationId;
            Type = type;
            RequiredLevel = requiredLevel < 0 ? 0 : requiredLevel;
            ExclusiveWith = exclusiveWith ?? Array.Empty<string>();
            GameplayEffectId = gameplayEffectId ?? string.Empty;
        }
    }

    public sealed class SpecializationNode
    {
        public SpecializationDefinition Definition { get; }
        public IReadOnlyList<string> Children { get; }
        public SpecializationNode(SpecializationDefinition definition, IReadOnlyList<string> children = null)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Children = children ?? Array.Empty<string>();
        }
    }

    public sealed class SpecializationTree
    {
        private readonly Dictionary<string, SpecializationNode> nodes = new Dictionary<string, SpecializationNode>();
        public bool Register(SpecializationNode node)
        {
            if (node == null || nodes.ContainsKey(node.Definition.SpecializationId)) return false;
            nodes.Add(node.Definition.SpecializationId, node);
            return true;
        }
        public bool TryGet(string id, out SpecializationNode node) => nodes.TryGetValue(id, out node);
        public IReadOnlyList<SpecializationNode> Query()
        {
            List<SpecializationNode> result = new List<SpecializationNode>(nodes.Values);
            result.Sort((left, right) => string.CompareOrdinal(left.Definition.SpecializationId, right.Definition.SpecializationId));
            return result;
        }
    }

    public sealed class SpecializationResolver
    {
        public bool Validate(SpecializationDefinition definition, int buildingLevel, IReadOnlyList<string> current)
        {
            if (buildingLevel < definition.RequiredLevel) return false;
            for (int i = 0; i < definition.ExclusiveWith.Count; i++)
            {
                for (int j = 0; j < current.Count; j++)
                {
                    if (definition.ExclusiveWith[i] == current[j]) return false;
                }
            }
            return true;
        }
    }

    public sealed class SpecializationDiagnostics
    {
        public int Registered { get; private set; }
        public int Unlocked { get; private set; }
        public int Applied { get; private set; }
        public int Removed { get; private set; }
        public int Resets { get; private set; }
        public void RecordRegistered(int count) => Registered = count;
        public void RecordUnlocked() => Unlocked++;
        public void RecordApplied() => Applied++;
        public void RecordRemoved() => Removed++;
        public void RecordReset() => Resets++;
    }

    public sealed class BuildingSpecializationManager
    {
        private readonly SpecializationTree tree = new SpecializationTree();
        private readonly SpecializationResolver resolver = new SpecializationResolver();
        private readonly Dictionary<string, List<string>> currentByBuilding = new Dictionary<string, List<string>>();
        private readonly IEventBus eventBus;

        public SpecializationDiagnostics Diagnostics { get; } = new SpecializationDiagnostics();
        public BuildingSpecializationManager(IEventBus eventBus = null) { this.eventBus = eventBus; }

        public bool RegisterSpecialization(SpecializationDefinition definition)
        {
            bool registered = tree.Register(new SpecializationNode(definition));
            if (registered) Diagnostics.RecordRegistered(tree.Query().Count);
            return registered;
        }

        public IReadOnlyList<SpecializationDefinition> QueryAvailableSpecializations(string buildingId, int buildingLevel)
        {
            List<SpecializationDefinition> result = new List<SpecializationDefinition>();
            IReadOnlyList<string> current = QueryCurrentSpecialization(buildingId);
            foreach (SpecializationNode node in tree.Query())
            {
                if (resolver.Validate(node.Definition, buildingLevel, current)) result.Add(node.Definition);
            }
            return result;
        }

        public bool UnlockSpecialization(string specializationId)
        {
            bool exists = tree.TryGet(specializationId, out _);
            if (exists) { Diagnostics.RecordUnlocked(); eventBus?.Publish(new SpecializationUnlocked(specializationId)); }
            return exists;
        }

        public bool ValidateSpecialization(string buildingId, string specializationId, int buildingLevel)
        {
            return tree.TryGet(specializationId, out SpecializationNode node) && resolver.Validate(node.Definition, buildingLevel, QueryCurrentSpecialization(buildingId));
        }

        public bool ApplySpecialization(string buildingId, string specializationId, int buildingLevel)
        {
            if (!ValidateSpecialization(buildingId, specializationId, buildingLevel)) return false;
            if (!currentByBuilding.TryGetValue(buildingId, out List<string> current))
            {
                current = new List<string>();
                currentByBuilding[buildingId] = current;
            }
            if (!current.Contains(specializationId)) current.Add(specializationId);
            current.Sort(StringComparer.Ordinal);
            Diagnostics.RecordApplied();
            eventBus?.Publish(new SpecializationApplied(buildingId, specializationId));
            eventBus?.Publish(new SpecializationChanged(buildingId));
            return true;
        }

        public bool RemoveSpecialization(string buildingId, string specializationId)
        {
            if (!currentByBuilding.TryGetValue(buildingId, out List<string> current)) return false;
            bool removed = current.Remove(specializationId);
            if (removed) { Diagnostics.RecordRemoved(); eventBus?.Publish(new SpecializationRemoved(buildingId, specializationId)); }
            return removed;
        }

        public bool ResetSpecialization(string buildingId)
        {
            bool removed = currentByBuilding.Remove(buildingId);
            if (removed) { Diagnostics.RecordReset(); eventBus?.Publish(new SpecializationReset(buildingId)); }
            return removed;
        }

        public IReadOnlyList<string> QueryCurrentSpecialization(string buildingId)
        {
            return currentByBuilding.TryGetValue(buildingId, out List<string> current) ? new List<string>(current) : Array.Empty<string>();
        }
    }

    public readonly struct SpecializationUnlocked : IGameplayEvent, IBuildingEvent { public string SpecializationId { get; } public SpecializationUnlocked(string specializationId) { SpecializationId = specializationId; } }
    public readonly struct SpecializationApplied : IGameplayEvent, IBuildingEvent { public string BuildingId { get; } public string SpecializationId { get; } public SpecializationApplied(string buildingId, string specializationId) { BuildingId = buildingId; SpecializationId = specializationId; } }
    public readonly struct SpecializationRemoved : IGameplayEvent, IBuildingEvent { public string BuildingId { get; } public string SpecializationId { get; } public SpecializationRemoved(string buildingId, string specializationId) { BuildingId = buildingId; SpecializationId = specializationId; } }
    public readonly struct SpecializationReset : IGameplayEvent, IBuildingEvent { public string BuildingId { get; } public SpecializationReset(string buildingId) { BuildingId = buildingId; } }
    public readonly struct SpecializationChanged : IGameplayEvent, IBuildingEvent { public string BuildingId { get; } public SpecializationChanged(string buildingId) { BuildingId = buildingId; } }
}
