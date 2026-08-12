using System.Security.Cryptography;
using System.Text;

namespace BeeKingdom.HiveOperations;

public sealed class HiveOperationService(IHiveStateRepository repository, IServerClock clock, IEnumerable<BuildingOperationDefinition> definitions, HiveOperationDiagnostics? diagnostics = null, IEnumerable<QueuedOperationDefinition>? queuedDefinitions = null, bool dailyRoundEnabled = false)
{
    public const string WorkshopQualificationStep = "chapter4.upgrade_batch_qualification";
    public const string WorkshopApplicationReadyStep = "chapter4.upgrade_application_ready";
    public const string WorkshopProductionSpecialization = "production";
    public const string WorkshopStorageSpecialization = "storage";
    public const string HoneyReserveFoundationChoice = "honey_reserve";
    public const string MixedFoundationChoice = "mixed_foundation";
    private readonly Dictionary<(string, int), BuildingOperationDefinition> _definitions = definitions.ToDictionary(x => (x.BuildingKey, x.FromLevel));
    private readonly Dictionary<string, QueuedOperationDefinition> _queuedDefinitions = (queuedDefinitions ?? []).ToDictionary(x => x.OperationKey, StringComparer.Ordinal);

    public async Task<PlayerHiveState?> ReadAsync(Guid playerId, Guid hiveId, CancellationToken ct = default)
    {
        if (await repository.ReadAsync(playerId, hiveId, ct) is null) return null;
        return await repository.ExecuteAtomicallyAsync(playerId, hiveId, Reconcile, ct);
    }

    public async Task<PlayerHiveState> EnsureAsync(Guid playerId, Guid hiveId, CancellationToken ct = default)
    {
        return await repository.ExecuteAtomicallyAsync(playerId, hiveId, Reconcile, ct);
    }

    public Task<HiveCommandResult> StartAsync(StartBuildingOperationCommand command, CancellationToken ct = default)
    {
        string payloadHash = Hash($"start|{command.PlayerId}|{command.HiveId}|{command.BuildingKey}|{command.ExpectedLevel}");
        HiveCommandResult? result = null;
        return Execute();

        async Task<HiveCommandResult> Execute()
        {
            await repository.ExecuteAtomicallyAsync(command.PlayerId, command.HiveId, state =>
            {
                state = Reconcile(state);
                if (TryReplay(state, command.IdempotencyKey, payloadHash, out result)) return state;
                if (state.Revision != command.ExpectedRevision) return Record(state, command.IdempotencyKey, payloadHash, Result(false, "revision_conflict", state), out result);
                int currentLevel = state.BuildingLevels.GetValueOrDefault(command.BuildingKey);
                if (currentLevel != command.ExpectedLevel || !_definitions.TryGetValue((command.BuildingKey, currentLevel), out BuildingOperationDefinition? definition))
                    return Record(state, command.IdempotencyKey, payloadHash, Result(false, "invalid_building_level", state), out result);
                if (state.Operations.Any(x => x.BuildingKey == command.BuildingKey && x.Status != HiveOperationStatus.Collected))
                    return Record(state, command.IdempotencyKey, payloadHash, Result(false, "building_busy", state), out result);
                if (definition.Costs.Any(cost => !state.Resources.TryGetValue(cost.Key, out ResourceBalance? balance) || balance.Amount < cost.Value))
                    return Record(state, command.IdempotencyKey, payloadHash, Result(false, "insufficient_resources", state), out result);

                Dictionary<string, ResourceBalance> resources = new(state.Resources);
                foreach ((string key, long amount) in definition.Costs) resources[key] = resources[key] with { Amount = resources[key].Amount - amount };
                DateTimeOffset now = clock.UtcNow;
                HiveOperation operation = new(Guid.NewGuid(), definition.BuildingKey, definition.FromLevel, definition.ToLevel, now, now + definition.Duration, HiveOperationStatus.Running, definition.ProducedResourceKey, definition.ProducedAmount, null);
                PlayerHiveState updated = state with { Revision = state.Revision + 1, Resources = resources, Operations = [.. state.Operations, operation] };
                if (dailyRoundEnabled) updated = HiveDailyRoundFacts.ApplyFreshFact(updated, clock.UtcNow, HiveDailyRoundFact.OperationLaunched, false);
                return Record(updated, command.IdempotencyKey, payloadHash, Result(true, "started", updated, operation.OperationId), out result);
            }, ct);
            return result!;
        }
    }

    public Task<HiveCommandResult> CollectAsync(CollectBuildingOperationCommand command, CancellationToken ct = default)
    {
        string payloadHash = Hash($"collect|{command.PlayerId}|{command.HiveId}|{command.OperationId}");
        HiveCommandResult? result = null;
        return Execute();

        async Task<HiveCommandResult> Execute()
        {
            await repository.ExecuteAtomicallyAsync(command.PlayerId, command.HiveId, state =>
            {
                state = Reconcile(state);
                if (TryReplay(state, command.IdempotencyKey, payloadHash, out result)) return state;
                if (state.Revision != command.ExpectedRevision) return Record(state, command.IdempotencyKey, payloadHash, Result(false, "revision_conflict", state), out result);
                int index = state.Operations.FindIndex(x => x.OperationId == command.OperationId);
                if (index < 0) return Record(state, command.IdempotencyKey, payloadHash, Result(false, "operation_not_found", state), out result);
                HiveOperation operation = state.Operations[index];
                if (operation.Status != HiveOperationStatus.AwaitingCollection) return Record(state, command.IdempotencyKey, payloadHash, Result(false, operation.Status == HiveOperationStatus.Collected ? "already_collected" : "not_ready", state), out result);
                ResourceBalance balance = state.Resources.GetValueOrDefault(operation.ProducedResourceKey, new ResourceBalance(0, 0));
                long credited = Math.Min(operation.ProducedAmount, Math.Max(0, balance.Capacity - balance.Amount));
                if (credited <= 0) return Record(state, command.IdempotencyKey, payloadHash, Result(false, "storage_full", state), out result);
                Dictionary<string, ResourceBalance> resources = new(state.Resources) { [operation.ProducedResourceKey] = balance with { Amount = balance.Amount + credited } };
                List<HiveOperation> operations = [.. state.Operations];
                operations[index] = operation with { Status = HiveOperationStatus.Collected, CollectedAtUtc = clock.UtcNow };
                Dictionary<string, int> levels = new(state.BuildingLevels);
                if (operation.Kind == HiveOperationKind.BuildingUpgrade) levels[operation.BuildingKey] = operation.ToLevel;
                PlayerHiveState updated = state with { Revision = state.Revision + 1, Resources = resources, BuildingLevels = levels, Operations = operations };
                return Record(updated, command.IdempotencyKey, payloadHash, Result(true, credited == operation.ProducedAmount ? "collected" : "collected_capacity_limited", updated, operation.OperationId), out result);
            }, ct);
            return result!;
        }
    }

    public Task<HiveCommandResult> StartQueuedOperationAsync(StartQueuedOperationCommand command, CancellationToken ct = default)
    {
        string payloadHash = Hash($"queue|{command.PlayerId}|{command.HiveId}|{command.OperationKey}");
        HiveCommandResult? result = null;
        return Execute();
        async Task<HiveCommandResult> Execute()
        {
            await repository.ExecuteAtomicallyAsync(command.PlayerId, command.HiveId, state =>
            {
                state = Reconcile(state);
                if (TryReplay(state, command.IdempotencyKey, payloadHash, out result)) return state;
                if (state.Revision != command.ExpectedRevision) return Record(state, command.IdempotencyKey, payloadHash, Result(false, "revision_conflict", state), out result);
                if (!_queuedDefinitions.TryGetValue(command.OperationKey, out QueuedOperationDefinition? definition)) return Record(state, command.IdempotencyKey, payloadHash, Result(false, "operation_not_found", state), out result);
                if (state.Operations.Any(x => x.Kind == definition.Kind && x.Status != HiveOperationStatus.Collected)) return Record(state, command.IdempotencyKey, payloadHash, Result(false, "queue_busy", state), out result);
                if (definition.Costs.Any(cost => !state.Resources.TryGetValue(cost.Key, out ResourceBalance? balance) || balance.Amount < cost.Value)) return Record(state, command.IdempotencyKey, payloadHash, Result(false, "insufficient_resources", state), out result);
                Dictionary<string, ResourceBalance> resources = new(state.Resources);
                foreach ((string key, long amount) in definition.Costs) resources[key] = resources[key] with { Amount = resources[key].Amount - amount };
                DateTimeOffset now = clock.UtcNow;
                HiveOperation operation = new(Guid.NewGuid(), definition.TargetKey, 0, 0, now, now + definition.Duration, HiveOperationStatus.Running, definition.ResultKey, definition.ResultAmount, null, definition.Kind);
                PlayerHiveState updated = state with { Revision = state.Revision + 1, Resources = resources, Operations = [.. state.Operations, operation] };
                return Record(updated, command.IdempotencyKey, payloadHash, Result(true, "started", updated, operation.OperationId), out result);
            }, ct);
            return result!;
        }
    }

    public Task<HiveCommandResult> SaveTutorialProgressAsync(SaveTutorialProgressCommand command, CancellationToken ct = default)
    {
        string payloadHash = Hash($"tutorial|{command.PlayerId}|{command.HiveId}|{command.ChapterKey}|{command.SafeResumeStepKey}|{command.LastObservedStepKey}");
        HiveCommandResult? result = null;
        return Execute();
        async Task<HiveCommandResult> Execute()
        {
            await repository.ExecuteAtomicallyAsync(command.PlayerId, command.HiveId, state =>
            {
                state = Reconcile(state);
                if (TryReplay(state, command.IdempotencyKey, payloadHash, out result)) return state;
                if (state.Revision != command.ExpectedRevision) return Record(state, command.IdempotencyKey, payloadHash, Result(false, "revision_conflict", state), out result);
                PlayerHiveState updated = state with { Revision = state.Revision + 1, Tutorial = new(command.ChapterKey, command.SafeResumeStepKey, command.LastObservedStepKey, clock.UtcNow) };
                return Record(updated, command.IdempotencyKey, payloadHash, Result(true, "tutorial_saved", updated), out result);
            }, ct);
            return result!;
        }
    }

    public Task<HiveCommandResult> ClaimRewardAsync(ClaimRewardCommand command, CancellationToken ct = default)
    {
        string payloadHash = Hash($"reward|{command.PlayerId}|{command.HiveId}|{command.RewardKey}");
        HiveCommandResult? result = null;
        return Execute();
        async Task<HiveCommandResult> Execute()
        {
            await repository.ExecuteAtomicallyAsync(command.PlayerId, command.HiveId, state =>
            {
                state = Reconcile(state);
                if (TryReplay(state, command.IdempotencyKey, payloadHash, out result)) return state;
                if (state.Revision != command.ExpectedRevision) return Record(state, command.IdempotencyKey, payloadHash, Result(false, "revision_conflict", state), out result);
                if (!state.Rewards!.TryGetValue(command.RewardKey, out RewardState? reward)) return Record(state, command.IdempotencyKey, payloadHash, Result(false, "reward_not_found", state), out result);
                if (reward.Claimed) return Record(state, command.IdempotencyKey, payloadHash, Result(false, "reward_already_claimed", state), out result);
                ResourceBalance balance = state.Resources.GetValueOrDefault(reward.ResourceKey, new(0, 0));
                long credited = Math.Min(reward.Amount, Math.Max(0, balance.Capacity - balance.Amount));
                if (credited <= 0) return Record(state, command.IdempotencyKey, payloadHash, Result(false, "storage_full", state), out result);
                DateTimeOffset now = clock.UtcNow;
                Dictionary<string, ResourceBalance> resources = new(state.Resources) { [reward.ResourceKey] = balance with { Amount = balance.Amount + credited } };
                Dictionary<string, RewardState> rewards = new(state.Rewards) { [reward.RewardKey] = reward with { Claimed = true, ClaimedAtUtc = now } };
                PlayerHiveState updated = state with { Revision = state.Revision + 1, Resources = resources, Rewards = rewards };
                if (state.RewardLedger is { } ledger && ledger.Entries.TryGetValue(reward.RewardKey, out RewardLedgerEntry? entry))
                {
                    Dictionary<string, RewardLedgerEntry> entries = new(ledger.Entries, StringComparer.Ordinal)
                    {
                        [reward.RewardKey] = entry with { Claimed = true, CreditedAmount = credited, ClaimedAtUtc = now }
                    };
                    List<RewardLedgerEvent> events = [.. ledger.Events, new RewardLedgerEvent(RewardLedgerService.EventRewardClaimed, reward.RewardKey, now)];
                    if (events.Count > 64) events.RemoveAt(0);
                    updated = updated with { RewardLedger = ledger with { Revision = ledger.Revision + 1, Entries = entries, Events = events } };
                }
                return Record(updated, command.IdempotencyKey, payloadHash, Result(true, credited == reward.Amount ? "reward_claimed" : "reward_claimed_capacity_limited", updated), out result);
            }, ct);
            return result!;
        }
    }

    public static readonly IReadOnlyList<string> Chapter1CertificationSteps = ["tutorial_intro_acknowledged", "hive_surface_acknowledged", "tutorial_sequence_completed"];

    public Task<HiveCommandResult> CertifyChapter1StepAsync(CertifyChapter1StepCommand command, CancellationToken ct = default)
    {
        string step = command.StepKey?.Trim() ?? string.Empty;
        string payloadHash = Hash($"chapter1-certify|{command.PlayerId}|{command.HiveId}|{step}");
        HiveCommandResult? result = null;
        return Execute();
        async Task<HiveCommandResult> Execute()
        {
            await repository.ExecuteAtomicallyAsync(command.PlayerId, command.HiveId, state =>
            {
                state = Reconcile(state);
                if (TryReplay(state, command.IdempotencyKey, payloadHash, out result)) return state;
                if (state.Revision != command.ExpectedRevision) return Record(state, command.IdempotencyKey, payloadHash, Result(false, "revision_conflict", state), out result);
                int current = state.Chapter1Certification is null ? -1 : Chapter1CertificationSteps.ToList().IndexOf(state.Chapter1Certification.StepKey);
                int requested = Chapter1CertificationSteps.ToList().IndexOf(step);
                if (requested < 0) return Record(state, command.IdempotencyKey, payloadHash, Result(false, "invalid_step", state), out result);
                if (requested != current + 1) return Record(state, command.IdempotencyKey, payloadHash, Result(false, requested <= current ? "step_already_certified" : "step_order_required", state), out result);
                DateTimeOffset now = clock.UtcNow;
                string? proof = requested == Chapter1CertificationSteps.Count - 1 ? Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant() : null;
                PlayerHiveState updated = state with { Revision = state.Revision + 1, Chapter1Certification = new(step, state.Revision + 1, now, proof), InstallationComplete = false };
                return Record(updated, command.IdempotencyKey, payloadHash, Result(true, proof is null ? "step_certified" : "chapter1_certified", updated), out result);
            }, ct);
            return result!;
        }
    }

    public Task<HiveCommandResult> ClaimFoundationDotationAsync(ClaimFoundationDotationCommand command, CancellationToken ct = default)
    {
        string choice = command.Choice?.Trim() ?? string.Empty;
        string payloadHash = Hash($"chapter01-foundation|{command.PlayerId}|{command.HiveId}|{choice}");
        HiveCommandResult? result = null;
        return Execute();

        async Task<HiveCommandResult> Execute()
        {
            await repository.ExecuteAtomicallyAsync(command.PlayerId, command.HiveId, state =>
            {
                state = Reconcile(state);
                if (TryReplay(state, command.IdempotencyKey, payloadHash, out result)) return state;
                if (state.Revision != command.ExpectedRevision)
                    return Record(state, command.IdempotencyKey, payloadHash, Result(false, "revision_conflict", state), out result);
                if (choice is not (HoneyReserveFoundationChoice or MixedFoundationChoice))
                    return Record(state, command.IdempotencyKey, payloadHash, Result(false, "invalid_foundation_choice", state), out result);
                if (!state.InstallationComplete)
                    return Record(state, command.IdempotencyKey, payloadHash, Result(false, "installation_incomplete", state), out result);
                if (state.FoundationDotation is not null)
                    return Record(state, command.IdempotencyKey, payloadHash, Result(false, "foundation_already_claimed", state), out result);

                (long honey, long pollen) = choice == HoneyReserveFoundationChoice ? (250, 0) : (170, 80);
                ResourceBalance honeyBalance = state.Resources.GetValueOrDefault("honey", new(0, 0));
                ResourceBalance pollenBalance = state.Resources.GetValueOrDefault("pollen", new(0, 0));
                if (honeyBalance.Amount + honey > honeyBalance.Capacity || pollenBalance.Amount + pollen > pollenBalance.Capacity)
                    return Record(state, command.IdempotencyKey, payloadHash, Result(false, "storage_capacity_insufficient", state), out result);

                Dictionary<string, ResourceBalance> resources = new(state.Resources)
                {
                    ["honey"] = honeyBalance with { Amount = honeyBalance.Amount + honey },
                    ["pollen"] = pollenBalance with { Amount = pollenBalance.Amount + pollen }
                };
                FoundationDotationState foundation = new(choice, honey, pollen, Guid.NewGuid().ToString("N"), clock.UtcNow);
                PlayerHiveState updated = state with
                {
                    Revision = state.Revision + 1,
                    Resources = resources,
                    FoundationDotation = foundation
                };
                return Record(updated, command.IdempotencyKey, payloadHash, Result(true, "foundation_claimed", updated), out result);
            }, ct);
            return result!;
        }
    }

    public Task<WorkshopBatchQualificationResult> QualifyWorkshopBatchAsync(QualifyWorkshopBatchCommand command, CancellationToken ct = default)
    {
        string answer = command.Answer?.Trim() ?? string.Empty;
        string payloadHash = Hash($"chapter4-qualification|{command.PlayerId}|{command.HiveId}|{command.ExpectedRevision}|{answer}");
        WorkshopBatchQualificationResult? result = null;
        return Execute();

        async Task<WorkshopBatchQualificationResult> Execute()
        {
            await repository.ExecuteAtomicallyAsync(command.PlayerId, command.HiveId, state =>
            {
                state = Reconcile(state);
                if (state.Receipts.TryGetValue(command.IdempotencyKey, out IdempotencyReceipt? receipt))
                {
                    if (receipt.PayloadHash != payloadHash)
                    {
                        result = new(false, "idempotency_conflict", "", "", answer, state.Revision, state.Revision, receipt.AcceptedAtUtc ?? receipt.CreatedAtUtc, state);
                        return state;
                    }
                    result = new(receipt.Succeeded, receipt.Code, receipt.PreviousStep ?? "chapter4.upgrade_batch_qualification", receipt.ResultingStep ?? "chapter4.upgrade_batch_qualification", receipt.Answer ?? answer, receipt.RevisionBefore ?? state.Revision, receipt.RevisionAfter ?? state.Revision, receipt.AcceptedAtUtc ?? receipt.CreatedAtUtc, state);
                    return state;
                }
                long before = state.Revision;
                const string expectedStep = WorkshopQualificationStep;
                const string nextStep = WorkshopApplicationReadyStep;
                WorkshopBatchQualificationState? qualification = state.WorkshopBatchQualification;
                if (command.ExpectedRevision < 0 || answer.Length is < 1 or > 32 || string.IsNullOrWhiteSpace(command.IdempotencyKey) || command.IdempotencyKey.Length > 256)
                    return RecordQualification(state, command.IdempotencyKey ?? string.Empty, payloadHash, false, "invalid_request", expectedStep, expectedStep, answer, before, before);
                if (before != command.ExpectedRevision)
                    return RecordQualification(state, command.IdempotencyKey, payloadHash, false, "revision_conflict", expectedStep, expectedStep, answer, before, before);
                if (qualification is null || qualification.StepKey != expectedStep || qualification.Revision != before || qualification.CollectedAmount <= 0)
                    return RecordQualification(state, command.IdempotencyKey, payloadHash, false, "tutorial_precondition_failed", expectedStep, expectedStep, answer, before, before);
                string expectedAnswer = qualification.Specialization switch { WorkshopProductionSpecialization => "heat", WorkshopStorageSpecialization => "load", _ => string.Empty };
                if (expectedAnswer.Length == 0)
                    return RecordQualification(state, command.IdempotencyKey, payloadHash, false, "tutorial_precondition_failed", expectedStep, expectedStep, answer, before, before);
                DateTimeOffset now = clock.UtcNow;
                if (!string.Equals(answer, expectedAnswer, StringComparison.Ordinal))
                    return RecordQualification(state, command.IdempotencyKey, payloadHash, false, "tutorial_answer_incorrect", expectedStep, expectedStep, answer, before, before, now);
                PlayerHiveState updated = state with
                {
                    Revision = before + 1,
                    WorkshopBatchQualification = qualification with { StepKey = nextStep, Revision = before + 1 }
                };
                return RecordQualification(updated, command.IdempotencyKey, payloadHash, true, "tutorial_advanced", expectedStep, nextStep, answer, before, before + 1, now);
            }, ct);
            return result!;
        }

        PlayerHiveState RecordQualification(PlayerHiveState state, string key, string hash, bool succeeded, string code, string previous, string next, string submittedAnswer, long before, long after, DateTimeOffset? acceptedAt = null)
        {
            DateTimeOffset timestamp = acceptedAt ?? clock.UtcNow;
            Dictionary<string, IdempotencyReceipt> receipts = new(state.Receipts)
            {
                [key] = new(hash, succeeded, code, null, timestamp, before, after, previous, next, submittedAnswer, timestamp)
            };
            PlayerHiveState recorded = state with { Receipts = receipts };
            result = new(succeeded, code, previous, next, submittedAnswer, before, after, timestamp, recorded);
            return recorded;
        }
    }

    // Branche 1 (Economie) de Docs/Product/BeeKingdom_ResearchTree_Design.md : trois voies de
    // paliers I->II->III (miel/cire/pollen) qui restent utiles en parallele, convergeant vers
    // une technologie finale qui exige les trois paliers III. Les bonus de paliers superieurs
    // s'additionnent a ceux des paliers deja completes (voir SumResearchBps dans
    // HiveOfflineProductionService) plutot que de les remplacer.
    public static readonly IReadOnlyDictionary<string, ResearchCatalogEntry> ResearchCatalog =
        new Dictionary<string, ResearchCatalogEntry>(StringComparer.Ordinal)
        {
            ["foraging_routes_i"] = new(
                new Dictionary<string, long> { ["honey"] = 240, ["pollen"] = 90 },
                TimeSpan.FromSeconds(120), new ResearchEffects(200, 0, 0, 0, 0, 0), Array.Empty<string>()),
            ["foraging_routes_ii"] = new(
                new Dictionary<string, long> { ["honey"] = 900, ["pollen"] = 500 },
                TimeSpan.FromSeconds(360), new ResearchEffects(500, 0, 0, 0, 0, 0), new[] { "foraging_routes_i" }),
            ["foraging_routes_iii"] = new(
                new Dictionary<string, long> { ["honey"] = 2400, ["pollen"] = 1400 },
                TimeSpan.FromSeconds(720), new ResearchEffects(800, 0, 0, 0, 0, 0), new[] { "foraging_routes_ii" }),
            ["tempered_combs_i"] = new(
                new Dictionary<string, long> { ["honey"] = 180, ["pollen"] = 120 },
                TimeSpan.FromSeconds(120), new ResearchEffects(0, 500, 0, 0, 0, 0), Array.Empty<string>()),
            ["tempered_combs_ii"] = new(
                new Dictionary<string, long> { ["honey"] = 900, ["pollen"] = 500 },
                TimeSpan.FromSeconds(360), new ResearchEffects(0, 800, 300, 0, 0, 0), new[] { "tempered_combs_i" }),
            ["tempered_combs_iii"] = new(
                new Dictionary<string, long> { ["honey"] = 2400, ["pollen"] = 1400 },
                TimeSpan.FromSeconds(720), new ResearchEffects(0, 1000, 500, 0, 0, 0), new[] { "tempered_combs_ii" }),
            ["pollen_sorting_i"] = new(
                new Dictionary<string, long> { ["honey"] = 200, ["wax"] = 150 },
                TimeSpan.FromSeconds(120), new ResearchEffects(0, 0, 0, 500, 0, 0), Array.Empty<string>()),
            ["pollen_sorting_ii"] = new(
                new Dictionary<string, long> { ["honey"] = 800, ["wax"] = 600 },
                TimeSpan.FromSeconds(360), new ResearchEffects(0, 0, 0, 800, 0, 0), new[] { "pollen_sorting_i" }),
            ["pollen_sorting_iii"] = new(
                new Dictionary<string, long> { ["honey"] = 2200, ["wax"] = 1600 },
                TimeSpan.FromSeconds(720), new ResearchEffects(0, 0, 0, 1000, 500, 0), new[] { "pollen_sorting_ii" }),
            ["sealed_reserves"] = new(
                new Dictionary<string, long> { ["honey"] = 6000, ["wax"] = 4000, ["pollen"] = 4000 },
                TimeSpan.FromSeconds(1200), new ResearchEffects(0, 0, 0, 0, 0, 1000),
                new[] { "foraging_routes_iii", "tempered_combs_iii", "pollen_sorting_iii" }),
        };

    public Task<DailyRoundCommandResult> RecordCollectionReceiptAsync(Guid playerId, Guid hiveId, Guid operationId, CancellationToken ct = default) =>
        MarkDailyRoundAsync(playerId, hiveId, "collection", operationId, ct);
    public Task<DailyRoundCommandResult> RecordOperationLaunchAsync(Guid playerId, Guid hiveId, Guid operationId, CancellationToken ct = default) =>
        MarkDailyRoundAsync(playerId, hiveId, "operation", operationId, ct);
    public Task<DailyRoundCommandResult> RecordSnapshotReadAsync(Guid playerId, Guid hiveId, CancellationToken ct = default) =>
        MarkDailyRoundAsync(playerId, hiveId, "snapshot", null, ct);

    private Task<DailyRoundCommandResult> MarkDailyRoundAsync(Guid playerId, Guid hiveId, string kind, Guid? operationId, CancellationToken ct)
    {
        DailyRoundCommandResult? result = null;
        return Execute();
        async Task<DailyRoundCommandResult> Execute()
        {
            await repository.ExecuteAtomicallyAsync(playerId, hiveId, state =>
            {
                DateTimeOffset now = clock.UtcNow;
                HiveDailyRoundState round = state.DailyRound is { } existing && existing.DayUtc.UtcDateTime.Date == now.UtcDateTime.Date
                    ? existing : new(new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero), false, false, false, null);
                if (kind == "collection" && (operationId is null || !state.Operations.Any(x => x.OperationId == operationId && x.Status == HiveOperationStatus.Collected))) return SetRoundResult(state, round, false, "collection_not_verified", out result);
                bool operationVerified = operationId is not null && state.Operations.Any(x => x.OperationId == operationId && x.Status != HiveOperationStatus.Collected && x.StartedAtUtc.UtcDateTime.Date == now.UtcDateTime.Date);
                if (!operationVerified && operationId is not null && state.Research?.ActiveOperation is { } researchOperation)
                    operationVerified = researchOperation.OperationId == operationId && researchOperation.StartedAtUtc.UtcDateTime.Date == now.UtcDateTime.Date;
                if (kind == "operation" && !operationVerified) return SetRoundResult(state, round, false, "operation_not_verified", out result);
                HiveDailyRoundState updatedRound = round with { CollectionReceived = round.CollectionReceived || kind == "collection", OperationLaunched = round.OperationLaunched || kind == "operation", SnapshotRead = round.SnapshotRead || kind == "snapshot" };
                bool changed = updatedRound != round || state.DailyRound is null || state.DailyRound.DayUtc != round.DayUtc;
                return SetRoundResult(state with { DailyRound = updatedRound, Revision = changed ? state.Revision + 1 : state.Revision }, updatedRound, true, "daily_round_recorded", out result);
            }, ct);
            return result!;
        }
    }

    public Task<DailyRoundCommandResult> ClaimDailyRoundAsync(ClaimHiveDailyRoundCommand command, CancellationToken ct = default)
    {
        string key = command.IdempotencyKey ?? string.Empty;
        string day = clock.UtcNow.UtcDateTime.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        string hash = Hash($"daily-round-claim|{command.PlayerId}|{command.HiveId}|{command.ExpectedDayUtc}|{command.ExpectedRevision}").ToLowerInvariant();
        DailyRoundCommandResult? result = null;
        return Execute();
        async Task<DailyRoundCommandResult> Execute()
        {
            await repository.ExecuteAtomicallyAsync(command.PlayerId, command.HiveId, state =>
            {
                DateTimeOffset now = clock.UtcNow; long before = state.Revision;
                if (state.DailyRoundReceipts?.TryGetValue(key, out HiveDailyRoundStoredReceipt? stored) == true)
                {
                    result = stored.PayloadHash == hash ? new(stored.Succeeded, stored.Code, stored.RevisionBefore, stored.RevisionAfter, stored.AcceptedAtUtc, state) : new(false, "idempotency_conflict", before, before, now, state); return state;
                }
                if (string.IsNullOrWhiteSpace(key) || key.Length > 256 || key.Trim() != key || command.ExpectedRevision == long.MaxValue || command.ExpectedDayUtc != day) { result = new(false, command.ExpectedDayUtc != day ? "daily_round_day_changed" : "invalid_request", before, before, now, state); return state; }
                if (before != command.ExpectedRevision) return RecordDailyClaim(state, key, hash, false, "revision_conflict", before, before, out result);
                HiveDailyRoundState? round = state.DailyRound;
                if (round is null || round.DayUtc.UtcDateTime.Date != now.UtcDateTime.Date || !round.CollectionReceived || !round.OperationLaunched || !round.SnapshotRead) return RecordDailyClaim(state, key, hash, false, "daily_round_incomplete", before, before, out result);
                if (round.ClaimedAtUtc is not null) return RecordDailyClaim(state, key, hash, false, "daily_round_already_claimed", before, before, out result);
                ResourceBalance honey = state.Resources.GetValueOrDefault("honey", new(0, 0)); ResourceBalance pollen = state.Resources.GetValueOrDefault("pollen", new(0, 0));
                if (honey.Amount + 120 > honey.Capacity || pollen.Amount + 60 > pollen.Capacity) return RecordDailyClaim(state, key, hash, false, "storage_capacity_insufficient", before, before, out result);
                Dictionary<string, ResourceBalance> resources = new(state.Resources) { ["honey"] = honey with { Amount = honey.Amount + 120 }, ["pollen"] = pollen with { Amount = pollen.Amount + 60 } };
                PlayerHiveState updated = state with { Revision = before + 1, Resources = resources, DailyRound = round with { ClaimedAtUtc = now } };
                return RecordDailyClaim(updated, key, hash, true, "daily_round_claimed", before, before + 1, out result);
            }, ct); return result!;
        }
    }

    private PlayerHiveState SetRoundResult(PlayerHiveState state, HiveDailyRoundState round, bool ok, string code, out DailyRoundCommandResult? result) { result = new(ok, code, state.Revision, state.Revision, clock.UtcNow, state); return state; }
    private PlayerHiveState RecordDailyClaim(PlayerHiveState state, string key, string hash, bool ok, string code, long before, long after, out DailyRoundCommandResult? result)
    { DateTimeOffset now = clock.UtcNow; var daily = new Dictionary<string,HiveDailyRoundStoredReceipt>(state.DailyRoundReceipts ?? new(), StringComparer.Ordinal) { [key] = new(hash,ok,new DateTimeOffset(now.UtcDateTime.Date,TimeSpan.Zero),before,after,now,ok?120:0,ok?60:0,code) }; if(daily.Count>128) { var victim=daily.Where(x=>x.Key!=key).OrderBy(x=>x.Value.AcceptedAtUtc).ThenBy(x=>x.Key,StringComparer.Ordinal).FirstOrDefault(); if(!string.IsNullOrEmpty(victim.Key)) daily.Remove(victim.Key); } var recorded = state with { DailyRoundReceipts=daily }; result = new(ok, code, before, after, now, recorded); return recorded; }

    public Task<ResearchCommandResult> StartResearchAsync(StartResearchCommand command, CancellationToken ct = default)
    {
        string id = command.ResearchId?.Trim() ?? string.Empty;
        string key = command.IdempotencyKey ?? string.Empty;
        string hash = Hash($"research-start|{command.PlayerId}|{command.HiveId}|{id}");
        ResearchCommandResult? result = null;
        return Execute();
        async Task<ResearchCommandResult> Execute()
        {
            await repository.ExecuteAtomicallyAsync(command.PlayerId, command.HiveId, state =>
            {
                state = Reconcile(state);
                if (state.Receipts.TryGetValue(key, out IdempotencyReceipt? receipt))
                {
                    result = receipt.PayloadHash == hash
                        ? new(receipt.Succeeded, receipt.Code, id, receipt.OperationId, receipt.RevisionBefore ?? state.Revision, receipt.RevisionAfter ?? state.Revision, receipt.AcceptedAtUtc ?? receipt.CreatedAtUtc, state)
                        : new(false, "idempotency_conflict", id, null, state.Revision, state.Revision, clock.UtcNow, state);
                    return state;
                }
                long before = state.Revision;
                if (!ResearchCatalog.TryGetValue(id, out ResearchCatalogEntry? definition) || command.ExpectedRevision < 0 || string.IsNullOrWhiteSpace(command.IdempotencyKey) || command.IdempotencyKey.Length > 256)
                    return RecordResearch(state, key, hash, false, "invalid_request", id, null, before, before, out result);
                if (before != command.ExpectedRevision) return RecordResearch(state, key, hash, false, "revision_conflict", id, null, before, before, out result);
                HiveResearchState research = state.Research ?? new(new Dictionary<string, ResearchCompletion>(StringComparer.Ordinal), null);
                if (research.Completed.ContainsKey(id)) return RecordResearch(state, key, hash, false, "research_already_completed", id, null, before, before, out result);
                if (research.ActiveOperation is not null) return RecordResearch(state, key, hash, false, "research_busy", id, research.ActiveOperation.OperationId, before, before, out result);
                if (definition.Prerequisites.Any(prerequisite => !research.Completed.ContainsKey(prerequisite)))
                    return RecordResearch(state, key, hash, false, "research_prerequisite_missing", id, null, before, before, out result);
                if (definition.Costs.Any(cost => state.Resources.GetValueOrDefault(cost.Key, new(0, 0)).Amount < cost.Value))
                    return RecordResearch(state, key, hash, false, "insufficient_resources", id, null, before, before, out result);
                DateTimeOffset now = clock.UtcNow;
                ResearchOperation operation = new(Guid.NewGuid(), id, now, now.Add(definition.Duration), before + 1);
                Dictionary<string, ResourceBalance> resources = new(state.Resources);
                foreach (KeyValuePair<string, long> cost in definition.Costs)
                {
                    ResourceBalance balance = resources.GetValueOrDefault(cost.Key, new(0, 0));
                    resources[cost.Key] = balance with { Amount = balance.Amount - cost.Value };
                }
                PlayerHiveState updated = state with { Revision = before + 1, Resources = resources, Research = research with { ActiveOperation = operation } };
                if (dailyRoundEnabled) updated = HiveDailyRoundFacts.ApplyFreshFact(updated, clock.UtcNow, HiveDailyRoundFact.OperationLaunched, false);
                return RecordResearch(updated, key, hash, true, "research_started", id, operation.OperationId, before, before + 1, out result);
            }, ct);
            return result!;
        }
    }

    public Task<ResearchCommandResult> CompleteResearchAsync(CompleteResearchCommand command, CancellationToken ct = default)
    {
        string key = command.IdempotencyKey ?? string.Empty;
        string hash = Hash($"research-complete|{command.PlayerId}|{command.HiveId}|{command.OperationId}");
        ResearchCommandResult? result = null;
        return Execute();
        async Task<ResearchCommandResult> Execute()
        {
            await repository.ExecuteAtomicallyAsync(command.PlayerId, command.HiveId, state =>
            {
                state = Reconcile(state);
                if (state.Receipts.TryGetValue(key, out IdempotencyReceipt? receipt))
                {
                    result = receipt.PayloadHash == hash ? new(receipt.Succeeded, receipt.Code, receipt.ResultingStep ?? string.Empty, receipt.OperationId, receipt.RevisionBefore ?? state.Revision, receipt.RevisionAfter ?? state.Revision, receipt.AcceptedAtUtc ?? receipt.CreatedAtUtc, state) : new(false, "idempotency_conflict", string.Empty, null, state.Revision, state.Revision, clock.UtcNow, state);
                    return state;
                }
                long before = state.Revision;
                if (command.ExpectedRevision < 0 || command.OperationId == Guid.Empty || string.IsNullOrWhiteSpace(command.IdempotencyKey) || command.IdempotencyKey.Length > 256)
                    return RecordResearch(state, key, hash, false, "invalid_request", string.Empty, null, before, before, out result);
                if (before != command.ExpectedRevision) return RecordResearch(state, key, hash, false, "revision_conflict", string.Empty, null, before, before, out result);
                HiveResearchState research = state.Research ?? new(new Dictionary<string, ResearchCompletion>(StringComparer.Ordinal), null);
                ResearchOperation? operation = research.ActiveOperation;
                if (operation is null || operation.OperationId != command.OperationId) return RecordResearch(state, key, hash, false, "research_not_found", string.Empty, null, before, before, out result);
                if (operation.EndsAtUtc > clock.UtcNow) return RecordResearch(state, key, hash, false, "research_not_ready", operation.ResearchId, operation.OperationId, before, before, out result);
                ResearchCatalogEntry definition = ResearchCatalog[operation.ResearchId];
                Dictionary<string, ResearchCompletion> completed = new(research.Completed, StringComparer.Ordinal)
                {
                    [operation.ResearchId] = new(operation.ResearchId, clock.UtcNow, definition.Effects)
                };
                PlayerHiveState updated = state with { Revision = before + 1, Research = new(completed, null) };
                return RecordResearch(updated, key, hash, true, "research_completed", operation.ResearchId, operation.OperationId, before, before + 1, out result);
            }, ct);
            return result!;
        }
    }

    private PlayerHiveState RecordResearch(PlayerHiveState state, string key, string hash, bool ok, string code, string researchId, Guid? operationId, long before, long after, out ResearchCommandResult? result)
    {
        DateTimeOffset now = clock.UtcNow;
        Dictionary<string, IdempotencyReceipt> receipts = new(state.Receipts) { [key] = new(hash, ok, code, operationId, now, before, after, null, researchId, null, now) };
        PlayerHiveState recorded = state with { Receipts = receipts };
        result = new(ok, code, researchId, operationId, before, after, now, recorded);
        return recorded;
    }

    private PlayerHiveState Reconcile(PlayerHiveState state)
    {
        state = HiveStateMigrator.ToCurrent(state);
        DateTimeOffset now = clock.UtcNow;
        bool changed = false;
        List<HiveOperation> operations = state.Operations.Select(x =>
        {
            if (x.Status == HiveOperationStatus.Running && x.CompletesAtUtc <= now) { changed = true; return x with { Status = HiveOperationStatus.AwaitingCollection }; }
            return x;
        }).ToList();
        return changed ? state with { Revision = state.Revision + 1, Operations = operations } : state;
    }

    private bool TryReplay(PlayerHiveState state, string key, string hash, out HiveCommandResult? result)
    {
        result = null;
        if (!state.Receipts.TryGetValue(key, out IdempotencyReceipt? receipt)) return false;
        result = receipt.PayloadHash == hash ? new(receipt.Succeeded, receipt.Code, state, receipt.OperationId) : Result(false, "idempotency_conflict", state);
        diagnostics?.RecordReplay(result.Code);
        return true;
    }

    private PlayerHiveState Record(PlayerHiveState state, string key, string hash, HiveCommandResult commandResult, out HiveCommandResult? result)
    {
        Dictionary<string, IdempotencyReceipt> receipts = new(state.Receipts) { [key] = new(hash, commandResult.Succeeded, commandResult.Code, commandResult.OperationId, clock.UtcNow) };
        PlayerHiveState recorded = state with { Receipts = receipts };
        result = commandResult with { State = recorded };
        diagnostics?.RecordResult(result.Code, result.Succeeded);
        return recorded;
    }

    private static HiveCommandResult Result(bool ok, string code, PlayerHiveState state, Guid? operationId = null) => new(ok, code, state, operationId);

    public Task<ChampionBeeCommandResult> GrantChampionBeeAsync(GrantChampionBeeCommand command, CancellationToken ct = default)
    {
        string id = command.BeeId?.Trim() ?? string.Empty;
        string hash = Hash($"champion-bee-grant|{command.PlayerId}|{command.HiveId}|{id}");
        ChampionBeeCommandResult? result = null;
        return Execute();
        async Task<ChampionBeeCommandResult> Execute()
        {
            await repository.ExecuteAtomicallyAsync(command.PlayerId, command.HiveId, state =>
            {
                state = Reconcile(state);
                if (TryReplayChampionBee(state, command.IdempotencyKey, hash, out result)) return state;
                long before = state.Revision;
                if (!ChampionBeeCatalog.Definitions.TryGetValue(id, out ChampionBeeDefinition? definition) || command.ExpectedRevision < 0 || string.IsNullOrWhiteSpace(command.IdempotencyKey) || command.IdempotencyKey.Length > 256)
                    return RecordChampionBee(state, command.IdempotencyKey, hash, false, "invalid_request", id, 0, before, before, out result);
                if (before != command.ExpectedRevision) return RecordChampionBee(state, command.IdempotencyKey, hash, false, "revision_conflict", id, 0, before, before, out result);
                ChampionBeeProgressState progress = state.ChampionBees ?? new(new Dictionary<string, int>(StringComparer.Ordinal), new List<string>());
                if (progress.Levels.ContainsKey(id)) return RecordChampionBee(state, command.IdempotencyKey, hash, false, "champion_bee_already_owned", id, progress.Levels[id], before, before, out result);
                int coeurRoyalLevel = state.BuildingLevels.GetValueOrDefault("administration_core", 1);
                if (coeurRoyalLevel < ChampionBeeCatalog.UnlockCoeurRoyalLevel(definition.Rarity))
                    return RecordChampionBee(state, command.IdempotencyKey, hash, false, "champion_bee_locked", id, 0, before, before, out result);
                Dictionary<string, int> levels = new(progress.Levels, StringComparer.Ordinal) { [id] = 1 };
                PlayerHiveState updated = state with { Revision = before + 1, ChampionBees = progress with { Levels = levels } };
                return RecordChampionBee(updated, command.IdempotencyKey, hash, true, "champion_bee_granted", id, 1, before, before + 1, out result);
            }, ct);
            return result!;
        }
    }

    public Task<ChampionBeeCommandResult> LevelUpChampionBeeAsync(LevelUpChampionBeeCommand command, CancellationToken ct = default)
    {
        string id = command.BeeId?.Trim() ?? string.Empty;
        string hash = Hash($"champion-bee-level-up|{command.PlayerId}|{command.HiveId}|{id}|{command.ExpectedRevision}");
        ChampionBeeCommandResult? result = null;
        return Execute();
        async Task<ChampionBeeCommandResult> Execute()
        {
            await repository.ExecuteAtomicallyAsync(command.PlayerId, command.HiveId, state =>
            {
                state = Reconcile(state);
                if (TryReplayChampionBee(state, command.IdempotencyKey, hash, out result)) return state;
                long before = state.Revision;
                if (!ChampionBeeCatalog.Definitions.TryGetValue(id, out ChampionBeeDefinition? definition) || command.ExpectedRevision < 0 || string.IsNullOrWhiteSpace(command.IdempotencyKey) || command.IdempotencyKey.Length > 256)
                    return RecordChampionBee(state, command.IdempotencyKey, hash, false, "invalid_request", id, 0, before, before, out result);
                if (before != command.ExpectedRevision) return RecordChampionBee(state, command.IdempotencyKey, hash, false, "revision_conflict", id, 0, before, before, out result);
                ChampionBeeProgressState progress = state.ChampionBees ?? new(new Dictionary<string, int>(StringComparer.Ordinal), new List<string>());
                if (!progress.Levels.TryGetValue(id, out int level) || level <= 0)
                    return RecordChampionBee(state, command.IdempotencyKey, hash, false, "champion_bee_not_owned", id, 0, before, before, out result);
                if (level >= ChampionBeeCatalog.MaxLevel) return RecordChampionBee(state, command.IdempotencyKey, hash, false, "champion_bee_max_level", id, level, before, before, out result);
                (long honeyCost, long pollenCost) = ChampionBeeCatalog.LevelUpCost(definition.Rarity, level);
                ResourceBalance honey = state.Resources.GetValueOrDefault("honey", new(0, 0));
                ResourceBalance pollen = state.Resources.GetValueOrDefault("pollen", new(0, 0));
                if (honey.Amount < honeyCost || pollen.Amount < pollenCost)
                    return RecordChampionBee(state, command.IdempotencyKey, hash, false, "insufficient_resources", id, level, before, before, out result);
                Dictionary<string, int> levels = new(progress.Levels, StringComparer.Ordinal) { [id] = level + 1 };
                Dictionary<string, ResourceBalance> resources = new(state.Resources)
                {
                    ["honey"] = honey with { Amount = honey.Amount - honeyCost },
                    ["pollen"] = pollen with { Amount = pollen.Amount - pollenCost }
                };
                PlayerHiveState updated = state with { Revision = before + 1, Resources = resources, ChampionBees = progress with { Levels = levels } };
                return RecordChampionBee(updated, command.IdempotencyKey, hash, true, "champion_bee_leveled_up", id, level + 1, before, before + 1, out result);
            }, ct);
            return result!;
        }
    }

    public Task<ChampionBeeCommandResult> SetChampionBeeAssignmentAsync(SetChampionBeeAssignmentCommand command, CancellationToken ct = default)
    {
        IReadOnlyList<string> requested = command.BeeIds ?? Array.Empty<string>();
        string hash = Hash($"champion-bee-assign|{command.PlayerId}|{command.HiveId}|{string.Join(',', requested)}|{command.ExpectedRevision}");
        ChampionBeeCommandResult? result = null;
        return Execute();
        async Task<ChampionBeeCommandResult> Execute()
        {
            await repository.ExecuteAtomicallyAsync(command.PlayerId, command.HiveId, state =>
            {
                state = Reconcile(state);
                if (TryReplayChampionBee(state, command.IdempotencyKey, hash, out result)) return state;
                long before = state.Revision;
                if (command.ExpectedRevision < 0 || string.IsNullOrWhiteSpace(command.IdempotencyKey) || command.IdempotencyKey.Length > 256)
                    return RecordChampionBee(state, command.IdempotencyKey, hash, false, "invalid_request", string.Empty, 0, before, before, out result);
                if (before != command.ExpectedRevision) return RecordChampionBee(state, command.IdempotencyKey, hash, false, "revision_conflict", string.Empty, 0, before, before, out result);
                ChampionBeeProgressState progress = state.ChampionBees ?? new(new Dictionary<string, int>(StringComparer.Ordinal), new List<string>());
                int coeurRoyalLevel = state.BuildingLevels.GetValueOrDefault("administration_core", 1);
                int maxAssigned = ChampionBeeCatalog.MaxAssignedForCoeurRoyalLevel(coeurRoyalLevel);
                List<string> distinct = requested.Distinct(StringComparer.Ordinal).ToList();
                if (distinct.Count > maxAssigned || distinct.Any(beeId => !progress.Levels.ContainsKey(beeId)))
                    return RecordChampionBee(state, command.IdempotencyKey, hash, false, "champion_bee_assignment_invalid", string.Empty, 0, before, before, out result);
                PlayerHiveState updated = state with { Revision = before + 1, ChampionBees = progress with { AssignedBeeIds = distinct } };
                return RecordChampionBee(updated, command.IdempotencyKey, hash, true, "champion_bee_assignment_updated", string.Empty, 0, before, before + 1, out result);
            }, ct);
            return result!;
        }
    }

    private bool TryReplayChampionBee(PlayerHiveState state, string key, string hash, out ChampionBeeCommandResult? result)
    {
        result = null;
        if (!state.Receipts.TryGetValue(key ?? string.Empty, out IdempotencyReceipt? receipt)) return false;
        ChampionBeeProgressState progress = state.ChampionBees ?? new(new Dictionary<string, int>(StringComparer.Ordinal), new List<string>());
        int replayedLevel = receipt.Answer is { } levelText && int.TryParse(levelText, out int parsedLevel) ? parsedLevel : 0;
        result = receipt.PayloadHash == hash
            ? new(receipt.Succeeded, receipt.Code, receipt.ResultingStep ?? string.Empty, replayedLevel, progress.AssignedBeeIds, receipt.RevisionBefore ?? state.Revision, receipt.RevisionAfter ?? state.Revision, receipt.AcceptedAtUtc ?? receipt.CreatedAtUtc, state)
            : new(false, "idempotency_conflict", string.Empty, 0, progress.AssignedBeeIds, state.Revision, state.Revision, clock.UtcNow, state);
        return true;
    }

    private PlayerHiveState RecordChampionBee(PlayerHiveState state, string key, string hash, bool ok, string code, string beeId, int level, long before, long after, out ChampionBeeCommandResult? result)
    {
        DateTimeOffset now = clock.UtcNow;
        Dictionary<string, IdempotencyReceipt> receipts = new(state.Receipts) { [key ?? string.Empty] = new(hash, ok, code, null, now, before, after, null, beeId, level.ToString(System.Globalization.CultureInfo.InvariantCulture), now) };
        PlayerHiveState recorded = state with { Receipts = receipts };
        IReadOnlyList<string> assigned = recorded.ChampionBees?.AssignedBeeIds ?? new List<string>();
        result = new(ok, code, beeId, level, assigned, before, after, now, recorded);
        return recorded;
    }

    public Task<TroopTierCommandResult> PromoteTroopTierAsync(PromoteTroopTierCommand command, CancellationToken ct = default)
    {
        string populationId = command.PopulationId?.Trim() ?? string.Empty;
        string hash = Hash($"troop-tier-promote|{command.PlayerId}|{command.HiveId}|{populationId}|{command.ExpectedRevision}");
        TroopTierCommandResult? result = null;
        return Execute();
        async Task<TroopTierCommandResult> Execute()
        {
            await repository.ExecuteAtomicallyAsync(command.PlayerId, command.HiveId, state =>
            {
                state = Reconcile(state);
                if (TryReplayTroopTier(state, command.IdempotencyKey, hash, out result)) return state;
                long before = state.Revision;
                if (!TroopTierCatalog.PopulationIds.Contains(populationId) || command.ExpectedRevision < 0 || string.IsNullOrWhiteSpace(command.IdempotencyKey) || command.IdempotencyKey.Length > 256)
                    return RecordTroopTier(state, command.IdempotencyKey, hash, false, "invalid_request", populationId, 0, before, before, out result);
                if (before != command.ExpectedRevision) return RecordTroopTier(state, command.IdempotencyKey, hash, false, "revision_conflict", populationId, 0, before, before, out result);
                TroopTierState progress = state.TroopTierProgress ?? new(new Dictionary<string, int>(StringComparer.Ordinal));
                int tier = progress.Tiers.GetValueOrDefault(populationId, 1);
                if (tier >= TroopTierCatalog.MaxTier) return RecordTroopTier(state, command.IdempotencyKey, hash, false, "troop_tier_max", populationId, tier, before, before, out result);
                (long honeyCost, long pollenCost) = TroopTierCatalog.PromotionCost(tier);
                ResourceBalance honey = state.Resources.GetValueOrDefault("honey", new(0, 0));
                ResourceBalance pollen = state.Resources.GetValueOrDefault("pollen", new(0, 0));
                if (honey.Amount < honeyCost || pollen.Amount < pollenCost)
                    return RecordTroopTier(state, command.IdempotencyKey, hash, false, "insufficient_resources", populationId, tier, before, before, out result);
                Dictionary<string, int> tiers = new(progress.Tiers, StringComparer.Ordinal) { [populationId] = tier + 1 };
                Dictionary<string, ResourceBalance> resources = new(state.Resources)
                {
                    ["honey"] = honey with { Amount = honey.Amount - honeyCost },
                    ["pollen"] = pollen with { Amount = pollen.Amount - pollenCost }
                };
                PlayerHiveState updated = state with { Revision = before + 1, Resources = resources, TroopTierProgress = progress with { Tiers = tiers } };
                return RecordTroopTier(updated, command.IdempotencyKey, hash, true, "troop_tier_promoted", populationId, tier + 1, before, before + 1, out result);
            }, ct);
            return result!;
        }
    }

    private bool TryReplayTroopTier(PlayerHiveState state, string key, string hash, out TroopTierCommandResult? result)
    {
        result = null;
        if (!state.Receipts.TryGetValue(key ?? string.Empty, out IdempotencyReceipt? receipt)) return false;
        result = receipt.PayloadHash == hash
            ? new(receipt.Succeeded, receipt.Code, receipt.ResultingStep ?? string.Empty, receipt.Answer is { } answerText && int.TryParse(answerText, out int parsedTier) ? parsedTier : 0, receipt.RevisionBefore ?? state.Revision, receipt.RevisionAfter ?? state.Revision, receipt.AcceptedAtUtc ?? receipt.CreatedAtUtc, state)
            : new(false, "idempotency_conflict", string.Empty, 0, state.Revision, state.Revision, clock.UtcNow, state);
        return true;
    }

    private PlayerHiveState RecordTroopTier(PlayerHiveState state, string key, string hash, bool ok, string code, string populationId, int tier, long before, long after, out TroopTierCommandResult? result)
    {
        DateTimeOffset now = clock.UtcNow;
        Dictionary<string, IdempotencyReceipt> receipts = new(state.Receipts) { [key ?? string.Empty] = new(hash, ok, code, null, now, before, after, null, populationId, tier.ToString(System.Globalization.CultureInfo.InvariantCulture), now) };
        PlayerHiveState recorded = state with { Receipts = receipts };
        result = new(ok, code, populationId, tier, before, after, now, recorded);
        return recorded;
    }

    public Task<VipCommandResult> GrantVipPointsAsync(GrantVipPointsCommand command, CancellationToken ct = default)
    {
        string hash = Hash($"vip-grant|{command.PlayerId}|{command.HiveId}|{command.Points}|{command.Source}|{command.ExpectedRevision}");
        VipCommandResult? result = null;
        return Execute();
        async Task<VipCommandResult> Execute()
        {
            await repository.ExecuteAtomicallyAsync(command.PlayerId, command.HiveId, state =>
            {
                state = Reconcile(state);
                if (TryReplayVip(state, command.IdempotencyKey, hash, out result)) return state;
                long before = state.Revision;
                if (command.Points <= 0 || command.Points > 1_000_000 || command.ExpectedRevision < 0 || string.IsNullOrWhiteSpace(command.IdempotencyKey) || command.IdempotencyKey.Length > 256)
                    return RecordVip(state, command.IdempotencyKey, hash, false, "invalid_request", state.Vip?.LifetimePoints ?? 0, before, before, out result);
                if (before != command.ExpectedRevision) return RecordVip(state, command.IdempotencyKey, hash, false, "revision_conflict", state.Vip?.LifetimePoints ?? 0, before, before, out result);
                long points = (state.Vip?.LifetimePoints ?? 0) + command.Points;
                PlayerHiveState updated = state with { Revision = before + 1, Vip = new VipProgressState(points) };
                return RecordVip(updated, command.IdempotencyKey, hash, true, "vip_points_granted", points, before, before + 1, out result);
            }, ct);
            return result!;
        }
    }

    private bool TryReplayVip(PlayerHiveState state, string key, string hash, out VipCommandResult? result)
    {
        result = null;
        if (!state.Receipts.TryGetValue(key ?? string.Empty, out IdempotencyReceipt? receipt)) return false;
        long points = state.Vip?.LifetimePoints ?? 0;
        result = receipt.PayloadHash == hash
            ? new(receipt.Succeeded, receipt.Code, points, VipCatalog.LevelForPoints(points), receipt.RevisionBefore ?? state.Revision, receipt.RevisionAfter ?? state.Revision, receipt.AcceptedAtUtc ?? receipt.CreatedAtUtc, state)
            : new(false, "idempotency_conflict", points, VipCatalog.LevelForPoints(points), state.Revision, state.Revision, clock.UtcNow, state);
        return true;
    }

    private PlayerHiveState RecordVip(PlayerHiveState state, string key, string hash, bool ok, string code, long points, long before, long after, out VipCommandResult? result)
    {
        DateTimeOffset now = clock.UtcNow;
        Dictionary<string, IdempotencyReceipt> receipts = new(state.Receipts) { [key ?? string.Empty] = new(hash, ok, code, null, now, before, after, null, points.ToString(System.Globalization.CultureInfo.InvariantCulture), null, now) };
        PlayerHiveState recorded = state with { Receipts = receipts };
        result = new(ok, code, points, VipCatalog.LevelForPoints(points), before, after, now, recorded);
        return recorded;
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
