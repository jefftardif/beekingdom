using Xunit;
namespace BeeKingdom.HiveOperations.Tests;

public sealed class HiveDailyRoundPersistenceTests
{
    [Fact]
    public void LegacyStateWithoutDailyReceiptsNormalizesEmpty() => Assert.Empty(HiveStateMigrator.ToCurrent(new PlayerHiveState(Guid.NewGuid(), Guid.NewGuid(), HiveStateMigrator.CurrentModelVersion, 0, new(), new(), new(), new()) with { DailyRoundReceipts = null }).DailyRoundReceipts!);

    [Fact]
    public void CorruptDailyRoundStateIsRejected()
    {
        var day = new DateTimeOffset(2026, 7, 23, 0, 0, 0, TimeSpan.Zero); var id = Guid.NewGuid(); var baseState = new PlayerHiveState(id, Guid.NewGuid(), 10, 1, new(), new(), new(), new(), DailyRound: new HiveDailyRoundState(day, true, true, true, null));
        Assert.Throws<InvalidDataException>(() => HiveStateMigrator.ToCurrent(baseState with { DailyRound = new HiveDailyRoundState(day.AddHours(1), true, true, true, null) }));
        Assert.Throws<InvalidDataException>(() => HiveStateMigrator.ToCurrent(baseState with { DailyRound = new HiveDailyRoundState(day, true, false, true, day.AddHours(1)) }));
    }

    [Fact]
    public void CorruptDailyReceiptsAreRejected()
    {
        var day = new DateTimeOffset(2026, 7, 23, 0, 0, 0, TimeSpan.Zero); var player = Guid.NewGuid(); var hive = Guid.NewGuid();
        var valid = new HiveDailyRoundStoredReceipt(new string('a', 64), true, day, 0, 1, day.AddHours(1), 120, 60, "daily_round_claimed");
        var state = new PlayerHiveState(player, hive, 10, 1, new(), new(), new(), new(), DailyRoundReceipts: new Dictionary<string, HiveDailyRoundStoredReceipt> { ["claim"] = valid });
        var cases = new (string Key, HiveDailyRoundStoredReceipt Receipt)[]
        {
            ("claim", valid with { PayloadHash = new string('A', 64) }),
            (" ", valid),
            ("claim", valid with { DayUtc = day.AddHours(1) }),
            ("claim", valid with { AcceptedAtUtc = day.AddDays(1) }),
            ("claim", valid with { RevisionAfter = 2 }),
            ("claim", valid with { CreditedHoney = 119 }),
            ("claim", valid with { Code = "wrong" }),
            ("claim", valid with { Succeeded = false, CreditedHoney = 1 }),
            ("claim", valid with { Succeeded = false, Code = "daily_round_claimed" })
        };
        foreach (var item in cases)
            Assert.Throws<InvalidDataException>(() => HiveStateMigrator.ToCurrent(state with { DailyRoundReceipts = new Dictionary<string, HiveDailyRoundStoredReceipt> { [item.Key] = item.Receipt } }));
    }

    [Fact]
    public async Task ValidDailyReceiptRoundTripsAfterReconstructionAndNextDay()
    {
        Guid p = Guid.NewGuid(), h = Guid.NewGuid(); var clock = new MutableClock(new DateTimeOffset(2026, 7, 23, 12, 0, 0, TimeSpan.Zero));
        string root = Path.Combine(Path.GetTempPath(), "daily-persist-" + Guid.NewGuid());
        try
        {
            var factory = (Guid x, Guid y) => new PlayerHiveState(x, y, HiveStateMigrator.CurrentModelVersion, 0,
                new Dictionary<string, ResourceBalance> { ["honey"] = new(0, 1000), ["pollen"] = new(0, 1000) }, new(), [], new(),
                DailyRound: new HiveDailyRoundState(new DateTimeOffset(2026, 7, 23, 0, 0, 0, TimeSpan.Zero), true, true, true, null));
            var repo = new DurableJsonHiveStateRepository(root, factory); var service = new HiveOperationService(repo, clock, []);
            var first = await service.ClaimDailyRoundAsync(new(p, h, 0, "persist-1", "2026-07-23"));
            Assert.True(first.Succeeded); var stored = (await repo.ReadAsync(p, h))!.DailyRoundReceipts!["persist-1"];
            var repo2 = new DurableJsonHiveStateRepository(root, factory); var service2 = new HiveOperationService(repo2, clock, []);
            await repo2.ExecuteAtomicallyAsync(p, h, s => s with { Revision = s.Revision + 1, Resources = new Dictionary<string, ResourceBalance>(s.Resources) { ["honey"] = new(10, 1000) } });
            clock.Now = clock.Now.AddDays(1);
            var replay = await service2.ClaimDailyRoundAsync(new(p, h, 0, "persist-1", "2026-07-23"));
            Assert.Equal(first.Succeeded, replay.Succeeded); Assert.Equal(first.Code, replay.Code); Assert.Equal(first.RevisionBefore, replay.RevisionBefore); Assert.Equal(first.RevisionAfter, replay.RevisionAfter); Assert.Equal(first.AcceptedAtUtc, replay.AcceptedAtUtc);
            var after = (await repo2.ReadAsync(p, h))!; Assert.Equal(stored, after.DailyRoundReceipts!["persist-1"]); Assert.Equal(10, after.Resources["honey"].Amount);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task RetentionKeepsNewest128AndNewReceiptReplayable()
    {
        Guid p = Guid.NewGuid(), h = Guid.NewGuid(); var clock = new MutableClock(new DateTimeOffset(2026, 7, 23, 12, 0, 0, TimeSpan.Zero)); string root = Path.Combine(Path.GetTempPath(), "daily-retain-" + Guid.NewGuid());
        try
        {
            var day = new DateTimeOffset(2026, 7, 23, 0, 0, 0, TimeSpan.Zero); var map = new Dictionary<string, HiveDailyRoundStoredReceipt>();
            for (int i = 0; i < 128; i++) map[$"k-{i:D3}"] = new(new string('a', 64), false, day, 0, 0, clock.UtcNow, 0, 0, "daily_round_incomplete");
            var factory = (Guid x, Guid y) => new PlayerHiveState(x, y, HiveStateMigrator.CurrentModelVersion, 0, new Dictionary<string, ResourceBalance> { ["honey"] = new(0, 1000), ["pollen"] = new(0, 1000) }, new(), [], new(), DailyRoundReceipts: new Dictionary<string, HiveDailyRoundStoredReceipt>(map));
            var repo = new DurableJsonHiveStateRepository(root, factory); var service = new HiveOperationService(repo, clock, []);
            var result = await service.ClaimDailyRoundAsync(new(p, h, 0, "k-new", "2026-07-23")); Assert.False(result.Succeeded);
            var state = (await repo.ReadAsync(p, h))!; Assert.Equal(128, state.DailyRoundReceipts!.Count); Assert.DoesNotContain("k-000", state.DailyRoundReceipts.Keys); Assert.Contains("k-new", state.DailyRoundReceipts.Keys);
            var replay = await service.ClaimDailyRoundAsync(new(p, h, 0, "k-new", "2026-07-23")); Assert.Equal(result.Code, replay.Code);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private sealed class MutableClock(DateTimeOffset value) : IServerClock { public DateTimeOffset Now { get; set; } = value; public DateTimeOffset UtcNow => Now; }
}
