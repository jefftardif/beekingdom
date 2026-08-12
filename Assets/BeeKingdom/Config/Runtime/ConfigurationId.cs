using System;

namespace BeeKingdom.Config.Runtime
{
    [Serializable]
    public readonly struct ConfigurationId : IEquatable<ConfigurationId>
    {
        public string Value { get; }

        public ConfigurationId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Configuration id is required.", nameof(value));
            }

            Value = value.Trim();
        }

        public bool Equals(ConfigurationId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ConfigurationId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
    }
}
