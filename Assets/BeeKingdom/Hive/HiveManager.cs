using System;
using System.Collections.Generic;
using BeeKingdom.Core.Services;
using BeeKingdom.Core.Simulation;

namespace BeeKingdom.Hive
{
    public sealed class HiveManager : ISimulationSystem
    {
        private static readonly Type[] EmptyDependencies = Array.Empty<Type>();
        private readonly Dictionary<string, HiveAggregate> hives = new Dictionary<string, HiveAggregate>();
        private readonly Dictionary<string, string> beeToHive = new Dictionary<string, string>();
        private readonly Dictionary<string, string> buildingToHive = new Dictionary<string, string>();
        private readonly IEventBus eventBus;

        public Type SystemType => typeof(HiveManager);
        public string Name => nameof(HiveManager);
        public SimulationPhase Phase => SimulationPhase.Simulation;
        public int Priority => 100;
        public IReadOnlyList<Type> RunsAfter => EmptyDependencies;
        public IReadOnlyList<Type> RunsBefore => EmptyDependencies;
        public HiveDiagnostics Diagnostics { get; } = new HiveDiagnostics();

        public HiveManager(IEventBus eventBus = null)
        {
            this.eventBus = eventBus;
        }

        public HiveAggregate CreateHive(string hiveId, string ownerId, string queenBeeId, HiveCapacity capacity)
        {
            HiveAggregate hive = new HiveAggregate(hiveId, ownerId, queenBeeId, capacity);
            hives.Add(hive.HiveId, hive);
            beeToHive.Add(queenBeeId, hive.HiveId);
            eventBus?.Publish(new HiveCreated(hive.HiveId));
            Record(hive);
            return hive;
        }

        public HiveAggregate LoadHive(HiveSnapshot snapshot)
        {
            HiveAggregate hive = HiveAggregate.FromSnapshot(snapshot);
            hives[hive.HiveId] = hive;
            foreach (string beeId in hive.BeeIds)
            {
                beeToHive[beeId] = hive.HiveId;
            }

            foreach (string buildingId in hive.BuildingIds)
            {
                buildingToHive[buildingId] = hive.HiveId;
            }

            eventBus?.Publish(new HiveLoaded(hive.HiveId));
            Record(hive);
            return hive;
        }

        public bool AddBee(string hiveId, string beeId)
        {
            HiveAggregate hive = GetHive(hiveId);
            if (beeToHive.ContainsKey(beeId) || !hive.AddBee(beeId))
            {
                return false;
            }

            beeToHive[beeId] = hiveId;
            eventBus?.Publish(new BeeAdded(hiveId, beeId));
            Record(hive);
            return true;
        }

        public bool RemoveBee(string hiveId, string beeId)
        {
            HiveAggregate hive = GetHive(hiveId);
            if (!hive.RemoveBee(beeId))
            {
                return false;
            }

            beeToHive.Remove(beeId);
            eventBus?.Publish(new BeeRemoved(hiveId, beeId));
            Record(hive);
            return true;
        }

        public bool RegisterBuilding(string hiveId, string buildingId)
        {
            HiveAggregate hive = GetHive(hiveId);
            if (buildingToHive.ContainsKey(buildingId) || !hive.RegisterBuilding(buildingId))
            {
                return false;
            }

            buildingToHive[buildingId] = hiveId;
            eventBus?.Publish(new BuildingRegistered(hiveId, buildingId));
            Record(hive);
            return true;
        }

        public bool RemoveBuilding(string hiveId, string buildingId)
        {
            HiveAggregate hive = GetHive(hiveId);
            if (!hive.RemoveBuilding(buildingId))
            {
                return false;
            }

            buildingToHive.Remove(buildingId);
            Record(hive);
            return true;
        }

        public HiveStatistics GetStatistics(string hiveId)
        {
            return GetHive(hiveId).GetStatistics();
        }

        public HiveValidationResult Validate(string hiveId)
        {
            HiveAggregate hive = GetHive(hiveId);
            HiveValidationResult result = hive.Validate();
            eventBus?.Publish(new HiveValidated(hiveId, result.IsValid));
            Record(hive);
            return result;
        }

        public void Execute(in SimulationExecutionContext context)
        {
        }

        private HiveAggregate GetHive(string hiveId)
        {
            if (hives.TryGetValue(hiveId, out HiveAggregate hive))
            {
                return hive;
            }

            throw new KeyNotFoundException($"Hive {hiveId} was not found.");
        }

        private void Record(HiveAggregate hive)
        {
            HiveValidationResult validation = hive.Validate();
            Diagnostics.Record(hive.GetStatistics(), validation.Issues.Count);
        }
    }
}
