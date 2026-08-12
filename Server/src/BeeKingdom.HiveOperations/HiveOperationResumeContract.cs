namespace BeeKingdom.HiveOperations;

public sealed record HiveOperationResumeSummary(
    Guid PlayerId, Guid HiveId, long Revision,
    IReadOnlyList<HiveOperationResumeEntry> Active,
    IReadOnlyList<HiveOperationResumeEntry> Completed);

public sealed record HiveOperationResumeEntry(
    Guid OperationId, string Kind, string DestinationId,
    string Status, DateTimeOffset StartedAtUtc, DateTimeOffset CompletesAtUtc,
    string? ResultResourceKey, long ResultAmount);

public static class HiveOperationResumeSummaryFactory
{
    public static HiveOperationResumeSummary FromAuthoritativeState(PlayerHiveState state)
    {
        HiveOperationResumeEntry Map(HiveOperation operation) => new(
            operation.OperationId, operation.Kind.ToString(), operation.BuildingKey,
            operation.Status.ToString(), operation.StartedAtUtc, operation.CompletesAtUtc,
            operation.ProducedResourceKey, operation.ProducedAmount);
        List<HiveOperationResumeEntry> active = state.Operations.Where(x => x.Status != HiveOperationStatus.Collected).Select(Map).ToList();
        List<HiveOperationResumeEntry> completed = state.Operations.Where(x => x.Status == HiveOperationStatus.Collected).Select(Map).ToList();
        if (state.Research?.ActiveOperation is ResearchOperation research)
            active.Add(new(research.OperationId, "Research", research.ResearchId, "Running", research.StartedAtUtc, research.EndsAtUtc, null, 0));
        if (state.Research?.Completed is { } completedResearch)
            completed.AddRange(completedResearch.Values.Select(x => new HiveOperationResumeEntry(Guid.Empty, "Research", x.ResearchId, "Completed", x.CompletedAtUtc, x.CompletedAtUtc, null, 0)));
        return new(state.PlayerId, state.HiveId, state.Revision, active, completed);
    }
}

public sealed class HiveOperationResumeOptions
{
    public const string SectionName = "HiveOperationResume";
    public bool Enabled { get; set; }
}
