using System;
using System.Collections.Generic;
using BeeKingdom.Core.Services;
using BeeKingdom.Core.Simulation;
using BeeKingdom.Core.Time;
using BeeKingdom.Economy;

namespace BeeKingdom.World
{
    public sealed class WaterManager : ISimulationSystem
    {
        private readonly Dictionary<string, WaterSource> sources = new Dictionary<string, WaterSource>();
        private readonly Dictionary<string, HydrationDemand> demands = new Dictionary<string, HydrationDemand>();
        private readonly ResourceFlowManager resourceFlow;
        private readonly IEventBus eventBus;
        private SimulationSeason season = SimulationSeason.Spring;
        private WorldWeather weather = WorldWeather.Clear;

        public Type SystemType => typeof(WaterManager);
        public string Name => nameof(WaterManager);
        public SimulationPhase Phase => SimulationPhase.Simulation;
        public int Priority => 85;
        public IReadOnlyList<Type> RunsAfter => new[] { typeof(WorldManager) };
        public IReadOnlyList<Type> RunsBefore => Array.Empty<Type>();
        public WaterDiagnostics Diagnostics { get; } = new WaterDiagnostics();

        public WaterManager(ResourceFlowManager resourceFlow = null, IEventBus eventBus = null)
        {
            this.resourceFlow = resourceFlow;
            this.eventBus = eventBus;
        }

        public void RegisterSource(WaterSource source)
        {
            sources[source.SourceId] = source;
            Record();
        }

        public WaterSource GetSource(string sourceId)
        {
            return sources[sourceId];
        }

        public IReadOnlyCollection<WaterSource> GetSources()
        {
            return sources.Values;
        }

        public void SetEnvironment(SimulationSeason currentSeason, WorldWeather currentWeather)
        {
            season = currentSeason;
            weather = currentWeather;
        }

        public void SetDemand(HydrationDemand demand)
        {
            demands[demand.HiveId] = demand;
            eventBus?.Publish(new HydrationDemandUpdated(demand.HiveId, demand.DailyDemand));
        }

        public double CollectWater(string sourceId, string destinationStorageId, double amount, double nowSeconds)
        {
            WaterSource source = GetSource(sourceId);
            double collected = source.Collect(amount);
            if (collected <= 0d)
            {
                Diagnostics.RecordDepleted();
                eventBus?.Publish(new WaterSourceDepleted(sourceId));
                return 0d;
            }

            resourceFlow?.Produce(sourceId, destinationStorageId, ResourceType.Water, collected, nowSeconds);
            Diagnostics.RecordTransport(collected);
            eventBus?.Publish(new WaterCollected(sourceId, collected));
            if (source.AvailableAmount <= 0d)
            {
                Diagnostics.RecordDepleted();
                eventBus?.Publish(new WaterSourceDepleted(sourceId));
            }

            Record();
            return collected;
        }

        public double GetDemandForSeconds(string hiveId, double seconds)
        {
            return demands.TryGetValue(hiveId, out HydrationDemand demand) ? demand.DemandForSeconds(seconds) : 0d;
        }

        public void SeedFromRegion(WorldRegion region, HexGrid grid)
        {
            if (!region.Resources.TryGetValue("water", out double water) || water <= 0d)
            {
                return;
            }

            IReadOnlyList<HexCoordinates> cells = grid.RegionIndex.GetCells(region.RegionId);
            HexCoordinates coordinates = cells.Count > 0 ? cells[0] : new HexCoordinates(region.Coordinate.X, region.Coordinate.Y);
            WaterSourceType type = region.BiomeType == WorldBiomeType.River ? WaterSourceType.River :
                region.BiomeType == WorldBiomeType.Marsh ? WaterSourceType.Pond :
                WaterSourceType.Dew;
            WaterQuality quality = region.BiomeType == WorldBiomeType.Marsh ? WaterQuality.Stagnant : WaterQuality.Clean;
            RegisterSource(new WaterSource(region.RegionId + "-water", region.RegionId, coordinates, type, quality, water, water * 0.5d, Math.Max(0.01d, water / 86400d)));
        }

        public void Execute(in SimulationExecutionContext context)
        {
            foreach (WaterSource source in sources.Values)
            {
                source.Recharge(context.DeltaSeconds, season, weather);
            }

            Record();
        }

        private void Record()
        {
            double total = 0d;
            foreach (WaterSource source in sources.Values)
            {
                total += source.AvailableAmount;
            }

            Diagnostics.RecordSources(sources.Count, total);
        }
    }
}
