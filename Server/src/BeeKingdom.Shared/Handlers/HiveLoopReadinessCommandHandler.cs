using BeeKingdom.Shared.Catalogs;
using BeeKingdom.Shared.Commands;

namespace BeeKingdom.Shared.Handlers;

public sealed class HiveLoopReadinessCommandHandler
{
    private readonly HiveLoopCatalogSet catalog;

    public HiveLoopReadinessCommandHandler(HiveLoopCatalogSet catalog)
    {
        this.catalog = catalog;
    }

    public HiveBuildingUpgradeCommandResponse Handle(HiveBuildingUpgradeRequestCommand command)
    {
        HiveLoopCommandValidationError? guardError = ValidateCommonGuards(
            command.IdempotencyKey,
            command.ExpectedCatalogVersion,
            command.NonLive,
            command.ReadinessOnly,
            command.OfficialProgressionRequested);

        if (guardError is not null)
        {
            return RejectUpgrade(command, guardError);
        }

        return HiveLoopCommandContractFactory.CreateReadinessUpgradeResponse(command, catalog);
    }

    public HiveTroopTrainingCommandResponse Handle(HiveTroopTrainingRequestCommand command)
    {
        HiveLoopCommandValidationError? guardError = ValidateCommonGuards(
            command.IdempotencyKey,
            command.ExpectedCatalogVersion,
            command.NonLive,
            command.ReadinessOnly,
            command.OfficialProgressionRequested);

        if (guardError is not null)
        {
            return RejectTraining(command, guardError);
        }

        return HiveLoopCommandContractFactory.CreateReadinessTrainingResponse(command, catalog);
    }

    private HiveLoopCommandValidationError? ValidateCommonGuards(
        string idempotencyKey,
        string expectedCatalogVersion,
        bool nonLive,
        bool readinessOnly,
        bool officialProgressionRequested)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return new HiveLoopCommandValidationError(
                HiveLoopCommandValidationErrorCode.MissingIdempotencyKey,
                "Idempotency key is required for future hive loop commands.",
                "IdempotencyKey");
        }

        if (!string.Equals(expectedCatalogVersion, catalog.CatalogVersion, StringComparison.Ordinal))
        {
            return new HiveLoopCommandValidationError(
                HiveLoopCommandValidationErrorCode.CatalogVersionMismatch,
                "Expected catalog version does not match the non-live readiness catalog.",
                "ExpectedCatalogVersion");
        }

        if (!nonLive || !readinessOnly || officialProgressionRequested)
        {
            return new HiveLoopCommandValidationError(
                HiveLoopCommandValidationErrorCode.OfficialProgressionNotAllowed,
                "Hive loop readiness handler cannot apply official player progression.",
                "OfficialProgressionRequested");
        }

        return null;
    }

    private static HiveBuildingUpgradeCommandResponse RejectUpgrade(HiveBuildingUpgradeRequestCommand command, HiveLoopCommandValidationError error)
    {
        return new HiveBuildingUpgradeCommandResponse(
            command.CommandId,
            HiveLoopCommandContractFactory.ReadinessRejectedResult,
            NonLive: true,
            ReadinessOnly: true,
            OfficialProgressionApplied: false,
            LiveMutationApplied: false,
            command.BuildingKey,
            command.FromLevel,
            command.ToLevel,
            [],
            ServerCalculatedDurationSeconds: null,
            [error],
            command.ContractVersion);
    }

    private static HiveTroopTrainingCommandResponse RejectTraining(HiveTroopTrainingRequestCommand command, HiveLoopCommandValidationError error)
    {
        return new HiveTroopTrainingCommandResponse(
            command.CommandId,
            HiveLoopCommandContractFactory.ReadinessRejectedResult,
            NonLive: true,
            ReadinessOnly: true,
            OfficialProgressionApplied: false,
            LiveMutationApplied: false,
            command.TroopKey,
            command.Quantity,
            [],
            ServerCalculatedDurationSeconds: null,
            [error],
            command.ContractVersion);
    }
}
