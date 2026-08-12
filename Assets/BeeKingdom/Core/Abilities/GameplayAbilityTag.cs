using System;

namespace BeeKingdom.Core.Abilities
{
    public readonly struct GameplayAbilityTag : IEquatable<GameplayAbilityTag>
    {
        public string Value { get; }

        public GameplayAbilityTag(string value)
        {
            Value = string.IsNullOrWhiteSpace(value) ? string.Empty : value;
        }

        public bool IsChildOf(GameplayAbilityTag parent)
        {
            return !string.IsNullOrWhiteSpace(parent.Value) &&
                (Value == parent.Value || Value.StartsWith(parent.Value + ".", StringComparison.Ordinal));
        }

        public bool Equals(GameplayAbilityTag other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is GameplayAbilityTag other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value == null ? 0 : Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
