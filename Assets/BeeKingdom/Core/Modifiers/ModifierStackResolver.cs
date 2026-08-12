using System.Collections.Generic;

namespace BeeKingdom.Core.Modifiers
{
    public sealed class ModifierStackResolver
    {
        public List<GameplayModifierInstance> Resolve(IReadOnlyList<GameplayModifierInstance> modifiers)
        {
            List<GameplayModifierInstance> resolved = new List<GameplayModifierInstance>();
            Dictionary<string, GameplayModifierInstance> exclusive = new Dictionary<string, GameplayModifierInstance>();
            Dictionary<string, GameplayModifierInstance> selected = new Dictionary<string, GameplayModifierInstance>();

            for (int i = 0; i < modifiers.Count; i++)
            {
                GameplayModifierInstance modifier = modifiers[i];
                if (!modifier.IsEnabled)
                {
                    continue;
                }

                string group = modifier.Definition.ExclusiveGroup;
                if (modifier.Definition.StackingRule == ModifierStackingRule.IgnoreDuplicate && ContainsModifier(resolved, modifier.Definition.ModifierId))
                {
                    continue;
                }

                if (modifier.Definition.StackingRule == ModifierStackingRule.ExclusiveGroup && !string.IsNullOrWhiteSpace(group))
                {
                    if (!exclusive.TryGetValue(group, out GameplayModifierInstance current) || modifier.Definition.Priority > current.Definition.Priority)
                    {
                        exclusive[group] = modifier;
                    }
                    continue;
                }

                if (modifier.Definition.StackingRule == ModifierStackingRule.HighestOnly ||
                    modifier.Definition.StackingRule == ModifierStackingRule.LowestOnly ||
                    modifier.Definition.StackingRule == ModifierStackingRule.Replace)
                {
                    string key = modifier.Definition.StackingRule + ":" + modifier.Definition.TargetKey + ":" + modifier.Definition.Operation;
                    if (!selected.TryGetValue(key, out GameplayModifierInstance current) ||
                        ShouldReplace(current, modifier))
                    {
                        selected[key] = modifier;
                    }
                    continue;
                }

                resolved.Add(modifier);
            }

            foreach (GameplayModifierInstance modifier in exclusive.Values)
            {
                resolved.Add(modifier);
            }

            foreach (GameplayModifierInstance modifier in selected.Values)
            {
                resolved.Add(modifier);
            }

            resolved.Sort((a, b) => a.Definition.Priority.CompareTo(b.Definition.Priority));
            return resolved;
        }

        private static bool ContainsModifier(List<GameplayModifierInstance> modifiers, string id)
        {
            for (int i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i].Definition.ModifierId == id) return true;
            }
            return false;
        }

        private static bool ShouldReplace(GameplayModifierInstance current, GameplayModifierInstance candidate)
        {
            if (candidate.Definition.StackingRule == ModifierStackingRule.HighestOnly)
            {
                return candidate.Definition.Value > current.Definition.Value;
            }

            if (candidate.Definition.StackingRule == ModifierStackingRule.LowestOnly)
            {
                return candidate.Definition.Value < current.Definition.Value;
            }

            return candidate.Definition.Priority >= current.Definition.Priority;
        }
    }
}
