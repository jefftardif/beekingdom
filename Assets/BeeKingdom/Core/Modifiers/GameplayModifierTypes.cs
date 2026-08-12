namespace BeeKingdom.Core.Modifiers
{
    public enum GameplayModifierOperation
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Override,
        Clamp,
        Minimum,
        Maximum,
        Curve,
        Formula
    }

    public enum ModifierStackingRule
    {
        Additive,
        Multiplicative,
        HighestOnly,
        LowestOnly,
        Replace,
        RefreshDuration,
        ExtendDuration,
        IgnoreDuplicate,
        ExclusiveGroup
    }

    public enum ModifierConditionOperator
    {
        And,
        Or,
        Not
    }
}
