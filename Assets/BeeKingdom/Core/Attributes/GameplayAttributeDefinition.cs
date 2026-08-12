namespace BeeKingdom.Core.Attributes
{
    public sealed class GameplayAttributeDefinition
    {
        public string AttributeId { get; }
        public string Category { get; }
        public GameplayAttributeType AttributeType { get; }
        public double DefaultValue { get; }
        public double Minimum { get; }
        public double Maximum { get; }
        public bool IsVisible { get; }
        public bool IsNetworkSynced { get; }
        public bool IsPersistent { get; }
        public int Precision { get; }

        public GameplayAttributeDefinition(string attributeId, string category, GameplayAttributeType attributeType, double defaultValue, double minimum, double maximum, bool isVisible = true, bool isNetworkSynced = false, bool isPersistent = true, int precision = 2)
        {
            AttributeId = string.IsNullOrWhiteSpace(attributeId) ? throw new System.ArgumentException("Attribute id is required.", nameof(attributeId)) : attributeId;
            Category = string.IsNullOrWhiteSpace(category) ? "General" : category;
            AttributeType = attributeType;
            Minimum = minimum;
            Maximum = maximum < minimum ? minimum : maximum;
            DefaultValue = Clamp(defaultValue);
            IsVisible = isVisible;
            IsNetworkSynced = isNetworkSynced;
            IsPersistent = isPersistent;
            Precision = precision < 0 ? 0 : precision;
        }

        public double Clamp(double value)
        {
            if (value < Minimum) return Minimum;
            if (value > Maximum) return Maximum;
            return value;
        }
    }
}
