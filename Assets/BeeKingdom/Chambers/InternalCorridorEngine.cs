using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Chambers
{
    public enum CorridorType { Standard, Narrow, Large, Vertical, Royal, Restricted, Emergency, Temporary }
    public enum CorridorState { Planned, UnderConstruction, Operational, Congested, Blocked, Collapsed, Destroyed }

    public sealed class CorridorDefinition
    {
        public string CorridorId { get; }
        public CorridorType Type { get; }
        public double Width { get; }
        public int Capacity { get; }
        public int MaximumTraffic { get; }
        public double MovementSpeed { get; }
        public double BaseTraversalCost { get; }

        public CorridorDefinition(string corridorId, CorridorType type, double width, int capacity, int maximumTraffic, double movementSpeed, double baseTraversalCost)
        {
            CorridorId = string.IsNullOrWhiteSpace(corridorId) ? throw new ArgumentException("Corridor id is required.", nameof(corridorId)) : corridorId;
            Type = type;
            Width = width <= 0d ? 1d : width;
            Capacity = capacity <= 0 ? 1 : capacity;
            MaximumTraffic = maximumTraffic <= 0 ? Capacity : maximumTraffic;
            MovementSpeed = movementSpeed <= 0d ? 1d : movementSpeed;
            BaseTraversalCost = baseTraversalCost <= 0d ? 1d : baseTraversalCost;
        }
    }

    public sealed class CorridorInstance
    {
        public string CorridorId { get; }
        public string EntityId { get; }
        public string SourceConnection { get; }
        public string DestinationConnection { get; }
        public double Length { get; }
        public double Width { get; }
        public int Capacity { get; }
        public int CurrentTraffic { get; private set; }
        public int MaximumTraffic { get; }
        public CorridorState State { get; private set; }
        public double MovementSpeed { get; }
        public double BaseTraversalCost { get; }
        public double CongestionFactor => Capacity <= 0 ? 1d : Math.Max(1d, (double)CurrentTraffic / Capacity);

        public CorridorInstance(string corridorId, string entityId, string sourceConnection, string destinationConnection, double length, CorridorDefinition definition)
        {
            CorridorId = corridorId ?? string.Empty;
            EntityId = entityId ?? string.Empty;
            SourceConnection = sourceConnection ?? string.Empty;
            DestinationConnection = destinationConnection ?? string.Empty;
            Length = length <= 0d ? 1d : length;
            Width = definition.Width;
            Capacity = definition.Capacity;
            MaximumTraffic = definition.MaximumTraffic;
            MovementSpeed = definition.MovementSpeed;
            BaseTraversalCost = definition.BaseTraversalCost;
            State = CorridorState.Planned;
        }

        public bool IsTraversable => (State == CorridorState.Operational || State == CorridorState.Congested) && CurrentTraffic < MaximumTraffic;

        public bool ChangeState(CorridorState state)
        {
            if (State == CorridorState.Destroyed && state != CorridorState.Destroyed) return false;
            State = state;
            return true;
        }

        public bool ReserveTraversal()
        {
            if (!IsTraversable) return false;
            CurrentTraffic++;
            if (CurrentTraffic > Capacity) State = CorridorState.Congested;
            return true;
        }

        public bool ReleaseTraversal()
        {
            if (CurrentTraffic <= 0) return false;
            CurrentTraffic--;
            if (State == CorridorState.Congested && CurrentTraffic <= Capacity) State = CorridorState.Operational;
            return true;
        }

        public double CalculateTravelCost()
        {
            return BaseTraversalCost + (Length / MovementSpeed) * CongestionFactor;
        }
    }

    public sealed class CorridorRegistry
    {
        private readonly Dictionary<string, CorridorDefinition> definitions = new Dictionary<string, CorridorDefinition>();
        public int Count => definitions.Count;
        public bool RegisterDefinition(CorridorDefinition definition)
        {
            if (definition == null || definitions.ContainsKey(definition.CorridorId)) return false;
            definitions.Add(definition.CorridorId, definition);
            return true;
        }
        public bool TryGetDefinition(string corridorId, out CorridorDefinition definition) => definitions.TryGetValue(corridorId, out definition);
    }

    public sealed class CorridorGraph
    {
        private readonly Dictionary<string, CorridorInstance> corridors = new Dictionary<string, CorridorInstance>();
        private readonly Dictionary<string, List<string>> corridorIdsByConnection = new Dictionary<string, List<string>>();

        public int Count => corridors.Count;

        public bool Add(CorridorInstance corridor)
        {
            if (corridor == null || corridors.ContainsKey(corridor.EntityId)) return false;
            corridors.Add(corridor.EntityId, corridor);
            AddConnection(corridor.SourceConnection, corridor.EntityId);
            AddConnection(corridor.DestinationConnection, corridor.EntityId);
            return true;
        }

        public bool Remove(string entityId)
        {
            if (!corridors.TryGetValue(entityId, out CorridorInstance corridor)) return false;
            corridors.Remove(entityId);
            RemoveConnection(corridor.SourceConnection, entityId);
            RemoveConnection(corridor.DestinationConnection, entityId);
            return true;
        }

        public bool TryGet(string entityId, out CorridorInstance corridor) => corridors.TryGetValue(entityId, out corridor);

        public IReadOnlyList<CorridorInstance> QueryCorridors()
        {
            List<CorridorInstance> result = new List<CorridorInstance>(corridors.Values);
            result.Sort((left, right) => string.CompareOrdinal(left.EntityId, right.EntityId));
            return result;
        }

        public IReadOnlyList<CorridorInstance> QueryByConnection(string connectionId)
        {
            List<CorridorInstance> result = new List<CorridorInstance>();
            if (!corridorIdsByConnection.TryGetValue(connectionId, out List<string> ids)) return result;
            for (int i = 0; i < ids.Count; i++)
            {
                result.Add(corridors[ids[i]]);
            }
            return result;
        }

        private void AddConnection(string connectionId, string corridorEntityId)
        {
            if (!corridorIdsByConnection.TryGetValue(connectionId, out List<string> ids))
            {
                ids = new List<string>();
                corridorIdsByConnection[connectionId] = ids;
            }
            ids.Add(corridorEntityId);
            ids.Sort(StringComparer.Ordinal);
        }

        private void RemoveConnection(string connectionId, string corridorEntityId)
        {
            if (corridorIdsByConnection.TryGetValue(connectionId, out List<string> ids))
            {
                ids.Remove(corridorEntityId);
            }
        }
    }

    public sealed class CorridorDiagnostics
    {
        public int RegisteredDefinitions { get; private set; }
        public int Created { get; private set; }
        public int Destroyed { get; private set; }
        public int Reservations { get; private set; }
        public int Releases { get; private set; }
        public int Congestions { get; private set; }
        public int Blocked { get; private set; }

        public void RecordDefinitions(int count) => RegisteredDefinitions = count;
        public void RecordCreated() => Created++;
        public void RecordDestroyed() => Destroyed++;
        public void RecordReservation() => Reservations++;
        public void RecordRelease() => Releases++;
        public void RecordCongestion() => Congestions++;
        public void RecordBlocked() => Blocked++;
    }

    public sealed class CorridorManager
    {
        private readonly CorridorRegistry registry = new CorridorRegistry();
        private readonly CorridorGraph graph = new CorridorGraph();
        private readonly IEventBus eventBus;
        private long counter;

        public CorridorDiagnostics Diagnostics { get; } = new CorridorDiagnostics();
        public int CorridorCount => graph.Count;

        public CorridorManager(IEventBus eventBus = null)
        {
            this.eventBus = eventBus;
        }

        public bool RegisterDefinition(CorridorDefinition definition)
        {
            bool registered = registry.RegisterDefinition(definition);
            if (registered) Diagnostics.RecordDefinitions(registry.Count);
            return registered;
        }

        public CorridorInstance CreateCorridor(string corridorDefinitionId, string sourceConnection, string destinationConnection, double length)
        {
            if (!registry.TryGetDefinition(corridorDefinitionId, out CorridorDefinition definition)) return null;
            CorridorInstance corridor = new CorridorInstance(corridorDefinitionId, corridorDefinitionId + "-" + (++counter), sourceConnection, destinationConnection, length, definition);
            corridor.ChangeState(CorridorState.Operational);
            graph.Add(corridor);
            Diagnostics.RecordCreated();
            eventBus?.Publish(new CorridorCreated(corridor.EntityId));
            eventBus?.Publish(new CorridorCompleted(corridor.EntityId));
            return corridor;
        }

        public bool DestroyCorridor(string entityId)
        {
            if (!graph.TryGet(entityId, out CorridorInstance corridor)) return false;
            corridor.ChangeState(CorridorState.Destroyed);
            graph.Remove(entityId);
            Diagnostics.RecordDestroyed();
            eventBus?.Publish(new CorridorDestroyed(entityId));
            return true;
        }

        public IReadOnlyList<CorridorInstance> QueryCorridors() => graph.QueryCorridors();

        public bool ReserveTraversal(string entityId)
        {
            if (!graph.TryGet(entityId, out CorridorInstance corridor)) return false;
            bool reserved = corridor.ReserveTraversal();
            if (reserved)
            {
                Diagnostics.RecordReservation();
                if (corridor.State == CorridorState.Congested)
                {
                    Diagnostics.RecordCongestion();
                    eventBus?.Publish(new CorridorCongested(entityId));
                }
            }
            return reserved;
        }

        public bool ReleaseTraversal(string entityId)
        {
            if (!graph.TryGet(entityId, out CorridorInstance corridor)) return false;
            bool released = corridor.ReleaseTraversal();
            if (released) Diagnostics.RecordRelease();
            return released;
        }

        public double CalculateTravelCost(string entityId)
        {
            return graph.TryGet(entityId, out CorridorInstance corridor) ? corridor.CalculateTravelCost() : double.PositiveInfinity;
        }

        public IReadOnlyList<CorridorInstance> DetectCongestion()
        {
            List<CorridorInstance> congested = new List<CorridorInstance>();
            IReadOnlyList<CorridorInstance> corridors = graph.QueryCorridors();
            for (int i = 0; i < corridors.Count; i++)
            {
                if (corridors[i].State == CorridorState.Congested) congested.Add(corridors[i]);
            }
            return congested;
        }

        public bool BlockCorridor(string entityId)
        {
            if (!graph.TryGet(entityId, out CorridorInstance corridor)) return false;
            bool changed = corridor.ChangeState(CorridorState.Blocked);
            if (changed) { Diagnostics.RecordBlocked(); eventBus?.Publish(new CorridorBlocked(entityId)); }
            return changed;
        }
    }

    public readonly struct CorridorCreated : IGameplayEvent, IBuildingEvent { public string EntityId { get; } public CorridorCreated(string entityId) { EntityId = entityId; } }
    public readonly struct CorridorCompleted : IGameplayEvent, IBuildingEvent { public string EntityId { get; } public CorridorCompleted(string entityId) { EntityId = entityId; } }
    public readonly struct CorridorBlocked : IGameplayEvent, IBuildingEvent { public string EntityId { get; } public CorridorBlocked(string entityId) { EntityId = entityId; } }
    public readonly struct CorridorCongested : IGameplayEvent, IBuildingEvent { public string EntityId { get; } public CorridorCongested(string entityId) { EntityId = entityId; } }
    public readonly struct CorridorCollapsed : IGameplayEvent, IBuildingEvent { public string EntityId { get; } public CorridorCollapsed(string entityId) { EntityId = entityId; } }
    public readonly struct CorridorDestroyed : IGameplayEvent, IBuildingEvent { public string EntityId { get; } public CorridorDestroyed(string entityId) { EntityId = entityId; } }
}
