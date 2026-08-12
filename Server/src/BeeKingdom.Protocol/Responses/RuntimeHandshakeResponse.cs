using BeeKingdom.Protocol.Versioning;

namespace BeeKingdom.Protocol.Responses;

public sealed record RuntimeHandshakeResponse(
    string Service,
    DateTimeOffset ServerTimeUtc,
    string Environment,
    string GameServerId,
    string DefaultWorldId,
    string ShardName,
    ProtocolVersion ProtocolVersion,
    bool ClientProtocolCompatible,
    string Availability,
    string MaintenanceMessage,
    string FallbackMode,
    bool NonGameplay,
    bool GameplayAuthorityGranted,
    bool MutationAllowed,
    bool RequiresAccount,
    RuntimeHandshakeLiveClaims LiveClaims);

public sealed record RuntimeHandshakeLiveClaims(
    bool Accounts,
    bool Sessions,
    bool Persistence,
    bool RealTimeSynchronization,
    bool Economy,
    bool Social,
    bool Ranking,
    bool Matchmaking);
