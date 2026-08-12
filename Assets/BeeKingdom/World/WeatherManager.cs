using System;
using System.Collections.Generic;
using BeeKingdom.Core.Services;
using BeeKingdom.Core.Simulation;
using BeeKingdom.Core.Time;

namespace BeeKingdom.World
{
    public sealed class WeatherManager : ISimulationSystem
    {
        private readonly IEventBus eventBus;
        private readonly WeatherProfile profile;
        private readonly ClimateRules climateRules;
        private readonly int seed;
        private double elapsedSeconds;
        private int weatherStep;

        public Type SystemType => typeof(WeatherManager);
        public string Name => nameof(WeatherManager);
        public SimulationPhase Phase => SimulationPhase.PreSimulation;
        public int Priority => 65;
        public IReadOnlyList<Type> RunsAfter => new[] { typeof(SeasonManager) };
        public IReadOnlyList<Type> RunsBefore => Array.Empty<Type>();
        public WorldWeather CurrentWeather { get; private set; }
        public double WeatherDurationSeconds { get; }
        public ClimateRules ClimateRules => climateRules;

        public WeatherManager(WorldSeed seed, WeatherProfile profile = null, ClimateRules climateRules = null, double weatherDurationSeconds = 21600d, IEventBus eventBus = null)
        {
            this.seed = seed.Hash;
            this.profile = profile ?? WeatherProfile.Temperate();
            this.climateRules = climateRules ?? ClimateRules.CreateDefault();
            WeatherDurationSeconds = weatherDurationSeconds <= 0d ? 1d : weatherDurationSeconds;
            this.eventBus = eventBus;
            CurrentWeather = WorldWeather.Clear;
        }

        public void SetWeather(WorldWeather weather)
        {
            if (CurrentWeather == weather)
            {
                return;
            }

            CurrentWeather = weather;
            eventBus?.Publish(new WeatherChanged(weather));
        }

        public void Execute(in SimulationExecutionContext context)
        {
            elapsedSeconds += context.DeltaSeconds;
            int step = (int)(elapsedSeconds / WeatherDurationSeconds);
            if (step == weatherStep)
            {
                return;
            }

            weatherStep = step;
            SetWeather(profile.Select(Random01(seed, step)));
        }

        public double GetProductionModifier(SimulationSeason season)
        {
            return climateRules.GetProductionModifier(season);
        }

        public double GetMovementModifier()
        {
            return climateRules.GetMovementModifier(CurrentWeather);
        }

        public double GetConsumptionModifier(SimulationSeason season)
        {
            return climateRules.GetConsumptionModifier(season);
        }

        private static double Random01(int seed, int step)
        {
            unchecked
            {
                uint state = (uint)(seed ^ (step * 1103515245));
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                return state / (double)uint.MaxValue;
            }
        }
    }

    public readonly struct WeatherChanged : BeeKingdom.Core.Events.IGameplayEvent
    {
        public WorldWeather Weather { get; }
        public WeatherChanged(WorldWeather weather) { Weather = weather; }
    }
}
