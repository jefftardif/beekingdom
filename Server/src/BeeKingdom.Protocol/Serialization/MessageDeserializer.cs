using System.Text.Json;
using BeeKingdom.Protocol.Messages;
using BeeKingdom.Shared.Serialization;

namespace BeeKingdom.Protocol.Serialization;

public sealed class MessageDeserializer : IMessageDeserializer
{
    public ProtocolMessage<TPayload>? Deserialize<TPayload>(ReadOnlySpan<byte> payload)
    {
        return JsonSerializer.Deserialize<ProtocolMessage<TPayload>>(payload, BeeJson.CreateDefaultOptions());
    }
}
