using BeeKingdom.Protocol.Messages;

namespace BeeKingdom.Protocol.Serialization;

public interface IMessageSerializer
{
    byte[] Serialize<TPayload>(ProtocolMessage<TPayload> message);
}
