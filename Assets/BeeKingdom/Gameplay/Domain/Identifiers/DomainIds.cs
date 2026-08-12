using System;

namespace BeeKingdom.Gameplay.Domain.Identifiers
{
    [Serializable]
    public readonly struct PlayerId : IEquatable<PlayerId>
    {
        public string Value { get; }
        public PlayerId(string value) { Value = RequireValue(value); }
        public static PlayerId New() => new PlayerId(System.Guid.NewGuid().ToString("N"));
        public bool Equals(PlayerId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is PlayerId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value;
        private static string RequireValue(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Identifier value is required.", nameof(value)) : value;
    }

    [Serializable]
    public readonly struct HiveId : IEquatable<HiveId>
    {
        public string Value { get; }
        public HiveId(string value) { Value = RequireValue(value); }
        public static HiveId New() => new HiveId(System.Guid.NewGuid().ToString("N"));
        public bool Equals(HiveId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is HiveId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value;
        private static string RequireValue(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Identifier value is required.", nameof(value)) : value;
    }

    [Serializable]
    public readonly struct BeeId : IEquatable<BeeId>
    {
        public string Value { get; }
        public BeeId(string value) { Value = RequireValue(value); }
        public static BeeId New() => new BeeId(System.Guid.NewGuid().ToString("N"));
        public bool Equals(BeeId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is BeeId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value;
        private static string RequireValue(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Identifier value is required.", nameof(value)) : value;
    }

    [Serializable]
    public readonly struct BuildingId : IEquatable<BuildingId>
    {
        public string Value { get; }
        public BuildingId(string value) { Value = RequireValue(value); }
        public static BuildingId New() => new BuildingId(System.Guid.NewGuid().ToString("N"));
        public bool Equals(BuildingId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is BuildingId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value;
        private static string RequireValue(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Identifier value is required.", nameof(value)) : value;
    }

    [Serializable]
    public readonly struct RegionId : IEquatable<RegionId>
    {
        public string Value { get; }
        public RegionId(string value) { Value = RequireValue(value); }
        public static RegionId New() => new RegionId(System.Guid.NewGuid().ToString("N"));
        public bool Equals(RegionId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is RegionId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value;
        private static string RequireValue(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Identifier value is required.", nameof(value)) : value;
    }

    [Serializable]
    public readonly struct AllianceId : IEquatable<AllianceId>
    {
        public string Value { get; }
        public AllianceId(string value) { Value = RequireValue(value); }
        public static AllianceId New() => new AllianceId(System.Guid.NewGuid().ToString("N"));
        public bool Equals(AllianceId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is AllianceId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value;
        private static string RequireValue(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Identifier value is required.", nameof(value)) : value;
    }

    [Serializable]
    public readonly struct ResearchId : IEquatable<ResearchId>
    {
        public string Value { get; }
        public ResearchId(string value) { Value = RequireValue(value); }
        public static ResearchId New() => new ResearchId(System.Guid.NewGuid().ToString("N"));
        public bool Equals(ResearchId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is ResearchId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value;
        private static string RequireValue(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Identifier value is required.", nameof(value)) : value;
    }

    [Serializable]
    public readonly struct InventoryId : IEquatable<InventoryId>
    {
        public string Value { get; }
        public InventoryId(string value) { Value = RequireValue(value); }
        public static InventoryId New() => new InventoryId(System.Guid.NewGuid().ToString("N"));
        public bool Equals(InventoryId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is InventoryId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value;
        private static string RequireValue(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Identifier value is required.", nameof(value)) : value;
    }

    [Serializable]
    public readonly struct ResourceStackId : IEquatable<ResourceStackId>
    {
        public string Value { get; }
        public ResourceStackId(string value) { Value = RequireValue(value); }
        public static ResourceStackId New() => new ResourceStackId(System.Guid.NewGuid().ToString("N"));
        public bool Equals(ResourceStackId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is ResourceStackId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value;
        private static string RequireValue(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Identifier value is required.", nameof(value)) : value;
    }

    [Serializable]
    public readonly struct FlowerNodeId : IEquatable<FlowerNodeId>
    {
        public string Value { get; }
        public FlowerNodeId(string value) { Value = RequireValue(value); }
        public static FlowerNodeId New() => new FlowerNodeId(System.Guid.NewGuid().ToString("N"));
        public bool Equals(FlowerNodeId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is FlowerNodeId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value;
        private static string RequireValue(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Identifier value is required.", nameof(value)) : value;
    }

    [Serializable]
    public readonly struct ArmyId : IEquatable<ArmyId>
    {
        public string Value { get; }
        public ArmyId(string value) { Value = RequireValue(value); }
        public static ArmyId New() => new ArmyId(System.Guid.NewGuid().ToString("N"));
        public bool Equals(ArmyId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is ArmyId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value;
        private static string RequireValue(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Identifier value is required.", nameof(value)) : value;
    }
}
