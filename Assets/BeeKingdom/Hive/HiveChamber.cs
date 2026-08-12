using System;
using System.Collections.Generic;

namespace BeeKingdom.Hive
{
    public sealed class HiveChamber
    {
        private readonly HashSet<string> connections = new HashSet<string>();
        private readonly List<string> cellIds = new List<string>();

        public string ChamberId { get; }
        public HiveChamberType ChamberType { get; }
        public HivePosition Position { get; }
        public HiveElementFunction Function { get; private set; }
        public int Level { get; private set; }
        public double Integrity { get; private set; }
        public int Capacity { get; private set; }
        public IReadOnlyCollection<string> Connections => connections;
        public IReadOnlyList<string> CellIds => cellIds;

        public HiveChamber(string chamberId, HiveChamberType chamberType, HivePosition position, int capacity = 6)
        {
            ChamberId = string.IsNullOrWhiteSpace(chamberId) ? Guid.NewGuid().ToString("N") : chamberId;
            ChamberType = chamberType;
            Position = position;
            Function = MapFunction(chamberType);
            Level = 1;
            Integrity = 1d;
            Capacity = capacity < 1 ? 1 : capacity;
        }

        public bool Connect(string otherChamberId)
        {
            return !string.IsNullOrWhiteSpace(otherChamberId) && otherChamberId != ChamberId && connections.Add(otherChamberId);
        }

        public bool AddCell(string cellId)
        {
            if (string.IsNullOrWhiteSpace(cellId) || cellIds.Count >= Capacity || cellIds.Contains(cellId))
            {
                return false;
            }

            cellIds.Add(cellId);
            return true;
        }

        public void Upgrade()
        {
            Level++;
            Capacity += Math.Max(1, Capacity / 2);
            Integrity = 1d;
        }

        public void Damage(double amount)
        {
            if (amount > 0d)
            {
                Integrity = Math.Max(0d, Integrity - amount);
            }
        }

        private static HiveElementFunction MapFunction(HiveChamberType type)
        {
            switch (type)
            {
                case HiveChamberType.Nursery: return HiveElementFunction.Brood;
                case HiveChamberType.HoneyStorage: return HiveElementFunction.HoneyStorage;
                case HiveChamberType.PollenStorage: return HiveElementFunction.PollenStorage;
                case HiveChamberType.RoyalChamber: return HiveElementFunction.Royal;
                case HiveChamberType.WaxWorkshop: return HiveElementFunction.WaxProduction;
                case HiveChamberType.Entrance: return HiveElementFunction.Entrance;
                case HiveChamberType.Defense: return HiveElementFunction.Defense;
                default: return HiveElementFunction.General;
            }
        }
    }
}
