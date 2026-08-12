using BeeKingdom.Gateway.Models;
using BeeKingdom.Protocol.Messages;

namespace BeeKingdom.Gateway.Routing;

public sealed class RequestRouter
{
    private readonly Dictionary<ProtocolMessageType, GatewayServiceTarget> routeByMessageType = new();

    public RequestRouter()
    {
        routeByMessageType[ProtocolMessageType.Request] = GatewayServiceTarget.Account;
        routeByMessageType[ProtocolMessageType.Command] = GatewayServiceTarget.Simulation;
        routeByMessageType[ProtocolMessageType.Event] = GatewayServiceTarget.Notification;
        routeByMessageType[ProtocolMessageType.Notification] = GatewayServiceTarget.Notification;
        routeByMessageType[ProtocolMessageType.Heartbeat] = GatewayServiceTarget.Authentication;
    }

    public void RegisterRoute(ProtocolMessageType messageType, GatewayServiceTarget target)
    {
        routeByMessageType[messageType] = target;
    }

    public GatewayRouteResult Resolve(ProtocolMessageType messageType)
    {
        return routeByMessageType.TryGetValue(messageType, out GatewayServiceTarget target)
            ? GatewayRouteResult.Success(target)
            : GatewayRouteResult.Failure("route_not_found");
    }
}
