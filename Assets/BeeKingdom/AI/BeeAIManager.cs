using System;
using System.Collections.Generic;
using BeeKingdom.Core.Services;
using BeeKingdom.Core.Simulation;
using BeeKingdom.Hive;
using BeeKingdom.Population;

namespace BeeKingdom.AI
{
    public sealed class BeeAIManager : ISimulationSystem
    {
        private readonly Dictionary<string, BeeBrain> brains = new Dictionary<string, BeeBrain>();
        private readonly Dictionary<BeeIntent, BehaviorDefinition> behaviorDefinitions = new Dictionary<BeeIntent, BehaviorDefinition>();
        private readonly Dictionary<string, BehaviorContext> behaviors = new Dictionary<string, BehaviorContext>();
        private readonly BehaviorExecutor behaviorExecutor = new BehaviorExecutor();
        private readonly BehaviorScheduler behaviorScheduler = new BehaviorScheduler();
        private readonly BeeBehaviorProfile profile = new BeeBehaviorProfile();
        private readonly IEventBus eventBus;
        private int updateCursor;
        private readonly int updatesPerTick;

        public Type SystemType => typeof(BeeAIManager);
        public string Name => nameof(BeeAIManager);
        public SimulationPhase Phase => SimulationPhase.LateSimulation;
        public int Priority => 300;
        public IReadOnlyList<Type> RunsAfter => new[] { typeof(TaskManager) };
        public IReadOnlyList<Type> RunsBefore => Array.Empty<Type>();
        public BeeAIDiagnostics Diagnostics { get; } = new BeeAIDiagnostics();

        public BeeAIManager(IEventBus eventBus = null, int updatesPerTick = 256)
        {
            this.eventBus = eventBus;
            this.updatesPerTick = updatesPerTick <= 0 ? 1 : updatesPerTick;
        }

        public bool RegisterBehavior(BehaviorDefinition definition)
        {
            if (definition == null || behaviorDefinitions.ContainsKey(definition.Intent))
            {
                return false;
            }

            behaviorDefinitions.Add(definition.Intent, definition);
            Diagnostics.RecordBehaviorRegistered();
            return true;
        }

        public BehaviorContext ExecuteBehavior(string beeId, BeeIntent intent, string targetId = "")
        {
            if (!behaviorDefinitions.TryGetValue(intent, out BehaviorDefinition definition))
            {
                return null;
            }

            BehaviorContext context = behaviorScheduler.Schedule(beeId, definition, targetId);
            behaviors[beeId] = context;
            behaviorExecutor.Start(context);
            eventBus?.Publish(new BehaviorStarted(beeId, intent));
            return context;
        }

        public bool InterruptBehavior(string beeId, string reason)
        {
            if (!behaviors.TryGetValue(beeId, out BehaviorContext context))
            {
                return false;
            }

            behaviorExecutor.Interrupt(context);
            Diagnostics.RecordInterrupt();
            eventBus?.Publish(new BehaviorInterrupted(beeId, reason ?? string.Empty));
            return true;
        }

        public bool ResumeBehavior(string beeId)
        {
            if (!behaviors.TryGetValue(beeId, out BehaviorContext context))
            {
                return false;
            }

            behaviorExecutor.Resume(context);
            eventBus?.Publish(new BeeResumed(beeId));
            return true;
        }

        public bool CancelBehavior(string beeId)
        {
            bool removed = behaviors.Remove(beeId);
            if (removed)
            {
                eventBus?.Publish(new BehaviorFailed(beeId, "cancelled"));
            }

            return removed;
        }

        public BehaviorContext QueryBehavior(string beeId)
        {
            return behaviors.TryGetValue(beeId, out BehaviorContext context) ? context : null;
        }

        public BeeBrain CreateBrain(string beeId, int energy, int health)
        {
            BeeBrain brain = new BeeBrain(beeId, new BeeBlackboard(energy, health), profile);
            brains.Add(beeId, brain);
            Record();
            return brain;
        }

        public bool AssignTask(string beeId, BeeKingdom.Hive.TaskInstance task)
        {
            BeeBrain brain = GetBrain(beeId);
            bool assigned = brain.AssignTask(task);
            if (assigned)
            {
                eventBus?.Publish(new BeeTaskStarted(beeId, task.TaskId));
                eventBus?.Publish(new BeeStateChanged(beeId, brain.GetCurrentState()));
            }

            return assigned;
        }

        public bool CancelTask(string beeId)
        {
            BeeBrain brain = GetBrain(beeId);
            bool cancelled = brain.CancelTask();
            if (cancelled)
            {
                eventBus?.Publish(new BeeIdle(beeId));
            }

            return cancelled;
        }

        public bool UpdateBehavior(string beeId, double deltaSeconds)
        {
            if (behaviors.TryGetValue(beeId, out BehaviorContext context))
            {
                BehaviorExecutionState state = behaviorExecutor.Tick(context, deltaSeconds);
                if (state == BehaviorExecutionState.Waiting)
                {
                    eventBus?.Publish(new BeeWaiting(beeId));
                }
                else if (state == BehaviorExecutionState.Completed)
                {
                    behaviors.Remove(beeId);
                    eventBus?.Publish(new BehaviorCompleted(beeId, context.Intent));
                    return true;
                }
                else if (state == BehaviorExecutionState.Failed)
                {
                    behaviors.Remove(beeId);
                    eventBus?.Publish(new BehaviorFailed(beeId, "execution_failed"));
                    return false;
                }
            }

            BeeBrain brain = GetBrain(beeId);
            string taskId = brain.Blackboard.CurrentTaskId;
            bool completed = brain.UpdateBehavior(deltaSeconds);
            if (completed)
            {
                eventBus?.Publish(new BeeTaskCompleted(beeId, taskId));
                eventBus?.Publish(new BeeIdle(beeId));
            }
            else if (brain.GetCurrentState() == BeeBehaviorState.Waiting)
            {
                eventBus?.Publish(new BeeWaiting(beeId));
            }

            return completed;
        }

        public void Interrupt(string beeId)
        {
            BeeBrain brain = GetBrain(beeId);
            brain.Interrupt();
            Diagnostics.RecordInterrupt();
            eventBus?.Publish(new BeeBehaviorInterrupted(beeId));
        }

        public void Resume(string beeId)
        {
            GetBrain(beeId).Resume();
        }

        public BeeBehaviorState GetCurrentState(string beeId)
        {
            return GetBrain(beeId).GetCurrentState();
        }

        public BeeAIStatistics GetStatistics()
        {
            int active = 0;
            int waiting = 0;
            foreach (BeeBrain brain in brains.Values)
            {
                BeeBehaviorState state = brain.GetCurrentState();
                if (state == BeeBehaviorState.Waiting) waiting++;
                if (state != BeeBehaviorState.Idle && state != BeeBehaviorState.Waiting && state != BeeBehaviorState.Dead) active++;
            }

            return new BeeAIStatistics(brains.Count, active, waiting);
        }

        public void Execute(in SimulationExecutionContext context)
        {
            if (brains.Count == 0)
            {
                return;
            }

            int processed = 0;
            foreach (BeeBrain brain in brains.Values)
            {
                if (processed >= updatesPerTick)
                {
                    break;
                }

                if (updateCursor > 0)
                {
                    updateCursor--;
                    continue;
                }

                brain.UpdateBehavior(context.DeltaSeconds);
                processed++;
            }

            updateCursor = (updateCursor + processed) % brains.Count;
            Record();
        }

        private BeeBrain GetBrain(string beeId)
        {
            if (brains.TryGetValue(beeId, out BeeBrain brain))
            {
                return brain;
            }

            throw new KeyNotFoundException($"Bee brain {beeId} was not found.");
        }

        private void Record()
        {
            Diagnostics.Record(GetStatistics());
        }
    }
}
