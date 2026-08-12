using BeeKingdom.Shared.Catalogs;
using BeeKingdom.Shared.ValueObjects;
using BeeKingdom.Shared.Versioning;

namespace BeeKingdom.Shared.Commands;

public sealed record HiveBuildingUpgradeRequestCommand(
    Guid CommandId,
    DateTimeOffset CreatedAtUtc,
    PlayerId PlayerId,
    WorldId WorldId,
    GameServerId GameServerId,
    BuildingId BuildingId,
    string BuildingKey,
    int FromLevel,
    int ToLevel,
    long ExpectedResourceRevision,
    long ExpectedBuildingRevision,
    string IdempotencyKey,
    string ExpectedCatalogVersion,
    bool NonLive,
    bool ReadinessOnly,
    bool OfficialProgressionRequested,
    ContractVersion ContractVersion) : ICommand;

public sealed record HiveBuildingUpgradeCommandResponse(
    Guid CommandId,
    string Result,
    bool NonLive,
    bool ReadinessOnly,
    bool OfficialProgressionApplied,
    bool LiveMutationApplied,
    string BuildingKey,
    int FromLevel,
    int ToLevel,
    IReadOnlyList<HiveResourceCostCatalogEntry> ServerCalculatedCosts,
    int? ServerCalculatedDurationSeconds,
    IReadOnlyList<HiveLoopCommandValidationError> Errors,
    ContractVersion ContractVersion);

public sealed record HiveTroopTrainingRequestCommand(
    Guid CommandId,
    DateTimeOffset CreatedAtUtc,
    PlayerId PlayerId,
    WorldId WorldId,
    GameServerId GameServerId,
    string TroopKey,
    int Quantity,
    long ExpectedResourceRevision,
    long ExpectedArmyRevision,
    string IdempotencyKey,
    string ExpectedCatalogVersion,
    bool NonLive,
    bool ReadinessOnly,
    bool OfficialProgressionRequested,
    ContractVersion ContractVersion) : ICommand;

public sealed record HiveTroopTrainingCommandResponse(
    Guid CommandId,
    string Result,
    bool NonLive,
    bool ReadinessOnly,
    bool OfficialProgressionApplied,
    bool LiveMutationApplied,
    string TroopKey,
    int Quantity,
    IReadOnlyList<HiveResourceCostCatalogEntry> ServerCalculatedCosts,
    int? ServerCalculatedDurationSeconds,
    IReadOnlyList<HiveLoopCommandValidationError> Errors,
    ContractVersion ContractVersion);

public sealed record HiveLoopCommandValidationError(
    HiveLoopCommandValidationErrorCode Code,
    string Message,
    string? Target);

public enum HiveLoopCommandValidationErrorCode
{
    InsufficientCost = 0,
    IdempotencyConflict = 1,
    TargetLocked = 2,
    CapacityExceeded = 3,
    UnknownCatalogEntry = 4,
    MissingIdempotencyKey = 5,
    CatalogVersionMismatch = 6,
    OfficialProgressionNotAllowed = 7
}

public static class HiveLoopCommandContractFactory
{
    public const string ReadinessAcceptedResult = "ReadinessAccepted";
    public const string ReadinessRejectedResult = "ReadinessRejected";

    public static HiveBuildingUpgradeCommandResponse CreateReadinessUpgradeResponse(HiveBuildingUpgradeRequestCommand command, HiveLoopCatalogSet catalog)
    {
        HiveBuildingUpgradeCatalogEntry? upgrade = catalog.BuildingUpgrades.SingleOrDefault(item =>
            item.BuildingKey == command.BuildingKey &&
            item.FromLevel == command.FromLevel &&
            item.ToLevel == command.ToLevel);

        if (upgrade is null)
        {
            return new HiveBuildingUpgradeCommandResponse(
                command.CommandId,
                ReadinessRejectedResult,
                NonLive: true,
                ReadinessOnly: true,
                OfficialProgressionApplied: false,
                LiveMutationApplied: false,
                command.BuildingKey,
                command.FromLevel,
                command.ToLevel,
                [],
                ServerCalculatedDurationSeconds: null,
                [new HiveLoopCommandValidationError(HiveLoopCommandValidationErrorCode.UnknownCatalogEntry, "Building upgrade is not present in the non-live catalog.", command.BuildingKey)],
                command.ContractVersion);
        }

        return new HiveBuildingUpgradeCommandResponse(
            command.CommandId,
            ReadinessAcceptedResult,
            NonLive: true,
            ReadinessOnly: true,
            OfficialProgressionApplied: false,
            LiveMutationApplied: false,
            command.BuildingKey,
            command.FromLevel,
            command.ToLevel,
            upgrade.ResourceCosts,
            upgrade.DurationSeconds,
            [],
            command.ContractVersion);
    }

    public static HiveTroopTrainingCommandResponse CreateReadinessTrainingResponse(HiveTroopTrainingRequestCommand command, HiveLoopCatalogSet catalog)
    {
        HiveTroopTrainingCatalogEntry? training = catalog.TroopTraining.SingleOrDefault(item => item.TroopKey == command.TroopKey);

        if (training is null)
        {
            return new HiveTroopTrainingCommandResponse(
                command.CommandId,
                ReadinessRejectedResult,
                NonLive: true,
                ReadinessOnly: true,
                OfficialProgressionApplied: false,
                LiveMutationApplied: false,
                command.TroopKey,
                command.Quantity,
                [],
                ServerCalculatedDurationSeconds: null,
                [new HiveLoopCommandValidationError(HiveLoopCommandValidationErrorCode.UnknownCatalogEntry, "Troop training is not present in the non-live catalog.", command.TroopKey)],
                command.ContractVersion);
        }

        if (command.Quantity < training.BatchSizeMin || command.Quantity > training.BatchSizeMax || command.Quantity % training.QuantityStep != 0)
        {
            return new HiveTroopTrainingCommandResponse(
                command.CommandId,
                ReadinessRejectedResult,
                NonLive: true,
                ReadinessOnly: true,
                OfficialProgressionApplied: false,
                LiveMutationApplied: false,
                command.TroopKey,
                command.Quantity,
                [],
                ServerCalculatedDurationSeconds: null,
                [new HiveLoopCommandValidationError(HiveLoopCommandValidationErrorCode.CapacityExceeded, "Training quantity is outside non-live catalog bounds.", command.TroopKey)],
                command.ContractVersion);
        }

        return new HiveTroopTrainingCommandResponse(
            command.CommandId,
            ReadinessAcceptedResult,
            NonLive: true,
            ReadinessOnly: true,
            OfficialProgressionApplied: false,
            LiveMutationApplied: false,
            command.TroopKey,
            command.Quantity,
            training.ResourceCosts.Select(cost => cost with { Amount = cost.Amount * command.Quantity }).ToArray(),
            training.DurationSecondsPerUnit * command.Quantity,
            [],
            command.ContractVersion);
    }
}
