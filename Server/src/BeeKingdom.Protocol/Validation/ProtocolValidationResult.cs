using BeeKingdom.Protocol.Errors;

namespace BeeKingdom.Protocol.Validation;

public sealed record ProtocolValidationResult(bool IsValid, ProtocolErrorCode ErrorCode, IReadOnlyList<string> Errors)
{
    public static ProtocolValidationResult Valid { get; } = new(true, ProtocolErrorCode.None, Array.Empty<string>());

    public static ProtocolValidationResult Invalid(ProtocolErrorCode errorCode, params string[] errors)
    {
        return new ProtocolValidationResult(false, errorCode, errors);
    }
}
