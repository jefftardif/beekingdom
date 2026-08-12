using System;

namespace BeeKingdom.Core.Abilities
{
    public readonly struct GameplayAbilityHandle : IEquatable<GameplayAbilityHandle>
    {
        public long Value { get; }
        public bool IsValid => Value > 0L;

        public GameplayAbilityHandle(long value)
        {
            Value = value;
        }

        public bool Equals(GameplayAbilityHandle other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is GameplayAbilityHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
