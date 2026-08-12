using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BeeKingdom.HiveOperations;

public sealed record OfflineProductionCollectionResult(bool Succeeded, string Code, OfflineProductionCollectResponse? Response, OfflineProductionReadSnapshot? Snapshot);

public sealed class HiveOfflineProductionService
{
    public const string ContractVersion = "living-hive-offline-production-v1";
    public const int MaxReceipts = 512;
    private readonly IHiveStateRepository repository;
    private readonly IServerClock clock;
    private readonly HiveOfflineProductionOptions options;
    private readonly bool dailyRoundEnabled;
    private readonly IReadOnlyList<OfflineProductionCatalogEntry> catalog;

    public HiveOfflineProductionService(IHiveStateRepository repository, IServerClock clock, HiveOfflineProductionOptions options, bool dailyRoundEnabled = false)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.dailyRoundEnabled = dailyRoundEnabled;
        options.Validate();
        if (!options.Enabled) throw new InvalidOperationException("Offline production is disabled");
        catalog = options.Catalog.OrderBy(x => x.BuildingKey, StringComparer.Ordinal).ToArray();
    }

    public async Task<OfflineProductionReadSnapshot> ReadSnapshotAsync(Guid playerId, Guid hiveId, CancellationToken ct = default)
    {
        ValidateIds(playerId, hiveId);
        DateTimeOffset now = clock.UtcNow;
        PlayerHiveState state = await repository.ExecuteAtomicallyAsync(playerId, hiveId, current => Accrue(current, playerId, hiveId, now), ct);
        return BuildSnapshot(state, now);
    }

    public async Task<OfflineProductionCollectionResult> CollectAsync(Guid playerId, Guid hiveId, string buildingKey, CollectOfflineProductionRequest request, CancellationToken ct = default)
    {
        ValidateIds(playerId, hiveId);
        if (request is null || request.ExpectedProductionRevision < 0 || string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 256 || !catalog.Any(x => x.BuildingKey == buildingKey))
            throw new ArgumentException("game.invalid_request");
        OfflineProductionCollectionResult? result = null;
        DateTimeOffset now = clock.UtcNow;
        await repository.ExecuteAtomicallyAsync(playerId, hiveId, state =>
        {
            string hash = Hash(buildingKey, request.ExpectedProductionRevision);
            if (state.OfflineProduction?.Receipts.TryGetValue(request.IdempotencyKey, out OfflineProductionStoredReceipt? stored) == true)
            {
                result = stored.PayloadHash == hash ? new(true, "game.idempotency_replay", stored.Response, stored.Response.Snapshot) : new(false, "game.idempotency_conflict", null, null);
                return state;
            }
            PlayerHiveState accrued = Accrue(state, playerId, hiveId, now);
            HiveOfflineProductionState production = accrued.OfflineProduction!;
            if (request.ExpectedProductionRevision != production.Revision) { result = new(false, "game.production_conflict", null, BuildSnapshot(accrued, now)); return accrued; }
            OfflineProductionCatalogEntry definition = catalog.Single(x => x.BuildingKey == buildingKey);
            decimal pending = production.PendingByBuilding.GetValueOrDefault(buildingKey);
            ResourceBalance balance = accrued.Resources.GetValueOrDefault(definition.ResourceKey, new ResourceBalance(0, 0));
            long whole = decimal.ToInt64(decimal.Floor(pending));
            long headroom = Math.Max(0, Math.Min(balance.Capacity, EffectiveCapacity(accrued, definition)) - balance.Amount);
            long credited = Math.Min(whole, headroom);
            if (credited <= 0) { result = new(false, whole > 0 ? "game.resource_capacity_full" : "game.production_not_ready", null, BuildSnapshot(accrued, now)); return accrued; }
            Dictionary<string, decimal> pendingMap = new(production.PendingByBuilding) { [buildingKey] = pending - credited };
            long nextRevision = production.Revision + 1;
            ResourceBalance resulting = balance with { Amount = checked(balance.Amount + credited) };
            Dictionary<string, ResourceBalance> resources = new(accrued.Resources) { [definition.ResourceKey] = resulting };
            PlayerHiveState updated = accrued with { Revision = checked(state.Revision + 1), Resources = resources, OfflineProduction = production with { Revision = nextRevision, PendingByBuilding = pendingMap } };
            // DailyRoundFacts is applied by the owning composition when enabled; the core remains flag-agnostic.
            OfflineProductionReadSnapshot snapshot = BuildSnapshot(updated, now);
            OfflineProductionReceipt receipt = new(playerId, hiveId, request.IdempotencyKey, buildingKey, definition.ResourceKey, credited, pendingMap[buildingKey], nextRevision, now, resulting);
            OfflineProductionCollectResponse response = new(receipt, snapshot);
            Dictionary<string, OfflineProductionStoredReceipt> receipts = new(production.Receipts) { [request.IdempotencyKey] = new(Hash(buildingKey, request.ExpectedProductionRevision), now, response) };
            if (receipts.Count > MaxReceipts) receipts.Remove(receipts.OrderBy(x => x.Value.AcceptedAtUtc).ThenBy(x => x.Key, StringComparer.Ordinal).First().Key);
            updated = updated with { OfflineProduction = updated.OfflineProduction! with { Receipts = receipts } };
            if (dailyRoundEnabled) updated = HiveDailyRoundFacts.ApplyFreshFact(updated, now, HiveDailyRoundFact.CollectionReceived, false);
            result = new(true, "game.production_collected", response, snapshot);
            return updated;
        }, ct);
        return result!;
    }

    private PlayerHiveState Accrue(PlayerHiveState state, Guid playerId, Guid hiveId, DateTimeOffset now)
    {
        if (now.Offset != TimeSpan.Zero) throw new InvalidOperationException("Server clock must be UTC");
        if (state.PlayerId != playerId || state.HiveId != hiveId) throw new InvalidDataException("Hive identity mismatch");
        HiveOfflineProductionState production = state.OfflineProduction ?? new(now, catalog.ToDictionary(x => x.BuildingKey, _ => 0m, StringComparer.Ordinal), 0, new Dictionary<string, OfflineProductionStoredReceipt>(StringComparer.Ordinal));
        DateTimeOffset asOf = production.ProductionAsOfUtc > now ? now : production.ProductionAsOfUtc;
        TimeSpan elapsed = now - asOf;
        if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;
        if (elapsed > options.MaxRecognizedDuration) elapsed = options.MaxRecognizedDuration;
        Dictionary<string, decimal> pending = new(production.PendingByBuilding, StringComparer.Ordinal);
        foreach (OfflineProductionCatalogEntry item in catalog)
            pending[item.BuildingKey] = Math.Min(EffectiveCapacity(state, item), pending.GetValueOrDefault(item.BuildingKey) + EffectiveRate(state, item) * elapsed.Ticks / (decimal)TimeSpan.TicksPerHour);
        bool changed = state.OfflineProduction is null || production.ProductionAsOfUtc != now || pending.Any(pair => production.PendingByBuilding.GetValueOrDefault(pair.Key) != pair.Value);
        production = production with { ProductionAsOfUtc = now, PendingByBuilding = pending };
        return state with { Revision = changed ? state.Revision + 1 : state.Revision, OfflineProduction = production };
    }

    private OfflineProductionReadSnapshot BuildSnapshot(PlayerHiveState state, DateTimeOffset now)
    {
        HiveOfflineProductionState production = state.OfflineProduction!;
        Dictionary<string, ResourceBalance> balances = new(StringComparer.Ordinal);
        foreach (string key in new[] { "honey", "wax", "pollen" }) { if (!state.Resources.TryGetValue(key, out ResourceBalance? balance) || balance.Amount < 0 || balance.Capacity < 0 || balance.Amount > balance.Capacity) throw new InvalidDataException("Invalid resource balance"); balances[key] = balance; }
        List<OfflineProductionLine> lines = catalog.Select(item => { ResourceBalance balance = balances[item.ResourceKey]; decimal pending = production.PendingByBuilding.GetValueOrDefault(item.BuildingKey); long whole = decimal.ToInt64(decimal.Floor(pending)); return new OfflineProductionLine(item.BuildingKey, item.ResourceKey, pending, EffectiveRate(state, item), EffectiveCapacity(state, item), Math.Min(whole, Math.Max(0, balance.Capacity - balance.Amount))); }).ToList();
        return new(state.PlayerId, state.HiveId, ContractVersion, options.CatalogVersion, production.Revision, now, production.ProductionAsOfUtc, options.MaxRecognizedDuration, lines.ToArray(), new Dictionary<string, ResourceBalance>(balances, StringComparer.Ordinal));
    }

    private static string Hash(string building, long revision) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { building, revision })))).ToLowerInvariant();
    // Le niveau de batiment (amelioration officielle, "BuildingUpgradeService") augmente
    // le taux et la capacite effective - un batiment sans entree dans BuildingLevels est
    // traite comme niveau 1 (aucune amelioration achetee), jamais niveau 0.
    private static int EffectiveBuildingLevel(PlayerHiveState state, OfflineProductionCatalogEntry item)
    {
        int level = state.BuildingLevels.GetValueOrDefault(item.BuildingKey, 1);
        if (level < 1) throw new InvalidDataException("Invalid building level");
        return level;
    }

    // Somme les bonus de recherche completes d'un type donne (miel/cire/pollen) - les paliers
    // superieurs s'additionnent aux paliers deja completes plutot que de les remplacer, voir
    // Docs/Product/BeeKingdom_ResearchTree_Design.md.
    private static int SumResearchBps(PlayerHiveState state, Func<ResearchEffects, int> selector)
    {
        if (state.Research is null) return 0;
        int total = 0;
        foreach (ResearchCompletion completion in state.Research.Completed.Values) total += selector(completion.Effects);
        if (total is < 0 or > 10_000) throw new InvalidDataException("Invalid research effect");
        return total;
    }

    private static decimal EffectiveRate(PlayerHiveState state, OfflineProductionCatalogEntry item)
    {
        int level = EffectiveBuildingLevel(state, item);
        int bps = item.ResourceKey switch
        {
            "honey" => SumResearchBps(state, effects => effects.HoneyProductionBonusBps),
            "wax" => SumResearchBps(state, effects => effects.WaxProductionBonusBps),
            "pollen" => SumResearchBps(state, effects => effects.PollenProductionBonusBps),
            _ => 0
        };
        bps += (int)StrategicPathBonusCatalog.ProductionRateBonusBpFor(state.StrategicPath?.SelectedPath);
        decimal levelMultiplier = 1m + 0.10m * (level - 1);
        return item.HourlyRate * levelMultiplier * (1m + bps / 10_000m);
    }
    private static long EffectiveCapacity(PlayerHiveState state, OfflineProductionCatalogEntry item)
    {
        int level = EffectiveBuildingLevel(state, item);
        long leveledCapacity = checked(item.Capacity * level);
        int researchBps = item.ResourceKey switch
        {
            "wax" => SumResearchBps(state, effects => effects.WaxCapacityBonusBps),
            "pollen" => SumResearchBps(state, effects => effects.PollenCapacityBonusBps),
            _ => 0
        };
        int globalBps = SumResearchBps(state, effects => effects.GlobalCapacityBonusBps);
        int vipBps = VipCatalog.CapacityBonusBps(VipCatalog.LevelForPoints(state.Vip?.LifetimePoints ?? 0));
        long strategicPathBps = StrategicPathBonusCatalog.CapacityBonusBpFor(state.StrategicPath?.SelectedPath);
        long bps = researchBps + globalBps + vipBps + strategicPathBps;
        return checked(leveledCapacity + (long)Math.Floor(leveledCapacity * (decimal)bps / 10_000m));
    }
    private static void ValidateIds(Guid playerId, Guid hiveId) { if (playerId == Guid.Empty || hiveId == Guid.Empty) throw new ArgumentException("game.invalid_request"); }
}
