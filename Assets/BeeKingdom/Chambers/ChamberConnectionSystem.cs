using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Chambers
{
    public enum ChamberConnectionType { Corridor, Direct, Vertical, Surface, Restricted, OneWay, Temporary }
    public enum ChamberConnectionState { Disconnected, Planned, UnderConstruction, Connected, Blocked, Collapsed, Destroyed }
    public enum ConnectionValidationStatus { Valid, InvalidEndpoint, Duplicate, TooFar, InvalidCapacity, ForbiddenLoop }

    public sealed class ChamberConnectionDefinition
    {
        public string DefinitionId { get; }
        public ChamberConnectionType ConnectionType { get; }
        public double MaxDistance { get; }
        public int DefaultCapacity { get; }
        public double DefaultTraversalCost { get; }
        public bool AllowSelfLoop { get; }

        public ChamberConnectionDefinition(string definitionId, ChamberConnectionType connectionType, double maxDistance, int defaultCapacity, double defaultTraversalCost, bool allowSelfLoop = false)
        {
            DefinitionId = string.IsNullOrWhiteSpace(definitionId) ? throw new ArgumentException("Definition id is required.", nameof(definitionId)) : definitionId;
            ConnectionType = connectionType;
            MaxDistance = maxDistance <= 0d ? 1d : maxDistance;
            DefaultCapacity = defaultCapacity <= 0 ? 1 : defaultCapacity;
            DefaultTraversalCost = defaultTraversalCost <= 0d ? 1d : defaultTraversalCost;
            AllowSelfLoop = allowSelfLoop;
        }
    }

    public sealed class ChamberConnection
    {
        public string ConnectionId { get; }
        public string SourceChamberId { get; }
        public string DestinationChamberId { get; }
        public ChamberConnectionType ConnectionType { get; }
        public double Distance { get; }
        public int Capacity { get; }
        public double TraversalCost { get; }
        public int CurrentLoad { get; private set; }
        public ChamberConnectionState State { get; private set; }

        public ChamberConnection(string connectionId, string sourceChamberId, string destinationChamberId, ChamberConnectionType connectionType, double distance, int capacity, double traversalCost, ChamberConnectionState state = ChamberConnectionState.Planned)
        {
            ConnectionId = string.IsNullOrWhiteSpace(connectionId) ? throw new ArgumentException("Connection id is required.", nameof(connectionId)) : connectionId;
            SourceChamberId = sourceChamberId ?? string.Empty;
            DestinationChamberId = destinationChamberId ?? string.Empty;
            ConnectionType = connectionType;
            Distance = distance < 0d ? 0d : distance;
            Capacity = capacity <= 0 ? 1 : capacity;
            TraversalCost = traversalCost <= 0d ? 1d : traversalCost;
            State = state;
        }

        public bool IsTraversable => State == ChamberConnectionState.Connected && CurrentLoad < Capacity;

        public bool ChangeState(ChamberConnectionState state)
        {
            if (State == ChamberConnectionState.Destroyed && state != ChamberConnectionState.Destroyed) return false;
            State = state;
            return true;
        }

        public bool TryReserveLoad()
        {
            if (!IsTraversable) return false;
            CurrentLoad++;
            return true;
        }

        public bool ReleaseLoad()
        {
            if (CurrentLoad <= 0) return false;
            CurrentLoad--;
            return true;
        }
    }

    public readonly struct ConnectionValidationResult
    {
        public bool IsValid { get; }
        public ConnectionValidationStatus Status { get; }

        public ConnectionValidationResult(bool isValid, ConnectionValidationStatus status)
        {
            IsValid = isValid;
            Status = status;
        }
    }

    public sealed class ChamberGraph
    {
        private readonly Dictionary<string, ChamberConnection> connectionsById = new Dictionary<string, ChamberConnection>();
        private readonly Dictionary<string, List<string>> connectionIdsBySource = new Dictionary<string, List<string>>();

        public int ConnectionCount => connectionsById.Count;

        public bool Add(ChamberConnection connection)
        {
            if (connection == null || connectionsById.ContainsKey(connection.ConnectionId)) return false;
            connectionsById.Add(connection.ConnectionId, connection);
            AddSource(connection.SourceChamberId, connection.ConnectionId);
            if (connection.ConnectionType != ChamberConnectionType.OneWay)
            {
                AddSource(connection.DestinationChamberId, connection.ConnectionId);
            }
            return true;
        }

        public bool Remove(string connectionId)
        {
            if (!connectionsById.TryGetValue(connectionId, out ChamberConnection connection)) return false;
            connectionsById.Remove(connectionId);
            RemoveSource(connection.SourceChamberId, connectionId);
            RemoveSource(connection.DestinationChamberId, connectionId);
            return true;
        }

        public bool TryGet(string connectionId, out ChamberConnection connection) => connectionsById.TryGetValue(connectionId, out connection);

        public IReadOnlyList<ChamberConnection> QueryConnections()
        {
            List<ChamberConnection> result = new List<ChamberConnection>(connectionsById.Values);
            result.Sort((left, right) => string.CompareOrdinal(left.ConnectionId, right.ConnectionId));
            return result;
        }

        public IReadOnlyList<string> QueryNeighbours(string chamberId)
        {
            List<string> result = new List<string>();
            if (!connectionIdsBySource.TryGetValue(chamberId, out List<string> ids)) return result;

            for (int i = 0; i < ids.Count; i++)
            {
                ChamberConnection connection = connectionsById[ids[i]];
                if (!connection.IsTraversable) continue;
                if (connection.SourceChamberId == chamberId) result.Add(connection.DestinationChamberId);
                else if (connection.ConnectionType != ChamberConnectionType.OneWay) result.Add(connection.SourceChamberId);
            }

            result.Sort(StringComparer.Ordinal);
            return result;
        }

        public IReadOnlyList<string> FindShortestPath(string sourceChamberId, string destinationChamberId)
        {
            HashSet<string> visited = new HashSet<string>();
            Dictionary<string, double> distance = new Dictionary<string, double>();
            Dictionary<string, string> previous = new Dictionary<string, string>();
            SortedSet<PathNode> queue = new SortedSet<PathNode>();

            distance[sourceChamberId] = 0d;
            queue.Add(new PathNode(sourceChamberId, 0d));

            while (queue.Count > 0)
            {
                PathNode current = queue.Min;
                queue.Remove(current);
                if (!visited.Add(current.ChamberId)) continue;
                if (current.ChamberId == destinationChamberId) break;

                if (!connectionIdsBySource.TryGetValue(current.ChamberId, out List<string> ids)) continue;
                for (int i = 0; i < ids.Count; i++)
                {
                    ChamberConnection connection = connectionsById[ids[i]];
                    if (!connection.IsTraversable) continue;
                    string neighbour = GetNeighbour(connection, current.ChamberId);
                    if (string.IsNullOrEmpty(neighbour) || visited.Contains(neighbour)) continue;

                    double candidate = current.Distance + connection.TraversalCost;
                    if (!distance.TryGetValue(neighbour, out double best) || candidate < best)
                    {
                        distance[neighbour] = candidate;
                        previous[neighbour] = current.ChamberId;
                        queue.Add(new PathNode(neighbour, candidate));
                    }
                }
            }

            if (!distance.ContainsKey(destinationChamberId)) return Array.Empty<string>();
            List<string> path = new List<string>();
            string step = destinationChamberId;
            while (!string.IsNullOrEmpty(step))
            {
                path.Add(step);
                if (step == sourceChamberId) break;
                previous.TryGetValue(step, out step);
            }
            path.Reverse();
            return path;
        }

        public void RebuildGraph(IReadOnlyList<ChamberConnection> connections)
        {
            connectionsById.Clear();
            connectionIdsBySource.Clear();
            for (int i = 0; i < connections.Count; i++)
            {
                Add(connections[i]);
            }
        }

        private void AddSource(string chamberId, string connectionId)
        {
            if (!connectionIdsBySource.TryGetValue(chamberId, out List<string> ids))
            {
                ids = new List<string>();
                connectionIdsBySource[chamberId] = ids;
            }
            ids.Add(connectionId);
            ids.Sort(StringComparer.Ordinal);
        }

        private void RemoveSource(string chamberId, string connectionId)
        {
            if (connectionIdsBySource.TryGetValue(chamberId, out List<string> ids))
            {
                ids.Remove(connectionId);
            }
        }

        private static string GetNeighbour(ChamberConnection connection, string chamberId)
        {
            if (connection.SourceChamberId == chamberId) return connection.DestinationChamberId;
            if (connection.ConnectionType != ChamberConnectionType.OneWay && connection.DestinationChamberId == chamberId) return connection.SourceChamberId;
            return string.Empty;
        }

        private readonly struct PathNode : IComparable<PathNode>
        {
            public string ChamberId { get; }
            public double Distance { get; }

            public PathNode(string chamberId, double distance)
            {
                ChamberId = chamberId;
                Distance = distance;
            }

            public int CompareTo(PathNode other)
            {
                int distanceCompare = Distance.CompareTo(other.Distance);
                return distanceCompare != 0 ? distanceCompare : string.CompareOrdinal(ChamberId, other.ChamberId);
            }
        }
    }

    public sealed class ConnectionValidator
    {
        public ConnectionValidationResult ValidateConnection(ChamberConnectionDefinition definition, string sourceChamberId, string destinationChamberId, double distance, int capacity, ChamberGraph graph)
        {
            if (definition == null || string.IsNullOrWhiteSpace(sourceChamberId) || string.IsNullOrWhiteSpace(destinationChamberId))
            {
                return new ConnectionValidationResult(false, ConnectionValidationStatus.InvalidEndpoint);
            }
            if (!definition.AllowSelfLoop && sourceChamberId == destinationChamberId)
            {
                return new ConnectionValidationResult(false, ConnectionValidationStatus.ForbiddenLoop);
            }
            if (distance > definition.MaxDistance)
            {
                return new ConnectionValidationResult(false, ConnectionValidationStatus.TooFar);
            }
            if (capacity <= 0)
            {
                return new ConnectionValidationResult(false, ConnectionValidationStatus.InvalidCapacity);
            }

            IReadOnlyList<ChamberConnection> existing = graph.QueryConnections();
            for (int i = 0; i < existing.Count; i++)
            {
                ChamberConnection connection = existing[i];
                bool sameDirection = connection.SourceChamberId == sourceChamberId && connection.DestinationChamberId == destinationChamberId;
                bool reverseDirection = connection.SourceChamberId == destinationChamberId && connection.DestinationChamberId == sourceChamberId;
                if (sameDirection || (definition.ConnectionType != ChamberConnectionType.OneWay && reverseDirection))
                {
                    return new ConnectionValidationResult(false, ConnectionValidationStatus.Duplicate);
                }
            }

            return new ConnectionValidationResult(true, ConnectionValidationStatus.Valid);
        }
    }

    public sealed class ConnectionDiagnostics
    {
        public int Created { get; private set; }
        public int Removed { get; private set; }
        public int Blocked { get; private set; }
        public int Restored { get; private set; }
        public int GraphUpdates { get; private set; }
        public int ValidationFailures { get; private set; }

        public void RecordCreated() => Created++;
        public void RecordRemoved() => Removed++;
        public void RecordBlocked() => Blocked++;
        public void RecordRestored() => Restored++;
        public void RecordGraphUpdate() => GraphUpdates++;
        public void RecordValidation(bool valid) { if (!valid) ValidationFailures++; }
    }

    public sealed class ChamberConnectionManager
    {
        private readonly Dictionary<string, ChamberConnectionDefinition> definitions = new Dictionary<string, ChamberConnectionDefinition>();
        private readonly ChamberGraph graph = new ChamberGraph();
        private readonly ConnectionValidator validator = new ConnectionValidator();
        private readonly IEventBus eventBus;
        private long counter;

        public ConnectionDiagnostics Diagnostics { get; } = new ConnectionDiagnostics();
        public int ConnectionCount => graph.ConnectionCount;

        public ChamberConnectionManager(IEventBus eventBus = null)
        {
            this.eventBus = eventBus;
        }

        public bool RegisterDefinition(ChamberConnectionDefinition definition)
        {
            if (definition == null || definitions.ContainsKey(definition.DefinitionId)) return false;
            definitions.Add(definition.DefinitionId, definition);
            return true;
        }

        public ConnectionValidationResult ValidateConnection(string definitionId, string sourceChamberId, string destinationChamberId, double distance, int capacity)
        {
            definitions.TryGetValue(definitionId, out ChamberConnectionDefinition definition);
            ConnectionValidationResult result = validator.ValidateConnection(definition, sourceChamberId, destinationChamberId, distance, capacity, graph);
            Diagnostics.RecordValidation(result.IsValid);
            return result;
        }

        public ChamberConnection ConnectChambers(string definitionId, string sourceChamberId, string destinationChamberId, double distance, int capacity = 0, double traversalCost = 0d)
        {
            if (!definitions.TryGetValue(definitionId, out ChamberConnectionDefinition definition)) return null;
            int resolvedCapacity = capacity <= 0 ? definition.DefaultCapacity : capacity;
            double resolvedCost = traversalCost <= 0d ? definition.DefaultTraversalCost : traversalCost;
            ConnectionValidationResult result = ValidateConnection(definitionId, sourceChamberId, destinationChamberId, distance, resolvedCapacity);
            if (!result.IsValid) return null;

            ChamberConnection connection = new ChamberConnection("connection-" + (++counter), sourceChamberId, destinationChamberId, definition.ConnectionType, distance, resolvedCapacity, resolvedCost, ChamberConnectionState.Connected);
            graph.Add(connection);
            Diagnostics.RecordCreated();
            Diagnostics.RecordGraphUpdate();
            eventBus?.Publish(new ConnectionCreated(connection.ConnectionId));
            eventBus?.Publish(new GraphUpdated(graph.ConnectionCount));
            return connection;
        }

        public bool DisconnectChambers(string connectionId)
        {
            bool removed = graph.Remove(connectionId);
            if (removed)
            {
                Diagnostics.RecordRemoved();
                Diagnostics.RecordGraphUpdate();
                eventBus?.Publish(new ConnectionRemoved(connectionId));
                eventBus?.Publish(new GraphUpdated(graph.ConnectionCount));
            }
            return removed;
        }

        public bool BlockConnection(string connectionId)
        {
            if (!graph.TryGet(connectionId, out ChamberConnection connection)) return false;
            bool changed = connection.ChangeState(ChamberConnectionState.Blocked);
            if (changed) { Diagnostics.RecordBlocked(); eventBus?.Publish(new ConnectionBlocked(connectionId)); }
            return changed;
        }

        public bool RestoreConnection(string connectionId)
        {
            if (!graph.TryGet(connectionId, out ChamberConnection connection)) return false;
            bool changed = connection.ChangeState(ChamberConnectionState.Connected);
            if (changed) { Diagnostics.RecordRestored(); eventBus?.Publish(new ConnectionRestored(connectionId)); }
            return changed;
        }

        public IReadOnlyList<ChamberConnection> QueryConnections() => graph.QueryConnections();
        public IReadOnlyList<string> QueryNeighbours(string chamberId) => graph.QueryNeighbours(chamberId);
        public IReadOnlyList<string> FindShortestPath(string sourceChamberId, string destinationChamberId) => graph.FindShortestPath(sourceChamberId, destinationChamberId);

        public void RebuildGraph(IReadOnlyList<ChamberConnection> connections)
        {
            graph.RebuildGraph(connections ?? Array.Empty<ChamberConnection>());
            Diagnostics.RecordGraphUpdate();
            eventBus?.Publish(new GraphUpdated(graph.ConnectionCount));
        }
    }

    public readonly struct ConnectionCreated : IGameplayEvent, IBuildingEvent { public string ConnectionId { get; } public ConnectionCreated(string connectionId) { ConnectionId = connectionId; } }
    public readonly struct ConnectionRemoved : IGameplayEvent, IBuildingEvent { public string ConnectionId { get; } public ConnectionRemoved(string connectionId) { ConnectionId = connectionId; } }
    public readonly struct ConnectionBlocked : IGameplayEvent, IBuildingEvent { public string ConnectionId { get; } public ConnectionBlocked(string connectionId) { ConnectionId = connectionId; } }
    public readonly struct ConnectionRestored : IGameplayEvent, IBuildingEvent { public string ConnectionId { get; } public ConnectionRestored(string connectionId) { ConnectionId = connectionId; } }
    public readonly struct GraphUpdated : IGameplayEvent, IBuildingEvent { public int ConnectionCount { get; } public GraphUpdated(int connectionCount) { ConnectionCount = connectionCount; } }
}
