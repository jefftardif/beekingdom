using BeeKingdom.Gateway.Models;
using BeeKingdom.Protocol.Messages;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Gateway.Events;

public interface IGatewayEvent
{
    DateTimeOffset OccurredAtUtc { get; }
    Guid ConnectionId { get; }
}

public sealed record ClientConnected(DateTimeOffset OccurredAtUtc, Guid ConnectionId) : IGatewayEvent;
public sealed record ClientAuthenticated(DateTimeOffset OccurredAtUtc, Guid ConnectionId, PlayerId PlayerId, string SessionId) : IGatewayEvent;
public sealed record ClientDisconnected(DateTimeOffset OccurredAtUtc, Guid ConnectionId) : IGatewayEvent;
public sealed record MessageRouted(DateTimeOffset OccurredAtUtc, Guid ConnectionId, ProtocolMessageType MessageType, GatewayServiceTarget Target) : IGatewayEvent;
public sealed record RateLimitExceeded(DateTimeOffset OccurredAtUtc, Guid ConnectionId, string Scope) : IGatewayEvent;
public sealed record InvalidMessageDetected(DateTimeOffset OccurredAtUtc, Guid ConnectionId, string Reason) : IGatewayEvent;

public interface IGatewayEventSink
{
    void Publish(IGatewayEvent gatewayEvent);
}

public sealed class InMemoryGatewayEventSink : IGatewayEventSink
{
    private readonly List<IGatewayEvent> events = new();
    public IReadOnlyList<IGatewayEvent> Events => events;
    public void Publish(IGatewayEvent gatewayEvent) => events.Add(gatewayEvent);
}
