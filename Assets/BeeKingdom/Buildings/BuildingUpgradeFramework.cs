using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Buildings
{
    public enum UpgradeState { Available, Planned, WaitingResources, WaitingBuilders, Upgrading, Completed, Suspended, Cancelled, Failed }

    public sealed class UpgradeRequirement
    {
        public int RequiredLevel { get; }
        public IReadOnlyList<string> RequiredTechnologies { get; }
        public IReadOnlyList<BuildingResourceCost> RequiredResources { get; }

        public UpgradeRequirement(int requiredLevel = 0, IReadOnlyList<string> requiredTechnologies = null, IReadOnlyList<BuildingResourceCost> requiredResources = null)
        {
            RequiredLevel = requiredLevel < 0 ? 0 : requiredLevel;
            RequiredTechnologies = requiredTechnologies ?? Array.Empty<string>();
            RequiredResources = requiredResources ?? Array.Empty<BuildingResourceCost>();
        }
    }

    public sealed class BuildingUpgradeDefinition
    {
        public string UpgradeId { get; }
        public string BuildingDefinitionId { get; }
        public int TargetLevel { get; }
        public IReadOnlyList<string> ExclusiveWith { get; }
        public UpgradeRequirement Requirement { get; }
        public string GameplayEffectId { get; }

        public BuildingUpgradeDefinition(string upgradeId, string buildingDefinitionId, int targetLevel, UpgradeRequirement requirement = null, IReadOnlyList<string> exclusiveWith = null, string gameplayEffectId = "")
        {
            UpgradeId = string.IsNullOrWhiteSpace(upgradeId) ? throw new ArgumentException("Upgrade id is required.", nameof(upgradeId)) : upgradeId;
            BuildingDefinitionId = buildingDefinitionId ?? string.Empty;
            TargetLevel = targetLevel <= 0 ? 1 : targetLevel;
            Requirement = requirement ?? new UpgradeRequirement();
            ExclusiveWith = exclusiveWith ?? Array.Empty<string>();
            GameplayEffectId = gameplayEffectId ?? string.Empty;
        }
    }

    public sealed class BuildingUpgradeInstance
    {
        public string InstanceId { get; }
        public string BuildingEntityId { get; }
        public BuildingUpgradeDefinition Definition { get; }
        public UpgradeState State { get; private set; }

        public BuildingUpgradeInstance(string instanceId, string buildingEntityId, BuildingUpgradeDefinition definition)
        {
            InstanceId = instanceId ?? string.Empty;
            BuildingEntityId = buildingEntityId ?? string.Empty;
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            State = UpgradeState.Planned;
        }

        public void ChangeState(UpgradeState state) => State = state;
    }

    public sealed class UpgradeTree
    {
        private readonly Dictionary<string, BuildingUpgradeDefinition> upgrades = new Dictionary<string, BuildingUpgradeDefinition>();
        public bool RegisterUpgrade(BuildingUpgradeDefinition definition)
        {
            if (definition == null || upgrades.ContainsKey(definition.UpgradeId)) return false;
            upgrades.Add(definition.UpgradeId, definition);
            return true;
        }
        public bool TryGet(string upgradeId, out BuildingUpgradeDefinition definition) => upgrades.TryGetValue(upgradeId, out definition);
        public IReadOnlyList<BuildingUpgradeDefinition> GetAll()
        {
            List<BuildingUpgradeDefinition> result = new List<BuildingUpgradeDefinition>(upgrades.Values);
            result.Sort((left, right) => string.CompareOrdinal(left.UpgradeId, right.UpgradeId));
            return result;
        }
    }

    public sealed class UpgradeDiagnostics
    {
        public int Registered { get; private set; }
        public int Started { get; private set; }
        public int Completed { get; private set; }
        public int Cancelled { get; private set; }
        public int Failed { get; private set; }
        public void RecordRegistered(int count) => Registered = count;
        public void RecordStarted() => Started++;
        public void RecordCompleted() => Completed++;
        public void RecordCancelled() => Cancelled++;
        public void RecordFailed() => Failed++;
    }

    public sealed class BuildingUpgradeManager
    {
        private readonly UpgradeTree tree = new UpgradeTree();
        private readonly Dictionary<string, int> levelsByBuilding = new Dictionary<string, int>();
        private readonly Dictionary<string, BuildingUpgradeInstance> active = new Dictionary<string, BuildingUpgradeInstance>();
        private readonly List<BuildingUpgradeInstance> history = new List<BuildingUpgradeInstance>();
        private readonly IEventBus eventBus;
        private long counter;

        public UpgradeDiagnostics Diagnostics { get; } = new UpgradeDiagnostics();

        public BuildingUpgradeManager(IEventBus eventBus = null) { this.eventBus = eventBus; }

        public bool RegisterUpgrade(BuildingUpgradeDefinition definition)
        {
            bool registered = tree.RegisterUpgrade(definition);
            if (registered) Diagnostics.RecordRegistered(tree.GetAll().Count);
            return registered;
        }

        public IReadOnlyList<BuildingUpgradeDefinition> GetAvailableUpgrades(string buildingEntityId, string buildingDefinitionId)
        {
            int level = levelsByBuilding.TryGetValue(buildingEntityId, out int current) ? current : 0;
            List<BuildingUpgradeDefinition> result = new List<BuildingUpgradeDefinition>();
            IReadOnlyList<BuildingUpgradeDefinition> all = tree.GetAll();
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].BuildingDefinitionId == buildingDefinitionId && all[i].Requirement.RequiredLevel <= level)
                {
                    result.Add(all[i]);
                    eventBus?.Publish(new UpgradeUnlocked(all[i].UpgradeId));
                }
            }
            return result;
        }

        public bool ValidateUpgrade(string buildingEntityId, string upgradeId)
        {
            if (!tree.TryGet(upgradeId, out BuildingUpgradeDefinition definition)) return false;
            int level = levelsByBuilding.TryGetValue(buildingEntityId, out int current) ? current : 0;
            return level >= definition.Requirement.RequiredLevel;
        }

        public BuildingUpgradeInstance StartUpgrade(string buildingEntityId, string upgradeId)
        {
            if (!ValidateUpgrade(buildingEntityId, upgradeId) || !tree.TryGet(upgradeId, out BuildingUpgradeDefinition definition)) return null;
            BuildingUpgradeInstance instance = new BuildingUpgradeInstance("upgrade-" + (++counter), buildingEntityId, definition);
            instance.ChangeState(UpgradeState.Upgrading);
            active[instance.InstanceId] = instance;
            Diagnostics.RecordStarted();
            eventBus?.Publish(new UpgradeStarted(instance.InstanceId));
            return instance;
        }

        public bool CancelUpgrade(string instanceId)
        {
            if (!active.TryGetValue(instanceId, out BuildingUpgradeInstance instance)) return false;
            instance.ChangeState(UpgradeState.Cancelled);
            active.Remove(instanceId);
            history.Add(instance);
            Diagnostics.RecordCancelled();
            eventBus?.Publish(new UpgradeCancelled(instanceId));
            return true;
        }

        public bool CompleteUpgrade(string instanceId)
        {
            if (!active.TryGetValue(instanceId, out BuildingUpgradeInstance instance)) return false;
            instance.ChangeState(UpgradeState.Completed);
            levelsByBuilding[instance.BuildingEntityId] = Math.Max(GetLevel(instance.BuildingEntityId), instance.Definition.TargetLevel);
            active.Remove(instanceId);
            history.Add(instance);
            Diagnostics.RecordCompleted();
            eventBus?.Publish(new UpgradeCompleted(instanceId));
            return true;
        }

        public IReadOnlyList<BuildingUpgradeInstance> QueryUpgradeHistory()
        {
            return new List<BuildingUpgradeInstance>(history);
        }

        private int GetLevel(string buildingEntityId) => levelsByBuilding.TryGetValue(buildingEntityId, out int level) ? level : 0;
    }

    public readonly struct UpgradeUnlocked : IGameplayEvent, IBuildingEvent { public string UpgradeId { get; } public UpgradeUnlocked(string upgradeId) { UpgradeId = upgradeId; } }
    public readonly struct UpgradeStarted : IGameplayEvent, IBuildingEvent { public string InstanceId { get; } public UpgradeStarted(string instanceId) { InstanceId = instanceId; } }
    public readonly struct UpgradeCompleted : IGameplayEvent, IBuildingEvent { public string InstanceId { get; } public UpgradeCompleted(string instanceId) { InstanceId = instanceId; } }
    public readonly struct UpgradeCancelled : IGameplayEvent, IBuildingEvent { public string InstanceId { get; } public UpgradeCancelled(string instanceId) { InstanceId = instanceId; } }
    public readonly struct UpgradeFailed : IGameplayEvent, IBuildingEvent { public string InstanceId { get; } public UpgradeFailed(string instanceId) { InstanceId = instanceId; } }
}
