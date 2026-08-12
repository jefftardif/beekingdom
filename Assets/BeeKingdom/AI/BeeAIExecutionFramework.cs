using System;
using BeeKingdom.Population;

namespace BeeKingdom.AI
{
    public enum BehaviorExecutionState { Idle, Thinking, Planning, Moving, Working, Waiting, Interrupted, Recovering, Completed, Failed }
    public enum BehaviorActionType { Build, Gather, Transport, Feed, Patrol, Defend, Explore, Repair, Clean, Rest, Eat, Drink, Follow, Escort, Flee, Custom }

    public sealed class BehaviorDefinition
    {
        public string BehaviorId { get; }
        public BeeIntent Intent { get; }
        public BehaviorActionType ActionType { get; }
        public double DurationSeconds { get; }
        public bool CanResume { get; }

        public BehaviorDefinition(string behaviorId, BeeIntent intent, BehaviorActionType actionType, double durationSeconds, bool canResume = true)
        {
            BehaviorId = string.IsNullOrWhiteSpace(behaviorId) ? throw new ArgumentException("Behavior id is required.", nameof(behaviorId)) : behaviorId;
            Intent = intent;
            ActionType = actionType;
            DurationSeconds = durationSeconds <= 0d ? 0.1d : durationSeconds;
            CanResume = canResume;
        }
    }

    public sealed class BehaviorContext
    {
        public string BeeId { get; }
        public string BehaviorId { get; }
        public BeeIntent Intent { get; }
        public string TargetId { get; }
        public BehaviorExecutionState State { get; private set; }
        public double ElapsedSeconds { get; private set; }
        public double DurationSeconds { get; }
        public bool CanResume { get; }

        public BehaviorContext(string beeId, BehaviorDefinition definition, string targetId)
        {
            BeeId = beeId ?? string.Empty;
            BehaviorId = definition.BehaviorId;
            Intent = definition.Intent;
            TargetId = targetId ?? string.Empty;
            DurationSeconds = definition.DurationSeconds;
            CanResume = definition.CanResume;
            State = BehaviorExecutionState.Idle;
        }

        public void SetState(BehaviorExecutionState state) => State = state;
        public void Advance(double deltaSeconds) => ElapsedSeconds += Math.Max(0d, deltaSeconds);
    }

    public sealed class BehaviorScheduler
    {
        public BehaviorContext Schedule(string beeId, BehaviorDefinition definition, string targetId)
        {
            BehaviorContext context = new BehaviorContext(beeId, definition, targetId);
            context.SetState(BehaviorExecutionState.Planning);
            return context;
        }
    }

    public sealed class BehaviorExecutor
    {
        public void Start(BehaviorContext context)
        {
            context.SetState(BehaviorExecutionState.Working);
        }

        public BehaviorExecutionState Tick(BehaviorContext context, double deltaSeconds)
        {
            if (context.State == BehaviorExecutionState.Interrupted) return BehaviorExecutionState.Waiting;
            if (context.State == BehaviorExecutionState.Failed || context.State == BehaviorExecutionState.Completed) return context.State;
            context.Advance(deltaSeconds);
            if (context.ElapsedSeconds >= context.DurationSeconds)
            {
                context.SetState(BehaviorExecutionState.Completed);
                return BehaviorExecutionState.Completed;
            }

            context.SetState(BehaviorExecutionState.Working);
            return context.State;
        }

        public void Interrupt(BehaviorContext context)
        {
            context.SetState(BehaviorExecutionState.Interrupted);
        }

        public void Resume(BehaviorContext context)
        {
            context.SetState(context.CanResume ? BehaviorExecutionState.Recovering : BehaviorExecutionState.Failed);
        }
    }
}
