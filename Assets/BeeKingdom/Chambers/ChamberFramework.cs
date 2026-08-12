using System;
using System.Collections.Generic;
using BeeKingdom.Buildings;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Chambers
{
    public enum ChamberState { Planned, UnderConstruction, Inactive, Operational, Overloaded, Damaged, Disabled, Destroyed }

    public readonly struct ChamberCapacity
    {
        public int Max { get; }
        public int Occupancy { get; }
        public double Efficiency => Max <= 0 ? 0d : Math.Min(1d, (double)Occupancy / Max);
        public bool IsOverloaded => Occupancy > Max;

        public ChamberCapacity(int max, int occupancy = 0)
        {
            Max = max < 0 ? 0 : max;
            Occupancy = occupancy < 0 ? 0 : occupancy;
        }
    }

    public sealed class ChamberDefinition
    {
        public string ChamberId { get; }
        public string DisplayName { get; }
        public string Category { get; }
        public string Description { get; }
        public int Capacity { get; }
        public BuildingSize Size { get; }
        public string Shape { get; }
        public IReadOnlyList<string> SupportedActivities { get; }
        public IReadOnlyList<string> AcceptedResources { get; }
        public IReadOnlyList<string> RequiredBuildings { get; }
        public IReadOnlyList<string> RequiredTechnologies { get; }
        public IReadOnlyList<string> GameplayTags { get; }

        public ChamberDefinition(string chamberId, string displayName, string category, int capacity, BuildingSize size, string description = "", string shape = "Rectangle", IReadOnlyList<string> supportedActivities = null, IReadOnlyList<string> acceptedResources = null, IReadOnlyList<string> requiredBuildings = null, IReadOnlyList<string> requiredTechnologies = null, IReadOnlyList<string> gameplayTags = null)
        {
            ChamberId = string.IsNullOrWhiteSpace(chamberId) ? throw new ArgumentException("Chamber id is required.", nameof(chamberId)) : chamberId;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? ChamberId : displayName;
            Category = string.IsNullOrWhiteSpace(category) ? "Utility" : category;
            Capacity = capacity < 0 ? 0 : capacity;
            Size = size;
            Description = description ?? string.Empty;
            Shape = string.IsNullOrWhiteSpace(shape) ? "Rectangle" : shape;
            SupportedActivities = supportedActivities ?? Array.Empty<string>();
            AcceptedResources = acceptedResources ?? Array.Empty<string>();
            RequiredBuildings = requiredBuildings ?? Array.Empty<string>();
            RequiredTechnologies = requiredTechnologies ?? Array.Empty<string>();
            GameplayTags = gameplayTags ?? Array.Empty<string>();
        }
    }

    public sealed class ChamberInstance
    {
        public string EntityId { get; }
        public string DefinitionId { get; }
        public string BuildingId { get; }
        public BuildingPosition Position { get; private set; }
        public int Rotation { get; private set; }
        public ChamberState CurrentState { get; private set; }
        public ChamberCapacity Capacity { get; private set; }
        public double Health { get; private set; } = 1d;
        public string AttributeSet { get; }

        public ChamberInstance(string entityId, string definitionId, string buildingId, BuildingPosition position, int rotation, int capacity, string attributeSet = "")
        {
            EntityId = string.IsNullOrWhiteSpace(entityId) ? throw new ArgumentException("Entity id is required.", nameof(entityId)) : entityId;
            DefinitionId = definitionId ?? string.Empty;
            BuildingId = buildingId ?? string.Empty;
            Position = position;
            Rotation = rotation;
            Capacity = new ChamberCapacity(capacity);
            AttributeSet = attributeSet ?? string.Empty;
            CurrentState = ChamberState.Planned;
        }

        public void SetOccupancy(int occupancy)
        {
            Capacity = new ChamberCapacity(Capacity.Max, occupancy);
            if (Capacity.IsOverloaded) CurrentState = ChamberState.Overloaded;
        }

        public bool ChangeState(ChamberState state)
        {
            if (CurrentState == ChamberState.Destroyed && state != ChamberState.Destroyed) return false;
            CurrentState = state;
            return true;
        }

        public void Damage(double amount)
        {
            if (amount <= 0d) return;
            Health = Math.Max(0d, Health - amount);
            CurrentState = Health <= 0d ? ChamberState.Destroyed : ChamberState.Damaged;
        }
    }

    public sealed class ChamberRegistry
    {
        private readonly Dictionary<string, ChamberDefinition> definitions = new Dictionary<string, ChamberDefinition>();
        public int Count => definitions.Count;
        public bool RegisterDefinition(ChamberDefinition definition)
        {
            if (definition == null || definitions.ContainsKey(definition.ChamberId)) return false;
            definitions.Add(definition.ChamberId, definition);
            return true;
        }
        public bool TryGetDefinition(string chamberId, out ChamberDefinition definition) => definitions.TryGetValue(chamberId, out definition);
    }

    public sealed class ChamberFactory
    {
        private long counter;
        public ChamberInstance Create(ChamberDefinition definition, string buildingId, BuildingPosition position, int rotation)
        {
            return new ChamberInstance(definition.ChamberId + "-" + (++counter), definition.ChamberId, buildingId, position, rotation, definition.Capacity);
        }
    }

    public sealed class ChamberDiagnostics
    {
        public int RegisteredDefinitions { get; private set; }
        public int Created { get; private set; }
        public int Destroyed { get; private set; }
        public int Overloaded { get; private set; }
        public int Snapshots { get; private set; }
        public int Restores { get; private set; }
        public void RecordDefinitions(int count) => RegisteredDefinitions = count;
        public void RecordCreated() => Created++;
        public void RecordDestroyed() => Destroyed++;
        public void RecordOverload() => Overloaded++;
        public void RecordSnapshot() => Snapshots++;
        public void RecordRestore() => Restores++;
    }

    public sealed class ChamberSnapshot { public int Version; public ChamberRecord[] Chambers; }
    public struct ChamberRecord
    {
        public string EntityId;
        public string DefinitionId;
        public string BuildingId;
        public int X;
        public int Y;
        public int Depth;
        public int Rotation;
        public ChamberState State;
        public int Capacity;
        public int Occupancy;
        public double Health;
        public string AttributeSet;
    }

    public sealed class ChamberManager
    {
        private const int SnapshotVersion = 1;
        private readonly ChamberRegistry registry = new ChamberRegistry();
        private readonly ChamberFactory factory = new ChamberFactory();
        private readonly Dictionary<string, ChamberInstance> chambers = new Dictionary<string, ChamberInstance>();
        private readonly IEventBus eventBus;

        public ChamberDiagnostics Diagnostics { get; } = new ChamberDiagnostics();
        public int ChamberCount => chambers.Count;

        public ChamberManager(IEventBus eventBus = null) { this.eventBus = eventBus; }

        public bool RegisterDefinition(ChamberDefinition definition)
        {
            bool registered = registry.RegisterDefinition(definition);
            if (registered) Diagnostics.RecordDefinitions(registry.Count);
            return registered;
        }

        public ChamberInstance CreateChamber(string definitionId, string buildingId, BuildingPosition position, int rotation)
        {
            if (!registry.TryGetDefinition(definitionId, out ChamberDefinition definition)) return null;
            ChamberInstance chamber = factory.Create(definition, buildingId, position, rotation);
            chambers.Add(chamber.EntityId, chamber);
            Diagnostics.RecordCreated();
            eventBus?.Publish(new ChamberCreated(chamber.EntityId));
            return chamber;
        }

        public bool DestroyChamber(string entityId)
        {
            if (!chambers.TryGetValue(entityId, out ChamberInstance chamber)) return false;
            chamber.ChangeState(ChamberState.Destroyed);
            chambers.Remove(entityId);
            Diagnostics.RecordDestroyed();
            eventBus?.Publish(new ChamberDestroyed(entityId));
            return true;
        }

        public bool GetChamber(string entityId, out ChamberInstance chamber) => chambers.TryGetValue(entityId, out chamber);
        public IReadOnlyList<ChamberInstance> QueryChambers() => new List<ChamberInstance>(chambers.Values);

        public IReadOnlyList<ChamberInstance> QueryByCategory(string category)
        {
            List<ChamberInstance> result = new List<ChamberInstance>();
            foreach (ChamberInstance chamber in chambers.Values)
            {
                if (registry.TryGetDefinition(chamber.DefinitionId, out ChamberDefinition definition) && definition.Category == category) result.Add(chamber);
            }
            return result;
        }

        public IReadOnlyList<ChamberInstance> QueryByActivity(string activity)
        {
            List<ChamberInstance> result = new List<ChamberInstance>();
            foreach (ChamberInstance chamber in chambers.Values)
            {
                if (!registry.TryGetDefinition(chamber.DefinitionId, out ChamberDefinition definition)) continue;
                for (int i = 0; i < definition.SupportedActivities.Count; i++)
                {
                    if (definition.SupportedActivities[i] == activity) { result.Add(chamber); break; }
                }
            }
            return result;
        }

        public void SetOccupancy(string entityId, int occupancy)
        {
            if (!chambers.TryGetValue(entityId, out ChamberInstance chamber)) return;
            chamber.SetOccupancy(occupancy);
            eventBus?.Publish(new ChamberCapacityChanged(entityId));
            if (chamber.Capacity.IsOverloaded) { Diagnostics.RecordOverload(); eventBus?.Publish(new ChamberOverloaded(entityId)); }
        }

        public ChamberSnapshot Snapshot()
        {
            List<ChamberRecord> records = new List<ChamberRecord>();
            foreach (ChamberInstance chamber in chambers.Values)
            {
                records.Add(new ChamberRecord { EntityId = chamber.EntityId, DefinitionId = chamber.DefinitionId, BuildingId = chamber.BuildingId, X = chamber.Position.X, Y = chamber.Position.Y, Depth = chamber.Position.Depth, Rotation = chamber.Rotation, State = chamber.CurrentState, Capacity = chamber.Capacity.Max, Occupancy = chamber.Capacity.Occupancy, Health = chamber.Health, AttributeSet = chamber.AttributeSet });
            }
            records.Sort((left, right) => string.CompareOrdinal(left.EntityId, right.EntityId));
            Diagnostics.RecordSnapshot();
            return new ChamberSnapshot { Version = SnapshotVersion, Chambers = records.ToArray() };
        }

        public void RestoreSnapshot(ChamberSnapshot snapshot)
        {
            chambers.Clear();
            if (snapshot?.Chambers == null) return;
            for (int i = 0; i < snapshot.Chambers.Length; i++)
            {
                ChamberRecord record = snapshot.Chambers[i];
                ChamberInstance chamber = new ChamberInstance(record.EntityId, record.DefinitionId, record.BuildingId, new BuildingPosition(record.X, record.Y, record.Depth), record.Rotation, record.Capacity, record.AttributeSet);
                chamber.SetOccupancy(record.Occupancy);
                chamber.ChangeState(record.State);
                if (record.Health < 1d) chamber.Damage(1d - record.Health);
                chambers[chamber.EntityId] = chamber;
            }
            Diagnostics.RecordRestore();
        }
    }

    public readonly struct ChamberCreated : IGameplayEvent, IBuildingEvent { public string EntityId { get; } public ChamberCreated(string entityId) { EntityId = entityId; } }
    public readonly struct ChamberActivated : IGameplayEvent, IBuildingEvent { public string EntityId { get; } public ChamberActivated(string entityId) { EntityId = entityId; } }
    public readonly struct ChamberCapacityChanged : IGameplayEvent, IBuildingEvent { public string EntityId { get; } public ChamberCapacityChanged(string entityId) { EntityId = entityId; } }
    public readonly struct ChamberOverloaded : IGameplayEvent, IBuildingEvent { public string EntityId { get; } public ChamberOverloaded(string entityId) { EntityId = entityId; } }
    public readonly struct ChamberDamaged : IGameplayEvent, IBuildingEvent { public string EntityId { get; } public ChamberDamaged(string entityId) { EntityId = entityId; } }
    public readonly struct ChamberDestroyed : IGameplayEvent, IBuildingEvent { public string EntityId { get; } public ChamberDestroyed(string entityId) { EntityId = entityId; } }
}
