using System;

namespace BeeKingdom.Hive
{
    public enum HiveChamberType
    {
        Nursery,
        HoneyStorage,
        PollenStorage,
        RoyalChamber,
        WaxWorkshop,
        Entrance,
        Defense,
        Utility
    }

    public enum HiveElementFunction
    {
        General,
        Brood,
        HoneyStorage,
        PollenStorage,
        Royal,
        WaxProduction,
        Entrance,
        Defense,
        Transit
    }

    public enum HoneycombCellState
    {
        Planned,
        Building,
        Complete,
        Damaged,
        Disabled
    }

    public enum ConstructionSiteState
    {
        Planned,
        Reserved,
        UnderConstruction,
        Completed,
        Upgradeable
    }

    public readonly struct HivePosition : IEquatable<HivePosition>
    {
        public int X { get; }
        public int Y { get; }
        public int Layer { get; }

        public HivePosition(int x, int y, int layer = 0)
        {
            X = x;
            Y = y;
            Layer = layer;
        }

        public int ManhattanDistance(HivePosition other)
        {
            return Math.Abs(X - other.X) + Math.Abs(Y - other.Y) + Math.Abs(Layer - other.Layer);
        }

        public bool Equals(HivePosition other)
        {
            return X == other.X && Y == other.Y && Layer == other.Layer;
        }

        public override bool Equals(object obj)
        {
            return obj is HivePosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = X;
                hash = (hash * 397) ^ Y;
                hash = (hash * 397) ^ Layer;
                return hash;
            }
        }
    }
}
