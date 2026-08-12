using BeeKingdom.HiveOperations;
using System.Text.Json;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class HiveOperationServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "bee-hive-ops-" + Guid.NewGuid().ToString("N"));
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 7, 21, 12, 0, 0, TimeSpan.Zero));
    private readonly Guid _player = Guid.NewGuid();
    private readonly Guid _hive = Guid.NewGuid();

    [Fact]
    public async Task Operation_survives_restart_finishes_offline_and_collects_once()
    {
        HiveOperationService service = CreateService();
        HiveCommandResult started = await service.StartAsync(new(_player, _hive, "honey_reserve", 1, 0, "start-1"));
        Assert.True(started.Succeeded);
        Assert.Equal(800, started.State.Resources["wax"].Amount);

        _clock.Advance(TimeSpan.FromMinutes(6));
        service = CreateService();
        PlayerHiveState persisted = (await service.ReadAsync(_player, _hive))!;
        HiveCommandResult collected = await service.CollectAsync(new(_player, _hive, started.OperationId!.Value, persisted.Revision, "collect-1"));
        Assert.True(collected.Succeeded);
        Assert.Equal(125, collected.State.Resources["honey"].Amount);
        Assert.Equal(2, collected.State.BuildingLevels["honey_reserve"]);

        HiveCommandResult retry = await service.CollectAsync(new(_player, _hive, started.OperationId.Value, collected.State.Revision, "collect-1"));
        Assert.True(retry.Succeeded);
        Assert.Equal(125, retry.State.Resources["honey"].Amount);
    }

    [Fact]
    public async Task Concurrent_starts_debit_only_once()
    {
        HiveOperationService service = CreateService();
        Task<HiveCommandResult>[] calls = [
            service.StartAsync(new(_player, _hive, "honey_reserve", 1, 0, "a")),
            service.StartAsync(new(_player, _hive, "honey_reserve", 1, 0, "b"))];
        HiveCommandResult[] results = await Task.WhenAll(calls);
        Assert.Single(results, x => x.Succeeded);
        Assert.Equal(800, results.Last().State.Resources["wax"].Amount);
    }

    [Fact]
    public async Task Collection_respects_capacity_and_client_clock_is_irrelevant()
    {
        HiveOperationService service = CreateService(honey: 990, capacity: 1000);
        HiveCommandResult started = await service.StartAsync(new(_player, _hive, "honey_reserve", 1, 0, "start"));
        _clock.Advance(TimeSpan.FromMinutes(5));
        PlayerHiveState persisted = (await service.ReadAsync(_player, _hive))!;
        HiveCommandResult collected = await service.CollectAsync(new(_player, _hive, started.OperationId!.Value, persisted.Revision, "collect"));
        Assert.Equal("collected_capacity_limited", collected.Code);
        Assert.Equal(1000, collected.State.Resources["honey"].Amount);
    }

    [Fact]
    public async Task Insufficient_resources_do_not_create_operation()
    {
        HiveOperationService service = CreateService(wax: 50);
        HiveCommandResult result = await service.StartAsync(new(_player, _hive, "honey_reserve", 1, 0, "start"));
        Assert.False(result.Succeeded);
        Assert.Equal("insufficient_resources", result.Code);
        Assert.Empty(result.State.Operations);
    }

    [Fact]
    public async Task Network_retry_replays_start_without_second_debit()
    {
        HiveOperationService service = CreateService();
        StartBuildingOperationCommand command = new(_player, _hive, "honey_reserve", 1, 0, "stable-key");
        HiveCommandResult first = await service.StartAsync(command);
        HiveCommandResult retry = await service.StartAsync(command);
        Assert.True(retry.Succeeded);
        Assert.Equal(first.OperationId, retry.OperationId);
        Assert.Equal(800, retry.State.Resources["wax"].Amount);
        Assert.Single(retry.State.Operations);
    }

    [Fact]
    public async Task Same_idempotency_key_with_different_payload_is_rejected()
    {
        HiveOperationService service = CreateService();
        await service.StartAsync(new(_player, _hive, "honey_reserve", 1, 0, "same-key"));
        HiveCommandResult conflict = await service.StartAsync(new(_player, _hive, "other_building", 1, 1, "same-key"));
        Assert.False(conflict.Succeeded);
        Assert.Equal("idempotency_conflict", conflict.Code);
    }

    [Fact]
    public async Task Version_one_state_migrates_and_tutorial_resumes_at_safe_step()
    {
        HiveOperationService service = CreateService();
        HiveCommandResult saved = await service.SaveTutorialProgressAsync(new(_player, _hive, 0, "chapter_02", "chapter_02.start", "chapter_02.care_payment", "tutorial-1"));
        Assert.True(saved.Succeeded);
        Assert.Equal(HiveStateMigrator.CurrentModelVersion, saved.State.ModelVersion);
        service = CreateService();
        PlayerHiveState restored = (await service.ReadAsync(_player, _hive))!;
        Assert.Equal("chapter_02.start", restored.Tutorial!.SafeResumeStepKey);
        Assert.Equal("chapter_02.care_payment", restored.Tutorial.LastObservedStepKey);
    }

    [Fact]
    public async Task Chapter1_certification_is_ordered_idempotent_and_never_completes_installation()
    {
        HiveOperationService service = CreateService(honey: 37, wax: 733, capacity: 777, waxCapacity: 999, pollen: 421);
        HiveCommandResult first = await service.CertifyChapter1StepAsync(new(_player, _hive, 0, "tutorial_intro_acknowledged", "cert-1"));
        Assert.True(first.Succeeded);
        Dictionary<string, ResourceBalance> resourcesBefore = first.State.Resources.ToDictionary(x => x.Key, x => x.Value);
        HiveCommandResult retry = await service.CertifyChapter1StepAsync(new(_player, _hive, 0, "tutorial_intro_acknowledged", "cert-1"));
        Assert.True(retry.Succeeded);
        Assert.Equal(first.State.Revision, retry.State.Revision);
        Assert.Equal(first.State.Chapter1Certification!.FinalProof, retry.State.Chapter1Certification!.FinalProof);
        HiveCommandResult payloadConflict = await service.CertifyChapter1StepAsync(new(_player, _hive, 0, "hive_surface_acknowledged", "cert-1"));
        Assert.Equal("idempotency_conflict", payloadConflict.Code);
        HiveCommandResult skip = await service.CertifyChapter1StepAsync(new(_player, _hive, first.State.Revision, "tutorial_sequence_completed", "cert-skip"));
        Assert.False(skip.Succeeded);
        Assert.Equal("step_order_required", skip.Code);
        HiveCommandResult failedRetryImmediate = await service.CertifyChapter1StepAsync(new(_player, _hive, first.State.Revision, "tutorial_sequence_completed", "cert-skip"));
        Assert.Equal(skip.Code, failedRetryImmediate.Code);
        Assert.Equal(skip.State.Revision, failedRetryImmediate.State.Revision);
        HiveCommandResult second = await service.CertifyChapter1StepAsync(new(_player, _hive, first.State.Revision, "hive_surface_acknowledged", "cert-2"));
        Assert.True(second.Succeeded);
        Assert.Equal(first.State.Revision + 1, second.State.Revision);
        HiveCommandResult backwards = await service.CertifyChapter1StepAsync(new(_player, _hive, second.State.Revision, "tutorial_intro_acknowledged", "cert-back"));
        Assert.Equal("step_already_certified", backwards.Code);
        HiveCommandResult final = await service.CertifyChapter1StepAsync(new(_player, _hive, second.State.Revision, "tutorial_sequence_completed", "cert-3"));
        Assert.Equal("chapter1_certified", final.Code);
        Assert.True(final.Succeeded);
        Assert.False(final.State.InstallationComplete);
        string proof = Assert.IsType<string>(final.State.Chapter1Certification!.FinalProof);
        Assert.Equal(32, proof.Length);
        Assert.True(proof.All(Uri.IsHexDigit));
        HiveCommandResult finalRetry = await service.CertifyChapter1StepAsync(new(_player, _hive, final.State.Revision, "tutorial_sequence_completed", "cert-3"));
        Assert.True(finalRetry.Succeeded);
        Assert.Equal(final.State.Revision, finalRetry.State.Revision);
        Assert.Equal(final.State.Chapter1Certification.FinalProof, finalRetry.State.Chapter1Certification!.FinalProof);
        Assert.Equal(final.State.Chapter1Certification.AcceptedAtUtc, finalRetry.State.Chapter1Certification.AcceptedAtUtc);
        HiveCommandResult stale = await service.CertifyChapter1StepAsync(new(_player, _hive, 0, "tutorial_intro_acknowledged", "cert-stale"));
        Assert.Equal("revision_conflict", stale.Code);
        HiveCommandResult failedConflict = await service.CertifyChapter1StepAsync(new(_player, _hive, final.State.Revision, "hive_surface_acknowledged", "cert-skip"));
        Assert.Equal("idempotency_conflict", failedConflict.Code);
        PlayerHiveState persisted = (await service.ReadAsync(_player, _hive))!;
        Assert.Equal(resourcesBefore, persisted.Resources);
        Assert.Equal(new ResourceBalance(37, 777), persisted.Resources["honey"]);
        Assert.Equal(new ResourceBalance(733, 999), persisted.Resources["wax"]);
        Assert.Equal(new ResourceBalance(421, 1000), persisted.Resources["pollen"]);
    }

    [Fact]
    public async Task Legacy_v4_installation_flag_is_neutralized_during_migration()
    {
        DurableJsonHiveStateRepository repository = new(_root, (_, _) => new(_player, _hive, 4, 0, new(), new(), [], new(), null, new(), true));
        PlayerHiveState migrated = await repository.ExecuteAtomicallyAsync(_player, _hive, state => state);
        Assert.Equal(HiveStateMigrator.CurrentModelVersion, migrated.ModelVersion);
        Assert.False(migrated.InstallationComplete);
    }

    [Fact]
    public void Brood_vitality_migration_rejects_empty_operation_identity_and_unknown_type()
    {
        PlayerHiveState state = new(_player, _hive, 5, 0, new(), new(), [], new(), null, new(), false, null, null,
            new(50, 60, 0, DateTimeOffset.UtcNow, new(Guid.Empty, "unknown", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)));
        Assert.Throws<InvalidOperationException>(() => HiveStateMigrator.ToCurrent(state));
    }

    [Fact]
    public void Brood_vitality_migration_rejects_each_invalid_bound_and_timestamp()
    {
        DateTimeOffset utc = new(2026, 7, 21, 12, 0, 0, TimeSpan.Zero);
        PlayerHiveState Base(BroodVitalityState vitality) => new(_player, _hive, 5, 0, new(), new(), [], new(), null, new(), false, null, null, vitality);
        BroodVitalityState[] invalid =
        [
            new(-1, 50, 0, utc, null), new(101, 50, 0, utc, null),
            new(50, -1, 0, utc, null), new(50, 101, 0, utc, null),
            new(50, 50, -1, utc, null),
            new(50, 50, 0, utc.ToOffset(TimeSpan.FromHours(1)), null),
            new(50, 50, 0, utc, new(Guid.Empty, BroodVitalityOperationTypes.Feeding, utc, utc)),
            new(50, 50, 0, utc, new(Guid.NewGuid(), "unknown", utc, utc)),
            new(50, 50, 0, utc, new(Guid.NewGuid(), BroodVitalityOperationTypes.Feeding, utc.ToOffset(TimeSpan.FromHours(1)), utc)),
            new(50, 50, 0, utc, new(Guid.NewGuid(), BroodVitalityOperationTypes.Feeding, utc, utc.ToOffset(TimeSpan.FromHours(1)))),
            new(50, 50, 0, utc, new(Guid.NewGuid(), BroodVitalityOperationTypes.Feeding, utc, utc.AddMinutes(-1)))
        ];
        foreach (BroodVitalityState vitality in invalid) Assert.Throws<InvalidOperationException>(() => HiveStateMigrator.ToCurrent(Base(vitality)));
    }

    [Fact]
    public void V5_to_v6_migration_preserves_absent_vitality()
    {
        PlayerHiveState legacy = new(_player, _hive, 5, 3, new(), new(), [], new(), null, new(), false, null, null, null);
        PlayerHiveState current = HiveStateMigrator.ToCurrent(legacy);
        Assert.Equal(HiveStateMigrator.CurrentModelVersion, current.ModelVersion);
        Assert.Null(current.BroodVitality);
    }

    [Fact]
    public void V5_to_v6_migration_preserves_valid_vitality_exactly()
    {
        DateTimeOffset updated = new(2026, 7, 21, 12, 0, 0, TimeSpan.Zero);
        BroodVitalityState vitality = new(72, 81, 3, updated, new(Guid.Parse("11111111-1111-1111-1111-111111111111"), BroodVitalityOperationTypes.Feeding, updated.AddMinutes(-1), updated.AddMinutes(-1).AddSeconds(12)));
        PlayerHiveState legacy = new(_player, _hive, 5, 3, new(), new(), [], new(), null, new(), false, null, null, vitality);
        PlayerHiveState current = HiveStateMigrator.ToCurrent(legacy);
        Assert.Equal(HiveStateMigrator.CurrentModelVersion, current.ModelVersion);
        Assert.Equal(vitality, current.BroodVitality);
    }

    [Fact]
    public async Task Claimed_reward_cannot_be_credited_twice()
    {
        HiveOperationService service = CreateService(withReward: true);
        HiveCommandResult claimed = await service.ClaimRewardAsync(new(_player, _hive, 0, "chapter_01", "reward-1"));
        Assert.True(claimed.Succeeded);
        Assert.Equal(50, claimed.State.Resources["honey"].Amount);
        HiveCommandResult second = await service.ClaimRewardAsync(new(_player, _hive, claimed.State.Revision, "chapter_01", "reward-2"));
        Assert.False(second.Succeeded);
        Assert.Equal("reward_already_claimed", second.Code);
        Assert.Equal(50, second.State.Resources["honey"].Amount);
    }

    [Fact]
    public async Task Foundation_choice_is_authoritative_idempotent_and_returns_persisted_proof()
    {
        HiveOperationService service = CreateService(installationComplete: true, pollen: 0);
        ClaimFoundationDotationCommand command = new(_player, _hive, 0, HiveOperationService.MixedFoundationChoice, "foundation-1");

        HiveCommandResult first = await service.ClaimFoundationDotationAsync(command);
        HiveCommandResult retry = await service.ClaimFoundationDotationAsync(command);

        Assert.True(first.Succeeded);
        Assert.Equal("foundation_claimed", first.Code);
        Assert.Equal(170, first.State.Resources["honey"].Amount);
        Assert.Equal(80, first.State.Resources["pollen"].Amount);
        Assert.NotNull(first.State.FoundationDotation);
        Assert.Equal(first.State.FoundationDotation!.Proof, retry.State.FoundationDotation!.Proof);
        Assert.Equal(first.State.Revision, retry.State.Revision);
    }

    [Fact]
    public async Task Foundation_same_key_with_other_choice_is_conflict_and_no_second_reward()
    {
        HiveOperationService service = CreateService(installationComplete: true, pollen: 0);
        await service.ClaimFoundationDotationAsync(new(_player, _hive, 0, HiveOperationService.HoneyReserveFoundationChoice, "foundation-2"));

        HiveCommandResult conflict = await service.ClaimFoundationDotationAsync(new(_player, _hive, 1, HiveOperationService.MixedFoundationChoice, "foundation-2"));

        Assert.False(conflict.Succeeded);
        Assert.Equal("idempotency_conflict", conflict.Code);
        Assert.Equal(250, conflict.State.Resources["honey"].Amount);
        Assert.Equal(0, conflict.State.Resources["pollen"].Amount);
    }

    [Fact]
    public async Task Foundation_requires_installation_and_rejects_second_distinct_claim()
    {
        HiveOperationService service = CreateService();
        HiveCommandResult notReady = await service.ClaimFoundationDotationAsync(new(_player, _hive, 0, HiveOperationService.HoneyReserveFoundationChoice, "foundation-3"));
        Assert.False(notReady.Succeeded);
        Assert.Equal("installation_incomplete", notReady.Code);

        Guid eligibleHive = Guid.NewGuid();
        service = CreateService(installationComplete: true, pollen: 0);
        HiveCommandResult first = await service.ClaimFoundationDotationAsync(new(_player, eligibleHive, 0, HiveOperationService.HoneyReserveFoundationChoice, "foundation-4"));
        HiveCommandResult second = await service.ClaimFoundationDotationAsync(new(_player, eligibleHive, first.State.Revision, HiveOperationService.MixedFoundationChoice, "foundation-5"));
        Assert.True(first.Succeeded);
        Assert.False(second.Succeeded);
        Assert.Equal("foundation_already_claimed", second.Code);
    }

    [Fact]
    public async Task Durable_repository_migrates_legacy_state_on_read_and_atomic_mutation()
    {
        Directory.CreateDirectory(_root);
        PlayerHiveState legacy = new(_player, _hive, 3, 4,
            new() { ["honey"] = new(10, 1000), ["pollen"] = new(0, 1000) },
            new() { ["honey_reserve"] = 1 }, [], new());
        string path = Path.Combine(_root, $"{_player:N}_{_hive:N}.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(legacy));

        DurableJsonHiveStateRepository repository = new(_root, (_, _) => legacy);
        PlayerHiveState? read = await repository.ReadAsync(_player, _hive);
        PlayerHiveState mutated = await repository.ExecuteAtomicallyAsync(_player, _hive, state => state with { Revision = state.Revision + 1 });

        Assert.NotNull(read);
        Assert.Equal(HiveStateMigrator.CurrentModelVersion, read!.ModelVersion);
        Assert.NotNull(read.Rewards);
        Assert.Equal(HiveStateMigrator.CurrentModelVersion, mutated.ModelVersion);
        Assert.Equal(5, mutated.Revision);
    }

    [Fact]
    public async Task Training_survives_restart_and_waits_for_manual_collection()
    {
        HiveOperationService service = CreateService(honey: 1000);
        HiveCommandResult started = await service.StartQueuedOperationAsync(new(_player, _hive, "train_worker", 0, "train-1"));
        Assert.True(started.Succeeded);
        Assert.Equal(900, started.State.Resources["honey"].Amount);
        _clock.Advance(TimeSpan.FromMinutes(3));
        service = CreateService();
        PlayerHiveState ready = (await service.ReadAsync(_player, _hive))!;
        Assert.Equal(HiveOperationStatus.AwaitingCollection, ready.Operations.Single().Status);
        Assert.Equal(0, ready.Resources["units.worker"].Amount);
        HiveCommandResult collected = await service.CollectAsync(new(_player, _hive, started.OperationId!.Value, ready.Revision, "train-collect"));
        Assert.Equal(5, collected.State.Resources["units.worker"].Amount);
    }

    [Fact]
    public async Task Production_queue_is_independent_and_capacity_limited_on_collection()
    {
        HiveOperationService service = CreateService(wax: 990, waxCapacity: 1000);
        HiveCommandResult started = await service.StartQueuedOperationAsync(new(_player, _hive, "produce_wax", 0, "produce-1"));
        _clock.Advance(TimeSpan.FromMinutes(4));
        PlayerHiveState ready = (await service.ReadAsync(_player, _hive))!;
        HiveCommandResult collected = await service.CollectAsync(new(_player, _hive, started.OperationId!.Value, ready.Revision, "produce-collect"));
        Assert.Equal("collected_capacity_limited", collected.Code);
        Assert.Equal(1000, collected.State.Resources["wax"].Amount);
    }

    private HiveOperationService CreateService(long honey = 0, long wax = 1000, long capacity = 1000, bool withReward = false, long waxCapacity = 1000, bool installationComplete = false, long pollen = 1000)
    {
        DurableJsonHiveStateRepository repository = new(_root, (player, hive) => new(player, hive, HiveStateMigrator.CurrentModelVersion, 0,
            new() { ["honey"] = new(honey, capacity), ["wax"] = new(wax, waxCapacity), ["pollen"] = new(pollen, 1000), ["units.worker"] = new(0, 100) },
            new() { ["honey_reserve"] = 1 }, [], new(), null,
            withReward ? new() { ["chapter_01"] = new("chapter_01", "honey", 50, false, null) } : new(),
            installationComplete));
        return new(repository, _clock,
            [new("honey_reserve", 1, 2, TimeSpan.FromMinutes(5), new Dictionary<string, long> { ["wax"] = 200 }, "honey", 125)],
            null,
            [
                new("train_worker", HiveOperationKind.Training, "nursery", TimeSpan.FromMinutes(3), new Dictionary<string, long> { ["honey"] = 100, ["pollen"] = 50 }, "units.worker", 5),
                new("produce_wax", HiveOperationKind.Production, "wax_workshop", TimeSpan.FromMinutes(4), new Dictionary<string, long> { ["pollen"] = 25 }, "wax", 40)
            ]);
    }

    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
    private sealed class FakeClock(DateTimeOffset now) : IServerClock { public DateTimeOffset UtcNow { get; private set; } = now; public void Advance(TimeSpan duration) => UtcNow += duration; }
}
