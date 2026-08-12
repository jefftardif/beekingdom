using System;
using System.Collections.Generic;

namespace BeeKingdom.Hive
{
    public sealed class HiveAggregate
    {
        private readonly HashSet<string> beeIds = new HashSet<string>();
        private readonly HashSet<string> buildingIds = new HashSet<string>();
        private readonly HashSet<string> inventoryIds = new HashSet<string>();

        public string HiveId { get; }
        public string OwnerId { get; }
        public string QueenBeeId { get; }
        public HiveCapacity Capacity { get; private set; }
        public HiveExpansionMap ExpansionMap { get; } = new HiveExpansionMap();
        public HiveState State { get; private set; } = HiveState.Active;
        public IReadOnlyCollection<string> BeeIds => beeIds;
        public IReadOnlyCollection<string> BuildingIds => buildingIds;
        public IReadOnlyCollection<string> InventoryIds => inventoryIds;

        public HiveAggregate(string hiveId, string ownerId, string queenBeeId, HiveCapacity capacity)
        {
            HiveId = Require(hiveId, nameof(hiveId));
            OwnerId = Require(ownerId, nameof(ownerId));
            QueenBeeId = Require(queenBeeId, nameof(queenBeeId));
            Capacity = capacity;
            beeIds.Add(QueenBeeId);
        }

        public bool AddBee(string beeId)
        {
            Require(beeId, nameof(beeId));
            if (beeIds.Count >= Capacity.MaxPopulation)
            {
                return false;
            }

            return beeIds.Add(beeId);
        }

        public bool RemoveBee(string beeId)
        {
            Require(beeId, nameof(beeId));
            if (beeId == QueenBeeId)
            {
                return false;
            }

            return beeIds.Remove(beeId);
        }

        public bool RegisterBuilding(string buildingId)
        {
            Require(buildingId, nameof(buildingId));
            if (buildingIds.Count >= Capacity.MaxBuildings)
            {
                return false;
            }

            return buildingIds.Add(buildingId);
        }

        public bool RemoveBuilding(string buildingId)
        {
            Require(buildingId, nameof(buildingId));
            return buildingIds.Remove(buildingId);
        }

        public bool RegisterInventory(string inventoryId)
        {
            Require(inventoryId, nameof(inventoryId));
            if (inventoryIds.Count >= Capacity.MaxInventories)
            {
                return false;
            }

            return inventoryIds.Add(inventoryId);
        }

        public HiveStatistics GetStatistics()
        {
            HiveValidationResult validation = Validate();
            return new HiveStatistics(beeIds.Count, buildingIds.Count, inventoryIds.Count, Capacity, validation.IsValid);
        }

        public HiveValidationResult Validate()
        {
            List<HiveValidationIssue> issues = new List<HiveValidationIssue>();
            if (string.IsNullOrWhiteSpace(QueenBeeId))
            {
                issues.Add(new HiveValidationIssue("Hive must have exactly one queen."));
            }

            if (!beeIds.Contains(QueenBeeId))
            {
                issues.Add(new HiveValidationIssue("Queen must belong to the hive population."));
            }

            if (beeIds.Count > Capacity.MaxPopulation)
            {
                issues.Add(new HiveValidationIssue("Hive population exceeds capacity."));
            }

            if (buildingIds.Count > Capacity.MaxBuildings)
            {
                issues.Add(new HiveValidationIssue("Hive buildings exceed capacity."));
            }

            if (inventoryIds.Count > Capacity.MaxInventories)
            {
                issues.Add(new HiveValidationIssue("Hive inventories exceed capacity."));
            }

            State = issues.Count == 0 ? HiveState.Active : HiveState.Invalid;
            return new HiveValidationResult(issues);
        }

        public HiveSnapshot ToSnapshot()
        {
            return new HiveSnapshot
            {
                HiveId = HiveId,
                OwnerId = OwnerId,
                QueenBeeId = QueenBeeId,
                BeeIds = Copy(beeIds),
                BuildingIds = Copy(buildingIds),
                InventoryIds = Copy(inventoryIds),
                MaxPopulation = Capacity.MaxPopulation,
                MaxBuildings = Capacity.MaxBuildings,
                MaxInventories = Capacity.MaxInventories
            };
        }

        public static HiveAggregate FromSnapshot(HiveSnapshot snapshot)
        {
            HiveAggregate hive = new HiveAggregate(
                snapshot.HiveId,
                snapshot.OwnerId,
                snapshot.QueenBeeId,
                new HiveCapacity(snapshot.MaxPopulation, snapshot.MaxBuildings, snapshot.MaxInventories));

            AddAll(hive.beeIds, snapshot.BeeIds);
            AddAll(hive.buildingIds, snapshot.BuildingIds);
            AddAll(hive.inventoryIds, snapshot.InventoryIds);
            hive.Validate();
            return hive;
        }

        private static void AddAll(HashSet<string> target, string[] values)
        {
            if (values == null)
            {
                return;
            }

            for (int i = 0; i < values.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(values[i]))
                {
                    target.Add(values[i]);
                }
            }
        }

        private static string[] Copy(HashSet<string> values)
        {
            string[] copy = new string[values.Count];
            values.CopyTo(copy);
            return copy;
        }

        private static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value is required.", name);
            }

            return value;
        }
    }
}
