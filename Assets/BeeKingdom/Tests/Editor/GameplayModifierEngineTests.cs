using System.Collections.Generic;
using BeeKingdom.Core.Abilities;
using BeeKingdom.Core.Modifiers;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class GameplayModifierEngineTests
    {
        [Test]
        public void PipelineAppliesOperationsInFixedOrder()
        {
            GameplayModifierEngine engine = new GameplayModifierEngine();
            engine.AddModifier(Modifier("add", GameplayModifierOperation.Add, 5d));
            engine.AddModifier(Modifier("multiply", GameplayModifierOperation.Multiply, 2d));
            engine.AddModifier(new GameplayModifierDefinition("clamp", "value", GameplayModifierOperation.Clamp, 0d, minValue: 0d, maxValue: 20d));

            double value = engine.Evaluate("value", 10d, new ModifierEvaluationContext());

            Assert.That(value, Is.EqualTo(20d));
        }

        [Test]
        public void SupportsSubtractDivideOverrideMinimumMaximum()
        {
            GameplayModifierEngine engine = new GameplayModifierEngine();
            engine.AddModifier(Modifier("subtract", GameplayModifierOperation.Subtract, 5d));
            engine.AddModifier(Modifier("divide", GameplayModifierOperation.Divide, 5d));
            engine.AddModifier(Modifier("min", GameplayModifierOperation.Minimum, 3d));
            engine.AddModifier(Modifier("max", GameplayModifierOperation.Maximum, 4d));

            Assert.That(engine.Evaluate("value", 20d, new ModifierEvaluationContext()), Is.EqualTo(3d));

            engine.AddModifier(Modifier("override", GameplayModifierOperation.Override, 7d));
            Assert.That(engine.Recalculate("value", 20d, new ModifierEvaluationContext()), Is.EqualTo(4d));
        }

        [Test]
        public void HighestOnlyKeepsLargestModifier()
        {
            GameplayModifierEngine engine = new GameplayModifierEngine();
            engine.AddModifier(Modifier("low", GameplayModifierOperation.Add, 2d, ModifierStackingRule.HighestOnly));
            engine.AddModifier(Modifier("high", GameplayModifierOperation.Add, 8d, ModifierStackingRule.HighestOnly));

            Assert.That(engine.Evaluate("value", 10d, new ModifierEvaluationContext()), Is.EqualTo(18d));
        }

        [Test]
        public void FormulaEvaluatorUsesVariables()
        {
            GameplayModifierEngine engine = new GameplayModifierEngine();
            Dictionary<string, double> variables = new Dictionary<string, double>
            {
                { "Population", 100d },
                { "QueenLevel", 2d }
            };

            double value = engine.EvaluateFormula("Population * 0.02 + QueenLevel * 1.5", variables);

            Assert.That(value, Is.EqualTo(5d));
        }

        [Test]
        public void ConditionsFilterModifiers()
        {
            GameplayModifierEngine engine = new GameplayModifierEngine();
            engine.AddModifier(new GameplayModifierDefinition("spring", "value", GameplayModifierOperation.Add, 5d, requiredTags: new[] { new GameplayAbilityTag("Season.Spring") }));

            double inactive = engine.Evaluate("value", 10d, new ModifierEvaluationContext(new[] { new GameplayAbilityTag("Season.Winter") }));
            double active = engine.Recalculate("value", 10d, new ModifierEvaluationContext(new[] { new GameplayAbilityTag("Season.Spring") }));

            Assert.That(inactive, Is.EqualTo(10d));
            Assert.That(active, Is.EqualTo(15d));
        }

        [Test]
        public void EvaluationIsDeterministic()
        {
            Assert.That(EvaluateSample(), Is.EqualTo(EvaluateSample()));
        }

        [Test]
        public void HandlesLargeModifierSet()
        {
            GameplayModifierEngine engine = new GameplayModifierEngine();
            for (int i = 0; i < 100000; i++)
            {
                engine.AddModifier(Modifier("m-" + i, GameplayModifierOperation.Add, 1d));
            }

            Assert.That(engine.Evaluate("value", 0d, new ModifierEvaluationContext()), Is.EqualTo(100000d));
            Assert.That(engine.Diagnostics.ModifierCount, Is.EqualTo(100000));
        }

        private static double EvaluateSample()
        {
            GameplayModifierEngine engine = new GameplayModifierEngine();
            engine.AddModifier(Modifier("add", GameplayModifierOperation.Add, 3d));
            engine.AddModifier(Modifier("multiply", GameplayModifierOperation.Multiply, 4d));
            return engine.Evaluate("value", 2d, new ModifierEvaluationContext());
        }

        private static GameplayModifierDefinition Modifier(string id, GameplayModifierOperation operation, double value, ModifierStackingRule stacking = ModifierStackingRule.Additive)
        {
            return new GameplayModifierDefinition(id, "value", operation, value, stackingRule: stacking);
        }
    }
}
