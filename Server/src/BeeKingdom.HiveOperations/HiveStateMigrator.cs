namespace BeeKingdom.HiveOperations;

public static class HiveStateMigrator
{
    public const int CurrentModelVersion = 10;

    public static PlayerHiveState ToCurrent(PlayerHiveState state)
    {
        if (state.ModelVersion <= 0 || state.ModelVersion > CurrentModelVersion)
            throw new InvalidOperationException($"Unsupported hive state model version '{state.ModelVersion}'.");

        Dictionary<string, int> buildingLevels = state.BuildingLevels is null
            ? new Dictionary<string, int>(StringComparer.Ordinal)
            : new Dictionary<string, int>(state.BuildingLevels, StringComparer.Ordinal);
        bool implicitBuildingDefaultsApplied = state.ImplicitBuildingDefaultsApplied;
        if (!buildingLevels.ContainsKey("guard_post"))
        {
            buildingLevels["guard_post"] = 1;
            implicitBuildingDefaultsApplied = true;
        }
        state = state with { BuildingLevels = buildingLevels, ImplicitBuildingDefaultsApplied = implicitBuildingDefaultsApplied };
        if (state.SpeedUps is { } speedUps && (speedUps.Count > 512 || speedUps.Any(item => string.IsNullOrWhiteSpace(item.Key) || item.Key.Length > 128 || item.Value < 0 || item.Value > 1_000_000_000)))
            throw new InvalidDataException("Invalid SpeedUp inventory state.");
        if (state.RoyalSeals < 0)
            throw new InvalidDataException("Invalid Royal Seals wallet balance.");

        if (state.BroodVitality is { } vitality)
        {
            if (vitality.Nutrition is < 0 or > 100 || vitality.Stability is < 0 or > 100 || vitality.Revision < 0 || vitality.Revision > state.Revision || vitality.UpdatedAtUtc.Offset != TimeSpan.Zero)
                throw new InvalidOperationException("Invalid brood vitality state.");
            if (vitality.ActiveOperation is { } operation && (operation.OperationId == Guid.Empty || !BroodVitalityOperationTypes.Allowed.Contains(operation.Type) || operation.StartedAtUtc.Offset != TimeSpan.Zero || operation.EndsAtUtc.Offset != TimeSpan.Zero || operation.EndsAtUtc < operation.StartedAtUtc || operation.EndsAtUtc - operation.StartedAtUtc != (operation.Type == BroodVitalityOperationTypes.Feeding ? TimeSpan.FromSeconds(12) : TimeSpan.FromSeconds(13))))
                throw new InvalidOperationException("Invalid brood vitality operation.");
        }
        if (state.DoctrineRoster is { } roster)
        {
            const long maxCount = 1_000_000_000;
            const int maxReceipts = 4096;
            if (roster.Revision < 0 || roster.Revision > state.Revision || roster.Counts is null || roster.Receipts is null || roster.Counts.Count > CombatDoctrineService.Families.Count || roster.Counts.Keys.Any(k => !CombatDoctrineService.Families.Contains(k)) || roster.Counts.Values.Any(v => v < 0 || v > maxCount) || roster.Receipts.Count > maxReceipts || roster.Receipts.Any(x => string.IsNullOrWhiteSpace(x.Key) || string.IsNullOrWhiteSpace(x.Value.PayloadHash)))
                throw new InvalidOperationException("Invalid doctrine roster state.");
            if (roster.ActiveOperation is { } op && (!CombatRecruitmentService.Catalog.TryGetValue(op.Family, out var definition) || op.OperationId == Guid.Empty || op.BatchSize != definition.BatchSize || op.StartedAtUtc.Offset != TimeSpan.Zero || op.EndsAtUtc.Offset != TimeSpan.Zero || op.EndsAtUtc < op.StartedAtUtc || op.Revision != roster.Revision || string.IsNullOrWhiteSpace(op.IdempotencyKey) || op.IdempotencyKey.Length > 256 || string.IsNullOrWhiteSpace(op.PayloadHash)))
                throw new InvalidOperationException("Invalid doctrine training operation.");
        }
        if (state.SquadReservation is { } reservation)
        {
            if (reservation.Capacity <= 0 || reservation.Capacity > CombatSquadReservationService.MaxCapacity || reservation.Revision < 0 || reservation.Revision > state.Revision || reservation.Reserved is null || reservation.Reserved.Count != 3 || reservation.Reserved.Keys.Any(k => !CombatDoctrineService.Families.Contains(k)) || reservation.Reserved.Values.Any(v => v < 0 || v > 1_000_000) || SafeSum(reservation.Reserved.Values) > reservation.Capacity || (reservation.ReservationId is null && reservation.Reserved.Values.Any(v => v != 0)) || (reservation.ReservationId is not null && (string.IsNullOrWhiteSpace(reservation.ReservationId) || SafeSum(reservation.Reserved.Values) <= 0)) || reservation.Receipts is null || reservation.Receipts.Count > 4096 || reservation.Receipts.Any(x => string.IsNullOrWhiteSpace(x.Key) || string.IsNullOrWhiteSpace(x.Value.PayloadHash)))
                throw new InvalidDataException("Invalid squad reservation state");
            var rosterCounts = state.DoctrineRoster?.Counts ?? new Dictionary<string, long>();
            if (reservation.Reserved.Any(x => x.Value > rosterCounts.GetValueOrDefault(x.Key)))
                throw new InvalidDataException("Squad reservation exceeds doctrine roster.");
        }
        if (state.HivePerimeterSortie is { } sortie)
        {
            if (sortie.Revision < 0 || sortie.Revision > state.Revision || sortie.CycleStartedAtUtc.Offset != TimeSpan.Zero || sortie.CycleEndsAtUtc.Offset != TimeSpan.Zero || sortie.CycleStartedAtUtc.Second != 0 || sortie.CycleStartedAtUtc.Millisecond != 0 || sortie.CycleStartedAtUtc.Ticks % TimeSpan.TicksPerSecond != 0 || sortie.CycleEndsAtUtc - sortie.CycleStartedAtUtc != TimeSpan.FromHours(8) || sortie.CycleStartedAtUtc.Hour % 8 != 0 || sortie.CycleStartedAtUtc.Minute != 0 || sortie.Receipts is null || sortie.Receipts.Count > 4096 || sortie.Receipts.Any(x => string.IsNullOrWhiteSpace(x.Key) || x.Key.Length > 256 || string.IsNullOrWhiteSpace(x.Value.PayloadHash)) || sortie.ClaimReceipts is not null && (sortie.ClaimReceipts.Count > 4096 || sortie.ClaimReceipts.Any(x => string.IsNullOrWhiteSpace(x.Key) || x.Key.Length > 256 || x.Value is null || x.Value.PlayerId != state.PlayerId || x.Value.HiveId != state.HiveId || x.Value.SortieId == Guid.Empty || !HivePerimeterSortieService.Catalog.ContainsKey(x.Value.SignalKey) || string.IsNullOrWhiteSpace(x.Value.SignalInstanceId) || x.Value.CycleStartedAtUtc != sortie.CycleStartedAtUtc || x.Value.CycleEndsAtUtc != sortie.CycleEndsAtUtc || x.Value.ServerTimeUtc.Offset != TimeSpan.Zero || x.Value.Revision < 0 || x.Value.Revision > state.Revision || x.Value.CreditedByResource is null || x.Value.CreditedByResource.Any(v => v.Value < 0) || x.Value.ResultingBalances is null || x.Value.ResultingBalances.Any(v => string.IsNullOrWhiteSpace(v.Key) || v.Value.Amount < 0 || v.Value.Capacity < v.Value.Amount))) || sortie.CompletedSignalKeys is null || sortie.CompletedSignalKeys.Count > HivePerimeterSortieService.Catalog.Count || sortie.CompletedSignalKeys.Any(x => !HivePerimeterSortieService.Catalog.ContainsKey(x)))
                throw new InvalidDataException("Invalid hive perimeter cycle state.");
            if (sortie.Active is { } active)
            {
                if (active.SortieId == Guid.Empty || !HivePerimeterSortieService.Catalog.TryGetValue(active.SignalKey, out var signal) || !CombatDoctrineService.Families.Contains(signal.HazardDoctrine) || string.IsNullOrWhiteSpace(active.SignalInstanceId) || active.SignalInstanceId != HivePerimeterSortieService.InstanceId(state.PlayerId, state.HiveId, sortie.CycleStartedAtUtc, active.SignalKey) || string.IsNullOrWhiteSpace(active.ReservationId) || active.ReservationId.Length > 256 || active.StartedAtUtc < sortie.CycleStartedAtUtc || active.StartedAtUtc >= sortie.CycleEndsAtUtc || active.StartedAtUtc.Offset != TimeSpan.Zero || active.EndsAtUtc.Offset != TimeSpan.Zero || active.EndsAtUtc < active.StartedAtUtc || active.EndsAtUtc - active.StartedAtUtc != signal.Duration || active.Revision != sortie.Revision || string.IsNullOrWhiteSpace(active.LaunchIdempotencyKey) || active.LaunchIdempotencyKey.Length > 256 || string.IsNullOrWhiteSpace(active.PayloadHash))
                    throw new InvalidDataException("Invalid hive perimeter active sortie.");
                if (state.SquadReservation?.ReservationId != active.ReservationId || state.SquadReservation is null || SafeSum(state.SquadReservation.Reserved.Values) <= 0)
                    throw new InvalidDataException("Perimeter sortie reservation is not held.");
                if (SafeSum(state.SquadReservation.Reserved.Values) < signal.MinimumSquad)
                    throw new InvalidDataException("Perimeter sortie squad is below the signal minimum.");
            }
        }
        if (state.CombatPatrol is { } patrol)
        {
            if (patrol.Revision < 0 || patrol.Revision > state.Revision || patrol.TierCooldownEndsAtUtc is null || patrol.TierCooldownEndsAtUtc.Keys.Any(t => !CombatPatrolCatalog.Tiers.ContainsKey(t)) || patrol.TierCooldownEndsAtUtc.Values.Any(v => v.Offset != TimeSpan.Zero) || patrol.Receipts is null || patrol.Receipts.Count > 4096 || patrol.Receipts.Any(x => string.IsNullOrWhiteSpace(x.Key) || x.Key.Length > 256 || string.IsNullOrWhiteSpace(x.Value.PayloadHash)))
                throw new InvalidDataException("Invalid combat patrol state.");
            if (patrol.Recovering is { } recovering && (recovering.Count > 4096 || recovering.Any(x => !CombatDoctrineService.Families.Contains(x.Family) || x.Count <= 0 || x.ReadyAtUtc.Offset != TimeSpan.Zero)))
                throw new InvalidDataException("Invalid combat patrol recovery queue.");
            if (patrol.ResourcePurchasedSlots < 0 || patrol.ResourcePurchasedSlots > CombatPatrolCatalog.MaxResourcePurchasedSlots || patrol.PremiumPurchasedSlots < 0 || patrol.PremiumPurchasedSlots > CombatPatrolCatalog.MaxPremiumPurchasedSlots)
                throw new InvalidDataException("Invalid combat patrol slot counters.");
            if (patrol.ActiveEncounters is null || patrol.ActiveEncounters.Count > CombatPatrolCatalog.MaxConcurrentSlots || patrol.ActiveEncounters.Select(e => e.EncounterId).Distinct().Count() != patrol.ActiveEncounters.Count)
                throw new InvalidDataException("Invalid combat patrol active encounter list.");
            foreach (CombatPatrolActiveEncounter active in patrol.ActiveEncounters)
            {
                if (active.EncounterId == Guid.Empty || !CombatPatrolCatalog.TryGet(active.Tier, out BestiaryTierDefinition tierDefinition) || active.CommittedTroops is null || active.CommittedTroops.Keys.Any(f => !CombatDoctrineService.Families.Contains(f)) || active.CommittedTroops.Values.Any(v => v < 0) || active.CommittedTroops.Values.Sum() <= 0 || active.StartedAtUtc.Offset != TimeSpan.Zero || active.EndsAtUtc.Offset != TimeSpan.Zero || active.EndsAtUtc < active.StartedAtUtc || active.EndsAtUtc - active.StartedAtUtc != tierDefinition.Duration || string.IsNullOrWhiteSpace(active.LaunchIdempotencyKey) || active.LaunchIdempotencyKey.Length > 256 || string.IsNullOrWhiteSpace(active.PayloadHash))
                    throw new InvalidDataException("Invalid combat patrol active encounter.");
            }
        }
        if (state.WorldResourceCollection is { } worldResources)
        {
            if (worldResources.Revision < 0 || worldResources.NodeReadyAtUtc is null || worldResources.NodeReadyAtUtc.Values.Any(v => v.Offset != TimeSpan.Zero) || worldResources.Receipts is null || worldResources.Receipts.Count > 4096 || worldResources.Receipts.Any(x => string.IsNullOrWhiteSpace(x.Key) || x.Key.Length > 256 || string.IsNullOrWhiteSpace(x.Value.PayloadHash)))
                throw new InvalidDataException("Invalid world resource collection state.");
            if (worldResources.Active is { } activeFlight && (activeFlight.FlightId == Guid.Empty || string.IsNullOrWhiteSpace(activeFlight.NodeId) || activeFlight.StartedAtUtc.Offset != TimeSpan.Zero || activeFlight.EndsAtUtc.Offset != TimeSpan.Zero || activeFlight.EndsAtUtc < activeFlight.StartedAtUtc || activeFlight.Revision != worldResources.Revision || string.IsNullOrWhiteSpace(activeFlight.LaunchIdempotencyKey) || activeFlight.LaunchIdempotencyKey.Length > 256 || string.IsNullOrWhiteSpace(activeFlight.PayloadHash)))
                throw new InvalidDataException("Invalid world resource collection active flight.");
            if (worldResources.ClaimReceipts is { } worldClaimReceipts && (worldClaimReceipts.Count > 4096 || worldClaimReceipts.Any(x => string.IsNullOrWhiteSpace(x.Key) || x.Key.Length > 256 || x.Value is null || x.Value.PlayerId != state.PlayerId || x.Value.HiveId != state.HiveId || x.Value.FlightId == Guid.Empty || x.Value.CreditedAmount < 0 || x.Value.ServerTimeUtc.Offset != TimeSpan.Zero || x.Value.Revision > worldResources.Revision)))
                throw new InvalidDataException("Invalid world resource collection claim receipts.");
        }
        if (state.MilestoneEvent is { } milestone)
        {
            if (milestone.Revision < 0 || milestone.WindowStartedAtUtc.Offset != TimeSpan.Zero || milestone.WindowEndsAtUtc.Offset != TimeSpan.Zero || milestone.WindowEndsAtUtc < milestone.WindowStartedAtUtc || milestone.Receipts is null || milestone.Receipts.Count > 16 || milestone.Receipts.Any(x => string.IsNullOrWhiteSpace(x.Key) || x.Key.Length > 256 || string.IsNullOrWhiteSpace(x.Value.PayloadHash)))
                throw new InvalidDataException("Invalid hive milestone event state.");
        }
        if (state.AdminAudit is { } audit)
        {
            if (audit.Count > 4096 || audit.Any(x => x.EntryId == Guid.Empty || x.AtUtc.Offset != TimeSpan.Zero || string.IsNullOrWhiteSpace(x.Action) || x.Action.Length > 128 || string.IsNullOrWhiteSpace(x.Details) || x.Details.Length > 2048 || string.IsNullOrWhiteSpace(x.Reason) || x.Reason.Length > 512))
                throw new InvalidDataException("Invalid admin audit trail.");
        }
        if (state.RewardLedger is { } rewardLedger)
        {
            if (rewardLedger.Revision < 0 || rewardLedger.Revision > state.Revision || rewardLedger.Entries is null || rewardLedger.Events is null || rewardLedger.SettledOperationIds is null || rewardLedger.Receipts is null)
                throw new InvalidDataException("Invalid reward ledger state.");
            if (rewardLedger.Entries.Count > 512 || rewardLedger.Entries.Keys.Any(k => string.IsNullOrWhiteSpace(k) || k.Length > 128) || rewardLedger.Entries.Values.Any(v => v is null || string.IsNullOrWhiteSpace(v.RewardKey) || string.IsNullOrWhiteSpace(v.Source) || v.Source.Length > 64 || string.IsNullOrWhiteSpace(v.ResourceKey) || v.Amount <= 0 || v.Amount > 1_000_000_000_000L || v.CreditedAmount < 0 || v.CreditedAmount > v.Amount || v.GrantedAtUtc.Offset != TimeSpan.Zero || v.ClaimedAtUtc is { } claimedAt && (claimedAt.Offset != TimeSpan.Zero || !v.Claimed || claimedAt < v.GrantedAtUtc) || v.Claimed && v.ClaimedAtUtc is null || v.NotificationKey is not null && (string.IsNullOrWhiteSpace(v.NotificationKey) || v.NotificationKey.Length > 128)))
                throw new InvalidDataException("Invalid reward ledger entries.");
            if (rewardLedger.Events.Count > 64 || rewardLedger.Events.Any(e => e is null || string.IsNullOrWhiteSpace(e.EventKey) || e.EventKey.Length > 64 || string.IsNullOrWhiteSpace(e.TargetKey) || e.TargetKey.Length > 256 || e.AtUtc.Offset != TimeSpan.Zero))
                throw new InvalidDataException("Invalid reward ledger events.");
            if (rewardLedger.SettledOperationIds.Count > 512 || rewardLedger.SettledOperationIds.Any(id => string.IsNullOrWhiteSpace(id) || id.Length > 128))
                throw new InvalidDataException("Invalid reward ledger settlement markers.");
            if (rewardLedger.Receipts.Count > 256 || rewardLedger.Receipts.Any(x => string.IsNullOrWhiteSpace(x.Key) || x.Key.Length > 256 || x.Value is null || string.IsNullOrWhiteSpace(x.Value.PayloadHash)))
                throw new InvalidDataException("Invalid reward ledger receipts.");
        }
        ValidateOfflineProduction(state);
        if (state.DailyRound is { } round)
        {
            if (round.DayUtc.Offset != TimeSpan.Zero || round.DayUtc.TimeOfDay != TimeSpan.Zero || round.ClaimedAtUtc is { } claimed && (claimed.Offset != TimeSpan.Zero || claimed < round.DayUtc || claimed >= round.DayUtc.AddDays(1) || !round.CollectionReceived || !round.OperationLaunched || !round.SnapshotRead))
                throw new InvalidDataException("Invalid daily round state.");
        }
        if (state.BroodCareReceipts is { } careReceipts)
        {
            if (careReceipts.Count > 128 || careReceipts.Any(x => string.IsNullOrWhiteSpace(x.Key) || x.Key.Trim() != x.Key || x.Key.Length > 256 || x.Key.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.')) || x.Value is null || !x.Value.Succeeded || x.Value.PayloadHash is null || x.Value.PayloadHash.Length != 64 || x.Value.PayloadHash.Any(c => !(c is >= '0' and <= '9' or >= 'a' and <= 'f')) || !BroodVitalityOperationTypes.Allowed.Contains(x.Value.Type) || x.Value.OperationId == Guid.Empty || x.Value.RevisionBefore < 0 || x.Value.RevisionAfter != x.Value.RevisionBefore + 1 || x.Value.RevisionAfter > state.Revision || x.Value.AcceptedAtUtc.Offset != TimeSpan.Zero || string.IsNullOrWhiteSpace(x.Value.Code) || x.Value.Code is not ("game.vitality_care_started" or "game.vitality_care_completed")))
                throw new InvalidDataException("Invalid brood care receipts.");
        }
        if (state.DailyRoundReceipts is { } daily)
        {
            if (daily.Count > 128 || daily.Any(x => string.IsNullOrWhiteSpace(x.Key) || x.Key.Trim()!=x.Key || x.Key.Length > 256 || x.Value is null || string.IsNullOrWhiteSpace(x.Value.PayloadHash) || x.Value.PayloadHash.Length != 64 || x.Value.PayloadHash.Any(c => !(c is >= '0' and <= '9' or >= 'a' and <= 'f')) || x.Value.DayUtc.Offset != TimeSpan.Zero || x.Value.DayUtc.TimeOfDay != TimeSpan.Zero || x.Value.AcceptedAtUtc.Offset != TimeSpan.Zero || x.Value.AcceptedAtUtc < x.Value.DayUtc || x.Value.AcceptedAtUtc >= x.Value.DayUtc.AddDays(1) || x.Value.RevisionBefore < 0 || x.Value.RevisionAfter < x.Value.RevisionBefore || x.Value.RevisionAfter > state.Revision || (x.Value.Succeeded && (x.Value.Code != "daily_round_claimed" || x.Value.RevisionAfter != x.Value.RevisionBefore + 1 || x.Value.CreditedHoney != 120 || x.Value.CreditedPollen != 60)) || (!x.Value.Succeeded && (x.Value.CreditedHoney != 0 || x.Value.CreditedPollen != 0 || !new[]{"daily_round_day_changed","invalid_request","revision_conflict","daily_round_incomplete","daily_round_already_claimed","storage_capacity_insufficient","idempotency_conflict"}.Contains(x.Value.Code,StringComparer.Ordinal))) || x.Value.Code.Length > 64)) throw new InvalidDataException("Invalid daily round receipts.");
        }
        return state with
        {
            ModelVersion = CurrentModelVersion,
            Resources = state.Resources ?? new(),
            BuildingLevels = state.BuildingLevels ?? new(),
            Operations = state.Operations ?? [],
            Receipts = state.Receipts ?? new(),
            DailyRoundReceipts = state.DailyRoundReceipts ?? new Dictionary<string, HiveDailyRoundStoredReceipt>(StringComparer.Ordinal),
            BroodCareReceipts = state.BroodCareReceipts ?? new Dictionary<string, BroodCareStoredReceipt>(StringComparer.Ordinal),
            Rewards = state.Rewards ?? new(),
            SpeedUps = state.SpeedUps ?? new Dictionary<string, int>(StringComparer.Ordinal),
            RewardLedger = state.RewardLedger is { } ledger
                ? ledger with
                {
                    Entries = ledger.Entries ?? new Dictionary<string, RewardLedgerEntry>(StringComparer.Ordinal),
                    Events = ledger.Events ?? new List<RewardLedgerEvent>(),
                    SettledOperationIds = ledger.SettledOperationIds ?? new HashSet<string>(StringComparer.Ordinal),
                    Receipts = ledger.Receipts ?? new Dictionary<string, IdempotencyReceipt>(StringComparer.Ordinal)
                }
                : new(0, new Dictionary<string, RewardLedgerEntry>(StringComparer.Ordinal), new List<RewardLedgerEvent>(), new HashSet<string>(StringComparer.Ordinal), new Dictionary<string, IdempotencyReceipt>(StringComparer.Ordinal)),
            InstallationComplete = state.ModelVersion < 5 ? false : state.InstallationComplete,
            HivePerimeterSortie = state.HivePerimeterSortie is { } perimeter ? perimeter with { ClaimReceipts = perimeter.ClaimReceipts ?? new Dictionary<string, HivePerimeterClaimReceipt>(StringComparer.Ordinal) } : null,
            ChampionBees = state.ChampionBees is { } championBees
                ? championBees with { Levels = championBees.Levels ?? new(StringComparer.Ordinal), AssignedBeeIds = championBees.AssignedBeeIds ?? new() }
                : new(new Dictionary<string, int>(StringComparer.Ordinal), new List<string>()),
            TroopTierProgress = state.TroopTierProgress is { } troopTiers
                ? troopTiers with { Tiers = troopTiers.Tiers ?? new(StringComparer.Ordinal) }
                : new(new Dictionary<string, int>(StringComparer.Ordinal)),
            Vip = state.Vip is { } vip ? vip with { LifetimePoints = Math.Max(0, vip.LifetimePoints) } : new(0)
        };
    }

    private static void ValidateOfflineProduction(PlayerHiveState state)
    {
        HiveOfflineProductionState? production = state.OfflineProduction;
        if (production is null) return;
        if (production.ProductionAsOfUtc.Offset != TimeSpan.Zero || production.Revision < 0 || production.Revision > state.Revision || production.PendingByBuilding is null || production.PendingByBuilding.Count > 3 || production.Receipts is null || production.Receipts.Count > 512)
            throw new InvalidDataException("Invalid offline production state.");
        string[] allowed = ["honey_storage", "wax_workshop", "warehouse_cells"];
        if (production.PendingByBuilding.Keys.Any(k => !allowed.Contains(k, StringComparer.Ordinal)) || production.PendingByBuilding.Values.Any(v => v < 0 || v > 1_000_000_000m)) throw new InvalidDataException("Invalid offline production pending.");
        foreach ((string key, OfflineProductionStoredReceipt receipt) in production.Receipts)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Length > 256 || receipt is null || string.IsNullOrEmpty(receipt.PayloadHash) || receipt.PayloadHash.Length != 64 || receipt.PayloadHash.Any(c => !(c is >= '0' and <= '9' or >= 'a' and <= 'f')) || receipt.AcceptedAtUtc.Offset != TimeSpan.Zero || receipt.Response is null || receipt.Response.Receipt is null || receipt.Response.Snapshot is null)
                throw new InvalidDataException("Invalid offline production receipt.");
            OfflineProductionReceipt r = receipt.Response.Receipt;
            string? expectedResource = ExpectedResource(r.BuildingKey);
            if (r.PlayerId != state.PlayerId || r.HiveId != state.HiveId || r.IdempotencyKey != key || expectedResource is null || r.ResourceKey != expectedResource || r.ServerTimeUtc.Offset != TimeSpan.Zero || r.ProductionRevision <= 0 || r.ProductionRevision > production.Revision || r.CreditedAmount is < 1 or > 1_000_000_000 || r.RemainingPending < 0 || r.RemainingPending > 1_000_000_000m || !ValidBalance(r.ResultingBalance) || receipt.AcceptedAtUtc != r.ServerTimeUtc)
                throw new InvalidDataException("Invalid offline production receipt scope.");
            OfflineProductionReadSnapshot snapshot = receipt.Response.Snapshot;
            if (snapshot.PlayerId != state.PlayerId || snapshot.HiveId != state.HiveId || snapshot.ContractVersion != HiveOfflineProductionService.ContractVersion || !ValidCatalogToken(snapshot.CatalogVersion) || snapshot.ProductionRevision != r.ProductionRevision || snapshot.ServerTimeUtc != r.ServerTimeUtc || snapshot.ProductionAsOfUtc.Offset != TimeSpan.Zero || snapshot.ProductionAsOfUtc > snapshot.ServerTimeUtc || snapshot.MaxRecognizedDuration <= TimeSpan.Zero || snapshot.MaxRecognizedDuration > TimeSpan.FromDays(7) || snapshot.Lines is null || snapshot.Balances is null || snapshot.Lines.Count != 3 || snapshot.Balances.Count != 3)
                throw new InvalidDataException("Invalid offline production snapshot envelope.");
            string[] buildings = ["honey_storage", "wax_workshop", "warehouse_cells"];
            if (!snapshot.Balances.Keys.OrderBy(x => x).SequenceEqual(new[] { "honey", "pollen", "wax" }) || snapshot.Lines.Select(x => x.BuildingKey).Distinct(StringComparer.Ordinal).Count() != 3 || snapshot.Lines.Select(x => x.ResourceKey).Distinct(StringComparer.Ordinal).Count() != 3 || snapshot.Lines.Any(x => ExpectedResource(x.BuildingKey) != x.ResourceKey || x.PendingAmount < 0 || x.PendingAmount > 1_000_000_000m || x.HourlyRate <= 0 || x.HourlyRate > 1_000_000m || x.Capacity <= 0 || x.Capacity > 1_000_000_000 || x.CollectableWholeUnits < 0 || x.CollectableWholeUnits > decimal.ToInt64(decimal.Floor(x.PendingAmount)) || !snapshot.Balances.TryGetValue(x.ResourceKey, out ResourceBalance? b) || x.CollectableWholeUnits > Math.Max(0, b.Capacity - b.Amount)) || snapshot.Lines.Any(x => !buildings.Contains(x.BuildingKey, StringComparer.Ordinal)))
                throw new InvalidDataException("Invalid offline production snapshot lines.");
            if (!snapshot.Balances.Keys.OrderBy(x => x).SequenceEqual(new[] { "honey", "pollen", "wax" }) || snapshot.Balances.Values.Any(x => !ValidBalance(x)))
                throw new InvalidDataException("Invalid offline production balances.");
            OfflineProductionLine line = snapshot.Lines.Single(x => x.BuildingKey == r.BuildingKey);
            if (line.PendingAmount != r.RemainingPending || !snapshot.Balances[r.ResourceKey].Equals(r.ResultingBalance))
                throw new InvalidDataException("Offline production receipt and snapshot diverge.");
        }
    }

    private static string? ExpectedResource(string building) => building switch { "honey_storage" => "honey", "wax_workshop" => "wax", "warehouse_cells" => "pollen", _ => null };
    private static bool ValidBalance(ResourceBalance balance) => balance is not null && balance.Amount >= 0 && balance.Capacity >= balance.Amount && balance.Capacity <= 1_000_000_000;
    private static bool ValidCatalogToken(string value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 64 && value.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '.' or '_' or '-');

    private static long SafeSum(IEnumerable<long> values)
    {
        try { return checked(values.Sum()); }
        catch (OverflowException) { throw new InvalidDataException("Numeric overflow in squad reservation state."); }
    }
}
