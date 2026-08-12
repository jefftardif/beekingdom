using System;
using BeeKingdom.Gameplay.Domain.Enums;
using BeeKingdom.Gameplay.Domain.Identifiers;

namespace BeeKingdom.Gameplay.Domain.ValueObjects
{
    [Serializable]
    public readonly struct Position2D : IEquatable<Position2D>
    {
        public float X { get; }
        public float Y { get; }
        public Position2D(float x, float y) { X = x; Y = y; }
        public bool Equals(Position2D other) => X.Equals(other.X) && Y.Equals(other.Y);
        public override bool Equals(object obj) => obj is Position2D other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y);
    }

    [Serializable]
    public readonly struct WorldCoordinate : IEquatable<WorldCoordinate>
    {
        public RegionId RegionId { get; }
        public Position2D Position { get; }
        public WorldCoordinate(RegionId regionId, Position2D position) { RegionId = regionId; Position = position; }
        public bool Equals(WorldCoordinate other) => RegionId.Equals(other.RegionId) && Position.Equals(other.Position);
        public override bool Equals(object obj) => obj is WorldCoordinate other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(RegionId, Position);
    }

    [Serializable]
    public readonly struct ResourceAmount : IEquatable<ResourceAmount>
    {
        public ResourceType Type { get; }
        public long Amount { get; }
        public ResourceAmount(ResourceType type, long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Resource amount cannot be negative.");
            Type = type;
            Amount = amount;
        }
        public bool Equals(ResourceAmount other) => Type == other.Type && Amount == other.Amount;
        public override bool Equals(object obj) => obj is ResourceAmount other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Type, Amount);
    }

    [Serializable]
    public readonly struct Health : IEquatable<Health>
    {
        public int Current { get; }
        public int Maximum { get; }
        public Health(int current, int maximum)
        {
            if (maximum <= 0) throw new ArgumentOutOfRangeException(nameof(maximum), "Maximum health must be positive.");
            if (current < 0 || current > maximum) throw new ArgumentOutOfRangeException(nameof(current), "Current health must be within 0..Maximum.");
            Current = current;
            Maximum = maximum;
        }
        public bool Equals(Health other) => Current == other.Current && Maximum == other.Maximum;
        public override bool Equals(object obj) => obj is Health other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Current, Maximum);
    }

    [Serializable]
    public readonly struct Energy : IEquatable<Energy>
    {
        public int Current { get; }
        public int Maximum { get; }
        public Energy(int current, int maximum)
        {
            if (maximum <= 0) throw new ArgumentOutOfRangeException(nameof(maximum), "Maximum energy must be positive.");
            if (current < 0 || current > maximum) throw new ArgumentOutOfRangeException(nameof(current), "Current energy must be within 0..Maximum.");
            Current = current;
            Maximum = maximum;
        }
        public bool Equals(Energy other) => Current == other.Current && Maximum == other.Maximum;
        public override bool Equals(object obj) => obj is Energy other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Current, Maximum);
    }

    [Serializable]
    public readonly struct SimulationTime : IEquatable<SimulationTime>
    {
        public long Ticks { get; }
        public SimulationTime(long ticks)
        {
            if (ticks < 0) throw new ArgumentOutOfRangeException(nameof(ticks), "Simulation time cannot be negative.");
            Ticks = ticks;
        }
        public bool Equals(SimulationTime other) => Ticks == other.Ticks;
        public override bool Equals(object obj) => obj is SimulationTime other && Equals(other);
        public override int GetHashCode() => Ticks.GetHashCode();
    }
}
