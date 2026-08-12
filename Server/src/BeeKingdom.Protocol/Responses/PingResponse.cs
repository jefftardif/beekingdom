using BeeKingdom.Protocol.Versioning;

namespace BeeKingdom.Protocol.Responses;

public sealed record PingResponse(
    string ServerName,
    ProtocolVersion ProtocolVersion,
    DateTimeOffset ServerTimeUtc,
    string Environment);
