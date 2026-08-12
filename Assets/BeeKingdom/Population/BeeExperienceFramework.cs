using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Population
{
    public enum ExperienceSource { Construction, Harvesting, Transport, Care, Defense, Exploration, Research, Production, Cleaning, Ventilation, Custom }
    public enum ExperienceLevel { Novice, Apprentice, Skilled, Experienced, Veteran, Elite, Legendary }

    public sealed class ExperienceDefinition
    {
        public string DefinitionId { get; }
        public IReadOnlyDictionary<ExperienceLevel, double> LevelThresholds { get; }
        public double BonusPerLevel { get; }

        public ExperienceDefinition(string definitionId, IReadOnlyDictionary<ExperienceLevel, double> levelThresholds, double bonusPerLevel)
        {
            DefinitionId = string.IsNullOrWhiteSpace(definitionId) ? throw new ArgumentException("Definition id is required.", nameof(definitionId)) : definitionId;
            LevelThresholds = levelThresholds ?? new Dictionary<ExperienceLevel, double>();
            BonusPerLevel = Math.Max(0d, bonusPerLevel);
        }
    }

    public sealed class ExperienceProfile
    {
        private readonly Dictionary<ExperienceSource, double> bySource = new Dictionary<ExperienceSource, double>();
        private readonly List<string> progressionHistory = new List<string>();

        public string BeeId { get; }
        public string DefinitionId { get; }
        public double TotalExperience { get; private set; }
        public double ServiceTimeDays { get; private set; }
        public ExperienceLevel Level { get; private set; }
        public IReadOnlyDictionary<ExperienceSource, double> ExperienceBySource => bySource;
        public IReadOnlyList<string> ProgressionHistory => progressionHistory;

        public ExperienceProfile(string beeId, string definitionId)
        {
            BeeId = beeId ?? string.Empty;
            DefinitionId = definitionId ?? string.Empty;
            Level = ExperienceLevel.Novice;
        }

        public void AddExperience(ExperienceSource source, double amount)
        {
            double safeAmount = Math.Max(0d, amount);
            TotalExperience += safeAmount;
            bySource.TryGetValue(source, out double existing);
            bySource[source] = existing + safeAmount;
        }

        public void AddServiceTime(double days) => ServiceTimeDays += Math.Max(0d, days);
        public void SetLevel(ExperienceLevel level)
        {
            if (Level == level) return;
            Level = level;
            progressionHistory.Add("level:" + level);
        }

        public void Reset()
        {
            TotalExperience = 0d;
            ServiceTimeDays = 0d;
            Level = ExperienceLevel.Novice;
            bySource.Clear();
            progressionHistory.Clear();
        }
    }

    public sealed class ExperienceCalculator
    {
        public ExperienceLevel CalculateLevel(ExperienceDefinition definition, double totalExperience)
        {
            ExperienceLevel level = ExperienceLevel.Novice;
            foreach (KeyValuePair<ExperienceLevel, double> threshold in definition.LevelThresholds)
            {
                if (totalExperience >= threshold.Value && threshold.Key > level) level = threshold.Key;
            }
            return level;
        }

        public double CalculateBonus(ExperienceDefinition definition, ExperienceLevel level)
        {
            return (int)level * definition.BonusPerLevel;
        }
    }

    public sealed class BeeExperienceEngine
    {
        private readonly ExperienceCalculator calculator = new ExperienceCalculator();

        public ExperienceLevel CalculateLevel(ExperienceDefinition definition, ExperienceProfile profile)
        {
            return calculator.CalculateLevel(definition, profile.TotalExperience);
        }

        public double CalculateBonus(ExperienceDefinition definition, ExperienceProfile profile)
        {
            return calculator.CalculateBonus(definition, profile.Level);
        }
    }

    public sealed class BeeExperienceDiagnostics
    {
        public int DefinitionsRegistered { get; private set; }
        public int ProfilesCreated { get; private set; }
        public int Gains { get; private set; }
        public int LevelChanges { get; private set; }
        public int Resets { get; private set; }

        public void RecordDefinitions(int count) => DefinitionsRegistered = count;
        public void RecordProfileCreated() => ProfilesCreated++;
        public void RecordGain() => Gains++;
        public void RecordLevelChange() => LevelChanges++;
        public void RecordReset() => Resets++;
    }

    public sealed class BeeExperienceManager
    {
        private readonly Dictionary<string, ExperienceDefinition> definitions = new Dictionary<string, ExperienceDefinition>();
        private readonly Dictionary<string, ExperienceProfile> profiles = new Dictionary<string, ExperienceProfile>();
        private readonly BeeExperienceEngine engine = new BeeExperienceEngine();
        private readonly IEventBus eventBus;

        public BeeExperienceDiagnostics Diagnostics { get; } = new BeeExperienceDiagnostics();

        public BeeExperienceManager(IEventBus eventBus = null) { this.eventBus = eventBus; }

        public bool RegisterExperienceDefinition(ExperienceDefinition definition)
        {
            if (definition == null || definitions.ContainsKey(definition.DefinitionId)) return false;
            definitions.Add(definition.DefinitionId, definition);
            Diagnostics.RecordDefinitions(definitions.Count);
            return true;
        }

        public ExperienceProfile CreateProfile(string beeId, string definitionId)
        {
            if (!definitions.ContainsKey(definitionId)) return null;
            ExperienceProfile profile = new ExperienceProfile(beeId, definitionId);
            profiles[beeId ?? string.Empty] = profile;
            Diagnostics.RecordProfileCreated();
            return profile;
        }

        public bool AddExperience(string beeId, ExperienceSource source, double amount, double serviceDays = 0d)
        {
            ExperienceProfile profile = QueryExperience(beeId);
            if (profile == null || !definitions.TryGetValue(profile.DefinitionId, out ExperienceDefinition definition)) return false;
            ExperienceLevel previous = profile.Level;
            profile.AddExperience(source, amount);
            profile.AddServiceTime(serviceDays);
            ExperienceLevel next = engine.CalculateLevel(definition, profile);
            profile.SetLevel(next);
            Diagnostics.RecordGain();
            eventBus?.Publish(new ExperienceGained(beeId, source, amount));
            eventBus?.Publish(new ExperienceUpdated(beeId));
            if (previous != next)
            {
                Diagnostics.RecordLevelChange();
                eventBus?.Publish(new LevelChanged(beeId, next));
                if (next >= ExperienceLevel.Veteran) eventBus?.Publish(new VeteranReached(beeId));
            }
            return true;
        }

        public ExperienceLevel CalculateLevel(string beeId)
        {
            ExperienceProfile profile = QueryExperience(beeId);
            if (profile == null || !definitions.TryGetValue(profile.DefinitionId, out ExperienceDefinition definition)) return ExperienceLevel.Novice;
            return engine.CalculateLevel(definition, profile);
        }

        public double CalculateBonus(string beeId)
        {
            ExperienceProfile profile = QueryExperience(beeId);
            if (profile == null || !definitions.TryGetValue(profile.DefinitionId, out ExperienceDefinition definition)) return 0d;
            return engine.CalculateBonus(definition, profile);
        }

        public ExperienceProfile QueryExperience(string beeId) => profiles.TryGetValue(beeId ?? string.Empty, out ExperienceProfile profile) ? profile : null;
        public ExperienceLevel QueryLevel(string beeId) => QueryExperience(beeId)?.Level ?? ExperienceLevel.Novice;

        public bool ResetExperience(string beeId)
        {
            ExperienceProfile profile = QueryExperience(beeId);
            if (profile == null) return false;
            profile.Reset();
            Diagnostics.RecordReset();
            eventBus?.Publish(new ExperienceUpdated(beeId));
            return true;
        }
    }

    public readonly struct ExperienceGained : IGameplayEvent, IBeeEvent { public string BeeId { get; } public ExperienceSource Source { get; } public double Amount { get; } public ExperienceGained(string beeId, ExperienceSource source, double amount) { BeeId = beeId; Source = source; Amount = amount; } }
    public readonly struct LevelChanged : IGameplayEvent, IBeeEvent { public string BeeId { get; } public ExperienceLevel Level { get; } public LevelChanged(string beeId, ExperienceLevel level) { BeeId = beeId; Level = level; } }
    public readonly struct VeteranReached : IGameplayEvent, IBeeEvent { public string BeeId { get; } public VeteranReached(string beeId) { BeeId = beeId; } }
    public readonly struct ExperienceUpdated : IGameplayEvent, IBeeEvent { public string BeeId { get; } public ExperienceUpdated(string beeId) { BeeId = beeId; } }
}
