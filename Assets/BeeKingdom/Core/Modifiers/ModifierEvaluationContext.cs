using System.Collections.Generic;
using BeeKingdom.Core.Abilities;

namespace BeeKingdom.Core.Modifiers
{
    public sealed class ModifierEvaluationContext
    {
        private readonly List<GameplayAbilityTag> tags;
        private readonly Dictionary<string, string> parameters;
        private readonly Dictionary<string, double> variables;

        public IReadOnlyList<GameplayAbilityTag> Tags => tags;
        public IReadOnlyDictionary<string, string> Parameters => parameters;
        public IReadOnlyDictionary<string, double> Variables => variables;

        public ModifierEvaluationContext(IReadOnlyList<GameplayAbilityTag> tags = null, IReadOnlyDictionary<string, string> parameters = null, IReadOnlyDictionary<string, double> variables = null)
        {
            this.tags = new List<GameplayAbilityTag>(tags ?? new GameplayAbilityTag[0]);
            this.parameters = new Dictionary<string, string>(parameters ?? new Dictionary<string, string>());
            this.variables = new Dictionary<string, double>(variables ?? new Dictionary<string, double>());
        }

        public bool HasTag(GameplayAbilityTag required)
        {
            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i].Equals(required) || tags[i].IsChildOf(required))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
