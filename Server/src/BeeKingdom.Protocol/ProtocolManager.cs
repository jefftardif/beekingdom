using System.Diagnostics;
using BeeKingdom.Protocol.Diagnostics;
using BeeKingdom.Protocol.Errors;
using BeeKingdom.Protocol.Messages;
using BeeKingdom.Protocol.Registry;
using BeeKingdom.Protocol.Serialization;
using BeeKingdom.Protocol.Validation;
using BeeKingdom.Protocol.Versioning;

namespace BeeKingdom.Protocol;

public sealed class ProtocolManager
{
    private readonly IMessageSerializer serializer;
    private readonly IMessageDeserializer deserializer;
    private readonly ProtocolValidator validator;
    private readonly MessageRegistry registry = new();

    public ProtocolManager()
        : this(new MessageSerializer(), new MessageDeserializer(), new ProtocolVersionManager())
    {
    }

    public ProtocolManager(IMessageSerializer serializer, IMessageDeserializer deserializer, ProtocolVersionManager versionManager)
    {
        this.serializer = serializer;
        this.deserializer = deserializer;
        VersionManager = versionManager;
        validator = new ProtocolValidator(versionManager);
    }

    public ProtocolVersionManager VersionManager { get; }
    public ProtocolDiagnostics Diagnostics { get; } = new();

    public byte[] Serialize<TPayload>(ProtocolMessage<TPayload> message)
    {
        long start = Stopwatch.GetTimestamp();
        byte[] bytes = serializer.Serialize(message);
        ProtocolValidationResult result = Validate(message, bytes.Length);
        if (!result.IsValid)
        {
            Diagnostics.RecordError(result.ErrorCode);
            throw new InvalidOperationException(string.Join("; ", result.Errors));
        }

        Diagnostics.RecordMessage(message.MessageType, bytes.Length, Stopwatch.GetTimestamp() - start);
        return bytes;
    }

    public ProtocolMessage<TPayload>? Deserialize<TPayload>(ReadOnlySpan<byte> payload)
    {
        ProtocolMessage<TPayload>? message = deserializer.Deserialize<TPayload>(payload);
        Diagnostics.RecordDeserialize(payload.Length);
        ProtocolValidationResult result = Validate(message, payload.Length);
        if (!result.IsValid)
        {
            Diagnostics.RecordError(result.ErrorCode);
            return null;
        }

        return message;
    }

    public ProtocolValidationResult Validate<TPayload>(ProtocolMessage<TPayload>? message, int payloadBytes)
    {
        return validator.Validate(message, payloadBytes);
    }

    public void RegisterMessage(string messageName, ProtocolMessageType messageType)
    {
        registry.RegisterMessage(messageName, messageType);
    }

    public ProtocolVersion GetProtocolVersion()
    {
        return VersionManager.Current;
    }

    public ProtocolVersion NegotiateVersion(IEnumerable<ProtocolVersion> clientSupportedVersions)
    {
        ProtocolVersion version = VersionManager.NegotiateVersion(clientSupportedVersions);
        if (version == default)
        {
            Diagnostics.RecordError(ProtocolErrorCode.UnsupportedVersion);
        }

        return version;
    }
}
