using System;
using System.Collections.Generic;
using System.Linq;
using BeeKingdom.Core.Time;

namespace BeeKingdom.World
{
    public enum WorldLayerType { World, Region, Zone, Sector, Cell, PointOfInterest }
    public enum RegionSimulationState { Active, Sleeping, Suspended, Loading, Unloading }
    public enum RegionLoadMode { OnDemand, Streaming, Preload, AutomaticUnload, Persistent }

    public sealed class WorldEngine
    {
        private readonly WorldRegistry registry = new WorldRegistry();
        public WorldRegistry Registry => registry;
        public WorldInstance CreateWorld(WorldDefinition definition)
        {
            WorldInstance instance = new WorldInstance(definition, new WorldSnapshot(definition.WorldId, definition.Seed, SimulationSeason.Spring, WorldWeather.Clear, 0, 0));
            registry.RegisterWorld(instance);
            return instance;
        }

        public WorldInstance LoadWorld(WorldSnapshot snapshot, WorldDefinition definition)
        {
            WorldInstance instance = new WorldInstance(definition, snapshot);
            registry.RegisterWorld(instance);
            return instance;
        }

        public WorldSnapshot SaveWorld(string worldId)
        {
            WorldInstance instance = registry.QueryWorld(worldId);
            return instance == null ? null : instance.Snapshot;
        }
    }

    public sealed class WorldDefinition
    {
        private readonly Dictionary<string, BiomeDefinition> biomes = new Dictionary<string, BiomeDefinition>();
        private readonly Dictionary<string, RegionDefinition> regions = new Dictionary<string, RegionDefinition>();

        public string WorldId { get; }
        public string Name { get; }
        public WorldSeed Seed { get; }
        public IReadOnlyDictionary<string, BiomeDefinition> Biomes => biomes;
        public IReadOnlyDictionary<string, RegionDefinition> Regions => regions;

        public WorldDefinition(string worldId, string name, WorldSeed seed)
        {
            WorldId = RequireId(worldId);
            Name = string.IsNullOrWhiteSpace(name) ? worldId : name;
            Seed = seed;
        }

        public void RegisterBiome(BiomeDefinition biome)
        {
            if (biome == null) throw new ArgumentNullException(nameof(biome));
            biomes[biome.BiomeId] = biome;
        }

        public void RegisterRegion(RegionDefinition region)
        {
            if (region == null) throw new ArgumentNullException(nameof(region));
            regions[region.RegionId] = region;
        }

        internal static string RequireId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Id is required.");
            return value;
        }
    }

    public sealed class WorldInstance
    {
        private readonly Dictionary<string, RegionInstance> regions = new Dictionary<string, RegionInstance>();
        public WorldDefinition Definition { get; }
        public WorldSnapshot Snapshot { get; private set; }
        public IReadOnlyDictionary<string, RegionInstance> Regions => regions;

        public WorldInstance(WorldDefinition definition, WorldSnapshot snapshot)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        }

        public void RegisterRegion(RegionInstance region)
        {
            regions[region.Definition.RegionId] = region;
            Snapshot = Snapshot.WithCounts(regions.Count, Snapshot.ActiveEventCount);
        }

        public void UpdateSeason(SimulationSeason season)
        {
            Snapshot = Snapshot.WithSeason(season);
        }

        public void UpdateWeather(WorldWeather weather)
        {
            Snapshot = Snapshot.WithWeather(weather);
        }
    }

    public sealed class WorldSnapshot
    {
        public string WorldId { get; }
        public WorldSeed Seed { get; }
        public SimulationSeason CurrentSeason { get; }
        public WorldWeather CurrentWeather { get; }
        public int ActiveColonies { get; }
        public int ActiveEventCount { get; }

        public WorldSnapshot(string worldId, WorldSeed seed, SimulationSeason currentSeason, WorldWeather currentWeather, int activeColonies, int activeEventCount)
        {
            WorldId = WorldDefinition.RequireId(worldId);
            Seed = seed;
            CurrentSeason = currentSeason;
            CurrentWeather = currentWeather;
            ActiveColonies = Math.Max(0, activeColonies);
            ActiveEventCount = Math.Max(0, activeEventCount);
        }

        public WorldSnapshot WithSeason(SimulationSeason season) { return new WorldSnapshot(WorldId, Seed, season, CurrentWeather, ActiveColonies, ActiveEventCount); }
        public WorldSnapshot WithWeather(WorldWeather weather) { return new WorldSnapshot(WorldId, Seed, CurrentSeason, weather, ActiveColonies, ActiveEventCount); }
        public WorldSnapshot WithCounts(int activeColonies, int activeEvents) { return new WorldSnapshot(WorldId, Seed, CurrentSeason, CurrentWeather, activeColonies, activeEvents); }
    }

    public sealed class WorldRegistry
    {
        private readonly Dictionary<string, WorldInstance> worlds = new Dictionary<string, WorldInstance>();
        public void RegisterWorld(WorldInstance instance) { worlds[instance.Definition.WorldId] = instance; }
        public WorldInstance QueryWorld(string worldId) { return worlds.TryGetValue(worldId, out WorldInstance instance) ? instance : null; }
        public IReadOnlyList<WorldInstance> QueryWorlds() { return worlds.Values.ToList(); }
    }

    public sealed class BiomeDefinition
    {
        public string BiomeId { get; }
        public WorldBiomeType Type { get; }
        public WorldClimate Climate { get; }

        public BiomeDefinition(string biomeId, WorldBiomeType type, WorldClimate climate)
        {
            BiomeId = WorldDefinition.RequireId(biomeId);
            Type = type;
            Climate = climate;
        }
    }

    public sealed class RegionManager
    {
        private readonly RegionEngine engine = new RegionEngine();
        public event Action<RegionInstance> RegionLoaded;
        public event Action<RegionInstance> RegionUnloaded;
        public event Action<RegionInstance> RegionActivated;
        public event Action<RegionInstance> RegionSuspended;
        public event Action<RegionInstance> RegionUpdated;
        public event Action<RegionInstance> NeighborRegionChanged;

        public void RegisterRegion(RegionDefinition definition) { engine.Registry.RegisterRegion(definition); }
        public RegionInstance CreateRegion(RegionDefinition definition) { RegisterRegion(definition); return engine.CreateRegion(definition.RegionId); }
        public RegionInstance LoadRegion(string regionId) { RegionInstance instance = engine.LoadRegion(regionId); RegionLoaded?.Invoke(instance); return instance; }
        public bool UnloadRegion(string regionId) { RegionInstance instance = engine.QueryRegion(regionId); bool changed = engine.UnloadRegion(regionId); if (changed) RegionUnloaded?.Invoke(instance); return changed; }
        public RegionInstance QueryRegion(string regionId) { return engine.QueryRegion(regionId); }
        public IReadOnlyList<RegionInstance> QueryNeighborRegions(string regionId) { return engine.QueryNeighborRegions(regionId); }
        public bool SetState(string regionId, RegionSimulationState state) { bool changed = engine.SetState(regionId, state); if (!changed) return false; RegionInstance instance = engine.QueryRegion(regionId); if (state == RegionSimulationState.Active) RegionActivated?.Invoke(instance); if (state == RegionSimulationState.Suspended) RegionSuspended?.Invoke(instance); RegionUpdated?.Invoke(instance); return true; }
        public bool NotifyNeighborRegionChanged(string regionId) { RegionInstance instance = engine.QueryRegion(regionId); if (instance == null) return false; NeighborRegionChanged?.Invoke(instance); return true; }
    }

    public sealed class RegionEngine
    {
        private readonly Dictionary<string, RegionInstance> loaded = new Dictionary<string, RegionInstance>();
        public RegionRegistry Registry { get; } = new RegionRegistry();

        public RegionInstance CreateRegion(string regionId)
        {
            RegionDefinition definition = Registry.QueryDefinition(regionId);
            RegionInstance instance = new RegionInstance(definition, new RegionSnapshot(regionId, definition.WorldId, definition.Seed, definition.Biome, definition.Weather, definition.Season, definition.Temperature, definition.Humidity, 0, 0, RegionSimulationState.Sleeping));
            loaded[regionId] = instance;
            return instance;
        }

        public RegionInstance LoadRegion(string regionId)
        {
            RegionInstance instance = loaded.TryGetValue(regionId, out RegionInstance existing) ? existing : CreateRegion(regionId);
            instance.SetState(RegionSimulationState.Active);
            return instance;
        }

        public bool UnloadRegion(string regionId)
        {
            if (!loaded.TryGetValue(regionId, out RegionInstance instance)) return false;
            instance.SetState(RegionSimulationState.Unloading);
            loaded.Remove(regionId);
            return true;
        }

        public RegionInstance QueryRegion(string regionId) { return loaded.TryGetValue(regionId, out RegionInstance instance) ? instance : null; }
        public IReadOnlyList<RegionInstance> QueryNeighborRegions(string regionId)
        {
            RegionDefinition definition = Registry.QueryDefinition(regionId);
            return definition.NeighborRegionIds.Select(QueryRegion).Where(r => r != null).ToList();
        }

        public bool SetState(string regionId, RegionSimulationState state)
        {
            RegionInstance instance = QueryRegion(regionId);
            if (instance == null) return false;
            instance.SetState(state);
            return true;
        }
    }

    public sealed class RegionDefinition
    {
        public string RegionId { get; }
        public string WorldId { get; }
        public WorldSeed Seed { get; }
        public WorldBiomeType Biome { get; }
        public WorldWeather Weather { get; }
        public SimulationSeason Season { get; }
        public double Temperature { get; }
        public double Humidity { get; }
        public int SectorSize { get; }
        public int CellSize { get; }
        public int ChunkSize { get; }
        public IReadOnlyList<string> NeighborRegionIds { get; }

        public RegionDefinition(string regionId, string worldId, WorldSeed seed, WorldBiomeType biome, WorldWeather weather, SimulationSeason season, double temperature, double humidity, int sectorSize, int cellSize, int chunkSize, IReadOnlyList<string> neighborRegionIds = null)
        {
            RegionId = WorldDefinition.RequireId(regionId);
            WorldId = WorldDefinition.RequireId(worldId);
            Seed = seed;
            Biome = biome;
            Weather = weather;
            Season = season;
            Temperature = temperature;
            Humidity = humidity;
            SectorSize = Math.Max(1, sectorSize);
            CellSize = Math.Max(1, cellSize);
            ChunkSize = Math.Max(1, chunkSize);
            NeighborRegionIds = neighborRegionIds ?? Array.Empty<string>();
        }
    }

    public sealed class RegionInstance
    {
        public RegionDefinition Definition { get; }
        public RegionSnapshot Snapshot { get; private set; }

        public RegionInstance(RegionDefinition definition, RegionSnapshot snapshot)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        }

        public void SetState(RegionSimulationState state)
        {
            Snapshot = Snapshot.WithState(state);
        }
    }

    public sealed class RegionRegistry
    {
        private readonly Dictionary<string, RegionDefinition> definitions = new Dictionary<string, RegionDefinition>();
        public void RegisterRegion(RegionDefinition definition) { definitions[definition.RegionId] = definition; }
        public RegionDefinition QueryDefinition(string regionId) { if (!definitions.TryGetValue(regionId, out RegionDefinition definition)) throw new KeyNotFoundException(regionId); return definition; }
    }

    public sealed class RegionSnapshot
    {
        public string RegionId { get; }
        public string WorldId { get; }
        public WorldSeed Seed { get; }
        public WorldBiomeType Biome { get; }
        public WorldWeather Weather { get; }
        public SimulationSeason Season { get; }
        public double Temperature { get; }
        public double Humidity { get; }
        public int ColonyCount { get; }
        public int ResourceCount { get; }
        public RegionSimulationState State { get; }

        public RegionSnapshot(string regionId, string worldId, WorldSeed seed, WorldBiomeType biome, WorldWeather weather, SimulationSeason season, double temperature, double humidity, int colonyCount, int resourceCount, RegionSimulationState state)
        {
            RegionId = WorldDefinition.RequireId(regionId);
            WorldId = WorldDefinition.RequireId(worldId);
            Seed = seed;
            Biome = biome;
            Weather = weather;
            Season = season;
            Temperature = temperature;
            Humidity = humidity;
            ColonyCount = Math.Max(0, colonyCount);
            ResourceCount = Math.Max(0, resourceCount);
            State = state;
        }

        public RegionSnapshot WithState(RegionSimulationState state)
        {
            return new RegionSnapshot(RegionId, WorldId, Seed, Biome, Weather, Season, Temperature, Humidity, ColonyCount, ResourceCount, state);
        }
    }

    public sealed class RegionDiagnostics
    {
        public int Loads { get; private set; }
        public int Unloads { get; private set; }
        public int Transitions { get; private set; }
        public void RecordLoad() { Loads++; }
        public void RecordUnload() { Unloads++; }
        public void RecordTransition() { Transitions++; }
    }
}
