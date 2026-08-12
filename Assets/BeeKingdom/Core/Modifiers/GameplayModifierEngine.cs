using System.Collections.Generic;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Core.Modifiers
{
    public sealed class GameplayModifierEngine
    {
        private readonly Dictionary<string, GameplayModifierInstance> modifiersById = new Dictionary<string, GameplayModifierInstance>();
        private readonly Dictionary<string, List<GameplayModifierInstance>> modifiersByTarget = new Dictionary<string, List<GameplayModifierInstance>>();
        private readonly Dictionary<string, double> cache = new Dictionary<string, double>();
        private readonly ModifierPipeline pipeline = new ModifierPipeline();
        private readonly FormulaEvaluator formulaEvaluator = new FormulaEvaluator();
        private readonly IEventBus eventBus;

        public ModifierDiagnostics Diagnostics { get; } = new ModifierDiagnostics();

        public GameplayModifierEngine(IEventBus eventBus = null)
        {
            this.eventBus = eventBus;
        }

        public bool AddModifier(GameplayModifierDefinition definition)
        {
            if (definition == null || modifiersById.ContainsKey(definition.ModifierId))
            {
                return false;
            }

            GameplayModifierInstance instance = new GameplayModifierInstance(definition);
            modifiersById.Add(definition.ModifierId, instance);
            if (!modifiersByTarget.TryGetValue(definition.TargetKey, out List<GameplayModifierInstance> list))
            {
                list = new List<GameplayModifierInstance>();
                modifiersByTarget[definition.TargetKey] = list;
            }
            list.Add(instance);
            cache.Remove(definition.TargetKey);
            Diagnostics.RecordModifiers(modifiersById.Count);
            eventBus?.Publish(new ModifierAdded(definition.ModifierId));
            return true;
        }

        public bool RemoveModifier(string modifierId)
        {
            if (!modifiersById.TryGetValue(modifierId, out GameplayModifierInstance instance))
            {
                return false;
            }

            modifiersById.Remove(modifierId);
            if (modifiersByTarget.TryGetValue(instance.Definition.TargetKey, out List<GameplayModifierInstance> list))
            {
                list.Remove(instance);
            }
            cache.Remove(instance.Definition.TargetKey);
            Diagnostics.RecordModifiers(modifiersById.Count);
            eventBus?.Publish(new ModifierRemoved(modifierId));
            return true;
        }

        public double Evaluate(string targetKey, double baseValue, ModifierEvaluationContext context)
        {
            if (!modifiersByTarget.TryGetValue(targetKey, out List<GameplayModifierInstance> modifiers))
            {
                return baseValue;
            }

            double value = pipeline.Evaluate(baseValue, modifiers, context ?? new ModifierEvaluationContext());
            cache[targetKey] = value;
            Diagnostics.RecordEvaluation(value);
            eventBus?.Publish(new ModifierEvaluated(targetKey, value));
            eventBus?.Publish(new FinalValueChanged(targetKey, value));
            return value;
        }

        public double Recalculate(string targetKey, double baseValue, ModifierEvaluationContext context)
        {
            cache.Remove(targetKey);
            return Evaluate(targetKey, baseValue, context);
        }

        public IReadOnlyList<GameplayModifierInstance> QueryModifiers(string targetKey)
        {
            return modifiersByTarget.TryGetValue(targetKey, out List<GameplayModifierInstance> modifiers) ? modifiers : new GameplayModifierInstance[0];
        }

        public double EvaluateFormula(string formula, IReadOnlyDictionary<string, double> variables)
        {
            double value = formulaEvaluator.EvaluateFormula(formula, variables);
            eventBus?.Publish(new FormulaEvaluated(formula, value));
            return value;
        }
    }
}
