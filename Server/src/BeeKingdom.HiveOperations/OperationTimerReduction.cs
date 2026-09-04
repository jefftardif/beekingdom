namespace BeeKingdom.HiveOperations;

// M045-CL: extracted from SpeedUpInventoryService's private per-category handlers (Construction/
// Research/Training/Healing) so a second caller - Alliance Help - can apply the exact same
// end-time math against the exact same PlayerHiveState fields, instead of re-deriving a second,
// possibly-diverging "end - duration" computation. SpeedUpInventoryService itself now delegates
// here (see SpeedUpContracts.cs) - this is the single source of truth for "reduce this operation's
// remaining time by X", used by both a solo gem-spend and a cooperative Alliance Help contribution.
public readonly record struct OperationTimerInfo(Guid OperationId, DateTimeOffset StartedAtUtc, DateTimeOffset EndsAtUtc, bool Completed);

public static class OperationTimerReduction
{
    // Read-only: locate the active operation for (category, targetId) without mutating anything.
    // Used by Alliance Help to validate eligibility (operation exists, still running, meets the
    // minimum-original-duration threshold) before ever creating a help request.
    public static bool TryPeek(PlayerHiveState state, string category, string targetId, DateTimeOffset now, out OperationTimerInfo info)
    {
        if (Handlers.TryGetValue(category, out ITimerHandler? handler) && handler.TryPeek(state, targetId, now, out info)) return true;
        info = default;
        return false;
    }

    // Mutating: same math SpeedUpInventoryService uses - subtracts `duration` from the operation's
    // end time (never before `now`), flips status to AwaitingCollection/completed when it reaches
    // zero, and returns the updated PlayerHiveState. Caller is responsible for persisting it
    // (typically inside the same IHiveStateRepository.ExecuteAtomicallyAsync the caller is already
    // running in) and for bumping PlayerHiveState.Revision - this helper never touches Revision
    // itself, matching SpeedUpInventoryService's existing convention.
    public static bool TryReduce(PlayerHiveState state, string category, string targetId, DateTimeOffset now, TimeSpan duration,
        out PlayerHiveState updatedState, out OperationTimerInfo info)
    {
        if (Handlers.TryGetValue(category, out ITimerHandler? handler) && handler.TryReduce(state, targetId, now, duration, out updatedState, out info))
            return true;
        updatedState = state;
        return Miss(out info);
    }

    private static bool Miss(out OperationTimerInfo info) { info = default; return false; }

    internal static readonly IReadOnlyDictionary<string, ITimerHandler> Handlers = new Dictionary<string, ITimerHandler>(StringComparer.Ordinal)
    {
        [SpeedUpCategories.Construction] = new OperationTimerHandler(HiveOperationKind.BuildingUpgrade),
        [SpeedUpCategories.Manufacturing] = new OperationTimerHandler(HiveOperationKind.Production),
        [SpeedUpCategories.Research] = new ResearchTimerHandler(),
        [SpeedUpCategories.Training] = new TrainingTimerHandler(),
        [SpeedUpCategories.Healing] = new HealingTimerHandler(),
    };

    internal interface ITimerHandler
    {
        bool TryPeek(PlayerHiveState state, string targetId, DateTimeOffset now, out OperationTimerInfo info);
        bool TryReduce(PlayerHiveState state, string targetId, DateTimeOffset now, TimeSpan duration, out PlayerHiveState updatedState, out OperationTimerInfo info);
    }

    private sealed class OperationTimerHandler(HiveOperationKind kind) : ITimerHandler
    {
        public bool TryPeek(PlayerHiveState state, string targetId, DateTimeOffset now, out OperationTimerInfo info)
        {
            HiveOperation? operation = state.Operations.FirstOrDefault(operation => operation.Kind == kind && operation.BuildingKey == targetId && operation.Status != HiveOperationStatus.Collected);
            if (operation is null) { info = default; return false; }
            info = new OperationTimerInfo(operation.OperationId, operation.StartedAtUtc, operation.CompletesAtUtc, operation.Status == HiveOperationStatus.AwaitingCollection || operation.CompletesAtUtc <= now);
            return true;
        }

        public bool TryReduce(PlayerHiveState state, string targetId, DateTimeOffset now, TimeSpan duration, out PlayerHiveState updatedState, out OperationTimerInfo info)
        {
            int index = state.Operations.FindIndex(operation => operation.Kind == kind && operation.BuildingKey == targetId && operation.Status != HiveOperationStatus.Collected);
            if (index < 0) { updatedState = state; info = default; return false; }
            HiveOperation operation = state.Operations[index];
            DateTimeOffset end = operation.CompletesAtUtc - duration;
            bool completed = end <= now;
            List<HiveOperation> operations = [.. state.Operations];
            operations[index] = operation with { CompletesAtUtc = end <= now ? now : end, Status = completed ? HiveOperationStatus.AwaitingCollection : HiveOperationStatus.Running };
            updatedState = state with { Operations = operations };
            info = new OperationTimerInfo(operation.OperationId, operation.StartedAtUtc, end <= now ? now : end, completed);
            return true;
        }
    }

    private sealed class ResearchTimerHandler : ITimerHandler
    {
        public bool TryPeek(PlayerHiveState state, string targetId, DateTimeOffset now, out OperationTimerInfo info)
        {
            ResearchOperation? operation = state.Research?.ActiveOperation;
            if (operation is null || operation.ResearchId != targetId) { info = default; return false; }
            info = new OperationTimerInfo(operation.OperationId, operation.StartedAtUtc, operation.EndsAtUtc, operation.EndsAtUtc <= now);
            return true;
        }

        public bool TryReduce(PlayerHiveState state, string targetId, DateTimeOffset now, TimeSpan duration, out PlayerHiveState updatedState, out OperationTimerInfo info)
        {
            ResearchOperation? operation = state.Research?.ActiveOperation;
            if (operation is null || operation.ResearchId != targetId) { updatedState = state; info = default; return false; }
            DateTimeOffset end = operation.EndsAtUtc - duration;
            bool completed = end <= now;
            HiveResearchState research = state.Research! with { ActiveOperation = operation with { EndsAtUtc = end <= now ? now : end } };
            updatedState = state with { Research = research };
            info = new OperationTimerInfo(operation.OperationId, operation.StartedAtUtc, end <= now ? now : end, completed);
            return true;
        }
    }

    private sealed class TrainingTimerHandler : ITimerHandler
    {
        public bool TryPeek(PlayerHiveState state, string targetId, DateTimeOffset now, out OperationTimerInfo info)
        {
            DoctrineTrainingOperation? operation = state.DoctrineRoster?.ActiveOperation;
            if (operation is null || operation.Family != targetId) { info = default; return false; }
            info = new OperationTimerInfo(operation.OperationId, operation.StartedAtUtc, operation.EndsAtUtc, operation.EndsAtUtc <= now);
            return true;
        }

        public bool TryReduce(PlayerHiveState state, string targetId, DateTimeOffset now, TimeSpan duration, out PlayerHiveState updatedState, out OperationTimerInfo info)
        {
            DoctrineTrainingOperation? operation = state.DoctrineRoster?.ActiveOperation;
            if (operation is null || operation.Family != targetId) { updatedState = state; info = default; return false; }
            DateTimeOffset end = operation.EndsAtUtc - duration;
            bool completed = end <= now;
            DoctrineRosterState roster = state.DoctrineRoster! with { ActiveOperation = operation with { EndsAtUtc = end <= now ? now : end } };
            updatedState = state with { DoctrineRoster = roster };
            info = new OperationTimerInfo(operation.OperationId, operation.StartedAtUtc, end <= now ? now : end, completed);
            return true;
        }
    }

    private sealed class HealingTimerHandler : ITimerHandler
    {
        public bool TryPeek(PlayerHiveState state, string targetId, DateTimeOffset now, out OperationTimerInfo info)
        {
            BroodVitalityOperation? operation = state.BroodVitality?.ActiveOperation;
            if (operation is null || operation.Type != targetId) { info = default; return false; }
            info = new OperationTimerInfo(operation.OperationId, operation.StartedAtUtc, operation.EndsAtUtc, operation.EndsAtUtc <= now);
            return true;
        }

        public bool TryReduce(PlayerHiveState state, string targetId, DateTimeOffset now, TimeSpan duration, out PlayerHiveState updatedState, out OperationTimerInfo info)
        {
            BroodVitalityOperation? operation = state.BroodVitality?.ActiveOperation;
            if (operation is null || operation.Type != targetId) { updatedState = state; info = default; return false; }
            DateTimeOffset end = operation.EndsAtUtc - duration;
            bool completed = end <= now;
            BroodVitalityState vitality = state.BroodVitality! with { ActiveOperation = operation with { EndsAtUtc = end <= now ? now : end } };
            updatedState = state with { BroodVitality = vitality };
            info = new OperationTimerInfo(operation.OperationId, operation.StartedAtUtc, end <= now ? now : end, completed);
            return true;
        }
    }
}
