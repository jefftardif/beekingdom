using BeeKingdom.Protocol.Errors;
using BeeKingdom.Protocol.Messages;
using BeeKingdom.Protocol.Versioning;

namespace BeeKingdom.Protocol.Validation;

public sealed class ProtocolValidator
{
    private readonly ProtocolVersionManager versionManager;
    private readonly int maxPayloadBytes;

    public ProtocolValidator(ProtocolVersionManager versionManager, int maxPayloadBytes = 64 * 1024)
    {
        this.versionManager = versionManager;
        this.maxPayloadBytes = maxPayloadBytes;
    }

    public ProtocolValidationResult Validate<TPayload>(ProtocolMessage<TPayload>? message, int payloadBytes)
    {
        if (message == null)
        {
            return ProtocolValidationResult.Invalid(ProtocolErrorCode.InvalidMessage, "Message is required.");
        }

        if (!versionManager.IsSupported(message.ProtocolVersion))
        {
            return ProtocolValidationResult.Invalid(ProtocolErrorCode.UnsupportedVersion, $"Unsupported protocol version {message.ProtocolVersion}.");
        }

        if (payloadBytes <= 0 || payloadBytes > maxPayloadBytes)
        {
            return ProtocolValidationResult.Invalid(ProtocolErrorCode.InvalidMessage, $"Payload size must be between 1 and {maxPayloadBytes} bytes.");
        }

        if (message.MessageId == Guid.Empty)
        {
            return ProtocolValidationResult.Invalid(ProtocolErrorCode.InvalidMessage, "MessageId is required.");
        }

        if (message.CorrelationId == Guid.Empty)
        {
            return ProtocolValidationResult.Invalid(ProtocolErrorCode.InvalidMessage, "CorrelationId is required.");
        }

        if (message.TraceId == Guid.Empty)
        {
            return ProtocolValidationResult.Invalid(ProtocolErrorCode.InvalidMessage, "TraceId is required.");
        }

        if (string.IsNullOrWhiteSpace(message.SessionId))
        {
            return ProtocolValidationResult.Invalid(ProtocolErrorCode.Unauthorized, "SessionId is required.");
        }

        if (message.PlayerId.Value == Guid.Empty)
        {
            return ProtocolValidationResult.Invalid(ProtocolErrorCode.Unauthorized, "PlayerId is required.");
        }

        if (message.ColonyId.Value == Guid.Empty)
        {
            return ProtocolValidationResult.Invalid(ProtocolErrorCode.ValidationError, "ColonyId is required.");
        }

        if (message.Payload == null)
        {
            return ProtocolValidationResult.Invalid(ProtocolErrorCode.InvalidMessage, "Payload is required.");
        }

        return ProtocolValidationResult.Valid;
    }
}
