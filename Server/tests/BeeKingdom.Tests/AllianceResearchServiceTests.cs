using BeeKingdom.Alliance;
using BeeKingdom.Alliance.Configuration;
using BeeKingdom.Alliance.Models;
using BeeKingdom.Alliance.Repositories;
using BeeKingdom.Alliance.Research;
using BeeKingdom.HiveOperations;
using BeeKingdom.Shared.ValueObjects;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Tests;

// M051-CL: exercises AllianceResearchService against the SAME PlayerHiveState resource-debit shape
// every other paid action in this codebase mutates (see e.g. AllianceHelpServiceTests' own class
// comment for the equivalent convention) - never a parallel/fabricated resource model. Runs against
// InMemory repositories (InMemoryAllianceRepository shared with AllianceService, so real membership
// truth backs every test) with a real AllianceService for alliance/membership setup.
public sealed class AllianceResearchServiceTests
{
    private static PlayerId NewPlayer() => PlayerId.New();

    private sealed record Fixture(
        AllianceService Alliances,
        AllianceResearchService Research,
        AllianceResearchBonusResolver BonusResolver,
        MemoryHiveStateRepository HiveStates,
        TestClock Clock,
        InMemoryAllianceRepository AllianceRepository,
        InMemoryAllianceResearchRepository ResearchRepository);

    private static Fixture CreateFixture()
    {
        var allianceOptions = Options.Create(new AllianceOptions { Enabled = true, MaxMembers = 100 });
        var allianceRepository = new InMemoryAllianceRepository();
        var researchRepository = new InMemoryAllianceResearchRepository();
        var hiveStates = new MemoryHiveStateRepository();
        var clock = new TestClock(DateTimeOffset.UtcNow);
        var researchOptions = Options.Create(new AllianceResearchOptions { Enabled = true });

        var alliances = new AllianceService(
            allianceRepository, new InMemoryAllianceActivityRepository(), new InMemoryAllianceDiplomacyRepository(), new InMemoryAllianceWarRepository(),
            allianceOptions);
        var research = new AllianceResearchService(allianceRepository, researchRepository, hiveStates, researchOptions, clock);
        var bonusResolver = new AllianceResearchBonusResolver(allianceRepository, researchRepository);

        return new Fixture(alliances, research, bonusResolver, hiveStates, clock, allianceRepository, researchRepository);
    }

    private static (AllianceId AllianceId, Guid MemberHiveId) SetUpAllianceOfTwo(Fixture fixture, PlayerId leader, PlayerId member, long honey = 10_000, long pollen = 10_000, long wax = 10_000)
    {
        AllianceEntity alliance = fixture.Alliances.CreateAlliance(leader, new CreateAllianceRequest("Golden Hive", "GLD", "desc", "fr-CA", "", AllianceJoinMode.Open, "create-" + leader.Value)).Alliance;
        fixture.AllianceRepository.SaveMembership(new AllianceMembership
        {
            AllianceId = alliance.AllianceId,
            PlayerId = member,
            Role = AllianceRole.Member,
            JoinedAtUtc = fixture.Clock.UtcNow,
            LastRoleChangedAtUtc = fixture.Clock.UtcNow,
            Revision = 0
        });
        Guid leaderHiveId = Guid.NewGuid(), memberHiveId = Guid.NewGuid();
        fixture.HiveStates.Seed(SeedState(leader.Value, leaderHiveId, honey, pollen, wax));
        fixture.HiveStates.Seed(SeedState(member.Value, memberHiveId, honey, pollen, wax));
        return (alliance.AllianceId, memberHiveId);
    }

    private static PlayerHiveState SeedState(Guid playerId, Guid hiveId, long honey, long pollen, long wax) => new(
        playerId, hiveId, 10, 0,
        new Dictionary<string, ResourceBalance> { ["honey"] = new(honey, 1_000_000), ["pollen"] = new(pollen, 1_000_000), ["wax"] = new(wax, 1_000_000) },
        new(), new(), new());

    private const string TechA1 = "prosperity_shared_reserves_i";
    private const string TechA2 = "prosperity_shared_reserves_ii";

    // ---------------- 1: read ----------------

    [Test]
    public async Task GetSnapshot_Member_ListsAllAlphaTechnologies()
    {
        Fixture fx = CreateFixture();
        PlayerId leader = NewPlayer(), member = NewPlayer();
        (_, Guid hiveId) = SetUpAllianceOfTwo(fx, leader, member);
        _ = hiveId;

        AllianceResearchReadSnapshot snapshot = await fx.Research.GetSnapshotAsync(leader);

        Assert.That(snapshot.Technologies.Count, Is.EqualTo(AllianceResearchCatalog.Technologies.Count));
        Assert.That(snapshot.Technologies.Single(t => t.TechnologyId == TechA1).Available, Is.True);
        Assert.That(snapshot.Technologies.Single(t => t.TechnologyId == TechA2).Locked, Is.True, "tier 2 must be locked until tier 1 completes");
    }

    // ---------------- 2: non-member ----------------

    [Test]
    public async Task Donate_NonMember_Rejected()
    {
        Fixture fx = CreateFixture();
        PlayerId leader = NewPlayer(), member = NewPlayer(), outsider = NewPlayer();
        (_, _) = SetUpAllianceOfTwo(fx, leader, member);
        fx.HiveStates.Seed(SeedState(outsider.Value, Guid.NewGuid(), 10_000, 10_000, 10_000));
        Guid outsiderHiveId = (await fx.HiveStates.ListHiveIdsAsync(outsider.Value)).Single();

        AllianceResearchDonateResult result = await fx.Research.DonateAsync(outsider, new DonateToAllianceResearchCommand(outsiderHiveId, TechA1, "req-1"));

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Code, Is.EqualTo("not_a_member"));
    }

    // ---------------- 3: locked ----------------

    [Test]
    public async Task Donate_LockedTechnology_Rejected()
    {
        Fixture fx = CreateFixture();
        PlayerId leader = NewPlayer(), member = NewPlayer();
        (_, Guid memberHiveId) = SetUpAllianceOfTwo(fx, leader, member);

        AllianceResearchDonateResult result = await fx.Research.DonateAsync(member, new DonateToAllianceResearchCommand(memberHiveId, TechA2, "req-1"));

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Code, Is.EqualTo("technology_locked"));
    }

    // ---------------- 4: completed ----------------

    [Test]
    public async Task Donate_CompletedTechnology_Rejected()
    {
        Fixture fx = CreateFixture();
        PlayerId leader = NewPlayer(), member = NewPlayer();
        (_, Guid memberHiveId) = SetUpAllianceOfTwo(fx, leader, member);
        await CompleteTechnology(fx, member, memberHiveId, TechA1);

        AllianceResearchDonateResult result = await fx.Research.DonateAsync(member, new DonateToAllianceResearchCommand(memberHiveId, TechA1, "req-after-complete"));

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Code, Is.EqualTo("technology_completed"));
    }

    // ---------------- 5: insufficient resources ----------------

    [Test]
    public async Task Donate_InsufficientResources_Rejected()
    {
        Fixture fx = CreateFixture();
        PlayerId leader = NewPlayer(), member = NewPlayer();
        (_, Guid memberHiveId) = SetUpAllianceOfTwo(fx, leader, member, honey: 1, pollen: 1, wax: 1);

        AllianceResearchDonateResult result = await fx.Research.DonateAsync(member, new DonateToAllianceResearchCommand(memberHiveId, TechA1, "req-1"));

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Code, Is.EqualTo("insufficient_resources"));
        PlayerHiveState state = (await fx.HiveStates.ReadAsync(member.Value, memberHiveId))!;
        Assert.That(state.Resources["honey"].Amount, Is.EqualTo(1), "a rejected donation must never debit anything");
    }

    // ---------------- 6/7/8: successful donation ----------------

    [Test]
    public async Task Donate_Success_DebitsResourcesIncrementsProgressAndContribution()
    {
        Fixture fx = CreateFixture();
        PlayerId leader = NewPlayer(), member = NewPlayer();
        (_, Guid memberHiveId) = SetUpAllianceOfTwo(fx, leader, member);
        AllianceResearchCatalog.TryGet(TechA1, out AllianceResearchCatalog.TechnologyDefinition definition);

        AllianceResearchDonateResult result = await fx.Research.DonateAsync(member, new DonateToAllianceResearchCommand(memberHiveId, TechA1, "req-1"));

        Assert.That(result.Succeeded, Is.True);
        PlayerHiveState state = (await fx.HiveStates.ReadAsync(member.Value, memberHiveId))!;
        foreach ((string resourceKey, long cost) in definition.DonationCost)
            Assert.That(state.Resources[resourceKey].Amount, Is.EqualTo(10_000 - cost), $"{resourceKey} must be debited by the real donation cost");

        AllianceTechnologyReadModel tech = result.Snapshot!.Technologies.Single(t => t.TechnologyId == TechA1);
        Assert.That(tech.CurrentProgress, Is.EqualTo(definition.DonationProgressPerDonation));
        Assert.That(result.Snapshot.MyContributionPoints, Is.EqualTo(definition.DonationProgressPerDonation));
        Assert.That(result.Snapshot.MyDonationCount, Is.EqualTo(1));
    }

    // ---------------- 9: persistence ----------------

    [Test]
    public async Task Donate_Success_PersistsInRepository()
    {
        Fixture fx = CreateFixture();
        PlayerId leader = NewPlayer(), member = NewPlayer();
        (AllianceId allianceId, Guid memberHiveId) = SetUpAllianceOfTwo(fx, leader, member);

        await fx.Research.DonateAsync(member, new DonateToAllianceResearchCommand(memberHiveId, TechA1, "req-1"));

        AllianceResearchState? persisted = await fx.ResearchRepository.ReadAsync(allianceId.Value);
        Assert.That(persisted, Is.Not.Null);
        Assert.That(persisted!.Technologies[TechA1].CurrentProgress, Is.EqualTo(10));
        // A second, independent read (Jeff donates, Stara must see the increase) - both go through
        // the same AllianceResearchService reading the same shared aggregate, never a per-player copy.
        AllianceResearchReadSnapshot leaderView = await fx.Research.GetSnapshotAsync(leader);
        Assert.That(leaderView.Technologies.Single(t => t.TechnologyId == TechA1).CurrentProgress, Is.EqualTo(10));
    }

    // ---------------- 10: concurrency ----------------

    [Test]
    public async Task ConcurrentDonations_FromTwoMembers_NeitherProgressIsLost()
    {
        Fixture fx = CreateFixture();
        PlayerId leader = NewPlayer(), member = NewPlayer();
        (_, Guid memberHiveId) = SetUpAllianceOfTwo(fx, leader, member);
        Guid leaderHiveId = (await fx.HiveStates.ListHiveIdsAsync(leader.Value)).Single();

        Task<AllianceResearchDonateResult> a = fx.Research.DonateAsync(leader, new DonateToAllianceResearchCommand(leaderHiveId, TechA1, "leader-1"));
        Task<AllianceResearchDonateResult> b = fx.Research.DonateAsync(member, new DonateToAllianceResearchCommand(memberHiveId, TechA1, "member-1"));
        await Task.WhenAll(a, b);

        Assert.That(a.Result.Succeeded, Is.True);
        Assert.That(b.Result.Succeeded, Is.True);
        AllianceTechnologyProgress progress = (await fx.ResearchRepository.ReadAsync(fx.AllianceRepository.GetActiveMembershipForPlayer(leader)!.AllianceId.Value))!.Technologies[TechA1];
        Assert.That(progress.CurrentProgress, Is.EqualTo(20), "two concurrent +10 donations must both land - no lost update");
    }

    // ---------------- 11: completion exactly once ----------------

    [Test]
    public async Task Completion_HappensExactlyOnce_AndClampsAtRequiredProgress()
    {
        Fixture fx = CreateFixture();
        PlayerId leader = NewPlayer(), member = NewPlayer();
        (AllianceId allianceId, Guid memberHiveId) = SetUpAllianceOfTwo(fx, leader, member, honey: 1_000_000, pollen: 1_000_000, wax: 1_000_000);
        AllianceResearchCatalog.TryGet(TechA1, out AllianceResearchCatalog.TechnologyDefinition definition);
        long donationsToComplete = definition.RequiredProgress / definition.DonationProgressPerDonation;

        DateTimeOffset? completedAt = null;
        for (int i = 0; i < donationsToComplete; i++)
        {
            AllianceResearchDonateResult result = await fx.Research.DonateAsync(member, new DonateToAllianceResearchCommand(memberHiveId, TechA1, $"req-{i}"));
            AllianceTechnologyReadModel tech = result.Snapshot!.Technologies.Single(t => t.TechnologyId == TechA1);
            if (tech.Completed)
            {
                Assert.That(completedAt, Is.Null, "completion must be observed exactly once");
                completedAt = tech.CompletedAtUtc;
            }
        }

        Assert.That(completedAt, Is.Not.Null);
        AllianceResearchState final = (await fx.ResearchRepository.ReadAsync(allianceId.Value))!;
        Assert.That(final.Technologies[TechA1].CurrentProgress, Is.EqualTo(definition.RequiredProgress), "progress must never exceed RequiredProgress");
    }

    // ---------------- 12: prerequisite unlock ----------------

    [Test]
    public async Task PrerequisiteUnlock_TierTwoBecomesAvailableAfterTierOneCompletes()
    {
        Fixture fx = CreateFixture();
        PlayerId leader = NewPlayer(), member = NewPlayer();
        (_, Guid memberHiveId) = SetUpAllianceOfTwo(fx, leader, member);

        AllianceResearchReadSnapshot before = await fx.Research.GetSnapshotAsync(member);
        Assert.That(before.Technologies.Single(t => t.TechnologyId == TechA2).Locked, Is.True);

        await CompleteTechnology(fx, member, memberHiveId, TechA1);

        AllianceResearchReadSnapshot after = await fx.Research.GetSnapshotAsync(member);
        Assert.That(after.Technologies.Single(t => t.TechnologyId == TechA2).Locked, Is.False);
        Assert.That(after.Technologies.Single(t => t.TechnologyId == TechA2).Available, Is.True);
    }

    // ---------------- 13: idempotent retry ----------------

    [Test]
    public async Task Donate_SameClientRequestIdRetried_DoesNotDoubleCharge()
    {
        Fixture fx = CreateFixture();
        PlayerId leader = NewPlayer(), member = NewPlayer();
        (_, Guid memberHiveId) = SetUpAllianceOfTwo(fx, leader, member);

        AllianceResearchDonateResult first = await fx.Research.DonateAsync(member, new DonateToAllianceResearchCommand(memberHiveId, TechA1, "same-req"));
        AllianceResearchDonateResult retry = await fx.Research.DonateAsync(member, new DonateToAllianceResearchCommand(memberHiveId, TechA1, "same-req"));

        Assert.That(first.Succeeded, Is.True);
        Assert.That(retry.Succeeded, Is.True);
        PlayerHiveState state = (await fx.HiveStates.ReadAsync(member.Value, memberHiveId))!;
        AllianceResearchCatalog.TryGet(TechA1, out AllianceResearchCatalog.TechnologyDefinition definition);
        foreach ((string resourceKey, long cost) in definition.DonationCost)
            Assert.That(state.Resources[resourceKey].Amount, Is.EqualTo(10_000 - cost), $"{resourceKey} must be debited exactly once across the retry");
        AllianceResearchReadSnapshot snapshot = await fx.Research.GetSnapshotAsync(member);
        Assert.That(snapshot.Technologies.Single(t => t.TechnologyId == TechA1).CurrentProgress, Is.EqualTo(definition.DonationProgressPerDonation), "progress must only advance once across the retry");
        Assert.That(snapshot.MyDonationCount, Is.EqualTo(1));
    }

    // ---------------- M051C-CL: real bonus magnitude reaches the read contract ----------------

    [Test]
    public async Task GetSnapshot_ExposesRealCatalogBonusMagnitudes_NotJustAGenericSummaryKey()
    {
        // M051C-CL: Stage 1 visual certification failed because the client had nowhere to read a
        // real number from - only a generic BonusSummaryKey existed. This proves the read contract
        // now carries the actual AllianceResearchCatalog values, so the UI can format "+X %" from
        // server truth instead of a client-hardcoded number that could drift from the catalog.
        Fixture fx = CreateFixture();
        PlayerId leader = NewPlayer(), member = NewPlayer();
        SetUpAllianceOfTwo(fx, leader, member);
        AllianceResearchCatalog.TryGet(TechA1, out AllianceResearchCatalog.TechnologyDefinition definition);

        AllianceResearchReadSnapshot snapshot = await fx.Research.GetSnapshotAsync(leader);

        AllianceTechnologyReadModel tech = snapshot.Technologies.Single(t => t.TechnologyId == TechA1);
        Assert.That(tech.ProductionBp, Is.EqualTo(definition.ProductionBp));
        Assert.That(tech.CapacityBp, Is.EqualTo(definition.CapacityBp));
        Assert.That(tech.CombatPowerBp, Is.EqualTo(definition.CombatPowerBp));
        Assert.That(tech.ProductionBp, Is.GreaterThan(0), "prosperity_shared_reserves_i must actually carry its real production bonus");
    }

    // ---------------- 14/15: membership bonus semantics ----------------

    [Test]
    public async Task Bonus_LeavingAlliance_NoLongerApplies()
    {
        Fixture fx = CreateFixture();
        PlayerId leader = NewPlayer(), member = NewPlayer();
        (AllianceId allianceId, Guid memberHiveId) = SetUpAllianceOfTwo(fx, leader, member);
        await CompleteTechnology(fx, member, memberHiveId, TechA1);

        AllianceResearchBonus whileJoined = await fx.BonusResolver.ResolveForPlayerAsync(member);
        Assert.That(whileJoined.ProductionBp, Is.GreaterThan(0));

        fx.Alliances.Leave(member);
        AllianceResearchBonus afterLeaving = await fx.BonusResolver.ResolveForPlayerAsync(member);
        Assert.That(afterLeaving, Is.EqualTo(AllianceResearchBonus.None), "a player who left must not keep their former alliance's bonus");
    }

    [Test]
    public async Task Bonus_JoiningAllianceWithCompletedTech_AppliesImmediately()
    {
        Fixture fx = CreateFixture();
        PlayerId leader = NewPlayer(), member = NewPlayer(), newcomer = NewPlayer();
        (AllianceId allianceId, Guid memberHiveId) = SetUpAllianceOfTwo(fx, leader, member);
        await CompleteTechnology(fx, member, memberHiveId, TechA1);

        AllianceResearchBonus beforeJoining = await fx.BonusResolver.ResolveForPlayerAsync(newcomer);
        Assert.That(beforeJoining, Is.EqualTo(AllianceResearchBonus.None));

        fx.AllianceRepository.SaveMembership(new AllianceMembership
        {
            AllianceId = allianceId, PlayerId = newcomer, Role = AllianceRole.Member,
            JoinedAtUtc = fx.Clock.UtcNow, LastRoleChangedAtUtc = fx.Clock.UtcNow, Revision = 0
        });
        AllianceResearchBonus afterJoining = await fx.BonusResolver.ResolveForPlayerAsync(newcomer);
        Assert.That(afterJoining.ProductionBp, Is.GreaterThan(0), "a newcomer must see the alliance's ALREADY completed research bonus immediately");
    }

    private static async Task CompleteTechnology(Fixture fx, PlayerId donor, Guid hiveId, string technologyId)
    {
        AllianceResearchCatalog.TryGet(technologyId, out AllianceResearchCatalog.TechnologyDefinition definition);
        long donations = definition.RequiredProgress / definition.DonationProgressPerDonation;
        for (int i = 0; i < donations; i++)
        {
            AllianceResearchDonateResult result = await fx.Research.DonateAsync(donor, new DonateToAllianceResearchCommand(hiveId, technologyId, $"complete-{technologyId}-{i}"));
            if (!result.Succeeded) throw new InvalidOperationException($"Failed to complete {technologyId} in test setup: {result.Code}");
        }
    }

    private sealed class TestClock(DateTimeOffset value) : IServerClock
    {
        public DateTimeOffset UtcNow { get; } = value;
    }

    // Multi-player-aware in-memory PlayerHiveState repository - same convention as
    // AllianceHelpServiceTests' own MemoryHiveStateRepository.
    private sealed class MemoryHiveStateRepository : IHiveStateRepository
    {
        private readonly object gate = new();
        private readonly Dictionary<(Guid PlayerId, Guid HiveId), PlayerHiveState> states = new();

        public void Seed(PlayerHiveState state) { lock (gate) states[(state.PlayerId, state.HiveId)] = state; }

        public Task<PlayerHiveState> ExecuteAtomicallyAsync(Guid playerId, Guid hiveId, Func<PlayerHiveState, PlayerHiveState> mutation, CancellationToken cancellationToken = default)
        {
            lock (gate)
            {
                PlayerHiveState updated = mutation(states[(playerId, hiveId)]);
                states[(playerId, hiveId)] = updated;
                return Task.FromResult(updated);
            }
        }

        public Task<PlayerHiveState?> ReadAsync(Guid playerId, Guid hiveId, CancellationToken cancellationToken = default)
        {
            lock (gate) return Task.FromResult(states.TryGetValue((playerId, hiveId), out PlayerHiveState? value) ? value : null);
        }

        public Task<IReadOnlyList<Guid>> ListHiveIdsAsync(Guid playerId, CancellationToken cancellationToken = default)
        {
            lock (gate) return Task.FromResult<IReadOnlyList<Guid>>(states.Keys.Where(key => key.PlayerId == playerId).Select(key => key.HiveId).ToList());
        }

        public Task<IReadOnlyList<PlayerHiveState>> ListRecentlyActiveAsync(int limit, CancellationToken cancellationToken = default)
        {
            lock (gate) return Task.FromResult<IReadOnlyList<PlayerHiveState>>(states.Values.Take(limit).ToList());
        }
    }
}
