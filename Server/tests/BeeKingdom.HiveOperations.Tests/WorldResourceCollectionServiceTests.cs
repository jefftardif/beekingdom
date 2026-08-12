using System.Linq;
using BeeKingdom.HiveOperations;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class WorldResourceCollectionServiceTests
{
    private static WorldResourceCollectionOptions Options() => new()
    {
        Enabled = true,
        CatalogVersion = "v1",
        Catalog =
        [
            new("res_pollen_core", "pollen", "rich", 80, TimeSpan.FromSeconds(90), TimeSpan.FromMinutes(4), "Champ de pollen"),
            new("res_wax_core", "wax", "medium", 30, TimeSpan.FromSeconds(45), TimeSpan.FromMinutes(2), "Depot de cire")
        ]
    };

    private static (Guid, Guid, DurableJsonHiveStateRepository) NewRepo(long guardians = 10, long wingrunners = 10, long darters = 10)
    {
        Guid p = Guid.NewGuid(), h = Guid.NewGuid();
        string root = Path.Combine(Path.GetTempPath(), "world-res-" + Guid.NewGuid());
        var repo = new DurableJsonHiveStateRepository(root, (x, y) => new PlayerHiveState(x, y, 10, 0,
            new Dictionary<string, ResourceBalance> { ["honey"] = new(0, 1_000_000), ["pollen"] = new(0, 1_000_000), ["wax"] = new(0, 1_000_000) },
            new Dictionary<string, int>(), [], new(),
            DoctrineRoster: new DoctrineRosterState(0, new Dictionary<string, long> { ["guardians"] = guardians, ["wingrunners"] = wingrunners, ["darters"] = darters }, null, new())));
        return (p, h, repo);
    }

    [Fact]
    public async Task LaunchIsBlockedWhileAnotherFlightIsActive()
    {
        (Guid p, Guid h, var repo) = NewRepo();
        var service = new WorldResourceCollectionService(repo, new Clock(0), Options());
        WorldResourceCollectionResult first = await service.LaunchAsync(p, h, "res_pollen_core", new(1, 0, 0, 0, "k1"));
        Assert.True(first.Succeeded, first.Code);
        WorldResourceCollectionResult second = await service.LaunchAsync(p, h, "res_wax_core", new(1, 0, 0, first.Snapshot.Revision, "k2"));
        Assert.False(second.Succeeded);
        Assert.Equal("game.world_resource_busy", second.Code);
    }

    [Fact]
    public async Task ClaimBeforeDurationElapsedIsRejected()
    {
        (Guid p, Guid h, var repo) = NewRepo();
        var clock = new Clock(0);
        var service = new WorldResourceCollectionService(repo, clock, Options());
        WorldResourceCollectionResult launch = await service.LaunchAsync(p, h, "res_pollen_core", new(1, 0, 0, 0, "k1"));
        Guid flightId = launch.Snapshot.Active!.FlightId;
        WorldResourceCollectionResult tooEarly = await service.ClaimAsync(p, h, flightId, new(launch.Snapshot.Revision, "k2"));
        Assert.False(tooEarly.Succeeded);
        Assert.Equal("game.world_resource_not_ready", tooEarly.Code);
    }

    [Fact]
    public async Task ClaimCreditsResourceAndStartsCooldownThenNodeIsUnavailableUntilCooldownEnds()
    {
        (Guid p, Guid h, var repo) = NewRepo();
        var clock = new Clock(0);
        var service = new WorldResourceCollectionService(repo, clock, Options());
        WorldResourceCollectionResult launch = await service.LaunchAsync(p, h, "res_pollen_core", new(1, 0, 0, 0, "k1"));
        Guid flightId = launch.Snapshot.Active!.FlightId;
        clock.AdvanceSeconds(90);
        WorldResourceCollectionResult claim = await service.ClaimAsync(p, h, flightId, new(launch.Snapshot.Revision, "k2"));
        Assert.True(claim.Succeeded);
        Assert.Equal("game.world_resource_claimed", claim.Code);
        long expectedCredited = ExpectedYield("res_pollen_core", 80, clock.UtcNow);
        Assert.Equal(expectedCredited, claim.ClaimReceipt!.CreditedAmount);
        Assert.Null(claim.Snapshot.Active);
        WorldResourceNodeReadModel pollenNode = claim.Snapshot.Nodes.Single(n => n.NodeId == "res_pollen_core");
        Assert.False(pollenNode.Ready);
        WorldResourceCollectionResult relaunchTooSoon = await service.LaunchAsync(p, h, "res_pollen_core", new(1, 0, 0, claim.Snapshot.Revision, "k3"));
        Assert.False(relaunchTooSoon.Succeeded);
        Assert.Equal("game.world_resource_cooling_down", relaunchTooSoon.Code);
        clock.AdvanceSeconds(240);
        WorldResourceCollectionResult relaunchReady = await service.LaunchAsync(p, h, "res_pollen_core", new(1, 0, 0, claim.Snapshot.Revision, "k4"));
        Assert.True(relaunchReady.Succeeded);
    }

    [Fact]
    public async Task LaunchReplaysIdenticallyOnSameIdempotencyKey()
    {
        (Guid p, Guid h, var repo) = NewRepo();
        var service = new WorldResourceCollectionService(repo, new Clock(0), Options());
        WorldResourceCollectionResult first = await service.LaunchAsync(p, h, "res_pollen_core", new(1, 0, 0, 0, "same-key"));
        WorldResourceCollectionResult replay = await service.LaunchAsync(p, h, "res_pollen_core", new(1, 0, 0, 0, "same-key"));
        Assert.True(first.Succeeded);
        Assert.True(replay.Succeeded);
        Assert.Equal(first.Snapshot.Active!.FlightId, replay.Snapshot.Active!.FlightId);
    }

    [Fact]
    public async Task LaunchRespectsResourceCapacityWhenCrediting()
    {
        Guid p = Guid.NewGuid(), h = Guid.NewGuid();
        string root = Path.Combine(Path.GetTempPath(), "world-res-cap-" + Guid.NewGuid());
        var repo = new DurableJsonHiveStateRepository(root, (x, y) => new PlayerHiveState(x, y, 10, 0,
            new Dictionary<string, ResourceBalance> { ["honey"] = new(0, 1_000_000), ["pollen"] = new(70, 100), ["wax"] = new(0, 1_000_000) },
            new Dictionary<string, int>(), [], new(),
            DoctrineRoster: new DoctrineRosterState(0, new Dictionary<string, long> { ["guardians"] = 10, ["wingrunners"] = 10, ["darters"] = 10 }, null, new())));
        var clock = new Clock(0);
        var service = new WorldResourceCollectionService(repo, clock, Options());
        WorldResourceCollectionResult launch = await service.LaunchAsync(p, h, "res_pollen_core", new(1, 0, 0, 0, "k1"));
        clock.AdvanceSeconds(90);
        WorldResourceCollectionResult claim = await service.ClaimAsync(p, h, launch.Snapshot.Active!.FlightId, new(launch.Snapshot.Revision, "k2"));
        Assert.True(claim.Succeeded);
        // Capacity (100) caps the credited amount regardless of any daily-focus bonus on the
        // raw yield (30, possibly boosted to 45) - the resulting balance must never exceed capacity.
        Assert.Equal(100, claim.ClaimReceipt!.ResultingBalance.Amount);
        Assert.Equal(30, claim.ClaimReceipt.CreditedAmount);
    }

    // Calcule le rendement attendu en tenant compte du bonus "cible du jour" (demande de Jeff,
    // 2026-07-31) - evite que ces tests deviennent fragiles/dependants du jour d'execution.
    private static long ExpectedYield(string nodeId, long baseYield, DateTimeOffset now)
    {
        bool isFeatured = string.Equals(nodeId, DailyFocusCatalog.FeaturedWorldResourceNodeId(now, ["res_pollen_core", "res_wax_core"]), StringComparison.Ordinal);
        return isFeatured ? DailyFocusCatalog.ApplyRewardBonus(baseYield) : baseYield;
    }

    [Fact]
    public async Task Claim_applies_daily_focus_bonus_only_to_the_featured_node()
    {
        (Guid p, Guid h, var repo) = NewRepo();
        var clock = new Clock(0);
        List<string> nodeIds = ["res_pollen_core", "res_wax_core"];
        string featuredNodeId = DailyFocusCatalog.FeaturedWorldResourceNodeId(clock.UtcNow, nodeIds)!;
        string otherNodeId = featuredNodeId == "res_pollen_core" ? "res_wax_core" : "res_pollen_core";
        var service = new WorldResourceCollectionService(repo, clock, Options());

        WorldResourceCollectionSnapshot snapshot = await service.ReadAsync(p, h);
        Assert.Equal(featuredNodeId, snapshot.FeaturedNodeId);
        Assert.True(snapshot.Nodes.Single(n => n.NodeId == featuredNodeId).IsDailyFocus);
        Assert.False(snapshot.Nodes.Single(n => n.NodeId == otherNodeId).IsDailyFocus);

        long baseYield = Options().Catalog.Single(n => n.NodeId == featuredNodeId).Yield;
        TimeSpan duration = Options().Catalog.Single(n => n.NodeId == featuredNodeId).Duration;
        WorldResourceCollectionResult launch = await service.LaunchAsync(p, h, featuredNodeId, new(1, 0, 0, 0, "k1"));
        Assert.True(launch.Succeeded, launch.Code);
        clock.AdvanceSeconds(duration.TotalSeconds);
        WorldResourceCollectionResult claim = await service.ClaimAsync(p, h, launch.Snapshot.Active!.FlightId, new(launch.Snapshot.Revision, "k2"));

        Assert.True(claim.Succeeded, claim.Code);
        Assert.True(claim.ClaimReceipt!.DailyFocusApplied);
        Assert.Equal(DailyFocusCatalog.ApplyRewardBonus(baseYield), claim.ClaimReceipt.CreditedAmount);
    }

    [Fact]
    public async Task Claim_applies_world_event_yield_bonus_only_to_the_matching_resource()
    {
        DateTimeOffset baseTime = DateTimeOffset.Parse("2026-07-31T12:00:00Z");
        // L'evenement mondial change toutes les 4h - on avance jusqu'a une fenetre "meteo" dont la
        // ressource visee existe dans ce catalogue de test a 2 noeuds (pollen/cire, pas de miel ici).
        DateTimeOffset t = baseTime;
        while (WorldEventCatalog.Active(t).Kind != WorldEventKind.Weather ||
               (WorldEventCatalog.Active(t).TargetKey != "pollen" && WorldEventCatalog.Active(t).TargetKey != "wax"))
            t = t.AddHours(4);
        ActiveWorldEvent activeEvent = WorldEventCatalog.Active(t);

        (Guid p, Guid h, var repo) = NewRepo();
        var clock = new Clock((t - baseTime).TotalSeconds);
        List<string> nodeIds = ["res_pollen_core", "res_wax_core"];
        string matchingNodeId = Options().Catalog.Single(n => n.ResourceKey == activeEvent.TargetKey).NodeId;
        string mismatchedNodeId = Options().Catalog.Single(n => n.NodeId != matchingNodeId).NodeId;
        var service = new WorldResourceCollectionService(repo, clock, Options());

        long baseYield = Options().Catalog.Single(n => n.NodeId == matchingNodeId).Yield;
        bool matchingIsDailyFocus = string.Equals(matchingNodeId, DailyFocusCatalog.FeaturedWorldResourceNodeId(clock.UtcNow, nodeIds), StringComparison.Ordinal);
        long expectedYield = matchingIsDailyFocus ? DailyFocusCatalog.ApplyRewardBonus(baseYield) : baseYield;
        expectedYield = WorldEventCatalog.ApplyBonusBp(expectedYield, activeEvent.BonusBp);

        TimeSpan duration = Options().Catalog.Single(n => n.NodeId == matchingNodeId).Duration;
        WorldResourceCollectionResult launch = await service.LaunchAsync(p, h, matchingNodeId, new(1, 0, 0, 0, "k1"));
        Assert.True(launch.Succeeded, launch.Code);
        clock.AdvanceSeconds(duration.TotalSeconds);
        WorldResourceCollectionResult claim = await service.ClaimAsync(p, h, launch.Snapshot.Active!.FlightId, new(launch.Snapshot.Revision, "k2"));

        Assert.True(claim.Succeeded, claim.Code);
        Assert.True(claim.ClaimReceipt!.WorldEventApplied);
        Assert.Equal(activeEvent.Key, claim.ClaimReceipt.WorldEventKey);
        Assert.Equal(expectedYield, claim.ClaimReceipt.CreditedAmount);

        long mismatchedBaseYield = Options().Catalog.Single(n => n.NodeId == mismatchedNodeId).Yield;
        bool mismatchedIsDailyFocus = string.Equals(mismatchedNodeId, DailyFocusCatalog.FeaturedWorldResourceNodeId(clock.UtcNow, nodeIds), StringComparison.Ordinal);
        long expectedMismatchedYield = mismatchedIsDailyFocus ? DailyFocusCatalog.ApplyRewardBonus(mismatchedBaseYield) : mismatchedBaseYield;
        TimeSpan mismatchedDuration = Options().Catalog.Single(n => n.NodeId == mismatchedNodeId).Duration;
        WorldResourceCollectionResult launchOther = await service.LaunchAsync(p, h, mismatchedNodeId, new(1, 0, 0, claim.Snapshot.Revision, "k3"));
        Assert.True(launchOther.Succeeded, launchOther.Code);
        clock.AdvanceSeconds(mismatchedDuration.TotalSeconds);
        WorldResourceCollectionResult claimOther = await service.ClaimAsync(p, h, launchOther.Snapshot.Active!.FlightId, new(launchOther.Snapshot.Revision, "k4"));
        Assert.True(claimOther.Succeeded, claimOther.Code);
        Assert.False(claimOther.ClaimReceipt!.WorldEventApplied);
        Assert.Equal(expectedMismatchedYield, claimOther.ClaimReceipt.CreditedAmount);
    }

    // --- Escouade reellement engagee (demande de Jeff, 2026-08-01) : premiere brique de
    // l'architecture de deploiement reutilisable plus tard pour le PvP, les raids, les renforts et
    // l'occupation de points d'interet. ---

    [Fact]
    public async Task Launch_commits_the_requested_troops_and_reserves_them_from_the_available_roster()
    {
        (Guid p, Guid h, var repo) = NewRepo(guardians: 10, wingrunners: 10, darters: 10);
        var service = new WorldResourceCollectionService(repo, new Clock(0), Options());

        WorldResourceCollectionResult launch = await service.LaunchAsync(p, h, "res_pollen_core", new(3, 1, 0, 0, "k1"));
        Assert.True(launch.Succeeded, launch.Code);
        Assert.Equal(3L, launch.Snapshot.Active!.CommittedTroops["guardians"]);
        Assert.Equal(1L, launch.Snapshot.Active.CommittedTroops["wingrunners"]);
        Assert.Equal(7L, launch.Snapshot.AvailableRoster!["guardians"]);
        Assert.Equal(9L, launch.Snapshot.AvailableRoster["wingrunners"]);
        Assert.Equal(10L, launch.Snapshot.AvailableRoster["darters"]);
    }

    [Fact]
    public async Task Launch_is_rejected_when_requested_troops_exceed_the_available_roster()
    {
        (Guid p, Guid h, var repo) = NewRepo(guardians: 2, wingrunners: 0, darters: 0);
        var service = new WorldResourceCollectionService(repo, new Clock(0), Options());

        WorldResourceCollectionResult launch = await service.LaunchAsync(p, h, "res_pollen_core", new(3, 0, 0, 0, "k1"));
        Assert.False(launch.Succeeded);
        Assert.Equal("game.world_resource_insufficient_troops", launch.Code);
        Assert.Null(launch.Snapshot.Active);
    }

    [Fact]
    public async Task Launch_is_rejected_when_no_troop_is_committed()
    {
        (Guid p, Guid h, var repo) = NewRepo();
        var service = new WorldResourceCollectionService(repo, new Clock(0), Options());

        WorldResourceCollectionResult launch = await service.LaunchAsync(p, h, "res_pollen_core", new(0, 0, 0, 0, "k1"));
        Assert.False(launch.Succeeded);
        Assert.Equal("game.invalid_request", launch.Code);
    }

    [Fact]
    public async Task Recall_returns_the_committed_troops_immediately_with_no_reward_and_no_cooldown()
    {
        (Guid p, Guid h, var repo) = NewRepo(guardians: 5);
        var clock = new Clock(0);
        var service = new WorldResourceCollectionService(repo, clock, Options());

        WorldResourceCollectionResult launch = await service.LaunchAsync(p, h, "res_pollen_core", new(4, 0, 0, 0, "k1"));
        Assert.True(launch.Succeeded, launch.Code);
        Assert.Equal(1L, launch.Snapshot.AvailableRoster!["guardians"]);

        WorldResourceCollectionResult recall = await service.RecallAsync(p, h, launch.Snapshot.Active!.FlightId, new(launch.Snapshot.Revision, "k2"));
        Assert.True(recall.Succeeded, recall.Code);
        Assert.Equal("game.world_resource_recalled", recall.Code);
        Assert.Null(recall.Snapshot.Active);
        Assert.Equal(5L, recall.Snapshot.AvailableRoster!["guardians"]);
        Assert.Null(recall.ClaimReceipt);
        // Aucun malus de repos : le noeud redevient immediatement disponible.
        Assert.True(recall.Snapshot.Nodes.Single(n => n.NodeId == "res_pollen_core").Ready);

        WorldResourceCollectionResult relaunch = await service.LaunchAsync(p, h, "res_pollen_core", new(5, 0, 0, recall.Snapshot.Revision, "k3"));
        Assert.True(relaunch.Succeeded, relaunch.Code);
    }

    [Fact]
    public async Task Recall_with_a_stale_revision_is_rejected()
    {
        (Guid p, Guid h, var repo) = NewRepo();
        var service = new WorldResourceCollectionService(repo, new Clock(0), Options());
        WorldResourceCollectionResult launch = await service.LaunchAsync(p, h, "res_pollen_core", new(1, 0, 0, 0, "k1"));

        WorldResourceCollectionResult recall = await service.RecallAsync(p, h, launch.Snapshot.Active!.FlightId, new(launch.Snapshot.Revision + 1, "k2"));
        Assert.False(recall.Succeeded);
        Assert.Equal("game.revision_conflict", recall.Code);
    }

    // Comptabilite partagee entre systemes de terrain (HiveTroopDeploymentAccounting) : des
    // troupes deja engagees en Combat Patrol ne peuvent pas etre re-engagees en Collecte mondiale,
    // et inversement - garantit qu'aucune abeille n'est jamais comptee deux fois.
    [Fact]
    public async Task Launch_is_rejected_when_the_troops_are_already_committed_to_a_combat_patrol()
    {
        // 8 gardiennes suffisent a franchir le seuil de blocage du palier 1 malgre le malus de
        // desavantage (wingrunners > guardians) - aucune n'est laissee libre pour la collecte.
        (Guid p, Guid h, var repo) = NewRepo(guardians: 8, wingrunners: 0, darters: 0);
        var patrolService = new CombatPatrolService(repo, new Clock(0));
        CombatPatrolResult patrolLaunch = await patrolService.LaunchAsync(new(p, h, 1, 8, 0, 0, 0, "patrol-launch"), default);
        Assert.True(patrolLaunch.Succeeded, patrolLaunch.Code);

        var collectionService = new WorldResourceCollectionService(repo, new Clock(0), Options());
        WorldResourceCollectionResult launch = await collectionService.LaunchAsync(p, h, "res_pollen_core", new(1, 0, 0, 0, "collect-launch"));
        Assert.False(launch.Succeeded);
        Assert.Equal("game.world_resource_insufficient_troops", launch.Code);
    }

    private sealed class Clock(double startSeconds) : IServerClock
    {
        private DateTimeOffset current = DateTimeOffset.Parse("2026-07-31T12:00:00Z").AddSeconds(startSeconds);
        public DateTimeOffset UtcNow => current;
        public void AdvanceSeconds(double seconds) => current = current.AddSeconds(seconds);
    }
}
