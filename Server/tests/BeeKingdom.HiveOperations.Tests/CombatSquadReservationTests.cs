using BeeKingdom.HiveOperations;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class CombatSquadReservationTests
{
    [Fact]
    public async Task Commit_and_release_are_idempotent_and_do_not_consume_roster()
    {
        var root = Path.Combine(Path.GetTempPath(), "squad-" + Guid.NewGuid()); Directory.CreateDirectory(root);
        var player = Guid.NewGuid(); var hive = Guid.NewGuid();
        var roster = new DoctrineRosterState(0, new() { ["guardians"] = 4, ["wingrunners"] = 3, ["darters"] = 0 }, null, new());
        var repo = new DurableJsonHiveStateRepository(root, (_, _) => new PlayerHiveState(player, hive, HiveStateMigrator.CurrentModelVersion, 0, new(), new(), [], new(), DoctrineRoster: roster));
        var service = new CombatSquadReservationService(repo);
        var q = new Dictionary<string,long> { ["guardians"] = 2, ["wingrunners"] = 1, ["darters"] = 0 };
        var first = await service.CommitAsync(new(player, hive, 0, q, "commit-1"), default);
        Assert.True(first.Succeeded); Assert.NotNull(first.Receipt); Assert.Equal(q, first.Receipt!.Quantities); Assert.Equal(0, first.Receipt.ReservationRevisionBefore); Assert.Equal(1, first.Receipt.ReservationRevisionAfter); Assert.Equal(3, first.Snapshot.Reserved.Values.Sum());
        var secondCommit = await service.CommitAsync(new(player, hive, 1, q, "commit-2"), default);
        Assert.Equal("game.revision_conflict", secondCommit.Code);
        var replay = await service.CommitAsync(new(player, hive, 0, q, "commit-1"), default);
        Assert.True(replay.Succeeded); Assert.NotNull(replay.Receipt); Assert.Equal(first.Receipt!.ReservationId, replay.Receipt!.ReservationId); Assert.Equal(first.Receipt.ReservationRevisionAfter, replay.Receipt.ReservationRevisionAfter); Assert.Equal(first.Receipt.Quantities, replay.Receipt.Quantities); Assert.Equal(first.Snapshot.ReservationId, replay.Snapshot.ReservationId);
        var conflict = await service.CommitAsync(new(player, hive, 0, new() { ["guardians"] = 1, ["wingrunners"] = 1, ["darters"] = 0 }, "commit-1"), default);
        Assert.Equal("game.idempotency_conflict", conflict.Code);
        var reconstructed = new CombatSquadReservationService(new DurableJsonHiveStateRepository(root, (_, _) => new PlayerHiveState(player, hive, HiveStateMigrator.CurrentModelVersion, 0, new(), new(), [], new(), DoctrineRoster: roster)));
        var persisted = await reconstructed.ReadAsync(player, hive, default);
        Assert.Equal(first.Snapshot.ReservationId, persisted.ReservationId);
        Assert.Equal(q, persisted.Reserved);
        var released = await reconstructed.ReleaseAsync(new(player, hive, 1, "release-1"), default);
        Assert.True(released.Succeeded); Assert.Equal(new Dictionary<string,long> { ["guardians"] = 0, ["wingrunners"] = 0, ["darters"] = 0 }, released.Snapshot.Reserved);
        var afterRelease = new CombatSquadReservationService(new DurableJsonHiveStateRepository(root, (_, _) => new PlayerHiveState(player, hive, HiveStateMigrator.CurrentModelVersion, 0, new(), new(), [], new(), DoctrineRoster: roster)));
        var reread = await afterRelease.ReadAsync(player, hive, default);
        Assert.Null(reread.ReservationId); Assert.Equal(0, reread.Reserved.Values.Sum()); Assert.Equal(roster.Counts, reread.Roster);
        var releaseReplay = await afterRelease.ReleaseAsync(new(player, hive, 1, "release-1"), default);
        Assert.True(releaseReplay.Succeeded); Assert.NotNull(releaseReplay.Receipt); Assert.Equal(released.Receipt!.ReservationRevisionAfter, releaseReplay.Receipt!.ReservationRevisionAfter); Assert.Equal(released.Receipt.AcceptedAtUtc, releaseReplay.Receipt.AcceptedAtUtc); Assert.Equal(reread.Reserved, releaseReplay.Snapshot.Reserved);
        var commitReplayAfterRelease = await afterRelease.CommitAsync(new(player, hive, 0, q, "commit-1"), default);
        Assert.True(commitReplayAfterRelease.Succeeded); Assert.NotNull(commitReplayAfterRelease.Receipt); Assert.Equal(first.Receipt!.ReservationId, commitReplayAfterRelease.Receipt!.ReservationId); Assert.Equal(first.Receipt.Quantities, commitReplayAfterRelease.Receipt.Quantities); Assert.Equal(first.Receipt.ReservationRevisionBefore, commitReplayAfterRelease.Receipt.ReservationRevisionBefore); Assert.Equal(first.Receipt.ReservationRevisionAfter, commitReplayAfterRelease.Receipt.ReservationRevisionAfter); Assert.Equal(first.Receipt.AcceptedAtUtc, commitReplayAfterRelease.Receipt.AcceptedAtUtc); Assert.Equal(first.Receipt.Code, commitReplayAfterRelease.Receipt.Code); Assert.Null(commitReplayAfterRelease.Snapshot.ReservationId);
        var otherPlayer = Guid.NewGuid(); var otherHive = Guid.NewGuid();
        var other = new CombatSquadReservationService(new DurableJsonHiveStateRepository(root, (_, _) => new PlayerHiveState(otherPlayer, otherHive, HiveStateMigrator.CurrentModelVersion, 0, new(), new(), [], new(), DoctrineRoster: roster)));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => other.ReadAsync(otherPlayer, otherHive, default));
    }

    [Fact]
    public async Task Rejects_over_capacity_and_over_roster()
    {
        var root = Path.Combine(Path.GetTempPath(), "squad-" + Guid.NewGuid()); Directory.CreateDirectory(root);
        var p = Guid.NewGuid(); var h = Guid.NewGuid();
        var roster = new DoctrineRosterState(0, new() { ["guardians"] = 1, ["wingrunners"] = 0, ["darters"] = 0 }, null, new());
        var repo = new DurableJsonHiveStateRepository(root, (_, _) => new PlayerHiveState(p, h, HiveStateMigrator.CurrentModelVersion, 0, new(), new(), [], new(), DoctrineRoster: roster));
        var service = new CombatSquadReservationService(repo);
        var tooMany = await service.CommitAsync(new(p, h, 0, new() { ["guardians"] = 2, ["wingrunners"] = 0, ["darters"] = 0 }, "k"), default);
        Assert.Equal("game.squad_over_reserved", tooMany.Code);
        var overCapacity = await service.CommitAsync(new(p, h, 0, new() { ["guardians"] = 13, ["wingrunners"] = 0, ["darters"] = 0 }, "capacity"), default);
        Assert.Equal("game.invalid_request", overCapacity.Code);
    }

    [Fact]
    public async Task Capacity_grows_with_guard_post_level_and_defaults_to_twelve()
    {
        var root = Path.Combine(Path.GetTempPath(), "squad-" + Guid.NewGuid()); Directory.CreateDirectory(root);
        var p = Guid.NewGuid(); var h = Guid.NewGuid();
        var roster = new DoctrineRosterState(0, new() { ["guardians"] = 20, ["wingrunners"] = 0, ["darters"] = 0 }, null, new());
        var buildingLevels = new Dictionary<string, int> { ["guard_post"] = 2 };
        var repo = new DurableJsonHiveStateRepository(root, (_, _) => new PlayerHiveState(p, h, HiveStateMigrator.CurrentModelVersion, 0, new(), buildingLevels, [], new(), DoctrineRoster: roster));
        var service = new CombatSquadReservationService(repo);

        Assert.Equal(12 + 2 * CombatSquadReservationService.CapacityPerGuardPostLevel, CombatSquadReservationService.ComputeCapacity(buildingLevels));
        Assert.Equal(12, CombatSquadReservationService.ComputeCapacity(new Dictionary<string, int>()));

        var withinGrownCapacity = await service.CommitAsync(new(p, h, 0, new() { ["guardians"] = 20, ["wingrunners"] = 0, ["darters"] = 0 }, "k"), default);
        Assert.True(withinGrownCapacity.Succeeded, withinGrownCapacity.Code);
        Assert.Equal(20, withinGrownCapacity.Snapshot.Capacity);
    }

    [Fact]
    public void Migration_rejects_each_corrupt_reservation_shape()
    {
        var p = Guid.NewGuid(); var h = Guid.NewGuid();
        PlayerHiveState Base(SquadReservationState q) => new(p, h, HiveStateMigrator.CurrentModelVersion, 1, new(), new(), [], new(), DoctrineRoster: new DoctrineRosterState(0, new() { ["guardians"] = 1, ["wingrunners"] = 0, ["darters"] = 0 }, null, new()), SquadReservation: q);
        var keys = new Dictionary<string,long> { ["guardians"] = 0, ["wingrunners"] = 0, ["darters"] = 0 };
        Assert.Throws<InvalidDataException>(() => HiveStateMigrator.ToCurrent(Base(new(0, 12, new() { ["guardians"] = 1 }, null, new()))));
        Assert.Throws<InvalidDataException>(() => HiveStateMigrator.ToCurrent(Base(new(0, 12, keys, "id", new()))));
        Assert.Throws<InvalidDataException>(() => HiveStateMigrator.ToCurrent(Base(new(0, 12, new() { ["guardians"] = 2, ["wingrunners"] = 0, ["darters"] = 0 }, "id", new()))));
        Assert.Throws<InvalidDataException>(() => HiveStateMigrator.ToCurrent(Base(new(0, 12, new() { ["guardians"] = long.MaxValue, ["wingrunners"] = 0, ["darters"] = 0 }, "id", new()))));
    }
}
