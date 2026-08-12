namespace BeeKingdom.Shared.Catalogs;

public sealed record HiveLoopCatalogSet(
    string CatalogVersion,
    bool ReadOnly,
    bool NonLive,
    IReadOnlyList<HiveResourceCatalogEntry> Resources,
    IReadOnlyList<HiveBuildingCatalogEntry> Buildings,
    IReadOnlyList<HiveBuildingLevelCatalogEntry> BuildingLevels,
    IReadOnlyList<HiveBuildingUpgradeCatalogEntry> BuildingUpgrades,
    IReadOnlyList<HiveTroopCatalogEntry> Troops,
    IReadOnlyList<HiveTroopTrainingCatalogEntry> TroopTraining,
    IReadOnlyList<HiveArmyCapacityCatalogEntry> ArmyCapacity,
    HiveIdempotencyPolicy IdempotencyPolicy,
    HiveAntiDoubleSpendPolicy AntiDoubleSpendPolicy)
{
    public HiveLoopCatalogValidationResult Validate()
    {
        List<string> errors = [];

        if (!ReadOnly)
        {
            errors.Add("Hive loop code-first catalog must remain read-only.");
        }

        if (!NonLive)
        {
            errors.Add("Hive loop code-first catalog must remain non-live.");
        }

        ValidateUnique(Resources.Select(resource => resource.ResourceKey), "resource", errors);
        ValidateUnique(Buildings.Select(building => building.BuildingKey), "building", errors);
        ValidateUnique(Troops.Select(troop => troop.TroopKey), "troop", errors);

        foreach (HiveResourceCatalogEntry resource in Resources)
        {
            if (resource.InitialAmount < resource.MinAmount)
            {
                errors.Add($"Resource '{resource.ResourceKey}' initial amount must be greater than or equal to min amount.");
            }

            if (resource.MinAmount < 0 || resource.BaseCapacity < 0 || resource.MaxTransactionalDelta <= 0)
            {
                errors.Add($"Resource '{resource.ResourceKey}' has invalid non-live bounds.");
            }
        }

        foreach (HiveBuildingCatalogEntry building in Buildings)
        {
            if (building.MaxLevel <= 0)
            {
                errors.Add($"Building '{building.BuildingKey}' max level must be positive.");
            }
        }

        foreach (HiveBuildingLevelCatalogEntry level in BuildingLevels)
        {
            if (!Buildings.Any(building => building.BuildingKey == level.BuildingKey))
            {
                errors.Add($"Building level references unknown building '{level.BuildingKey}'.");
            }

            if (level.Level < 0)
            {
                errors.Add($"Building level for '{level.BuildingKey}' must be non-negative.");
            }
        }

        foreach (HiveBuildingUpgradeCatalogEntry upgrade in BuildingUpgrades)
        {
            if (!Buildings.Any(building => building.BuildingKey == upgrade.BuildingKey))
            {
                errors.Add($"Upgrade references unknown building '{upgrade.BuildingKey}'.");
            }

            if (upgrade.ToLevel != upgrade.FromLevel + 1)
            {
                errors.Add($"Upgrade '{upgrade.BuildingKey}' must advance exactly one level.");
            }

            if (upgrade.DurationSeconds <= 0)
            {
                errors.Add($"Upgrade '{upgrade.BuildingKey}' duration must be positive.");
            }

            ValidateCosts(upgrade.ResourceCosts, $"upgrade '{upgrade.BuildingKey}'", errors);
        }

        foreach (HiveTroopCatalogEntry troop in Troops)
        {
            if (troop.BaseCapacityCost <= 0)
            {
                errors.Add($"Troop '{troop.TroopKey}' capacity cost must be positive.");
            }
        }

        foreach (HiveTroopTrainingCatalogEntry training in TroopTraining)
        {
            if (!Troops.Any(troop => troop.TroopKey == training.TroopKey))
            {
                errors.Add($"Training references unknown troop '{training.TroopKey}'.");
            }

            if (training.QuantityStep <= 0 || training.BatchSizeMin <= 0 || training.BatchSizeMax < training.BatchSizeMin)
            {
                errors.Add($"Training '{training.TroopKey}' batch bounds are invalid.");
            }

            if (training.DurationSecondsPerUnit <= 0)
            {
                errors.Add($"Training '{training.TroopKey}' duration per unit must be positive.");
            }

            ValidateCosts(training.ResourceCosts, $"training '{training.TroopKey}'", errors);
        }

        foreach (HiveArmyCapacityCatalogEntry capacity in ArmyCapacity)
        {
            if (capacity.CapacityBonus < 0)
            {
                errors.Add($"Army capacity source '{capacity.SourceKey}' must not be negative.");
            }
        }

        if (!IdempotencyPolicy.Required || !AntiDoubleSpendPolicy.RequiresAtomicResourceDebitAndQueueCreate)
        {
            errors.Add("Hive loop catalog must keep idempotency and anti-double-spend protections enabled.");
        }

        return new HiveLoopCatalogValidationResult(errors.Count == 0, errors);
    }

    public long CalculateUpgradeCost(string buildingKey, int fromLevel, string resourceKey)
    {
        HiveBuildingUpgradeCatalogEntry upgrade = BuildingUpgrades.Single(item => item.BuildingKey == buildingKey && item.FromLevel == fromLevel);
        return upgrade.ResourceCosts.SingleOrDefault(cost => cost.ResourceKey == resourceKey)?.Amount ?? 0;
    }

    public long CalculateTrainingCost(string troopKey, int quantity, string resourceKey)
    {
        HiveTroopTrainingCatalogEntry training = TroopTraining.Single(item => item.TroopKey == troopKey);

        if (quantity < training.BatchSizeMin || quantity > training.BatchSizeMax || quantity % training.QuantityStep != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Training quantity is outside non-live catalog bounds.");
        }

        long unitCost = training.ResourceCosts.SingleOrDefault(cost => cost.ResourceKey == resourceKey)?.Amount ?? 0;
        return unitCost * quantity;
    }

    private static void ValidateUnique(IEnumerable<string> keys, string kind, List<string> errors)
    {
        string[] duplicates = keys
            .GroupBy(key => key, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        foreach (string duplicate in duplicates)
        {
            errors.Add($"Duplicate {kind} key '{duplicate}'.");
        }
    }

    private static void ValidateCosts(IReadOnlyList<HiveResourceCostCatalogEntry> costs, string owner, List<string> errors)
    {
        if (costs.Count == 0)
        {
            errors.Add($"{owner} must declare at least one server-side cost.");
        }

        foreach (HiveResourceCostCatalogEntry cost in costs)
        {
            if (cost.Amount < 0)
            {
                errors.Add($"{owner} cost '{cost.ResourceKey}' must be non-negative.");
            }
        }
    }
}

public sealed record HiveResourceCatalogEntry(
    string ResourceKey,
    string DisplayName,
    string StorageGroup,
    long InitialAmount,
    long BaseCapacity,
    long MinAmount,
    long MaxTransactionalDelta,
    string Precision,
    bool Enabled,
    string CatalogVersion);

public sealed record HiveBuildingCatalogEntry(
    string BuildingKey,
    string DisplayName,
    string BuildingCategory,
    int MaxLevel,
    string QueueType,
    int? RequiredPlayerLevel,
    IReadOnlyList<string> RequiredBuildingKeys,
    bool Enabled,
    string CatalogVersion);

public sealed record HiveBuildingLevelCatalogEntry(
    string BuildingKey,
    int Level,
    long? StorageCapacityBonus,
    int? TrainingCapacityBonus,
    decimal? ProductionModifier,
    IReadOnlyList<string> Unlocks,
    string CatalogVersion);

public sealed record HiveBuildingUpgradeCatalogEntry(
    string BuildingKey,
    int FromLevel,
    int ToLevel,
    IReadOnlyList<HiveResourceCostCatalogEntry> ResourceCosts,
    int DurationSeconds,
    bool RequiresServerClock,
    string CatalogVersion);

public sealed record HiveResourceCostCatalogEntry(string ResourceKey, long Amount);

public sealed record HiveTroopCatalogEntry(
    string TroopKey,
    string DisplayName,
    string TroopCategory,
    int BasePowerBand,
    int BaseCapacityCost,
    string RequiredBuildingKey,
    int RequiredBuildingLevel,
    bool Enabled,
    string CatalogVersion);

public sealed record HiveTroopTrainingCatalogEntry(
    string TroopKey,
    int QuantityStep,
    IReadOnlyList<HiveResourceCostCatalogEntry> ResourceCosts,
    int DurationSecondsPerUnit,
    int BatchSizeMin,
    int BatchSizeMax,
    string RequiredTrainingBuildingKey,
    int RequiredTrainingBuildingLevel,
    string CatalogVersion);

public sealed record HiveArmyCapacityCatalogEntry(
    string SourceType,
    string SourceKey,
    int CapacityBonus,
    string? AppliesToTroopCategory,
    string CatalogVersion);

public sealed record HiveIdempotencyPolicy(
    bool Required,
    string HeaderName,
    bool StoreHashOnly,
    bool RequiresPayloadHash,
    string ReplaySamePayloadResult,
    string ReplayDifferentPayloadResult);

public sealed record HiveAntiDoubleSpendPolicy(
    bool RequiresExpectedResourceRevision,
    bool RequiresExpectedTargetRevision,
    bool RequiresAtomicResourceDebitAndQueueCreate,
    bool RejectsClientProvidedCost,
    bool RejectsClientProvidedDuration,
    bool RejectsCrossWorldScope);

public sealed record HiveLoopCatalogValidationResult(bool IsValid, IReadOnlyList<string> Errors);

public static class HiveLoopCodeFirstCatalogs
{
    public const string ReadinessCatalogVersion = "server-035-readiness-non-live";

    public static HiveLoopCatalogSet CreateReadinessCatalog()
    {
        return new HiveLoopCatalogSet(
            ReadinessCatalogVersion,
            ReadOnly: true,
            NonLive: true,
            Resources:
            [
                new HiveResourceCatalogEntry("honey", "Honey", "basic_storage", InitialAmount: 0, BaseCapacity: 500, MinAmount: 0, MaxTransactionalDelta: 100_000, "Integer", Enabled: true, ReadinessCatalogVersion),
                new HiveResourceCatalogEntry("wax", "Wax", "basic_storage", InitialAmount: 0, BaseCapacity: 250, MinAmount: 0, MaxTransactionalDelta: 100_000, "Integer", Enabled: true, ReadinessCatalogVersion),
                new HiveResourceCatalogEntry("pollen", "Pollen", "basic_storage", InitialAmount: 0, BaseCapacity: 300, MinAmount: 0, MaxTransactionalDelta: 100_000, "Integer", Enabled: true, ReadinessCatalogVersion)
            ],
            Buildings:
            [
                new HiveBuildingCatalogEntry("honey_storage", "Honey Storage", "Storage", MaxLevel: 3, "Construction", RequiredPlayerLevel: null, RequiredBuildingKeys: [], Enabled: true, ReadinessCatalogVersion),
                new HiveBuildingCatalogEntry("training_nursery", "Training Nursery", "Training", MaxLevel: 3, "Construction", RequiredPlayerLevel: null, RequiredBuildingKeys: ["honey_storage"], Enabled: true, ReadinessCatalogVersion)
            ],
            BuildingLevels:
            [
                new HiveBuildingLevelCatalogEntry("honey_storage", 0, StorageCapacityBonus: 0, TrainingCapacityBonus: null, ProductionModifier: null, Unlocks: [], ReadinessCatalogVersion),
                new HiveBuildingLevelCatalogEntry("honey_storage", 1, StorageCapacityBonus: 250, TrainingCapacityBonus: null, ProductionModifier: null, Unlocks: ["basic_storage"], ReadinessCatalogVersion),
                new HiveBuildingLevelCatalogEntry("training_nursery", 0, StorageCapacityBonus: null, TrainingCapacityBonus: 0, ProductionModifier: null, Unlocks: [], ReadinessCatalogVersion),
                new HiveBuildingLevelCatalogEntry("training_nursery", 1, StorageCapacityBonus: null, TrainingCapacityBonus: 10, ProductionModifier: null, Unlocks: ["worker_training"], ReadinessCatalogVersion)
            ],
            BuildingUpgrades:
            [
                new HiveBuildingUpgradeCatalogEntry("honey_storage", FromLevel: 0, ToLevel: 1, [new HiveResourceCostCatalogEntry("honey", 50), new HiveResourceCostCatalogEntry("wax", 10)], DurationSeconds: 60, RequiresServerClock: true, ReadinessCatalogVersion),
                new HiveBuildingUpgradeCatalogEntry("training_nursery", FromLevel: 0, ToLevel: 1, [new HiveResourceCostCatalogEntry("honey", 80), new HiveResourceCostCatalogEntry("wax", 20)], DurationSeconds: 90, RequiresServerClock: true, ReadinessCatalogVersion)
            ],
            Troops:
            [
                new HiveTroopCatalogEntry("worker_bee", "Worker Bee", "Worker", BasePowerBand: 1, BaseCapacityCost: 1, RequiredBuildingKey: "training_nursery", RequiredBuildingLevel: 1, Enabled: true, ReadinessCatalogVersion),
                new HiveTroopCatalogEntry("guard_bee", "Guard Bee", "Defense", BasePowerBand: 2, BaseCapacityCost: 2, RequiredBuildingKey: "training_nursery", RequiredBuildingLevel: 2, Enabled: false, ReadinessCatalogVersion)
            ],
            TroopTraining:
            [
                new HiveTroopTrainingCatalogEntry("worker_bee", QuantityStep: 1, [new HiveResourceCostCatalogEntry("honey", 8), new HiveResourceCostCatalogEntry("pollen", 3)], DurationSecondsPerUnit: 15, BatchSizeMin: 1, BatchSizeMax: 20, RequiredTrainingBuildingKey: "training_nursery", RequiredTrainingBuildingLevel: 1, ReadinessCatalogVersion),
                new HiveTroopTrainingCatalogEntry("guard_bee", QuantityStep: 1, [new HiveResourceCostCatalogEntry("honey", 15), new HiveResourceCostCatalogEntry("pollen", 6)], DurationSecondsPerUnit: 30, BatchSizeMin: 1, BatchSizeMax: 10, RequiredTrainingBuildingKey: "training_nursery", RequiredTrainingBuildingLevel: 2, ReadinessCatalogVersion)
            ],
            ArmyCapacity:
            [
                new HiveArmyCapacityCatalogEntry("BuildingLevel", "training_nursery:1", CapacityBonus: 10, AppliesToTroopCategory: null, ReadinessCatalogVersion),
                new HiveArmyCapacityCatalogEntry("BuildingLevel", "training_nursery:2", CapacityBonus: 25, AppliesToTroopCategory: null, ReadinessCatalogVersion)
            ],
            new HiveIdempotencyPolicy(
                Required: true,
                HeaderName: "Idempotency-Key",
                StoreHashOnly: true,
                RequiresPayloadHash: true,
                ReplaySamePayloadResult: "AlreadyApplied",
                ReplayDifferentPayloadResult: "Conflict"),
            new HiveAntiDoubleSpendPolicy(
                RequiresExpectedResourceRevision: true,
                RequiresExpectedTargetRevision: true,
                RequiresAtomicResourceDebitAndQueueCreate: true,
                RejectsClientProvidedCost: true,
                RejectsClientProvidedDuration: true,
                RejectsCrossWorldScope: true));
    }
}
