using BeeKingdom.Protocol.Messages;

namespace BeeKingdom.Protocol.Serialization;

public interface IMessageDeserializer
{
    ProtocolMessage<TPayload>? Deserialize<TPayload>(ReadOnlySpan<byte> payload);
}
