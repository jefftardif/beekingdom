using BeeKingdom.Authentication.Models;
using BeeKingdom.Gateway.Connections;
using BeeKingdom.Gateway.Configuration;
using BeeKingdom.Gateway.Diagnostics;
using BeeKingdom.Gateway.Events;
using BeeKingdom.Gateway.Models;
using BeeKingdom.Gateway.RateLimiting;
using BeeKingdom.Gateway.Routing;
using BeeKingdom.Infrastructure.Time;
using BeeKingdom.Protocol;
using BeeKingdom.Protocol.Messages;
using BeeKingdom.Protocol.Validation;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Gateway;

public sealed class GatewayManager
{
    private readonly ConnectionManager connections;
    private readonly SessionRouter sessions;
    private readonly RequestRouter requests;
    private readonly GatewayRateLimiter rateLimiter;
    private readonly ProtocolManager protocol;
    private readonly IGatewayEventSink events;
    private readonly IServerClock clock;
    private readonly GatewayOptions options;

    public GatewayManager(ConnectionManager connections, SessionRouter sessions, RequestRouter requests, GatewayRateLimiter rateLimiter, ProtocolManager protocol, IGatewayEventSink events, IServerClock clock, IOptions<GatewayOptions> options)
    {
        this.connections = connections;
        this.sessions = sessions;
        this.requests = requests;
        this.rateLimiter = rateLimiter;
        this.protocol = protocol;
        this.events = events;
        this.clock = clock;
        this.options = options.Value;
    }

    public GatewayDiagnostics Diagnostics { get; } = new();

    public GatewayConnection AcceptConnection(GatewayConnectionRequest request)
    {
        GatewayConnection connection = connections.AcceptConnection(request);
        Diagnostics.RecordConnection(connection.LatencyMilliseconds);
        events.Publish(new ClientConnected(clock.UtcNow, connection.ConnectionId));
        return connection;
    }

    public GatewayConnection AuthenticateSession(Guid connectionId, string accessToken)
    {
        TokenValidationResult result = sessions.ValidateSession(accessToken);
        if (!result.IsValid)
        {
            throw new UnauthorizedAccessException(result.ErrorCode);
        }

        GatewayConnection connection = connections.Authenticate(connectionId, result.SessionId, result.PlayerId);
        events.Publish(new ClientAuthenticated(clock.UtcNow, connection.ConnectionId, result.PlayerId, result.SessionId));
        return connection;
    }

    public GatewayRouteResult RouteMessage<TPayload>(Guid connectionId, ProtocolMessage<TPayload> message, int payloadBytes)
    {
        GatewayConnection? connection = connections.Get(connectionId);
        if (connection == null || connection.ConnectionState is GatewayConnectionState.Disconnected or GatewayConnectionState.Disconnecting)
        {
            Diagnostics.RecordRoutingError();
            return GatewayRouteResult.Failure("connection_invalid");
        }

        if (payloadBytes > options.MaxMessageBytes)
        {
            Diagnostics.RecordRoutingError();
            events.Publish(new InvalidMessageDetected(clock.UtcNow, connectionId, "message_too_large"));
            return GatewayRouteResult.Failure("message_too_large");
        }

        ProtocolValidationResult validation = protocol.Validate(message, payloadBytes);
        if (!validation.IsValid)
        {
            Diagnostics.RecordRoutingError();
            events.Publish(new InvalidMessageDetected(clock.UtcNow, connectionId, validation.ErrorCode.ToString()));
            return GatewayRouteResult.Failure(validation.ErrorCode.ToString());
        }

        if (message.SessionId != connection.SessionId || message.PlayerId != connection.PlayerId)
        {
            Diagnostics.RecordRoutingError();
            events.Publish(new InvalidMessageDetected(clock.UtcNow, connectionId, "session_mismatch"));
            return GatewayRouteResult.Failure("session_mismatch");
        }

        if (!rateLimiter.IsAllowed(connection, message.MessageType, out string scope))
        {
            Diagnostics.RecordRoutingError();
            events.Publish(new RateLimitExceeded(clock.UtcNow, connectionId, scope));
            return GatewayRouteResult.Failure("rate_limited");
        }

        GatewayRouteResult route = requests.Resolve(message.MessageType);
        if (!route.Succeeded || route.Target == null)
        {
            Diagnostics.RecordRoutingError();
            return route;
        }

        connections.Touch(connectionId);
        Diagnostics.RecordMessage(payloadBytes);
        events.Publish(new MessageRouted(clock.UtcNow, connectionId, message.MessageType, route.Target.Value));
        return route;
    }

    public bool Disconnect(Guid connectionId)
    {
        bool disconnected = connections.Disconnect(connectionId);
        if (disconnected)
        {
            Diagnostics.RecordDisconnection();
            events.Publish(new ClientDisconnected(clock.UtcNow, connectionId));
        }

        return disconnected;
    }

    public IReadOnlyList<GatewayConnection> QueryConnections() => connections.QueryConnections();

    public GatewayStatistics GetGatewayStatistics()
    {
        return new GatewayStatistics(Diagnostics.ActiveConnections, Diagnostics.NewConnections, Diagnostics.Disconnections, Diagnostics.AverageLatency, Diagnostics.BandwidthBytes, Diagnostics.MessagesRouted, Diagnostics.RoutingErrors);
    }
}
