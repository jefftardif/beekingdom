using System.Collections.Generic;

namespace BeeKingdom.Core.Modifiers
{
    public sealed class ModifierPipeline
    {
        private readonly ModifierStackResolver stackResolver = new ModifierStackResolver();
        private readonly ModifierAggregator aggregator = new ModifierAggregator();

        public double Evaluate(double baseValue, IReadOnlyList<GameplayModifierInstance> modifiers, ModifierEvaluationContext context)
        {
            return aggregator.Evaluate(baseValue, stackResolver.Resolve(modifiers), context);
        }
    }
}
