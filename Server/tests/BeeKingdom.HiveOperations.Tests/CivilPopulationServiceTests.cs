using BeeKingdom.HiveOperations;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class CivilPopulationServiceTests
{
    private static readonly DateTimeOffset Origin =
        new(2026, 10, 21, 16, 0, 0, TimeSpan.Zero);

    private sealed class FakeClock(DateTimeOffset utc) : IServerClock
    {
        public DateTimeOffset UtcNow { get; set; } = utc;
    }

    private static string Temp() => Path.Combine(
        Path.GetTempPath(), "bk-civil-" + Guid.NewGuid().ToString("N"));

    private static PlayerHiveState Seed(Guid player, Guid hive,
        int nurseryLevel = 1, long honey = 5000, long pollen = 1000) =>
        new(player, hive, HiveStateMigrator.CurrentModelVersion, 0,
            new Dictionary<string, ResourceBalance>(StringComparer.Ordinal)
            {
                ["honey"] = new(honey, 10000),
                ["pollen"] = new(pollen, 10000),
                ["wax"] = new(500, 1000)
            },
            nurseryLevel == 0
                ? new Dictionary<string, int>()
                : new Dictionary<string, int> { ["nursery_cluster"] = nurseryLevel },
            [], new Dictionary<string, IdempotencyReceipt>(),
            NurseryCohorts: new NurseryCohortState(
                0, 17, null, new Dictionary<string, IdempotencyReceipt>()));

    private static CivilPopulationOptions Options(bool enabled = true,
        long capacity = 200) => new()
    {
        Enabled = enabled, WorkersPerCohort = 20, HoneyCost = 50,
        PollenCost = 10, IncubationMinutes = 60,
        CapacityPerNurseryLevel = capacity
    };

    private static DurableJsonHiveStateRepository Repo(string root,
        Func<Guid, Guid, PlayerHiveState>? factory = null) =>
        new(root, factory ?? ((p, h) => Seed(p, h)));

    [Fact]
    public async Task Flag_off_by_default_does_not_create_free_civilians()
    {
        string root = Temp();
        try
        {
            Guid p = Guid.NewGuid(), h = Guid.NewGuid();
            var repo = Repo(root);
            await repo.ExecuteAtomicallyAsync(p, h, state => state);
            var service = new CivilPopulationService(repo, new FakeClock(Origin),
                new CivilPopulationOptions());
            Assert.False(new CivilPopulationOptions().Enabled);
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.ReadAsync(p, h));
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.StartAsync(p, h, new(0, "disabled-key")));
            Assert.Null((await repo.ReadAsync(p, h))!.CivilPopulation);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Old_hive_stays_intact_and_paid_civilians_never_use_military_or_nursery_reserve()
    {
        string root = Temp();
        try
        {
            Guid p = Guid.NewGuid(), h = Guid.NewGuid();
            var repo = Repo(root);
            await repo.ExecuteAtomicallyAsync(p, h, state => state);
            var service = new CivilPopulationService(repo, new FakeClock(Origin), Options());
            var before = await service.ReadAsync(p, h);
            Assert.Equal(0, before.AvailableWorkers);
            Assert.Null(before.Active);
            var started = await service.StartAsync(p, h, new(0, "paid-cohort-0001"));
            Assert.True(started.Succeeded);
            Assert.Equal(20, started.Snapshot.Active!.ExpectedWorkers);
            Assert.Equal(0, started.Snapshot.AvailableWorkers);
            Assert.Equal(Origin.AddHours(1), started.Snapshot.Active.ReadyAtUtc);
            PlayerHiveState after = (await repo.ReadAsync(p, h))!;
            Assert.Equal(4950, after.Resources["honey"].Amount);
            Assert.Equal(990, after.Resources["pollen"].Amount);
            Assert.Equal(17, after.NurseryCohorts!.RecruitsInReserve);
            Assert.Null(after.DoctrineRoster);
            Assert.Null(after.WorldResourceCollection);
            Assert.Equal(1, after.Revision);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Same_command_replay_returns_original_receipt_without_second_charge()
    {
        string root = Temp();
        try
        {
            Guid p = Guid.NewGuid(), h = Guid.NewGuid();
            var repo = Repo(root);
            await repo.ExecuteAtomicallyAsync(p, h, state => state);
            var service = new CivilPopulationService(repo, new FakeClock(Origin), Options());
            var request = new StartCivilCohortRequest(0, "same-command-0001");
            var first = await service.StartAsync(p, h, request);
            var second = await service.StartAsync(p, h, request);
            Assert.True(first.Succeeded && second.Succeeded);
            Assert.Equal(first.Receipt, second.Receipt);
            Assert.Equal(first.Snapshot.Active!.CohortId, second.Snapshot.Active!.CohortId);
            Assert.Equal(1, second.Snapshot.HiveRevision);
            var conflict = await service.StartAsync(
                p, h, new(1, "same-command-0001"));
            Assert.Equal("game.idempotency_conflict", conflict.Code);
            PlayerHiveState state = (await repo.ReadAsync(p, h))!;
            Assert.Equal(4950, state.Resources["honey"].Amount);
            Assert.Equal(990, state.Resources["pollen"].Amount);
            Assert.Equal(1, state.Revision);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Civilians_mature_offline_and_auto_settle_exactly_once_after_reopen()
    {
        string root = Temp();
        try
        {
            Guid p = Guid.NewGuid(), h = Guid.NewGuid();
            await Repo(root).ExecuteAtomicallyAsync(p, h, state => state);
            var original = new CivilPopulationService(Repo(root), new FakeClock(Origin), Options());
            Assert.True((await original.StartAsync(p, h, new(0, "offline-start-0001"))).Succeeded);
            var restartedClock = new FakeClock(Origin.AddHours(3));
            var reopened = new CivilPopulationService(Repo(root), restartedClock, Options());
            var settled = await reopened.ReadAsync(p, h);
            Assert.Equal(20, settled.AvailableWorkers);
            Assert.Null(settled.Active);
            Assert.Equal(2, settled.HiveRevision);
            Assert.Equal(20, (await reopened.ReadAsync(p, h)).AvailableWorkers);
            Assert.Equal(2, (await reopened.ReadAsync(p, h)).HiveRevision);
            PlayerHiveState persisted = (await Repo(root).ReadAsync(p, h))!;
            Assert.Equal(20, persisted.CivilPopulation!.AvailableWorkers);
            Assert.Equal(4950, persisted.Resources["honey"].Amount);
            Assert.Equal(990, persisted.Resources["pollen"].Amount);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Competing_commands_cannot_reserve_two_cohorts()
    {
        string root = Temp();
        try
        {
            Guid p = Guid.NewGuid(), h = Guid.NewGuid();
            await Repo(root).ExecuteAtomicallyAsync(p, h, state => state);
            var service = new CivilPopulationService(Repo(root), new FakeClock(Origin), Options());
            CivilPopulationCommandResult[] results = await Task.WhenAll(
                service.StartAsync(p, h, new(0, "parallel-command-a")),
                service.StartAsync(p, h, new(0, "parallel-command-b")));
            Assert.Single(results.Where(x => x.Succeeded));
            Assert.Single(results.Where(x => !x.Succeeded));
            PlayerHiveState persisted = (await Repo(root).ReadAsync(p, h))!;
            Assert.Equal(4950, persisted.Resources["honey"].Amount);
            Assert.Equal(990, persisted.Resources["pollen"].Amount);
            Assert.NotNull(persisted.CivilPopulation!.Active);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Insufficient_resources_and_capacity_reject_without_charging()
    {
        string root = Temp();
        try
        {
            Guid p = Guid.NewGuid(), h = Guid.NewGuid();
            var repo = Repo(root, (x, y) => Seed(x, y, honey: 5));
            await repo.ExecuteAtomicallyAsync(p, h, state => state);
            var svc = new CivilPopulationService(repo, new FakeClock(Origin), Options());
            var denied = await svc.StartAsync(p, h, new(0, "too-poor-start-0001"));
            Assert.False(denied.Succeeded);
            Assert.Equal("game.insufficient_resources", denied.Code);
            Assert.Equal(5, (await repo.ReadAsync(p, h))!.Resources["honey"].Amount);
            Assert.Null((await repo.ReadAsync(p, h))!.CivilPopulation);

            Guid secondPlayer = Guid.NewGuid(), secondHive = Guid.NewGuid();
            await repo.ExecuteAtomicallyAsync(secondPlayer, secondHive, state => state);
            var limited = new CivilPopulationService(repo, new FakeClock(Origin),
                Options(capacity: 19));
            var full = await limited.StartAsync(secondPlayer, secondHive,
                new(0, "too-small-cap-0001"));
            Assert.False(full.Succeeded);
            Assert.Equal("game.civil_population_capacity", full.Code);
            Assert.Equal(5, (await repo.ReadAsync(secondPlayer, secondHive))!.Resources["honey"].Amount);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Unknown_hive_cannot_be_created_by_mutation()
    {
        string root = Temp();
        try
        {
            Guid p = Guid.NewGuid(), h = Guid.NewGuid();
            var repo = Repo(root);
            var service = new CivilPopulationService(repo, new FakeClock(Origin), Options());
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.StartAsync(p, h, new(0, "unknown-hive-0001")));
            Assert.Null(await repo.ReadAsync(p, h));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void Invalid_persisted_population_fails_validation()
    {
        var invalid = new CivilPopulationState(0, -1, 0, 0, null, new());
        Assert.Throws<InvalidDataException>(() =>
            CivilPopulationService.ValidateStored(invalid));
    }
}
