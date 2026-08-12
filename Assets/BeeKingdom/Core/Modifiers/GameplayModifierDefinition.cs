using System.Collections.Generic;
using BeeKingdom.Core.Abilities;

namespace BeeKingdom.Core.Modifiers
{
    public sealed class GameplayModifierDefinition
    {
        private readonly List<GameplayAbilityTag> requiredTags;
        private readonly Dictionary<string, string> requiredParameters;

        public string ModifierId { get; }
        public string TargetKey { get; }
        public GameplayModifierOperation Operation { get; }
        public ModifierStackingRule StackingRule { get; }
        public ModifierConditionOperator ConditionOperator { get; }
        public int Priority { get; }
        public double Value { get; }
        public double MinValue { get; }
        public double MaxValue { get; }
        public string Formula { get; }
        public string ExclusiveGroup { get; }
        public IReadOnlyList<GameplayAbilityTag> RequiredTags => requiredTags;
        public IReadOnlyDictionary<string, string> RequiredParameters => requiredParameters;

        public GameplayModifierDefinition(string modifierId, string targetKey, GameplayModifierOperation operation, double value, int priority = 0, ModifierStackingRule stackingRule = ModifierStackingRule.Additive, string formula = "", double minValue = double.MinValue, double maxValue = double.MaxValue, string exclusiveGroup = "", IReadOnlyList<GameplayAbilityTag> requiredTags = null, IReadOnlyDictionary<string, string> requiredParameters = null, ModifierConditionOperator conditionOperator = ModifierConditionOperator.And)
        {
            ModifierId = string.IsNullOrWhiteSpace(modifierId) ? throw new System.ArgumentException("Modifier id is required.", nameof(modifierId)) : modifierId;
            TargetKey = string.IsNullOrWhiteSpace(targetKey) ? "value" : targetKey;
            Operation = operation;
            Value = value;
            Priority = priority;
            StackingRule = stackingRule;
            Formula = formula ?? string.Empty;
            MinValue = minValue;
            MaxValue = maxValue;
            ExclusiveGroup = exclusiveGroup ?? string.Empty;
            this.requiredTags = new List<GameplayAbilityTag>(requiredTags ?? new GameplayAbilityTag[0]);
            this.requiredParameters = new Dictionary<string, string>(requiredParameters ?? new Dictionary<string, string>());
            ConditionOperator = conditionOperator;
        }
    }
}
