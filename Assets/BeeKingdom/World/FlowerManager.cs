using System;
using System.Collections.Generic;
using BeeKingdom.Core.Services;
using BeeKingdom.Core.Simulation;
using BeeKingdom.Core.Time;

namespace BeeKingdom.World
{
    public sealed class FlowerManager : ISimulationSystem
    {
        private readonly Dictionary<string, FlowerSpecies> speciesById = new Dictionary<string, FlowerSpecies>();
        private readonly Dictionary<string, FlowerPatch> patchesById = new Dictionary<string, FlowerPatch>();
        private readonly IEventBus eventBus;
        private SimulationSeason currentSeason = SimulationSeason.Spring;
        private WorldWeather currentWeather = WorldWeather.Clear;

        public Type SystemType => typeof(FlowerManager);
        public string Name => nameof(FlowerManager);
        public SimulationPhase Phase => SimulationPhase.Simulation;
        public int Priority => 80;
        public IReadOnlyList<Type> RunsAfter => new[] { typeof(WorldManager) };
        public IReadOnlyList<Type> RunsBefore => Array.Empty<Type>();
        public FlowerDiagnostics Diagnostics { get; } = new FlowerDiagnostics();

        public FlowerManager(IEventBus eventBus = null)
        {
            this.eventBus = eventBus;
        }

        public void RegisterSpecies(FlowerSpecies species)
        {
            speciesById[species.SpeciesId] = species;
        }

        public FlowerPatch CreatePatch(string patchId, string speciesId, string regionId, HexCoordinates coordinates)
        {
            FlowerPatch patch = new FlowerPatch(patchId, regionId, coordinates, speciesById[speciesId]);
            patchesById[patch.PatchId] = patch;
            Record();
            return patch;
        }

        public FlowerPatch GetPatch(string patchId)
        {
            return patchesById[patchId];
        }

        public IReadOnlyCollection<FlowerPatch> GetPatches()
        {
            return patchesById.Values;
        }

        public void SetEnvironment(SimulationSeason season, WorldWeather weather)
        {
            currentSeason = season;
            currentWeather = weather;
        }

        public FlowerHarvestResult Harvest(string patchId, double nectarAmount, double pollenAmount)
        {
            FlowerPatch patch = GetPatch(patchId);
            FlowerHarvestResult result = patch.Harvest(nectarAmount, pollenAmount);
            if (result.IsDepleted)
            {
                Diagnostics.RecordDepleted();
                eventBus?.Publish(new FlowerDepleted(patchId));
            }

            return result;
        }

        public void SeedFromRegion(WorldRegion region, HexGrid grid)
        {
            foreach (string speciesId in region.FloralSpecies)
            {
                if (!speciesById.ContainsKey(speciesId))
                {
                    RegisterSpecies(new FlowerSpecies(speciesId, speciesId, 10d * region.Richness, 6d * region.Richness, BloomCycle.CreateDefault(), PollinationRules.CreateDefault()));
                }
            }

            IReadOnlyList<HexCoordinates> cells = grid.RegionIndex.GetCells(region.RegionId);
            int index = 0;
            foreach (HexCoordinates cell in cells)
            {
                string speciesId = region.FloralSpecies[index % region.FloralSpecies.Count];
                CreatePatch(region.RegionId + "-flower-" + index, speciesId, region.RegionId, cell);
                index++;
            }
        }

        public void Execute(in SimulationExecutionContext context)
        {
            foreach (FlowerPatch patch in patchesById.Values)
            {
                if (patch.Advance(context.DeltaSeconds, currentSeason, currentWeather))
                {
                    eventBus?.Publish(new FlowerBloomed(patch.PatchId));
                }
            }

            Record();
        }

        private void Record()
        {
            int blooming = 0;
            foreach (FlowerPatch patch in patchesById.Values)
            {
                if (patch.Stage == FlowerGrowthStage.Blooming)
                {
                    blooming++;
                }
            }

            Diagnostics.Record(patchesById.Count, blooming);
        }
    }
}
