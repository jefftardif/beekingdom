using System;

namespace BeeKingdom.Core.Effects
{
    public readonly struct GameplayEffectHandle : IEquatable<GameplayEffectHandle>
    {
        public long Value { get; }
        public bool IsValid => Value > 0L;

        public GameplayEffectHandle(long value)
        {
            Value = value;
        }

        public bool Equals(GameplayEffectHandle other) => Value == other.Value;
        public override bool Equals(object obj) => obj is GameplayEffectHandle other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
    }
}
