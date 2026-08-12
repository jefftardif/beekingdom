using BeeKingdom.Protocol.Messages;

namespace BeeKingdom.Protocol.Registry;

public sealed class MessageRegistry
{
    private readonly Dictionary<string, ProtocolMessageType> messageTypes = new(StringComparer.Ordinal);

    public void RegisterMessage(string messageName, ProtocolMessageType messageType)
    {
        if (string.IsNullOrWhiteSpace(messageName))
        {
            throw new ArgumentException("Message name is required.", nameof(messageName));
        }

        messageTypes[messageName] = messageType;
    }

    public bool TryGetMessageType(string messageName, out ProtocolMessageType messageType)
    {
        return messageTypes.TryGetValue(messageName, out messageType);
    }
}
