using System.Security.Cryptography;
using System.Text;

namespace BeeKingdom.HiveOperations;

public sealed record BuildingUpgradeCatalogEntry(string BuildingKey, int FromLevel, int ToLevel, TimeSpan Duration, IReadOnlyDictionary<string, long> Costs);

public sealed class BuildingUpgradeOptions
{
    public const string SectionName = "BuildingUpgrades";
    private static readonly HashSet<string> KnownBuildingKeys = new(StringComparer.Ordinal)
    {
        "nursery_cluster", "honey_storage", "guard_post", "defense_growth", "genetics_garden",
        "research_node", "warehouse_cells", "wax_workshop", "infirmary_grove", "academy_canopy",
        "hive_bank", "administration_core", "alliance_future_hall", "archives_honeyfall"
    };
    private static readonly HashSet<string> KnownResourceKeys = new(StringComparer.Ordinal) { "honey", "wax", "pollen" };

    public bool Enabled { get; set; }
    public string CatalogVersion { get; set; } = "";
    public List<BuildingUpgradeCatalogEntry> Catalog { get; set; } = [];

    public void Validate()
    {
        if (!Enabled && Catalog.Count == 0) return;
        if (Catalog.Count == 0
            || CatalogVersion.Length is < 1 or > 64
            || CatalogVersion.Trim() != CatalogVersion
            || !System.Text.RegularExpressions.Regex.IsMatch(CatalogVersion, "^[a-z0-9._-]+$"))
            throw new InvalidDataException("Invalid building upgrade options");

        var seen = new HashSet<(string, int)>();
        foreach (BuildingUpgradeCatalogEntry entry in Catalog)
        {
            if (!KnownBuildingKeys.Contains(entry.BuildingKey)
                || entry.FromLevel < 1
                || entry.ToLevel != entry.FromLevel + 1
                || entry.Duration <= TimeSpan.Zero
                || entry.Duration > TimeSpan.FromDays(7)
                || entry.Costs.Count == 0
                || entry.Costs.Any(cost => !KnownResourceKeys.Contains(cost.Key) || cost.Value <= 0)
                || !seen.Add((entry.BuildingKey, entry.FromLevel)))
                throw new InvalidDataException("Invalid building upgrade catalog");
        }
    }
}

public sealed record BuildingUpgradeOffer(string BuildingKey, int FromLevel, int ToLevel, TimeSpan Duration, IReadOnlyDictionary<string, long> Costs);
public sealed record BuildingUpgradeActiveOperation(Guid OperationId, string BuildingKey, int FromLevel, int ToLevel, DateTimeOffset StartedAtUtc, DateTimeOffset CompletesAtUtc, string Status);
public sealed record BuildingUpgradeReadSnapshot(Guid PlayerId, Guid HiveId, string ContractVersion, string CatalogVersion, long Revision, DateTimeOffset ServerTimeUtc, IReadOnlyDictionary<string, ResourceBalance> Balances, IReadOnlyDictionary<string, int> BuildingLevels, IReadOnlyList<BuildingUpgradeOffer> Offers, BuildingUpgradeActiveOperation? ActiveOperation);
public sealed record StartBuildingUpgradeRequest(long ExpectedRevision, string IdempotencyKey);
public sealed record CompleteBuildingUpgradeRequest(long ExpectedRevision, string IdempotencyKey);
public sealed record BuildingUpgradeReceipt(Guid PlayerId, Guid HiveId, string IdempotencyKey, Guid OperationId, string BuildingKey, int FromLevel, int ToLevel, long Revision, DateTimeOffset AcceptedAtUtc, string Code);
public sealed record BuildingUpgradeResponse(BuildingUpgradeReceipt Receipt, BuildingUpgradeReadSnapshot Snapshot);
public sealed record BuildingUpgradeCommandResult(bool Succeeded, string Code, BuildingUpgradeReadSnapshot Snapshot, BuildingUpgradeResponse? Response = null);

// Systeme d'amelioration de batiment generique (miel/cire/pollen, plusieurs
// paliers par batiment) - le catalogue est fourni en configuration, cle par
// (BuildingKey, FromLevel), meme convention que HiveOperationService.
public sealed class BuildingUpgradeService(IHiveStateRepository repository, IServerClock clock, BuildingUpgradeOptions options, bool dailyRoundEnabled = false)
{
    public const string ContractVersion = "living-hive-building-upgrade-v1";
    private readonly BuildingUpgradeOptions o = options ?? throw new ArgumentNullException(nameof(options));
    private Dictionary<(string BuildingKey, int FromLevel), BuildingUpgradeCatalogEntry> CatalogByKey => o.Catalog.ToDictionary(x => (x.BuildingKey, x.FromLevel));

    public async Task<BuildingUpgradeReadSnapshot> ReadAsync(Guid playerId, Guid hiveId, CancellationToken ct = default)
    {
        Ensure();
        DateTimeOffset now = Utc();
        return Snapshot(await repository.ExecuteAtomicallyAsync(playerId, hiveId, state => state, ct), now);
    }

    public async Task<BuildingUpgradeCommandResult> StartAsync(Guid playerId, Guid hiveId, string buildingKey, StartBuildingUpgradeRequest request, CancellationToken ct = default)
    {
        Ensure();
        if (string.IsNullOrWhiteSpace(buildingKey) || request is null || request.ExpectedRevision < 0 || !ValidKey(request.IdempotencyKey))
            return Fail(playerId, hiveId, "game.invalid_request");

        var catalogByKey = CatalogByKey;
        BuildingUpgradeCommandResult? result = null;
        await repository.ExecuteAtomicallyAsync(playerId, hiveId, state =>
        {
            DateTimeOffset now = Utc();
            string hash = Hash($"start|{buildingKey}|{request.ExpectedRevision}");
            if (state.Receipts.TryGetValue(request.IdempotencyKey, out IdempotencyReceipt? stored))
            {
                result = stored.PayloadHash == hash ? Replay(state, stored, now, request.IdempotencyKey) : Fail(state, "game.idempotency_conflict", now);
                return state;
            }
            if (state.Revision != request.ExpectedRevision) { result = Fail(state, "game.revision_conflict", now); return state; }

            int currentLevel = state.BuildingLevels.GetValueOrDefault(buildingKey, 1);
            if (!catalogByKey.TryGetValue((buildingKey, currentLevel), out BuildingUpgradeCatalogEntry? entry))
            {
                result = Fail(state, "game.invalid_building_level", now);
                return state;
            }
            // Un seul chantier a la fois pour toute la ruche (pas par batiment) - le modele
            // de lecture n'a qu'un seul emplacement ActiveOperation ; autoriser plusieurs
            // chantiers simultanes afficherait un faux "occupe" sur un batiment qui n'a
            // pourtant rien en cours.
            if (state.Operations.Any(x => x.Kind == HiveOperationKind.BuildingUpgrade && x.Status != HiveOperationStatus.Collected))
            {
                result = Fail(state, "game.construction_busy", now);
                return state;
            }
            if (entry.Costs.Any(cost => !state.Resources.TryGetValue(cost.Key, out ResourceBalance? balance) || balance.Amount < cost.Value))
            {
                result = Fail(state, "game.insufficient_resources", now);
                return state;
            }

            Dictionary<string, ResourceBalance> resources = new(state.Resources);
            foreach (KeyValuePair<string, long> cost in entry.Costs)
                resources[cost.Key] = resources[cost.Key] with { Amount = resources[cost.Key].Amount - cost.Value };
            HiveOperation operation = new(Guid.NewGuid(), buildingKey, entry.FromLevel, entry.ToLevel, now, now + entry.Duration, HiveOperationStatus.Running, "", 0, null);
            PlayerHiveState updated = state with { Revision = state.Revision + 1, Resources = resources, Operations = [.. state.Operations, operation] };
            if (dailyRoundEnabled) updated = HiveDailyRoundFacts.ApplyFreshFact(updated, now, HiveDailyRoundFact.OperationLaunched, false);
            result = Success(updated, now, operation, request.IdempotencyKey, "game.building_upgrade_started");
            return Receipt(updated, request.IdempotencyKey, hash, result, now);
        }, ct);
        return result!;
    }

    public async Task<BuildingUpgradeCommandResult> CompleteAsync(Guid playerId, Guid hiveId, Guid operationId, CompleteBuildingUpgradeRequest request, CancellationToken ct = default)
    {
        Ensure();
        if (operationId == Guid.Empty || request is null || request.ExpectedRevision < 0 || !ValidKey(request.IdempotencyKey))
            return Fail(playerId, hiveId, "game.invalid_request");

        BuildingUpgradeCommandResult? result = null;
        await repository.ExecuteAtomicallyAsync(playerId, hiveId, state =>
        {
            DateTimeOffset now = Utc();
            string hash = Hash($"complete|{operationId}|{request.ExpectedRevision}");
            if (state.Receipts.TryGetValue(request.IdempotencyKey, out IdempotencyReceipt? stored))
            {
                result = stored.PayloadHash == hash ? Replay(state, stored, now, request.IdempotencyKey) : Fail(state, "game.idempotency_conflict", now);
                return state;
            }
            if (state.Revision != request.ExpectedRevision) { result = Fail(state, "game.revision_conflict", now); return state; }

            int index = state.Operations.FindIndex(x => x.OperationId == operationId && x.Kind == HiveOperationKind.BuildingUpgrade);
            if (index < 0) { result = Fail(state, "game.operation_not_found", now); return state; }
            HiveOperation operation = state.Operations[index];
            if (operation.CompletesAtUtc > now) { result = Fail(state, "game.not_ready", now); return state; }
            // Une autre lecture (production hors ligne, etc.) peut deja avoir fait passer le
            // statut stocke de Running a AwaitingCollection avec le temps - les deux sont
            // valides avant collecte, seul Collected signifie reellement "deja termine".
            if (operation.Status is not (HiveOperationStatus.Running or HiveOperationStatus.AwaitingCollection))
            {
                result = Fail(state, "game.already_completed", now);
                return state;
            }

            List<HiveOperation> operations = [.. state.Operations];
            operations[index] = operation with { Status = HiveOperationStatus.Collected, CollectedAtUtc = now };
            Dictionary<string, int> levels = new(state.BuildingLevels) { [operation.BuildingKey] = operation.ToLevel };
            PlayerHiveState updated = state with { Revision = state.Revision + 1, Operations = operations, BuildingLevels = levels };
            result = Success(updated, now, operations[index], request.IdempotencyKey, "game.building_upgrade_completed");
            return Receipt(updated, request.IdempotencyKey, hash, result, now);
        }, ct);
        return result!;
    }

    private void Ensure()
    {
        o.Validate();
        if (!o.Enabled) throw new InvalidOperationException("Building upgrades are disabled");
    }

    private DateTimeOffset Utc()
    {
        DateTimeOffset now = clock.UtcNow;
        if (now.Offset != TimeSpan.Zero) throw new InvalidDataException("Server clock must be UTC");
        return now;
    }

    private static bool ValidKey(string? key) => !string.IsNullOrWhiteSpace(key) && key.Trim() == key && key.Length <= 256;
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private BuildingUpgradeReadSnapshot Snapshot(PlayerHiveState state, DateTimeOffset now)
    {
        var catalogByKey = CatalogByKey;
        Dictionary<string, int> levels = new(state.BuildingLevels);
        // Un batiment jamais ameliore n'a pas d'entree dans BuildingLevels - le client
        // exige que le niveau soit explicitement present dans l'instantane pour chaque
        // offre proposee, donc on le materialise ici (niveau 1 implicite) plutot que de
        // laisser une offre de palier 1 sans niveau correspondant.
        foreach (string buildingKey in o.Catalog.Select(x => x.BuildingKey).Distinct(StringComparer.Ordinal))
            if (!levels.ContainsKey(buildingKey)) levels[buildingKey] = 1;
        List<BuildingUpgradeOffer> offers = [];
        foreach (string buildingKey in o.Catalog.Select(x => x.BuildingKey).Distinct(StringComparer.Ordinal))
        {
            int level = levels.GetValueOrDefault(buildingKey, 1);
            if (catalogByKey.TryGetValue((buildingKey, level), out BuildingUpgradeCatalogEntry? entry))
                offers.Add(new BuildingUpgradeOffer(entry.BuildingKey, entry.FromLevel, entry.ToLevel, entry.Duration, new Dictionary<string, long>(entry.Costs)));
        }
        HiveOperation? active = state.Operations.FirstOrDefault(x => x.Kind == HiveOperationKind.BuildingUpgrade && x.Status != HiveOperationStatus.Collected);
        BuildingUpgradeActiveOperation? activeOperation = active is null
            ? null
            : new BuildingUpgradeActiveOperation(active.OperationId, active.BuildingKey, active.FromLevel, active.ToLevel, active.StartedAtUtc, active.CompletesAtUtc, active.CompletesAtUtc <= now ? "awaiting_completion" : "running");
        return new BuildingUpgradeReadSnapshot(state.PlayerId, state.HiveId, ContractVersion, o.CatalogVersion, state.Revision, now, new Dictionary<string, ResourceBalance>(state.Resources), new Dictionary<string, int>(levels), offers, activeOperation);
    }

    private BuildingUpgradeCommandResult Fail(Guid playerId, Guid hiveId, string code) =>
        new(false, code, new BuildingUpgradeReadSnapshot(playerId, hiveId, ContractVersion, o.CatalogVersion, 0, DateTimeOffset.UnixEpoch, new Dictionary<string, ResourceBalance>(), new Dictionary<string, int>(), Array.Empty<BuildingUpgradeOffer>(), null));

    private BuildingUpgradeCommandResult Fail(PlayerHiveState state, string code, DateTimeOffset now) => new(false, code, Snapshot(state, now));

    private BuildingUpgradeCommandResult Success(PlayerHiveState state, DateTimeOffset now, HiveOperation operation, string idempotencyKey, string code)
    {
        BuildingUpgradeReadSnapshot snapshot = Snapshot(state, now);
        var receipt = new BuildingUpgradeReceipt(state.PlayerId, state.HiveId, idempotencyKey, operation.OperationId, operation.BuildingKey, operation.FromLevel, operation.ToLevel, state.Revision, now, code);
        return new BuildingUpgradeCommandResult(true, code, snapshot, new BuildingUpgradeResponse(receipt, snapshot));
    }

    private BuildingUpgradeCommandResult Replay(PlayerHiveState state, IdempotencyReceipt receipt, DateTimeOffset now, string idempotencyKey)
    {
        BuildingUpgradeReadSnapshot snapshot = Snapshot(state, now);
        HiveOperation? operation = receipt.OperationId is { } opId ? state.Operations.FirstOrDefault(x => x.OperationId == opId) : null;
        BuildingUpgradeReceipt? typedReceipt = operation is null
            ? null
            : new BuildingUpgradeReceipt(state.PlayerId, state.HiveId, idempotencyKey, operation.OperationId, operation.BuildingKey, operation.FromLevel, operation.ToLevel, receipt.RevisionAfter ?? state.Revision, receipt.AcceptedAtUtc ?? receipt.CreatedAtUtc, receipt.Code);
        return new BuildingUpgradeCommandResult(receipt.Succeeded, receipt.Code, snapshot, typedReceipt is null ? null : new BuildingUpgradeResponse(typedReceipt, snapshot));
    }

    private PlayerHiveState Receipt(PlayerHiveState state, string idempotencyKey, string hash, BuildingUpgradeCommandResult result, DateTimeOffset now)
    {
        Dictionary<string, IdempotencyReceipt> receipts = new(state.Receipts)
        {
            [idempotencyKey] = new IdempotencyReceipt(hash, result.Succeeded, result.Code, result.Response?.Receipt.OperationId, now, state.Revision - 1, state.Revision, AcceptedAtUtc: now)
        };
        return state with { Receipts = receipts };
    }
}
