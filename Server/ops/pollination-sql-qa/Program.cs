using BeeKingdom.HiveOperations;
using Microsoft.Data.SqlClient;
using System.Data;

string connection = Environment.GetEnvironmentVariable("BEEKINGDOM_POLLINATION_QA_SQL")
    ?? @"Server=(localdb)\BKPollinationQA;Database=BeeKingdomPollinationQA;Integrated Security=True;Encrypt=False;Connect Timeout=15";
Guid playerA = Guid.NewGuid(), playerB = Guid.NewGuid();
Guid hiveA = Guid.NewGuid(), hiveB = Guid.NewGuid();
string suffix = Guid.NewGuid().ToString("N")[..12];
string worldId = "world-qa-" + suffix;
string siteId = "clover-qa-" + suffix;
var origin = new DateTimeOffset(2026, 10, 21, 19, 0, 0, TimeSpan.Zero);
var clock = new TestClock { UtcNow = origin };
var floral = new FloralSiteOptions
{
    Enabled = true, CatalogVersion = "sql-qa-v1",
    Sites = [new(worldId, "region-qa", siteId, "clover",
        "Trèfle partagé", "Shared clover", 15, 6000, 5500)]
};
var rules = new CivilExpeditionOptions
{
    Enabled = true, DurationMinutes = 15, MaxWorkers = 100,
    PollenPerWorker = 1, FertilityGainBp = 5, BiodiversityGainBp = 2
};

var hives = new SqlHiveStateRepository(connection, NewHive);
var sites = new SqlFloralSiteRepository(connection);
var coordinator = new SqlCivilExpeditionUnitOfWork(connection);
var service = new CivilExpeditionService(hives, coordinator, clock, rules, floral);

try
{
    await hives.ExecuteAtomicallyAsync(playerA, hiveA, s => s);
    await hives.ExecuteAtomicallyAsync(playerB, hiveB, s => s);
    FloralSiteState before = await sites.ReadOrCreateAsync(
        floral.Sites[0], floral.CatalogVersion, clock.UtcNow);
    Check(before.PollenRemaining == 15, "initial site inventory");

    var startA = new StartCivilExpeditionRequest(
        siteId, 12, 0, 0, "sql-alpha-flight-01");
    var startB = new StartCivilExpeditionRequest(
        siteId, 12, 0, 0, "sql-bravo-flight-01");
    CivilExpeditionResult first = await service.StartAsync(playerA, hiveA, startA);
    CivilExpeditionResult replay = await service.StartAsync(playerA, hiveA, startA);
    CivilExpeditionResult second = await service.StartAsync(playerB, hiveB, startB);
    Check(first.Succeeded && second.Succeeded && replay.Succeeded,
        "both launches and first exact replay");
    Check(first.Receipt == replay.Receipt, "immutable idempotent replay receipt");
    Check((await coordinator.DueAsync(clock.UtcNow, 10)).Count == 0,
        "not due before UTC deadline");

    // Simulate a stale/duplicate contribution row to force SQL PK error.
    // The attempted settlement MUST roll back all hive+site writes.
    clock.UtcNow = origin.AddMinutes(17);
    Guid missionA = first.Receipt!.MissionId;
    await InsertConflictingLedgerRow(connection, missionA, playerA, hiveA,
        worldId, siteId, clock.UtcNow);
    bool conflict = false;
    try
    {
        await service.ReadAsync(playerA, hiveA);
    }
    catch (SqlException e) when (e.Number is 2627 or 2601)
    {
        conflict = true;
    }
    Check(conflict, "forced SQL unique-key rollback occurred");
    FloralSiteState rollbackSite = await sites.ReadOrCreateAsync(
        floral.Sites[0], floral.CatalogVersion, clock.UtcNow);
    PlayerHiveState rollbackHive = (await hives.ReadAsync(playerA, hiveA))!;
    Check(rollbackSite.PollenRemaining == 15
        && rollbackSite.TotalPollinationPoints == 0
        && rollbackHive.Resources["pollen"].Amount == 0
        && rollbackHive.CivilPopulation!.AssignedWorkers == 12
        && rollbackHive.CivilExpeditions!.Active != null,
        "atomic rollback retained site, hive resources and civilian flight");
    Check((await coordinator.DueAsync(clock.UtcNow, 10)).Count == 2,
        "durable due rows survive aborted settlement");
    await RemoveConflictingLedgerRow(connection, missionA);

    // Real overlapping SQL transactions across TWO different hives against ONE site.
    CivilExpeditionSnapshot[] finishes = await Task.WhenAll(
        service.ReadAsync(playerA, hiveA),
        service.ReadAsync(playerB, hiveB));
    Check(finishes.All(x => x.Active is null && x.AssignedWorkers == 0),
        "both flight outcomes completed and all civilians released");

    FloralSiteState finalSite = await sites.ReadOrCreateAsync(
        floral.Sites[0], floral.CatalogVersion, clock.UtcNow);
    PlayerHiveState finalA = (await hives.ReadAsync(playerA, hiveA))!;
    PlayerHiveState finalB = (await hives.ReadAsync(playerB, hiveB))!;
    Check(finalSite.PollenRemaining == 0 && finalSite.TotalPollinationPoints == 15,
        "no pollen minting nor negative world balance");
    Check(finalSite.ContributionPointsByPlayer?.Count == 2,
        "distinct two-player pollination contributions");
    Check(finalSite.FertilityBp == 6010
        && finalSite.BiodiversityBp == 5504, "ecological changes applied once per mission");
    Check(finalA.Resources["pollen"].Amount + finalB.Resources["pollen"].Amount == 15,
        "world debit equals total hive credit");
    Check(finalA.CivilPopulation!.AssignedWorkers == 0
        && finalB.CivilPopulation!.AssignedWorkers == 0, "civilian reservations conserved");
    Check(await ReadContributionCount(connection, worldId) == (2L, 15L),
        "real immutable SQL contribution ledger contains exactly two rows");
    Check((await coordinator.DueAsync(clock.UtcNow, 100)).Count == 0,
        "real SQL due-work index emptied atomically");

    // Reopen the services after they would have lost their in-memory state.
    var reopenedHives = new SqlHiveStateRepository(connection, NewHive);
    var reopenedCoordinator = new SqlCivilExpeditionUnitOfWork(connection);
    var reopenedService = new CivilExpeditionService(
        reopenedHives, reopenedCoordinator, clock, rules, floral);
    await Task.WhenAll(
        reopenedService.ReadAsync(playerA, hiveA),
        reopenedService.ReadAsync(playerB, hiveB));
    Check(await ReadContributionCount(connection, worldId) == (2L, 15L),
        "reopening and repeat GET cannot double-award");
    // A second field proves the indexed server due-sweeper settles a flight
    // without a single player GET after the deadline.
    string dueSiteId = "clover-due-" + suffix;
    var dueFlora = new FloralSiteOptions
    {
        Enabled = true, CatalogVersion = "sql-qa-v1",
        Sites = [new(worldId, "region-qa", dueSiteId, "clover",
            "Second champ", "Second field", 20, 6000, 5500)]
    };
    await sites.ReadOrCreateAsync(dueFlora.Sites[0], dueFlora.CatalogVersion, clock.UtcNow);
    var dueService = new CivilExpeditionService(
        hives, coordinator, clock, rules, dueFlora);
    CivilExpeditionResult dueStart = await dueService.StartAsync(
        playerA, hiveA, new StartCivilExpeditionRequest(
            dueSiteId, 10, finalA.Revision, 0, "sql-offline-auto-due"));
    Check(dueStart.Succeeded, "second site launched for offline settlement");
    clock.UtcNow = origin.AddHours(5);
    CivilExpeditionDueBatch dueBatch = await new CivilExpeditionDueProcessor(
        coordinator, dueService, clock).ProcessAsync(100);
    Check(dueBatch.Inspected == 1 && dueBatch.Settled == 1
        && dueBatch.RetryMissionIds.Count == 0,
        "indexed SQL server due processor settled without player GET");
    Check((await sites.ReadOrCreateAsync(
        dueFlora.Sites[0], dueFlora.CatalogVersion, clock.UtcNow)).PollenRemaining == 10,
        "second field debited by the server processor");
    Check((await hives.ReadAsync(playerA, hiveA))!.Resources["pollen"].Amount
        == finalA.Resources["pollen"].Amount + 10,
        "offline server due processor credited the hive exactly once");
    Check((await coordinator.DueAsync(clock.UtcNow, 100)).Count == 0,
        "all indexed flights cleared after server processing");
    Console.WriteLine("SQL_QA_PASS: two hives + one shared floral site + "
        + "forced SQL rollback + replay + concurrent completion + due-index cleanup");
    Console.WriteLine($"SQL_QA_SCENARIOS=2 WORLD={worldId} "
        + "STOCK_SITE1=0 POINTS_SITE1=15 UNIQUE_PLAYERS_SITE1=2 LEDGER_TOTAL=3");
}
finally
{
    await Cleanup(connection, worldId, playerA, hiveA, playerB, hiveB);
}

static PlayerHiveState NewHive(Guid player, Guid hive) =>
    new(player, hive, HiveStateMigrator.CurrentModelVersion, 0,
        new Dictionary<string, ResourceBalance>
        {
            ["pollen"] = new(0, 100),
            ["honey"] = new(200, 1000),
            ["wax"] = new(50, 100)
        },
        new Dictionary<string, int> { ["nursery_cluster"] = 1 },
        [], new Dictionary<string, IdempotencyReceipt>(),
        CivilPopulation: new CivilPopulationState(0, 40, 0, 0, null, new()));

static void Check(bool passed, string message)
{
    if (!passed) throw new InvalidOperationException("SQL_QA_FAIL: " + message);
    Console.WriteLine("PASS " + message);
}

static async Task InsertConflictingLedgerRow(string conn, Guid mission,
    Guid player, Guid hive, string world, string site, DateTimeOffset at)
{
    await using var sql = new SqlConnection(conn);
    await sql.OpenAsync();
    await using var cmd = sql.CreateCommand();
    cmd.CommandText =
        "INSERT dbo.FloralCivilContributions "
        + "(MissionId,PlayerId,HiveId,WorldId,SiteId,"
        + "PollenRemoved,PollenCredited,PollinationPoints,CompletedAtUtc) "
        + "VALUES(@id,@player,@hive,@world,@site,1,1,1,@at)";
    cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.UniqueIdentifier){Value=mission});
    cmd.Parameters.Add(new SqlParameter("@player", SqlDbType.UniqueIdentifier){Value=player});
    cmd.Parameters.Add(new SqlParameter("@hive", SqlDbType.UniqueIdentifier){Value=hive});
    cmd.Parameters.Add(new SqlParameter("@world", SqlDbType.NVarChar, 64){Value=world});
    cmd.Parameters.Add(new SqlParameter("@site", SqlDbType.NVarChar, 64){Value=site});
    cmd.Parameters.Add(new SqlParameter("@at", SqlDbType.DateTime2){Value=at.UtcDateTime});
    await cmd.ExecuteNonQueryAsync();
}

static async Task RemoveConflictingLedgerRow(string conn, Guid mission)
{
    await using var sql = new SqlConnection(conn);
    await sql.OpenAsync();
    await using var cmd = sql.CreateCommand();
    cmd.CommandText = "DELETE dbo.FloralCivilContributions WHERE MissionId=@mission";
    cmd.Parameters.Add(new SqlParameter("@mission", SqlDbType.UniqueIdentifier)
    {Value=mission});
    Check(await cmd.ExecuteNonQueryAsync() == 1, "recovered injected conflict");
}

static async Task<(long Rows,long Total)> ReadContributionCount(
    string conn, string world)
{
    await using var sql = new SqlConnection(conn);
    await sql.OpenAsync();
    await using var cmd = sql.CreateCommand();
    cmd.CommandText =
        "SELECT COUNT_BIG(*),COALESCE(SUM(PollenCredited),0) "
        + "FROM dbo.FloralCivilContributions WHERE WorldId=@world";
    cmd.Parameters.Add(new SqlParameter("@world", SqlDbType.NVarChar,64){Value=world});
    await using var reader = await cmd.ExecuteReaderAsync();
    await reader.ReadAsync();
    return (reader.GetInt64(0),reader.GetInt64(1));
}

static async Task Cleanup(string conn, string world, Guid a, Guid ah,
    Guid b, Guid bh)
{
    await using var sql = new SqlConnection(conn);
    await sql.OpenAsync();
    await using var cmd = sql.CreateCommand();
    cmd.CommandText =
        "DELETE dbo.FloralCivilActiveMissions WHERE WorldId=@world; "
        + "DELETE dbo.FloralCivilContributions WHERE WorldId=@world; "
        + "DELETE dbo.FloralSiteStates WHERE WorldId=@world; "
        + "DELETE dbo.HivePlayerStates WHERE (PlayerId=@a AND HiveId=@ah) "
        + "OR (PlayerId=@b AND HiveId=@bh);";
    cmd.Parameters.Add(new SqlParameter("@world",SqlDbType.NVarChar,64){Value=world});
    cmd.Parameters.Add(new SqlParameter("@a",SqlDbType.UniqueIdentifier){Value=a});
    cmd.Parameters.Add(new SqlParameter("@ah",SqlDbType.UniqueIdentifier){Value=ah});
    cmd.Parameters.Add(new SqlParameter("@b",SqlDbType.UniqueIdentifier){Value=b});
    cmd.Parameters.Add(new SqlParameter("@bh",SqlDbType.UniqueIdentifier){Value=bh});
    await cmd.ExecuteNonQueryAsync();
    Console.WriteLine("SQL_QA_CLEANUP=COMPLETE");
}

internal sealed class TestClock : IServerClock
{
    public DateTimeOffset UtcNow { get; set; }
}
