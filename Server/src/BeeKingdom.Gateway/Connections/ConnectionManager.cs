using BeeKingdom.Gateway.Configuration;
using BeeKingdom.Gateway.Models;
using BeeKingdom.Infrastructure.Time;
using BeeKingdom.Shared.ValueObjects;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Gateway.Connections;

public sealed class ConnectionManager
{
    private readonly Dictionary<Guid, GatewayConnection> connections = new();
    private readonly IServerClock clock;
    private readonly GatewayOptions options;
    private readonly object sync = new();

    public ConnectionManager(IServerClock clock, IOptions<GatewayOptions> options)
    {
        this.clock = clock;
        this.options = options.Value;
    }

    public GatewayConnection AcceptConnection(GatewayConnectionRequest request)
    {
        lock (sync)
        {
            if (connections.Values.Count(connection => connection.ConnectionState != GatewayConnectionState.Disconnected) >= options.MaxConnections)
            {
                throw new InvalidOperationException("Gateway maximum connection count reached.");
            }

            GatewayConnection connection = new(
                Guid.NewGuid(),
                string.Empty,
                default,
                request.ClientVersion,
                request.ProtocolVersion,
                request.Region,
                0,
                request.IpAddress,
                GatewayConnectionState.Connecting,
                clock.UtcNow,
                clock.UtcNow);

            connections[connection.ConnectionId] = connection;
            return connection;
        }
    }

    public GatewayConnection Authenticate(Guid connectionId, string sessionId, PlayerId playerId)
    {
        lock (sync)
        {
            GatewayConnection connection = Require(connectionId);
            GatewayConnection updated = connection with
            {
                SessionId = sessionId,
                PlayerId = playerId,
                ConnectionState = GatewayConnectionState.Connected,
                LastActivityUtc = clock.UtcNow
            };
            connections[connectionId] = updated;
            return updated;
        }
    }

    public GatewayConnection? Get(Guid connectionId)
    {
        lock (sync)
        {
            return connections.TryGetValue(connectionId, out GatewayConnection? connection) ? connection : null;
        }
    }

    public IReadOnlyList<GatewayConnection> QueryConnections()
    {
        lock (sync)
        {
            return connections.Values.OrderBy(connection => connection.ConnectedAtUtc).ToArray();
        }
    }

    public bool Disconnect(Guid connectionId)
    {
        lock (sync)
        {
            if (!connections.TryGetValue(connectionId, out GatewayConnection? connection))
            {
                return false;
            }

            connections[connectionId] = connection with { ConnectionState = GatewayConnectionState.Disconnected, LastActivityUtc = clock.UtcNow };
            return true;
        }
    }

    public void Touch(Guid connectionId)
    {
        lock (sync)
        {
            GatewayConnection connection = Require(connectionId);
            connections[connectionId] = connection with { LastActivityUtc = clock.UtcNow };
        }
    }

    private GatewayConnection Require(Guid connectionId)
    {
        return connections.TryGetValue(connectionId, out GatewayConnection? connection)
            ? connection
            : throw new KeyNotFoundException($"Connection {connectionId} was not found.");
    }
}
