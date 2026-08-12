using BeeKingdom.HiveOperations;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class HivePerimeterSortieTests
{
    [Fact]
    public async Task Launch_replay_reconstruction_and_release_guard()
    {
        var root = Temp(); var p = Guid.NewGuid(); var h = Guid.NewGuid(); var clock = new MutableClock(new(2026, 7, 21, 7, 30, 0, TimeSpan.Zero));
        try
        {
            var repo = Repo(root, p, h); var service = new HivePerimeterSortieService(repo, clock); var snap = await service.ReadAsync(p, h, default); Assert.Equal(0, snap.Revision); Assert.Equal(clock.UtcNow, snap.ServerTimeUtc); Assert.Equal(TimeSpan.Zero, snap.ServerTimeUtc.Offset); var signal = snap.Signals.Single(x => x.SignalKey == "foraging_scout");
            var launched = await service.LaunchAsync(new(p, h, signal.SignalKey, signal.SignalInstanceId, snap.Reservation.ReservationId!, 0, "launch-1"), default); Assert.True(launched.Succeeded); Assert.Equal(1, launched.Snapshot.Revision); Assert.Equal(clock.UtcNow, launched.Snapshot.ServerTimeUtc);
            var replay = await new HivePerimeterSortieService(Repo(root, p, h), clock).LaunchAsync(new(p, h, signal.SignalKey, signal.SignalInstanceId, snap.Reservation.ReservationId!, 0, "launch-1"), default); Assert.True(replay.Succeeded, replay.Code); Assert.Equal(1, replay.Snapshot.Revision); Assert.Equal(launched.Snapshot.Active!.SortieId, replay.Snapshot.Active!.SortieId); Assert.Equal(1, replay.Snapshot.Revision);
            var conflict = await new HivePerimeterSortieService(Repo(root, p, h), clock).LaunchAsync(new(p, h, "brood_watch", signal.SignalInstanceId, snap.Reservation.ReservationId!, 0, "launch-1"), default); Assert.Equal("game.idempotency_conflict", conflict.Code);
            var release = await new CombatSquadReservationService(Repo(root, p, h)).ReleaseAsync(new(p, h, 1, "release"), default); Assert.Equal("game.squad_in_use", release.Code);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Claim_after_rollover_reconstructs_exactly_once_and_reopens_next_cycle()
    {
        var root = Temp(); var p = Guid.NewGuid(); var h = Guid.NewGuid(); var clock = new MutableClock(new(2026, 7, 21, 7, 59, 50, TimeSpan.Zero));
        try
        {
            var service = new HivePerimeterSortieService(Repo(root, p, h), clock); var before = await service.ReadAsync(p, h, default); var s = before.Signals.Single(x => x.SignalKey == "foraging_scout"); var launch = await service.LaunchAsync(new(p, h, s.SignalKey, s.SignalInstanceId, before.Reservation.ReservationId!, 0, "l"), default); Assert.True(launch.Succeeded); clock.Advance(TimeSpan.FromSeconds(20));
            var claimService = new HivePerimeterSortieService(Repo(root, p, h), clock); var claim = await claimService.ClaimAsync(new(p, h, launch.Snapshot.Active!.SortieId, 1, "c"), default); Assert.True(claim.Succeeded, claim.Code); Assert.Equal(2, claim.Snapshot.Revision); Assert.Equal(clock.UtcNow, claim.Snapshot.ServerTimeUtc); Assert.Equal(0, claim.Snapshot.Reservation.Reserved.Values.Sum()); Assert.Null(claim.Snapshot.Reservation.ReservationId); Assert.True(claim.Snapshot.Signals.Single(x => x.SignalKey == "foraging_scout").Completed);
            var replay = await new HivePerimeterSortieService(Repo(root, p, h), clock).ClaimAsync(new(p, h, launch.Snapshot.Active!.SortieId, 1, "c"), default); Assert.True(replay.Succeeded, replay.Code); Assert.Equal(2, replay.Snapshot.Revision); Assert.Equal(claim.Snapshot.Reservation.ReservationId, replay.Snapshot.Reservation.ReservationId); Assert.Equal(claim.Snapshot.Signals.Select(x => x.Completed), replay.Snapshot.Signals.Select(x => x.Completed));
            var next = await new HivePerimeterSortieService(Repo(root, p, h), clock).ReadAsync(p, h, default); Assert.NotEqual(s.SignalInstanceId, next.Signals.Single(x => x.SignalKey == "foraging_scout").SignalInstanceId);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Claim_receipt_reports_partial_credit_and_replays_stably_after_reconstruction()
    {
        var root = Temp(); var p = Guid.NewGuid(); var h = Guid.NewGuid(); var clock = new MutableClock(new(2026, 7, 21, 9, 0, 0, TimeSpan.Zero));
        try
        {
            var repo = new DurableJsonHiveStateRepository(root, (_, _) => new PlayerHiveState(p, h, HiveStateMigrator.CurrentModelVersion, 0, new Dictionary<string, ResourceBalance> { ["honey"] = new(120, 130), ["pollen"] = new(100, 1000) }, new(), [], new(), DoctrineRoster: new DoctrineRosterState(0, new() { ["guardians"] = 4, ["wingrunners"] = 6, ["darters"] = 4 }, null, new()), SquadReservation: new SquadReservationState(0, 12, new() { ["guardians"] = 3, ["wingrunners"] = 6, ["darters"] = 3 }, "reservation", new())));
            repo.ExecuteAtomicallyAsync(p, h, s => s).GetAwaiter().GetResult();
            var service = new HivePerimeterSortieService(repo, clock); var board = await service.ReadAsync(p, h, default); var signal = board.Signals.Single(x => x.SignalKey == "foraging_scout");
            var launch = await service.LaunchAsync(new(p, h, signal.SignalKey, signal.SignalInstanceId, board.Reservation.ReservationId!, 0, "launch"), default); clock.Advance(TimeSpan.FromSeconds(17));
            var claim = await new HivePerimeterSortieService(new DurableJsonHiveStateRepository(root, (_, _) => null), clock).ClaimAsync(new(p, h, launch.Snapshot.Active!.SortieId, 1, "claim"), default);
            Assert.True(claim.Succeeded); Assert.Equal(10, claim.ClaimReceipt!.CreditedByResource["honey"]); Assert.Equal(20, claim.ClaimReceipt.CreditedByResource["pollen"]); Assert.Equal(130, claim.ClaimReceipt.ResultingBalances["honey"].Amount); Assert.Equal(120, claim.ClaimReceipt.ResultingBalances["pollen"].Amount);
            var replay = await new HivePerimeterSortieService(new DurableJsonHiveStateRepository(root, (_, _) => null), clock).ClaimAsync(new(p, h, launch.Snapshot.Active.SortieId, 1, "claim"), default);
            Assert.True(replay.Succeeded); Assert.Equal(claim.ClaimReceipt!.PlayerId, replay.ClaimReceipt!.PlayerId); Assert.Equal(claim.ClaimReceipt.SortieId, replay.ClaimReceipt.SortieId); Assert.Equal(claim.ClaimReceipt.SignalInstanceId, replay.ClaimReceipt.SignalInstanceId); Assert.Equal(claim.ClaimReceipt.Revision, replay.ClaimReceipt.Revision); Assert.Equal(claim.ClaimReceipt.ServerTimeUtc, replay.ClaimReceipt.ServerTimeUtc); Assert.Equal(claim.ClaimReceipt.CreditedByResource["honey"], replay.ClaimReceipt.CreditedByResource["honey"]); Assert.Equal(claim.ClaimReceipt.CreditedByResource["pollen"], replay.ClaimReceipt.CreditedByResource["pollen"]); Assert.Equal(claim.Snapshot.ServerTimeUtc, replay.Snapshot.ServerTimeUtc);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Recall_releases_without_reward_or_completion_and_invalid_migration_is_rejected()
    {
        var root = Temp(); var p = Guid.NewGuid(); var h = Guid.NewGuid(); var clock = new MutableClock(new(2026, 7, 21, 9, 0, 0, TimeSpan.Zero));
        try
        {
            var service = new HivePerimeterSortieService(Repo(root, p, h), clock); var s = await service.ReadAsync(p, h, default); var sig = s.Signals[0]; var launch = await service.LaunchAsync(new(p, h, sig.SignalKey, sig.SignalInstanceId, s.Reservation.ReservationId!, 0, "l"), default); var recall = await new HivePerimeterSortieService(Repo(root, p, h), clock).RecallAsync(new(p, h, launch.Snapshot.Active!.SortieId, 1, "r"), default); Assert.True(recall.Succeeded); Assert.False(recall.Snapshot.Signals.Any(x => x.Completed)); Assert.Equal(0, recall.Snapshot.Reservation.Reserved.Values.Sum());
            var bad = new PlayerHiveState(p, h, HiveStateMigrator.CurrentModelVersion, 0, new(), new(), [], new(), HivePerimeterSortie: new HivePerimeterSortieState(0, clock.UtcNow.AddTicks(1), clock.UtcNow.AddHours(8), null, new(), new HashSet<string>())); Assert.Throws<InvalidDataException>(() => HiveStateMigrator.ToCurrent(bad));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Capacity_failure_is_atomic_and_isolation_is_scoped()
    {
        var root = Temp(); var p = Guid.NewGuid(); var h = Guid.NewGuid(); var clock = new MutableClock(new(2026, 7, 21, 9, 0, 0, TimeSpan.Zero));
        try
        {
            var repo = new DurableJsonHiveStateRepository(root, (_, _) => new PlayerHiveState(p, h, HiveStateMigrator.CurrentModelVersion, 0, new Dictionary<string, ResourceBalance> { ["honey"] = new(0, 0), ["pollen"] = new(0, 0) }, new(), [], new(), DoctrineRoster: new DoctrineRosterState(0, new() { ["guardians"] = 4, ["wingrunners"] = 6, ["darters"] = 4 }, null, new()), SquadReservation: new SquadReservationState(0, 12, new() { ["guardians"] = 3, ["wingrunners"] = 6, ["darters"] = 3 }, "reservation", new()))); repo.ExecuteAtomicallyAsync(p, h, s => s).GetAwaiter().GetResult(); var service = new HivePerimeterSortieService(repo, clock); var s = await service.ReadAsync(p, h, default); var l = await service.LaunchAsync(new(p, h, s.Signals[0].SignalKey, s.Signals[0].SignalInstanceId, "reservation", 0, "l"), default); clock.Advance(TimeSpan.FromMinutes(1)); var c = await service.ClaimAsync(new(p, h, l.Snapshot.Active!.SortieId, 1, "c"), default); Assert.True(c.Succeeded); Assert.Equal("game.perimeter_claimed", c.Code); Assert.Equal(0, c.ClaimReceipt!.CreditedByResource["honey"]); Assert.Equal(0, c.ClaimReceipt.CreditedByResource["pollen"]); Assert.Equal(0, c.Snapshot.Reservation.Reserved["guardians"]); Assert.True(c.Snapshot.Signals.Single(x => x.SignalKey == "foraging_scout").Completed); await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ReadAsync(Guid.NewGuid(), h, default));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact] public void Catalog_and_bounds_are_strict() { Assert.Equal(2, HivePerimeterSortieService.Catalog.Count); Assert.All(HivePerimeterSortieService.Catalog.Values, x => Assert.Contains(x.HazardDoctrine, new[] { "guardians", "wingrunners", "darters" })); }

    private static string Temp() => Path.Combine(Path.GetTempPath(), "bee-perimeter-" + Guid.NewGuid().ToString("N"));
    private static DurableJsonHiveStateRepository Repo(string root, Guid p, Guid h) { var r = new DurableJsonHiveStateRepository(root, (_, _) => new PlayerHiveState(p, h, HiveStateMigrator.CurrentModelVersion, 0, new Dictionary<string, ResourceBalance> { ["honey"] = new(100, 1000), ["pollen"] = new(100, 1000) }, new(), [], new(), DoctrineRoster: new DoctrineRosterState(0, new() { ["guardians"] = 4, ["wingrunners"] = 6, ["darters"] = 4 }, null, new()), SquadReservation: new SquadReservationState(0, 12, new() { ["guardians"] = 3, ["wingrunners"] = 6, ["darters"] = 3 }, "reservation", new()))); r.ExecuteAtomicallyAsync(p, h, s => s).GetAwaiter().GetResult(); return r; }
    private sealed class MutableClock(DateTimeOffset now) : IServerClock { public DateTimeOffset UtcNow { get; private set; } = now; public void Advance(TimeSpan value) => UtcNow += value; }
}







