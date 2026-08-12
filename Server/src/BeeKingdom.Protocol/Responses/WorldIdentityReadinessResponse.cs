namespace BeeKingdom.Protocol.Responses;

public sealed record WorldIdentityReadinessResponse(
    string Service,
    DateTimeOffset ServerTimeUtc,
    string Environment,
    string GameServerId,
    string DefaultWorldId,
    string ShardName,
    bool GameServerIdValid,
    bool DefaultWorldIdValid,
    bool GameServerIdAndWorldIdDistinct,
    bool RequiresWorldScopeForAccounts,
    bool RequiresWorldScopeForColonies,
    bool RequiresWorldScopeForWorldMap,
    bool SingleWorldAssumptionAllowed,
    bool LiveWorldSelectionAllowed,
    bool OfficialProgressionAllowed,
    IReadOnlyList<WorldIdentityScope> RequiredScopes,
    IReadOnlyList<string> Blockers);

public sealed record WorldIdentityScope(
    string Domain,
    bool RequiresGameServerId,
    bool RequiresWorldId,
    string Status);
