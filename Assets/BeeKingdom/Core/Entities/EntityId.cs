using System;

namespace BeeKingdom.Core.Entities
{
    public readonly struct EntityId : IEquatable<EntityId>
    {
        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);
        public EntityId(string value) { Value = value ?? string.Empty; }
        public bool Equals(EntityId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is EntityId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : Value.GetHashCode();
        public override string ToString() => Value;
    }
}
