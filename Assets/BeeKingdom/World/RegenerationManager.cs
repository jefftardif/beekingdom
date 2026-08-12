using System;
using System.Collections.Generic;
using BeeKingdom.Core.Services;
using BeeKingdom.Core.Simulation;
using BeeKingdom.Economy;

namespace BeeKingdom.World
{
    public sealed class RegenerationManager : ISimulationSystem
    {
        private readonly Dictionary<string, NaturalResourceNode> nodes = new Dictionary<string, NaturalResourceNode>();
        private readonly Dictionary<string, EcologicalBalance> balanceByRegion = new Dictionary<string, EcologicalBalance>();
        private readonly IEventBus eventBus;

        public Type SystemType => typeof(RegenerationManager);
        public string Name => nameof(RegenerationManager);
        public SimulationPhase Phase => SimulationPhase.Simulation;
        public int Priority => 90;
        public IReadOnlyList<Type> RunsAfter => new[] { typeof(FlowerManager), typeof(WaterManager), typeof(WeatherManager) };
        public IReadOnlyList<Type> RunsBefore => Array.Empty<Type>();
        public RegenerationDiagnostics Diagnostics { get; } = new RegenerationDiagnostics();

        public RegenerationManager(IEventBus eventBus = null)
        {
            this.eventBus = eventBus;
        }

        public void RegisterNode(NaturalResourceNode node)
        {
            nodes[node.NodeId] = node;
            if (!balanceByRegion.ContainsKey(node.RegionId))
            {
                balanceByRegion[node.RegionId] = new EcologicalBalance();
            }

            Record();
        }

        public NaturalResourceNode GetNode(string nodeId)
        {
            return nodes[nodeId];
        }

        public IReadOnlyCollection<NaturalResourceNode> GetNodes()
        {
            return nodes.Values;
        }

        public void SetBalance(string regionId, EcologicalBalance balance)
        {
            balanceByRegion[regionId] = balance ?? new EcologicalBalance();
        }

        public double Harvest(string nodeId, double amount)
        {
            NaturalResourceNode node = GetNode(nodeId);
            double harvested = node.Harvest(amount);
            if (node.State == ResourceNodeState.Depleted)
            {
                eventBus?.Publish(new NaturalResourceDepleted(node.NodeId, node.ResourceType));
            }

            Record();
            return harvested;
        }

        public void SeedFromRegion(WorldRegion region, HexGrid grid)
        {
            IReadOnlyList<HexCoordinates> cells = grid.RegionIndex.GetCells(region.RegionId);
            int index = 0;
            foreach (var pair in region.Resources)
            {
                if (!TryMapResource(pair.Key, out ResourceType type))
                {
                    continue;
                }

                HexCoordinates coordinates = cells.Count > 0 ? cells[index % cells.Count] : new HexCoordinates(region.Coordinate.X, region.Coordinate.Y);
                double capacity = Math.Max(1d, pair.Value);
                RegisterNode(new NaturalResourceNode(region.RegionId + "-node-" + pair.Key, region.RegionId, coordinates, type, capacity, capacity * 0.5d, new ResourceNodeLifecycle(Math.Max(0.01d, capacity / 86400d), 0.25d)));
                index++;
            }

            SetBalance(region.RegionId, new EcologicalBalance(1d, 1d, region.Richness));
        }

        public void Execute(in SimulationExecutionContext context)
        {
            foreach (NaturalResourceNode node in nodes.Values)
            {
                EcologicalBalance balance = balanceByRegion.TryGetValue(node.RegionId, out EcologicalBalance value) ? value : new EcologicalBalance();
                if (node.Regenerate(context.DeltaSeconds, balance))
                {
                    Diagnostics.RecordRegenerated();
                    eventBus?.Publish(new NaturalResourceRegenerated(node.NodeId));
                }
            }

            Record();
        }

        private static bool TryMapResource(string key, out ResourceType resourceType)
        {
            switch (key)
            {
                case "nectar": resourceType = ResourceType.Nectar; return true;
                case "pollen": resourceType = ResourceType.Pollen; return true;
                case "water": resourceType = ResourceType.Water; return true;
                default: resourceType = ResourceType.Nectar; return false;
            }
        }

        private void Record()
        {
            int available = 0;
            int depleted = 0;
            foreach (NaturalResourceNode node in nodes.Values)
            {
                if (node.State == ResourceNodeState.Available) available++;
                if (node.State == ResourceNodeState.Depleted) depleted++;
            }

            Diagnostics.Record(nodes.Count, available, depleted);
        }
    }
}
