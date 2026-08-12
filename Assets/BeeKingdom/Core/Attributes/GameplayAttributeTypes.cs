namespace BeeKingdom.Core.Attributes
{
    public enum GameplayAttributeType
    {
        Integer,
        Float,
        Boolean,
        Enum,
        Percentage,
        Curve,
        Calculated
    }

    public enum GameplayAttributeState
    {
        Registered,
        Initialized,
        Active,
        Modified,
        Recalculated,
        Serialized,
        Restored
    }
}
