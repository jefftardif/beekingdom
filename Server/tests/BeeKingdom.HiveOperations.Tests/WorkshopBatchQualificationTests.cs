using BeeKingdom.HiveOperations;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class WorkshopBatchQualificationTests
{
    [Fact]
    public async Task Wrong_answer_is_pedagogical_and_correct_answer_advances_once_with_replay()
    {
        string root = Path.Combine(Path.GetTempPath(), "bee-workshop-" + Guid.NewGuid().ToString("N"));
        Guid player = Guid.NewGuid(), hive = Guid.NewGuid();
        var repository = new DurableJsonHiveStateRepository(root, (p, h) => new PlayerHiveState(p, h, HiveStateMigrator.CurrentModelVersion, 0, new(), new(), [], new(), WorkshopBatchQualification: new("production", 160, "chapter4.upgrade_batch_qualification", 0)));
        var service = new HiveOperationService(repository, new FixedClock(), []);
        WorkshopBatchQualificationResult wrong = await service.QualifyWorkshopBatchAsync(new(player, hive, 0, "load", "k1"));
        Assert.False(wrong.Succeeded); Assert.Equal("tutorial_answer_incorrect", wrong.Code); Assert.Equal(0, wrong.RevisionAfter);
        WorkshopBatchQualificationResult right = await service.QualifyWorkshopBatchAsync(new(player, hive, 0, "heat", "k2"));
        Assert.True(right.Succeeded); Assert.Equal("chapter4.upgrade_application_ready", right.ResultingStep); Assert.Equal(1, right.RevisionAfter);
        WorkshopBatchQualificationResult replay = await service.QualifyWorkshopBatchAsync(new(player, hive, 0, "heat", "k2"));
        Assert.Equal(right.Code, replay.Code); Assert.Equal(right.RevisionAfter, replay.RevisionAfter); Assert.Equal(right.AcceptedAtUtc, replay.AcceptedAtUtc);
        WorkshopBatchQualificationResult conflict = await service.QualifyWorkshopBatchAsync(new(player, hive, 0, "load", "k2"));
        Assert.Equal("idempotency_conflict", conflict.Code);
    }

    [Fact]
    public async Task Storage_branch_rejects_heat_then_accepts_load_and_rejects_late_new_key()
    {
        (HiveOperationService service, Guid player, Guid hive) = Create("storage", 120);
        WorkshopBatchQualificationResult wrong = await service.QualifyWorkshopBatchAsync(new(player, hive, 0, "heat", "s1"));
        Assert.Equal("tutorial_answer_incorrect", wrong.Code);
        WorkshopBatchQualificationResult right = await service.QualifyWorkshopBatchAsync(new(player, hive, 0, "load", "s2"));
        Assert.Equal("tutorial_advanced", right.Code);
        WorkshopBatchQualificationResult late = await service.QualifyWorkshopBatchAsync(new(player, hive, 1, "load", "s3"));
        Assert.Equal("tutorial_precondition_failed", late.Code);
    }

    [Fact]
    public async Task Stale_revision_and_invalid_state_do_not_advance()
    {
        (HiveOperationService service, Guid player, Guid hive) = Create("production", 160);
        WorkshopBatchQualificationResult stale = await service.QualifyWorkshopBatchAsync(new(player, hive, 4, "heat", "stale"));
        Assert.Equal("revision_conflict", stale.Code);
        WorkshopBatchQualificationResult jump = await service.QualifyWorkshopBatchAsync(new(player, hive, 0, "heat", "jump"));
        Assert.Equal("tutorial_advanced", jump.Code);
        (HiveOperationService invalidService, Guid invalidPlayer, Guid invalidHive) = Create("unknown", 160);
        WorkshopBatchQualificationResult invalid = await invalidService.QualifyWorkshopBatchAsync(new(invalidPlayer, invalidHive, 0, "heat", "invalid"));
        Assert.Equal("tutorial_precondition_failed", invalid.Code);
    }

    [Fact]
    public async Task Same_error_key_replays_same_code_and_conflicts_on_changed_revision()
    {
        (HiveOperationService service, Guid player, Guid hive) = Create("production", 120);
        WorkshopBatchQualificationResult first = await service.QualifyWorkshopBatchAsync(new(player, hive, 0, "load", "e1"));
        WorkshopBatchQualificationResult replay = await service.QualifyWorkshopBatchAsync(new(player, hive, 0, "load", "e1"));
        Assert.Equal(first.Code, replay.Code); Assert.Equal(first.RevisionAfter, replay.RevisionAfter); Assert.Equal(first.AcceptedAtUtc, replay.AcceptedAtUtc);
        WorkshopBatchQualificationResult changed = await service.QualifyWorkshopBatchAsync(new(player, hive, 1, "load", "e1"));
        Assert.Equal("idempotency_conflict", changed.Code);
    }

    private static (HiveOperationService Service, Guid Player, Guid Hive) Create(string specialization, long amount)
    {
        string root = Path.Combine(Path.GetTempPath(), "bee-workshop-" + Guid.NewGuid().ToString("N"));
        Guid player = Guid.NewGuid(), hive = Guid.NewGuid();
        var repository = new DurableJsonHiveStateRepository(root, (p, h) => new PlayerHiveState(p, h, HiveStateMigrator.CurrentModelVersion, 0,
            new Dictionary<string, ResourceBalance> { ["honey"] = new(17, 100), ["wax"] = new(9, 100) }, new(), [], new(),
            WorkshopBatchQualification: new(specialization, amount, HiveOperationService.WorkshopQualificationStep, 0)));
        return (new HiveOperationService(repository, new FixedClock(), []), player, hive);
    }

    private sealed class FixedClock : IServerClock { public DateTimeOffset UtcNow => new(2026, 7, 21, 12, 0, 0, TimeSpan.Zero); }
}
