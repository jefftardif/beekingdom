using System.Text.Json;
using BeeKingdom.Protocol.Messages;
using BeeKingdom.Shared.Serialization;

namespace BeeKingdom.Protocol.Serialization;

public sealed class MessageSerializer : IMessageSerializer
{
    public byte[] Serialize<TPayload>(ProtocolMessage<TPayload> message)
    {
        return JsonSerializer.SerializeToUtf8Bytes(message, BeeJson.CreateDefaultOptions());
    }
}
