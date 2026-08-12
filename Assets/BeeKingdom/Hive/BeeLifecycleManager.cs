using System;
using System.Collections.Generic;
using BeeKingdom.Core.Services;
using BeeKingdom.Core.Simulation;

namespace BeeKingdom.Hive
{
    public sealed class BeeLifecycleManager : ISimulationSystem
    {
        private readonly Dictionary<string, BeeLifecycleBee> bees = new Dictionary<string, BeeLifecycleBee>();
        private readonly BeeLifecycleRules rules;
        private readonly BeeLifecycleStateMachine stateMachine;
        private readonly IEventBus eventBus;

        public Type SystemType => typeof(BeeLifecycleManager);
        public string Name => nameof(BeeLifecycleManager);
        public SimulationPhase Phase => SimulationPhase.Simulation;
        public int Priority => 120;
        public IReadOnlyList<Type> RunsAfter => new[] { typeof(HiveManager), typeof(QueenManager) };
        public IReadOnlyList<Type> RunsBefore => Array.Empty<Type>();
        public BeeLifecycleDiagnostics Diagnostics { get; } = new BeeLifecycleDiagnostics();

        public BeeLifecycleManager(BeeLifecycleRules rules, IEventBus eventBus = null)
        {
            this.rules = rules;
            this.eventBus = eventBus;
            stateMachine = new BeeLifecycleStateMachine(rules);
        }

        public BeeLifecycleBee CreateBee(string beeId, string hiveId, double birthTime, BeeLifecycleRole role, int health, int energy, string geneticsId)
        {
            BeeLifecycleBee bee = new BeeLifecycleBee(beeId, hiveId, birthTime, role, health, energy, geneticsId);
            bees.Add(beeId, bee);
            eventBus?.Publish(new BeeBorn(beeId, hiveId));
            Record();
            return bee;
        }

        public void AdvanceLifecycle(string beeId, double deltaSeconds, float seasonModifier = 1f, float queenModifier = 1f, float researchModifier = 1f)
        {
            BeeLifecycleBee bee = GetBee(beeId);
            if (!bee.Alive)
            {
                return;
            }

            float multiplier = rules.BaseBiologicalAgeMultiplier * seasonModifier * queenModifier * researchModifier;
            bee.AdvanceAge(deltaSeconds, multiplier);
            eventBus?.Publish(new BeeAged(beeId, bee.Age.AgeSeconds));

            BeeLifecycleStage resolved = stateMachine.ResolveStage(bee.Age.BiologicalAgeSeconds);
            if (resolved == BeeLifecycleStage.Dead)
            {
                KillBee(beeId, BeeMortalityCause.OldAge);
                return;
            }

            if (resolved != bee.CurrentStage)
            {
                ChangeStage(beeId, resolved);
            }

            Record();
        }

        public bool ChangeStage(string beeId, BeeLifecycleStage stage)
        {
            BeeLifecycleBee bee = GetBee(beeId);
            if (!stateMachine.TryTransition(bee.CurrentStage, stage, out BeeLifecycleStage result))
            {
                return false;
            }

            bee.ChangeStage(result);
            eventBus?.Publish(new BeeStageChanged(beeId, result));
            Record();
            return true;
        }

        public bool ChangeRole(string beeId, BeeLifecycleRole role)
        {
            BeeLifecycleBee bee = GetBee(beeId);
            if (!bee.Alive)
            {
                return false;
            }

            bee.ChangeRole(role);
            eventBus?.Publish(new BeeRoleChanged(beeId, role));
            return true;
        }

        public bool KillBee(string beeId, BeeMortalityCause cause)
        {
            BeeLifecycleBee bee = GetBee(beeId);
            if (!bee.Alive)
            {
                return false;
            }

            bee.Kill();
            eventBus?.Publish(new BeeDied(beeId, cause));
            Record();
            return true;
        }

        public bool Validate(string beeId)
        {
            BeeLifecycleBee bee = GetBee(beeId);
            return !string.IsNullOrWhiteSpace(bee.BeeId) &&
                !string.IsNullOrWhiteSpace(bee.HiveId) &&
                (!bee.Alive || bee.Health > 0);
        }

        public BeeLifecycleBee GetBee(string beeId)
        {
            if (bees.TryGetValue(beeId, out BeeLifecycleBee bee))
            {
                return bee;
            }

            throw new KeyNotFoundException($"Bee {beeId} was not found.");
        }

        public void Execute(in SimulationExecutionContext context)
        {
            foreach (BeeLifecycleBee bee in bees.Values)
            {
                if (bee.Alive)
                {
                    AdvanceLifecycle(bee.BeeId, context.DeltaSeconds);
                }
            }
        }

        private void Record()
        {
            int alive = 0;
            foreach (BeeLifecycleBee bee in bees.Values)
            {
                if (bee.Alive)
                {
                    alive++;
                }
            }

            Diagnostics.Record(bees.Count, alive, bees.Count - alive);
        }
    }
}
