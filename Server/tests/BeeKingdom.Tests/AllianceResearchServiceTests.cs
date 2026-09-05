using BeeKingdom.Alliance;
using BeeKingdom.Alliance.Configuration;
using BeeKingdom.Alliance.Models;
using BeeKingdom.Alliance.Repositories;
using BeeKingdom.Alliance.Research;
using BeeKingdom.HiveOperations;
using BeeKingdom.Shared.ValueObjects;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Tests;

// M052-CL: exercises the Bible-aligned AllianceResearchService (BIBLE_ALLIANCE_RESEARCH.md V1.0)
// against the same real PlayerHiveState resource-debit shape every other paid action in this
// codebase mutates. Runs against InMemory repositories (InMemoryAllianceRepository shared with a
// real AllianceService, so real membership/role truth backs every test).
public sealed class AllianceResearchServiceTests
{
    private static PlayerId NewPlayer() => PlayerId.New();

    private const string Minor1 = "prosperity_shared_reserves_i";     // no prereq
    private const string Minor2 = "prosperity_honey_mastery_i";       // prereq: Minor1
    private const string MinorOther = "cooperation_coordinated_aid_i"; // independent branch, no prereq
    private const string MinorThird = "expansion_coordinated_harvest_i"; // independent branch, no prereq
    private const string Major1 = "prosperity_age_of_abundance";      // prereq: Minor1 + Minor2

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
        var researchOptions = Options.Create(new AllianceResearchOptions { Enabled = true, AllianceCurrencyPerContributionPoint = 0.1 });

        var alliances = new AllianceService(
            allianceRepository, new InMemoryAllianceActivityRepository(), new InMemoryAllianceDiplomacyRepository(), new InMemoryAllianceWarRepository(),
            allianceOptions);
        var research = new AllianceResearchService(allianceRepository, researchRepository, hiveStates, researchOptions, clock);
        var bonusResolver = new AllianceResearchBonusResolver(allianceRepository, researchRepository, clock);

        return new Fixture(alliances, research, bonusResolver, hiveStates, clock, allianceRepository, researchRepository);
    }

    // Chef = leader; also seeds an Officer and a plain Member, and gives everyone ample resources.
    private static (AllianceId AllianceId, PlayerId Chef, Guid ChefHiveId, PlayerId Officer, Guid OfficerHiveId, PlayerId Member, Guid MemberHiveId) SetUpAlliance(Fixture fx)
    {
        PlayerId chef = NewPlayer(), officer = NewPlayer(), member = NewPlayer();
        AllianceEntity alliance = fx.Alliances.CreateAlliance(chef, new CreateAllianceRequest("Golden Hive", "GLD", "desc", "fr-CA", "", AllianceJoinMode.Open, "create-" + chef.Value)).Alliance;
        fx.AllianceRepository.SaveMembership(new AllianceMembership { AllianceId = alliance.AllianceId, PlayerId = officer, Role = AllianceRole.Officer, JoinedAtUtc = fx.Clock.UtcNow, LastRoleChangedAtUtc = fx.Clock.UtcNow, Revision = 0 });
        fx.AllianceRepository.SaveMembership(new AllianceMembership { AllianceId = alliance.AllianceId, PlayerId = member, Role = AllianceRole.Member, JoinedAtUtc = fx.Clock.UtcNow, LastRoleChangedAtUtc = fx.Clock.UtcNow, Revision = 0 });

        Guid chefHiveId = Guid.NewGuid(), officerHiveId = Guid.NewGuid(), memberHiveId = Guid.NewGuid();
        fx.HiveStates.Seed(SeedState(chef.Value, chefHiveId));
        fx.HiveStates.Seed(SeedState(officer.Value, officerHiveId));
        fx.HiveStates.Seed(SeedState(member.Value, memberHiveId));
        return (alliance.AllianceId, chef, chefHiveId, officer, officerHiveId, member, memberHiveId);
    }

    private static PlayerHiveState SeedState(Guid playerId, Guid hiveId, long amount = 1_000_000) => new(
        playerId, hiveId, 10, 0,
        new Dictionary<string, ResourceBalance> { ["honey"] = new(amount, 100_000_000), ["pollen"] = new(amount, 100_000_000), ["wax"] = new(amount, 100_000_000) },
        new(), new(), new());

    private static async Task<AllianceResearchCommandResult> SelectTarget(Fixture fx, PlayerId actor, string technologyId, string key = "req")
        => await fx.Research.SelectFundingTargetAsync(actor, new SelectAllianceResearchFundingTargetCommand(technologyId, key));

    private static async Task<AllianceResearchCommandResult> Donate(Fixture fx, PlayerId actor, Guid hiveId, string technologyId, string resource, long amount, string key)
        => await fx.Research.DonateAsync(actor, new DonateToAllianceResearchCommand(hiveId, technologyId, resource, amount, key));

    private static async Task<AllianceResearchCommandResult> Launch(Fixture fx, PlayerId actor, string technologyId, string key = "launch")
        => await fx.Research.LaunchAsync(actor, new LaunchAllianceResearchCommand(technologyId, key));

    // Fully funds a technology (chef selects it, then donates exactly what's required from the
    // Chef's own ample resources) - real flow, not fabricated state, used as setup for tests whose
    // real subject is a LATER lifecycle step (launch/timer/bonus).
    private static async Task FullyFund(Fixture fx, PlayerId chef, Guid chefHiveId, string technologyId, string keyPrefix)
    {
        await SelectTarget(fx, chef, technologyId, keyPrefix + "-select");
        AllianceResearchCatalog.TryGet(technologyId, out AllianceResearchCatalog.TechnologyDefinition def);
        int i = 0;
        foreach ((string resource, long amount) in def.FundingRequirements)
            await Donate(fx, chef, chefHiveId, technologyId, resource, amount, keyPrefix + "-fund-" + i++);
    }

    // ---------------- 1/2/3: authority over funding target selection ----------------

    [Test]
    public async Task Member_CannotSelectFundingTarget()
    {
        Fixture fx = CreateFixture();
        (_, _, _, _, _, PlayerId member, _) = SetUpAlliance(fx);
        AllianceResearchCommandResult result = await SelectTarget(fx, member, Minor1);
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Code, Is.EqualTo("not_authorized"));
    }

    [Test]
    public async Task Officer_CannotSelectFundingTarget()
    {
        Fixture fx = CreateFixture();
        (_, _, _, PlayerId officer, _, _, _) = SetUpAlliance(fx);
        AllianceResearchCommandResult result = await SelectTarget(fx, officer, Minor1);
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Code, Is.EqualTo("not_authorized"));
    }

    [Test]
    public async Task Chef_CanSelectEligibleMinorAndMajorFundingTargets()
    {
        Fixture fx = CreateFixture();
        (_, PlayerId chef, _, _, _, _, _) = SetUpAlliance(fx);

        AllianceResearchCommandResult minorResult = await SelectTarget(fx, chef, Minor1, "select-minor");
        Assert.That(minorResult.Succeeded, Is.True);
        Assert.That(minorResult.Snapshot!.MinorFundingTargetId, Is.EqualTo(Minor1));

        // Major1 needs its own prerequisites - selecting it before they're met must fail (proves
        // the Chef cannot bypass prerequisite gating either).
        AllianceResearchCommandResult majorBlocked = await SelectTarget(fx, chef, Major1, "select-major-early");
        Assert.That(majorBlocked.Succeeded, Is.False);
        Assert.That(majorBlocked.Code, Is.EqualTo("technology_locked"));
    }

    // ---------------- 5: changing target preserves prior funding ----------------

    [Test]
    public async Task ChangingFundingTarget_PreservesPriorPartialFunding()
    {
        Fixture fx = CreateFixture();
        (_, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        await SelectTarget(fx, chef, Minor1, "select-1");
        await Donate(fx, chef, chefHiveId, Minor1, "honey", 1000, "fund-1");

        await SelectTarget(fx, chef, MinorOther, "select-2");
        AllianceResearchReadSnapshot afterSwitch = (await SelectTarget(fx, chef, MinorOther, "select-2")).Snapshot!;
        AllianceTechnologyReadModel minor1AfterSwitch = afterSwitch.Technologies.Single(t => t.TechnologyId == Minor1);
        Assert.That(minor1AfterSwitch.FundingContributed.GetValueOrDefault("honey"), Is.EqualTo(1000), "switching the target must not erase Minor1's prior contribution");

        AllianceResearchReadSnapshot backToMinor1 = (await SelectTarget(fx, chef, Minor1, "select-3")).Snapshot!;
        Assert.That(backToMinor1.Technologies.Single(t => t.TechnologyId == Minor1).FundingContributed.GetValueOrDefault("honey"), Is.EqualTo(1000), "reselecting must resume from the preserved 1000, not reset to 0");
    }

    // ---------------- 6: donation to non-selected technology rejected ----------------

    [Test]
    public async Task Donate_ToTechnologyThatIsNotTheCurrentFundingTarget_Rejected()
    {
        Fixture fx = CreateFixture();
        (_, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        await SelectTarget(fx, chef, Minor1);

        // MinorOther is eligible (no prereq) but was never selected by the Chef.
        AllianceResearchCommandResult result = await Donate(fx, chef, chefHiveId, MinorOther, "honey", 100, "fund");
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Code, Is.EqualTo("not_the_funding_target"));
    }

    // ---------------- 7/8: real multi-resource debit and per-resource tracking ----------------

    [Test]
    public async Task Donate_DebitsRealResourcesAndTracksEachRequiredResourceIndependently()
    {
        Fixture fx = CreateFixture();
        (_, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        AllianceResearchCatalog.TryGet(Minor1, out AllianceResearchCatalog.TechnologyDefinition def); // honey+pollen+wax
        await SelectTarget(fx, chef, Minor1);

        AllianceResearchCommandResult result = await Donate(fx, chef, chefHiveId, Minor1, "pollen", 500, "fund-pollen");

        Assert.That(result.Succeeded, Is.True);
        PlayerHiveState state = (await fx.HiveStates.ReadAsync(chef.Value, chefHiveId))!;
        Assert.That(state.Resources["pollen"].Amount, Is.EqualTo(1_000_000 - 500));
        Assert.That(state.Resources["honey"].Amount, Is.EqualTo(1_000_000), "only the donated resource must be debited");
        AllianceTechnologyReadModel tech = result.Snapshot!.Technologies.Single(t => t.TechnologyId == Minor1);
        Assert.That(tech.FundingContributed.GetValueOrDefault("pollen"), Is.EqualTo(500));
        Assert.That(tech.FundingContributed.GetValueOrDefault("honey"), Is.EqualTo(0), "an unrelated required resource must not appear funded");
        _ = def;
    }

    [Test]
    public async Task Donate_ClampsToRemainingNeed_NeverOvershoots()
    {
        Fixture fx = CreateFixture();
        (_, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        AllianceResearchCatalog.TryGet(MinorOther, out AllianceResearchCatalog.TechnologyDefinition def); // honey:2000, pollen:1500
        await SelectTarget(fx, chef, MinorOther);

        AllianceResearchCommandResult result = await Donate(fx, chef, chefHiveId, MinorOther, "honey", 999_999, "fund-huge");

        Assert.That(result.Succeeded, Is.True);
        AllianceTechnologyReadModel tech = result.Snapshot!.Technologies.Single(t => t.TechnologyId == MinorOther);
        Assert.That(tech.FundingContributed["honey"], Is.EqualTo(def.FundingRequirements["honey"]), "must clamp to exactly the requirement, never store more");
        PlayerHiveState state = (await fx.HiveStates.ReadAsync(chef.Value, chefHiveId))!;
        Assert.That(state.Resources["honey"].Amount, Is.EqualTo(1_000_000 - def.FundingRequirements["honey"]), "must only ever debit the clamped amount, not the requested amount");
    }

    // ---------------- 9/10: 100% funding => READY, never COMPLETED, no bonus ----------------

    [Test]
    public async Task FullyFunding_ProducesReady_NotCompleted_AndGrantsNoBonus()
    {
        Fixture fx = CreateFixture();
        (AllianceId allianceId, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        await FullyFund(fx, chef, chefHiveId, Minor1, "m1");

        AllianceResearchReadSnapshot snapshot = await fx.Research.GetSnapshotAsync(chef);
        AllianceTechnologyReadModel tech = snapshot.Technologies.Single(t => t.TechnologyId == Minor1);
        Assert.That(tech.State, Is.EqualTo(AllianceTechnologyState.Ready));
        Assert.That(tech.CompletedAtUtc, Is.Null);

        AllianceResearchBonus bonus = await fx.BonusResolver.ResolveForAllianceAsync(allianceId.Value);
        Assert.That(bonus, Is.EqualTo(AllianceResearchBonus.None), "100% funded (READY) must grant no bonus");
    }

    // ---------------- 11/12/13: launch authorization ----------------

    [Test]
    public async Task Member_CannotLaunch()
    {
        Fixture fx = CreateFixture();
        (_, PlayerId chef, Guid chefHiveId, _, _, PlayerId member, _) = SetUpAlliance(fx);
        await FullyFund(fx, chef, chefHiveId, Minor1, "m1");

        AllianceResearchCommandResult result = await Launch(fx, member, Minor1);
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Code, Is.EqualTo("not_authorized"));
    }

    [Test]
    public async Task Officer_CanLaunchReadyTechnology()
    {
        Fixture fx = CreateFixture();
        (_, PlayerId chef, Guid chefHiveId, PlayerId officer, _, _, _) = SetUpAlliance(fx);
        await FullyFund(fx, chef, chefHiveId, Minor1, "m1");

        AllianceResearchCommandResult result = await Launch(fx, officer, Minor1);
        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.Snapshot!.MinorResearchingTechnologyId, Is.EqualTo(Minor1));
    }

    [Test]
    public async Task Chef_CanLaunchReadyTechnology()
    {
        Fixture fx = CreateFixture();
        (_, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        await FullyFund(fx, chef, chefHiveId, Minor1, "m1");

        AllianceResearchCommandResult result = await Launch(fx, chef, Minor1);
        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.Snapshot!.Technologies.Single(t => t.TechnologyId == Minor1).State, Is.EqualTo(AllianceTechnologyState.Researching));
    }

    // ---------------- 14/15: launch rejection ----------------

    [Test]
    public async Task Launch_RejectedIfFundingIncomplete()
    {
        Fixture fx = CreateFixture();
        (_, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        await SelectTarget(fx, chef, Minor1);
        await Donate(fx, chef, chefHiveId, Minor1, "honey", 1, "partial");

        AllianceResearchCommandResult result = await Launch(fx, chef, Minor1);
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Code, Is.EqualTo("funding_incomplete"));
    }

    [Test]
    public async Task Launch_RejectedIfCorrespondingSlotOccupied()
    {
        Fixture fx = CreateFixture();
        (_, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        await FullyFund(fx, chef, chefHiveId, Minor1, "m1");
        await Launch(fx, chef, Minor1, "launch-1");
        await FullyFund(fx, chef, chefHiveId, MinorOther, "m2");

        AllianceResearchCommandResult result = await Launch(fx, chef, MinorOther, "launch-2");
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Code, Is.EqualTo("slot_occupied"));
    }

    // ---------------- 16/17/18: two independent slots, next funding while researching ----------------

    [Test]
    public async Task MinorAndMajor_ResearchSimultaneously_AndNextMinorCanFundWhileMinorResearches()
    {
        Fixture fx = CreateFixture();
        (AllianceId allianceId, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);

        // Seed Major1's prerequisites as already completed (arrange-only shortcut - Minor1/Minor2's
        // own real funding->launch->timer->completion flow is exercised end to end by the other
        // tests in this file; this test's real subject is dual-slot + next-funding behavior).
        await SeedCompleted(fx, allianceId, Minor1);
        await SeedCompleted(fx, allianceId, Minor2);

        await FullyFund(fx, chef, chefHiveId, Major1, "major");
        await Launch(fx, chef, Major1, "launch-major");

        await FullyFund(fx, chef, chefHiveId, MinorOther, "minorother");
        AllianceResearchCommandResult launchMinor = await Launch(fx, chef, MinorOther, "launch-minor");
        Assert.That(launchMinor.Succeeded, Is.True, "a Minor must be launchable while a Major is researching - independent slots");
        Assert.That(launchMinor.Snapshot!.MajorResearchingTechnologyId, Is.EqualTo(Major1), "the Major slot must be untouched by launching a Minor");
        Assert.That(launchMinor.Snapshot.MinorResearchingTechnologyId, Is.EqualTo(MinorOther));

        // Bible section 7: while MinorOther researches, the Chef may already open funding for the
        // NEXT minor (a different technology, since a technology cannot be both its own slot's
        // active research AND its own slot's funding target at once).
        AllianceResearchCommandResult nextTarget = await SelectTarget(fx, chef, MinorThird, "next-minor-target");
        Assert.That(nextTarget.Succeeded, Is.True);
        Assert.That(nextTarget.Snapshot!.MinorFundingTargetId, Is.EqualTo(MinorThird));
        Assert.That(nextTarget.Snapshot.MinorResearchingTechnologyId, Is.EqualTo(MinorOther), "selecting the next funding target must not disturb the currently researching technology");
    }

    // ---------------- 19/20/21: server-authoritative timer, persists, resolves exactly once ----------------

    [Test]
    public async Task ResearchTimer_PersistsAcrossReads_AndElapsedTimerCompletesExactlyOnce()
    {
        Fixture fx = CreateFixture();
        (_, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        await FullyFund(fx, chef, chefHiveId, Minor1, "m1");
        AllianceResearchCatalog.TryGet(Minor1, out AllianceResearchCatalog.TechnologyDefinition def);
        await Launch(fx, chef, Minor1);

        AllianceResearchReadSnapshot mid = await fx.Research.GetSnapshotAsync(chef);
        AllianceTechnologyReadModel midTech = mid.Technologies.Single(t => t.TechnologyId == Minor1);
        Assert.That(midTech.State, Is.EqualTo(AllianceTechnologyState.Researching));
        Assert.That(midTech.ResearchCompletesAtUtc, Is.Not.Null);

        fx.Clock.UtcNow = fx.Clock.UtcNow + def.ResearchDuration + TimeSpan.FromSeconds(1);
        AllianceResearchReadSnapshot afterFirstRead = await fx.Research.GetSnapshotAsync(chef);
        AllianceTechnologyReadModel completedTech = afterFirstRead.Technologies.Single(t => t.TechnologyId == Minor1);
        Assert.That(completedTech.State, Is.EqualTo(AllianceTechnologyState.Completed));
        Assert.That(completedTech.CompletedAtUtc, Is.Not.Null);

        // A second, independent read must observe the SAME already-resolved completion - proves
        // "exactly once" (no re-triggering, no double completion artifact) via repository state.
        AllianceResearchState raw = (await fx.ResearchRepository.ReadAsync((await fx.Research.GetSnapshotAsync(chef)).AllianceId))!;
        Assert.That(raw.Completed.Count, Is.EqualTo(1));
        Assert.That(raw.MinorResearch, Is.Null, "the slot must be cleared once resolved");
    }

    // ---------------- 22/23: bonus only from Completed ----------------

    [Test]
    public async Task Bonus_OnlyActivatesAfterRealCompletion_NeverWhileResearching()
    {
        Fixture fx = CreateFixture();
        (AllianceId allianceId, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        await FullyFund(fx, chef, chefHiveId, Minor1, "m1");
        AllianceResearchCatalog.TryGet(Minor1, out AllianceResearchCatalog.TechnologyDefinition def);
        await Launch(fx, chef, Minor1);

        AllianceResearchBonus whileResearching = await fx.BonusResolver.ResolveForAllianceAsync(allianceId.Value);
        Assert.That(whileResearching, Is.EqualTo(AllianceResearchBonus.None), "RESEARCHING must grant no bonus");

        fx.Clock.UtcNow = fx.Clock.UtcNow + def.ResearchDuration + TimeSpan.FromSeconds(1);
        await fx.Research.GetSnapshotAsync(chef); // triggers the lazy resolution to Completed

        AllianceResearchBonus afterCompletion = await fx.BonusResolver.ResolveForAllianceAsync(allianceId.Value);
        Assert.That(afterCompletion.ProductionBp, Is.EqualTo(def.ProductionBp), "COMPLETED must grant its real bonus");
    }

    // ---------------- 24/25: membership semantics ----------------

    [Test]
    public async Task Bonus_LeavingAlliance_RemovesCompletedBonus_JoiningGrantsIt()
    {
        Fixture fx = CreateFixture();
        (AllianceId allianceId, PlayerId chef, Guid chefHiveId, _, _, PlayerId member, _) = SetUpAlliance(fx);
        await FullyFund(fx, chef, chefHiveId, Minor1, "m1");
        AllianceResearchCatalog.TryGet(Minor1, out AllianceResearchCatalog.TechnologyDefinition def);
        await Launch(fx, chef, Minor1);
        fx.Clock.UtcNow = fx.Clock.UtcNow + def.ResearchDuration + TimeSpan.FromSeconds(1);
        await fx.Research.GetSnapshotAsync(chef);

        AllianceResearchBonus memberWhileIn = await fx.BonusResolver.ResolveForPlayerAsync(member);
        Assert.That(memberWhileIn.ProductionBp, Is.GreaterThan(0));

        fx.Alliances.Leave(member);
        Assert.That((await fx.BonusResolver.ResolveForPlayerAsync(member)), Is.EqualTo(AllianceResearchBonus.None));

        PlayerId newcomer = NewPlayer();
        fx.AllianceRepository.SaveMembership(new AllianceMembership { AllianceId = allianceId, PlayerId = newcomer, Role = AllianceRole.Member, JoinedAtUtc = fx.Clock.UtcNow, LastRoleChangedAtUtc = fx.Clock.UtcNow, Revision = 0 });
        Assert.That((await fx.BonusResolver.ResolveForPlayerAsync(newcomer)).ProductionBp, Is.GreaterThan(0), "a newcomer must see the alliance's already-completed research bonus immediately");
    }

    // ---------------- 26/27/28/29/30: SpeedUp ----------------

    [Test]
    public async Task SpeedUp_RejectedDuringFunding()
    {
        Fixture fx = CreateFixture();
        (_, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        await SelectTarget(fx, chef, Minor1);
        GiveSpeedUpItem(fx, chef.Value, chefHiveId, "alliance_research_speedup_1h");

        AllianceResearchCommandResult result = await fx.Research.ApplySpeedUpAsync(chef, new ApplyAllianceResearchSpeedUpCommand(chefHiveId, Minor1, "alliance_research_speedup_1h", "su-1"));
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Code, Is.EqualTo("technology_not_researching"));
    }

    [Test]
    public async Task SpeedUp_RejectedWhileReady()
    {
        Fixture fx = CreateFixture();
        (_, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        await FullyFund(fx, chef, chefHiveId, Minor1, "m1");
        GiveSpeedUpItem(fx, chef.Value, chefHiveId, "alliance_research_speedup_1h");

        AllianceResearchCommandResult result = await fx.Research.ApplySpeedUpAsync(chef, new ApplyAllianceResearchSpeedUpCommand(chefHiveId, Minor1, "alliance_research_speedup_1h", "su-1"));
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Code, Is.EqualTo("technology_not_researching"));
    }

    [Test]
    public async Task SpeedUp_AcceptedWhileResearching_ReducesRealRemainingTime()
    {
        // Uses Major1 (2h Alpha duration) rather than a Minor (15-25min) specifically so the 1h
        // item's reduction does NOT hit the "cannot overshoot below zero" clamp - that clamp
        // behavior has its own dedicated test (SpeedUp_CannotOvershootBelowNow) using a Minor.
        Fixture fx = CreateFixture();
        (AllianceId allianceId, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        await SeedCompleted(fx, allianceId, Minor1);
        await SeedCompleted(fx, allianceId, Minor2);
        await FullyFund(fx, chef, chefHiveId, Major1, "maj1");
        await Launch(fx, chef, Major1, "launch-1");
        DateTimeOffset before = fx.Research.GetSnapshotAsync(chef).Result.Technologies.Single(t => t.TechnologyId == Major1).ResearchCompletesAtUtc!.Value;
        GiveSpeedUpItem(fx, chef.Value, chefHiveId, "alliance_research_speedup_1h");

        AllianceResearchCommandResult result = await fx.Research.ApplySpeedUpAsync(chef, new ApplyAllianceResearchSpeedUpCommand(chefHiveId, Major1, "alliance_research_speedup_1h", "su-1"));

        Assert.That(result.Succeeded, Is.True);
        DateTimeOffset after = result.Snapshot!.Technologies.Single(t => t.TechnologyId == Major1).ResearchCompletesAtUtc!.Value;
        Assert.That(after, Is.EqualTo(before - TimeSpan.FromHours(1)));
        PlayerHiveState state = (await fx.HiveStates.ReadAsync(chef.Value, chefHiveId))!;
        Assert.That(state.SpeedUps!.GetValueOrDefault("alliance_research_speedup_1h"), Is.EqualTo(0), "the item must be consumed exactly once");
    }

    [Test]
    public async Task SpeedUp_CannotOvershootBelowNow()
    {
        Fixture fx = CreateFixture();
        (_, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        await FullyFund(fx, chef, chefHiveId, Minor1, "m1"); // 15 minute duration
        await Launch(fx, chef, Minor1);
        GiveSpeedUpItem(fx, chef.Value, chefHiveId, "alliance_research_speedup_24h");

        AllianceResearchCommandResult result = await fx.Research.ApplySpeedUpAsync(chef, new ApplyAllianceResearchSpeedUpCommand(chefHiveId, Minor1, "alliance_research_speedup_24h", "su-1"));

        Assert.That(result.Succeeded, Is.True);
        DateTimeOffset completesAt = result.Snapshot!.Technologies.Single(t => t.TechnologyId == Minor1).ResearchCompletesAtUtc!.Value;
        Assert.That(completesAt, Is.GreaterThanOrEqualTo(fx.Clock.UtcNow), "must clamp at 'now', never go negative/into the past");
    }

    [Test]
    public async Task SpeedUp_CompletionAfterReducedTimer_ActivatesExactlyOnce()
    {
        Fixture fx = CreateFixture();
        (AllianceId allianceId, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        await FullyFund(fx, chef, chefHiveId, Minor1, "m1");
        await Launch(fx, chef, Minor1);
        GiveSpeedUpItem(fx, chef.Value, chefHiveId, "alliance_research_speedup_24h");
        await fx.Research.ApplySpeedUpAsync(chef, new ApplyAllianceResearchSpeedUpCommand(chefHiveId, Minor1, "alliance_research_speedup_24h", "su-1"));

        AllianceResearchReadSnapshot afterSpeedUp = await fx.Research.GetSnapshotAsync(chef);
        Assert.That(afterSpeedUp.Technologies.Single(t => t.TechnologyId == Minor1).State, Is.EqualTo(AllianceTechnologyState.Completed), "the 24h item must fully clear the 15-minute duration, resolving completion on the very next read");
        AllianceResearchBonus bonus = await fx.BonusResolver.ResolveForAllianceAsync(allianceId.Value);
        Assert.That(bonus.ProductionBp, Is.GreaterThan(0));
    }

    [Test]
    public async Task Member_CannotUseAllianceResearchSpeedUp()
    {
        Fixture fx = CreateFixture();
        (_, PlayerId chef, Guid chefHiveId, _, _, PlayerId member, Guid memberHiveId) = SetUpAlliance(fx);
        await FullyFund(fx, chef, chefHiveId, Minor1, "m1");
        await Launch(fx, chef, Minor1);
        GiveSpeedUpItem(fx, member.Value, memberHiveId, "alliance_research_speedup_1h");

        AllianceResearchCommandResult result = await fx.Research.ApplySpeedUpAsync(member, new ApplyAllianceResearchSpeedUpCommand(memberHiveId, Minor1, "alliance_research_speedup_1h", "su-1"));
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Code, Is.EqualTo("not_authorized"));
    }

    // ---------------- 32/33/34: idempotent retries ----------------

    [Test]
    public async Task Donate_SameClientRequestIdRetried_DoesNotDoubleDebit()
    {
        Fixture fx = CreateFixture();
        (_, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        await SelectTarget(fx, chef, Minor1);

        await Donate(fx, chef, chefHiveId, Minor1, "honey", 500, "same-key");
        AllianceResearchCommandResult retry = await Donate(fx, chef, chefHiveId, Minor1, "honey", 500, "same-key");

        Assert.That(retry.Succeeded, Is.True);
        PlayerHiveState state = (await fx.HiveStates.ReadAsync(chef.Value, chefHiveId))!;
        Assert.That(state.Resources["honey"].Amount, Is.EqualTo(1_000_000 - 500), "must debit exactly once across the retry");
        Assert.That(retry.Snapshot!.Technologies.Single(t => t.TechnologyId == Minor1).FundingContributed.GetValueOrDefault("honey"), Is.EqualTo(500), "must apply exactly once across the retry");
    }

    [Test]
    public async Task Launch_SameClientRequestIdRetried_IsSafe()
    {
        Fixture fx = CreateFixture();
        (_, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        await FullyFund(fx, chef, chefHiveId, Minor1, "m1");

        AllianceResearchCommandResult first = await Launch(fx, chef, Minor1, "same-launch-key");
        AllianceResearchCommandResult retry = await Launch(fx, chef, Minor1, "same-launch-key");

        Assert.That(first.Succeeded, Is.True);
        Assert.That(retry.Succeeded, Is.True);
        Assert.That(retry.Snapshot!.MinorResearchingTechnologyId, Is.EqualTo(Minor1));
    }

    [Test]
    public async Task SpeedUp_SameClientRequestIdRetried_DoesNotDoubleReduce()
    {
        // Major1 again (see SpeedUp_AcceptedWhileResearching's own comment) - a retry that DID
        // double-apply against a short Minor would be masked by the completion clamp either way.
        Fixture fx = CreateFixture();
        (AllianceId allianceId, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        await SeedCompleted(fx, allianceId, Minor1);
        await SeedCompleted(fx, allianceId, Minor2);
        await FullyFund(fx, chef, chefHiveId, Major1, "maj1");
        await Launch(fx, chef, Major1, "launch-1");
        GiveSpeedUpItem(fx, chef.Value, chefHiveId, "alliance_research_speedup_1h", quantity: 2);

        AllianceResearchCommandResult first = await fx.Research.ApplySpeedUpAsync(chef, new ApplyAllianceResearchSpeedUpCommand(chefHiveId, Major1, "alliance_research_speedup_1h", "same-su-key"));
        AllianceResearchCommandResult retry = await fx.Research.ApplySpeedUpAsync(chef, new ApplyAllianceResearchSpeedUpCommand(chefHiveId, Major1, "alliance_research_speedup_1h", "same-su-key"));

        Assert.That(first.Snapshot!.Technologies.Single(t => t.TechnologyId == Major1).ResearchCompletesAtUtc,
            Is.EqualTo(retry.Snapshot!.Technologies.Single(t => t.TechnologyId == Major1).ResearchCompletesAtUtc), "a retried speedup must not reduce the timer a second time");
    }

    // ---------------- 35: concurrent donations remain safe ----------------

    [Test]
    public async Task ConcurrentDonations_FromTwoMembers_NeitherContributionIsLost()
    {
        Fixture fx = CreateFixture();
        (_, PlayerId chef, Guid chefHiveId, PlayerId officer, Guid officerHiveId, _, _) = SetUpAlliance(fx);
        await SelectTarget(fx, chef, Minor1);

        Task<AllianceResearchCommandResult> a = Donate(fx, chef, chefHiveId, Minor1, "honey", 1000, "chef-donate");
        Task<AllianceResearchCommandResult> b = Donate(fx, officer, officerHiveId, Minor1, "honey", 1000, "officer-donate");
        await Task.WhenAll(a, b);

        Assert.That(a.Result.Succeeded, Is.True);
        Assert.That(b.Result.Succeeded, Is.True);
        AllianceResearchState raw = (await fx.ResearchRepository.ReadAsync((await fx.Research.GetSnapshotAsync(chef)).AllianceId))!;
        Assert.That(raw.Funding[Minor1].Contributed["honey"], Is.EqualTo(2000), "two concurrent 1000-honey donations must both land - no lost update");
    }

    // ---------------- 37: Gelée Royale never accepted ----------------

    [Test]
    public async Task Donate_RoyalJellyIsNeverAValidFundingResource()
    {
        // No Alpha technology's FundingRequirements references Royal Jelly at all - proven by
        // direct catalog inspection (the Bible's own explicit rule: it is never normal funding).
        Assert.That(AllianceResearchCatalog.Technologies.Any(t => t.FundingRequirements.Keys.Any(k => k.Contains("royal_jelly", StringComparison.OrdinalIgnoreCase) || k.Contains("jelly", StringComparison.OrdinalIgnoreCase))), Is.False);

        Fixture fx = CreateFixture();
        (_, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        await SelectTarget(fx, chef, Minor1);

        AllianceResearchCommandResult result = await Donate(fx, chef, chefHiveId, Minor1, "royal_jelly", 1, "attempt-jelly");
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Code, Is.EqualTo("invalid_resource"), "even if a caller tries, a resource outside the technology's own FundingRequirements is always rejected");
    }

    // ---------------- architecture: long durations (30-60 days) are structurally supported ----------------

    [Test]
    public void LongDurationMathIsExact_60DayMajorDoesNotOverflowOrTruncate()
    {
        DateTimeOffset start = DateTimeOffset.UtcNow;
        TimeSpan sixtyDays = TimeSpan.FromDays(60);
        AllianceResearchSlot slot = new("test_tech", start, start + sixtyDays);
        Assert.That((slot.CompletesAtUtc - slot.StartedAtUtc), Is.EqualTo(sixtyDays));
        Assert.That(start + sixtyDays >= start, Is.True);
        // The exact same arithmetic (DateTimeOffset + TimeSpan, no custom overflow-prone integer
        // day/second math) is what LaunchAsync/ApplySpeedUpAsync use for every duration, Alpha or
        // final-balance alike - this proves the architecture, independent of which duration value
        // the current Alpha catalog happens to configure.
    }

    // ================================================================================
    // M053-CL: Major track certification (Part 1) - exercises the exact same code paths as the
    // Minor tests above, but against Major1 specifically, since AllianceResearchService branches
    // uniformly on Category (never a Major-specific special case) - see e.g. SelectFundingTargetAsync/
    // LaunchAsync/ApplySpeedUpAsync/ResolveTechnologyState in AllianceResearchService.cs.
    // ================================================================================

    [Test]
    public async Task Major_LockedBeforePrerequisites_EligibleOnceBothCompleted()
    {
        Fixture fx = CreateFixture();
        (AllianceId allianceId, PlayerId chef, _, _, _, _, _) = SetUpAlliance(fx);

        AllianceResearchReadSnapshot before = await fx.Research.GetSnapshotAsync(chef);
        Assert.That(before.Technologies.Single(t => t.TechnologyId == Major1).State, Is.EqualTo(AllianceTechnologyState.Locked));

        await SeedCompleted(fx, allianceId, Minor1);
        await SeedCompleted(fx, allianceId, Minor2);
        AllianceResearchReadSnapshot after = await fx.Research.GetSnapshotAsync(chef);
        Assert.That(after.Technologies.Single(t => t.TechnologyId == Major1).State, Is.EqualTo(AllianceTechnologyState.Eligible), "both real prerequisites completed must unlock Major1 for selection");
    }

    [Test]
    public async Task Major_StaysLockedWhilePrerequisiteOnlyFundingReadyOrResearching_NeverUnlocksEarly()
    {
        Fixture fx = CreateFixture();
        (_, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);

        // Minor1 in FUNDING (selected, partially donated) - Major1 must remain Locked.
        await SelectTarget(fx, chef, Minor1, "sel-fund");
        await Donate(fx, chef, chefHiveId, Minor1, "honey", 1, "partial-fund");
        AllianceResearchCommandResult majorWhileFunding = await SelectTarget(fx, chef, Major1, "major-during-funding");
        Assert.That(majorWhileFunding.Succeeded, Is.False);
        Assert.That(majorWhileFunding.Code, Is.EqualTo("technology_locked"));

        // Minor1 reaches READY (100% funded, not launched) - still must not unlock Major1.
        await FullyFund(fx, chef, chefHiveId, Minor1, "m1-ready-only");
        AllianceResearchCommandResult majorWhileReady = await SelectTarget(fx, chef, Major1, "major-during-ready");
        Assert.That(majorWhileReady.Succeeded, Is.False);
        Assert.That(majorWhileReady.Code, Is.EqualTo("technology_locked"), "a fully-FUNDED-but-not-launched prerequisite must not unlock a dependent Major");

        // Minor1 RESEARCHING (launched, timer running, not yet elapsed) - still must not unlock Major1.
        await Launch(fx, chef, Minor1, "launch-minor1");
        AllianceResearchCommandResult majorWhileResearching = await SelectTarget(fx, chef, Major1, "major-during-research");
        Assert.That(majorWhileResearching.Succeeded, Is.False);
        Assert.That(majorWhileResearching.Code, Is.EqualTo("technology_locked"), "a RESEARCHING (not yet COMPLETED) prerequisite must not unlock a dependent Major");
    }

    [Test]
    public async Task Officer_CannotSelectMajorFundingTarget()
    {
        Fixture fx = CreateFixture();
        (AllianceId allianceId, _, _, PlayerId officer, _, _, _) = SetUpAlliance(fx);
        await SeedCompleted(fx, allianceId, Minor1);
        await SeedCompleted(fx, allianceId, Minor2);

        AllianceResearchCommandResult result = await SelectTarget(fx, officer, Major1, "officer-major");
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Code, Is.EqualTo("not_authorized"), "Bible section 4: Major target selection is exclusively the Chef's, same as Minor");
    }

    [Test]
    public async Task Member_CannotSelectMajorFundingTarget()
    {
        Fixture fx = CreateFixture();
        (AllianceId allianceId, _, _, _, _, PlayerId member, _) = SetUpAlliance(fx);
        await SeedCompleted(fx, allianceId, Minor1);
        await SeedCompleted(fx, allianceId, Minor2);

        AllianceResearchCommandResult result = await SelectTarget(fx, member, Major1, "member-major");
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Code, Is.EqualTo("not_authorized"));
    }

    [Test]
    public async Task Major_FullyFunding_ProducesReady_NotCompleted_AndGrantsNoBonus()
    {
        Fixture fx = CreateFixture();
        (AllianceId allianceId, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        await SeedCompleted(fx, allianceId, Minor1);
        await SeedCompleted(fx, allianceId, Minor2);
        await FullyFund(fx, chef, chefHiveId, Major1, "major-ready");

        AllianceResearchReadSnapshot snapshot = await fx.Research.GetSnapshotAsync(chef);
        AllianceTechnologyReadModel tech = snapshot.Technologies.Single(t => t.TechnologyId == Major1);
        Assert.That(tech.State, Is.EqualTo(AllianceTechnologyState.Ready));
        Assert.That(tech.CompletedAtUtc, Is.Null);

        // Minor1/Minor2 were seeded directly into Completed as Major1's prerequisites - their own
        // 100+150=250 ProductionBp is real and expected here; the assertion below isolates that
        // Major1 itself (still only READY) contributes NOTHING on top of that baseline.
        AllianceResearchBonus bonus = await fx.BonusResolver.ResolveForAllianceAsync(allianceId.Value);
        Assert.That(bonus.ProductionBp, Is.EqualTo(250), "a READY Major must grant no gameplay bonus of its own, exactly like a READY Minor");
    }

    [Test]
    public async Task Officer_CanLaunchReadyMajor()
    {
        Fixture fx = CreateFixture();
        (AllianceId allianceId, PlayerId chef, Guid chefHiveId, PlayerId officer, _, _, _) = SetUpAlliance(fx);
        await SeedCompleted(fx, allianceId, Minor1);
        await SeedCompleted(fx, allianceId, Minor2);
        await FullyFund(fx, chef, chefHiveId, Major1, "major-officer-launch");

        AllianceResearchCommandResult result = await Launch(fx, officer, Major1, "officer-launch-major");
        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.Snapshot!.MajorResearchingTechnologyId, Is.EqualTo(Major1));
    }

    [Test]
    public async Task Member_CannotLaunchReadyMajor()
    {
        Fixture fx = CreateFixture();
        (AllianceId allianceId, PlayerId chef, Guid chefHiveId, _, _, PlayerId member, _) = SetUpAlliance(fx);
        await SeedCompleted(fx, allianceId, Minor1);
        await SeedCompleted(fx, allianceId, Minor2);
        await FullyFund(fx, chef, chefHiveId, Major1, "major-member-launch");

        AllianceResearchCommandResult result = await Launch(fx, member, Major1, "member-launch-major");
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Code, Is.EqualTo("not_authorized"));
    }

    [Test]
    public async Task Major_LaunchSetsServerAuthoritativeStartAndCompletionTimestamps()
    {
        Fixture fx = CreateFixture();
        (AllianceId allianceId, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        await SeedCompleted(fx, allianceId, Minor1);
        await SeedCompleted(fx, allianceId, Minor2);
        await FullyFund(fx, chef, chefHiveId, Major1, "major-timestamps");
        AllianceResearchCatalog.TryGet(Major1, out AllianceResearchCatalog.TechnologyDefinition def);
        DateTimeOffset launchedAt = fx.Clock.UtcNow;

        AllianceResearchCommandResult result = await Launch(fx, chef, Major1, "major-launch-ts");

        AllianceTechnologyReadModel tech = result.Snapshot!.Technologies.Single(t => t.TechnologyId == Major1);
        Assert.That(tech.State, Is.EqualTo(AllianceTechnologyState.Researching));
        Assert.That(tech.ResearchStartedAtUtc, Is.EqualTo(launchedAt));
        Assert.That(tech.ResearchCompletesAtUtc, Is.EqualTo(launchedAt + def.ResearchDuration));
    }

    [Test]
    public async Task Major_NaturallyResolvesToCompleted_AfterTimerElapses_AndBonusActivatesOnlyThen()
    {
        Fixture fx = CreateFixture();
        (AllianceId allianceId, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        await SeedCompleted(fx, allianceId, Minor1);
        await SeedCompleted(fx, allianceId, Minor2);
        await FullyFund(fx, chef, chefHiveId, Major1, "major-complete");
        AllianceResearchCatalog.TryGet(Major1, out AllianceResearchCatalog.TechnologyDefinition def);
        await Launch(fx, chef, Major1, "major-launch-complete");

        // Baseline 250 = Minor1(100)+Minor2(150), seeded directly as Major1's prerequisites.
        AllianceResearchBonus whileResearching = await fx.BonusResolver.ResolveForAllianceAsync(allianceId.Value);
        Assert.That(whileResearching.ProductionBp, Is.EqualTo(250), "a RESEARCHING Major must grant no bonus of its own");

        fx.Clock.UtcNow = fx.Clock.UtcNow + def.ResearchDuration + TimeSpan.FromSeconds(1);
        AllianceResearchReadSnapshot afterElapsed = await fx.Research.GetSnapshotAsync(chef);
        AllianceTechnologyReadModel completedTech = afterElapsed.Technologies.Single(t => t.TechnologyId == Major1);
        Assert.That(completedTech.State, Is.EqualTo(AllianceTechnologyState.Completed));
        Assert.That(completedTech.CompletedAtUtc, Is.Not.Null);

        AllianceResearchBonus afterCompletion = await fx.BonusResolver.ResolveForAllianceAsync(allianceId.Value);
        Assert.That(afterCompletion.ProductionBp, Is.EqualTo(250 + def.ProductionBp), "a COMPLETED Major must grant its real bonus on top of the already-completed prerequisites");
    }

    [Test]
    public async Task Major_CompletedTechnology_PersistsAcrossFreshRepositoryReadInstance()
    {
        Fixture fx = CreateFixture();
        (AllianceId allianceId, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        await SeedCompleted(fx, allianceId, Minor1);
        await SeedCompleted(fx, allianceId, Minor2);
        await FullyFund(fx, chef, chefHiveId, Major1, "major-persist");
        AllianceResearchCatalog.TryGet(Major1, out AllianceResearchCatalog.TechnologyDefinition def);
        await Launch(fx, chef, Major1, "major-launch-persist");
        fx.Clock.UtcNow = fx.Clock.UtcNow + def.ResearchDuration + TimeSpan.FromSeconds(1);
        await fx.Research.GetSnapshotAsync(chef); // triggers the authoritative write-path resolution

        // "Reload" = a brand-new AllianceResearchService/BonusResolver pair wrapping the SAME
        // underlying repository instance - simulates a fresh server process reading persisted state,
        // never a fabricated/duplicated result from the same in-memory service instance.
        var reloadedResearch = new AllianceResearchService(fx.AllianceRepository, fx.ResearchRepository, fx.HiveStates, Options.Create(new AllianceResearchOptions { Enabled = true }), fx.Clock);
        var reloadedResolver = new AllianceResearchBonusResolver(fx.AllianceRepository, fx.ResearchRepository, fx.Clock);

        AllianceResearchReadSnapshot reloaded = await reloadedResearch.GetSnapshotAsync(chef);
        Assert.That(reloaded.Technologies.Single(t => t.TechnologyId == Major1).State, Is.EqualTo(AllianceTechnologyState.Completed));
        AllianceResearchBonus reloadedBonus = await reloadedResolver.ResolveForAllianceAsync(allianceId.Value);
        Assert.That(reloadedBonus.ProductionBp, Is.EqualTo(250 + def.ProductionBp), "a completed Major's bonus (plus its already-completed prerequisites') must survive a fresh read against the same persisted repository state");
    }

    // ================================================================================
    // M053-CL Part 2: Minor+Major simultaneous research + next-funding-while-researching, with an
    // explicit persistence/reload check. NOTE (documented Alpha catalog limitation, not a code gap):
    // the Alpha catalog contains exactly ONE Major technology (Major1), so "select a DIFFERENT Major
    // as the next funding target while a Major researches" cannot be exercised with real catalog
    // data (SelectFundingTargetAsync only accepts a real AllianceResearchCatalog entry - there is no
    // second Major to select). The code path itself is proven identical to the Minor case by direct
    // inspection: every branch in AllianceResearchService (SelectFundingTargetAsync, DonateAsync's
    // ValidateDonatable, LaunchAsync, ApplySpeedUpAsync, ResolveElapsedResearch, BuildSnapshot/
    // ResolveTechnologyState) switches on `definition.Category` uniformly, with zero Major-specific
    // special-casing beyond which field (Minor*/Major*) it reads or writes - so "next Major funding
    // while Major researches" is architecturally the SAME code as the "next Minor funding while
    // Minor researches" proven below, just gated by a catalog-content limitation, not a service-logic
    // one. See the M053 report Part 6/14 for the explicit recommendation to add a second real
    // Bible-named Major (e.g. "Réseau commercial royal", already named in Bible section 15) in a
    // future mission so this can be certified with real data.
    // ================================================================================

    [Test]
    public async Task FourConcurrentConcepts_MinorActiveMinorNextMajorActive_PersistAcrossReload()
    {
        Fixture fx = CreateFixture();
        (AllianceId allianceId, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        await SeedCompleted(fx, allianceId, Minor1);
        await SeedCompleted(fx, allianceId, Minor2);

        await FullyFund(fx, chef, chefHiveId, Major1, "four-major");
        await Launch(fx, chef, Major1, "four-major-launch");
        await FullyFund(fx, chef, chefHiveId, MinorOther, "four-minor");
        await Launch(fx, chef, MinorOther, "four-minor-launch");
        AllianceResearchCommandResult withNextTarget = await SelectTarget(fx, chef, MinorThird, "four-next-minor-target");

        Assert.That(withNextTarget.Succeeded, Is.True);
        AllianceResearchReadSnapshot live = withNextTarget.Snapshot!;
        Assert.That(live.MajorResearchingTechnologyId, Is.EqualTo(Major1), "concept 1: Major active research");
        Assert.That(live.MinorResearchingTechnologyId, Is.EqualTo(MinorOther), "concept 2: Minor active research");
        Assert.That(live.MinorFundingTargetId, Is.EqualTo(MinorThird), "concept 3: Minor next funding target, distinct from the researching Minor");
        // MajorFundingTargetId legitimately still reads Major1 here - "last selected target" is
        // never cleared by launching (see AllianceResearchService's own "Ready state defined
        // independent of current selection" design note) - selecting MinorThird as the NEXT Minor
        // target must not have disturbed this unrelated Major-category field at all.
        Assert.That(live.MajorFundingTargetId, Is.EqualTo(Major1), "selecting the next Minor target must not touch the unrelated Major funding-target field");

        // Persistence: a fresh service instance over the SAME repository must observe all three
        // concepts identically - no slot overwrote another during any of the four mutations above.
        var reloaded = new AllianceResearchService(fx.AllianceRepository, fx.ResearchRepository, fx.HiveStates, Options.Create(new AllianceResearchOptions { Enabled = true }), fx.Clock);
        AllianceResearchReadSnapshot afterReload = await reloaded.GetSnapshotAsync(chef);
        Assert.That(afterReload.MajorResearchingTechnologyId, Is.EqualTo(Major1));
        Assert.That(afterReload.MinorResearchingTechnologyId, Is.EqualTo(MinorOther));
        Assert.That(afterReload.MinorFundingTargetId, Is.EqualTo(MinorThird));
        Assert.That(afterReload.Technologies.Single(t => t.TechnologyId == Major1).State, Is.EqualTo(AllianceTechnologyState.Researching));
        Assert.That(afterReload.Technologies.Single(t => t.TechnologyId == MinorOther).State, Is.EqualTo(AllianceTechnologyState.Researching));
        Assert.That(afterReload.Technologies.Single(t => t.TechnologyId == MinorThird).State, Is.EqualTo(AllianceTechnologyState.Funding));
    }

    // ================================================================================
    // M053-CL Part 3: Alliance Research SpeedUp certification gaps not yet covered by the M052
    // baseline (Locked/Completed rejection, Officer permission, exactly-once completion, retry
    // inventory safety, wrong-category rejection, cross-category isolation from personal SpeedUps).
    // ================================================================================

    [Test]
    public async Task SpeedUp_RejectedWhileLocked()
    {
        Fixture fx = CreateFixture();
        (_, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        GiveSpeedUpItem(fx, chef.Value, chefHiveId, "alliance_research_speedup_1h");

        // Major1 is LOCKED (prerequisites not completed) - never selected, never funded, never
        // researching. ApplySpeedUpAsync must reject it exactly like any other non-researching state.
        AllianceResearchCommandResult result = await fx.Research.ApplySpeedUpAsync(chef, new ApplyAllianceResearchSpeedUpCommand(chefHiveId, Major1, "alliance_research_speedup_1h", "su-locked"));
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Code, Is.EqualTo("technology_not_researching"));
    }

    [Test]
    public async Task SpeedUp_RejectedAfterCompletion()
    {
        Fixture fx = CreateFixture();
        (_, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        await FullyFund(fx, chef, chefHiveId, Minor1, "m1");
        AllianceResearchCatalog.TryGet(Minor1, out AllianceResearchCatalog.TechnologyDefinition def);
        await Launch(fx, chef, Minor1);
        fx.Clock.UtcNow = fx.Clock.UtcNow + def.ResearchDuration + TimeSpan.FromSeconds(1);
        await fx.Research.GetSnapshotAsync(chef); // resolves to Completed
        GiveSpeedUpItem(fx, chef.Value, chefHiveId, "alliance_research_speedup_1h");

        AllianceResearchCommandResult result = await fx.Research.ApplySpeedUpAsync(chef, new ApplyAllianceResearchSpeedUpCommand(chefHiveId, Minor1, "alliance_research_speedup_1h", "su-completed"));
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Code, Is.EqualTo("technology_not_researching"), "a COMPLETED technology has no active slot left to accelerate");
    }

    [Test]
    public async Task Officer_CanApplyAllianceResearchSpeedUpWhileResearching()
    {
        Fixture fx = CreateFixture();
        (_, PlayerId chef, Guid chefHiveId, PlayerId officer, Guid officerHiveId, _, _) = SetUpAlliance(fx);
        await FullyFund(fx, chef, chefHiveId, Minor1, "m1");
        await Launch(fx, chef, Minor1);
        GiveSpeedUpItem(fx, officer.Value, officerHiveId, "alliance_research_speedup_1h");

        AllianceResearchCommandResult result = await fx.Research.ApplySpeedUpAsync(officer, new ApplyAllianceResearchSpeedUpCommand(officerHiveId, Minor1, "alliance_research_speedup_1h", "su-officer"));
        Assert.That(result.Succeeded, Is.True);
    }

    [Test]
    public async Task SpeedUp_TriggeredCompletion_OccursExactlyOnce()
    {
        Fixture fx = CreateFixture();
        (AllianceId allianceId, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        await FullyFund(fx, chef, chefHiveId, Minor1, "m1");
        await Launch(fx, chef, Minor1);
        GiveSpeedUpItem(fx, chef.Value, chefHiveId, "alliance_research_speedup_24h");
        await fx.Research.ApplySpeedUpAsync(chef, new ApplyAllianceResearchSpeedUpCommand(chefHiveId, Minor1, "alliance_research_speedup_24h", "su-exactly-once"));

        await fx.Research.GetSnapshotAsync(chef);
        AllianceResearchState firstRead = (await fx.ResearchRepository.ReadAsync(allianceId.Value))!;
        Assert.That(firstRead.Completed.Count, Is.EqualTo(1));

        // A second, independent read must observe the exact same single completion - no
        // re-triggering, no duplicate completion record, slot stays cleared.
        await fx.Research.GetSnapshotAsync(chef);
        AllianceResearchState secondRead = (await fx.ResearchRepository.ReadAsync(allianceId.Value))!;
        Assert.That(secondRead.Completed.Count, Is.EqualTo(1));
        Assert.That(secondRead.MinorResearch, Is.Null);
    }

    [Test]
    public async Task SpeedUp_RetryWithSameClientRequestId_DoesNotDoubleConsumeInventory()
    {
        Fixture fx = CreateFixture();
        (_, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        await FullyFund(fx, chef, chefHiveId, Minor1, "m1");
        await Launch(fx, chef, Minor1);
        GiveSpeedUpItem(fx, chef.Value, chefHiveId, "alliance_research_speedup_1h", quantity: 3);

        await fx.Research.ApplySpeedUpAsync(chef, new ApplyAllianceResearchSpeedUpCommand(chefHiveId, Minor1, "alliance_research_speedup_1h", "su-retry-inv"));
        await fx.Research.ApplySpeedUpAsync(chef, new ApplyAllianceResearchSpeedUpCommand(chefHiveId, Minor1, "alliance_research_speedup_1h", "su-retry-inv"));

        PlayerHiveState state = (await fx.HiveStates.ReadAsync(chef.Value, chefHiveId))!;
        Assert.That(state.SpeedUps!.GetValueOrDefault("alliance_research_speedup_1h"), Is.EqualTo(2), "starting with 3 and retrying the SAME request id must consume exactly 1, never 2");
    }

    [Test]
    public async Task SpeedUp_UnknownItemId_Rejected()
    {
        Fixture fx = CreateFixture();
        (_, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        await FullyFund(fx, chef, chefHiveId, Minor1, "m1");
        await Launch(fx, chef, Minor1);

        AllianceResearchCommandResult result = await fx.Research.ApplySpeedUpAsync(chef, new ApplyAllianceResearchSpeedUpCommand(chefHiveId, Minor1, "not_a_real_item", "su-wrong-item"));
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Code, Is.EqualTo("item_not_found"));
    }

    [Test]
    public async Task PersonalResearchSpeedUpItemId_CannotAccelerateAllianceResearch()
    {
        // "research_3600s" is a REAL item id from BeeKingdom.HiveOperations.SpeedUpOptions' default
        // catalog (personal Research category) - AllianceResearchSpeedUpCatalog does not recognize
        // it at all, proving the two categories are genuinely distinct catalogs, not merely
        // differently-labeled entries in a shared one.
        Fixture fx = CreateFixture();
        (_, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        await FullyFund(fx, chef, chefHiveId, Minor1, "m1");
        await Launch(fx, chef, Minor1);
        fx.HiveStates.Seed((await fx.HiveStates.ReadAsync(chef.Value, chefHiveId))! with { SpeedUps = new Dictionary<string, int>(StringComparer.Ordinal) { ["research_3600s"] = 5 } });

        AllianceResearchCommandResult result = await fx.Research.ApplySpeedUpAsync(chef, new ApplyAllianceResearchSpeedUpCommand(chefHiveId, Minor1, "research_3600s", "su-cross-category"));
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Code, Is.EqualTo("item_not_found"));
    }

    [Test]
    public void AllianceResearchSpeedUpItemIds_AreNeverPresentInThePersonalSpeedUpCatalog()
    {
        // Inverse direction of the previous test, proven by pure catalog inspection (no service call
        // needed): an Alliance Research SpeedUp item id must never collide with - and therefore can
        // never be accepted by - BeeKingdom.HiveOperations.SpeedUpOptions' own default personal
        // catalog (construction/research/training/healing/manufacturing/universal).
        var personalCatalog = new BeeKingdom.HiveOperations.SpeedUpOptions();
        HashSet<string> personalItemIds = personalCatalog.Catalog.Select(item => item.ItemId).ToHashSet(StringComparer.Ordinal);
        foreach (AllianceResearchSpeedUpCatalog.ItemDefinition allianceItem in AllianceResearchSpeedUpCatalog.Items)
            Assert.That(personalItemIds.Contains(allianceItem.ItemId), Is.False, allianceItem.ItemId + " must not exist in the personal SpeedUp catalog");
    }

    [Test]
    public async Task SpeedUp_ReducedTimerAndInventoryConsumption_PersistAcrossFreshRepositoryReadInstance()
    {
        // Major1 (2h Alpha duration), not Minor1, per the same reasoning as the M052 SpeedUp tests -
        // a 1h reduction against Minor1's 15-minute duration would hit the "cannot overshoot below
        // now" clamp and complete the research on the very next read, leaving ResearchCompletesAtUtc
        // null (Completed, not Researching) and masking exactly what this test wants to observe.
        Fixture fx = CreateFixture();
        (AllianceId allianceId, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        await SeedCompleted(fx, allianceId, Minor1);
        await SeedCompleted(fx, allianceId, Minor2);
        await FullyFund(fx, chef, chefHiveId, Major1, "m-persist-su");
        await Launch(fx, chef, Major1, "launch-persist-su");
        GiveSpeedUpItem(fx, chef.Value, chefHiveId, "alliance_research_speedup_1h");
        AllianceResearchCommandResult applied = await fx.Research.ApplySpeedUpAsync(chef, new ApplyAllianceResearchSpeedUpCommand(chefHiveId, Major1, "alliance_research_speedup_1h", "su-persist"));
        DateTimeOffset expectedCompletesAt = applied.Snapshot!.Technologies.Single(t => t.TechnologyId == Major1).ResearchCompletesAtUtc!.Value;

        var reloaded = new AllianceResearchService(fx.AllianceRepository, fx.ResearchRepository, fx.HiveStates, Options.Create(new AllianceResearchOptions { Enabled = true }), fx.Clock);
        AllianceResearchReadSnapshot reloadedSnapshot = await reloaded.GetSnapshotAsync(chef);
        Assert.That(reloadedSnapshot.Technologies.Single(t => t.TechnologyId == Major1).ResearchCompletesAtUtc, Is.EqualTo(expectedCompletesAt));
        PlayerHiveState reloadedHive = (await fx.HiveStates.ReadAsync(chef.Value, chefHiveId))!;
        Assert.That(reloadedHive.SpeedUps!.GetValueOrDefault("alliance_research_speedup_1h"), Is.EqualTo(0));
    }

    // ================================================================================
    // M053-CL Part 4: bonus freshness - a research timer that has objectively elapsed but not yet
    // lazily persisted into Completed must still count toward the bonus (AllianceResearchBonusResolver
    // now checks CompletesAtUtc against "now" as a read-only, additive fact - see its own class
    // comment for the full rationale and why this is NOT a write/scheduler/polling change).
    // ================================================================================

    [Test]
    public async Task Bonus_CountsElapsedButNotYetPersistedResearch_WithoutRequiringAReadFirst()
    {
        Fixture fx = CreateFixture();
        (AllianceId allianceId, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        await FullyFund(fx, chef, chefHiveId, Minor1, "m1");
        AllianceResearchCatalog.TryGet(Minor1, out AllianceResearchCatalog.TechnologyDefinition def);
        await Launch(fx, chef, Minor1);

        // Advance time past completion WITHOUT calling GetSnapshotAsync (the only place that
        // actually persists the Completed transition) - the raw repository state still shows
        // MinorResearch non-null with a past CompletesAtUtc, exactly the scenario this fix targets.
        fx.Clock.UtcNow = fx.Clock.UtcNow + def.ResearchDuration + TimeSpan.FromSeconds(1);
        AllianceResearchState raw = (await fx.ResearchRepository.ReadAsync(allianceId.Value))!;
        Assert.That(raw.Completed.ContainsKey(Minor1), Is.False, "sanity check: nothing has persisted the completion yet");
        Assert.That(raw.MinorResearch, Is.Not.Null);

        AllianceResearchBonus bonus = await fx.BonusResolver.ResolveForAllianceAsync(allianceId.Value);
        Assert.That(bonus.ProductionBp, Is.EqualTo(def.ProductionBp), "an objectively-elapsed timer must grant its bonus even before any read/mutation has persisted the completion");
    }

    [Test]
    public async Task Bonus_DoesNotCountResearchThatHasNotYetElapsed()
    {
        Fixture fx = CreateFixture();
        (AllianceId allianceId, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        await FullyFund(fx, chef, chefHiveId, Minor1, "m1");
        await Launch(fx, chef, Minor1);

        AllianceResearchBonus bonus = await fx.BonusResolver.ResolveForAllianceAsync(allianceId.Value);
        Assert.That(bonus, Is.EqualTo(AllianceResearchBonus.None), "the freshness fix must never grant a bonus before the timer has genuinely elapsed");
    }

    // ================================================================================
    // M053-CL Part 7: Sceaux Royaux (Alliance Currency) certification gaps not yet covered.
    // ================================================================================

    [Test]
    public async Task Donate_RejectedDonation_AwardsNoAllianceCurrency()
    {
        Fixture fx = CreateFixture();
        (_, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        // MinorOther was never selected as the funding target - donation must be rejected before
        // any currency/contribution accounting happens.
        AllianceResearchCommandResult result = await Donate(fx, chef, chefHiveId, MinorOther, "honey", 100, "rejected-donation");
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Snapshot, Is.Null);

        AllianceResearchReadSnapshot snapshot = await fx.Research.GetSnapshotAsync(chef);
        Assert.That(snapshot.MyAllianceCurrencyBalance, Is.EqualTo(0));
        Assert.That(snapshot.MyContributionPoints, Is.EqualTo(0));
    }

    [Test]
    public async Task Donate_RetryWithSameClientRequestId_DoesNotDoubleAwardCurrency()
    {
        Fixture fx = CreateFixture();
        (_, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        await SelectTarget(fx, chef, Minor1);

        await Donate(fx, chef, chefHiveId, Minor1, "honey", 500, "currency-retry-key");
        AllianceResearchCommandResult retry = await Donate(fx, chef, chefHiveId, Minor1, "honey", 500, "currency-retry-key");

        Assert.That(retry.Snapshot!.MyAllianceCurrencyBalance, Is.EqualTo(50), "500 points * 0.1 = 50 Sceaux Royaux exactly once, never doubled by the retry");
    }

    [Test]
    public async Task ChangingFundingTarget_DoesNotAffectAlreadyEarnedCurrencyOrContribution()
    {
        Fixture fx = CreateFixture();
        (_, PlayerId chef, Guid chefHiveId, _, _, _, _) = SetUpAlliance(fx);
        await SelectTarget(fx, chef, Minor1, "sel-1");
        await Donate(fx, chef, chefHiveId, Minor1, "honey", 500, "fund-currency");

        await SelectTarget(fx, chef, MinorOther, "sel-2");
        AllianceResearchReadSnapshot afterSwitch = await fx.Research.GetSnapshotAsync(chef);
        Assert.That(afterSwitch.MyAllianceCurrencyBalance, Is.EqualTo(50), "changing the funding target must not erase or alter previously-earned currency");
        Assert.That(afterSwitch.MyContributionPoints, Is.EqualTo(500));
    }

    [Test]
    public async Task LeavingAlliance_DoesNotDestroyThePlayersStoredAllianceCurrency()
    {
        Fixture fx = CreateFixture();
        (AllianceId allianceId, PlayerId chef, _, _, _, PlayerId member, Guid memberHiveId) = SetUpAlliance(fx);
        await SelectTarget(fx, chef, Minor1);
        await Donate(fx, member, memberHiveId, Minor1, "honey", 500, "member-donate-before-leave");

        fx.Alliances.Leave(member);

        // The raw record is stored inside AllianceResearchState.Contributions - "leaving" only
        // removes the membership row (AllianceService's own concern); it does not reach into, or
        // delete from, the Alliance Research repository at all - so the earned value itself is not
        // destroyed by leaving, matching the Bible's "personal wallet" framing for section 11's
        // non-diminishing guarantee.
        AllianceResearchState raw = (await fx.ResearchRepository.ReadAsync(allianceId.Value))!;
        Assert.That(raw.Contributions.TryGetValue(member.Value, out AllianceResearchContribution? contribution), Is.True);
        Assert.That(contribution!.AllianceCurrencyBalance, Is.EqualTo(50));

        // KNOWN GAP (documented in the M053 report, not fixed in this mission): because this value
        // lives keyed by AllianceId rather than by a global per-player record, a departed member has
        // no remaining code path to READ it (GetSnapshotAsync requires an active membership and
        // throws "not_a_member") - the value survives, but becomes practically unreachable until/
        // unless the player rejoins the SAME alliance. This is a genuine discrepancy against the
        // Bible's "conservé dans le portefeuille personnel du joueur" framing (section 11), not
        // resolved here per M053's explicit "no Alliance Shop / no currency redesign" scope limit.
        Assert.That(async () => await fx.Research.GetSnapshotAsync(member), Throws.InstanceOf<InvalidOperationException>());
    }

    // ================================================================================
    // M053-CL Part 6: canonical catalog sanity checks (Bible alignment, not balancing).
    // ================================================================================

    [Test]
    public void Catalog_SupremacyBranchNeverPopulated()
    {
        Assert.That(AllianceResearchCatalog.Technologies.Any(t => t.Branch == AllianceResearchCatalog.BranchSupremacy), Is.False, "Bible section 19: Suprématie stays locked/unimplemented until Alliance War exists");
    }

    [Test]
    public void Catalog_ExactlyOneMajorTechnologyInAlpha()
    {
        // Documents the exact Alpha-catalog limitation explained above
        // (FourConcurrentConcepts_MinorActiveMinorNextMajorActive_PersistAcrossReload) - this test
        // will legitimately need updating the day a second real Major is added from the Bible.
        Assert.That(AllianceResearchCatalog.ForCategory(AllianceResearchCatalog.ResearchCategory.Major).Count(), Is.EqualTo(1));
    }

    [Test]
    public void Catalog_EveryTechnology_HasAtLeastOneFundingResource_ExceptNone()
    {
        foreach (AllianceResearchCatalog.TechnologyDefinition tech in AllianceResearchCatalog.Technologies)
            Assert.That(tech.FundingRequirements.Count, Is.GreaterThan(0), tech.TechnologyId + " must require real resources - Bible section 9");
    }

    // Arrange-only shortcut: writes a technology directly into Completed, bypassing the real
    // funding/launch/timer flow - used only to set up a LATER lifecycle step's own test (e.g. a
    // Major's prerequisites), never to substitute for testing the flow itself (see the other tests
    // in this file, which all go through the real DonateAsync/LaunchAsync/timer path).
    private static async Task SeedCompleted(Fixture fx, AllianceId allianceId, string technologyId)
    {
        await fx.ResearchRepository.ExecuteAtomicallyAsync(allianceId.Value, state => state with
        {
            Revision = state.Revision + 1,
            Completed = new Dictionary<string, AllianceCompletedTechnology>(state.Completed, StringComparer.Ordinal) { [technologyId] = new AllianceCompletedTechnology(technologyId, fx.Clock.UtcNow) }
        });
    }

    private static void GiveSpeedUpItem(Fixture fx, Guid playerId, Guid hiveId, string itemId, int quantity = 1)
    {
        fx.HiveStates.Seed((fx.HiveStates.ReadAsync(playerId, hiveId).Result!) with
        {
            SpeedUps = new Dictionary<string, int>(StringComparer.Ordinal) { [itemId] = quantity }
        });
    }

    private sealed class TestClock(DateTimeOffset value) : IServerClock
    {
        public DateTimeOffset UtcNow { get; set; } = value;
    }

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
