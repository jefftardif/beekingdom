using System.Collections.Generic;

namespace BeeKingdom.Core.Abilities
{
    public sealed class GameplayAbilityDefinition
    {
        private readonly List<GameplayAbilityTag> tags;
        private readonly List<string> conditions;
        private readonly Dictionary<string, double> costs;
        private readonly List<string> effectIds;
        private readonly Dictionary<string, string> liveOpsMetadata;

        public string AbilityId { get; }
        public string DisplayName { get; }
        public string Category { get; }
        public int Priority { get; }
        public IReadOnlyList<GameplayAbilityTag> Tags => tags;
        public IReadOnlyList<string> Conditions => conditions;
        public IReadOnlyDictionary<string, double> Costs => costs;
        public IReadOnlyList<string> EffectIds => effectIds;
        public IReadOnlyDictionary<string, string> LiveOpsMetadata => liveOpsMetadata;

        public GameplayAbilityDefinition(
            string abilityId,
            string displayName,
            string category,
            int priority,
            IReadOnlyList<GameplayAbilityTag> tags = null,
            IReadOnlyList<string> conditions = null,
            IReadOnlyDictionary<string, double> costs = null,
            IReadOnlyList<string> effectIds = null,
            IReadOnlyDictionary<string, string> liveOpsMetadata = null)
        {
            AbilityId = string.IsNullOrWhiteSpace(abilityId) ? throw new System.ArgumentException("Ability id is required.", nameof(abilityId)) : abilityId;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? AbilityId : displayName;
            Category = string.IsNullOrWhiteSpace(category) ? "General" : category;
            Priority = priority;
            this.tags = new List<GameplayAbilityTag>(tags ?? new GameplayAbilityTag[0]);
            this.conditions = new List<string>(conditions ?? new string[0]);
            this.costs = new Dictionary<string, double>(costs ?? new Dictionary<string, double>());
            this.effectIds = new List<string>(effectIds ?? new string[0]);
            this.liveOpsMetadata = new Dictionary<string, string>(liveOpsMetadata ?? new Dictionary<string, string>());
        }
    }
}
