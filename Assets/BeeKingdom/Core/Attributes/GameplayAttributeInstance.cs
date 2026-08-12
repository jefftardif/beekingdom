namespace BeeKingdom.Core.Attributes
{
    public sealed class GameplayAttributeInstance
    {
        public GameplayAttributeDefinition Definition { get; }
        public double BaseValue { get; private set; }
        public double FinalValue { get; private set; }
        public GameplayAttributeState State { get; private set; }

        public GameplayAttributeInstance(GameplayAttributeDefinition definition)
        {
            Definition = definition;
            BaseValue = definition.DefaultValue;
            FinalValue = definition.DefaultValue;
            State = GameplayAttributeState.Initialized;
        }

        public bool SetBaseValue(double value, out bool clamped)
        {
            double next = Definition.Clamp(value);
            clamped = next != value;
            bool changed = next != BaseValue;
            BaseValue = next;
            if (changed) State = GameplayAttributeState.Modified;
            return changed;
        }

        public bool SetFinalValue(double value, out bool clamped)
        {
            double next = Definition.Clamp(value);
            clamped = next != value;
            bool changed = next != FinalValue;
            FinalValue = next;
            State = GameplayAttributeState.Recalculated;
            return changed;
        }

        public void Restore(double baseValue, double finalValue)
        {
            BaseValue = Definition.Clamp(baseValue);
            FinalValue = Definition.Clamp(finalValue);
            State = GameplayAttributeState.Restored;
        }
    }
}
