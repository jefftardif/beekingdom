using System.Collections.Generic;

namespace BeeKingdom.Core.Abilities
{
    public sealed class GameplayAbilityRegistry
    {
        private readonly Dictionary<string, GameplayAbilityDefinition> definitions = new Dictionary<string, GameplayAbilityDefinition>();

        public int Count => definitions.Count;

        public bool RegisterAbility(GameplayAbilityDefinition definition)
        {
            if (definition == null || definitions.ContainsKey(definition.AbilityId))
            {
                return false;
            }

            definitions.Add(definition.AbilityId, definition);
            return true;
        }

        public bool UnregisterAbility(string abilityId)
        {
            return definitions.Remove(abilityId);
        }

        public bool TryGet(string abilityId, out GameplayAbilityDefinition definition)
        {
            return definitions.TryGetValue(abilityId, out definition);
        }

        public IReadOnlyList<GameplayAbilityDefinition> QueryAbilities(GameplayAbilityTag tag)
        {
            List<GameplayAbilityDefinition> result = new List<GameplayAbilityDefinition>();
            foreach (GameplayAbilityDefinition definition in definitions.Values)
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
