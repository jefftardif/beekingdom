using System;
using System.Collections.Generic;
using BeeKingdom.Core.Services;
using BeeKingdom.Core.Simulation;

namespace BeeKingdom.Hive
{
    public sealed class QueenManager : ISimulationSystem
    {
        private static readonly Type[] EmptyDependencies = Array.Empty<Type>();
        private readonly Dictionary<string, QueenAggregate> queens = new Dictionary<string, QueenAggregate>();
        private readonly IEventBus eventBus;

        public Type SystemType => typeof(QueenManager);
        public string Name => nameof(QueenManager);
        public SimulationPhase Phase => SimulationPhase.Simulation;
        public int Priority => 110;
        public IReadOnlyList<Type> RunsAfter => new[] { typeof(HiveManager) };
        public IReadOnlyList<Type> RunsBefore => EmptyDependencies;
        public QueenDiagnostics Diagnostics { get; } = new QueenDiagnostics();

        public QueenManager(IEventBus eventBus = null)
        {
            this.eventBus = eventBus;
        }

        public QueenAggregate CreateQueen(string queenId, string hiveId, QueenHealth health, int energy, float fertility, float baseEggsPerMinute)
        {
            QueenAggregate queen = new QueenAggregate(
                queenId,
                hiveId,
                health,
                energy,
                fertility,
                new QueenEggProduction(baseEggsPerMinute),
                new QueenEvolution());

            queens.Add(queenId, queen);
            eventBus?.Publish(new QueenCreated(queenId, hiveId));
            Record(queen);
            return queen;
        }

        public QueenAggregate LoadQueen(QueenSnapshot snapshot)
        {
            QueenAggregate queen = QueenAggregate.FromSnapshot(snapshot);
            queens[queen.QueenId] = queen;
            Record(queen);
            return queen;
        }

        public bool UpdateState(string queenId, QueenState state)
        {
            QueenAggregate queen = GetQueen(queenId);
            if (!queen.UpdateState(state))
            {
                return false;
            }

            eventBus?.Publish(new QueenStateChanged(queenId, state));
            if (state == QueenState.Dead)
            {
                eventBus?.Publish(new QueenDied(queenId));
            }

            Record(queen);
            return true;
        }

        public int ProduceEggs(string queenId, double deltaSeconds, float seasonModifier = 1f, float researchModifier = 1f)
        {
            QueenAggregate queen = GetQueen(queenId);
            int produced = queen.ProduceEggs(deltaSeconds, seasonModifier, researchModifier);
            if (produced > 0)
            {
                eventBus?.Publish(new QueenEggProduced(queenId, produced));
            }

            Record(queen);
            return produced;
        }

        public bool AddExperience(string queenId, int amount)
        {
            QueenAggregate queen = GetQueen(queenId);
            bool leveled = queen.AddExperience(amount);
            if (leveled)
            {
                eventBus?.Publish(new QueenLevelUp(queenId, queen.Evolution.Level));
            }

            Record(queen);
            return leveled;
        }

        public void ApplyBonus(string queenId, QueenBonusType type, float value)
        {
            QueenAggregate queen = GetQueen(queenId);
            queen.ApplyBonus(type, value);
            eventBus?.Publish(new QueenBonusChanged(queenId, type, value));
            Record(queen);
        }

        public bool Validate(string queenId)
        {
            QueenAggregate queen = GetQueen(queenId);
            bool valid = queen.Validate();
            Record(queen);
            return valid;
        }

        public QueenStatistics GetStatistics(string queenId)
        {
            return GetQueen(queenId).GetStatistics();
        }

        public QueenAggregate GetQueen(string queenId)
        {
            if (queens.TryGetValue(queenId, out QueenAggregate queen))
            {
                return queen;
            }

            throw new KeyNotFoundException($"Queen {queenId} was not found.");
        }

        public void Execute(in SimulationExecutionContext context)
        {
            foreach (QueenAggregate queen in queens.Values)
            {
                queen.Age(context.DeltaSeconds);
            }
        }

        private void Record(QueenAggregate queen)
        {
            Diagnostics.Record(queen, queen.Validate());
        }
    }
}
