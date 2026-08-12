using System;
using System.Collections.Generic;

namespace BeeKingdom.World
{
    public enum WorldGenerationProfileType
    {
        Tutorial,
        Standard,
        Rich,
        Harsh,
        Custom
    }

    public enum WorldBiomeType
    {
        Prairie,
        Forest,
        Mountain,
        River,
        Marsh,
        FlowerFields,
        Wetland,
        Meadow,
        Orchard,
        Farmland,
        Urban,
        SpecialEventArea
    }

    public enum WorldClimate
    {
        Temperate,
        Humid,
        Dry,
        Cold,
        Variable
    }

    public enum WorldWeather
    {
        Clear,
        Cloudy,
        Rain,
        Wind,
        Storm
    }

    public readonly struct WorldSeed : IEquatable<WorldSeed>
    {
        public string Value { get; }
        public int Hash { get; }

        public WorldSeed(string value)
        {
            Value = string.IsNullOrWhiteSpace(value) ? "bee-kingdom" : value;
            Hash = StableHash(Value);
        }

        public bool Equals(WorldSeed other)
        {
            return Hash == other.Hash && Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is WorldSeed other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Hash;
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                const int offset = (int)2166136261;
                const int prime = 16777619;
                int hash = offset;
                for (int i = 0; i < value.Length; i++)
                {
                    hash ^= value[i];
                    hash *= prime;
                }

                return hash;
            }
        }
    }

    public readonly struct WorldChunkCoordinate : IEquatable<WorldChunkCoordinate>
    {
        public int X { get; }
        public int Y { get; }

        public WorldChunkCoordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(WorldChunkCoordinate other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is WorldChunkCoordinate other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }
    }

    public sealed class WorldResourceProfile
    {
        private readonly Dictionary<string, double> resources;

        public IReadOnlyDictionary<string, double> Resources => resources;

        public WorldResourceProfile(IReadOnlyDictionary<string, double> resources)
        {
            this.resources = new Dictionary<string, double>(resources ?? new Dictionary<string, double>());
        }
    }
}
