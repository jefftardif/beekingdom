namespace BeeKingdom.HiveOperations;

public sealed record OfflineProductionCatalogEntry(string BuildingKey, string ResourceKey, decimal HourlyRate, long Capacity);
public sealed record OfflineProductionLine(string BuildingKey, string ResourceKey, decimal PendingAmount, decimal HourlyRate, long Capacity, long CollectableWholeUnits);
public sealed record OfflineProductionReadSnapshot(Guid PlayerId, Guid HiveId, string ContractVersion, string CatalogVersion, long ProductionRevision, DateTimeOffset ServerTimeUtc, DateTimeOffset ProductionAsOfUtc, TimeSpan MaxRecognizedDuration, IReadOnlyList<OfflineProductionLine> Lines, IReadOnlyDictionary<string, ResourceBalance> Balances);
public sealed record CollectOfflineProductionRequest(long ExpectedProductionRevision, string IdempotencyKey);
public sealed record OfflineProductionReceipt(Guid PlayerId, Guid HiveId, string IdempotencyKey, string BuildingKey, string ResourceKey, long CreditedAmount, decimal RemainingPending, long ProductionRevision, DateTimeOffset ServerTimeUtc, ResourceBalance ResultingBalance);
public sealed record OfflineProductionCollectResponse(OfflineProductionReceipt Receipt, OfflineProductionReadSnapshot Snapshot);
public sealed record OfflineProductionStoredReceipt(string PayloadHash, DateTimeOffset AcceptedAtUtc, OfflineProductionCollectResponse Response);

public sealed class HiveOfflineProductionOptions
{
    public const string SectionName = "HiveOfflineProduction";
    public bool Enabled { get; set; }
    public string CatalogVersion { get; set; } = "";
    public List<OfflineProductionCatalogEntry> Catalog { get; set; } = [];
    public TimeSpan MaxRecognizedDuration { get; set; } = TimeSpan.FromDays(7);
    public void Validate()
    {
        if (Catalog is null) throw new InvalidDataException("Invalid offline production options");
        if (!Enabled && Catalog.Count == 0) return;
        if (string.IsNullOrWhiteSpace(CatalogVersion) || CatalogVersion.Trim() != CatalogVersion || CatalogVersion.Length > 64 || !System.Text.RegularExpressions.Regex.IsMatch(CatalogVersion, "^[a-z0-9._-]+$") || MaxRecognizedDuration <= TimeSpan.Zero || MaxRecognizedDuration > TimeSpan.FromDays(7) || Catalog.Count != 3) throw new InvalidDataException("Invalid offline production options");
        string[] keys = ["honey_storage", "wax_workshop", "warehouse_cells"];
        if (Catalog.Select(x => x.BuildingKey).Distinct(StringComparer.Ordinal).Count() != 3 || Catalog.Select(x => x.ResourceKey).Distinct(StringComparer.Ordinal).Count() != 3 || Catalog.Any(x => !keys.Contains(x.BuildingKey, StringComparer.Ordinal) || x.ResourceKey != (x.BuildingKey == "honey_storage" ? "honey" : x.BuildingKey == "wax_workshop" ? "wax" : "pollen") || x.HourlyRate <= 0 || x.HourlyRate > 1_000_000m || x.Capacity <= 0 || x.Capacity > 1_000_000_000)) throw new InvalidDataException("Invalid offline production catalog");
    }
}
