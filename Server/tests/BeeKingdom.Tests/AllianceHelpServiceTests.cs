using BeeKingdom.Alliance;
using BeeKingdom.Alliance.Configuration;
using BeeKingdom.Alliance.Help;
using BeeKingdom.Alliance.Models;
using BeeKingdom.Alliance.Repositories;
using BeeKingdom.HiveOperations;
using BeeKingdom.Shared.ValueObjects;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Tests;

// M045-CL: exercises AllianceHelpService against the SAME OperationTimerReduction/PlayerHiveState
// shape SpeedUpInventoryService and the real Construction/Research/Training/Healing services mutate
// - never a parallel/fabricated timer model. AllianceService and AllianceHelpService both run against
// InMemory repositories (same convention as AllianceServiceTests) with the real membership authority
// (IAllianceRepository) shared between them, so leave/kick/dissolve sync is exercised end to end, not
// mocked away.
public sealed class AllianceHelpServiceTests
{
    private static PlayerId NewPlayer() => PlayerId.New();

    private sealed record Fixture(
        AllianceService Alliances,
        AllianceHelpService Help,
        MemoryHiveStateRepository HiveStates,
        TestClock Clock,
        InMemoryAllianceRepository AllianceRepository,
        InMemoryAllianceHelpRepository HelpRepository);

    private static Fixture CreateFixture(int maxHelpCount = 10, int minEligibleSeconds = 300, double percent = 0.01, int minReduction = 60, int maxReduction = 300)
    {
        var allianceOptions = Options.Create(new AllianceOptions { Enabled = true, MaxMembers = 100 });
        var allianceRepository = new InMemoryAllianceRepository();
        var helpRepository = new InMemoryAllianceHelpRepository();
        var hiveStates = new MemoryHiveStateRepository();
        var clock = new TestClock(DateTimeOffset.UtcNow);
        var helpOptions = Options.Create(new AllianceHelpOptions
        {
            Enabled = true,
            MaxHelpCount = maxHelpCount,
            MinEligibleOriginalDurationSeconds = minEligibleSeconds,
            ReductionPercentOfOriginalDuration = percent,
            MinReductionSeconds = minReduction,
            MaxReductionSeconds = maxReduction
        });

        var alliances = new AllianceService(
            allianceRepository, new InMemoryAllianceActivityRepository(), new InMemoryAllianceDiplomacyRepository(), new InMemoryAllianceWarRepository(),
            allianceOptions, allianceHelpRepository: helpRepository);
        var help = new AllianceHelpService(helpRepository, allianceRepository, hiveStates, helpOptions, clock);

        return new Fixture(alliances, help, hiveStates, clock, allianceRepository, helpRepository);
    }

    private static (AllianceId AllianceId, Guid RequesterHiveId) SetUpAllianceOfTwo(Fixture fixture, PlayerId leader, PlayerId member)
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
        Guid requesterHiveId = Guid.NewGuid();
        fixture.HiveStates.Seed(CreateStateWithConstruction(leader.Value, requesterHiveId, fixture.Clock.UtcNow, TimeSpan.FromMinutes(10)));
        return (alliance.AllianceId, requesterHiveId);
    }

    // ---------------- 1/2/3/4: request creation ----------------

    [Test]
    public async Task CreateRequest_MemberWithEligibleOperation_Succeeds()
    {
        Fixture fx = CreateFixture();
        PlayerId leader = NewPlayer(), member = NewPlayer();
        (AllianceId _, Guid hiveId) = SetUpAllianceOfTwo(fx, leader, member);

        AllianceHelpCommandResult result = await fx.Help.CreateRequestAsync(leader, new CreateAllianceHelpRequestCommand(hiveId, SpeedUpCategories.Construction, "honey_storage", "req-1"));

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.Request!.Status, Is.EqualTo(AllianceHelpRequestStatus.Open));
        Assert.That(result.Request.OriginalDurationSeconds, Is.EqualTo(600));
    }

    [Test]
    public async Task CreateRequest_NonMember_Rejected()
    {
        Fixture fx = CreateFixture();
        PlayerId lone = NewPlayer();
        Guid hiveId = Guid.NewGuid();
        fx.HiveStates.Seed(CreateStateWithConstruction(lone.Value, hiveId, fx.Clock.UtcNow, TimeSpan.FromMinutes(10)));

        AllianceHelpCommandResult result = await fx.Help.CreateRequestAsync(lone, new CreateAllianceHelpRequestCommand(hiveId, SpeedUpCategories.Construction, "honey_storage", "req-1"));

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Code, Is.EqualTo("not_a_member"));
    }

    [Test]
    public async Task CreateRequest_ForAnotherPlayersHive_Rejected()
    {
        Fixture fx = CreateFixture();
        PlayerId leader = NewPlayer(), member = NewPlayer();
        (AllianceId _, Guid leaderHiveId) = SetUpAllianceOfTwo(fx, leader, member);

        // `member` tries to request help for `leader`'s hive/operation - never trusted from the client.
        AllianceHelpCommandResult result = await fx.Help.CreateRequestAsync(member, new CreateAllianceHelpRequestCommand(leaderHiveId, SpeedUpCategories.Construction, "honey_storage", "req-1"));

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Code, Is.EqualTo("hive_not_owned"));
    }

    [Test]
    public async Task CreateRequest_OperationTooShort_Rejected()
    {
        Fixture fx = CreateFixture(minEligibleSeconds: 300);
        PlayerId leader = NewPlayer(), member = NewPlayer();
        (AllianceId allianceId, _) = SetUpAllianceOfTwo(fx, leader, member);
        Guid shortHiveId = Guid.NewGuid();
        fx.HiveStates.Seed(CreateStateWithConstruction(leader.Value, shortHiveId, fx.Clock.UtcNow, TimeSpan.FromMinutes(3)));

        AllianceHelpCommandResult result = await fx.Help.CreateRequestAsync(leader, new CreateAllianceHelpRequestCommand(shortHiveId, SpeedUpCategories.Construction, "honey_storage", "req-1"));

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Code, Is.EqualTo("operation_too_short"));
    }

    [Test]
    public async Task CreateRequest_Repeated_ReturnsSameOpenRequestInsteadOfError()
    {
        Fixture fx = CreateFixture();
        PlayerId leader = NewPlayer(), member = NewPlayer();
        (AllianceId _, Guid hiveId) = SetUpAllianceOfTwo(fx, leader, member);
        var command = new CreateAllianceHelpRequestCommand(hiveId, SpeedUpCategories.Construction, "honey_storage", "req-1");

        AllianceHelpCommandResult first = await fx.Help.CreateRequestAsync(leader, command);
        AllianceHelpCommandResult second = await fx.Help.CreateRequestAsync(leader, command);

        Assert.That(second.Succeeded, Is.True);
        Assert.That(second.Request!.HelpRequestId, Is.EqualTo(first.Request!.HelpRequestId));
    }

    // ---------------- 5/6/9/10/11/12/14/20: contribution ----------------

    [Test]
    public async Task Contribute_DifferentAllianceMemberHelpsOnce_ReducesRealTimerExactlyOnce()
    {
        Fixture fx = CreateFixture();
        PlayerId leader = NewPlayer(), helper = NewPlayer();
        (AllianceId _, Guid hiveId) = SetUpAllianceOfTwo(fx, leader, helper);
        AllianceHelpRequest request = (await fx.Help.CreateRequestAsync(leader, new CreateAllianceHelpRequestCommand(hiveId, SpeedUpCategories.Construction, "honey_storage", "req-1"))).Request!;
        DateTimeOffset before = fx.HiveStates.Get(leader.Value, hiveId)!.Operations[0].CompletesAtUtc;

        ContributeAllianceHelpResult result = await fx.Help.ContributeAsync(helper, request.HelpRequestId, "help-1");

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.Request!.HelpCount, Is.EqualTo(1));
        DateTimeOffset after = fx.HiveStates.Get(leader.Value, hiveId)!.Operations[0].CompletesAtUtc;
        Assert.That(after, Is.LessThan(before));
        // 1% of 600s = 6s, clamped to the configured 60s minimum.
        Assert.That((before - after).TotalSeconds, Is.EqualTo(60));

        // Reduction is real and persists on the next independent read (invariant 20: reconnect).
        Assert.That(fx.HiveStates.Get(leader.Value, hiveId)!.Operations[0].CompletesAtUtc, Is.EqualTo(after));
    }

    [Test]
    public async Task Contribute_RequesterCannotHelpOwnRequest()
    {
        Fixture fx = CreateFixture();
        PlayerId leader = NewPlayer(), member = NewPlayer();
        (AllianceId _, Guid hiveId) = SetUpAllianceOfTwo(fx, leader, member);
        AllianceHelpRequest request = (await fx.Help.CreateRequestAsync(leader, new CreateAllianceHelpRequestCommand(hiveId, SpeedUpCategories.Construction, "honey_storage", "req-1"))).Request!;

        ContributeAllianceHelpResult result = await fx.Help.ContributeAsync(leader, request.HelpRequestId, "help-1");

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Code, Is.EqualTo("cannot_help_own_request"));
    }

    [Test]
    public async Task Contribute_SameHelperTwice_SecondCallIsIdempotentNoDoubleReduction()
    {
        Fixture fx = CreateFixture();
        PlayerId leader = NewPlayer(), helper = NewPlayer();
        (AllianceId _, Guid hiveId) = SetUpAllianceOfTwo(fx, leader, helper);
        AllianceHelpRequest request = (await fx.Help.CreateRequestAsync(leader, new CreateAllianceHelpRequestCommand(hiveId, SpeedUpCategories.Construction, "honey_storage", "req-1"))).Request!;

        await fx.Help.ContributeAsync(helper, request.HelpRequestId, "help-1");
        DateTimeOffset afterFirst = fx.HiveStates.Get(leader.Value, hiveId)!.Operations[0].CompletesAtUtc;
        ContributeAllianceHelpResult replay = await fx.Help.ContributeAsync(helper, request.HelpRequestId, "help-1-retry");

        Assert.That(replay.Succeeded, Is.True);
        Assert.That(replay.Code, Is.EqualTo("already_helped"));
        Assert.That(replay.Request!.HelpCount, Is.EqualTo(1));
        Assert.That(fx.HiveStates.Get(leader.Value, hiveId)!.Operations[0].CompletesAtUtc, Is.EqualTo(afterFirst));
    }

    [Test]
    public async Task Contribute_SecondDifferentHelper_AlsoApplies()
    {
        Fixture fx = CreateFixture();
        PlayerId leader = NewPlayer(), helperA = NewPlayer(), helperB = NewPlayer();
        (AllianceId allianceId, Guid hiveId) = SetUpAllianceOfTwo(fx, leader, helperA);
        fx.AllianceRepository.SaveMembership(new AllianceMembership { AllianceId = allianceId, PlayerId = helperB, Role = AllianceRole.Member, JoinedAtUtc = fx.Clock.UtcNow, LastRoleChangedAtUtc = fx.Clock.UtcNow, Revision = 0 });
        AllianceHelpRequest request = (await fx.Help.CreateRequestAsync(leader, new CreateAllianceHelpRequestCommand(hiveId, SpeedUpCategories.Construction, "honey_storage", "req-1"))).Request!;

        await fx.Help.ContributeAsync(helperA, request.HelpRequestId, "help-a");
        ContributeAllianceHelpResult second = await fx.Help.ContributeAsync(helperB, request.HelpRequestId, "help-b");

        Assert.That(second.Succeeded, Is.True);
        Assert.That(second.Request!.HelpCount, Is.EqualTo(2));
    }

    [Test]
    public async Task Contribute_HelperFromAnotherAlliance_Rejected()
    {
        Fixture fx = CreateFixture();
        PlayerId leader = NewPlayer(), member = NewPlayer(), outsider = NewPlayer();
        (AllianceId _, Guid hiveId) = SetUpAllianceOfTwo(fx, leader, member);
        fx.Alliances.CreateAlliance(outsider, new CreateAllianceRequest("Other Hive", "OTH", "desc", "fr-CA", "", AllianceJoinMode.Open, "create-" + outsider.Value));
        AllianceHelpRequest request = (await fx.Help.CreateRequestAsync(leader, new CreateAllianceHelpRequestCommand(hiveId, SpeedUpCategories.Construction, "honey_storage", "req-1"))).Request!;

        ContributeAllianceHelpResult result = await fx.Help.ContributeAsync(outsider, request.HelpRequestId, "help-1");

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Code, Is.EqualTo("different_alliance"));
    }

    [Test]
    public async Task Contribute_MaxHelpCountEnforced()
    {
        Fixture fx = CreateFixture(maxHelpCount: 1);
        PlayerId leader = NewPlayer(), helperA = NewPlayer(), helperB = NewPlayer();
        (AllianceId allianceId, Guid hiveId) = SetUpAllianceOfTwo(fx, leader, helperA);
        fx.AllianceRepository.SaveMembership(new AllianceMembership { AllianceId = allianceId, PlayerId = helperB, Role = AllianceRole.Member, JoinedAtUtc = fx.Clock.UtcNow, LastRoleChangedAtUtc = fx.Clock.UtcNow, Revision = 0 });
        AllianceHelpRequest request = (await fx.Help.CreateRequestAsync(leader, new CreateAllianceHelpRequestCommand(hiveId, SpeedUpCategories.Construction, "honey_storage", "req-1"))).Request!;

        ContributeAllianceHelpResult first = await fx.Help.ContributeAsync(helperA, request.HelpRequestId, "help-a");
        ContributeAllianceHelpResult second = await fx.Help.ContributeAsync(helperB, request.HelpRequestId, "help-b");

        Assert.That(first.Succeeded, Is.True);
        Assert.That(first.Request!.Status, Is.EqualTo(AllianceHelpRequestStatus.Completed));
        Assert.That(second.Succeeded, Is.False);
        // Reaching MaxHelpCount atomically flips the request to Completed in the same transaction
        // as the HelpCount increment (see IAllianceHelpRepository.TryContributeAsync) - a later
        // caller always observes "request_not_open" first, never a Status=Open/HelpCount>=Max gap.
        Assert.That(second.Code, Is.EqualTo("request_not_open"));
    }

    [Test]
    public async Task Contribute_ConcurrentHelpsCannotExceedMaxHelpCount()
    {
        Fixture fx = CreateFixture(maxHelpCount: 1);
        PlayerId leader = NewPlayer(), helperA = NewPlayer(), helperB = NewPlayer();
        (AllianceId allianceId, Guid hiveId) = SetUpAllianceOfTwo(fx, leader, helperA);
        fx.AllianceRepository.SaveMembership(new AllianceMembership { AllianceId = allianceId, PlayerId = helperB, Role = AllianceRole.Member, JoinedAtUtc = fx.Clock.UtcNow, LastRoleChangedAtUtc = fx.Clock.UtcNow, Revision = 0 });
        AllianceHelpRequest request = (await fx.Help.CreateRequestAsync(leader, new CreateAllianceHelpRequestCommand(hiveId, SpeedUpCategories.Construction, "honey_storage", "req-1"))).Request!;

        Task<ContributeAllianceHelpResult> first = fx.Help.ContributeAsync(helperA, request.HelpRequestId, "help-a");
        Task<ContributeAllianceHelpResult> second = fx.Help.ContributeAsync(helperB, request.HelpRequestId, "help-b");
        ContributeAllianceHelpResult[] results = await Task.WhenAll(first, second);

        Assert.That(results.Count(result => result.Succeeded), Is.EqualTo(1));
        Assert.That(fx.HiveStates.Get(leader.Value, hiveId)!.Operations[0].Status, Is.Not.EqualTo(HiveOperationStatus.Collected));
    }

    [Test]
    public async Task Contribute_NeverMakesRemainingDurationNegative()
    {
        // MaxReductionSeconds (300) exceeds the operation's own remaining time (90s) - the applied
        // reduction must clamp to the remaining duration, never overshoot past "now".
        Fixture fx = CreateFixture(minEligibleSeconds: 60, minReduction: 300, maxReduction: 300);
        PlayerId leader = NewPlayer(), helper = NewPlayer();
        AllianceEntity alliance = fx.Alliances.CreateAlliance(leader, new CreateAllianceRequest("Golden Hive", "GLD", "desc", "fr-CA", "", AllianceJoinMode.Open, "create-" + leader.Value)).Alliance;
        fx.AllianceRepository.SaveMembership(new AllianceMembership { AllianceId = alliance.AllianceId, PlayerId = helper, Role = AllianceRole.Member, JoinedAtUtc = fx.Clock.UtcNow, LastRoleChangedAtUtc = fx.Clock.UtcNow, Revision = 0 });
        Guid hiveId = Guid.NewGuid();
        fx.HiveStates.Seed(CreateStateWithConstruction(leader.Value, hiveId, fx.Clock.UtcNow, TimeSpan.FromSeconds(90)));
        AllianceHelpRequest request = (await fx.Help.CreateRequestAsync(leader, new CreateAllianceHelpRequestCommand(hiveId, SpeedUpCategories.Construction, "honey_storage", "req-1"))).Request!;

        ContributeAllianceHelpResult result = await fx.Help.ContributeAsync(helper, request.HelpRequestId, "help-1");

        Assert.That(result.Succeeded, Is.True);
        HiveOperation operation = fx.HiveStates.Get(leader.Value, hiveId)!.Operations[0];
        Assert.That(operation.CompletesAtUtc, Is.EqualTo(fx.Clock.UtcNow));
        Assert.That(operation.Status, Is.EqualTo(HiveOperationStatus.AwaitingCollection));
    }

    [Test]
    public async Task Contribute_CompletedOperation_RejectsHelp()
    {
        Fixture fx = CreateFixture();
        PlayerId leader = NewPlayer(), helper = NewPlayer();
        (AllianceId _, Guid hiveId) = SetUpAllianceOfTwo(fx, leader, helper);
        AllianceHelpRequest request = (await fx.Help.CreateRequestAsync(leader, new CreateAllianceHelpRequestCommand(hiveId, SpeedUpCategories.Construction, "honey_storage", "req-1"))).Request!;

        // The real operation completes independently of Alliance Help (e.g. time simply passes) -
        // simulate that by collecting it directly against the hive state.
        fx.HiveStates.Mutate(leader.Value, hiveId, state =>
        {
            HiveOperation op = state.Operations[0];
            return state with { Operations = [op with { Status = HiveOperationStatus.Collected, CollectedAtUtc = fx.Clock.UtcNow }] };
        });

        ContributeAllianceHelpResult result = await fx.Help.ContributeAsync(helper, request.HelpRequestId, "help-1");

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Code, Is.EqualTo("operation_completed"));
    }

    // ---------------- 16/17/18: membership lifecycle ----------------

    [Test]
    public async Task Leave_BlocksFurtherContributionsToRequesterOwnOpenRequest()
    {
        Fixture fx = CreateFixture();
        PlayerId leader = NewPlayer(), member = NewPlayer(), helper = NewPlayer();
        (AllianceId allianceId, Guid hiveId) = SetUpAllianceOfTwo(fx, leader, member);
        fx.AllianceRepository.SaveMembership(new AllianceMembership { AllianceId = allianceId, PlayerId = helper, Role = AllianceRole.Member, JoinedAtUtc = fx.Clock.UtcNow, LastRoleChangedAtUtc = fx.Clock.UtcNow, Revision = 0 });
        AllianceHelpRequest request = (await fx.Help.CreateRequestAsync(member, new CreateAllianceHelpRequestCommand(
            // member requests help for their own hive, seeded separately below
            SeedMemberHive(fx, member), SpeedUpCategories.Construction, "honey_storage", "req-1"))).Request!;

        fx.Alliances.Leave(member);
        ContributeAllianceHelpResult result = await fx.Help.ContributeAsync(helper, request.HelpRequestId, "help-1");

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Code, Is.EqualTo("request_not_open"));
    }

    [Test]
    public async Task Kick_BlocksFurtherContributionsToTargetOpenRequest()
    {
        Fixture fx = CreateFixture();
        PlayerId leader = NewPlayer(), member = NewPlayer(), helper = NewPlayer();
        (AllianceId allianceId, Guid hiveId) = SetUpAllianceOfTwo(fx, leader, member);
        fx.AllianceRepository.SaveMembership(new AllianceMembership { AllianceId = allianceId, PlayerId = helper, Role = AllianceRole.Member, JoinedAtUtc = fx.Clock.UtcNow, LastRoleChangedAtUtc = fx.Clock.UtcNow, Revision = 0 });
        AllianceHelpRequest request = (await fx.Help.CreateRequestAsync(member, new CreateAllianceHelpRequestCommand(
            SeedMemberHive(fx, member), SpeedUpCategories.Construction, "honey_storage", "req-1"))).Request!;

        fx.Alliances.Kick(leader, member);
        ContributeAllianceHelpResult result = await fx.Help.ContributeAsync(helper, request.HelpRequestId, "help-1");

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Code, Is.EqualTo("request_not_open"));
    }

    [Test]
    public async Task LeadershipTransfer_DoesNotInvalidateExistingRequest()
    {
        Fixture fx = CreateFixture();
        PlayerId leader = NewPlayer(), member = NewPlayer();
        (AllianceId _, Guid hiveId) = SetUpAllianceOfTwo(fx, leader, member);
        AllianceHelpRequest request = (await fx.Help.CreateRequestAsync(leader, new CreateAllianceHelpRequestCommand(hiveId, SpeedUpCategories.Construction, "honey_storage", "req-1"))).Request!;

        fx.Alliances.TransferLeadership(leader, member);
        ContributeAllianceHelpResult result = await fx.Help.ContributeAsync(member, request.HelpRequestId, "help-1");

        Assert.That(result.Succeeded, Is.True);
    }

    // ---------------- 21/22/23/24: per-category adapters ----------------

    [Test]
    public async Task ResearchAdapter_HelpReducesRealResearchTimer()
    {
        Fixture fx = CreateFixture();
        PlayerId leader = NewPlayer(), helper = NewPlayer();
        AllianceEntity alliance = fx.Alliances.CreateAlliance(leader, new CreateAllianceRequest("Golden Hive", "GLD", "desc", "fr-CA", "", AllianceJoinMode.Open, "create-" + leader.Value)).Alliance;
        fx.AllianceRepository.SaveMembership(new AllianceMembership { AllianceId = alliance.AllianceId, PlayerId = helper, Role = AllianceRole.Member, JoinedAtUtc = fx.Clock.UtcNow, LastRoleChangedAtUtc = fx.Clock.UtcNow, Revision = 0 });
        Guid hiveId = Guid.NewGuid();
        fx.HiveStates.Seed(CreateStateWithResearch(leader.Value, hiveId, fx.Clock.UtcNow, TimeSpan.FromMinutes(10)));
        AllianceHelpRequest request = (await fx.Help.CreateRequestAsync(leader, new CreateAllianceHelpRequestCommand(hiveId, SpeedUpCategories.Research, "foraging_routes_i", "req-1"))).Request!;

        ContributeAllianceHelpResult result = await fx.Help.ContributeAsync(helper, request.HelpRequestId, "help-1");

        Assert.That(result.Succeeded, Is.True);
        Assert.That(fx.HiveStates.Get(leader.Value, hiveId)!.Research!.ActiveOperation!.EndsAtUtc, Is.EqualTo(fx.Clock.UtcNow.AddMinutes(10).AddSeconds(-60)));
    }

    [Test]
    public async Task TrainingAdapter_HelpReducesRealTrainingTimer()
    {
        Fixture fx = CreateFixture();
        PlayerId leader = NewPlayer(), helper = NewPlayer();
        AllianceEntity alliance = fx.Alliances.CreateAlliance(leader, new CreateAllianceRequest("Golden Hive", "GLD", "desc", "fr-CA", "", AllianceJoinMode.Open, "create-" + leader.Value)).Alliance;
        fx.AllianceRepository.SaveMembership(new AllianceMembership { AllianceId = alliance.AllianceId, PlayerId = helper, Role = AllianceRole.Member, JoinedAtUtc = fx.Clock.UtcNow, LastRoleChangedAtUtc = fx.Clock.UtcNow, Revision = 0 });
        Guid hiveId = Guid.NewGuid();
        fx.HiveStates.Seed(CreateStateWithTraining(leader.Value, hiveId, fx.Clock.UtcNow, TimeSpan.FromMinutes(10)));
        AllianceHelpRequest request = (await fx.Help.CreateRequestAsync(leader, new CreateAllianceHelpRequestCommand(hiveId, SpeedUpCategories.Training, "guardians", "req-1"))).Request!;

        ContributeAllianceHelpResult result = await fx.Help.ContributeAsync(helper, request.HelpRequestId, "help-1");

        Assert.That(result.Succeeded, Is.True);
        Assert.That(fx.HiveStates.Get(leader.Value, hiveId)!.DoctrineRoster!.ActiveOperation!.EndsAtUtc, Is.EqualTo(fx.Clock.UtcNow.AddMinutes(10).AddSeconds(-60)));
    }

    [Test]
    public async Task HealingAdapter_HelpReducesRealHealingTimer()
    {
        // M045-CL: real Brood Vitality durations are hardcoded to 12s/13s (BroodVitalityCareService)
        // - always below MinEligibleOriginalDurationSeconds in real gameplay, so this uses a
        // synthetic longer duration purely to exercise the adapter itself (matches the mission's own
        // "use a legitimate longer operation during testing... do not distort live Alpha economy"
        // guidance). Human runtime certification against the real 12s/13s healing operation is
        // therefore N/A for Alpha and explicitly documented as such in the mission report, not
        // silently assumed to work.
        Fixture fx = CreateFixture();
        PlayerId leader = NewPlayer(), helper = NewPlayer();
        AllianceEntity alliance = fx.Alliances.CreateAlliance(leader, new CreateAllianceRequest("Golden Hive", "GLD", "desc", "fr-CA", "", AllianceJoinMode.Open, "create-" + leader.Value)).Alliance;
        fx.AllianceRepository.SaveMembership(new AllianceMembership { AllianceId = alliance.AllianceId, PlayerId = helper, Role = AllianceRole.Member, JoinedAtUtc = fx.Clock.UtcNow, LastRoleChangedAtUtc = fx.Clock.UtcNow, Revision = 0 });
        Guid hiveId = Guid.NewGuid();
        fx.HiveStates.Seed(CreateStateWithHealing(leader.Value, hiveId, fx.Clock.UtcNow, TimeSpan.FromMinutes(10)));
        AllianceHelpRequest request = (await fx.Help.CreateRequestAsync(leader, new CreateAllianceHelpRequestCommand(hiveId, SpeedUpCategories.Healing, "feeding", "req-1"))).Request!;

        ContributeAllianceHelpResult result = await fx.Help.ContributeAsync(helper, request.HelpRequestId, "help-1");

        Assert.That(result.Succeeded, Is.True);
        Assert.That(fx.HiveStates.Get(leader.Value, hiveId)!.BroodVitality!.ActiveOperation!.EndsAtUtc, Is.EqualTo(fx.Clock.UtcNow.AddMinutes(10).AddSeconds(-60)));
    }

    // ---------------- 25: Help All ----------------

    [Test]
    public async Task ContributeAll_AppliesAtMostOneContributionPerEligibleRequest()
    {
        Fixture fx = CreateFixture();
        PlayerId leader = NewPlayer(), other = NewPlayer(), helper = NewPlayer();
        AllianceEntity alliance = fx.Alliances.CreateAlliance(leader, new CreateAllianceRequest("Golden Hive", "GLD", "desc", "fr-CA", "", AllianceJoinMode.Open, "create-" + leader.Value)).Alliance;
        foreach (PlayerId p in new[] { other, helper })
            fx.AllianceRepository.SaveMembership(new AllianceMembership { AllianceId = alliance.AllianceId, PlayerId = p, Role = AllianceRole.Member, JoinedAtUtc = fx.Clock.UtcNow, LastRoleChangedAtUtc = fx.Clock.UtcNow, Revision = 0 });

        Guid leaderHiveId = Guid.NewGuid(), otherHiveId = Guid.NewGuid();
        fx.HiveStates.Seed(CreateStateWithConstruction(leader.Value, leaderHiveId, fx.Clock.UtcNow, TimeSpan.FromMinutes(10)));
        fx.HiveStates.Seed(CreateStateWithConstruction(other.Value, otherHiveId, fx.Clock.UtcNow, TimeSpan.FromMinutes(10)));
        AllianceHelpRequest requestA = (await fx.Help.CreateRequestAsync(leader, new CreateAllianceHelpRequestCommand(leaderHiveId, SpeedUpCategories.Construction, "honey_storage", "req-a"))).Request!;
        AllianceHelpRequest requestB = (await fx.Help.CreateRequestAsync(other, new CreateAllianceHelpRequestCommand(otherHiveId, SpeedUpCategories.Construction, "honey_storage", "req-b"))).Request!;

        ContributeAllianceHelpAllResult first = await fx.Help.ContributeAllAsync(helper, "help-all-1");
        ContributeAllianceHelpAllResult replay = await fx.Help.ContributeAllAsync(helper, "help-all-2");

        Assert.That(first.Results, Has.Count.EqualTo(2));
        Assert.That(first.Results, Has.All.Matches<ContributeAllianceHelpResult>(r => r.Succeeded));
        // Nothing left to help on replay - both requests already carry this helper's contribution.
        Assert.That(replay.Results, Is.Empty);
        Assert.That((await fx.Help.GetMyOpenRequestAsync(leader, SpeedUpCategories.Construction, "honey_storage"))!.HelpCount, Is.EqualTo(1));
        Assert.That((await fx.Help.GetMyOpenRequestAsync(other, SpeedUpCategories.Construction, "honey_storage"))!.HelpCount, Is.EqualTo(1));
        _ = requestA; _ = requestB;
    }

    // ---------------- fixtures ----------------

    private static Guid SeedMemberHive(Fixture fx, PlayerId player)
    {
        Guid hiveId = Guid.NewGuid();
        fx.HiveStates.Seed(CreateStateWithConstruction(player.Value, hiveId, fx.Clock.UtcNow, TimeSpan.FromMinutes(10)));
        return hiveId;
    }

    private static PlayerHiveState CreateStateWithConstruction(Guid playerId, Guid hiveId, DateTimeOffset now, TimeSpan duration) => new(
        playerId, hiveId, 1, 0,
        new Dictionary<string, ResourceBalance>(), new Dictionary<string, int>(),
        new List<HiveOperation> { new(Guid.NewGuid(), "honey_storage", 1, 2, now, now + duration, HiveOperationStatus.Running, "", 0, null) },
        new Dictionary<string, IdempotencyReceipt>());

    private static PlayerHiveState CreateStateWithResearch(Guid playerId, Guid hiveId, DateTimeOffset now, TimeSpan duration) => new(
        playerId, hiveId, 1, 0,
        new Dictionary<string, ResourceBalance>(), new Dictionary<string, int>(),
        new List<HiveOperation>(), new Dictionary<string, IdempotencyReceipt>(),
        Research: new HiveResearchState(new Dictionary<string, ResearchCompletion>(), new ResearchOperation(Guid.NewGuid(), "foraging_routes_i", now, now + duration, 0)));

    private static PlayerHiveState CreateStateWithTraining(Guid playerId, Guid hiveId, DateTimeOffset now, TimeSpan duration) => new(
        playerId, hiveId, 1, 0,
        new Dictionary<string, ResourceBalance>(), new Dictionary<string, int>(),
        new List<HiveOperation>(), new Dictionary<string, IdempotencyReceipt>(),
        DoctrineRoster: new DoctrineRosterState(0, new Dictionary<string, long>(), new DoctrineTrainingOperation(Guid.NewGuid(), "guardians", 5, now, now + duration, 0, "seed", "seed-hash", false), new Dictionary<string, IdempotencyReceipt>()));

    private static PlayerHiveState CreateStateWithHealing(Guid playerId, Guid hiveId, DateTimeOffset now, TimeSpan duration) => new(
        playerId, hiveId, 1, 0,
        new Dictionary<string, ResourceBalance>(), new Dictionary<string, int>(),
        new List<HiveOperation>(), new Dictionary<string, IdempotencyReceipt>(),
        BroodVitality: new BroodVitalityState(50, 50, 0, now, new BroodVitalityOperation(Guid.NewGuid(), "feeding", now, now + duration)));

    private sealed class TestClock(DateTimeOffset value) : IServerClock
    {
        public DateTimeOffset UtcNow { get; } = value;
    }

    // Multi-player-aware version of the single-state MemoryRepository pattern already established
    // in SpeedUpInventoryServiceTests - Alliance Help genuinely needs more than one player's hive
    // state in the same repository (the helper's own alliance membership lives elsewhere; the
    // OPERATION being helped lives on the requester's hive).
    private sealed class MemoryHiveStateRepository : IHiveStateRepository
    {
        private readonly object gate = new();
        private readonly Dictionary<(Guid PlayerId, Guid HiveId), PlayerHiveState> states = new();

        public void Seed(PlayerHiveState state) { lock (gate) states[(state.PlayerId, state.HiveId)] = state; }
        public PlayerHiveState? Get(Guid playerId, Guid hiveId) { lock (gate) return states.TryGetValue((playerId, hiveId), out PlayerHiveState? value) ? value : null; }
        public void Mutate(Guid playerId, Guid hiveId, Func<PlayerHiveState, PlayerHiveState> mutation) { lock (gate) states[(playerId, hiveId)] = mutation(states[(playerId, hiveId)]); }

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
