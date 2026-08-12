using System;
using System.Collections.Generic;
using BeeKingdom.Core.Services;
using BeeKingdom.Core.Simulation;
using BeeKingdom.Core.Time;

namespace BeeKingdom.World
{
    public sealed class SeasonManager : ISimulationSystem
    {
        private readonly IEventBus eventBus;
        private double elapsedSeconds;

        public Type SystemType => typeof(SeasonManager);
        public string Name => nameof(SeasonManager);
        public SimulationPhase Phase => SimulationPhase.PreSimulation;
        public int Priority => 60;
        public IReadOnlyList<Type> RunsAfter => new[] { typeof(WorldManager) };
        public IReadOnlyList<Type> RunsBefore => Array.Empty<Type>();
        public SimulationSeason CurrentSeason { get; private set; }
        public double SeasonLengthSeconds { get; }

        public SeasonManager(double seasonLengthSeconds = 604800d, IEventBus eventBus = null)
        {
            SeasonLengthSeconds = seasonLengthSeconds <= 0d ? 1d : seasonLengthSeconds;
            CurrentSeason = SimulationSeason.Spring;
            this.eventBus = eventBus;
        }

        public void SetSeason(SimulationSeason season)
        {
            if (CurrentSeason == season)
            {
                return;
            }

            CurrentSeason = season;
            eventBus?.Publish(new SeasonChanged(season));
        }

        public void Execute(in SimulationExecutionContext context)
        {
            elapsedSeconds += context.DeltaSeconds;
            int index = (int)(elapsedSeconds / SeasonLengthSeconds) % 4;
            SetSeason((SimulationSeason)index);
        }
    }
}
