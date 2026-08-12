using BeeKingdom.Core.Events;
using BeeKingdom.Core.Time;

namespace BeeKingdom.World
{
    public readonly struct WorldCreated : IGameplayEvent
    {
        public string WorldId { get; }
        public WorldCreated(string worldId) { WorldId = worldId; }
    }

    public readonly struct WorldLoaded : IGameplayEvent
    {
        public string WorldId { get; }
        public WorldLoaded(string worldId) { WorldId = worldId; }
    }

    public readonly struct WorldSaved : IGameplayEvent
    {
        public string WorldId { get; }
        public WorldSaved(string worldId) { WorldId = worldId; }
    }

    public readonly struct RegionGenerated : IGameplayEvent
    {
        public string RegionId { get; }
        public RegionGenerated(string regionId) { RegionId = regionId; }
    }

    public readonly struct RegionLoaded : IGameplayEvent
    {
        public string RegionId { get; }
        public RegionLoaded(string regionId) { RegionId = regionId; }
    }

    public readonly struct RegionUnloaded : IGameplayEvent
    {
        public string RegionId { get; }
        public RegionUnloaded(string regionId) { RegionId = regionId; }
    }

    public readonly struct RegionActivated : IGameplayEvent
    {
        public string RegionId { get; }
        public RegionActivated(string regionId) { RegionId = regionId; }
    }

    public readonly struct RegionSuspended : IGameplayEvent
    {
        public string RegionId { get; }
        public RegionSuspended(string regionId) { RegionId = regionId; }
    }

    public readonly struct RegionUpdated : IGameplayEvent
    {
        public string RegionId { get; }
        public RegionUpdated(string regionId) { RegionId = regionId; }
    }

    public readonly struct NeighborRegionChanged : IGameplayEvent
    {
        public string RegionId { get; }
        public NeighborRegionChanged(string regionId) { RegionId = regionId; }
    }

    public readonly struct WorldWeatherChanged : IGameplayEvent
    {
        public string RegionId { get; }
        public WorldWeather Weather { get; }
        public WorldWeatherChanged(string regionId, WorldWeather weather) { RegionId = regionId; Weather = weather; }
    }

    public readonly struct WorldSeasonChanged : IGameplayEvent
    {
        public SimulationSeason Season { get; }
        public WorldSeasonChanged(SimulationSeason season) { Season = season; }
    }

    public readonly struct WorldValidated : IGameplayEvent
    {
        public bool IsValid { get; }
        public WorldValidated(bool isValid) { IsValid = isValid; }
    }
}
