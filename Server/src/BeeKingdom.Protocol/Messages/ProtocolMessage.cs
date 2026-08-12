using BeeKingdom.Protocol.Versioning;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Protocol.Messages;

public sealed record ProtocolMessage<TPayload>(
    ProtocolVersion ProtocolVersion,
    Guid MessageId,
    ProtocolMessageType MessageType,
    Guid CorrelationId,
    Guid TraceId,
    DateTimeOffset TimestampUtc,
    string SessionId,
    PlayerId PlayerId,
    ColonyId ColonyId,
    TPayload Payload)
{
    public static ProtocolMessage<TPayload> Create(
        ProtocolMessageType messageType,
        string sessionId,
        PlayerId playerId,
        ColonyId colonyId,
        TPayload payload,
        Guid? correlationId = null,
        Guid? traceId = null)
    {
        return new ProtocolMessage<TPayload>(
            ProtocolVersion.Current,
            Guid.NewGuid(),
            messageType,
            correlationId ?? Guid.NewGuid(),
            traceId ?? Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            sessionId,
            playerId,
            colonyId,
            payload);
    }
}
