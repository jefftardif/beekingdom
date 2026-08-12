using System.Collections.Generic;
using BeeKingdom.Core.Abilities;

namespace BeeKingdom.Core.Effects
{
    public sealed class GameplayEffectRegistry
    {
        private readonly Dictionary<string, GameplayEffectDefinition> definitions = new Dictionary<string, GameplayEffectDefinition>();
        public int Count => definitions.Count;

        public bool RegisterEffect(GameplayEffectDefinition definition)
        {
            if (definition == null || definitions.ContainsKey(definition.EffectId)) return false;
            definitions.Add(definition.EffectId, definition);
            return true;
        }

        public bool TryGet(string effectId, out GameplayEffectDefinition definition) => definitions.TryGetValue(effectId, out definition);
        public bool UnregisterEffect(string effectId) => definitions.Remove(effectId);

        public IReadOnlyList<GameplayEffectDefinition> QueryEffects(GameplayAbilityTag tag)
        {
            List<GameplayEffectDefinition> result = new List<GameplayEffectDefinition>();
            foreach (GameplayEffectDefinition definition in definitions.Values)
            {
                for (int i = 0; i < definition.Tags.Count; i++)
                {
                    if (definition.Tags[i].IsChildOf(tag) || definition.Tags[i].Equals(tag))
                    {
                        result.Add(definition);
                        break;
                    }
                }
            }

            return result;
        }
    }
}
