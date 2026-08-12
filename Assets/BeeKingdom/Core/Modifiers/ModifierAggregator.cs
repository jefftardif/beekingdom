using System;
using System.Collections.Generic;

namespace BeeKingdom.Core.Modifiers
{
    public sealed class ModifierAggregator
    {
        private readonly FormulaEvaluator formulaEvaluator = new FormulaEvaluator();

        public double Evaluate(double baseValue, IReadOnlyList<GameplayModifierInstance> modifiers, ModifierEvaluationContext context)
        {
            double value = baseValue;
            double min = double.MinValue;
            double max = double.MaxValue;
            bool hasOverride = false;
            double overrideValue = 0d;

            for (int phase = 0; phase < 5; phase++)
            {
                for (int i = 0; i < modifiers.Count; i++)
                {
                    GameplayModifierDefinition definition = modifiers[i].Definition;
                    if (!MatchesPhase(definition.Operation, phase) || !ConditionsPass(definition, context))
                    {
                        continue;
                    }

                    switch (definition.Operation)
                    {
                        case GameplayModifierOperation.Add: value += definition.Value; break;
                        case GameplayModifierOperation.Subtract: value -= definition.Value; break;
                        case GameplayModifierOperation.Multiply: value *= definition.Value; break;
                        case GameplayModifierOperation.Divide: if (definition.Value != 0d) value /= definition.Value; break;
                        case GameplayModifierOperation.Formula: value = formulaEvaluator.EvaluateFormula(definition.Formula, context.Variables); break;
                        case GameplayModifierOperation.Override: hasOverride = true; overrideValue = definition.Value; break;
                        case GameplayModifierOperation.Minimum: min = Math.Max(min, definition.Value); break;
                        case GameplayModifierOperation.Maximum: max = Math.Min(max, definition.Value); break;
                        case GameplayModifierOperation.Clamp: min = Math.Max(min, definition.MinValue); max = Math.Min(max, definition.MaxValue); break;
                        case GameplayModifierOperation.Curve: value *= definition.Value; break;
                    }
                }
            }

            if (hasOverride) value = overrideValue;
            if (value < min) value = min;
            if (value > max) value = max;
            return value;
        }

        private static bool MatchesPhase(GameplayModifierOperation operation, int phase)
        {
            if (phase == 0) return operation == GameplayModifierOperation.Add || operation == GameplayModifierOperation.Subtract;
            if (phase == 1) return operation == GameplayModifierOperation.Multiply || operation == GameplayModifierOperation.Divide || operation == GameplayModifierOperation.Curve;
            if (phase == 2) return operation == GameplayModifierOperation.Formula;
            if (phase == 3) return operation == GameplayModifierOperation.Override;
            return operation == GameplayModifierOperation.Clamp || operation == GameplayModifierOperation.Minimum || operation == GameplayModifierOperation.Maximum;
        }

        private static bool ConditionsPass(GameplayModifierDefinition definition, ModifierEvaluationContext context)
        {
            bool tagResult = definition.RequiredTags.Count == 0;
            if (definition.RequiredTags.Count > 0)
            {
                tagResult = definition.ConditionOperator == ModifierConditionOperator.And;
                for (int i = 0; i < definition.RequiredTags.Count; i++)
                {
                    bool has = context.HasTag(definition.RequiredTags[i]);
                    if (definition.ConditionOperator == ModifierConditionOperator.Or) tagResult |= has;
                    else if (definition.ConditionOperator == ModifierConditionOperator.Not) tagResult = !has;
                    else tagResult &= has;
                }
            }

            bool paramResult = true;
            foreach (var pair in definition.RequiredParameters)
            {
                paramResult &= context.Parameters.TryGetValue(pair.Key, out string value) && value == pair.Value;
            }

            return tagResult && paramResult;
        }
    }
}
