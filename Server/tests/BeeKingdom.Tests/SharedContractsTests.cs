using BeeKingdom.Shared.Commands;
using BeeKingdom.Shared.Auth;
using BeeKingdom.Shared.Catalogs;
using BeeKingdom.Shared.Contracts;
using BeeKingdom.Shared.DevOnly;
using BeeKingdom.Shared.DTO;
using BeeKingdom.Shared.Enums;
using BeeKingdom.Shared.Extensions;
using BeeKingdom.Shared.Handlers;
using BeeKingdom.Shared.Persistence;
using BeeKingdom.Shared.Responses;
using BeeKingdom.Shared.Serialization;
using BeeKingdom.Shared.ValueObjects;
using BeeKingdom.Shared.Versioning;
using BeeKingdom.Shared.WorldMap;
using System.Text.Json;

namespace BeeKingdom.Tests;

public sealed class SharedContractsTests
{
    [Test]
    public void CommandsImplementContractBase()
    {
        BuildStructureCommand command = new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            ColonyId.New(),
            "storage.basic",
            new HexCoordinate(2, -1),
            ContractVersion.Current);

        Assert.Multiple(() =>
        {
            Assert.That(command, Is.InstanceOf<ICommand>());
            Assert.That(command, Is.InstanceOf<IContract>());
            Assert.That(command.ContractVersion, Is.EqualTo(ContractVersion.Current));
        });
    }

    [Test]
    public void SerializerRoundTripsSharedDto()
    {
        IContractSerializer serializer = new SystemTextJsonContractSerializer();
        PlayerDto dto = new(PlayerId.New(), "Apiary", 12, DateTimeOffset.UnixEpoch);

        string payload = serializer.Serialize(dto);
        PlayerDto? restored = serializer.Deserialize<PlayerDto>(payload);

        Assert.That(restored, Is.EqualTo(dto));
    }

    [Test]
    public void ResponseEnvelopeSupportsValidationErrors()
    {
        Guid requestId = Guid.NewGuid();
        ResponseEnvelope<PlayerDto> response = ResponseEnvelope<PlayerDto>.Failure(
            requestId,
            ResponseStatus.ValidationError,
            [new ContractError("name.required", "Name is required.", "displayName")]);

        Assert.Multiple(() =>
        {
            Assert.That(response.RequestId, Is.EqualTo(requestId));
            Assert.That(response.Status, Is.EqualTo(ResponseStatus.ValidationError));
            Assert.That(response.Errors, Has.Count.EqualTo(1));
            Assert.That(response.Payload, Is.Null);
        });
    }

    [Test]
    public void ContractVersionCompatibilityRejectsMajorMismatch()
    {
        Assert.Multiple(() =>
        {
            Assert.That(new ContractVersion(1, 0, 0).IsCompatibleWithCurrent(), Is.True);
            Assert.That(new ContractVersion(2, 0, 0).IsCompatibleWithCurrent(), Is.False);
        });
    }

    [Test]
    public void SharedAssemblyHasNoUnitySqlOrAspNetDependency()
    {
        string[] referencedAssemblies = typeof(PlayerDto).Assembly
            .GetReferencedAssemblies()
            .Select(name => name.Name ?? string.Empty)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(referencedAssemblies, Does.Not.Contain("UnityEngine"));
            Assert.That(referencedAssemblies, Does.Not.Contain("System.Data"));
            Assert.That(referencedAssemblies.Any(name => name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)), Is.False);
        });
    }

    [Test]
    public void HiveLoopCodeFirstCatalogRemainsReadinessNonLive()
    {
        HiveLoopCatalogSet catalog = HiveLoopCodeFirstCatalogs.CreateReadinessCatalog();
        HiveLoopCatalogValidationResult validation = catalog.Validate();

        Assert.Multiple(() =>
        {
            Assert.That(catalog.ReadOnly, Is.True);
            Assert.That(catalog.NonLive, Is.True);
            Assert.That(catalog.CatalogVersion, Is.EqualTo(HiveLoopCodeFirstCatalogs.ReadinessCatalogVersion));
            Assert.That(validation.IsValid, Is.True, string.Join(Environment.NewLine, validation.Errors));
            Assert.That(catalog.Resources.Select(resource => resource.ResourceKey), Is.SupersetOf(new[] { "honey", "wax", "pollen" }));
            Assert.That(catalog.Buildings.Select(building => building.BuildingKey), Does.Contain("honey_storage"));
            Assert.That(catalog.BuildingUpgrades, Has.Some.Matches<HiveBuildingUpgradeCatalogEntry>(upgrade => upgrade.BuildingKey == "honey_storage" && upgrade.FromLevel == 0 && upgrade.ToLevel == 1));
            Assert.That(catalog.Troops.Select(troop => troop.TroopKey), Does.Contain("worker_bee"));
            Assert.That(catalog.TroopTraining.Select(training => training.TroopKey), Does.Contain("worker_bee"));
            Assert.That(catalog.ArmyCapacity, Has.Some.Matches<HiveArmyCapacityCatalogEntry>(capacity => capacity.CapacityBonus > 0));
        });
    }

    [Test]
    public void HiveLoopCatalogComputesServerSideCostsAndTrainingBounds()
    {
        HiveLoopCatalogSet catalog = HiveLoopCodeFirstCatalogs.CreateReadinessCatalog();

        long honeyUpgradeCost = catalog.CalculateUpgradeCost("honey_storage", fromLevel: 0, "honey");
        long waxUpgradeCost = catalog.CalculateUpgradeCost("honey_storage", fromLevel: 0, "wax");
        long workerHoneyTrainingCost = catalog.CalculateTrainingCost("worker_bee", quantity: 3, "honey");

        Assert.Multiple(() =>
        {
            Assert.That(honeyUpgradeCost, Is.EqualTo(50));
            Assert.That(waxUpgradeCost, Is.EqualTo(10));
            Assert.That(workerHoneyTrainingCost, Is.EqualTo(24));
            Assert.That(() => catalog.CalculateTrainingCost("worker_bee", quantity: 21, "honey"), Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void HiveLoopCatalogKeepsAntiDoubleSpendAndIdempotencyPoliciesEnabled()
    {
        HiveLoopCatalogSet catalog = HiveLoopCodeFirstCatalogs.CreateReadinessCatalog();

        Assert.Multiple(() =>
        {
            Assert.That(catalog.IdempotencyPolicy.Required, Is.True);
            Assert.That(catalog.IdempotencyPolicy.HeaderName, Is.EqualTo("Idempotency-Key"));
            Assert.That(catalog.IdempotencyPolicy.StoreHashOnly, Is.True);
            Assert.That(catalog.IdempotencyPolicy.RequiresPayloadHash, Is.True);
            Assert.That(catalog.IdempotencyPolicy.ReplaySamePayloadResult, Is.EqualTo("AlreadyApplied"));
            Assert.That(catalog.IdempotencyPolicy.ReplayDifferentPayloadResult, Is.EqualTo("Conflict"));
            Assert.That(catalog.AntiDoubleSpendPolicy.RequiresExpectedResourceRevision, Is.True);
            Assert.That(catalog.AntiDoubleSpendPolicy.RequiresExpectedTargetRevision, Is.True);
            Assert.That(catalog.AntiDoubleSpendPolicy.RequiresAtomicResourceDebitAndQueueCreate, Is.True);
            Assert.That(catalog.AntiDoubleSpendPolicy.RejectsClientProvidedCost, Is.True);
            Assert.That(catalog.AntiDoubleSpendPolicy.RejectsClientProvidedDuration, Is.True);
            Assert.That(catalog.AntiDoubleSpendPolicy.RejectsCrossWorldScope, Is.True);
        });
    }

    [Test]
    public void HiveLoopUpgradeCommandContractUsesCatalogWithoutApplyingLiveProgression()
    {
        HiveLoopCatalogSet catalog = HiveLoopCodeFirstCatalogs.CreateReadinessCatalog();
        HiveBuildingUpgradeRequestCommand command = new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            PlayerId.New(),
            WorldId.New(),
            GameServerId.New(),
            BuildingId.New(),
            "honey_storage",
            FromLevel: 0,
            ToLevel: 1,
            ExpectedResourceRevision: 7,
            ExpectedBuildingRevision: 3,
            IdempotencyKey: "upgrade-idempotency-key",
            HiveLoopCodeFirstCatalogs.ReadinessCatalogVersion,
            NonLive: true,
            ReadinessOnly: true,
            OfficialProgressionRequested: false,
            ContractVersion.Current);

        HiveBuildingUpgradeCommandResponse response = HiveLoopCommandContractFactory.CreateReadinessUpgradeResponse(command, catalog);

        Assert.Multiple(() =>
        {
            Assert.That(command, Is.InstanceOf<ICommand>());
            Assert.That(command.NonLive, Is.True);
            Assert.That(command.ReadinessOnly, Is.True);
            Assert.That(command.OfficialProgressionRequested, Is.False);
            Assert.That(response.Result, Is.EqualTo(HiveLoopCommandContractFactory.ReadinessAcceptedResult));
            Assert.That(response.NonLive, Is.True);
            Assert.That(response.ReadinessOnly, Is.True);
            Assert.That(response.OfficialProgressionApplied, Is.False);
            Assert.That(response.LiveMutationApplied, Is.False);
            Assert.That(response.ServerCalculatedCosts.Single(cost => cost.ResourceKey == "honey").Amount, Is.EqualTo(50));
            Assert.That(response.ServerCalculatedDurationSeconds, Is.EqualTo(60));
            Assert.That(response.Errors, Is.Empty);
        });
    }

    [Test]
    public void HiveLoopTrainingCommandContractUsesCatalogWithoutApplyingLiveProgression()
    {
        HiveLoopCatalogSet catalog = HiveLoopCodeFirstCatalogs.CreateReadinessCatalog();
        HiveTroopTrainingRequestCommand command = new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            PlayerId.New(),
            WorldId.New(),
            GameServerId.New(),
            "worker_bee",
            Quantity: 3,
            ExpectedResourceRevision: 11,
            ExpectedArmyRevision: 2,
            IdempotencyKey: "training-idempotency-key",
            HiveLoopCodeFirstCatalogs.ReadinessCatalogVersion,
            NonLive: true,
            ReadinessOnly: true,
            OfficialProgressionRequested: false,
            ContractVersion.Current);

        HiveTroopTrainingCommandResponse response = HiveLoopCommandContractFactory.CreateReadinessTrainingResponse(command, catalog);

        Assert.Multiple(() =>
        {
            Assert.That(command, Is.InstanceOf<ICommand>());
            Assert.That(command.NonLive, Is.True);
            Assert.That(command.ReadinessOnly, Is.True);
            Assert.That(command.OfficialProgressionRequested, Is.False);
            Assert.That(response.Result, Is.EqualTo(HiveLoopCommandContractFactory.ReadinessAcceptedResult));
            Assert.That(response.NonLive, Is.True);
            Assert.That(response.ReadinessOnly, Is.True);
            Assert.That(response.OfficialProgressionApplied, Is.False);
            Assert.That(response.LiveMutationApplied, Is.False);
            Assert.That(response.ServerCalculatedCosts.Single(cost => cost.ResourceKey == "honey").Amount, Is.EqualTo(24));
            Assert.That(response.ServerCalculatedDurationSeconds, Is.EqualTo(45));
            Assert.That(response.Errors, Is.Empty);
        });
    }

    [Test]
    public void HiveLoopCommandContractsExposeFutureValidationErrors()
    {
        HiveLoopCatalogSet catalog = HiveLoopCodeFirstCatalogs.CreateReadinessCatalog();
        HiveTroopTrainingRequestCommand command = new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            PlayerId.New(),
            WorldId.New(),
            GameServerId.New(),
            "unknown_troop",
            Quantity: 1,
            ExpectedResourceRevision: 1,
            ExpectedArmyRevision: 1,
            IdempotencyKey: "unknown-training-key",
            HiveLoopCodeFirstCatalogs.ReadinessCatalogVersion,
            NonLive: true,
            ReadinessOnly: true,
            OfficialProgressionRequested: false,
            ContractVersion.Current);

        HiveTroopTrainingCommandResponse response = HiveLoopCommandContractFactory.CreateReadinessTrainingResponse(command, catalog);
        HiveLoopCommandValidationErrorCode[] requiredCodes =
        [
            HiveLoopCommandValidationErrorCode.InsufficientCost,
            HiveLoopCommandValidationErrorCode.IdempotencyConflict,
            HiveLoopCommandValidationErrorCode.TargetLocked,
            HiveLoopCommandValidationErrorCode.CapacityExceeded,
            HiveLoopCommandValidationErrorCode.UnknownCatalogEntry
        ];

        Assert.Multiple(() =>
        {
            Assert.That(response.Result, Is.EqualTo(HiveLoopCommandContractFactory.ReadinessRejectedResult));
            Assert.That(response.OfficialProgressionApplied, Is.False);
            Assert.That(response.LiveMutationApplied, Is.False);
            Assert.That(response.Errors.Single().Code, Is.EqualTo(HiveLoopCommandValidationErrorCode.UnknownCatalogEntry));
            Assert.That(Enum.GetValues<HiveLoopCommandValidationErrorCode>(), Is.SupersetOf(requiredCodes));
        });
    }

    [Test]
    public void HiveLoopReadinessHandlerAcceptsUpgradeWithoutLiveMutation()
    {
        HiveLoopReadinessCommandHandler handler = new(HiveLoopCodeFirstCatalogs.CreateReadinessCatalog());
        HiveBuildingUpgradeRequestCommand command = CreateUpgradeCommand();

        HiveBuildingUpgradeCommandResponse response = handler.Handle(command);

        Assert.Multiple(() =>
        {
            Assert.That(response.Result, Is.EqualTo(HiveLoopCommandContractFactory.ReadinessAcceptedResult));
            Assert.That(response.NonLive, Is.True);
            Assert.That(response.ReadinessOnly, Is.True);
            Assert.That(response.OfficialProgressionApplied, Is.False);
            Assert.That(response.LiveMutationApplied, Is.False);
            Assert.That(response.ServerCalculatedCosts.Single(cost => cost.ResourceKey == "honey").Amount, Is.EqualTo(50));
            Assert.That(response.ServerCalculatedDurationSeconds, Is.EqualTo(60));
            Assert.That(response.Errors, Is.Empty);
        });
    }

    [Test]
    public void HiveLoopReadinessHandlerAcceptsTrainingWithoutLiveMutation()
    {
        HiveLoopReadinessCommandHandler handler = new(HiveLoopCodeFirstCatalogs.CreateReadinessCatalog());
        HiveTroopTrainingRequestCommand command = CreateTrainingCommand(quantity: 2);

        HiveTroopTrainingCommandResponse response = handler.Handle(command);

        Assert.Multiple(() =>
        {
            Assert.That(response.Result, Is.EqualTo(HiveLoopCommandContractFactory.ReadinessAcceptedResult));
            Assert.That(response.NonLive, Is.True);
            Assert.That(response.ReadinessOnly, Is.True);
            Assert.That(response.OfficialProgressionApplied, Is.False);
            Assert.That(response.LiveMutationApplied, Is.False);
            Assert.That(response.ServerCalculatedCosts.Single(cost => cost.ResourceKey == "honey").Amount, Is.EqualTo(16));
            Assert.That(response.ServerCalculatedDurationSeconds, Is.EqualTo(30));
            Assert.That(response.Errors, Is.Empty);
        });
    }

    [Test]
    public void HiveLoopReadinessHandlerRejectsMissingIdempotencyKey()
    {
        HiveLoopReadinessCommandHandler handler = new(HiveLoopCodeFirstCatalogs.CreateReadinessCatalog());
        HiveBuildingUpgradeRequestCommand command = CreateUpgradeCommand() with { IdempotencyKey = "" };

        HiveBuildingUpgradeCommandResponse response = handler.Handle(command);

        Assert.Multiple(() =>
        {
            Assert.That(response.Result, Is.EqualTo(HiveLoopCommandContractFactory.ReadinessRejectedResult));
            Assert.That(response.OfficialProgressionApplied, Is.False);
            Assert.That(response.LiveMutationApplied, Is.False);
            Assert.That(response.Errors.Single().Code, Is.EqualTo(HiveLoopCommandValidationErrorCode.MissingIdempotencyKey));
        });
    }

    [Test]
    public void HiveLoopReadinessHandlerRejectsCatalogMismatchAndUnknownTargets()
    {
        HiveLoopReadinessCommandHandler handler = new(HiveLoopCodeFirstCatalogs.CreateReadinessCatalog());
        HiveBuildingUpgradeRequestCommand catalogMismatch = CreateUpgradeCommand() with { ExpectedCatalogVersion = "future-live-catalog" };
        HiveBuildingUpgradeRequestCommand unknownBuilding = CreateUpgradeCommand() with { BuildingKey = "unknown_building" };

        HiveBuildingUpgradeCommandResponse mismatchResponse = handler.Handle(catalogMismatch);
        HiveBuildingUpgradeCommandResponse unknownResponse = handler.Handle(unknownBuilding);

        Assert.Multiple(() =>
        {
            Assert.That(mismatchResponse.Result, Is.EqualTo(HiveLoopCommandContractFactory.ReadinessRejectedResult));
            Assert.That(mismatchResponse.Errors.Single().Code, Is.EqualTo(HiveLoopCommandValidationErrorCode.CatalogVersionMismatch));
            Assert.That(mismatchResponse.LiveMutationApplied, Is.False);
            Assert.That(unknownResponse.Result, Is.EqualTo(HiveLoopCommandContractFactory.ReadinessRejectedResult));
            Assert.That(unknownResponse.Errors.Single().Code, Is.EqualTo(HiveLoopCommandValidationErrorCode.UnknownCatalogEntry));
            Assert.That(unknownResponse.OfficialProgressionApplied, Is.False);
        });
    }

    [Test]
    public void HiveLoopReadinessHandlerRejectsInvalidTrainingQuantity()
    {
        HiveLoopReadinessCommandHandler handler = new(HiveLoopCodeFirstCatalogs.CreateReadinessCatalog());
        HiveTroopTrainingRequestCommand command = CreateTrainingCommand(quantity: 21);

        HiveTroopTrainingCommandResponse response = handler.Handle(command);

        Assert.Multiple(() =>
        {
            Assert.That(response.Result, Is.EqualTo(HiveLoopCommandContractFactory.ReadinessRejectedResult));
            Assert.That(response.Errors.Single().Code, Is.EqualTo(HiveLoopCommandValidationErrorCode.CapacityExceeded));
            Assert.That(response.OfficialProgressionApplied, Is.False);
            Assert.That(response.LiveMutationApplied, Is.False);
            Assert.That(response.ServerCalculatedCosts, Is.Empty);
        });
    }

    [Test]
    public void HiveLoopPersistenceReadinessDesignDeclaresFutureTablesWithoutLiveSqlWrites()
    {
        HiveLoopPersistenceReadinessDesign design = HiveLoopPersistenceReadinessCatalog.CreateReadinessDesign();
        HiveLoopPersistenceReadinessValidationResult validation = design.Validate();

        Assert.Multiple(() =>
        {
            Assert.That(design.ReadOnly, Is.True);
            Assert.That(design.NonLive, Is.True);
            Assert.That(design.ProductionMigrationAllowed, Is.False);
            Assert.That(design.LiveSqlWritesAllowed, Is.False);
            Assert.That(validation.IsValid, Is.True, string.Join(Environment.NewLine, validation.Errors));
            Assert.That(design.FutureTables, Is.EquivalentTo(new[]
            {
                HiveLoopPersistenceReadinessTables.PlayerResources,
                HiveLoopPersistenceReadinessTables.HiveBuildings,
                HiveLoopPersistenceReadinessTables.ConstructionQueue,
                HiveLoopPersistenceReadinessTables.TroopCounts,
                HiveLoopPersistenceReadinessTables.TrainingQueue,
                HiveLoopPersistenceReadinessTables.IdempotencyRecords
            }));
        });
    }

    [Test]
    public void HiveLoopPersistenceReadinessRecordsKeepWorldServerScopeAndReadOnlyFlags()
    {
        PlayerId playerId = PlayerId.New();
        WorldId worldId = WorldId.New();
        GameServerId gameServerId = GameServerId.New();
        BuildingId buildingId = BuildingId.New();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        HivePlayerResourceReadinessRecord resource = new(playerId, worldId, gameServerId, "honey", Amount: 100, Capacity: 500, Revision: 4, HiveLoopCodeFirstCatalogs.ReadinessCatalogVersion, ReadOnly: true, NonLive: true);
        HiveBuildingReadinessRecord building = new(playerId, worldId, gameServerId, buildingId, "honey_storage", Level: 1, Revision: 2, HiveLoopCodeFirstCatalogs.ReadinessCatalogVersion, ReadOnly: true, NonLive: true);
        HiveConstructionQueueReadinessRecord construction = new(Guid.NewGuid(), playerId, worldId, gameServerId, buildingId, "honey_storage", FromLevel: 1, ToLevel: 2, now, now.AddMinutes(5), ExpectedResourceRevision: 4, ExpectedBuildingRevision: 2, HiveLoopQueueItemReadinessStatus.Pending, "key-hash", HiveLoopCodeFirstCatalogs.ReadinessCatalogVersion, ReadOnly: true, NonLive: true);
        HiveTroopCountReadinessRecord troop = new(playerId, worldId, gameServerId, "worker_bee", Quantity: 3, ArmyRevision: 1, HiveLoopCodeFirstCatalogs.ReadinessCatalogVersion, ReadOnly: true, NonLive: true);
        HiveTrainingQueueReadinessRecord training = new(Guid.NewGuid(), playerId, worldId, gameServerId, "worker_bee", Quantity: 2, now, now.AddMinutes(1), ExpectedResourceRevision: 4, ExpectedArmyRevision: 1, HiveLoopQueueItemReadinessStatus.Pending, "training-key-hash", HiveLoopCodeFirstCatalogs.ReadinessCatalogVersion, ReadOnly: true, NonLive: true);
        HiveIdempotencyReadinessRecord idempotency = new(playerId, worldId, gameServerId, "key-hash", "payload-hash", "HiveBuildingUpgrade", "result-hash", now, now.AddHours(1), ReadOnly: true, NonLive: true);

        Assert.Multiple(() =>
        {
            Assert.That(resource.WorldId, Is.EqualTo(worldId));
            Assert.That(resource.GameServerId, Is.EqualTo(gameServerId));
            Assert.That(building.WorldId, Is.EqualTo(worldId));
            Assert.That(construction.ExpectedResourceRevision, Is.EqualTo(resource.Revision));
            Assert.That(construction.ExpectedBuildingRevision, Is.EqualTo(building.Revision));
            Assert.That(troop.WorldId, Is.EqualTo(worldId));
            Assert.That(training.ExpectedArmyRevision, Is.EqualTo(troop.ArmyRevision));
            Assert.That(idempotency.RequestPayloadHash, Is.Not.Empty);
            Assert.That(new[] { resource.ReadOnly, building.ReadOnly, construction.ReadOnly, troop.ReadOnly, training.ReadOnly, idempotency.ReadOnly }, Is.All.True);
            Assert.That(new[] { resource.NonLive, building.NonLive, construction.NonLive, troop.NonLive, training.NonLive, idempotency.NonLive }, Is.All.True);
        });
    }

    [Test]
    public void HiveLoopPersistenceTransactionPolicyRequiresAtomicDebitQueueAndIdempotency()
    {
        HiveLoopPersistenceTransactionPolicy policy = HiveLoopPersistenceReadinessCatalog.CreateReadinessDesign().TransactionPolicy;

        Assert.Multiple(() =>
        {
            Assert.That(policy.RequiresAtomicResourceDebitAndQueueInsert, Is.True);
            Assert.That(policy.RequiresExpectedRevision, Is.True);
            Assert.That(policy.RequiresIdempotencyPayloadHash, Is.True);
            Assert.That(policy.RequiresWorldAndGameServerScope, Is.True);
            Assert.That(policy.RejectsCrossWorldReplay, Is.True);
            Assert.That(policy.RejectsDifferentPayloadForSameIdempotencyKey, Is.True);
        });
    }

    [Test]
    public void HiveLoopRepositoryReadinessContractsRemainNonLiveLocalOnly()
    {
        HiveLoopRepositoryReadinessDescriptor descriptor = HiveLoopRepositoryReadinessContracts.CreateDescriptor();
        HiveLoopRepositoryReadinessValidationResult validation = descriptor.Validate();

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.NonLive, Is.True);
            Assert.That(descriptor.ReadinessOnly, Is.True);
            Assert.That(descriptor.LocalOnly, Is.True);
            Assert.That(descriptor.ProductionSqlAllowed, Is.False);
            Assert.That(descriptor.EndpointExposed, Is.False);
            Assert.That(descriptor.OfficialProgressionAllowed, Is.False);
            Assert.That(validation.IsValid, Is.True, string.Join(Environment.NewLine, validation.Errors));
            Assert.That(descriptor.SupportedFutureTables, Is.EquivalentTo(HiveLoopPersistenceReadinessCatalog.CreateReadinessDesign().FutureTables));
        });
    }

    [Test]
    public void HiveLoopRepositoryReadinessContractsDeclareFutureAtomicIntentions()
    {
        string[] methodNames = typeof(IHiveLoopReadinessRepository).GetMethods().Select(method => method.Name).ToArray();
        HiveLoopRepositoryReadinessDescriptor descriptor = HiveLoopRepositoryReadinessContracts.CreateDescriptor();

        Assert.Multiple(() =>
        {
            Assert.That(methodNames, Does.Contain(nameof(IHiveLoopReadinessRepository.ReadPlayerResourcesAsync)));
            Assert.That(methodNames, Does.Contain(nameof(IHiveLoopReadinessRepository.ReadHiveBuildingsAsync)));
            Assert.That(methodNames, Does.Contain(nameof(IHiveLoopReadinessRepository.ReadConstructionQueueAsync)));
            Assert.That(methodNames, Does.Contain(nameof(IHiveLoopReadinessRepository.ReadTroopCountsAsync)));
            Assert.That(methodNames, Does.Contain(nameof(IHiveLoopReadinessRepository.ReadTrainingQueueAsync)));
            Assert.That(methodNames, Does.Contain(nameof(IHiveLoopReadinessRepository.ReadIdempotencyRecordAsync)));
            Assert.That(methodNames, Does.Contain(nameof(IHiveLoopReadinessRepository.TryReserveUpgradeAsync)));
            Assert.That(methodNames, Does.Contain(nameof(IHiveLoopReadinessRepository.TryReserveTrainingAsync)));
            Assert.That(methodNames, Does.Contain(nameof(IHiveLoopReadinessRepository.CompleteDueQueuesAsync)));
            Assert.That(methodNames, Does.Contain(nameof(IHiveLoopReadinessRepository.RecordIdempotencyResultAsync)));
            Assert.That(descriptor.IntendedAtomicOperations, Is.EquivalentTo(new[]
            {
                HiveLoopRepositoryReadinessOperations.TryReserveUpgrade,
                HiveLoopRepositoryReadinessOperations.TryReserveTraining,
                HiveLoopRepositoryReadinessOperations.CompleteDueQueues,
                HiveLoopRepositoryReadinessOperations.RecordIdempotencyResult
            }));
        });
    }

    [Test]
    public void HiveLoopRepositoryIntentResultsCannotClaimOfficialProgression()
    {
        HiveLoopRepositoryReservationResult reservation = new(
            Accepted: true,
            NonLive: true,
            ReadinessOnly: true,
            OfficialProgressionApplied: false,
            LiveSqlWriteApplied: false,
            QueueItemId: Guid.NewGuid(),
            ResultCode: "ReadinessReserved",
            ValidationErrors: []);
        HiveLoopDueQueueCompletionResult completion = new(
            NonLive: true,
            ReadinessOnly: true,
            OfficialProgressionApplied: false,
            LiveSqlWriteApplied: false,
            ConstructionItemsConsidered: 1,
            TrainingItemsConsidered: 1,
            QueueItemIds: [Guid.NewGuid()]);
        HiveLoopIdempotencyRecordResult idempotency = new(
            Recorded: true,
            NonLive: true,
            ReadinessOnly: true,
            OfficialProgressionApplied: false,
            LiveSqlWriteApplied: false,
            ResultCode: "ReadinessRecorded",
            ValidationErrors: []);

        Assert.Multiple(() =>
        {
            Assert.That(new[] { reservation.NonLive, completion.NonLive, idempotency.NonLive }, Is.All.True);
            Assert.That(new[] { reservation.ReadinessOnly, completion.ReadinessOnly, idempotency.ReadinessOnly }, Is.All.True);
            Assert.That(new[] { reservation.OfficialProgressionApplied, completion.OfficialProgressionApplied, idempotency.OfficialProgressionApplied }, Is.All.False);
            Assert.That(new[] { reservation.LiveSqlWriteApplied, completion.LiveSqlWriteApplied, idempotency.LiveSqlWriteApplied }, Is.All.False);
        });
    }

    [Test]
    public void HiveActionLoopDevOnlyBridgeDescriptorKeepsOfficialClaimsDisabled()
    {
        HiveActionLoopDevOnlyBridgeDescriptor descriptor = HiveActionLoopDevOnlyBridge.CreateDescriptor();
        HiveActionLoopDevOnlyValidationResult validation = descriptor.Validate();

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.EvidenceId, Is.EqualTo("SERVER-042-BEE-858-BEE-859"));
            Assert.That(descriptor.DevOnly, Is.True);
            Assert.That(descriptor.NonLive, Is.True);
            Assert.That(descriptor.ServerOfficialClaimAllowed, Is.False);
            Assert.That(descriptor.OfficialSaveEnabled, Is.False);
            Assert.That(descriptor.OfficialEconomyEnabled, Is.False);
            Assert.That(descriptor.OfficialPersistentArmyEnabled, Is.False);
            Assert.That(descriptor.ProductionSqlAllowed, Is.False);
            Assert.That(descriptor.OfficialEndpointAllowed, Is.False);
            Assert.That(descriptor.PublishAllowed, Is.False);
            Assert.That(descriptor.WorldMapRuntimeAllowed, Is.False);
            Assert.That(validation.IsValid, Is.True, string.Join(Environment.NewLine, validation.Errors));
        });
    }

    [Test]
    public void HiveActionLoopDevOnlyContractsCoverResourceTickUpgradeTrainingAndArmySnapshot()
    {
        HiveActionLoopDevOnlyBridgeDescriptor descriptor = HiveActionLoopDevOnlyBridge.CreateDescriptor();

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.SupportedContracts, Is.SupersetOf(new[]
            {
                HiveActionLoopDevOnlyContracts.ResourceTick,
                HiveActionLoopDevOnlyContracts.ResourceCommand,
                HiveActionLoopDevOnlyContracts.UpgradeCommand,
                HiveActionLoopDevOnlyContracts.TrainingCommand,
                HiveActionLoopDevOnlyContracts.RejectionCatalog,
                HiveActionLoopDevOnlyContracts.SnapshotEnvelope,
                HiveActionLoopDevOnlyContracts.SnapshotRevision,
                HiveActionLoopDevOnlyContracts.Reconciliation,
                HiveActionLoopDevOnlyContracts.ArmySnapshot
            }));
            Assert.That(descriptor.IdempotencyPolicy.RequiredForUpgrade, Is.True);
            Assert.That(descriptor.IdempotencyPolicy.RequiredForTraining, Is.True);
            Assert.That(descriptor.IdempotencyPolicy.RequiresPayloadHash, Is.True);
            Assert.That(descriptor.AntiDoubleSpendPolicy.RequiresExpectedSnapshotRevision, Is.True);
            Assert.That(descriptor.AntiDoubleSpendPolicy.RequiresAtomicCostAndQueueReservation, Is.True);
            Assert.That(descriptor.AntiDoubleSpendPolicy.RejectsClientCalculatedCost, Is.True);
            Assert.That(descriptor.AntiDoubleSpendPolicy.RejectsClientCalculatedDuration, Is.True);
        });
    }

    [Test]
    public void HiveActionLoopDevOnlyCommandsRequireIdempotencyAndExpectedRevisions()
    {
        PlayerId playerId = PlayerId.New();
        WorldId worldId = WorldId.New();
        GameServerId gameServerId = GameServerId.New();
        BuildingId buildingId = BuildingId.New();

        HiveUpgradeCommandDevOnlyContract upgrade = new(
            Guid.NewGuid(),
            playerId,
            worldId,
            gameServerId,
            buildingId,
            "honey_storage",
            FromLevel: 0,
            ToLevel: 1,
            IdempotencyKey: "upgrade-key",
            PayloadHash: "upgrade-payload-hash",
            ExpectedSnapshotRevision: 12,
            ExpectedResourceRevision: 7,
            ExpectedBuildingRevision: 3,
            DevOnly: true,
            NonLive: true,
            OfficialProgressionRequested: false,
            ContractVersion.Current);
        HiveTrainingCommandDevOnlyContract training = new(
            Guid.NewGuid(),
            playerId,
            worldId,
            gameServerId,
            "worker_bee",
            Quantity: 2,
            IdempotencyKey: "training-key",
            PayloadHash: "training-payload-hash",
            ExpectedSnapshotRevision: 12,
            ExpectedResourceRevision: 7,
            ExpectedTrainingQueueRevision: 4,
            DevOnly: true,
            NonLive: true,
            OfficialProgressionRequested: false,
            ContractVersion.Current);
        HiveResourceCommandDevOnlyContract resource = new(
            Guid.NewGuid(),
            playerId,
            worldId,
            gameServerId,
            "honey",
            CommandKind: "preview_tick",
            ExpectedSnapshotRevision: 12,
            ExpectedResourceRevision: 7,
            IdempotencyKey: "resource-key",
            PayloadHash: "resource-payload-hash",
            DevOnly: true,
            NonLive: true,
            OfficialEconomyRequested: false,
            ContractVersion.Current);

        Assert.Multiple(() =>
        {
            Assert.That(resource.IdempotencyKey, Is.Not.Empty);
            Assert.That(resource.PayloadHash, Is.Not.Empty);
            Assert.That(resource.ExpectedSnapshotRevision, Is.GreaterThan(0));
            Assert.That(resource.ExpectedResourceRevision, Is.GreaterThan(0));
            Assert.That(upgrade.IdempotencyKey, Is.Not.Empty);
            Assert.That(upgrade.PayloadHash, Is.Not.Empty);
            Assert.That(upgrade.ExpectedSnapshotRevision, Is.GreaterThan(0));
            Assert.That(upgrade.ExpectedResourceRevision, Is.GreaterThan(0));
            Assert.That(upgrade.ExpectedBuildingRevision, Is.GreaterThan(0));
            Assert.That(training.IdempotencyKey, Is.Not.Empty);
            Assert.That(training.PayloadHash, Is.Not.Empty);
            Assert.That(training.ExpectedSnapshotRevision, Is.GreaterThan(0));
            Assert.That(training.ExpectedResourceRevision, Is.GreaterThan(0));
            Assert.That(training.ExpectedTrainingQueueRevision, Is.GreaterThan(0));
            Assert.That(new[] { resource.DevOnly, upgrade.DevOnly, training.DevOnly }, Is.All.True);
            Assert.That(new[] { resource.NonLive, upgrade.NonLive, training.NonLive }, Is.All.True);
            Assert.That(resource.OfficialEconomyRequested, Is.False);
            Assert.That(new[] { upgrade.OfficialProgressionRequested, training.OfficialProgressionRequested }, Is.All.False);
        });
    }

    [Test]
    public void HiveActionLoopDevOnlySnapshotsAndSavePlanRemainFutureOnly()
    {
        PlayerId playerId = PlayerId.New();
        WorldId worldId = WorldId.New();
        GameServerId gameServerId = GameServerId.New();
        BuildingId buildingId = BuildingId.New();
        HiveActionLoopFutureSnapshotSet snapshots = new(
            new HiveResourcesSnapshotDevOnly(playerId, worldId, gameServerId, SnapshotRevision: 5, [new HiveResourceAmountDevOnly("honey", 100, 500)]),
            new HiveBuildingStateSnapshotDevOnly(playerId, worldId, gameServerId, SnapshotRevision: 5, [new HiveBuildingStateDevOnly(buildingId, "honey_storage", Level: 1, UpgradeRunning: false)]),
            new HiveTrainingQueueSnapshotDevOnly(playerId, worldId, gameServerId, SnapshotRevision: 5, [new HiveTrainingQueueItemDevOnly(Guid.NewGuid(), "worker_bee", Quantity: 2, DateTimeOffset.UtcNow.AddMinutes(1))]),
            new HiveArmySnapshotDevOnlyContract(playerId, worldId, gameServerId, SnapshotRevision: 5, [new HiveArmyCountDevOnly("worker_bee", LocalCount: 2)], DevOnly: true, NonLive: true, OfficialPersistentArmyClaimed: false, ContractVersion.Current),
            DevOnly: true,
            NonLive: true,
            OfficialSaveApplied: false);
        HiveOfficialSaveFuturePreparation savePlan = HiveOfficialSaveFuturePreparation.Create();

        Assert.Multiple(() =>
        {
            Assert.That(snapshots.DevOnly, Is.True);
            Assert.That(snapshots.NonLive, Is.True);
            Assert.That(snapshots.OfficialSaveApplied, Is.False);
            Assert.That(snapshots.ArmySnapshot.OfficialPersistentArmyClaimed, Is.False);
            Assert.That(savePlan.Prepared, Is.True);
            Assert.That(savePlan.Activated, Is.False);
            Assert.That(savePlan.OfficialSaveClaimAllowed, Is.False);
            Assert.That(savePlan.ProductionSqlAllowed, Is.False);
            Assert.That(savePlan.FutureSnapshotKinds, Is.EquivalentTo(new[]
            {
                HiveActionLoopSnapshotKinds.Resources,
                HiveActionLoopSnapshotKinds.BuildingState,
                HiveActionLoopSnapshotKinds.TrainingQueue,
                HiveActionLoopSnapshotKinds.LocalArmyCounts
            }));
        });
    }

    [Test]
    public void HiveActionLoopDevOnlyErrorsCoverFutureServerRejections()
    {
        HiveActionLoopDevOnlyErrorCode[] expected =
        [
            HiveActionLoopDevOnlyErrorCode.InsufficientResources,
            HiveActionLoopDevOnlyErrorCode.AlreadyRunning,
            HiveActionLoopDevOnlyErrorCode.QueueBusy,
            HiveActionLoopDevOnlyErrorCode.CapReached,
            HiveActionLoopDevOnlyErrorCode.StaleSnapshot,
            HiveActionLoopDevOnlyErrorCode.IdempotencyConflict,
            HiveActionLoopDevOnlyErrorCode.UnknownCatalogEntry,
            HiveActionLoopDevOnlyErrorCode.Conflict
        ];

        Assert.Multiple(() =>
        {
            Assert.That(Enum.GetValues<HiveActionLoopDevOnlyErrorCode>(), Is.SupersetOf(expected));
            Assert.That(HiveActionLoopDevOnlyRejectionCatalog.RequiredCodes, Is.SupersetOf(expected));
        });
    }

    [Test]
    public void HiveActionLoopDevOnlyResponsesCoverBuilderAVisibleStatesWithoutOfficialClaims()
    {
        HiveActionLoopDevOnlyResponseStatus[] expected =
        [
            HiveActionLoopDevOnlyResponseStatus.Accepted,
            HiveActionLoopDevOnlyResponseStatus.Rejected,
            HiveActionLoopDevOnlyResponseStatus.Pending,
            HiveActionLoopDevOnlyResponseStatus.StaleSnapshot,
            HiveActionLoopDevOnlyResponseStatus.Conflict,
            HiveActionLoopDevOnlyResponseStatus.CapReached,
            HiveActionLoopDevOnlyResponseStatus.InsufficientResources,
            HiveActionLoopDevOnlyResponseStatus.AlreadyRunning,
            HiveActionLoopDevOnlyResponseStatus.QueueBusy
        ];
        HiveActionLoopDevOnlyServerResponse accepted = new(
            Guid.NewGuid(),
            HiveActionLoopDevOnlyResponseStatus.Accepted,
            ErrorCode: null,
            Message: "Accepted for dev-only readiness.",
            ServerSnapshotRevision: 21,
            SnapshotVersion: "dev-only-hive-action-loop-v1",
            DevOnly: true,
            NonLive: true,
            OfficialProgressionApplied: false,
            OfficialSaveApplied: false,
            OfficialEconomyApplied: false,
            OfficialPersistentArmyApplied: false,
            ContractVersion.Current);

        Assert.Multiple(() =>
        {
            Assert.That(HiveActionLoopDevOnlyRejectionCatalog.RequiredStatuses, Is.SupersetOf(expected));
            Assert.That(accepted.DevOnly, Is.True);
            Assert.That(accepted.NonLive, Is.True);
            Assert.That(accepted.OfficialProgressionApplied, Is.False);
            Assert.That(accepted.OfficialSaveApplied, Is.False);
            Assert.That(accepted.OfficialEconomyApplied, Is.False);
            Assert.That(accepted.OfficialPersistentArmyApplied, Is.False);
        });
    }

    [Test]
    public void HiveActionLoopDevOnlySnapshotEnvelopeCarriesVersionRevisionAndLocalArmyOnly()
    {
        PlayerId playerId = PlayerId.New();
        WorldId worldId = WorldId.New();
        GameServerId gameServerId = GameServerId.New();
        BuildingId buildingId = BuildingId.New();
        HiveActionLoopSnapshotEnvelopeDevOnly envelope = new(
            SnapshotVersion: "dev-only-hive-action-loop-v1",
            SnapshotRevision: 44,
            DateTimeOffset.UtcNow,
            new HiveResourcesSnapshotDevOnly(playerId, worldId, gameServerId, SnapshotRevision: 44, [new HiveResourceAmountDevOnly("honey", 120, 500)]),
            new HiveBuildingStateSnapshotDevOnly(playerId, worldId, gameServerId, SnapshotRevision: 44, [new HiveBuildingStateDevOnly(buildingId, "nursery", Level: 2, UpgradeRunning: false)]),
            new HiveTrainingQueueSnapshotDevOnly(playerId, worldId, gameServerId, SnapshotRevision: 44, [new HiveTrainingQueueItemDevOnly(Guid.NewGuid(), "worker_bee", Quantity: 1, DateTimeOffset.UtcNow.AddMinutes(2))]),
            new HiveArmySnapshotDevOnlyContract(playerId, worldId, gameServerId, SnapshotRevision: 44, [new HiveArmyCountDevOnly("worker_bee", LocalCount: 3)], DevOnly: true, NonLive: true, OfficialPersistentArmyClaimed: false, ContractVersion.Current),
            DevOnly: true,
            NonLive: true,
            OfficialSaveApplied: false,
            ContractVersion.Current);

        Assert.Multiple(() =>
        {
            Assert.That(envelope.SnapshotVersion, Is.EqualTo("dev-only-hive-action-loop-v1"));
            Assert.That(envelope.SnapshotRevision, Is.EqualTo(44));
            Assert.That(envelope.Resources.SnapshotRevision, Is.EqualTo(44));
            Assert.That(envelope.BuildingState.SnapshotRevision, Is.EqualTo(44));
            Assert.That(envelope.TrainingQueue.SnapshotRevision, Is.EqualTo(44));
            Assert.That(envelope.LocalArmySnapshot.SnapshotRevision, Is.EqualTo(44));
            Assert.That(envelope.DevOnly, Is.True);
            Assert.That(envelope.NonLive, Is.True);
            Assert.That(envelope.OfficialSaveApplied, Is.False);
            Assert.That(envelope.LocalArmySnapshot.OfficialPersistentArmyClaimed, Is.False);
        });
    }

    [Test]
    public void HiveActionLoopDevOnlyReconciliationSeparatesLocalPreviewFromFutureServerAuthority()
    {
        PlayerId playerId = PlayerId.New();
        WorldId worldId = WorldId.New();
        GameServerId gameServerId = GameServerId.New();
        HiveSnapshotVersionRevisionDevOnly revision = new(
            playerId,
            worldId,
            gameServerId,
            SnapshotVersion: "dev-only-hive-action-loop-v1",
            LocalSnapshotRevision: 8,
            ServerSnapshotRevision: 11,
            DevOnly: true,
            NonLive: true,
            OfficialSaveApplied: false,
            ContractVersion.Current);
        HiveLocalServerReconciliationDevOnlyContract reconciliation = new(
            Guid.NewGuid(),
            playerId,
            worldId,
            gameServerId,
            SnapshotVersion: revision.SnapshotVersion,
            LocalSnapshotRevision: revision.LocalSnapshotRevision,
            ServerSnapshotRevision: revision.ServerSnapshotRevision,
            HiveLocalServerReconciliationOutcome.ConflictRequiresFutureOfficialAuthority,
            HiveActionLoopDevOnlyErrorCode.Conflict,
            DevOnly: true,
            NonLive: true,
            OfficialSaveApplied: false,
            OfficialProgressionApplied: false,
            ContractVersion.Current);

        Assert.Multiple(() =>
        {
            Assert.That(revision.ServerSnapshotRevision, Is.GreaterThan(revision.LocalSnapshotRevision));
            Assert.That(reconciliation.Outcome, Is.EqualTo(HiveLocalServerReconciliationOutcome.ConflictRequiresFutureOfficialAuthority));
            Assert.That(reconciliation.ErrorCode, Is.EqualTo(HiveActionLoopDevOnlyErrorCode.Conflict));
            Assert.That(new[] { revision.DevOnly, reconciliation.DevOnly }, Is.All.True);
            Assert.That(new[] { revision.NonLive, reconciliation.NonLive }, Is.All.True);
            Assert.That(reconciliation.OfficialSaveApplied, Is.False);
            Assert.That(reconciliation.OfficialProgressionApplied, Is.False);
        });
    }

    [Test]
    public void HiveActionLoopFutureOfficialPersistenceInventoryStaysNonLive()
    {
        HiveOfficialPersistenceRequirementsInventory inventory = HiveActionLoopFutureOfficialPrep.CreatePersistenceInventory();

        Assert.Multiple(() =>
        {
            Assert.That(inventory.EvidenceId, Is.EqualTo("SERVER-044-BEE-887-BEE-891"));
            Assert.That(inventory.DevOnly, Is.True);
            Assert.That(inventory.NonLive, Is.True);
            Assert.That(inventory.OfficialSaveActive, Is.False);
            Assert.That(inventory.OfficialEconomyActive, Is.False);
            Assert.That(inventory.OfficialPersistentArmyActive, Is.False);
            Assert.That(inventory.ProductionMigrationAllowed, Is.False);
            Assert.That(inventory.OfficialEndpointAllowed, Is.False);
            Assert.That(inventory.WorldMapScopeAllowed, Is.False);
            Assert.That(inventory.CandidateData.Select(candidate => candidate.FutureStorageName), Is.SupersetOf(new[]
            {
                "player_resources",
                "hive_buildings",
                "training_queue",
                "troop_counts",
                "hive_action_history",
                "hive_snapshot_revisions"
            }));
            Assert.That(inventory.CandidateData.Select(candidate => candidate.AllowedNow), Is.All.False);
            Assert.That(inventory.ForbiddenLiveClaims, Does.Contain("official save"));
            Assert.That(inventory.RequiredFutureGates, Does.Contain("QA official save validation"));
        });
    }

    [Test]
    public void HiveActionLoopFutureIdempotencyReplaySafetyDistinguishesSamePayloadFromConflict()
    {
        HiveFutureIdempotencyReplaySafetyPolicy policy = new(
            DevOnly: true,
            NonLive: true,
            IdempotencyKeyRequired: true,
            PayloadHashRequired: true,
            SamePayloadReturnsSameResult: true,
            DifferentPayloadRejectedAsConflict: true,
            OfficialProgressionApplied: false,
            OfficialEconomyApplied: false,
            ContractVersion.Current);

        Assert.Multiple(() =>
        {
            Assert.That(policy.DecideReplay("payload-a", "payload-a"), Is.EqualTo(HiveFutureIdempotencyReplayDecision.SamePayloadReplay));
            Assert.That(policy.DecideReplay("payload-a", "payload-b"), Is.EqualTo(HiveFutureIdempotencyReplayDecision.DifferentPayloadConflict));
            Assert.That(policy.IdempotencyKeyRequired, Is.True);
            Assert.That(policy.PayloadHashRequired, Is.True);
            Assert.That(policy.OfficialProgressionApplied, Is.False);
            Assert.That(policy.OfficialEconomyApplied, Is.False);
        });
    }

    [Test]
    public void HiveActionLoopSnapshotDeltaAuditCarriesRevisionDiffWithoutOfficialSave()
    {
        HiveSnapshotDeltaAuditDevOnlyContract audit = new(
            Guid.NewGuid(),
            PlayerId.New(),
            WorldId.New(),
            GameServerId.New(),
            HiveActionLoopFutureOfficialPrep.SnapshotVersion,
            BeforeRevision: 9,
            AfterRevision: 10,
            Deltas:
            [
                new HiveSnapshotDeltaEntryDevOnly(HiveActionLoopSnapshotKinds.Resources, "honey", "120", "95", "future upgrade cost audit"),
                new HiveSnapshotDeltaEntryDevOnly(HiveActionLoopSnapshotKinds.BuildingState, "nursery.level", "1", "2", "future upgrade completion audit"),
                new HiveSnapshotDeltaEntryDevOnly(HiveActionLoopSnapshotKinds.TrainingQueue, "worker_bee.quantity", "0", "2", "future training queue audit"),
                new HiveSnapshotDeltaEntryDevOnly(HiveActionLoopSnapshotKinds.LocalArmyCounts, "worker_bee", "2", "4", "future local army audit")
            ],
            DevOnly: true,
            NonLive: true,
            OfficialSaveApplied: false,
            OfficialProgressionApplied: false,
            ContractVersion.Current);

        Assert.Multiple(() =>
        {
            Assert.That(audit.AfterRevision, Is.GreaterThan(audit.BeforeRevision));
            Assert.That(audit.Deltas.Select(delta => delta.SnapshotKind), Is.SupersetOf(new[]
            {
                HiveActionLoopSnapshotKinds.Resources,
                HiveActionLoopSnapshotKinds.BuildingState,
                HiveActionLoopSnapshotKinds.TrainingQueue,
                HiveActionLoopSnapshotKinds.LocalArmyCounts
            }));
            Assert.That(audit.DevOnly, Is.True);
            Assert.That(audit.NonLive, Is.True);
            Assert.That(audit.OfficialSaveApplied, Is.False);
            Assert.That(audit.OfficialProgressionApplied, Is.False);
        });
    }

    [Test]
    public void HiveActionLoopReconciliationDrillBlocksOfficialRestoreAndLiveHandler()
    {
        HiveLocalServerReconciliationDrillDevOnly drill = new(
            Guid.NewGuid(),
            PlayerId.New(),
            WorldId.New(),
            GameServerId.New(),
            HiveActionLoopFutureOfficialPrep.SnapshotVersion,
            LocalSnapshotRevision: 4,
            FutureServerSnapshotRevision: 6,
            HiveLocalServerReconciliationOutcome.RefreshRequired,
            HiveActionLoopDevOnlyErrorCode.StaleSnapshot,
            PlayerFacingState: "server_required_preview",
            DevOnly: true,
            NonLive: true,
            OfficialRestoreApplied: false,
            OfficialSaveApplied: false,
            HandlerLive: false,
            ContractVersion.Current);

        Assert.Multiple(() =>
        {
            Assert.That(drill.FutureServerSnapshotRevision, Is.GreaterThan(drill.LocalSnapshotRevision));
            Assert.That(drill.Outcome, Is.EqualTo(HiveLocalServerReconciliationOutcome.RefreshRequired));
            Assert.That(drill.ErrorCode, Is.EqualTo(HiveActionLoopDevOnlyErrorCode.StaleSnapshot));
            Assert.That(drill.OfficialRestoreApplied, Is.False);
            Assert.That(drill.OfficialSaveApplied, Is.False);
            Assert.That(drill.HandlerLive, Is.False);
        });
    }

    [Test]
    public void HiveActionLoopFutureAuthoritativeHandlerHandoffListsFutureWorkWithoutActivation()
    {
        HiveFutureAuthoritativeActionHandlerHandoff handoff = HiveActionLoopFutureOfficialPrep.CreateHandlerHandoff();

        Assert.Multiple(() =>
        {
            Assert.That(handoff.EvidenceId, Is.EqualTo("SERVER-044-BEE-887-BEE-891"));
            Assert.That(handoff.FutureHandlers, Is.SupersetOf(new[]
            {
                "ResourceCommandHandler",
                "UpgradeCommandHandler",
                "TrainingCommandHandler",
                "SnapshotReadHandler",
                "ReconciliationDecisionHandler"
            }));
            Assert.That(handoff.FutureRepositories, Does.Contain("IdempotencyRecordsRepository"));
            Assert.That(handoff.FutureEndpoints, Does.Contain("GET /players/{playerId}/hive/snapshot"));
            Assert.That(handoff.DevOnly, Is.True);
            Assert.That(handoff.NonLive, Is.True);
            Assert.That(handoff.HandlerLive, Is.False);
            Assert.That(handoff.RepositoryLive, Is.False);
            Assert.That(handoff.MigrationLive, Is.False);
            Assert.That(handoff.OfficialEndpointLive, Is.False);
            Assert.That(handoff.OfficialSaveActive, Is.False);
            Assert.That(handoff.RequiredFutureGates, Does.Contain("Production publish authorization"));
        });
    }

    [Test]
    public void HiveActionLoopEvidencePrepNonClaimGuardBlocksOfficialClaims()
    {
        HiveOfficialPersistenceNonClaimGuard guard = HiveActionLoopEvidencePrep.CreateNonClaimGuard();

        Assert.Multiple(() =>
        {
            Assert.That(guard.EvidenceId, Is.EqualTo("SERVER-045-BEE-914-BEE-916"));
            Assert.That(guard.DevOnly, Is.True);
            Assert.That(guard.NonLive, Is.True);
            Assert.That(guard.OfficialLiveServerClaimAllowed, Is.False);
            Assert.That(guard.OfficialEndpointClaimAllowed, Is.False);
            Assert.That(guard.OfficialSaveClaimAllowed, Is.False);
            Assert.That(guard.OfficialEconomyClaimAllowed, Is.False);
            Assert.That(guard.OfficialPersistentArmyClaimAllowed, Is.False);
            Assert.That(guard.ProductionMigrationAllowed, Is.False);
            Assert.That(guard.ProductionPublishAllowed, Is.False);
            Assert.That(guard.Bee881ScopeAllowed, Is.False);
            Assert.That(guard.WorldMapScopeAllowed, Is.False);
            Assert.That(guard.RequiredLabels, Is.SupersetOf(new[]
            {
                "local_preview",
                "dev_only",
                "future_server_required",
                "not_official_save",
                "not_official_endpoint"
            }));
            Assert.That(guard.ForbiddenLabels, Does.Contain("officially saved"));
        });
    }

    [Test]
    public void HiveActionLoopEvidencePrepIdempotencyReplayFieldsStayDevOnly()
    {
        HiveIdempotencyReplayEvidenceFieldSet evidence = new(
            HiveActionLoopEvidencePrep.EvidenceId,
            Guid.NewGuid(),
            PlayerId.New(),
            WorldId.New(),
            GameServerId.New(),
            ActionKind: "upgrade",
            IdempotencyKeyLabel: "idem-key-present",
            PayloadHashLabel: "payload-hash-present",
            HiveFutureIdempotencyReplayDecision.SamePayloadReplay,
            HiveActionLoopDevOnlyResponseStatus.Accepted,
            SamePayloadReturnedSameResult: true,
            DifferentPayloadRejectedAsConflict: true,
            CostAppliedOnce: true,
            QueueCreatedOnce: true,
            DevOnly: true,
            NonLive: true,
            OfficialProgressionApplied: false,
            OfficialEconomyApplied: false,
            ContractVersion.Current);

        Assert.Multiple(() =>
        {
            Assert.That(evidence.IdempotencyKeyLabel, Is.Not.Empty);
            Assert.That(evidence.PayloadHashLabel, Is.Not.Empty);
            Assert.That(evidence.SamePayloadReturnedSameResult, Is.True);
            Assert.That(evidence.DifferentPayloadRejectedAsConflict, Is.True);
            Assert.That(evidence.CostAppliedOnce, Is.True);
            Assert.That(evidence.QueueCreatedOnce, Is.True);
            Assert.That(evidence.DevOnly, Is.True);
            Assert.That(evidence.NonLive, Is.True);
            Assert.That(evidence.OfficialProgressionApplied, Is.False);
            Assert.That(evidence.OfficialEconomyApplied, Is.False);
        });
    }

    [Test]
    public void HiveActionLoopEvidencePrepSnapshotDeltaReconciliationFieldsDoNotClaimRestoreOrSave()
    {
        HiveSnapshotDeltaReconciliationEvidenceFieldSet evidence = new(
            HiveActionLoopEvidencePrep.EvidenceId,
            Guid.NewGuid(),
            PlayerId.New(),
            WorldId.New(),
            GameServerId.New(),
            HiveActionLoopFutureOfficialPrep.SnapshotVersion,
            BeforeRevision: 15,
            AfterRevision: 16,
            Deltas:
            [
                new HiveSnapshotDeltaEntryDevOnly(HiveActionLoopSnapshotKinds.Resources, "honey", "200", "175", "single upgrade cost evidence"),
                new HiveSnapshotDeltaEntryDevOnly(HiveActionLoopSnapshotKinds.BuildingState, "nursery.level", "1", "2", "upgrade result evidence")
            ],
            HiveLocalServerReconciliationOutcome.RefreshRequired,
            HiveActionLoopDevOnlyErrorCode.StaleSnapshot,
            DevOnly: true,
            NonLive: true,
            OfficialRestoreApplied: false,
            OfficialSaveApplied: false,
            OfficialEndpointUsed: false,
            ContractVersion.Current);

        Assert.Multiple(() =>
        {
            Assert.That(evidence.AfterRevision, Is.GreaterThan(evidence.BeforeRevision));
            Assert.That(evidence.Deltas, Has.Count.EqualTo(2));
            Assert.That(evidence.ReconciliationOutcome, Is.EqualTo(HiveLocalServerReconciliationOutcome.RefreshRequired));
            Assert.That(evidence.ReconciliationError, Is.EqualTo(HiveActionLoopDevOnlyErrorCode.StaleSnapshot));
            Assert.That(evidence.DevOnly, Is.True);
            Assert.That(evidence.NonLive, Is.True);
            Assert.That(evidence.OfficialRestoreApplied, Is.False);
            Assert.That(evidence.OfficialSaveApplied, Is.False);
            Assert.That(evidence.OfficialEndpointUsed, Is.False);
        });
    }

    [Test]
    public void HiveActionLoopEvidencePrepQaChecklistKeepsLocalHiveHonestAboutServer()
    {
        HiveActionLoopEvidenceQaChecklist checklist = HiveActionLoopEvidencePrep.CreateQaChecklist();

        Assert.Multiple(() =>
        {
            Assert.That(checklist.EvidenceId, Is.EqualTo("SERVER-045-BEE-914-BEE-916"));
            Assert.That(checklist.Criteria, Does.Contain("Evidence does not claim official live server."));
            Assert.That(checklist.Criteria, Does.Contain("Evidence does not claim official endpoint."));
            Assert.That(checklist.Criteria, Does.Contain("Evidence does not claim official save."));
            Assert.That(checklist.Criteria, Does.Contain("Idempotency evidence includes idempotency key label, payload hash label, replay result and conflict result."));
            Assert.That(checklist.Criteria, Does.Contain("Snapshot evidence includes snapshot version, before revision, after revision, delta list and reconciliation outcome."));
            Assert.That(checklist.DevOnly, Is.True);
            Assert.That(checklist.NonLive, Is.True);
            Assert.That(checklist.OfficialQaClosureClaimAllowed, Is.False);
        });
    }

    [Test]
    public void HiveActionLoopEvidenceCarryForwardPreservesNonClaimLabelsForDemo075Qa075()
    {
        HiveNonClaimEvidenceCarryForward carryForward = HiveActionLoopEvidencePrep.CreateCarryForward();

        Assert.Multiple(() =>
        {
            Assert.That(carryForward.EvidenceId, Is.EqualTo("SERVER-046-BEE-935-BEE-937"));
            Assert.That(carryForward.SourceEvidenceId, Is.EqualTo("SERVER-045-BEE-914-BEE-916"));
            Assert.That(carryForward.TargetDemo, Is.EqualTo("DEMO-075"));
            Assert.That(carryForward.TargetQa, Is.EqualTo("QA-075"));
            Assert.That(carryForward.DevOnly, Is.True);
            Assert.That(carryForward.NonLive, Is.True);
            Assert.That(carryForward.OfficialLiveServerClaimAllowed, Is.False);
            Assert.That(carryForward.OfficialEndpointClaimAllowed, Is.False);
            Assert.That(carryForward.OfficialSaveClaimAllowed, Is.False);
            Assert.That(carryForward.OfficialEconomyClaimAllowed, Is.False);
            Assert.That(carryForward.OfficialPersistentArmyClaimAllowed, Is.False);
            Assert.That(carryForward.ProductionMigrationAllowed, Is.False);
            Assert.That(carryForward.ProductionDeploymentAllowed, Is.False);
            Assert.That(carryForward.RequiredCarryForwardLabels, Is.SupersetOf(new[]
            {
                "local_preview",
                "demo_proof",
                "official_live_false",
                "official_endpoint_false",
                "official_save_false",
                "official_economy_false",
                "official_persistent_army_false"
            }));
            Assert.That(carryForward.RequiredQaChecks, Does.Contain("Reject any live endpoint claim."));
        });
    }

    [Test]
    public void HiveActionLoopIdempotencySnapshotEvidenceContinuityCarriesFieldsWithoutLiveUse()
    {
        HiveIdempotencySnapshotEvidenceContinuity continuity = new(
            HiveActionLoopEvidencePrep.CarryForwardEvidenceId,
            HiveActionLoopEvidencePrep.EvidenceId,
            Guid.NewGuid(),
            IdempotencyKeyLabelCarried: true,
            PayloadHashLabelCarried: true,
            SamePayloadReplayCarried: true,
            DifferentPayloadConflictCarried: true,
            CostAppliedOnceCarried: true,
            QueueCreatedOnceCarried: true,
            SnapshotVersionCarried: true,
            BeforeAfterRevisionCarried: true,
            DeltaListCarried: true,
            ReconciliationOutcomeCarried: true,
            DevOnly: true,
            NonLive: true,
            OfficialEndpointUsed: false,
            OfficialSaveApplied: false,
            ContractVersion.Current);

        Assert.Multiple(() =>
        {
            Assert.That(continuity.SourceEvidenceId, Is.EqualTo(HiveActionLoopEvidencePrep.EvidenceId));
            Assert.That(continuity.IdempotencyKeyLabelCarried, Is.True);
            Assert.That(continuity.PayloadHashLabelCarried, Is.True);
            Assert.That(continuity.SamePayloadReplayCarried, Is.True);
            Assert.That(continuity.DifferentPayloadConflictCarried, Is.True);
            Assert.That(continuity.CostAppliedOnceCarried, Is.True);
            Assert.That(continuity.QueueCreatedOnceCarried, Is.True);
            Assert.That(continuity.SnapshotVersionCarried, Is.True);
            Assert.That(continuity.BeforeAfterRevisionCarried, Is.True);
            Assert.That(continuity.DeltaListCarried, Is.True);
            Assert.That(continuity.ReconciliationOutcomeCarried, Is.True);
            Assert.That(continuity.DevOnly, Is.True);
            Assert.That(continuity.NonLive, Is.True);
            Assert.That(continuity.OfficialEndpointUsed, Is.False);
            Assert.That(continuity.OfficialSaveApplied, Is.False);
        });
    }

    [Test]
    public void HiveActionLoopPreviewDemoLiveStateMatrixKeepsOfficialLiveFalse()
    {
        HivePreviewDemoLiveStateMatrix matrix = HiveActionLoopEvidencePrep.CreatePreviewDemoLiveStateMatrix();

        Assert.Multiple(() =>
        {
            Assert.That(matrix.EvidenceId, Is.EqualTo("SERVER-046-BEE-935-BEE-937"));
            Assert.That(matrix.DevOnly, Is.True);
            Assert.That(matrix.NonLive, Is.True);
            Assert.That(matrix.Rows.Select(row => row.Domain), Is.SupersetOf(new[]
            {
                "resource_collect",
                "upgrade_building",
                "training_queue",
                "idempotency_replay",
                "snapshot_delta"
            }));
            Assert.That(matrix.Rows.Select(row => row.LocalPreview), Is.All.True);
            Assert.That(matrix.Rows.Select(row => row.DemoProofAllowed), Is.All.True);
            Assert.That(matrix.Rows.Select(row => row.OfficialLiveAllowed), Is.All.False);
            Assert.That(matrix.Rows.Select(row => row.FalseClaimRisk), Does.Contain("snapshot delta must not imply official restore"));
        });
    }

    [Test]
    public void HiveServerFutureSupportManifestCarriesDemo076NonClaims()
    {
        HiveServerFutureSupportNonClaimManifest manifest = HiveActionLoopEvidencePrep.CreateServerFutureSupportManifest();

        Assert.Multiple(() =>
        {
            Assert.That(manifest.EvidenceId, Is.EqualTo("SERVER-047-BEE-958"));
            Assert.That(manifest.TargetDemo, Is.EqualTo("DEMO-076"));
            Assert.That(manifest.TargetQa, Is.EqualTo("QA-076"));
            Assert.That(manifest.LocalPreview, Is.True);
            Assert.That(manifest.DemoProof, Is.True);
            Assert.That(manifest.ApkTraceable, Is.True);
            Assert.That(manifest.PhysicalDeviceProofPendingAllowed, Is.True);
            Assert.That(manifest.OfficialServerLive, Is.False);
            Assert.That(manifest.Endpoint, Is.False);
            Assert.That(manifest.Save, Is.False);
            Assert.That(manifest.Economy, Is.False);
            Assert.That(manifest.ArmyPersistence, Is.False);
            Assert.That(manifest.ProductionMigrationAllowed, Is.False);
            Assert.That(manifest.ProductionDeploymentAllowed, Is.False);
            Assert.That(manifest.RequiredDemo076Fields, Is.SupersetOf(new[]
            {
                "official_server_live",
                "endpoint",
                "save",
                "economy",
                "army_persistence"
            }));
            Assert.That(manifest.ForbiddenDemo076Claims, Does.Contain("official save active"));
        });
    }

    [Test]
    public void HiveServerFutureSupportQaChecklistDetectsFalseLiveClaims()
    {
        HiveServerFutureSupportQaLiveClaimChecklist checklist = HiveActionLoopEvidencePrep.CreateServerFutureSupportQaChecklist();

        Assert.Multiple(() =>
        {
            Assert.That(checklist.EvidenceId, Is.EqualTo("SERVER-047-BEE-958"));
            Assert.That(checklist.Criteria, Does.Contain("Reject any official live server claim."));
            Assert.That(checklist.Criteria, Does.Contain("Reject any official endpoint claim."));
            Assert.That(checklist.Criteria, Does.Contain("Reject any official save claim."));
            Assert.That(checklist.Criteria, Does.Contain("Reject any official economy claim."));
            Assert.That(checklist.Criteria, Does.Contain("Reject any official persistent army claim."));
            Assert.That(checklist.Criteria, Does.Contain("Reject any production migration or deployment claim."));
            Assert.That(checklist.Criteria, Does.Contain("Confirm local preview, demo proof, APK traceable and official server are separate states."));
            Assert.That(checklist.DevOnly, Is.True);
            Assert.That(checklist.NonLive, Is.True);
            Assert.That(checklist.OfficialLivePassAllowed, Is.False);
        });
    }

    [Test]
    public void HiveOfficialServerClaimBoundarySeparatesLocalDemoPhysicalAndOfficialServer()
    {
        HiveOfficialServerClaimBoundary boundary = HiveActionLoopEvidencePrep.CreateOfficialServerClaimBoundary();

        Assert.Multiple(() =>
        {
            Assert.That(boundary.EvidenceId, Is.EqualTo("SERVER-048-BEE-975"));
            Assert.That(boundary.TargetDemo, Is.EqualTo("DEMO-077"));
            Assert.That(boundary.TargetQa, Is.EqualTo("QA-077"));
            Assert.That(boundary.LocalDemoProofAllowed, Is.True);
            Assert.That(boundary.PhysicalDeviceProofSeparate, Is.True);
            Assert.That(boundary.PhysicalDeviceProofPendingAllowed, Is.True);
            Assert.That(boundary.OfficialServerLiveAllowed, Is.False);
            Assert.That(boundary.OfficialEndpointAllowed, Is.False);
            Assert.That(boundary.OfficialSaveAllowed, Is.False);
            Assert.That(boundary.OfficialEconomyAllowed, Is.False);
            Assert.That(boundary.OfficialPersistentArmyAllowed, Is.False);
            Assert.That(boundary.ProductionMigrationAllowed, Is.False);
            Assert.That(boundary.ProductionDeploymentAllowed, Is.False);
            Assert.That(boundary.RequiredManifestFields, Is.SupersetOf(new[]
            {
                "local_demo_proof",
                "physical_device_proof",
                "official_server_live",
                "endpoint",
                "save",
                "economy",
                "army_persistence"
            }));
            Assert.That(boundary.ForbiddenTerms, Does.Contain("Serveur officiel"));
            Assert.That(boundary.ForbiddenTerms, Does.Contain("Sauvegarde officielle"));
        });
    }

    [Test]
    public void HiveOfficialServerClaimBoundaryQaCriteriaRejectsEveryFalseOfficialClaim()
    {
        HiveOfficialServerClaimBoundaryQaCriteria criteria = HiveActionLoopEvidencePrep.CreateOfficialServerClaimBoundaryQaCriteria();

        Assert.Multiple(() =>
        {
            Assert.That(criteria.EvidenceId, Is.EqualTo("SERVER-048-BEE-975"));
            Assert.That(criteria.Criteria, Does.Contain("Reject any artifact claiming official live server."));
            Assert.That(criteria.Criteria, Does.Contain("Reject any artifact claiming an official endpoint is active."));
            Assert.That(criteria.Criteria, Does.Contain("Reject any artifact claiming official save."));
            Assert.That(criteria.Criteria, Does.Contain("Reject any artifact claiming official economy."));
            Assert.That(criteria.Criteria, Does.Contain("Reject any artifact claiming official persistent army."));
            Assert.That(criteria.Criteria, Does.Contain("Reject any artifact merging local/demo proof with physical device proof."));
            Assert.That(criteria.Criteria, Does.Contain("Confirm local/demo proof, physical device proof and official server are separate statuses."));
            Assert.That(criteria.DevOnly, Is.True);
            Assert.That(criteria.NonLive, Is.True);
            Assert.That(criteria.OfficialServerClaimAllowed, Is.False);
        });
    }

    [Test]
    public void HiveServerLiveClaimVisualGuardRequiresRealVisualArtifactAndNoOfficialClaims()
    {
        HiveServerLiveClaimVisualGuard guard = HiveActionLoopEvidencePrep.CreateServerLiveClaimVisualGuard();

        Assert.Multiple(() =>
        {
            Assert.That(guard.EvidenceId, Is.EqualTo("SERVER-049-BEE-997"));
            Assert.That(guard.TargetDemo, Is.EqualTo("DEMO-078"));
            Assert.That(guard.TargetQa, Is.EqualTo("QA-078"));
            Assert.That(guard.RequiresRealImageOrVideoArtifact, Is.True);
            Assert.That(guard.TextPlanCountsAsVisualProof, Is.False);
            Assert.That(guard.PhysicalDeviceProofSeparate, Is.True);
            Assert.That(guard.PhysicalDeviceProofPendingWithoutRealDeviceArtifacts, Is.True);
            Assert.That(guard.OfficialServerLiveAllowed, Is.False);
            Assert.That(guard.OfficialEndpointAllowed, Is.False);
            Assert.That(guard.OfficialSaveAllowed, Is.False);
            Assert.That(guard.OfficialEconomyAllowed, Is.False);
            Assert.That(guard.OfficialPersistentArmyAllowed, Is.False);
            Assert.That(guard.ProductionMigrationAllowed, Is.False);
            Assert.That(guard.ProductionDeploymentAllowed, Is.False);
            Assert.That(guard.RequiredArtifactFields, Is.SupersetOf(new[]
            {
                "artifact_path",
                "artifact_kind",
                "dimensions",
                "caption",
                "local_demo",
                "physical_device",
                "official_server_live",
                "endpoint",
                "save",
                "economy",
                "army_persistence"
            }));
            Assert.That(guard.ForbiddenVisualClaims, Does.Contain("official save"));
            Assert.That(guard.ForbiddenVisualClaims, Does.Contain("physical device proof complete"));
        });
    }

    [Test]
    public void HiveServerLiveClaimVisualGuardQaCriteriaRejectsFalseVisualServerClaims()
    {
        HiveServerLiveClaimVisualGuardQaCriteria criteria = HiveActionLoopEvidencePrep.CreateServerLiveClaimVisualGuardQaCriteria();

        Assert.Multiple(() =>
        {
            Assert.That(criteria.EvidenceId, Is.EqualTo("SERVER-049-BEE-997"));
            Assert.That(criteria.Criteria, Does.Contain("Reject any screenshot, contact sheet, video or manifest claiming official server/live."));
            Assert.That(criteria.Criteria, Does.Contain("Reject any screenshot, contact sheet, video or manifest claiming official endpoint."));
            Assert.That(criteria.Criteria, Does.Contain("Reject any screenshot, contact sheet, video or manifest claiming official save."));
            Assert.That(criteria.Criteria, Does.Contain("Reject any screenshot, contact sheet, video or manifest claiming official economy."));
            Assert.That(criteria.Criteria, Does.Contain("Reject any screenshot, contact sheet, video or manifest claiming official persistent army."));
            Assert.That(criteria.Criteria, Does.Contain("Reject any visual proof declared without a real image or video file."));
            Assert.That(criteria.Criteria, Does.Contain("Accept textual plans only as support, never as visual proof."));
            Assert.That(criteria.DevOnly, Is.True);
            Assert.That(criteria.NonLive, Is.True);
            Assert.That(criteria.OfficialLiveClaimAllowed, Is.False);
        });
    }

    [Test]
    public void OfficialAuthFoundationKeepsServerAuthoritativeButNonLive()
    {
        OfficialAuthFoundationDescriptor descriptor = OfficialAuthFoundation.CreateDescriptor();

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.EvidenceId, Is.EqualTo("SERVER-052-OFFICIAL-AUTH-FOUNDATION"));
            Assert.That(descriptor.ServerAuthoritativeRequired, Is.True);
            Assert.That(descriptor.UnityConnected, Is.False);
            Assert.That(descriptor.OfficialAccountLiveClaimAllowed, Is.False);
            Assert.That(descriptor.GoogleProviderActive, Is.False);
            Assert.That(descriptor.FacebookProviderActive, Is.False);
            Assert.That(descriptor.OAuthSecretsAllowedInRepository, Is.False);
            Assert.That(descriptor.ProductionPublishAllowed, Is.False);
            Assert.That(descriptor.ServerOwnedData, Is.SupersetOf(new[]
            {
                "AccountId",
                "PlayerId",
                "AuthenticationSessions",
                "ProviderLinks",
                "WorldId",
                "GameServerId"
            }));
            Assert.That(descriptor.TemporaryLocalDemoAllowed, Does.Contain("guest demo label"));
            Assert.That(descriptor.OutOfScopeNow, Does.Contain("real Google OAuth"));
            Assert.That(descriptor.OutOfScopeNow, Does.Contain("Unity connection"));
        });
    }

    [Test]
    public void OfficialAuthFoundationListsNextEndpointsWithoutOfficialLiveClaims()
    {
        OfficialAuthFoundationDescriptor descriptor = OfficialAuthFoundation.CreateDescriptor();
        string[] paths = descriptor.Endpoints.Select(endpoint => endpoint.Path).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(paths, Is.SupersetOf(new[]
            {
                "/auth/accounts",
                "/auth/login",
                "/auth/refresh",
                "/me/profile",
                "/game-servers",
                "/me/game-server-selection",
                "/auth/link/google",
                "/auth/link/facebook",
                "/auth/guest"
            }));
            Assert.That(descriptor.Endpoints.Single(endpoint => endpoint.Path == "/auth/login").ImplementedNow, Is.True);
            Assert.That(descriptor.Endpoints.Single(endpoint => endpoint.Path == "/auth/login").OfficialLiveNow, Is.False);
            Assert.That(descriptor.Endpoints.Single(endpoint => endpoint.Path == "/auth/refresh").ImplementedNow, Is.True);
            Assert.That(descriptor.Endpoints.Single(endpoint => endpoint.Path == "/auth/refresh").NextAction, Does.Contain("one-time refresh token"));
            Assert.That(descriptor.Endpoints.Single(endpoint => endpoint.Path == "/auth/refresh").OfficialLiveNow, Is.False);
            Assert.That(descriptor.Endpoints.Where(endpoint => endpoint.Path.Contains("google", StringComparison.OrdinalIgnoreCase)).Select(endpoint => endpoint.OfficialLiveNow), Is.All.False);
            Assert.That(descriptor.Endpoints.Where(endpoint => endpoint.Path.Contains("facebook", StringComparison.OrdinalIgnoreCase)).Select(endpoint => endpoint.OfficialLiveNow), Is.All.False);
        });
    }

    [Test]
    public void OfficialAuthFoundationCoversRequiredErrorsAndSecurityRisks()
    {
        OfficialAuthFoundationDescriptor descriptor = OfficialAuthFoundation.CreateDescriptor();

        Assert.Multiple(() =>
        {
            Assert.That(OfficialAuthFoundation.RequiredErrorCodes, Is.SupersetOf(new[]
            {
                OfficialAuthErrorCode.EmailAlreadyUsed,
                OfficialAuthErrorCode.DisplayNameAlreadyUsed,
                OfficialAuthErrorCode.InvalidPassword,
                OfficialAuthErrorCode.InvalidCredentials,
                OfficialAuthErrorCode.ServerUnavailable,
                OfficialAuthErrorCode.SessionExpired,
                OfficialAuthErrorCode.GoogleProviderNotConfigured,
                OfficialAuthErrorCode.FacebookProviderNotConfigured,
                OfficialAuthErrorCode.AccountSuspended,
                OfficialAuthErrorCode.AccountBanned
            }));
            Assert.That(descriptor.SecurityRisks, Does.Contain("credential stuffing without rate limiting"));
            Assert.That(descriptor.SecurityRisks, Does.Contain("secret leakage in repository or logs"));
            Assert.That(descriptor.NextSteps, Does.Contain("Add profile endpoint behind token validation."));
        });
    }

    [Test]
    public void WorldMapChunkReadinessContractBuildsDeterministicFiveByFiveWindow()
    {
        WorldId worldId = new(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        GameServerId gameServerId = new(Guid.Parse("00000000-0000-0000-0000-000000000002"));

        WorldMapChunkWindowResponse first = WorldMapChunkReadinessContract.CreateReadinessWindow(worldId, gameServerId, centerChunkX: 10, centerChunkY: -4);
        WorldMapChunkWindowResponse second = WorldMapChunkReadinessContract.CreateReadinessWindow(worldId, gameServerId, centerChunkX: 10, centerChunkY: -4);

        Assert.Multiple(() =>
        {
            Assert.That(first.ReadOnly, Is.True);
            Assert.That(first.NonLive, Is.True);
            Assert.That(first.OfficialEndpoint, Is.False);
            Assert.That(first.MutationAllowed, Is.False);
            Assert.That(first.Chunks, Has.Count.EqualTo(25));
            Assert.That(first.Cache.ETag, Is.EqualTo(second.Cache.ETag));
            Assert.That(first.Cache.ManifestHash, Is.EqualTo(second.Cache.ManifestHash));
            Assert.That(first.Pagination.DeterministicOrdering, Is.True);
            Assert.That(first.Pagination.PageSize, Is.EqualTo(25));
            Assert.That(first.Chunks.First().ChunkX, Is.EqualTo(8));
            Assert.That(first.Chunks.First().ChunkY, Is.EqualTo(-6));
            Assert.That(first.Chunks.Last().ChunkX, Is.EqualTo(12));
            Assert.That(first.Chunks.Last().ChunkY, Is.EqualTo(-2));
        });
    }

    [Test]
    public void WorldMapChunkReadinessContractClipsAtWorldEdges()
    {
        WorldMapChunkWindowResponse response = WorldMapChunkReadinessContract.CreateReadinessWindow(
            WorldId.New(),
            GameServerId.New(),
            centerChunkX: 0,
            centerChunkY: 0,
            worldMinChunkX: 0,
            worldMaxChunkX: 2,
            worldMinChunkY: 0,
            worldMaxChunkY: 2);

        Assert.Multiple(() =>
        {
            Assert.That(response.Chunks, Has.Count.EqualTo(9));
            Assert.That(response.Chunks.Select(chunk => chunk.ChunkX), Is.All.GreaterThanOrEqualTo(0));
            Assert.That(response.Chunks.Select(chunk => chunk.ChunkY), Is.All.GreaterThanOrEqualTo(0));
            Assert.That(response.Chunks.Select(chunk => chunk.ChunkX), Is.All.LessThanOrEqualTo(2));
            Assert.That(response.Chunks.Select(chunk => chunk.ChunkY), Is.All.LessThanOrEqualTo(2));
        });
    }

    [Test]
    public void WorldMapChunkReadinessContractSeparatesOverlaysAndKeepsFlightsAirOnly()
    {
        WorldMapChunkWindowResponse response = WorldMapChunkReadinessContract.CreateReadinessWindow(WorldId.New(), GameServerId.New(), centerChunkX: 2, centerChunkY: 3);

        Assert.Multiple(() =>
        {
            Assert.That(response.Chunks.Select(chunk => chunk.ContainsPaintedOverlays), Is.All.False);
            Assert.That(response.Chunks.Select(chunk => chunk.SeamContinuityRequired), Is.All.True);
            Assert.That(response.Overlays.PaintedIntoBackground, Is.False);
            Assert.That(response.Overlays.Hives, Has.Count.EqualTo(1));
            Assert.That(response.Overlays.Resources, Has.Count.EqualTo(1));
            Assert.That(response.Overlays.Flights, Has.Count.EqualTo(1));
            Assert.That(response.Overlays.Flights.Select(flight => flight.AirOnly), Is.All.True);
            Assert.That(response.Overlays.Flights.Select(flight => flight.RoadGraphUsed), Is.All.False);
            Assert.That(response.Overlays.Flights.Select(flight => flight.Live), Is.All.False);
            Assert.That(response.Overlays.Flights.Select(flight => flight.ServerAuthoritative), Is.All.False);
        });
    }

    [Test]
    public void WorldMapChunkReadinessContractCarriesPayloadGuardrailsAndNonClaims()
    {
        WorldMapChunkWindowResponse response = WorldMapChunkReadinessContract.CreateReadinessWindow(WorldId.New(), GameServerId.New(), centerChunkX: 1, centerChunkY: 1);

        Assert.Multiple(() =>
        {
            Assert.That(response.Guardrails.EstimatedPayloadBytes, Is.LessThanOrEqualTo(response.Guardrails.PayloadBudgetBytes));
            Assert.That(response.Guardrails.MaxRadius, Is.EqualTo(2));
            Assert.That(response.Guardrails.MaxWindowChunks, Is.EqualTo(25));
            Assert.That(response.Guardrails.RequiresOverlaysSeparateFromBackground, Is.True);
            Assert.That(response.Guardrails.RequiresAirOnlyFlights, Is.True);
            Assert.That(response.Guardrails.RequiresNoRoadGraph, Is.True);
            Assert.That(response.Guardrails.ErrorCodes, Does.Contain(nameof(WorldMapChunkErrorCode.OverlayContractViolation)));
            Assert.That(response.NonClaims.OfficialEndpointLive, Is.False);
            Assert.That(response.NonClaims.OfficialPersistenceLive, Is.False);
            Assert.That(response.NonClaims.OfficialPlayerData, Is.False);
            Assert.That(response.NonClaims.OfficialProgression, Is.False);
            Assert.That(response.NonClaims.ServerAuthorityActive, Is.False);
            Assert.That(response.NonClaims.UnityConnected, Is.False);
            Assert.That(response.NonClaims.SqlBacked, Is.False);
            Assert.That(response.NonClaims.StagingOrProductionTouched, Is.False);
        });
    }

    [Test]
    public void WorldMapChunkJsonRoundTripsCanonicalRequestAndResponseScalars()
    {
        WorldId worldId = new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        GameServerId gameServerId = new(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
        WorldMapChunkRequest request = new(
            worldId,
            gameServerId,
            CenterChunkX: 10,
            CenterChunkY: -4,
            Radius: 2,
            Seed: "round-trip-seed",
            ArtisticRevision: "round-trip-art-001",
            IfNoneMatch: "passive-if-none-match",
            SinceRevision: 7,
            DeltaPageToken: "passive-delta-page-token",
            new ContractVersion(9, 1, 2));
        JsonSerializerOptions options = WorldMapChunkJson.CreateOptions(writeIndented: true);

        string requestPayload = JsonSerializer.Serialize(request, options);
        WorldMapChunkRequest roundTrippedRequest = JsonSerializer.Deserialize<WorldMapChunkRequest>(requestPayload, options)!;
        using JsonDocument requestDocument = JsonDocument.Parse(requestPayload);

        WorldMapChunkWindowResponse response = WorldMapChunkReadinessContract.CreateReadinessWindow(request);
        string responsePayload = JsonSerializer.Serialize(response, options);
        WorldMapChunkWindowResponse roundTrippedResponse = JsonSerializer.Deserialize<WorldMapChunkWindowResponse>(responsePayload, options)!;

        Assert.Multiple(() =>
        {
            Assert.That(requestDocument.RootElement.GetProperty("worldId").GetString(), Is.EqualTo("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"));
            Assert.That(requestDocument.RootElement.GetProperty("gameServerId").GetString(), Is.EqualTo("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"));
            Assert.That(requestDocument.RootElement.GetProperty("contractVersion").GetString(), Is.EqualTo("9.1.2"));
            Assert.That(roundTrippedRequest, Is.EqualTo(request));
            Assert.That(JsonSerializer.Serialize(roundTrippedResponse, options), Is.EqualTo(responsePayload));
            Assert.That(roundTrippedResponse.WorldId, Is.EqualTo(worldId));
            Assert.That(roundTrippedResponse.GameServerId, Is.EqualTo(gameServerId));
            Assert.That(roundTrippedResponse.Chunks.Select(chunk => (chunk.ChunkX, chunk.ChunkY)), Is.EqualTo(response.Chunks.Select(chunk => (chunk.ChunkX, chunk.ChunkY))));
            Assert.That(roundTrippedResponse.Cache.ManifestHash, Is.EqualTo(response.Cache.ManifestHash));
            Assert.That(roundTrippedResponse.Cache.ETag, Is.EqualTo(response.Cache.ETag));
            Assert.That(roundTrippedResponse.Overlays.OverlayRevision, Is.EqualTo("overlay-readiness-001"));
            Assert.That(roundTrippedResponse.Overlays.OverlayHash, Is.EqualTo(response.Overlays.OverlayHash));
            Assert.That(roundTrippedResponse.Overlays.OverlayHash, Has.Length.EqualTo(64));
            Assert.That(roundTrippedResponse.Pagination.SinceRevisionApplied, Is.EqualTo(7));
            Assert.That(roundTrippedResponse.PreparatoryFeatures.IfNoneMatchPassive, Is.True);
            Assert.That(roundTrippedResponse.PreparatoryFeatures.DeltaPageTokenPassive, Is.True);
            Assert.That(roundTrippedResponse.PreparatoryFeatures.ContractVersionNegotiationPassive, Is.True);
            Assert.That(roundTrippedResponse.PreparatoryFeatures.FutureErrorCodesPassive, Is.True);
        });

        string uppercaseWorldId = requestPayload.Replace(
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
            StringComparison.Ordinal);
        string nonCanonicalVersion = requestPayload.Replace("\"9.1.2\"", "\"09.1.2\"", StringComparison.Ordinal);
        string unknownMember = requestPayload[..^1] + ",\"unknownMember\":true}";

        Assert.Multiple(() =>
        {
            Assert.That(() => JsonSerializer.Deserialize<WorldMapChunkRequest>(uppercaseWorldId, options), Throws.TypeOf<JsonException>());
            Assert.That(() => JsonSerializer.Deserialize<WorldMapChunkRequest>(nonCanonicalVersion, options), Throws.TypeOf<JsonException>());
            Assert.That(() => JsonSerializer.Deserialize<WorldMapChunkRequest>(unknownMember, options), Throws.TypeOf<JsonException>());
        });
    }

    [Test]
    public void WorldMapChunkJsonExamplesMatchRuntimeGeneratedResponses()
    {
        WorldId worldId = new(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        GameServerId gameServerId = new(Guid.Parse("00000000-0000-0000-0000-000000000002"));
        JsonSerializerOptions options = WorldMapChunkJson.CreateOptions(writeIndented: true);
        WorldMapChunkWindowResponse expectedFull = WorldMapChunkReadinessContract.CreateReadinessWindow(worldId, gameServerId, 10, -4);
        WorldMapChunkWindowResponse expectedEdge = WorldMapChunkReadinessContract.CreateReadinessWindow(
            worldId,
            gameServerId,
            0,
            0,
            worldMinChunkX: 0,
            worldMaxChunkX: 2,
            worldMinChunkY: 0,
            worldMaxChunkY: 2);

        string fullPayload = File.ReadAllText(FindRepositoryFile("ops", "world-map-chunk-contract", "example-window-5x5.json"));
        string edgePayload = File.ReadAllText(FindRepositoryFile("ops", "world-map-chunk-contract", "example-edge-window.json"));
        WorldMapChunkWindowResponse full = JsonSerializer.Deserialize<WorldMapChunkWindowResponse>(fullPayload, options)!;
        WorldMapChunkWindowResponse edge = JsonSerializer.Deserialize<WorldMapChunkWindowResponse>(edgePayload, options)!;

        Assert.Multiple(() =>
        {
            Assert.That(NormalizeJson(fullPayload), Is.EqualTo(NormalizeJson(JsonSerializer.Serialize(expectedFull, options))));
            Assert.That(NormalizeJson(edgePayload), Is.EqualTo(NormalizeJson(JsonSerializer.Serialize(expectedEdge, options))));
            Assert.That(full.Chunks, Has.Count.EqualTo(25));
            Assert.That(edge.Chunks, Has.Count.EqualTo(9));
            Assert.That(full.Cache.ManifestHash, Is.EqualTo("a6e46a84bc24cb94111c09a1a3ea44aced10323575f6a2dad47497b477b55fa1"));
            Assert.That(full.Overlays.OverlayRevision, Is.EqualTo("overlay-readiness-001"));
            Assert.That(full.Overlays.OverlayHash, Is.EqualTo("3b959d7e6403e3a8d0b9e4815224419805b99e241188ed49baa5f01fddf9ae67"));
            Assert.That(full.Cache.ETag, Is.EqualTo("W/\"ac2b6a99deb6456e95fed31fd40e0417c87bd88eed336644419abc4fbef92d72\""));
            Assert.That(edge.Overlays.OverlayHash, Is.EqualTo("4b37971dfde47f8ba1130dd0dadb4eca7cd8709cc89b6c924720b546e84d80f3"));
            Assert.That(edge.Cache.ETag, Is.EqualTo("W/\"06948970e15cf1d8bdb8246318ded665b7c20b0375770b4ea442a08dfe689aa1\""));
            Assert.That(full.Guardrails.EstimatedPayloadBytes, Is.EqualTo(15_744));
        });
    }

    [Test]
    public void WorldMapChunkCacheInvalidatesDeterministicallyForSeedAndArtRevisionChanges()
    {
        WorldId worldId = new(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        GameServerId gameServerId = new(Guid.Parse("00000000-0000-0000-0000-000000000002"));
        WorldMapChunkWindowResponse baseline = WorldMapChunkReadinessContract.CreateReadinessWindow(
            worldId,
            gameServerId,
            10,
            -4,
            seed: "stable-seed",
            artisticRevision: "art-001");
        WorldMapChunkWindowResponse repeated = WorldMapChunkReadinessContract.CreateReadinessWindow(
            worldId,
            gameServerId,
            10,
            -4,
            seed: "stable-seed",
            artisticRevision: "art-001");
        WorldMapChunkWindowResponse seedChanged = WorldMapChunkReadinessContract.CreateReadinessWindow(
            worldId,
            gameServerId,
            10,
            -4,
            seed: "changed-seed",
            artisticRevision: "art-001");
        WorldMapChunkWindowResponse artChanged = WorldMapChunkReadinessContract.CreateReadinessWindow(
            worldId,
            gameServerId,
            10,
            -4,
            seed: "stable-seed",
            artisticRevision: "art-002");

        Assert.Multiple(() =>
        {
            Assert.That(repeated.Cache.ManifestHash, Is.EqualTo(baseline.Cache.ManifestHash));
            Assert.That(repeated.Cache.ETag, Is.EqualTo(baseline.Cache.ETag));
            Assert.That(repeated.Cache.InvalidationKey, Is.EqualTo(baseline.Cache.InvalidationKey));
            Assert.That(seedChanged.Cache.ManifestHash, Is.Not.EqualTo(baseline.Cache.ManifestHash));
            Assert.That(seedChanged.Cache.ETag, Is.Not.EqualTo(baseline.Cache.ETag));
            Assert.That(seedChanged.Cache.InvalidationKey, Is.EqualTo(baseline.Cache.InvalidationKey));
            Assert.That(artChanged.Cache.ManifestHash, Is.Not.EqualTo(baseline.Cache.ManifestHash));
            Assert.That(artChanged.Cache.ETag, Is.Not.EqualTo(baseline.Cache.ETag));
            Assert.That(artChanged.Cache.InvalidationKey, Is.EqualTo($"world:{worldId}:map:art-002"));
            Assert.That(artChanged.Cache.InvalidationKey, Is.Not.EqualTo(baseline.Cache.InvalidationKey));
            Assert.That(seedChanged.Pagination.DeltaToken, Is.EqualTo($"delta:{seedChanged.Cache.ETag}"));
            Assert.That(artChanged.Pagination.DeltaToken, Is.EqualTo($"delta:{artChanged.Cache.ETag}"));
        });
    }

    [Test]
    public void WorldMapChunkReadinessContractRejectsOversizedRadiusWithoutLiveClaims()
    {
        WorldMapChunkRequest request = new(
            WorldId.New(),
            GameServerId.New(),
            CenterChunkX: 0,
            CenterChunkY: 0,
            Radius: 3,
            Seed: "seed",
            ArtisticRevision: "art",
            IfNoneMatch: null,
            SinceRevision: null,
            DeltaPageToken: null,
            ContractVersion.Current);

        WorldMapChunkWindowResponse response = WorldMapChunkReadinessContract.CreateReadinessWindow(request);
        JsonSerializerOptions options = WorldMapChunkJson.CreateOptions();
        string payload = JsonSerializer.Serialize(response, options);
        WorldMapChunkWindowResponse roundTripped = JsonSerializer.Deserialize<WorldMapChunkWindowResponse>(payload, options)!;

        Assert.Multiple(() =>
        {
            Assert.That(roundTripped.Errors.Single().Code, Is.EqualTo(WorldMapChunkErrorCode.RadiusOutOfRange));
            Assert.That(roundTripped.Chunks, Is.Empty);
            Assert.That(roundTripped.ReadOnly, Is.True);
            Assert.That(roundTripped.NonLive, Is.True);
            Assert.That(roundTripped.OfficialEndpoint, Is.False);
            Assert.That(roundTripped.MutationAllowed, Is.False);
            Assert.That(roundTripped.Overlays.OverlayRevision, Is.EqualTo("overlay-empty-readiness-001"));
            Assert.That(roundTripped.Overlays.OverlayHash, Has.Length.EqualTo(64));
            Assert.That(roundTripped.PreparatoryFeatures.IfNoneMatchPassive, Is.True);
            Assert.That(roundTripped.PreparatoryFeatures.DeltaPageTokenPassive, Is.True);
            Assert.That(roundTripped.PreparatoryFeatures.ContractVersionNegotiationPassive, Is.True);
            Assert.That(roundTripped.PreparatoryFeatures.FutureErrorCodesPassive, Is.True);
        });
    }

    [Test]
    public async Task WorldMapChunkQueryServiceReturnsSuccessWithCanonicalResponse()
    {
        WorldId worldId = new(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        GameServerId gameServerId = new(Guid.Parse("00000000-0000-0000-0000-000000000002"));
        IWorldMapChunkQueryService service = CreateWorldMapChunkQueryService(worldId, gameServerId);

        WorldMapChunkQueryResult result = await service.QueryAsync(CreateWorldMapChunkRequest(worldId, gameServerId, 10, -4));

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(WorldMapChunkQueryResultState.Success));
            Assert.That(result.Response, Is.Not.Null);
            Assert.That(result.Response!.Chunks, Has.Count.EqualTo(25));
            Assert.That(result.Response.Chunks.First().ChunkX, Is.EqualTo(8));
            Assert.That(result.Response.Chunks.First().ChunkY, Is.EqualTo(-6));
            Assert.That(result.Response.Overlays.PaintedIntoBackground, Is.False);
            Assert.That(result.Response.Overlays.Live, Is.False);
            Assert.That(result.Response.Overlays.ServerAuthoritative, Is.False);
            Assert.That(result.Response.Overlays.Hives.Select(hive => hive.Live || hive.ServerAuthoritative), Is.All.False);
            Assert.That(result.Response.Overlays.Resources.Select(resource => resource.Live || resource.ServerAuthoritative), Is.All.False);
            Assert.That(result.Response.Overlays.Flights.Select(flight => flight.Live || flight.ServerAuthoritative), Is.All.False);
            Assert.That(result.Response.Overlays.Flights.Select(flight => flight.AirOnly), Is.All.True);
            Assert.That(result.Response.Overlays.Flights.Select(flight => flight.RoadGraphUsed), Is.All.False);
            Assert.That(result.Response.Overlays.OverlayRevision, Is.Not.Empty);
            Assert.That(result.Response.Overlays.OverlayHash, Has.Length.EqualTo(64));
            Assert.That(result.Response.NonClaims.ServerAuthorityActive, Is.False);
            Assert.That(result.Response.NonClaims.SqlBacked, Is.False);
            Assert.That(result.ETag, Is.EqualTo(result.Response.Cache.ETag));
            Assert.That(result.ManifestHash, Is.EqualTo(result.Response.Cache.ManifestHash));
            Assert.That(result.InvalidationKey, Is.EqualTo(result.Response.Cache.InvalidationKey));
            Assert.That(result.Errors, Is.Empty);
        });
    }

    [Test]
    public async Task WorldMapChunkQueryServiceKeepsCacheBytesHashAndEtagDeterministic()
    {
        WorldId worldId = WorldId.New();
        GameServerId gameServerId = GameServerId.New();
        IWorldMapChunkQueryService service = CreateWorldMapChunkQueryService(worldId, gameServerId);
        WorldMapChunkRequest request = CreateWorldMapChunkRequest(worldId, gameServerId, 3, 4);

        WorldMapChunkQueryResult first = await service.QueryAsync(request);
        WorldMapChunkQueryResult second = await service.QueryAsync(request);
        string firstPayload = JsonSerializer.Serialize(first.Response, WorldMapChunkJson.CreateOptions());
        string secondPayload = JsonSerializer.Serialize(second.Response, WorldMapChunkJson.CreateOptions());

        Assert.Multiple(() =>
        {
            Assert.That(first.State, Is.EqualTo(WorldMapChunkQueryResultState.Success));
            Assert.That(second.State, Is.EqualTo(WorldMapChunkQueryResultState.Success));
            Assert.That(second.Response!.Cache.ETag, Is.EqualTo(first.Response!.Cache.ETag));
            Assert.That(second.Response.Cache.ManifestHash, Is.EqualTo(first.Response.Cache.ManifestHash));
            Assert.That(second.Response.Cache.InvalidationKey, Is.EqualTo(first.Response.Cache.InvalidationKey));
            Assert.That(second.Response.Overlays.OverlayRevision, Is.EqualTo(first.Response.Overlays.OverlayRevision));
            Assert.That(second.Response.Overlays.OverlayHash, Is.EqualTo(first.Response.Overlays.OverlayHash));
            Assert.That(secondPayload, Is.EqualTo(firstPayload));
        });
    }

    [Test]
    public async Task WorldMapChunkQueryServiceReturnsNotModifiedWithoutBodyWhenIfNoneMatchHits()
    {
        WorldId worldId = WorldId.New();
        GameServerId gameServerId = GameServerId.New();
        IWorldMapChunkQueryService service = CreateWorldMapChunkQueryService(worldId, gameServerId);
        WorldMapChunkRequest request = CreateWorldMapChunkRequest(worldId, gameServerId, 1, 1);
        WorldMapChunkQueryResult first = await service.QueryAsync(request);

        WorldMapChunkQueryResult cached = await service.QueryAsync(request with { IfNoneMatch = first.Response!.Cache.ETag });

        Assert.Multiple(() =>
        {
            Assert.That(cached.State, Is.EqualTo(WorldMapChunkQueryResultState.NotModified));
            Assert.That(cached.Response, Is.Null);
            Assert.That(cached.ETag, Is.EqualTo(first.Response.Cache.ETag));
            Assert.That(cached.ManifestHash, Is.EqualTo(first.Response.Cache.ManifestHash));
            Assert.That(cached.InvalidationKey, Is.EqualTo(first.Response.Cache.InvalidationKey));
            Assert.That(cached.Errors, Is.Empty);
        });
    }

    [Test]
    public async Task WorldMapChunkQueryServiceChangesManifestHashEtagAndInvalidationWhenSeedOrRevisionChanges()
    {
        WorldId worldId = WorldId.New();
        GameServerId gameServerId = GameServerId.New();
        IWorldMapChunkQueryService baselineService = CreateWorldMapChunkQueryService(worldId, gameServerId, seed: "seed-a", artisticRevision: "art-a");
        IWorldMapChunkQueryService seedService = CreateWorldMapChunkQueryService(worldId, gameServerId, seed: "seed-b", artisticRevision: "art-a");
        IWorldMapChunkQueryService revisionService = CreateWorldMapChunkQueryService(worldId, gameServerId, seed: "seed-a", artisticRevision: "art-b");

        WorldMapChunkQueryResult baseline = await baselineService.QueryAsync(CreateWorldMapChunkRequest(worldId, gameServerId, 0, 0, seed: "seed-a", artisticRevision: "art-a"));
        WorldMapChunkQueryResult changedSeed = await seedService.QueryAsync(CreateWorldMapChunkRequest(worldId, gameServerId, 0, 0, seed: "seed-b", artisticRevision: "art-a"));
        WorldMapChunkQueryResult changedRevision = await revisionService.QueryAsync(CreateWorldMapChunkRequest(worldId, gameServerId, 0, 0, seed: "seed-a", artisticRevision: "art-b"));

        Assert.Multiple(() =>
        {
            Assert.That(changedSeed.Response!.Cache.ManifestHash, Is.Not.EqualTo(baseline.Response!.Cache.ManifestHash));
            Assert.That(changedSeed.Response.Cache.ETag, Is.Not.EqualTo(baseline.Response.Cache.ETag));
            Assert.That(changedSeed.Response.Cache.InvalidationKey, Is.EqualTo(baseline.Response.Cache.InvalidationKey));
            Assert.That(changedSeed.Response.Overlays.OverlayRevision, Is.EqualTo(baseline.Response.Overlays.OverlayRevision));
            Assert.That(changedSeed.Response.Overlays.OverlayHash, Is.EqualTo(baseline.Response.Overlays.OverlayHash));
            Assert.That(changedRevision.Response!.Cache.ManifestHash, Is.Not.EqualTo(baseline.Response.Cache.ManifestHash));
            Assert.That(changedRevision.Response.Cache.ETag, Is.Not.EqualTo(baseline.Response.Cache.ETag));
            Assert.That(changedRevision.Response.Cache.InvalidationKey, Is.Not.EqualTo(baseline.Response.Cache.InvalidationKey));
            Assert.That(changedRevision.Response.Overlays.OverlayRevision, Is.EqualTo(baseline.Response.Overlays.OverlayRevision));
            Assert.That(changedRevision.Response.Overlays.OverlayHash, Is.EqualTo(baseline.Response.Overlays.OverlayHash));
        });
    }

    [Test]
    public async Task WorldMapChunkQueryServiceInvalidatesCombinedEtagForDynamicOverlayChanges()
    {
        WorldId worldId = WorldId.New();
        GameServerId gameServerId = GameServerId.New();
        WorldMapChunkRequest request = CreateWorldMapChunkRequest(worldId, gameServerId, 0, 0);
        WorldMapChunkOverlayEnvelope baselineOverlays = CreateCanonicalWorldMapOverlays(worldId, gameServerId);
        WorldHiveOverlay hive = baselineOverlays.Hives.Single();
        WorldResourceOverlay resource = baselineOverlays.Resources.Single();
        WorldFlightOverlay flight = baselineOverlays.Flights.Single();
        WorldMapChunkOverlayEnvelope[] changedOverlays =
        [
            baselineOverlays with
            {
                OverlayRevision = "overlay-readiness-002",
                Flights = [flight with { Destination = flight.Destination with { X = flight.Destination.X + 1 } }]
            },
            baselineOverlays with
            {
                OverlayRevision = "overlay-readiness-003",
                Resources = [resource with { ResourceNodeId = "resource-respawned-001" }]
            },
            baselineOverlays with
            {
                OverlayRevision = "overlay-readiness-004",
                Hives = [hive with { PowerBand = "evolved_band" }]
            }
        ];
        IWorldMapChunkQueryService baselineService = CreateWorldMapChunkQueryService(
            worldId,
            gameServerId,
            overlayProvider: new FixedWorldMapChunkOverlayProvider(baselineOverlays));
        WorldMapChunkQueryResult baseline = await baselineService.QueryAsync(request);
        WorldMapChunkQueryResult[] changed = await Task.WhenAll(changedOverlays.Select(overlays =>
            CreateWorldMapChunkQueryService(
                    worldId,
                    gameServerId,
                    overlayProvider: new FixedWorldMapChunkOverlayProvider(overlays))
                .QueryAsync(request)
                .AsTask()));

        Assert.Multiple(() =>
        {
            Assert.That(changed.Select(result => result.State), Is.All.EqualTo(WorldMapChunkQueryResultState.Success));
            Assert.That(changed.Select(result => result.Response!.Cache.ManifestHash), Is.All.EqualTo(baseline.Response!.Cache.ManifestHash));
            Assert.That(changed.Select(result => result.Response!.Overlays.OverlayRevision), Is.EqualTo(new[]
            {
                "overlay-readiness-002",
                "overlay-readiness-003",
                "overlay-readiness-004"
            }));
            Assert.That(changed.Select(result => result.Response!.Overlays.OverlayHash), Is.All.Not.EqualTo(baseline.Response.Overlays.OverlayHash));
            Assert.That(changed.Select(result => result.Response!.Overlays.OverlayHash).Distinct().Count(), Is.EqualTo(3));
            Assert.That(changed.Select(result => result.Response!.Cache.ETag), Is.All.Not.EqualTo(baseline.Response.Cache.ETag));
            Assert.That(changed.Select(result => result.Response!.Cache.ETag).Distinct().Count(), Is.EqualTo(3));
        });
    }

    [Test]
    public async Task WorldMapChunkQueryServiceReturnsSuccessForStaleCombinedEtagThenNotModifiedForExactEtag()
    {
        WorldId worldId = WorldId.New();
        GameServerId gameServerId = GameServerId.New();
        WorldMapChunkOverlayEnvelope baseline = CreateCanonicalWorldMapOverlays(worldId, gameServerId);
        MutableWorldMapChunkOverlayProvider overlayProvider = new(baseline);
        IWorldMapChunkQueryService service = CreateWorldMapChunkQueryService(worldId, gameServerId, overlayProvider: overlayProvider);
        WorldMapChunkRequest request = CreateWorldMapChunkRequest(worldId, gameServerId, 0, 0);
        WorldMapChunkQueryResult first = await service.QueryAsync(request);
        WorldFlightOverlay flight = baseline.Flights.Single();
        overlayProvider.Overlays = baseline with
        {
            OverlayRevision = "overlay-readiness-002",
            Flights = [flight with { Destination = flight.Destination with { Y = flight.Destination.Y + 1 } }]
        };

        WorldMapChunkQueryResult stale = await service.QueryAsync(request with { IfNoneMatch = first.Response!.Cache.ETag });
        WorldMapChunkQueryResult exact = await service.QueryAsync(request with { IfNoneMatch = stale.Response!.Cache.ETag });

        Assert.Multiple(() =>
        {
            Assert.That(stale.State, Is.EqualTo(WorldMapChunkQueryResultState.Success));
            Assert.That(stale.Response, Is.Not.Null);
            Assert.That(stale.Response!.Cache.ManifestHash, Is.EqualTo(first.Response.Cache.ManifestHash));
            Assert.That(stale.Response.Overlays.OverlayRevision, Is.EqualTo("overlay-readiness-002"));
            Assert.That(stale.Response.Overlays.OverlayHash, Is.Not.EqualTo(first.Response.Overlays.OverlayHash));
            Assert.That(stale.Response.Cache.ETag, Is.Not.EqualTo(first.Response.Cache.ETag));
            Assert.That(exact.State, Is.EqualTo(WorldMapChunkQueryResultState.NotModified));
            Assert.That(exact.Response, Is.Null);
            Assert.That(exact.ETag, Is.EqualTo(stale.Response.Cache.ETag));
            Assert.That(exact.ManifestHash, Is.EqualTo(stale.Response.Cache.ManifestHash));
            Assert.That(exact.InvalidationKey, Is.EqualTo(stale.Response.Cache.InvalidationKey));
            Assert.That(exact.Errors, Is.Empty);
        });
    }

    [Test]
    public async Task WorldMapChunkQueryServiceUsesOverlayHashToSurviveRevisionCollision()
    {
        WorldId worldId = WorldId.New();
        GameServerId gameServerId = GameServerId.New();
        WorldMapChunkOverlayEnvelope providerSnapshot = CreateCanonicalWorldMapOverlays(worldId, gameServerId) with
        {
            OverlayRevision = "overlay-collision-001",
            OverlayHash = "provider-stale-hash"
        };
        MutableWorldMapChunkOverlayProvider overlayProvider = new(providerSnapshot);
        IWorldMapChunkQueryService service = CreateWorldMapChunkQueryService(worldId, gameServerId, overlayProvider: overlayProvider);
        WorldMapChunkRequest request = CreateWorldMapChunkRequest(worldId, gameServerId, 0, 0);
        WorldMapChunkQueryResult first = await service.QueryAsync(request);
        WorldHiveOverlay hive = providerSnapshot.Hives.Single();
        overlayProvider.Overlays = providerSnapshot with
        {
            Hives = [hive with { PowerBand = "collision_mutation_band" }]
        };

        WorldMapChunkQueryResult collision = await service.QueryAsync(request with { IfNoneMatch = first.Response!.Cache.ETag });

        Assert.Multiple(() =>
        {
            Assert.That(collision.State, Is.EqualTo(WorldMapChunkQueryResultState.Success));
            Assert.That(collision.Response, Is.Not.Null);
            Assert.That(collision.Response!.Cache.ManifestHash, Is.EqualTo(first.Response.Cache.ManifestHash));
            Assert.That(collision.Response.Overlays.OverlayRevision, Is.EqualTo(first.Response.Overlays.OverlayRevision));
            Assert.That(collision.Response.Overlays.OverlayHash, Is.Not.EqualTo(first.Response.Overlays.OverlayHash));
            Assert.That(collision.Response.Overlays.OverlayHash, Is.Not.EqualTo("provider-stale-hash"));
            Assert.That(collision.Response.Cache.ETag, Is.Not.EqualTo(first.Response.Cache.ETag));
        });
    }

    [Test]
    public async Task WorldMapChunkQueryServiceCanonicalizesOverlaySerializationOrder()
    {
        WorldId worldId = WorldId.New();
        GameServerId gameServerId = GameServerId.New();
        WorldMapChunkOverlayEnvelope baseline = CreateCanonicalWorldMapOverlays(worldId, gameServerId);
        WorldHiveOverlay hive1 = baseline.Hives.Single();
        WorldHiveOverlay hive2 = hive1 with { HiveMarkerId = "hive-readiness-002" };
        WorldResourceOverlay resource1 = baseline.Resources.Single();
        WorldResourceOverlay resource2 = resource1 with { ResourceNodeId = "resource-readiness-002" };
        WorldFlightOverlay flight1 = baseline.Flights.Single();
        WorldFlightOverlay flight2 = flight1 with { FlightId = "flight-readiness-002" };
        WorldMapChunkOverlayEnvelope ascending = baseline with
        {
            Hives = [hive1, hive2],
            Resources = [resource1, resource2],
            Flights = [flight1, flight2],
            OverlayRevision = "overlay-order-001",
            OverlayHash = "ignored-provider-hash-a"
        };
        WorldMapChunkOverlayEnvelope descending = ascending with
        {
            Hives = [hive2, hive1],
            Resources = [resource2, resource1],
            Flights = [flight2, flight1],
            OverlayHash = "ignored-provider-hash-b"
        };
        WorldMapChunkRequest request = CreateWorldMapChunkRequest(worldId, gameServerId, 0, 0);
        WorldMapChunkQueryResult first = await CreateWorldMapChunkQueryService(
                worldId,
                gameServerId,
                overlayProvider: new FixedWorldMapChunkOverlayProvider(ascending))
            .QueryAsync(request);
        WorldMapChunkQueryResult second = await CreateWorldMapChunkQueryService(
                worldId,
                gameServerId,
                overlayProvider: new FixedWorldMapChunkOverlayProvider(descending))
            .QueryAsync(request);
        string firstPayload = JsonSerializer.Serialize(first.Response, WorldMapChunkJson.CreateOptions());
        string secondPayload = JsonSerializer.Serialize(second.Response, WorldMapChunkJson.CreateOptions());

        Assert.Multiple(() =>
        {
            Assert.That(first.State, Is.EqualTo(WorldMapChunkQueryResultState.Success));
            Assert.That(second.State, Is.EqualTo(WorldMapChunkQueryResultState.Success));
            Assert.That(second.Response!.Overlays.Hives.Select(hive => hive.HiveMarkerId), Is.EqualTo(new[] { "hive-readiness-001", "hive-readiness-002" }));
            Assert.That(second.Response.Overlays.Resources.Select(resource => resource.ResourceNodeId), Is.EqualTo(new[] { "resource-readiness-001", "resource-readiness-002" }));
            Assert.That(second.Response.Overlays.Flights.Select(flight => flight.FlightId), Is.EqualTo(new[] { "flight-readiness-001", "flight-readiness-002" }));
            Assert.That(second.Response.Overlays.OverlayRevision, Is.EqualTo(first.Response!.Overlays.OverlayRevision));
            Assert.That(second.Response.Overlays.OverlayHash, Is.EqualTo(first.Response.Overlays.OverlayHash));
            Assert.That(second.Response.Cache.ETag, Is.EqualTo(first.Response.Cache.ETag));
            Assert.That(secondPayload, Is.EqualTo(firstPayload));
        });
    }

    [Test]
    public async Task WorldMapChunkQueryServiceRejectsEmptyOverlayRevision()
    {
        WorldId worldId = WorldId.New();
        GameServerId gameServerId = GameServerId.New();
        WorldMapChunkOverlayEnvelope overlays = CreateCanonicalWorldMapOverlays(worldId, gameServerId) with
        {
            OverlayRevision = " ",
            OverlayHash = "provider-stale-hash"
        };
        IWorldMapChunkQueryService service = CreateWorldMapChunkQueryService(
            worldId,
            gameServerId,
            overlayProvider: new FixedWorldMapChunkOverlayProvider(overlays));

        WorldMapChunkQueryResult result = await service.QueryAsync(CreateWorldMapChunkRequest(worldId, gameServerId, 0, 0));

        AssertRejectedResult(result, WorldMapChunkErrorCode.OverlayContractViolation);
    }

    [Test]
    public async Task WorldMapChunkQueryServiceRejectsWorldServerMismatchAndBadRevision()
    {
        WorldId worldId = WorldId.New();
        GameServerId gameServerId = GameServerId.New();
        IWorldMapChunkQueryService service = CreateWorldMapChunkQueryService(worldId, gameServerId);

        WorldMapChunkQueryResult wrongWorld = await service.QueryAsync(CreateWorldMapChunkRequest(WorldId.New(), gameServerId, 0, 0));
        WorldMapChunkQueryResult wrongServer = await service.QueryAsync(CreateWorldMapChunkRequest(worldId, GameServerId.New(), 0, 0));
        WorldMapChunkQueryResult wrongRevision = await service.QueryAsync(CreateWorldMapChunkRequest(worldId, gameServerId, 0, 0, artisticRevision: "other-art"));

        AssertRejectedResult(wrongWorld, WorldMapChunkErrorCode.UnknownWorld);
        AssertRejectedResult(wrongServer, WorldMapChunkErrorCode.UnknownWorld);
        AssertRejectedResult(wrongRevision, WorldMapChunkErrorCode.ManifestRevisionMismatch);
    }

    [Test]
    public async Task WorldMapChunkQueryServicePreservesEdgeClippingAndPayloadGuardrails()
    {
        WorldId worldId = WorldId.New();
        GameServerId gameServerId = GameServerId.New();
        IWorldMapChunkQueryService service = CreateWorldMapChunkQueryService(worldId, gameServerId, minChunkX: 0, maxChunkX: 2, minChunkY: 0, maxChunkY: 2);

        WorldMapChunkQueryResult result = await service.QueryAsync(CreateWorldMapChunkRequest(worldId, gameServerId, 0, 0));

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(WorldMapChunkQueryResultState.Success));
            Assert.That(result.Response!.Chunks, Has.Count.EqualTo(9));
            Assert.That(result.Response.Chunks.Select(chunk => chunk.ChunkX), Is.All.InRange(0, 2));
            Assert.That(result.Response.Chunks.Select(chunk => chunk.ChunkY), Is.All.InRange(0, 2));
            Assert.That(result.Response.Guardrails.EstimatedPayloadBytes, Is.LessThanOrEqualTo(result.Response.Guardrails.PayloadBudgetBytes));
            Assert.That(result.Response.Pagination.PageSize, Is.EqualTo(9));
        });
    }

    [Test]
    public async Task WorldMapChunkQueryServiceRejectsOversizedFinalOverlayPayload()
    {
        WorldId worldId = WorldId.New();
        GameServerId gameServerId = GameServerId.New();
        WorldMapChunkOverlayEnvelope baseline = CreateCanonicalWorldMapOverlays(worldId, gameServerId);
        WorldResourceOverlay resource = baseline.Resources.Single();
        WorldMapChunkOverlayEnvelope oversized = baseline with
        {
            Resources = Enumerable.Range(0, 1000)
                .Select(index => resource with
                {
                    ResourceNodeId = $"resource-readiness-{index:D4}",
                    Position = new WorldCoordinate(index, index)
                })
                .ToArray()
        };
        WorldMapChunkWindowResponse canonical = WorldMapChunkReadinessContract.CreateReadinessWindow(worldId, gameServerId, 0, 0);
        WorldMapChunkWindowResponse finalized = WorldMapChunkReadinessContract.FinalizeReadinessOverlays(canonical, oversized);
        WorldMapChunkWindowResponse repeatedFinalization = WorldMapChunkReadinessContract.FinalizeReadinessOverlays(finalized, oversized);
        IWorldMapChunkQueryService service = CreateWorldMapChunkQueryService(
            worldId,
            gameServerId,
            overlayProvider: new FixedWorldMapChunkOverlayProvider(oversized));

        WorldMapChunkQueryResult result = await service.QueryAsync(CreateWorldMapChunkRequest(worldId, gameServerId, 0, 0));

        Assert.Multiple(() =>
        {
            Assert.That(finalized.Guardrails.EstimatedPayloadBytes, Is.EqualTo(271_488));
            Assert.That(finalized.Guardrails.EstimatedPayloadBytes, Is.GreaterThan(finalized.Guardrails.PayloadBudgetBytes));
            Assert.That(finalized.Errors.Single().Code, Is.EqualTo(WorldMapChunkErrorCode.PayloadBudgetExceeded));
            Assert.That(repeatedFinalization.Guardrails.EstimatedPayloadBytes, Is.EqualTo(271_488));
            Assert.That(repeatedFinalization.Errors, Has.Count.EqualTo(1));
        });
        AssertRejectedResult(result, WorldMapChunkErrorCode.PayloadBudgetExceeded);
    }

    [Test]
    public async Task WorldMapChunkQueryServiceRecalculatesSuccessfulProviderOverlayPayload()
    {
        WorldId worldId = WorldId.New();
        GameServerId gameServerId = GameServerId.New();
        WorldMapChunkOverlayEnvelope baseline = CreateCanonicalWorldMapOverlays(worldId, gameServerId);
        WorldHiveOverlay hive = baseline.Hives.Single();
        WorldResourceOverlay resource = baseline.Resources.Single();
        WorldFlightOverlay flight = baseline.Flights.Single();
        WorldMapChunkOverlayEnvelope overlays = baseline with
        {
            Hives = [hive, hive with { HiveMarkerId = "hive-readiness-002" }],
            Resources =
            [
                resource,
                resource with { ResourceNodeId = "resource-readiness-002" },
                resource with { ResourceNodeId = "resource-readiness-003" }
            ],
            Flights = [flight, flight with { FlightId = "flight-readiness-002" }]
        };
        IWorldMapChunkQueryService service = CreateWorldMapChunkQueryService(
            worldId,
            gameServerId,
            overlayProvider: new FixedWorldMapChunkOverlayProvider(overlays));

        WorldMapChunkQueryResult result = await service.QueryAsync(CreateWorldMapChunkRequest(worldId, gameServerId, 0, 0));

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(WorldMapChunkQueryResultState.Success));
            Assert.That(result.Response, Is.Not.Null);
            Assert.That(result.Response!.Overlays.Hives, Has.Count.EqualTo(2));
            Assert.That(result.Response.Overlays.Resources, Has.Count.EqualTo(3));
            Assert.That(result.Response.Overlays.Flights, Has.Count.EqualTo(2));
            Assert.That(result.Response.Guardrails.EstimatedPayloadBytes, Is.EqualTo(16_896));
            Assert.That(result.Response.Guardrails.EstimatedPayloadBytes, Is.LessThan(result.Response.Guardrails.PayloadBudgetBytes));
            Assert.That(result.ETag, Is.EqualTo(result.Response.Cache.ETag));
            Assert.That(result.ManifestHash, Is.EqualTo(result.Response.Cache.ManifestHash));
            Assert.That(result.InvalidationKey, Is.EqualTo(result.Response.Cache.InvalidationKey));
            Assert.That(result.Errors, Is.Empty);
        });
    }

    [Test]
    public async Task WorldMapChunkQueryServiceRejectsLiveOrAuthoritativeOverlayEnvelope()
    {
        WorldId worldId = WorldId.New();
        GameServerId gameServerId = GameServerId.New();
        WorldMapChunkOverlayEnvelope baseline = CreateCanonicalWorldMapOverlays(worldId, gameServerId);
        WorldMapChunkOverlayEnvelope[] invalidOverlays =
        [
            baseline with { Live = true },
            baseline with { ServerAuthoritative = true }
        ];

        foreach (WorldMapChunkOverlayEnvelope overlays in invalidOverlays)
        {
            IWorldMapChunkQueryService service = CreateWorldMapChunkQueryService(
                worldId,
                gameServerId,
                overlayProvider: new FixedWorldMapChunkOverlayProvider(overlays));
            WorldMapChunkQueryResult result = await service.QueryAsync(CreateWorldMapChunkRequest(worldId, gameServerId, 0, 0));

            AssertRejectedResult(result, WorldMapChunkErrorCode.OverlayContractViolation);
        }
    }

    [Test]
    public async Task WorldMapChunkQueryServiceRejectsLiveOrAuthoritativeOverlayEntities()
    {
        WorldId worldId = WorldId.New();
        GameServerId gameServerId = GameServerId.New();
        WorldMapChunkOverlayEnvelope baseline = CreateCanonicalWorldMapOverlays(worldId, gameServerId);
        WorldHiveOverlay hive = baseline.Hives.Single();
        WorldResourceOverlay resource = baseline.Resources.Single();
        WorldFlightOverlay flight = baseline.Flights.Single();
        WorldMapChunkOverlayEnvelope[] invalidOverlays =
        [
            baseline with { Hives = [hive with { Live = true }] },
            baseline with { Hives = [hive with { ServerAuthoritative = true }] },
            baseline with { Resources = [resource with { Live = true }] },
            baseline with { Resources = [resource with { ServerAuthoritative = true }] },
            baseline with { Flights = [flight with { Live = true }] },
            baseline with { Flights = [flight with { ServerAuthoritative = true }] }
        ];

        foreach (WorldMapChunkOverlayEnvelope overlays in invalidOverlays)
        {
            IWorldMapChunkQueryService service = CreateWorldMapChunkQueryService(
                worldId,
                gameServerId,
                overlayProvider: new FixedWorldMapChunkOverlayProvider(overlays));
            WorldMapChunkQueryResult result = await service.QueryAsync(CreateWorldMapChunkRequest(worldId, gameServerId, 0, 0));

            AssertRejectedResult(result, WorldMapChunkErrorCode.OverlayContractViolation);
        }
    }

    [Test]
    public async Task WorldMapChunkQueryServiceRejectsPaintedOverlayProviderPayload()
    {
        WorldId worldId = WorldId.New();
        GameServerId gameServerId = GameServerId.New();
        WorldMapChunkOverlayEnvelope overlays = CreateCanonicalWorldMapOverlays(worldId, gameServerId) with
        {
            PaintedIntoBackground = true
        };
        IWorldMapChunkQueryService service = CreateWorldMapChunkQueryService(
            worldId,
            gameServerId,
            overlayProvider: new FixedWorldMapChunkOverlayProvider(overlays));

        WorldMapChunkQueryResult result = await service.QueryAsync(CreateWorldMapChunkRequest(worldId, gameServerId, 0, 0));

        AssertRejectedResult(result, WorldMapChunkErrorCode.OverlayContractViolation);
    }

    [Test]
    public async Task WorldMapChunkQueryServiceRejectsNonAirborneFlightProviderPayload()
    {
        WorldId worldId = WorldId.New();
        GameServerId gameServerId = GameServerId.New();
        WorldMapChunkOverlayEnvelope baseline = CreateCanonicalWorldMapOverlays(worldId, gameServerId);
        WorldMapChunkOverlayEnvelope overlays = baseline with
        {
            Flights = [baseline.Flights.Single() with { AirOnly = false }]
        };
        IWorldMapChunkQueryService service = CreateWorldMapChunkQueryService(
            worldId,
            gameServerId,
            overlayProvider: new FixedWorldMapChunkOverlayProvider(overlays));

        WorldMapChunkQueryResult result = await service.QueryAsync(CreateWorldMapChunkRequest(worldId, gameServerId, 0, 0));

        AssertRejectedResult(result, WorldMapChunkErrorCode.OverlayContractViolation);
    }

    [Test]
    public async Task WorldMapChunkQueryServiceRejectsRoadGraphFlightProviderPayload()
    {
        WorldId worldId = WorldId.New();
        GameServerId gameServerId = GameServerId.New();
        WorldMapChunkOverlayEnvelope baseline = CreateCanonicalWorldMapOverlays(worldId, gameServerId);
        WorldMapChunkOverlayEnvelope overlays = baseline with
        {
            Flights = [baseline.Flights.Single() with { RoadGraphUsed = true }]
        };
        IWorldMapChunkQueryService service = CreateWorldMapChunkQueryService(
            worldId,
            gameServerId,
            overlayProvider: new FixedWorldMapChunkOverlayProvider(overlays));

        WorldMapChunkQueryResult result = await service.QueryAsync(CreateWorldMapChunkRequest(worldId, gameServerId, 0, 0));

        AssertRejectedResult(result, WorldMapChunkErrorCode.OverlayContractViolation);
    }

    [Test]
    public void WorldMapChunkQueryServiceHonorsCancellation()
    {
        WorldId worldId = WorldId.New();
        GameServerId gameServerId = GameServerId.New();
        IWorldMapChunkQueryService service = CreateWorldMapChunkQueryService(worldId, gameServerId);
        using CancellationTokenSource cts = new();
        cts.Cancel();

        Assert.That(
            async () => await service.QueryAsync(CreateWorldMapChunkRequest(worldId, gameServerId, 0, 0), cts.Token),
            Throws.TypeOf<OperationCanceledException>());
    }

    [Test]
    public async Task WorldMapChunkQueryServiceSupportsConcurrentReadsWithoutCrossWorldLeakage()
    {
        WorldId worldA = WorldId.New();
        WorldId worldB = WorldId.New();
        GameServerId serverA = GameServerId.New();
        GameServerId serverB = GameServerId.New();
        SharedWorldMapChunkIdentityProvider identityProvider = new(
        [
            CreateWorldMapChunkWorldState(worldA, serverA, seed: "seed-a", artisticRevision: "art-a"),
            CreateWorldMapChunkWorldState(worldB, serverB, seed: "seed-b", artisticRevision: "art-b")
        ]);
        IWorldMapChunkOverlayProvider overlayProvider = new DeterministicLocalWorldMapChunkOverlayProvider();
        IWorldMapChunkQueryService service = new WorldMapChunkQueryService(identityProvider, overlayProvider);

        Task<WorldMapChunkQueryResult>[] tasks =
        [
            service.QueryAsync(CreateWorldMapChunkRequest(worldA, serverA, 0, 0, seed: "seed-a", artisticRevision: "art-a")).AsTask(),
            service.QueryAsync(CreateWorldMapChunkRequest(worldA, serverA, 1, 1, seed: "seed-a", artisticRevision: "art-a")).AsTask(),
            service.QueryAsync(CreateWorldMapChunkRequest(worldB, serverB, 0, 0, seed: "seed-b", artisticRevision: "art-b")).AsTask(),
            service.QueryAsync(CreateWorldMapChunkRequest(worldB, serverB, 1, 1, seed: "seed-b", artisticRevision: "art-b")).AsTask()
        ];

        WorldMapChunkQueryResult[] results = await Task.WhenAll(tasks);
        WorldMapChunkQueryResult crossedScope = await service.QueryAsync(
            CreateWorldMapChunkRequest(worldA, serverB, 0, 0, seed: "seed-a", artisticRevision: "art-a"));

        Assert.Multiple(() =>
        {
            Assert.That(results.Select(result => result.State), Is.All.EqualTo(WorldMapChunkQueryResultState.Success));
            Assert.That(results.Take(2).Select(result => result.Response!.WorldId), Is.All.EqualTo(worldA));
            Assert.That(results.Skip(2).Select(result => result.Response!.WorldId), Is.All.EqualTo(worldB));
            Assert.That(results.Take(2).Select(result => result.Response!.GameServerId), Is.All.EqualTo(serverA));
            Assert.That(results.Skip(2).Select(result => result.Response!.GameServerId), Is.All.EqualTo(serverB));
            Assert.That(results[0].Response!.Cache.InvalidationKey, Does.Contain(worldA.ToString()));
            Assert.That(results[2].Response!.Cache.InvalidationKey, Does.Contain(worldB.ToString()));
            Assert.That(results[0].Response!.Cache.ManifestHash, Is.Not.EqualTo(results[2].Response!.Cache.ManifestHash));
            Assert.That(results[0].Response!.Cache.ETag, Is.Not.EqualTo(results[2].Response!.Cache.ETag));
            Assert.That(results.Select(result => result.Response!.Overlays.OverlayRevision), Is.All.Not.Empty);
            Assert.That(results.Select(result => result.Response!.Overlays.OverlayHash.Length), Is.All.EqualTo(64));
        });
        AssertRejectedResult(crossedScope, WorldMapChunkErrorCode.UnknownWorld);
    }

    [Test]
    public async Task LocalWorldMapOverlaySnapshotProviderPublishesStrictlyMonotonicRevisionsPerScope()
    {
        WorldMapOverlayScope scopeA = new(WorldId.New(), GameServerId.New());
        WorldMapOverlayScope scopeB = new(WorldId.New(), GameServerId.New());
        LocalWorldMapOverlaySnapshotProvider provider = new([scopeA, scopeB]);
        WorldMapOverlaySnapshotContent baselineA = CreateWorldMapOverlaySnapshotContent(scopeA);
        WorldMapOverlaySnapshotContent baselineB = CreateWorldMapOverlaySnapshotContent(scopeB);

        WorldMapOverlayPublicationResult firstA = await provider.PublishAsync(
            new WorldMapOverlayPublishRequest(scopeA, baselineA, ExpectedRevision: 0));
        WorldMapOverlayPublicationResult firstB = await provider.PublishAsync(
            new WorldMapOverlayPublishRequest(scopeB, baselineB, ExpectedRevision: 0));
        WorldMapOverlayPublicationResult secondA = await provider.PublishAsync(new WorldMapOverlayPublishRequest(
            scopeA,
            baselineA with { Hives = [baselineA.Hives.Single() with { PowerBand = "scope-a-revision-2" }] },
            firstA.Snapshot!.Revision,
            firstA.Snapshot.OverlayHash));
        WorldMapOverlayPublicationResult secondB = await provider.PublishAsync(new WorldMapOverlayPublishRequest(
            scopeB,
            baselineB with { Resources = [baselineB.Resources.Single() with { RichnessBand = "scope-b-revision-2" }] },
            firstB.Snapshot!.Revision,
            firstB.Snapshot.OverlayHash));

        Assert.Multiple(() =>
        {
            Assert.That(firstA.State, Is.EqualTo(WorldMapOverlayPublicationState.Published));
            Assert.That(firstB.State, Is.EqualTo(WorldMapOverlayPublicationState.Published));
            Assert.That(secondA.State, Is.EqualTo(WorldMapOverlayPublicationState.Published));
            Assert.That(secondB.State, Is.EqualTo(WorldMapOverlayPublicationState.Published));
            Assert.That(firstA.Snapshot!.Revision, Is.EqualTo(1));
            Assert.That(firstB.Snapshot!.Revision, Is.EqualTo(1));
            Assert.That(secondA.Snapshot!.Revision, Is.EqualTo(2));
            Assert.That(secondB.Snapshot!.Revision, Is.EqualTo(2));
            Assert.That(firstA.Snapshot.OverlayRevision, Is.EqualTo("overlay-snapshot-00000000000000000001"));
            Assert.That(secondA.Snapshot.OverlayRevision, Is.EqualTo("overlay-snapshot-00000000000000000002"));
            Assert.That(firstA.Snapshot.OverlayHash, Has.Length.EqualTo(64));
            Assert.That(secondA.Snapshot.OverlayHash, Is.Not.EqualTo(firstA.Snapshot.OverlayHash));
            Assert.That(secondB.Snapshot.OverlayHash, Is.Not.EqualTo(firstB.Snapshot.OverlayHash));
        });
    }

    [Test]
    public async Task LocalWorldMapOverlaySnapshotProviderReturnsNoChangeForCanonicalSemanticMatch()
    {
        WorldMapOverlayScope scope = new(WorldId.New(), GameServerId.New());
        LocalWorldMapOverlaySnapshotProvider provider = new([scope]);
        WorldMapOverlaySnapshotContent baseline = CreateWorldMapOverlaySnapshotContent(scope);
        WorldMapOverlaySnapshotContent expanded = baseline with
        {
            Hives = [baseline.Hives.Single(), baseline.Hives.Single() with { HiveMarkerId = "hive-readiness-002" }],
            Resources = [baseline.Resources.Single(), baseline.Resources.Single() with { ResourceNodeId = "resource-readiness-002" }],
            Flights = [baseline.Flights.Single(), baseline.Flights.Single() with { FlightId = "flight-readiness-002" }]
        };
        WorldMapOverlayPublicationResult published = await provider.PublishAsync(
            new WorldMapOverlayPublishRequest(scope, expanded));
        IWorldMapChunkQueryService service = CreateWorldMapChunkQueryService(
            scope.WorldId,
            scope.GameServerId,
            overlayProvider: provider);
        WorldMapChunkRequest query = CreateWorldMapChunkRequest(scope.WorldId, scope.GameServerId, 0, 0);
        WorldMapChunkQueryResult before = await service.QueryAsync(query);
        byte[] beforeBytes = JsonSerializer.SerializeToUtf8Bytes(before.Response, WorldMapChunkJson.CreateOptions());
        WorldMapOverlaySnapshotContent reordered = expanded with
        {
            Hives = expanded.Hives.Reverse().ToArray(),
            Resources = expanded.Resources.Reverse().ToArray(),
            Flights = expanded.Flights.Reverse().ToArray()
        };

        WorldMapOverlayPublicationResult noChange = await provider.PublishAsync(
            new WorldMapOverlayPublishRequest(scope, reordered));
        WorldMapChunkQueryResult after = await service.QueryAsync(query);
        byte[] afterBytes = JsonSerializer.SerializeToUtf8Bytes(after.Response, WorldMapChunkJson.CreateOptions());

        Assert.Multiple(() =>
        {
            Assert.That(published.State, Is.EqualTo(WorldMapOverlayPublicationState.Published));
            Assert.That(noChange.State, Is.EqualTo(WorldMapOverlayPublicationState.NoChange));
            Assert.That(noChange.Snapshot!.Revision, Is.EqualTo(published.Snapshot!.Revision));
            Assert.That(noChange.Snapshot.OverlayRevision, Is.EqualTo(published.Snapshot.OverlayRevision));
            Assert.That(noChange.Snapshot.OverlayHash, Is.EqualTo(published.Snapshot.OverlayHash));
            Assert.That(after.Response!.Cache.ETag, Is.EqualTo(before.Response!.Cache.ETag));
            Assert.That(after.Response.Overlays.OverlayHash, Is.EqualTo(before.Response.Overlays.OverlayHash));
            Assert.That(afterBytes, Is.EqualTo(beforeBytes));
        });
    }

    [Test]
    public async Task LocalWorldMapOverlaySnapshotProviderInvalidatesCombinedEtagForDynamicPublications()
    {
        WorldMapOverlayScope scope = new(WorldId.New(), GameServerId.New());
        LocalWorldMapOverlaySnapshotProvider provider = new([scope], new WorldMapOverlaySnapshotOptions(4));
        WorldMapOverlaySnapshotContent baseline = CreateWorldMapOverlaySnapshotContent(scope);
        IWorldMapChunkQueryService service = CreateWorldMapChunkQueryService(
            scope.WorldId,
            scope.GameServerId,
            overlayProvider: provider);
        WorldMapChunkRequest query = CreateWorldMapChunkRequest(scope.WorldId, scope.GameServerId, 0, 0);

        WorldMapOverlayPublicationResult first = await provider.PublishAsync(new WorldMapOverlayPublishRequest(scope, baseline));
        WorldMapChunkQueryResult firstQuery = await service.QueryAsync(query);
        WorldFlightOverlay flight = baseline.Flights.Single();
        WorldMapOverlaySnapshotContent movedFlight = baseline with
        {
            Flights = [flight with { Destination = flight.Destination with { X = flight.Destination.X + 1 } }]
        };
        WorldMapOverlayPublicationResult second = await provider.PublishAsync(new WorldMapOverlayPublishRequest(
            scope,
            movedFlight,
            first.Snapshot!.Revision,
            first.Snapshot.OverlayHash));
        WorldMapChunkQueryResult secondQuery = await service.QueryAsync(query);
        WorldMapOverlaySnapshotContent respawnedResource = movedFlight with
        {
            Resources = [baseline.Resources.Single() with { ResourceNodeId = "resource-respawned-067" }]
        };
        WorldMapOverlayPublicationResult third = await provider.PublishAsync(new WorldMapOverlayPublishRequest(
            scope,
            respawnedResource,
            second.Snapshot!.Revision,
            second.Snapshot.OverlayHash));
        WorldMapChunkQueryResult thirdQuery = await service.QueryAsync(query);
        WorldMapOverlaySnapshotContent evolvedHive = respawnedResource with
        {
            Hives = [baseline.Hives.Single() with { PowerBand = "evolved-wave-067" }]
        };
        WorldMapOverlayPublicationResult fourth = await provider.PublishAsync(new WorldMapOverlayPublishRequest(
            scope,
            evolvedHive,
            third.Snapshot!.Revision,
            third.Snapshot.OverlayHash));
        WorldMapChunkQueryResult fourthQuery = await service.QueryAsync(query);
        WorldMapChunkQueryResult staleValidator = await service.QueryAsync(
            query with { IfNoneMatch = thirdQuery.Response!.Cache.ETag });
        WorldMapChunkQueryResult exactValidator = await service.QueryAsync(
            query with { IfNoneMatch = fourthQuery.Response!.Cache.ETag });

        WorldMapChunkQueryResult[] queries = [firstQuery, secondQuery, thirdQuery, fourthQuery];
        Assert.Multiple(() =>
        {
            Assert.That(new[] { first.Snapshot!.Revision, second.Snapshot!.Revision, third.Snapshot!.Revision, fourth.Snapshot!.Revision },
                Is.EqualTo(new long[] { 1, 2, 3, 4 }));
            Assert.That(queries.Select(result => result.Response!.Cache.ManifestHash).Distinct().Count(), Is.EqualTo(1));
            Assert.That(queries.Select(result => result.Response!.Overlays.OverlayHash).Distinct().Count(), Is.EqualTo(4));
            Assert.That(queries.Select(result => result.Response!.Cache.ETag).Distinct().Count(), Is.EqualTo(4));
            Assert.That(staleValidator.State, Is.EqualTo(WorldMapChunkQueryResultState.Success));
            Assert.That(exactValidator.State, Is.EqualTo(WorldMapChunkQueryResultState.NotModified));
            Assert.That(exactValidator.Response, Is.Null);
            Assert.That(exactValidator.ETag, Is.EqualTo(fourthQuery.Response.Cache.ETag));
        });
    }

    [Test]
    public async Task LocalWorldMapOverlaySnapshotProviderRejectsConcurrentStaleCompareAndSwapWriter()
    {
        WorldMapOverlayScope scope = new(WorldId.New(), GameServerId.New());
        LocalWorldMapOverlaySnapshotProvider provider = new([scope]);
        WorldMapOverlayPublicationResult first = await provider.PublishAsync(
            new WorldMapOverlayPublishRequest(scope, CreateTaggedWorldMapOverlaySnapshotContent(scope, "base")));
        TaskCompletionSource start = new(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<WorldMapOverlayPublicationResult> PublishAfterStartAsync(string tag)
        {
            await start.Task;
            return await provider.PublishAsync(new WorldMapOverlayPublishRequest(
                scope,
                CreateTaggedWorldMapOverlaySnapshotContent(scope, tag),
                first.Snapshot!.Revision,
                first.Snapshot.OverlayHash));
        }

        Task<WorldMapOverlayPublicationResult> writerA = PublishAfterStartAsync("writer-a");
        Task<WorldMapOverlayPublicationResult> writerB = PublishAfterStartAsync("writer-b");
        start.SetResult();
        WorldMapOverlayPublicationResult[] results = await Task.WhenAll(writerA, writerB);
        WorldMapOverlaySnapshotReadResult latest = await provider.ReadLatestAsync(scope);
        WorldMapOverlayPublicationResult staleRevision = await provider.PublishAsync(new WorldMapOverlayPublishRequest(
            scope,
            CreateTaggedWorldMapOverlaySnapshotContent(scope, "stale-revision"),
            ExpectedRevision: 1));
        WorldMapOverlayPublicationResult staleHash = await provider.PublishAsync(new WorldMapOverlayPublishRequest(
            scope,
            CreateTaggedWorldMapOverlaySnapshotContent(scope, "stale-hash"),
            ExpectedOverlayHash: first.Snapshot!.OverlayHash));

        Assert.Multiple(() =>
        {
            Assert.That(results.Count(result => result.State == WorldMapOverlayPublicationState.Published), Is.EqualTo(1));
            Assert.That(results.Count(result => result.State == WorldMapOverlayPublicationState.RejectedConflict), Is.EqualTo(1));
            Assert.That(results.Single(result => result.State == WorldMapOverlayPublicationState.Published).Snapshot!.Revision, Is.EqualTo(2));
            Assert.That(results.Single(result => result.State == WorldMapOverlayPublicationState.RejectedConflict).Snapshot!.Revision, Is.EqualTo(2));
            Assert.That(latest.State, Is.EqualTo(WorldMapOverlaySnapshotReadState.Found));
            Assert.That(latest.Snapshot!.Revision, Is.EqualTo(2));
            Assert.That(staleRevision.State, Is.EqualTo(WorldMapOverlayPublicationState.RejectedConflict));
            Assert.That(staleHash.State, Is.EqualTo(WorldMapOverlayPublicationState.RejectedConflict));
            Assert.That(staleRevision.Snapshot!.Revision, Is.EqualTo(2));
            Assert.That(staleHash.Snapshot!.Revision, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task LocalWorldMapOverlaySnapshotProviderKeepsCrossedScopesIsolated()
    {
        WorldMapOverlayScope scopeA = new(WorldId.New(), GameServerId.New());
        WorldMapOverlayScope scopeB = new(WorldId.New(), GameServerId.New());
        WorldMapOverlayScope crossed = new(scopeA.WorldId, scopeB.GameServerId);
        LocalWorldMapOverlaySnapshotProvider provider = new([scopeA, scopeB]);

        WorldMapOverlaySnapshotReadResult emptyKnown = await provider.ReadLatestAsync(scopeA);
        WorldMapOverlaySnapshotReadResult missing = await provider.ReadLatestAsync(crossed);
        WorldMapOverlayPublicationResult rejected = await provider.PublishAsync(
            new WorldMapOverlayPublishRequest(crossed, CreateWorldMapOverlaySnapshotContent(scopeA)));
        WorldMapOverlayPublicationResult publishedA = await provider.PublishAsync(
            new WorldMapOverlayPublishRequest(scopeA, CreateTaggedWorldMapOverlaySnapshotContent(scopeA, "scope-a")));
        WorldMapOverlayPublicationResult publishedB = await provider.PublishAsync(
            new WorldMapOverlayPublishRequest(scopeB, CreateTaggedWorldMapOverlaySnapshotContent(scopeB, "scope-b")));

        Assert.Multiple(() =>
        {
            Assert.That(emptyKnown.State, Is.EqualTo(WorldMapOverlaySnapshotReadState.SnapshotNotFound));
            Assert.That(missing.State, Is.EqualTo(WorldMapOverlaySnapshotReadState.ScopeNotFound));
            Assert.That(rejected.State, Is.EqualTo(WorldMapOverlayPublicationState.ScopeNotFound));
            Assert.That(publishedA.Snapshot!.Scope, Is.EqualTo(scopeA));
            Assert.That(publishedB.Snapshot!.Scope, Is.EqualTo(scopeB));
            Assert.That(publishedA.Snapshot.Revision, Is.EqualTo(1));
            Assert.That(publishedB.Snapshot.Revision, Is.EqualTo(1));
            Assert.That(publishedA.Snapshot.Overlays.Hives.Single().PowerBand, Is.EqualTo("scope-a"));
            Assert.That(publishedB.Snapshot.Overlays.Hives.Single().PowerBand, Is.EqualTo("scope-b"));
        });
    }

    [Test]
    public async Task LocalWorldMapOverlaySnapshotProviderPublishesAtomicConcurrentSnapshotsAcrossTwoScopes()
    {
        WorldMapOverlayScope scopeA = new(WorldId.New(), GameServerId.New());
        WorldMapOverlayScope scopeB = new(WorldId.New(), GameServerId.New());
        LocalWorldMapOverlaySnapshotProvider provider = new([scopeA, scopeB], new WorldMapOverlaySnapshotOptions(32));
        await provider.PublishAsync(new WorldMapOverlayPublishRequest(
            scopeA,
            CreateTaggedWorldMapOverlaySnapshotContent(scopeA, "scope-a-base")));
        await provider.PublishAsync(new WorldMapOverlayPublishRequest(
            scopeB,
            CreateTaggedWorldMapOverlaySnapshotContent(scopeB, "scope-b-base")));
        TaskCompletionSource start = new(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<WorldMapOverlayPublicationResult> PublishAfterStartAsync(WorldMapOverlayScope scope, string tag)
        {
            await start.Task;
            return await provider.PublishAsync(new WorldMapOverlayPublishRequest(
                scope,
                CreateTaggedWorldMapOverlaySnapshotContent(scope, tag)));
        }

        async Task ReadAfterStartAsync(WorldMapOverlayScope scope)
        {
            await start.Task;
            for (int index = 0; index < 200; index++)
            {
                WorldMapOverlaySnapshotReadResult read = await provider.ReadLatestAsync(scope);
                AssertCoherentTaggedSnapshot(read.Snapshot!);
                await Task.Yield();
            }
        }

        Task<WorldMapOverlayPublicationResult>[] writersA = Enumerable.Range(1, 12)
            .Select(index => PublishAfterStartAsync(scopeA, $"scope-a-{index:D2}"))
            .ToArray();
        Task<WorldMapOverlayPublicationResult>[] writersB = Enumerable.Range(1, 5)
            .Select(index => PublishAfterStartAsync(scopeB, $"scope-b-{index:D2}"))
            .ToArray();
        Task[] readers =
        [
            ReadAfterStartAsync(scopeA),
            ReadAfterStartAsync(scopeA),
            ReadAfterStartAsync(scopeB),
            ReadAfterStartAsync(scopeB)
        ];

        start.SetResult();
        WorldMapOverlayPublicationResult[] publishedA = await Task.WhenAll(writersA);
        WorldMapOverlayPublicationResult[] publishedB = await Task.WhenAll(writersB);
        await Task.WhenAll(readers);
        WorldMapOverlaySnapshotReadResult latestA = await provider.ReadLatestAsync(scopeA);
        WorldMapOverlaySnapshotReadResult latestB = await provider.ReadLatestAsync(scopeB);

        Assert.Multiple(() =>
        {
            Assert.That(publishedA.Select(result => result.State), Is.All.EqualTo(WorldMapOverlayPublicationState.Published));
            Assert.That(publishedB.Select(result => result.State), Is.All.EqualTo(WorldMapOverlayPublicationState.Published));
            Assert.That(publishedA.Select(result => result.Snapshot!.Revision).Order(), Is.EqualTo(Enumerable.Range(2, 12).Select(value => (long)value)));
            Assert.That(publishedB.Select(result => result.Snapshot!.Revision).Order(), Is.EqualTo(Enumerable.Range(2, 5).Select(value => (long)value)));
            Assert.That(latestA.Snapshot!.Revision, Is.EqualTo(13));
            Assert.That(latestB.Snapshot!.Revision, Is.EqualTo(6));
        });
        AssertCoherentTaggedSnapshot(latestA.Snapshot!);
        AssertCoherentTaggedSnapshot(latestB.Snapshot!);
    }

    [Test]
    public async Task LocalWorldMapOverlaySnapshotProviderPurgesBoundedHistoryDeterministically()
    {
        WorldMapOverlayScope scope = new(WorldId.New(), GameServerId.New());
        LocalWorldMapOverlaySnapshotProvider provider = new([scope], new WorldMapOverlaySnapshotOptions(2));
        Assert.That(
            () => new LocalWorldMapOverlaySnapshotProvider([scope], new WorldMapOverlaySnapshotOptions(1)),
            Throws.TypeOf<ArgumentOutOfRangeException>());

        for (int revision = 1; revision <= 4; revision++)
        {
            WorldMapOverlayPublicationResult result = await provider.PublishAsync(new WorldMapOverlayPublishRequest(
                scope,
                CreateTaggedWorldMapOverlaySnapshotContent(scope, $"history-{revision}")));
            Assert.That(result.Snapshot!.Revision, Is.EqualTo(revision));
        }

        WorldMapOverlaySnapshotHistoryResult history = await provider.ReadHistoryAsync(scope);

        Assert.Multiple(() =>
        {
            Assert.That(history.State, Is.EqualTo(WorldMapOverlaySnapshotReadState.Found));
            Assert.That(history.Snapshots.Select(snapshot => snapshot.Revision), Is.EqualTo(new long[] { 3, 4 }));
            Assert.That(history.Snapshots.Select(snapshot => snapshot.Overlays.Hives.Single().PowerBand),
                Is.EqualTo(new[] { "history-3", "history-4" }));
        });
    }

    [Test]
    public async Task LocalWorldMapOverlaySnapshotProviderHonorsCancellationAndExceptionBeforeCommit()
    {
        WorldMapOverlayScope scope = new(WorldId.New(), GameServerId.New());
        LocalWorldMapOverlaySnapshotProvider provider = new([scope]);
        WorldMapOverlaySnapshotContent baseline = CreateWorldMapOverlaySnapshotContent(scope);
        WorldMapOverlayPublicationResult first = await provider.PublishAsync(
            new WorldMapOverlayPublishRequest(scope, baseline));
        using CancellationTokenSource cancelled = new();
        cancelled.Cancel();

        Assert.That(
            async () => await provider.PublishAsync(
                new WorldMapOverlayPublishRequest(scope, CreateTaggedWorldMapOverlaySnapshotContent(scope, "cancelled")),
                cancelled.Token),
            Throws.TypeOf<OperationCanceledException>());
        WorldMapOverlaySnapshotReadResult afterCancellation = await provider.ReadLatestAsync(scope);

        WorldMapOverlaySnapshotContent throwingContent = new(
            new ThrowingReadOnlyList<WorldHiveOverlay>(),
            baseline.Resources,
            baseline.Flights,
            baseline.PaintedIntoBackground,
            baseline.ServerAuthoritative,
            baseline.Live);
        Assert.That(
            async () => await provider.PublishAsync(new WorldMapOverlayPublishRequest(scope, throwingContent)),
            Throws.TypeOf<InvalidOperationException>());
        WorldMapOverlaySnapshotReadResult afterException = await provider.ReadLatestAsync(scope);

        Assert.Multiple(() =>
        {
            Assert.That(first.Snapshot!.Revision, Is.EqualTo(1));
            Assert.That(afterCancellation.Snapshot!.Revision, Is.EqualTo(1));
            Assert.That(afterCancellation.Snapshot.OverlayHash, Is.EqualTo(first.Snapshot.OverlayHash));
            Assert.That(afterException.Snapshot!.Revision, Is.EqualTo(1));
            Assert.That(afterException.Snapshot.OverlayHash, Is.EqualTo(first.Snapshot.OverlayHash));
        });
    }

    [Test]
    public async Task LocalWorldMapOverlaySnapshotProviderRejectsInvalidGuardrailsBudgetAndDuplicateIds()
    {
        WorldMapOverlayScope scope = new(WorldId.New(), GameServerId.New());
        LocalWorldMapOverlaySnapshotProvider provider = new([scope]);
        WorldMapOverlaySnapshotContent baseline = CreateWorldMapOverlaySnapshotContent(scope);
        WorldHiveOverlay hive = baseline.Hives.Single();
        WorldResourceOverlay resource = baseline.Resources.Single();
        WorldFlightOverlay flight = baseline.Flights.Single();
        IReadOnlyList<WorldResourceOverlay> oversizedResources = Enumerable.Range(1, 1000)
            .Select(index => resource with { ResourceNodeId = $"resource-{index:D4}" })
            .ToArray();
        (WorldMapOverlaySnapshotContent Content, WorldMapOverlaySnapshotContractErrorCode Code)[] invalid =
        [
            (baseline with { PaintedIntoBackground = true }, WorldMapOverlaySnapshotContractErrorCode.OverlayContractViolation),
            (baseline with { ServerAuthoritative = true }, WorldMapOverlaySnapshotContractErrorCode.OverlayContractViolation),
            (baseline with { Live = true }, WorldMapOverlaySnapshotContractErrorCode.OverlayContractViolation),
            (baseline with { Hives = [hive with { ServerAuthoritative = true }] }, WorldMapOverlaySnapshotContractErrorCode.OverlayContractViolation),
            (baseline with { Hives = [hive with { Live = true }] }, WorldMapOverlaySnapshotContractErrorCode.OverlayContractViolation),
            (baseline with { Resources = [resource with { ServerAuthoritative = true }] }, WorldMapOverlaySnapshotContractErrorCode.OverlayContractViolation),
            (baseline with { Resources = [resource with { Live = true }] }, WorldMapOverlaySnapshotContractErrorCode.OverlayContractViolation),
            (baseline with { Flights = [flight with { ServerAuthoritative = true }] }, WorldMapOverlaySnapshotContractErrorCode.OverlayContractViolation),
            (baseline with { Flights = [flight with { Live = true }] }, WorldMapOverlaySnapshotContractErrorCode.OverlayContractViolation),
            (baseline with { Flights = [flight with { AirOnly = false }] }, WorldMapOverlaySnapshotContractErrorCode.OverlayContractViolation),
            (baseline with { Flights = [flight with { RoadGraphUsed = true }] }, WorldMapOverlaySnapshotContractErrorCode.OverlayContractViolation),
            (baseline with { Resources = oversizedResources }, WorldMapOverlaySnapshotContractErrorCode.PayloadBudgetExceeded),
            (baseline with { Hives = [hive, hive] }, WorldMapOverlaySnapshotContractErrorCode.DuplicateHiveMarkerId),
            (baseline with { Resources = [resource, resource] }, WorldMapOverlaySnapshotContractErrorCode.DuplicateResourceNodeId),
            (baseline with { Flights = [flight, flight] }, WorldMapOverlaySnapshotContractErrorCode.DuplicateFlightId)
        ];

        foreach ((WorldMapOverlaySnapshotContent content, WorldMapOverlaySnapshotContractErrorCode code) in invalid)
        {
            WorldMapOverlayPublicationResult result = await provider.PublishAsync(
                new WorldMapOverlayPublishRequest(scope, content));
            Assert.Multiple(() =>
            {
                Assert.That(result.State, Is.EqualTo(WorldMapOverlayPublicationState.RejectedContract));
                Assert.That(result.Snapshot, Is.Null);
                Assert.That(result.Errors.Select(error => error.Code), Does.Contain(code));
            });
        }

        WorldMapOverlaySnapshotReadResult latest = await provider.ReadLatestAsync(scope);
        Assert.That(latest.State, Is.EqualTo(WorldMapOverlaySnapshotReadState.SnapshotNotFound));
    }

    [Test]
    public async Task LocalWorldMapOverlaySnapshotProviderDetachesPublishedCollections()
    {
        WorldMapOverlayScope scope = new(WorldId.New(), GameServerId.New());
        LocalWorldMapOverlaySnapshotProvider provider = new([scope]);
        WorldMapOverlaySnapshotContent baseline = CreateWorldMapOverlaySnapshotContent(scope);
        WorldHiveOverlay originalHive = baseline.Hives.Single();
        WorldHiveOverlay[] hives = [originalHive];
        WorldResourceOverlay[] resources = [baseline.Resources.Single()];
        WorldFlightOverlay[] flights = [baseline.Flights.Single()];
        WorldMapOverlaySnapshotContent callerOwned = new(hives, resources, flights, false, false, false);

        WorldMapOverlayPublicationResult published = await provider.PublishAsync(
            new WorldMapOverlayPublishRequest(scope, callerOwned));
        hives[0] = originalHive with { PowerBand = "caller-mutated-after-publish" };
        WorldMapOverlaySnapshotReadResult read = await provider.ReadLatestAsync(scope);
        IList<WorldHiveOverlay> exposed = (IList<WorldHiveOverlay>)read.Snapshot!.Overlays.Hives;

        Assert.Multiple(() =>
        {
            Assert.That(published.State, Is.EqualTo(WorldMapOverlayPublicationState.Published));
            Assert.That(read.Snapshot.Overlays.Hives.Single().PowerBand, Is.EqualTo(originalHive.PowerBand));
            Assert.That(exposed.IsReadOnly, Is.True);
            Assert.That(() => exposed[0] = originalHive with { PowerBand = "forbidden" }, Throws.TypeOf<NotSupportedException>());
        });
    }

    private static string FindRepositoryFile(params string[] relativeSegments)
    {
        DirectoryInfo? current = new(TestContext.CurrentContext.TestDirectory);
        while (current is not null)
        {
            string candidate = Path.Combine([current.FullName, .. relativeSegments]);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException($"Could not find repository file: {Path.Combine(relativeSegments)}");
    }

    private static string NormalizeJson(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd();
    }

    private static HiveBuildingUpgradeRequestCommand CreateUpgradeCommand()
    {
        return new HiveBuildingUpgradeRequestCommand(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            PlayerId.New(),
            WorldId.New(),
            GameServerId.New(),
            BuildingId.New(),
            "honey_storage",
            FromLevel: 0,
            ToLevel: 1,
            ExpectedResourceRevision: 1,
            ExpectedBuildingRevision: 1,
            IdempotencyKey: "upgrade-readiness-key",
            HiveLoopCodeFirstCatalogs.ReadinessCatalogVersion,
            NonLive: true,
            ReadinessOnly: true,
            OfficialProgressionRequested: false,
            ContractVersion.Current);
    }

    private static HiveTroopTrainingRequestCommand CreateTrainingCommand(int quantity)
    {
        return new HiveTroopTrainingRequestCommand(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            PlayerId.New(),
            WorldId.New(),
            GameServerId.New(),
            "worker_bee",
            quantity,
            ExpectedResourceRevision: 1,
            ExpectedArmyRevision: 1,
            IdempotencyKey: "training-readiness-key",
            HiveLoopCodeFirstCatalogs.ReadinessCatalogVersion,
            NonLive: true,
            ReadinessOnly: true,
            OfficialProgressionRequested: false,
            ContractVersion.Current);
    }

    private static IWorldMapChunkQueryService CreateWorldMapChunkQueryService(
        WorldId worldId,
        GameServerId gameServerId,
        string seed = "bee-kingdom-world-map-readiness-seed",
        string artisticRevision = "art-revision-readiness-001",
        int minChunkX = -1024,
        int maxChunkX = 1024,
        int minChunkY = -1024,
        int maxChunkY = 1024,
        IWorldMapChunkOverlayProvider? overlayProvider = null)
    {
        WorldMapChunkWorldState state = CreateWorldMapChunkWorldState(
            worldId,
            gameServerId,
            seed,
            artisticRevision,
            minChunkX,
            maxChunkX,
            minChunkY,
            maxChunkY);

        return new WorldMapChunkQueryService(
            new DeterministicLocalWorldMapChunkIdentityProvider(state),
            overlayProvider ?? new DeterministicLocalWorldMapChunkOverlayProvider());
    }

    private static WorldMapChunkWorldState CreateWorldMapChunkWorldState(
        WorldId worldId,
        GameServerId gameServerId,
        string seed = "bee-kingdom-world-map-readiness-seed",
        string artisticRevision = "art-revision-readiness-001",
        int minChunkX = -1024,
        int maxChunkX = 1024,
        int minChunkY = -1024,
        int maxChunkY = 1024)
    {
        return new WorldMapChunkWorldState(
            worldId,
            gameServerId,
            minChunkX,
            maxChunkX,
            minChunkY,
            maxChunkY,
            seed,
            artisticRevision,
            ReadOnly: true,
            NonLive: true,
            ContractVersion.Current);
    }

    private static WorldMapChunkOverlayEnvelope CreateCanonicalWorldMapOverlays(WorldId worldId, GameServerId gameServerId)
    {
        return WorldMapChunkReadinessContract.CreateReadinessWindow(worldId, gameServerId, 0, 0).Overlays;
    }

    private static WorldMapOverlaySnapshotContent CreateWorldMapOverlaySnapshotContent(WorldMapOverlayScope scope)
    {
        return WorldMapOverlaySnapshotContent.FromEnvelope(
            CreateCanonicalWorldMapOverlays(scope.WorldId, scope.GameServerId));
    }

    private static WorldMapOverlaySnapshotContent CreateTaggedWorldMapOverlaySnapshotContent(
        WorldMapOverlayScope scope,
        string tag)
    {
        WorldMapOverlaySnapshotContent baseline = CreateWorldMapOverlaySnapshotContent(scope);
        return baseline with
        {
            Hives = [baseline.Hives.Single() with { PowerBand = tag }],
            Resources = [baseline.Resources.Single() with { RichnessBand = tag }],
            Flights = [baseline.Flights.Single() with { FlightId = $"flight-{tag}" }]
        };
    }

    private static void AssertCoherentTaggedSnapshot(WorldMapOverlaySnapshot snapshot)
    {
        string hiveTag = snapshot.Overlays.Hives.Single().PowerBand;
        string resourceTag = snapshot.Overlays.Resources.Single().RichnessBand;
        string flightId = snapshot.Overlays.Flights.Single().FlightId;
        if (!string.Equals(resourceTag, hiveTag, StringComparison.Ordinal)
            || !string.Equals(flightId, $"flight-{hiveTag}", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A reader observed a partially mixed overlay snapshot.");
        }
    }

    private static void AssertRejectedResult(WorldMapChunkQueryResult result, WorldMapChunkErrorCode errorCode)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(WorldMapChunkQueryResultState.Rejected));
            Assert.That(result.Response, Is.Null);
            Assert.That(result.ETag, Is.Null);
            Assert.That(result.ManifestHash, Is.Null);
            Assert.That(result.InvalidationKey, Is.Null);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors.Single().Code, Is.EqualTo(errorCode));
        });
    }

    private static WorldMapChunkRequest CreateWorldMapChunkRequest(
        WorldId worldId,
        GameServerId gameServerId,
        int centerChunkX,
        int centerChunkY,
        int radius = 2,
        string seed = "bee-kingdom-world-map-readiness-seed",
        string artisticRevision = "art-revision-readiness-001")
    {
        return new WorldMapChunkRequest(
            worldId,
            gameServerId,
            centerChunkX,
            centerChunkY,
            radius,
            seed,
            artisticRevision,
            IfNoneMatch: null,
            SinceRevision: null,
            DeltaPageToken: null,
            ContractVersion.Current);
    }

    private sealed class FixedWorldMapChunkOverlayProvider(WorldMapChunkOverlayEnvelope overlays) : IWorldMapChunkOverlayProvider
    {
        public ValueTask<WorldMapChunkOverlayEnvelope> GetOverlaysAsync(
            WorldMapChunkOverlayQuery query,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(overlays);
        }
    }

    private sealed class MutableWorldMapChunkOverlayProvider(WorldMapChunkOverlayEnvelope overlays) : IWorldMapChunkOverlayProvider
    {
        public WorldMapChunkOverlayEnvelope Overlays { get; set; } = overlays;

        public ValueTask<WorldMapChunkOverlayEnvelope> GetOverlaysAsync(
            WorldMapChunkOverlayQuery query,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(Overlays);
        }
    }

    private sealed class SharedWorldMapChunkIdentityProvider(IEnumerable<WorldMapChunkWorldState> states) : IWorldMapChunkIdentityProvider
    {
        private readonly IReadOnlyDictionary<(WorldId WorldId, GameServerId GameServerId), WorldMapChunkWorldState> statesByScope =
            states.ToDictionary(state => (state.WorldId, state.GameServerId));

        public ValueTask<WorldMapChunkWorldState?> GetWorldStateAsync(
            WorldId worldId,
            GameServerId gameServerId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            statesByScope.TryGetValue((worldId, gameServerId), out WorldMapChunkWorldState? state);
            return ValueTask.FromResult(state);
        }
    }

    private sealed class ThrowingReadOnlyList<T> : IReadOnlyList<T>
    {
        public T this[int index] => throw new InvalidOperationException("Synthetic pre-commit enumeration failure.");

        public int Count => 1;

        public IEnumerator<T> GetEnumerator() => throw new InvalidOperationException("Synthetic pre-commit enumeration failure.");

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
