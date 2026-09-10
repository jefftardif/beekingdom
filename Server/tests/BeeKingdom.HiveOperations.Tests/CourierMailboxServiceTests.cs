using System.Linq;
using BeeKingdom.HiveOperations;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class CourierMailboxServiceTests
{
    private static CourierMailboxOptions Options() => new() { Enabled = true };

    private static (Guid, Guid, DurableJsonHiveStateRepository) NewRepo(CourierMailboxState mailbox = null)
    {
        Guid p = Guid.NewGuid(), h = Guid.NewGuid();
        string root = Path.Combine(Path.GetTempPath(), "courier-mailbox-" + Guid.NewGuid());
        var repo = new DurableJsonHiveStateRepository(root, (x, y) => new PlayerHiveState(x, y, 12, 0,
            new Dictionary<string, ResourceBalance> { ["honey"] = new(0, 1_000_000), ["pollen"] = new(0, 1_000_000), ["wax"] = new(0, 1_000_000) },
            new Dictionary<string, int>(), [], new(),
            CourierMailbox: mailbox));
        return (p, h, repo);
    }

    [Fact]
    public async Task ReadCreatesEmptyMailboxByDefault()
    {
        (Guid p, Guid h, var repo) = NewRepo();
        var service = new CourierMailboxService(repo, new Clock(), Options());
        CourierMailboxSnapshot snapshot = await service.ReadAsync(p, h);
        Assert.Empty(snapshot.Messages);
    }

    [Fact]
    public async Task AppendInsertsAtFront()
    {
        (Guid p, Guid h, var repo) = NewRepo();
        var service = new CourierMailboxService(repo, new Clock(), Options());
        await service.AppendAsync(p, h, new("combat:1", "report", "Rapport de combat — Tier 2", "preview", "body", null));
        CourierMailboxSnapshot snapshot = await service.AppendAsync(p, h, new("combat:2", "report", "Rapport de combat — Tier 3", "preview", "body", null));
        Assert.Equal(2, snapshot.Messages.Count);
        Assert.Equal("combat:2", snapshot.Messages[0].Id);
        Assert.Equal("combat:1", snapshot.Messages[1].Id);
    }

    [Fact]
    public async Task AppendIsIdempotentByMessageId()
    {
        (Guid p, Guid h, var repo) = NewRepo();
        var service = new CourierMailboxService(repo, new Clock(), Options());
        await service.AppendAsync(p, h, new("combat:1", "report", "Rapport A", "p1", "b1", null));
        CourierMailboxSnapshot snapshot = await service.AppendAsync(p, h, new("combat:1", "report", "Rapport A rejoue", "p2", "b2", null));
        Assert.Single(snapshot.Messages);
        Assert.Equal("Rapport A", snapshot.Messages[0].Title);
    }

    [Fact]
    public async Task AppendPreservesRewardsWithAmount()
    {
        (Guid p, Guid h, var repo) = NewRepo();
        var service = new CourierMailboxService(repo, new Clock(), Options());
        var rewards = new List<CourierRewardRecord> { new("honey", 240, true), new("wax", 60, true) };
        CourierMailboxSnapshot snapshot = await service.AppendAsync(p, h, new("combat:1", "report", "Rapport", "p", "b", rewards));
        Assert.Equal(2, snapshot.Messages[0].Rewards.Count);
        Assert.Equal(240, snapshot.Messages[0].Rewards.Single(r => r.ItemId == "honey").Amount);
    }

    [Fact]
    public async Task SetReadTogglesOnlyTargetMessage()
    {
        (Guid p, Guid h, var repo) = NewRepo();
        var service = new CourierMailboxService(repo, new Clock(), Options());
        await service.AppendAsync(p, h, new("a", "report", "A", null, null, null));
        await service.AppendAsync(p, h, new("b", "report", "B", null, null, null));
        CourierMailboxSnapshot snapshot = await service.SetReadAsync(p, h, "a", true);
        Assert.True(snapshot.Messages.Single(m => m.Id == "a").Read);
        Assert.False(snapshot.Messages.Single(m => m.Id == "b").Read);
    }

    [Fact]
    public async Task DeleteReadRemovesReadNonFavoritesOnly()
    {
        (Guid p, Guid h, var repo) = NewRepo();
        var service = new CourierMailboxService(repo, new Clock(), Options());
        await service.AppendAsync(p, h, new("a", "report", "A", null, null, null));
        await service.AppendAsync(p, h, new("b", "report", "B", null, null, null));
        await service.SetReadAsync(p, h, "a", true);
        await service.SetReadAsync(p, h, "b", true);
        await service.SetFavoriteAsync(p, h, "b", true);
        CourierMailboxSnapshot snapshot = await service.DeleteReadAsync(p, h);
        Assert.Single(snapshot.Messages);
        Assert.Equal("b", snapshot.Messages[0].Id);
    }

    [Fact]
    public async Task CollectRewardsMarksAllRewardsCollected()
    {
        (Guid p, Guid h, var repo) = NewRepo();
        var service = new CourierMailboxService(repo, new Clock(), Options());
        var rewards = new List<CourierRewardRecord> { new("honey", 10, false) };
        await service.AppendAsync(p, h, new("a", "reward", "A", null, null, rewards));
        CourierMailboxSnapshot snapshot = await service.CollectRewardsAsync(p, h, "a");
        Assert.True(snapshot.Messages[0].Rewards[0].Collected);
    }

    [Fact]
    public async Task MailboxSurvivesReadWriteRoundTrip()
    {
        (Guid p, Guid h, var repo) = NewRepo();
        var service = new CourierMailboxService(repo, new Clock(), Options());
        await service.AppendAsync(p, h, new("combat:1", "report", "Rapport persiste", "p", "b", null));

        var reloaded = new CourierMailboxService(repo, new Clock(), Options());
        CourierMailboxSnapshot snapshot = await reloaded.ReadAsync(p, h);
        Assert.Single(snapshot.Messages);
        Assert.Equal("Rapport persiste", snapshot.Messages[0].Title);
    }

    private sealed class Clock : IServerClock
    {
        private DateTimeOffset current = DateTimeOffset.Parse("2026-09-10T12:00:00Z");
        public DateTimeOffset UtcNow => current;
    }
}
