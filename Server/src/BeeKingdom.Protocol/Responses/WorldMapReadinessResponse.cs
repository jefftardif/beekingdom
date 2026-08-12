namespace BeeKingdom.Protocol.Responses;

public sealed record WorldMapReadinessResponse(
    string Service,
    DateTimeOffset ServerTimeUtc,
    string Environment,
    string GameServerId,
    string DefaultWorldId,
    string ShardName,
    string ProductionTarget,
    string WorldMapStatus,
    string WorldMapBoundary,
    bool ReadOnly,
    bool NonLive,
    bool ProductionRouteProven,
    bool MapGameplayEnabled,
    bool LiveTerritoryEnabled,
    bool LiveAllianceEnabled,
    bool LiveScoutingEnabled,
    bool LiveWarEnabled,
    bool LiveEconomyEnabled,
    bool RealTimeSynchronizationEnabled,
    bool OfficialProgressionEnabled,
    IReadOnlyList<WorldMapNodeModel> NodeModels,
    WorldMapForbiddenClaims ForbiddenClaims,
    IReadOnlyList<string> Blockers);

public sealed record WorldMapNodeModel(
    string NodeType,
    string DraftStatus,
    bool WorldScoped,
    bool GameServerScoped,
    bool ReadOnly,
    bool LiveClaimAllowed,
    string Purpose);

public sealed record WorldMapForbiddenClaims(
    bool LiveWorldMap,
    bool OfficialTerritory,
    bool ActiveAlliance,
    bool LiveScouting,
    bool LiveFlightPath,
    bool LiveWar,
    bool LivePvp,
    bool LiveEconomy,
    bool Ranking,
    bool Matchmaking,
    bool RealTimeSynchronization);
