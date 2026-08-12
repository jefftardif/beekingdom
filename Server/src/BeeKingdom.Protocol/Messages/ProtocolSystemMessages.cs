using BeeKingdom.Protocol.Errors;
using BeeKingdom.Protocol.Versioning;

namespace BeeKingdom.Protocol.Messages;

public sealed record HeartbeatPayload(DateTimeOffset ClientTimeUtc);

public sealed record AcknowledgementPayload(Guid AcknowledgedMessageId, DateTimeOffset AcknowledgedAtUtc);

public sealed record ErrorPayload(ProtocolErrorCode ErrorCode, string Message, IReadOnlyList<string> Details, ProtocolVersion ProtocolVersion);
