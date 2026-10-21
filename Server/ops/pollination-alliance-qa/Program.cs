using BeeKingdom.HiveOperations;
using Microsoft.Data.SqlClient;
using System.Data;

string connection = Environment.GetEnvironmentVariable("BEEKINGDOM_POLLINATION_QA_SQL")
    ?? @"Server=(localdb)\BKPollinationQA;Database=BeeKingdomPollinationQA;Integrated Security=True;Encrypt=False;Connect Timeout=15";
string suffix = Guid.NewGuid().ToString("N")[..12];
Guid alliance = Guid.NewGuid(), leader = Guid.NewGuid(),
    member = Guid.NewGuid(), outsider = Guid.NewGuid();
Guid leaderHive = Guid.NewGuid(), memberHive = Guid.NewGuid();
string world = "world-qa-" + suffix;
string siteId = "floral-qa-" + suffix;
var startTime = new DateTimeOffset(2026, 10, 21, 19, 0, 0, TimeSpan.Zero);
var clock = new AdjustableClock { UtcNow = startTime };
var projectOptions = new FloralAllianceProjectOptions
{
    Enabled = true, GoalPoints = 20, RequiredDistinctMembers = 2,
    MinimumPointsPerMember = 5, LevelTwoExperience = 20,
    LevelThreeExperience = 100
};
var floral = new FloralSiteOptions
{
    Enabled = true, CatalogVersion = "p4.qa.v1",
    Sites = [new(world, "region-qa", siteId, "clover",
        "Trèfles collectifs", "Cooperative clover", 30, 6200, 5400)]
};
var expeditionRules = new CivilExpeditionOptions
{
    Enabled = true, DurationMinutes = 15, MaxWorkers = 50,
    PollenPerWorker = 1, FertilityGainBp = 5,
    BiodiversityGainBp = 2
};
var hiveRepository = new SqlHiveStateRepository(connection, NewHive);
var coordinator = new SqlCivilExpeditionUnitOfWork(
    connection, collectiveOptions: projectOptions);
var projectService = new SqlFloralAllianceProjectService(
    connection, clock, projectOptions, floral);
var expeditionService = new CivilExpeditionService(
    hiveRepository, coordinator, clock, expeditionRules, floral);
var sites = new SqlFloralSiteRepository(connection);
Guid alternativeAlliance = Guid.NewGuid();

try
{
    await SeedAlliance(connection, alliance, leader, member, suffix, startTime);
    await hiveRepository.ExecuteAtomicallyAsync(leader, leaderHive, s=>s);
    await hiveRepository.ExecuteAtomicallyAsync(member, memberHive, s=>s);
    var site = await sites.ReadOrCreateAsync(floral.Sites[0],
        floral.CatalogVersion, clock.UtcNow);
    Check(site.PollenRemaining == 30, "official shared floral site seeded");

    var create = new CreateFloralAllianceProjectRequest(
        siteId, 0, "collective-project-qa");
    var created = await projectService.CreateAsync(leader, alliance, create);
    Check(created.Succeeded && created.Snapshot!.Project.Status == "Active",
        "leader created genuine collective project");
    Guid projectId = created.Snapshot!.Project.ProjectId;
    var replayCreate = await projectService.CreateAsync(leader, alliance, create);
    Check(replayCreate.Succeeded &&
        replayCreate.Snapshot!.Project.ProjectId == projectId &&
        replayCreate.Snapshot.Progression.Revision == 1,
        "create idempotency key replays without second project");
    bool rejectedMember = false;
    try { await projectService.CreateAsync(member, alliance,
        new(siteId, 1, "member-forbidden-qa")); }
    catch (UnauthorizedAccessException) { rejectedMember = true; }
    Check(rejectedMember, "ordinary member cannot create alliance projects");
    bool rejectedOutsider = false;
    try { await projectService.ReadAsync(outsider, alliance, projectId); }
    catch (UnauthorizedAccessException) { rejectedOutsider = true; }
    Check(rejectedOutsider, "nonmember cannot read private alliance project");

    CivilExpeditionResult first = await expeditionService.StartAsync(
        leader, leaderHive, new(siteId, 25, 0, 0, "leader-flight-qa"));
    Check(first.Succeeded, "leader sent 25 civilians");
    Check(first.Snapshot.Active!.AllianceIdAtStart == alliance,
        "SQL flight snapshots the official alliance at departure");
    clock.UtcNow = startTime.AddMinutes(17);

    // Duplicate unique delivery while the official world/house ledger is clean:
    // the whole settlement transaction MUST roll back with no money or XP.
    await InjectDuplicateProjectDelivery(connection, first.Receipt!.MissionId,
        projectId, leader, clock.UtcNow);
    bool failedAtomic = false;
    try { await expeditionService.ReadAsync(leader, leaderHive); }
    catch (SqlException sqlError) when (sqlError.Number is 2601 or 2627)
    { failedAtomic = true; }
    Check(failedAtomic, "project delivery uniqueness aborts SQL transaction");
    var rollback = (await hiveRepository.ReadAsync(leader, leaderHive))!;
    site = await sites.ReadOrCreateAsync(floral.Sites[0], floral.CatalogVersion, clock.UtcNow);
    Check(rollback.CivilPopulation!.AssignedWorkers == 25
        && rollback.Resources["pollen"].Amount == 0
        && site.PollenRemaining == 30,
        "failed collective delivery rolled back hive and field balances");
    Check((await projectService.ReadAsync(leader, alliance, projectId))
        .Project.Points == 0, "failed delivery did not advance project");
    await RemoveDuplicateProjectDelivery(connection, first.Receipt!.MissionId);

    var finishedA = await expeditionService.ReadAsync(leader, leaderHive);
    var soloProject = await projectService.ReadAsync(leader, alliance, projectId);
    Check(finishedA.LastCompletion!.PollenCredited == 25,
        "first player actually harvested 25 units");
    Check(soloProject.Project.Points == 25 &&
        soloProject.Project.QualifiedMembers == 1 &&
        soloProject.Project.Status == "Active" &&
        soloProject.Progression.Level == 1,
        "solo over-goal contributions do NOT complete project");

    CivilExpeditionResult second = await expeditionService.StartAsync(
        member, memberHive, new(siteId, 15, 0, 1, "member-flight-qa"));
    Check(second.Succeeded, "second distinct member launched expedition");
    clock.UtcNow = startTime.AddMinutes(40);
    var finishedB = await expeditionService.ReadAsync(member, memberHive);
    var completed = await projectService.ReadAsync(member, alliance, projectId);
    Check(finishedB.LastCompletion!.PollenCredited == 5,
        "shared stock clips second player to remaining five");
    Check(completed.IsCompleted && completed.Project.Points == 30
        && completed.Project.QualifiedMembers == 2,
        "two genuinely qualified members unlock collective objective");
    Check(completed.Progression.Level == 2
        && completed.Progression.Experience == 20
        && completed.Progression.CompletedProjects == 1,
        "alliance level advances once on cooperative completion");
    var summary = await projectService.ListAsync(member, alliance);
    Check(summary.Count == 1 && summary[0].Project.ProjectId == projectId,
        "member can read shared server-authoritative project list");
    await Task.WhenAll(expeditionService.ReadAsync(leader, leaderHive),
        expeditionService.ReadAsync(member, memberHive));
    var again = await projectService.ReadAsync(leader, alliance, projectId);
    Check(again.Progression.Experience == 20 &&
        again.Progression.CompletedProjects == 1 &&
        await CountProjectDeliveries(connection, projectId) == 2,
        "reopening and retry cannot duplicate collective progress or XP");
    site = await sites.ReadOrCreateAsync(
        floral.Sites[0], floral.CatalogVersion, clock.UtcNow);
    Check(site.PollenRemaining == 0 &&
        site.TotalPollinationPoints == 30 &&
        (await hiveRepository.ReadAsync(leader, leaderHive))!.Resources["pollen"].Amount
        + (await hiveRepository.ReadAsync(member, memberHive))!.Resources["pollen"].Amount
        == 30, "total world debit equals both player credits");

    var reopened = new SqlFloralAllianceProjectService(
        connection, clock, projectOptions, floral);
    var afterRestart = await reopened.ReadAsync(member, alliance, projectId);
    Check(afterRestart.IsCompleted && afterRestart.Progression.Level == 2,
        "project completion and alliance progression survive service restart");
    // Membership-switch anti-exploit: the player cannot launch while in A
    // and have the pollen credited to a different alliance B after joining B.
    string alternativeSite = "clover-switch-" + suffix;
    floral.Sites.Add(new(world, "region-qa", alternativeSite, "clover",
        "Champ anti-transfert", "Transfer protection field", 10, 6000, 5000));
    await sites.ReadOrCreateAsync(floral.Sites.Last(),
        floral.CatalogVersion, clock.UtcNow);
    await SeedAlternateAlliance(connection, alternativeAlliance,
        outsider, suffix, clock.UtcNow);
    var altProject = await projectService.CreateAsync(
        outsider, alternativeAlliance,
        new(alternativeSite, 0, "other-alliance-project"));
    Check(altProject.Succeeded, "second alliance project created before flight");
    var transferred = await expeditionService.StartAsync(member, memberHive,
        new(alternativeSite, 5,
            (await hiveRepository.ReadAsync(member, memberHive))!.Revision,
            0, "switch-membership-flight"));
    Check(transferred.Succeeded &&
        transferred.Snapshot.Active!.AllianceIdAtStart == alliance,
        "departure froze the original alliance before membership switch");
    await SwitchMembership(connection, alliance, alternativeAlliance,
        member, clock.UtcNow);
    clock.UtcNow = clock.UtcNow.AddMinutes(20);
    var transferResult = await expeditionService.ReadAsync(member, memberHive);
    var noncredited = await projectService.ReadAsync(
        outsider, alternativeAlliance, altProject.Snapshot!.Project.ProjectId);
    Check(transferResult.LastCompletion!.PollenCredited == 5
        && noncredited.Project.Points == 0 &&
        noncredited.Project.QualifiedMembers == 0,
        "flight credits personal pollen but cannot donate to new alliance");
    Console.WriteLine("P4_SQL_QA_PASS: authority + 2 players + anti-solo + "
        + "atomic rollback + finite ecology + one-time alliance level.");
}
finally
{
    await Cleanup(connection, alliance, alternativeAlliance, world, leader, member);
}

static PlayerHiveState NewHive(Guid player, Guid hive) =>
    new(player, hive, HiveStateMigrator.CurrentModelVersion, 0,
        new Dictionary<string, ResourceBalance>
        {
            ["honey"] = new(400, 1000),
            ["pollen"] = new(0, 100),
            ["wax"] = new(50, 100)
        },
        new Dictionary<string, int> { ["nursery_cluster"] = 1 },
        [], new Dictionary<string, IdempotencyReceipt>(),
        CivilPopulation: new CivilPopulationState(0, 40, 0, 0, null, new()));

static void Check(bool okay, string reason)
{
    if (!okay) throw new InvalidOperationException("P4_SQL_QA_FAIL: " + reason);
    Console.WriteLine("PASS " + reason);
}

static async Task SeedAlliance(string connection, Guid alliance,
    Guid leader, Guid member, string suffix, DateTimeOffset created)
{
    await using var conn = new SqlConnection(connection);
    await conn.OpenAsync();
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = """
        INSERT dbo.Alliances
            (AllianceId,Name,Tag,Description,Language,EmblemKey,
             JoinMode,Status,CreatedAtUtc,CreatedByPlayerId,LeaderPlayerId,
             MemberCount,MaxMembers,PublicSlug,ChatConversationId,
             Revision,DisbandedAtUtc)
        VALUES(@alliance,@name,@tag,N'',N'fr-CA',N'',
               N'Open',N'Active',@now,@leader,@leader,
               2,50,@slug,NULL,1,NULL);
        INSERT dbo.AllianceMemberships
            (AllianceId,PlayerId,Role,JoinedAtUtc,InvitedByPlayerId,
             ApplicationId,LastRoleChangedAtUtc,RemovedAtUtc,Revision)
        VALUES(@alliance,@leader,N'Leader',@now,NULL,NULL,@now,NULL,1),
              (@alliance,@member,N'Member',@now,NULL,NULL,@now,NULL,1);
        """;
    cmd.Parameters.AddWithValue("@alliance", alliance);
    cmd.Parameters.AddWithValue("@leader", leader);
    cmd.Parameters.AddWithValue("@member", member);
    cmd.Parameters.AddWithValue("@now", created.UtcDateTime);
    cmd.Parameters.AddWithValue("@name", "QA Floral Alliance " + suffix);
    cmd.Parameters.AddWithValue("@tag", "QA" + suffix[..5]);
    cmd.Parameters.AddWithValue("@slug", "qa-floral-" + suffix);
    await cmd.ExecuteNonQueryAsync();
}

static async Task SeedAlternateAlliance(string connection, Guid id,
    Guid outsider, string suffix, DateTimeOffset now)
{
    await using var conn = new SqlConnection(connection);
    await conn.OpenAsync();
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = """
        INSERT dbo.Alliances
            (AllianceId,Name,Tag,Description,Language,EmblemKey,
             JoinMode,Status,CreatedAtUtc,CreatedByPlayerId,LeaderPlayerId,
             MemberCount,MaxMembers,PublicSlug,ChatConversationId,
             Revision,DisbandedAtUtc)
        VALUES(@alliance,@name,@tag,N'',N'fr-CA',N'',
               N'Open',N'Active',@now,@leader,@leader,
               1,50,@slug,NULL,1,NULL);
        INSERT dbo.AllianceMemberships
            (AllianceId,PlayerId,Role,JoinedAtUtc,InvitedByPlayerId,
             ApplicationId,LastRoleChangedAtUtc,RemovedAtUtc,Revision)
        VALUES(@alliance,@leader,N'Leader',@now,NULL,NULL,@now,NULL,1);
        """;
    cmd.Parameters.AddWithValue("@alliance", id);
    cmd.Parameters.AddWithValue("@leader", outsider);
    cmd.Parameters.AddWithValue("@now", now.UtcDateTime);
    cmd.Parameters.AddWithValue("@name", "QA Alternate Alliance " + suffix);
    cmd.Parameters.AddWithValue("@tag", "ALT" + suffix[..5]);
    cmd.Parameters.AddWithValue("@slug", "qa-alternate-" + suffix);
    await cmd.ExecuteNonQueryAsync();
}

static async Task SwitchMembership(string connection,
    Guid previous, Guid next, Guid member, DateTimeOffset now)
{
    await using var conn = new SqlConnection(connection);
    await conn.OpenAsync();
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = """
        UPDATE dbo.AllianceMemberships
        SET RemovedAtUtc=@now,Revision=Revision+1
        WHERE AllianceId=@previous AND PlayerId=@player
            AND RemovedAtUtc IS NULL;
        IF @@ROWCOUNT<>1 THROW 51092,'Original membership missing.',1;
        INSERT dbo.AllianceMemberships
            (AllianceId,PlayerId,Role,JoinedAtUtc,InvitedByPlayerId,
             ApplicationId,LastRoleChangedAtUtc,RemovedAtUtc,Revision)
        VALUES(@next,@player,N'Member',@now,NULL,NULL,@now,NULL,1);
        """;
    cmd.Parameters.AddWithValue("@previous", previous);
    cmd.Parameters.AddWithValue("@next", next);
    cmd.Parameters.AddWithValue("@player", member);
    cmd.Parameters.AddWithValue("@now", now.UtcDateTime);
    await cmd.ExecuteNonQueryAsync();
}
static async Task InjectDuplicateProjectDelivery(string connection,
    Guid mission, Guid projectId, Guid player, DateTimeOffset now)
{
    await using var conn = new SqlConnection(connection);
    await conn.OpenAsync();
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = """
        INSERT dbo.FloralAllianceProjectDeliveries
            (MissionId,ProjectId,PlayerId,Points,DeliveredAtUtc)
        VALUES(@mission,@project,@player,1,@at);
        """;
    cmd.Parameters.AddWithValue("@mission", mission);
    cmd.Parameters.AddWithValue("@project", projectId);
    cmd.Parameters.AddWithValue("@player", player);
    cmd.Parameters.AddWithValue("@at", now.UtcDateTime);
    await cmd.ExecuteNonQueryAsync();
}
static async Task RemoveDuplicateProjectDelivery(string connection, Guid mission)
{
    await using var conn = new SqlConnection(connection);
    await conn.OpenAsync();
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = "DELETE dbo.FloralAllianceProjectDeliveries WHERE MissionId=@mission";
    cmd.Parameters.AddWithValue("@mission", mission);
    Check(await cmd.ExecuteNonQueryAsync() == 1, "injected ledger conflict removed");
}
static async Task<int> CountProjectDeliveries(string connection, Guid projectId)
{
    await using var conn = new SqlConnection(connection);
    await conn.OpenAsync();
    await using var cmd = conn.CreateCommand();
    cmd.CommandText =
        "SELECT COUNT(*) FROM dbo.FloralAllianceProjectDeliveries WHERE ProjectId=@project";
    cmd.Parameters.AddWithValue("@project", projectId);
    return Convert.ToInt32(await cmd.ExecuteScalarAsync());
}
static async Task Cleanup(string connection, Guid alliance, Guid otherAlliance,
    string world, Guid leader, Guid member)
{
    await using var conn = new SqlConnection(connection);
    await conn.OpenAsync();
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = """
        DELETE FROM dbo.FloralAllianceProjectDeliveries
          WHERE ProjectId IN (SELECT ProjectId FROM dbo.FloralAllianceProjects
              WHERE AllianceId IN (@alliance,@other));
        DELETE FROM dbo.FloralAllianceProjectMembers
          WHERE ProjectId IN (SELECT ProjectId FROM dbo.FloralAllianceProjects
              WHERE AllianceId IN (@alliance,@other));
        DELETE FROM dbo.FloralAllianceProjects WHERE AllianceId IN (@alliance,@other);
        DELETE FROM dbo.FloralAllianceProgression WHERE AllianceId IN (@alliance,@other);
        DELETE FROM dbo.FloralCivilActiveMissions WHERE WorldId=@world;
        DELETE FROM dbo.FloralCivilContributions WHERE WorldId=@world;
        DELETE FROM dbo.FloralSiteStates WHERE WorldId=@world;
        DELETE FROM dbo.HivePlayerStates WHERE PlayerId IN (@leader,@member);
        DELETE FROM dbo.AllianceMemberships WHERE AllianceId IN (@alliance,@other);
        DELETE FROM dbo.Alliances WHERE AllianceId IN (@alliance,@other);
        """;
    cmd.Parameters.AddWithValue("@alliance", alliance);
    cmd.Parameters.AddWithValue("@other", otherAlliance);
    cmd.Parameters.AddWithValue("@world", world);
    cmd.Parameters.AddWithValue("@leader", leader);
    cmd.Parameters.AddWithValue("@member", member);
    await cmd.ExecuteNonQueryAsync();
    Console.WriteLine("P4_SQL_QA_CLEANUP=COMPLETE");
}

internal sealed class AdjustableClock : IServerClock
{
    public DateTimeOffset UtcNow {get;set;}
}
