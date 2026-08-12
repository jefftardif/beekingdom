using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Buildings
{
    public enum BuildingCategory { Chamber, Corridor, Entrance, Storage, Production, Nursery, Royal, Defense, Utility, Decoration }
    public enum BuildingState { Draft, Planned, Reserved, UnderConstruction, Operational, Damaged, Disabled, Destroyed }

    public readonly struct BuildingSize
    {
        public int Width { get; }
        public int Height { get; }

        public BuildingSize(int width, int height)
        {
            Width = width <= 0 ? 1 : width;
            Height = height <= 0 ? 1 : height;
        }
    }

    public readonly struct BuildingPosition : IEquatable<BuildingPosition>
    {
        public int X { get; }
        public int Y { get; }
        public int Depth { get; }

        public BuildingPosition(int x, int y, int depth = 0)
        {
            X = x;
            Y = y;
            Depth = depth;
        }

        public bool Equals(BuildingPosition other) => X == other.X && Y == other.Y && Depth == other.Depth;
        public override bool Equals(object obj) => obj is BuildingPosition other && Equals(other);
        public override int GetHashCode() => (X * 397) ^ (Y * 31) ^ Depth;
    }

    public readonly struct BuildingResourceCost
    {
        public string ResourceId { get; }
        public double Amount { get; }

        public BuildingResourceCost(string resourceId, double amount)
        {
            ResourceId = string.IsNullOrWhiteSpace(resourceId) ? throw new ArgumentException("Resource id is required.", nameof(resourceId)) : resourceId;
            Amount = amount < 0d ? 0d : amount;
        }
    }

    public sealed class BuildingDefinition
    {
        public string BuildingId { get; }
        public string DisplayName { get; }
        public BuildingCategory Category { get; }
        public string Description { get; }
        public BuildingSize Size { get; }
        public string Shape { get; }
        public IReadOnlyList<BuildingResourceCost> ConstructionCost { get; }
        public double ConstructionTimeSeconds { get; }
        public IReadOnlyList<string> UpgradeTree { get; }
        public IReadOnlyList<BuildingResourceCost> MaintenanceCost { get; }
        public IReadOnlyList<string> RequiredTechnologies { get; }
        public IReadOnlyList<string> RequiredBuildings { get; }
        public IReadOnlyList<string> GameplayTags { get; }

        public BuildingDefinition(
            string buildingId,
            string displayName,
            BuildingCategory category,
            BuildingSize size,
            string description = "",
            string shape = "Rectangle",
            IReadOnlyList<BuildingResourceCost> constructionCost = null,
            double constructionTimeSeconds = 0d,
            IReadOnlyList<string> upgradeTree = null,
            IReadOnlyList<BuildingResourceCost> maintenanceCost = null,
            IReadOnlyList<string> requiredTechnologies = null,
            IReadOnlyList<string> requiredBuildings = null,
            IReadOnlyList<string> gameplayTags = null)
        {
            BuildingId = string.IsNullOrWhiteSpace(buildingId) ? throw new ArgumentException("Building id is required.", nameof(buildingId)) : buildingId;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? BuildingId : displayName;
            Category = category;
            Size = size;
            Description = description ?? string.Empty;
            Shape = string.IsNullOrWhiteSpace(shape) ? "Rectangle" : shape;
            ConstructionCost = constructionCost ?? Array.Empty<BuildingResourceCost>();
            ConstructionTimeSeconds = constructionTimeSeconds < 0d ? 0d : constructionTimeSeconds;
            UpgradeTree = upgradeTree ?? Array.Empty<string>();
            MaintenanceCost = maintenanceCost ?? Array.Empty<BuildingResourceCost>();
            RequiredTechnologies = requiredTechnologies ?? Array.Empty<string>();
            RequiredBuildings = requiredBuildings ?? Array.Empty<string>();
            GameplayTags = gameplayTags ?? Array.Empty<string>();
        }
    }

    public sealed class BuildingInstance
    {
        public string EntityId { get; }
        public string DefinitionId { get; }
        public BuildingPosition Position { get; private set; }
        public int Rotation { get; private set; }
        public BuildingState CurrentState { get; private set; }
        public double Health { get; private set; }
        public double Progress { get; private set; }
        public string OwnerHive { get; }
        public string AttributeSet { get; }
        public string ConstructionWorkflowId { get; private set; }

        public BuildingInstance(string entityId, string definitionId, BuildingPosition position, int rotation, string ownerHive, string attributeSet = "", string constructionWorkflowId = "")
        {
            EntityId = string.IsNullOrWhiteSpace(entityId) ? throw new ArgumentException("Entity id is required.", nameof(entityId)) : entityId;
            DefinitionId = string.IsNullOrWhiteSpace(definitionId) ? throw new ArgumentException("Definition id is required.", nameof(definitionId)) : definitionId;
            Position = position;
            Rotation = NormalizeRotation(rotation);
            OwnerHive = ownerHive ?? string.Empty;
            AttributeSet = attributeSet ?? string.Empty;
            ConstructionWorkflowId = constructionWorkflowId ?? string.Empty;
            CurrentState = BuildingState.Planned;
            Health = 1d;
        }

        public bool ChangeState(BuildingState next)
        {
            if (!CanTransition(CurrentState, next)) return false;
            CurrentState = next;
            return true;
        }

        public void Place(BuildingPosition position, int rotation)
        {
            Position = position;
            Rotation = NormalizeRotation(rotation);
        }

        public void AssignWorkflow(string workflowId)
        {
            ConstructionWorkflowId = workflowId ?? string.Empty;
        }

        public void SetProgress(double progress)
        {
            Progress = progress < 0d ? 0d : progress > 1d ? 1d : progress;
        }

        public void Damage(double amount)
        {
            if (amount <= 0d) return;
            Health = Math.Max(0d, Health - amount);
            if (Health <= 0d) CurrentState = BuildingState.Destroyed;
            else if (CurrentState == BuildingState.Operational) CurrentState = BuildingState.Damaged;
        }

        private static int NormalizeRotation(int rotation)
        {
            int normalized = rotation % 360;
            return normalized < 0 ? normalized + 360 : normalized;
        }

        private static bool CanTransition(BuildingState current, BuildingState next)
        {
            if (current == next) return true;
            if (next == BuildingState.Destroyed) return true;
            if (current == BuildingState.Draft && next == BuildingState.Planned) return true;
            if (current == BuildingState.Planned && next == BuildingState.Reserved) return true;
            if (current == BuildingState.Reserved && next == BuildingState.UnderConstruction) return true;
            if (current == BuildingState.UnderConstruction && next == BuildingState.Operational) return true;
            if (current == BuildingState.Operational && (next == BuildingState.Damaged || next == BuildingState.Disabled)) return true;
            if (current == BuildingState.Damaged && (next == BuildingState.Operational || next == BuildingState.Disabled)) return true;
            if (current == BuildingState.Disabled && next == BuildingState.Operational) return true;
            return false;
        }
    }

    public sealed class BuildingRegistry
    {
        private readonly Dictionary<string, BuildingDefinition> definitions = new Dictionary<string, BuildingDefinition>();

        public int Count => definitions.Count;

        public bool RegisterDefinition(BuildingDefinition definition)
        {
            if (definition == null || definitions.ContainsKey(definition.BuildingId)) return false;
            definitions.Add(definition.BuildingId, definition);
            return true;
        }

        public bool TryGetDefinition(string buildingId, out BuildingDefinition definition)
        {
            return definitions.TryGetValue(buildingId, out definition);
        }
    }

    public sealed class BuildingFactory
    {
        private long counter;

        public BuildingInstance Create(BuildingDefinition definition, BuildingPosition position, int rotation, string ownerHive)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            string entityId = definition.BuildingId + "-" + (++counter);
            return new BuildingInstance(entityId, definition.BuildingId, position, rotation, ownerHive);
        }
    }

    public sealed class BuildingDiagnostics
    {
        public int RegisteredDefinitions { get; private set; }
        public int CreatedBuildings { get; private set; }
        public int DestroyedBuildings { get; private set; }
        public int SnapshotsCreated { get; private set; }
        public int SnapshotsRestored { get; private set; }

        public void RecordDefinitions(int count) => RegisteredDefinitions = count;
        public void RecordCreated() => CreatedBuildings++;
        public void RecordDestroyed() => DestroyedBuildings++;
        public void RecordSnapshot() => SnapshotsCreated++;
        public void RecordRestore() => SnapshotsRestored++;
    }

    public sealed class BuildingSnapshot
    {
        public int Version;
        public BuildingRecord[] Buildings;
    }

    public struct BuildingRecord
    {
        public string EntityId;
        public string DefinitionId;
        public int X;
        public int Y;
        public int Depth;
        public int Rotation;
        public BuildingState State;
        public double Health;
        public double Progress;
        public string OwnerHive;
        public string AttributeSet;
        public string ConstructionWorkflowId;
    }

    public sealed class BuildingManager
    {
        private const int SnapshotVersion = 1;

        private readonly Dictionary<string, BuildingInstance> buildings = new Dictionary<string, BuildingInstance>();
        private readonly BuildingRegistry registry = new BuildingRegistry();
        private readonly BuildingFactory factory = new BuildingFactory();
        private readonly IEventBus eventBus;

        public BuildingDiagnostics Diagnostics { get; } = new BuildingDiagnostics();
        public int BuildingCount => buildings.Count;

        public BuildingManager(IEventBus eventBus = null)
        {
            this.eventBus = eventBus;
        }

        public bool RegisterDefinition(BuildingDefinition definition)
        {
            bool registered = registry.RegisterDefinition(definition);
            if (registered) Diagnostics.RecordDefinitions(registry.Count);
            return registered;
        }

        public BuildingInstance CreateBuilding(string definitionId, BuildingPosition position, int rotation, string ownerHive)
        {
            if (!registry.TryGetDefinition(definitionId, out BuildingDefinition definition)) return null;
            BuildingInstance instance = factory.Create(definition, position, rotation, ownerHive);
            buildings.Add(instance.EntityId, instance);
            Diagnostics.RecordCreated();
            eventBus?.Publish(new BuildingCreated(instance.EntityId, definitionId));
            return instance;
        }

        public bool DestroyBuilding(string entityId)
        {
            if (!buildings.TryGetValue(entityId, out BuildingInstance instance)) return false;
            instance.ChangeState(BuildingState.Destroyed);
            buildings.Remove(entityId);
            Diagnostics.RecordDestroyed();
            eventBus?.Publish(new BuildingDestroyed(entityId));
            return true;
        }

        public bool TryGetBuilding(string entityId, out BuildingInstance building)
        {
            return buildings.TryGetValue(entityId, out building);
        }

        public IReadOnlyList<BuildingInstance> QueryBuildings()
        {
            return new List<BuildingInstance>(buildings.Values);
        }

        public IReadOnlyList<BuildingInstance> QueryByCategory(BuildingCategory category)
        {
            List<BuildingInstance> result = new List<BuildingInstance>();
            foreach (BuildingInstance instance in buildings.Values)
            {
                if (registry.TryGetDefinition(instance.DefinitionId, out BuildingDefinition definition) && definition.Category == category)
                {
                    result.Add(instance);
                }
            }
            return result;
        }

        public IReadOnlyList<BuildingInstance> QueryByTag(string tag)
        {
            List<BuildingInstance> result = new List<BuildingInstance>();
            foreach (BuildingInstance instance in buildings.Values)
            {
                if (!registry.TryGetDefinition(instance.DefinitionId, out BuildingDefinition definition)) continue;
                for (int i = 0; i < definition.GameplayTags.Count; i++)
                {
                    if (definition.GameplayTags[i] == tag)
                    {
                        result.Add(instance);
                        break;
                    }
                }
            }
            return result;
        }

        public BuildingSnapshot Snapshot()
        {
            List<BuildingRecord> records = new List<BuildingRecord>(buildings.Count);
            foreach (BuildingInstance instance in buildings.Values)
            {
                records.Add(new BuildingRecord
                {
                    EntityId = instance.EntityId,
                    DefinitionId = instance.DefinitionId,
                    X = instance.Position.X,
                    Y = instance.Position.Y,
                    Depth = instance.Position.Depth,
                    Rotation = instance.Rotation,
                    State = instance.CurrentState,
                    Health = instance.Health,
                    Progress = instance.Progress,
                    OwnerHive = instance.OwnerHive,
                    AttributeSet = instance.AttributeSet,
                    ConstructionWorkflowId = instance.ConstructionWorkflowId
                });
            }

            records.Sort((left, right) => string.CompareOrdinal(left.EntityId, right.EntityId));
            Diagnostics.RecordSnapshot();
            return new BuildingSnapshot { Version = SnapshotVersion, Buildings = records.ToArray() };
        }

        public void RestoreSnapshot(BuildingSnapshot snapshot)
        {
            buildings.Clear();
            if (snapshot?.Buildings == null) return;

            for (int i = 0; i < snapshot.Buildings.Length; i++)
            {
                BuildingRecord record = snapshot.Buildings[i];
                BuildingInstance instance = new BuildingInstance(
                    record.EntityId,
                    record.DefinitionId,
                    new BuildingPosition(record.X, record.Y, record.Depth),
                    record.Rotation,
                    record.OwnerHive,
                    record.AttributeSet,
                    record.ConstructionWorkflowId);
                instance.ChangeState(record.State);
                instance.SetProgress(record.Progress);
                if (record.Health < 1d) instance.Damage(1d - record.Health);
                buildings[instance.EntityId] = instance;
            }

            Diagnostics.RecordRestore();
        }
    }

    public readonly struct BuildingCreated : IGameplayEvent, IBuildingEvent
    {
        public string EntityId { get; }
        public string DefinitionId { get; }
        public BuildingCreated(string entityId, string definitionId) { EntityId = entityId; DefinitionId = definitionId; }
    }

    public readonly struct BuildingPlaced : IGameplayEvent, IBuildingEvent { public string EntityId { get; } public BuildingPlaced(string entityId) { EntityId = entityId; } }
    public readonly struct BuildingConstructionStarted : IGameplayEvent, IBuildingEvent { public string EntityId { get; } public BuildingConstructionStarted(string entityId) { EntityId = entityId; } }
    public readonly struct BuildingConstructionCompleted : IGameplayEvent, IBuildingEvent { public string EntityId { get; } public BuildingConstructionCompleted(string entityId) { EntityId = entityId; } }
    public readonly struct BuildingDamaged : IGameplayEvent, IBuildingEvent { public string EntityId { get; } public BuildingDamaged(string entityId) { EntityId = entityId; } }
    public readonly struct BuildingDestroyed : IGameplayEvent, IBuildingEvent { public string EntityId { get; } public BuildingDestroyed(string entityId) { EntityId = entityId; } }
    public readonly struct BuildingUpgraded : IGameplayEvent, IBuildingEvent { public string EntityId { get; } public BuildingUpgraded(string entityId) { EntityId = entityId; } }
}
