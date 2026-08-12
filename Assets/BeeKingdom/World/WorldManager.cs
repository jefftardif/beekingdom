using System;
using System.Collections.Generic;
using BeeKingdom.Core.Services;
using BeeKingdom.Core.Simulation;

namespace BeeKingdom.World
{
    public sealed class WorldManager : ISimulationSystem
    {
        private readonly WorldGenerator generator;
        private readonly WorldLayoutValidator validator;
        private readonly IEventBus eventBus;
        private readonly Dictionary<string, BiomeDefinition> registeredBiomes = new Dictionary<string, BiomeDefinition>();
        private WorldState world;
        private WorldGenerationProfile profile;

        public Type SystemType => typeof(WorldManager);
        public string Name => nameof(WorldManager);
        public SimulationPhase Phase => SimulationPhase.PreSimulation;
        public int Priority => 50;
        public IReadOnlyList<Type> RunsAfter => Array.Empty<Type>();
        public IReadOnlyList<Type> RunsBefore => Array.Empty<Type>();
        public WorldDiagnostics Diagnostics { get; } = new WorldDiagnostics();

        public WorldManager(IEventBus eventBus = null)
            : this(new WorldGenerator(), new WorldLayoutValidator(), eventBus)
        {
        }

        public WorldManager(WorldGenerator generator, WorldLayoutValidator validator, IEventBus eventBus = null)
        {
            this.generator = generator;
            this.validator = validator;
            this.eventBus = eventBus;
        }

        public WorldState CreateWorld(WorldSeed seed, WorldGenerationProfile generationProfile)
        {
            profile = generationProfile ?? WorldGenerationProfile.CreateDefault(WorldGenerationProfileType.Standard);
            world = generator.CreateWorld(seed, profile);
            foreach (WorldRegion region in world.Regions.Values)
            {
                Diagnostics.RecordRegionGenerated();
                eventBus?.Publish(new RegionGenerated(region.RegionId));
            }

            Diagnostics.RecordWorldCreated(GetStatistics());
            eventBus?.Publish(new WorldCreated(world.WorldId));
            return world;
        }

        public WorldState LoadWorld(WorldState loadedWorld, WorldGenerationProfile generationProfile)
        {
            world = loadedWorld ?? throw new ArgumentNullException(nameof(loadedWorld));
            profile = generationProfile ?? WorldGenerationProfile.CreateDefault(world.ProfileType);
            foreach (WorldRegion region in world.Regions.Values)
            {
                Diagnostics.RecordRegionLoaded();
                eventBus?.Publish(new RegionLoaded(region.RegionId));
            }

            eventBus?.Publish(new WorldLoaded(world.WorldId));
            return world;
        }

        public WorldState QueryWorld()
        {
            EnsureWorld();
            return world;
        }

        public WorldState SaveWorld()
        {
            EnsureWorld();
            eventBus?.Publish(new WorldSaved(world.WorldId));
            return world;
        }

        public void RegisterBiome(BiomeDefinition biome)
        {
            if (biome == null)
            {
                throw new ArgumentNullException(nameof(biome));
            }

            registeredBiomes[biome.BiomeId] = biome;
        }

        public IReadOnlyDictionary<string, BiomeDefinition> QueryRegisteredBiomes()
        {
            return registeredBiomes;
        }

        public void RegisterRegion(WorldRegion region, WorldChunk chunk)
        {
            EnsureWorld();
            world.AddRegion(region, chunk);
            Diagnostics.RecordRegionLoaded();
            eventBus?.Publish(new RegionLoaded(region.RegionId));
        }


        public WorldRegion GenerateRegion(WorldChunkCoordinate coordinate)
        {
            EnsureWorld();
            WorldRegion region = generator.GenerateRegion(world.Seed, profile, coordinate);
            WorldChunk chunk = new WorldChunk(coordinate, profile.ChunkSize);
            world.AddRegion(region, chunk);
            Diagnostics.RecordRegionGenerated();
            eventBus?.Publish(new RegionGenerated(region.RegionId));
            return region;
        }

        public WorldRegion GetRegion(string regionId)
        {
            EnsureWorld();
            if (!world.TryGetRegion(regionId, out WorldRegion region))
            {
                throw new KeyNotFoundException($"Region {regionId} was not found.");
            }

            return region;
        }

        public WorldSeed GetSeed()
        {
            EnsureWorld();
            return world.Seed;
        }

        public WorldValidationResult ValidateWorld()
        {
            WorldValidationResult result = validator.Validate(world);
            Diagnostics.RecordValidation(result);
            eventBus?.Publish(new WorldValidated(result.IsValid));
            return result;
        }

        public WorldStatistics GetStatistics()
        {
            EnsureWorld();
            double richness = 0d;
            double difficulty = 0d;
            foreach (WorldRegion region in world.Regions.Values)
            {
                richness += region.Richness;
                difficulty += region.Difficulty;
            }

            int count = world.Regions.Count;
            return new WorldStatistics(count, world.Chunks.Count, count == 0 ? 0d : richness / count, count == 0 ? 0d : difficulty / count);
        }

        public void Execute(in SimulationExecutionContext context)
        {
        }

        private void EnsureWorld()
        {
            if (world == null)
            {
                throw new InvalidOperationException("World has not been created or loaded.");
            }
        }
    }
}
