using BeeKingdom.Core.Events;

namespace BeeKingdom.Core.Modifiers
{
    public readonly struct ModifierAdded : IGameplayEvent { public string ModifierId { get; } public ModifierAdded(string modifierId) { ModifierId = modifierId; } }
    public readonly struct ModifierRemoved : IGameplayEvent { public string ModifierId { get; } public ModifierRemoved(string modifierId) { ModifierId = modifierId; } }
    public readonly struct ModifierEvaluated : IGameplayEvent { public string TargetKey { get; } public double Value { get; } public ModifierEvaluated(string targetKey, double value) { TargetKey = targetKey; Value = value; } }
    public readonly struct FormulaEvaluated : IGameplayEvent { public string Formula { get; } public double Value { get; } public FormulaEvaluated(string formula, double value) { Formula = formula; Value = value; } }
    public readonly struct FinalValueChanged : IGameplayEvent { public string TargetKey { get; } public double Value { get; } public FinalValueChanged(string targetKey, double value) { TargetKey = targetKey; Value = value; } }
}
