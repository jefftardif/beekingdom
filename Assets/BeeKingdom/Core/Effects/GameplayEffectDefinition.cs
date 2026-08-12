using System.Collections.Generic;
using BeeKingdom.Core.Abilities;

namespace BeeKingdom.Core.Effects
{
    public sealed class GameplayEffectDefinition
    {
        private readonly List<GameplayAbilityTag> tags;
        private readonly List<string> conditions;
        private readonly List<string> modifierIds;
        private readonly Dictionary<string, string> liveOpsMetadata;

        public string EffectId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public GameplayEffectType EffectType { get; }
        public double DurationSeconds { get; }
        public double PeriodSeconds { get; }
        public IReadOnlyList<GameplayAbilityTag> Tags => tags;
        public IReadOnlyList<string> Conditions => conditions;
        public IReadOnlyList<string> ModifierIds => modifierIds;
        public IReadOnlyDictionary<string, string> LiveOpsMetadata => liveOpsMetadata;

        public GameplayEffectDefinition(string effectId, string displayName, string description, GameplayEffectType effectType, double durationSeconds, double periodSeconds, IReadOnlyList<GameplayAbilityTag> tags = null, IReadOnlyList<string> conditions = null, IReadOnlyList<string> modifierIds = null, IReadOnlyDictionary<string, string> liveOpsMetadata = null)
        {
            EffectId = string.IsNullOrWhiteSpace(effectId) ? throw new System.ArgumentException("Effect id is required.", nameof(effectId)) : effectId;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? EffectId : displayName;
            Description = description ?? string.Empty;
            EffectType = effectType;
            DurationSeconds = durationSeconds < 0d ? 0d : durationSeconds;
            PeriodSeconds = periodSeconds < 0d ? 0d : periodSeconds;
            this.tags = new List<GameplayAbilityTag>(tags ?? new GameplayAbilityTag[0]);
            this.conditions = new List<string>(conditions ?? new string[0]);
            this.modifierIds = new List<string>(modifierIds ?? new string[0]);
            this.liveOpsMetadata = new Dictionary<string, string>(liveOpsMetadata ?? new Dictionary<string, string>());
        }
    }
}
