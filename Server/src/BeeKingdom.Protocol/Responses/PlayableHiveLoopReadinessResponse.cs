namespace BeeKingdom.Protocol.Responses;

public sealed record PlayableHiveLoopReadinessResponse(
    string Service,
    DateTimeOffset ServerTimeUtc,
    string Environment,
    string GameServerId,
    string WorldId,
    string ContractStatus,
    bool ReadOnly,
    bool NonLive,
    bool OfficialEndpoint,
    bool MutationAllowed,
    bool PersistenceClaimAllowed,
    bool RealTimeSynchronizationEnabled,
    PlayableHiveLoopReadinessModel ReadModel,
    PlayableHiveLoopForbiddenClaims ForbiddenClaims,
    IReadOnlyList<string> Blockers);

public sealed record PlayableHiveLoopReadinessModel(
    IReadOnlyList<PlayerResourceReadModel> PlayerResources,
    IReadOnlyList<PlayerBuildingReadModel> Buildings,
    IReadOnlyList<BuildingLevelReadModel> BuildingLevels,
    IReadOnlyList<BuildingUpgradeReadModel> BuildingUpgrades,
    IReadOnlyList<ConstructionQueueReadModel> ConstructionQueue,
    IReadOnlyList<PlayerTroopReadModel> Troops,
    IReadOnlyList<TroopTrainingReadModel> Training,
    PlayerArmyReadModel Army);

public sealed record PlayerResourceReadModel(
    string ResourceKey,
    string DisplayName,
    int? Amount,
    int? Capacity,
    bool ServerAuthoritative,
    bool Live);

public sealed record PlayerBuildingReadModel(
    string BuildingKey,
    string DisplayName,
    int? Level,
    string Status,
    bool UpgradeAvailable,
    bool ServerAuthoritative,
    bool Live);

public sealed record BuildingLevelReadModel(
    string BuildingKey,
    int Level,
    IReadOnlyList<ResourceCostReadModel> Costs,
    int? ConstructionSeconds,
    bool ServerAuthoritative,
    bool Live);

public sealed record BuildingUpgradeReadModel(
    string BuildingKey,
    int? FromLevel,
    int? ToLevel,
    string Status,
    IReadOnlyList<ResourceCostReadModel> Costs,
    int? DurationSeconds,
    bool MutationAllowed,
    bool ServerAuthoritative,
    bool Live);

public sealed record ConstructionQueueReadModel(
    string QueueSlotKey,
    string? BuildingKey,
    string Status,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletesAtUtc,
    bool ServerAuthoritative,
    bool Live);

public sealed record PlayerTroopReadModel(
    string TroopKey,
    string DisplayName,
    int? Count,
    int? Level,
    bool ServerAuthoritative,
    bool Live);

public sealed record TroopTrainingReadModel(
    string TrainingSlotKey,
    string? TroopKey,
    int? Quantity,
    string Status,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletesAtUtc,
    bool MutationAllowed,
    bool ServerAuthoritative,
    bool Live);

public sealed record PlayerArmyReadModel(
    int? TotalTroops,
    int? Capacity,
    int? AssignedTroops,
    int? AvailableTroops,
    bool ServerAuthoritative,
    bool Live);

public sealed record ResourceCostReadModel(
    string ResourceKey,
    int? Amount,
    bool ServerAuthoritative,
    bool Live);

public sealed record PlayableHiveLoopForbiddenClaims(
    bool OfficialEndpoint,
    bool OfficialResources,
    bool OfficialBuildings,
    bool OfficialBuildingLevels,
    bool OfficialBuildingUpgrades,
    bool OfficialConstructionQueue,
    bool OfficialTroops,
    bool OfficialTraining,
    bool OfficialArmy,
    bool OfficialProgression,
    bool OfficialPersistence,
    bool RealTimeSynchronization);
