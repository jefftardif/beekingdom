using BeeKingdom.Protocol.Messages;
using BeeKingdom.Protocol.Versioning;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Gateway.Models;

public enum GatewayServiceTarget
{
    Authentication = 0,
    Account = 1,
    Colony = 2,
    World = 3,
    Simulation = 4,
    Chat = 5,
    Alliance = 6,
    Notification = 7,
    LiveOps = 8,
    Administration = 9
}

public enum GatewayConnectionState
{
    Connecting = 0,
    Authenticating = 1,
    Connected = 2,
    Idle = 3,
    Disconnecting = 4,
    Disconnected = 5
}

public sealed record GatewayConnection(
    Guid ConnectionId,
    string SessionId,
    PlayerId PlayerId,
    string ClientVersion,
    ProtocolVersion ProtocolVersion,
    string Region,
    double LatencyMilliseconds,
    string IpAddress,
    GatewayConnectionState ConnectionState,
    DateTimeOffset ConnectedAtUtc,
    DateTimeOffset LastActivityUtc);

public sealed record GatewayConnectionRequest(string ClientVersion, ProtocolVersion ProtocolVersion, string Region, string IpAddress);

public sealed record GatewayRoute(string RouteKey, GatewayServiceTarget Target, ProtocolMessageType MessageType);

public sealed record GatewayRouteResult(bool Succeeded, GatewayServiceTarget? Target, string? ErrorCode)
{
    public static GatewayRouteResult Success(GatewayServiceTarget target) => new(true, target, null);
    public static GatewayRouteResult Failure(string errorCode) => new(false, null, errorCode);
}

public sealed record GatewayStatistics(long ActiveConnections, long NewConnections, long Disconnections, double AverageLatency, long BandwidthBytes, long MessagesRouted, long RoutingErrors);
