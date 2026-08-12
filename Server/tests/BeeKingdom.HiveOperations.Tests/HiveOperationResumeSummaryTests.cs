using BeeKingdom.HiveOperations;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class HiveOperationResumeSummaryTests
{
    [Fact]
    public void SummarySeparatesActiveAndCompletedAndUsesAuthoritativeFields()
    {
        Guid player = Guid.NewGuid(), hive = Guid.NewGuid(), activeId = Guid.NewGuid(), doneId = Guid.NewGuid();
        DateTimeOffset start = DateTimeOffset.Parse("2026-07-22T12:00:00Z");
        PlayerHiveState state = new(player, hive, 6, 4, new(), new(), [
            new(activeId, "apiary", 0, 1, start, start.AddSeconds(16), HiveOperationStatus.Running, "honey", 2, null),
            new(doneId, "training", 0, 0, start.AddMinutes(-2), start.AddMinutes(-1), HiveOperationStatus.Collected, "pollen", 3, start.AddMinutes(-1))], new(),
            Research: new(new Dictionary<string, ResearchCompletion> { ["foraging_routes_i"] = new("foraging_routes_i", start, new ResearchEffects(200, 0, 0, 0, 0, 0)) }, null));
        HiveOperationResumeSummary summary = HiveOperationResumeSummaryFactory.FromAuthoritativeState(state);
        Assert.Equal(4, summary.Revision); Assert.Single(summary.Active); Assert.Equal(activeId, summary.Active[0].OperationId);
        Assert.Equal(2, summary.Completed.Count); Assert.Contains(summary.Completed, x => x.OperationId == doneId && x.ResultResourceKey == "pollen");
        Assert.Contains(summary.Completed, x => x.DestinationId == "foraging_routes_i" && x.Kind == "Research");
    }
}
