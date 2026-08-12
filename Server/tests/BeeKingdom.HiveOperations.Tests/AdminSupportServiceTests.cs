using BeeKingdom.HiveOperations;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class AdminSupportServiceTests
{
    [Fact]
    public async Task AdjustResource_credits_and_appends_one_audit_entry()
    {
        string root = Temp(); Guid p = Guid.NewGuid(); Guid h = Guid.NewGuid(); var clock = new MutableClock(new(2026, 7, 25, 9, 0, 0, TimeSpan.Zero));
        try
        {
            var repo = Repo(root, p, h, honey: 100);
            var service = new AdminSupportService(repo, clock);

            AdminMutationResult result = await service.AdjustResourceAsync(new(p, h, "honey", 50, "Bug #42 refund", 0), default);

            Assert.True(result.Succeeded, result.Code);
            Assert.Equal(150, result.State.Resources["honey"].Amount);
            Assert.Single(result.State.AdminAudit!);
            Assert.Equal("Bug #42 refund", result.State.AdminAudit![0].Reason);
            Assert.Equal("resource_adjust", result.State.AdminAudit[0].Action);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task AdjustResource_clamps_to_capacity_and_zero()
    {
        string root = Temp(); Guid p = Guid.NewGuid(); Guid h = Guid.NewGuid(); var clock = new MutableClock(new(2026, 7, 25, 9, 0, 0, TimeSpan.Zero));
        try
        {
            var repo = Repo(root, p, h, honey: 100, honeyCapacity: 120);
            var service = new AdminSupportService(repo, clock);

            AdminMutationResult over = await service.AdjustResourceAsync(new(p, h, "honey", 1000, "test", 0), default);
            Assert.Equal(120, over.State.Resources["honey"].Amount);

            AdminMutationResult under = await service.AdjustResourceAsync(new(p, h, "honey", -10_000, "test", over.State.Revision), default);
            Assert.Equal(0, under.State.Resources["honey"].Amount);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task AdjustResource_refuses_unknown_resource_key()
    {
        string root = Temp(); Guid p = Guid.NewGuid(); Guid h = Guid.NewGuid(); var clock = new MutableClock(new(2026, 7, 25, 9, 0, 0, TimeSpan.Zero));
        try
        {
            var repo = Repo(root, p, h, honey: 100);
            var service = new AdminSupportService(repo, clock);

            AdminMutationResult result = await service.AdjustResourceAsync(new(p, h, "royal_jelly", 10, "test", 0), default);

            Assert.False(result.Succeeded);
            Assert.Equal("game.invalid_request", result.Code);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task AdjustResource_requires_a_reason()
    {
        string root = Temp(); Guid p = Guid.NewGuid(); Guid h = Guid.NewGuid(); var clock = new MutableClock(new(2026, 7, 25, 9, 0, 0, TimeSpan.Zero));
        try
        {
            var repo = Repo(root, p, h, honey: 100);
            var service = new AdminSupportService(repo, clock);

            await Assert.ThrowsAsync<ArgumentException>(() => service.AdjustResourceAsync(new(p, h, "honey", 10, "", 0), default));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task AdjustResource_rejects_stale_revision()
    {
        string root = Temp(); Guid p = Guid.NewGuid(); Guid h = Guid.NewGuid(); var clock = new MutableClock(new(2026, 7, 25, 9, 0, 0, TimeSpan.Zero));
        try
        {
            var repo = Repo(root, p, h, honey: 100);
            var service = new AdminSupportService(repo, clock);
            await service.AdjustResourceAsync(new(p, h, "honey", 10, "first", 0), default);

            AdminMutationResult stale = await service.AdjustResourceAsync(new(p, h, "honey", 10, "second", 0), default);

            Assert.False(stale.Succeeded);
            Assert.Equal("game.revision_conflict", stale.Code);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task AdjustRoster_credits_family_and_appends_audit()
    {
        string root = Temp(); Guid p = Guid.NewGuid(); Guid h = Guid.NewGuid(); var clock = new MutableClock(new(2026, 7, 25, 9, 0, 0, TimeSpan.Zero));
        try
        {
            var repo = Repo(root, p, h, guardians: 5);
            var service = new AdminSupportService(repo, clock);

            AdminMutationResult result = await service.AdjustRosterAsync(new(p, h, "guardians", 3, "restore lost troops", 0), default);

            Assert.True(result.Succeeded, result.Code);
            Assert.Equal(8, result.State.DoctrineRoster!.Counts["guardians"]);
            Assert.Single(result.State.AdminAudit!);
            Assert.Equal("roster_adjust", result.State.AdminAudit![0].Action);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task AdjustRoster_rejects_unknown_family()
    {
        string root = Temp(); Guid p = Guid.NewGuid(); Guid h = Guid.NewGuid(); var clock = new MutableClock(new(2026, 7, 25, 9, 0, 0, TimeSpan.Zero));
        try
        {
            var repo = Repo(root, p, h);
            var service = new AdminSupportService(repo, clock);

            await Assert.ThrowsAsync<ArgumentException>(() => service.AdjustRosterAsync(new(p, h, "scouts", 1, "test", 0), default));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task GrantCombatPatrolSlot_increments_and_refuses_beyond_two()
    {
        string root = Temp(); Guid p = Guid.NewGuid(); Guid h = Guid.NewGuid(); var clock = new MutableClock(new(2026, 7, 25, 9, 0, 0, TimeSpan.Zero));
        try
        {
            var repo = Repo(root, p, h);
            var service = new AdminSupportService(repo, clock);

            AdminMutationResult first = await service.GrantCombatPatrolSlotAsync(new(p, h, false, "compensation", 0), default);
            Assert.True(first.Succeeded, first.Code);
            AdminMutationResult second = await service.GrantCombatPatrolSlotAsync(new(p, h, false, "compensation", first.State.Revision), default);
            Assert.True(second.Succeeded, second.Code);
            Assert.Equal(2, second.State.CombatPatrol!.ResourcePurchasedSlots);

            AdminMutationResult third = await service.GrantCombatPatrolSlotAsync(new(p, h, false, "compensation", second.State.Revision), default);
            Assert.False(third.Succeeded);
            Assert.Equal("game.patrol_slot_limit_reached", third.Code);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task ReadDiagnostics_aggregates_resources_roster_and_patrol_summary()
    {
        string root = Temp(); Guid p = Guid.NewGuid(); Guid h = Guid.NewGuid(); var clock = new MutableClock(new(2026, 7, 25, 9, 0, 0, TimeSpan.Zero));
        try
        {
            var repo = Repo(root, p, h, honey: 250, guardians: 12);
            var service = new AdminSupportService(repo, clock);

            AdminDiagnostics diagnostics = await service.ReadDiagnosticsAsync(p, h, default);

            Assert.Equal(250, diagnostics.Resources["honey"].Amount);
            Assert.Equal(12, diagnostics.Roster["guardians"]);
            Assert.Equal(1, diagnostics.CombatPatrolTotalSlots);
            Assert.Empty(diagnostics.AdminAudit);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task ReadDiagnostics_throws_when_hive_never_seeded()
    {
        string root = Temp(); Guid p = Guid.NewGuid(); Guid h = Guid.NewGuid(); var clock = new MutableClock(new(2026, 7, 25, 9, 0, 0, TimeSpan.Zero));
        try
        {
            var repo = new DurableJsonHiveStateRepository(root, (playerId, hiveId) => throw new InvalidOperationException("should not seed"));
            var service = new AdminSupportService(repo, clock);
            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ReadDiagnosticsAsync(p, h, default));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static string Temp() => Path.Combine(Path.GetTempPath(), "bee-admin-support-" + Guid.NewGuid().ToString("N"));

    private static DurableJsonHiveStateRepository Repo(string root, Guid p, Guid h, long honey = 0, long honeyCapacity = 1_000_000, long guardians = 0)
    {
        var repo = new DurableJsonHiveStateRepository(root, (_, _) => new PlayerHiveState(
            p, h, HiveStateMigrator.CurrentModelVersion, 0,
            new Dictionary<string, ResourceBalance> { ["honey"] = new(honey, honeyCapacity), ["pollen"] = new(0, 1_000_000) },
            new Dictionary<string, int>(), [], new(),
            DoctrineRoster: new DoctrineRosterState(0, new() { ["guardians"] = guardians, ["wingrunners"] = 0, ["darters"] = 0 }, null, new())));
        repo.ExecuteAtomicallyAsync(p, h, s => s).GetAwaiter().GetResult();
        return repo;
    }

    private sealed class MutableClock(DateTimeOffset now) : IServerClock { public DateTimeOffset UtcNow { get; private set; } = now; public void Advance(TimeSpan value) => UtcNow += value; }
}
