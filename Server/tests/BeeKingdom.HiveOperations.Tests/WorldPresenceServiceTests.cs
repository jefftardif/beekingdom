using BeeKingdom.HiveOperations;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class WorldPresenceServiceTests
{
    private static WorldResourceCollectionOptions Options() => new()
    {
        Enabled = true,
        CatalogVersion = "v1",
        Catalog = [new("res_pollen_core", "pollen", "rich", 80, TimeSpan.FromSeconds(90), TimeSpan.FromMinutes(4), "Champ de pollen")]
    };

    private static DurableJsonHiveStateRepository NewRepo(string root) => new(root, (x, y) => new PlayerHiveState(x, y, 10, 0,
        new Dictionary<string, ResourceBalance> { ["honey"] = new(0, 1_000_000), ["pollen"] = new(0, 1_000_000), ["wax"] = new(0, 1_000_000) },
        new Dictionary<string, int>(), [], new(),
        DoctrineRoster: new DoctrineRosterState(0, new Dictionary<string, long> { ["guardians"] = 5, ["wingrunners"] = 5, ["darters"] = 5 }, null, new())));

    [Fact]
    public async Task ReadAsync_reports_another_colonys_active_flight_but_never_the_callers_own()
    {
        string root = Path.Combine(Path.GetTempPath(), "world-presence-" + Guid.NewGuid());
        var repo = NewRepo(root);
        try
        {
            var clock = new Clock(0);
            var collection = new WorldResourceCollectionService(repo, clock, Options());

            (Guid otherPlayer, Guid otherHive) = (Guid.NewGuid(), Guid.NewGuid());
            WorldResourceCollectionResult launch = await collection.LaunchAsync(otherPlayer, otherHive, "res_pollen_core", new(1, 0, 0, 0, "k1"));
            Assert.True(launch.Succeeded, launch.Code);

            (Guid me, Guid myHive) = (Guid.NewGuid(), Guid.NewGuid());
            var presence = new WorldPresenceService(repo, clock);
            WorldPresenceSnapshot snapshot = await presence.ReadAsync(myHive);

            WorldPresenceSighting sighting = Assert.Single(snapshot.Sightings);
            Assert.Equal(otherHive, sighting.HiveId);
            Assert.Equal("res_pollen_core", sighting.NodeId);
            Assert.StartsWith("Colonie #", sighting.ColonyLabel);

            // Jamais sa propre ruche, meme si elle a aussi un vol actif au moment de la lecture.
            WorldResourceCollectionResult ownLaunch = await collection.LaunchAsync(me, myHive, "res_pollen_core", new(1, 0, 0, 0, "k2"));
            Assert.True(ownLaunch.Succeeded, ownLaunch.Code);
            WorldPresenceSnapshot snapshotExcludingSelf = await presence.ReadAsync(myHive);
            Assert.DoesNotContain(snapshotExcludingSelf.Sightings, s => s.HiveId == myHive);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task ReadAsync_excludes_flights_that_have_already_ended()
    {
        string root = Path.Combine(Path.GetTempPath(), "world-presence-" + Guid.NewGuid());
        var repo = NewRepo(root);
        try
        {
            var clock = new Clock(0);
            var collection = new WorldResourceCollectionService(repo, clock, Options());
            (Guid otherPlayer, Guid otherHive) = (Guid.NewGuid(), Guid.NewGuid());
            await collection.LaunchAsync(otherPlayer, otherHive, "res_pollen_core", new(1, 0, 0, 0, "k1"));

            clock.AdvanceSeconds(9999);
            var presence = new WorldPresenceService(repo, clock);
            WorldPresenceSnapshot snapshot = await presence.ReadAsync(Guid.NewGuid());
            Assert.Empty(snapshot.Sightings);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private sealed class Clock(double startSeconds) : IServerClock
    {
        private DateTimeOffset current = DateTimeOffset.Parse("2026-07-31T12:00:00Z").AddSeconds(startSeconds);
        public DateTimeOffset UtcNow => current;
        public void AdvanceSeconds(double seconds) => current = current.AddSeconds(seconds);
    }
}
