using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Population
{
    public enum PersonalityTraitKind { Curiosity, Discipline, Courage, Patience, Sociability, Adaptability, Persistence, Efficiency, RiskTolerance, ExplorationAffinity, Custom }

    public sealed class PersonalityTrait
    {
        public string TraitId { get; }
        public PersonalityTraitKind Kind { get; }
        public double MinimumValue { get; }
        public double MaximumValue { get; }
        public double GeneticWeight { get; }
        public double ExperienceWeight { get; }
        public double EnvironmentWeight { get; }
        public double EvolutionLimit { get; }

        public PersonalityTrait(string traitId, PersonalityTraitKind kind, double minimumValue, double maximumValue, double geneticWeight, double experienceWeight, double environmentWeight, double evolutionLimit)
        {
            TraitId = string.IsNullOrWhiteSpace(traitId) ? throw new ArgumentException("Trait id is required.", nameof(traitId)) : traitId;
            Kind = kind;
            MinimumValue = minimumValue;
            MaximumValue = maximumValue < minimumValue ? minimumValue : maximumValue;
            GeneticWeight = Clamp01(geneticWeight);
            ExperienceWeight = Clamp01(experienceWeight);
            EnvironmentWeight = Clamp01(environmentWeight);
            EvolutionLimit = Math.Max(0d, evolutionLimit);
        }

        public double Clamp(double value) => value < MinimumValue ? MinimumValue : value > MaximumValue ? MaximumValue : value;
        private static double Clamp01(double value) => value < 0d ? 0d : value > 1d ? 1d : value;
    }

    public sealed class PersonalityDefinition
    {
        public string DefinitionId { get; }
        public IReadOnlyList<PersonalityTrait> Traits { get; }

        public PersonalityDefinition(string definitionId, IReadOnlyList<PersonalityTrait> traits)
        {
            DefinitionId = string.IsNullOrWhiteSpace(definitionId) ? throw new ArgumentException("Definition id is required.", nameof(definitionId)) : definitionId;
            Traits = traits ?? Array.Empty<PersonalityTrait>();
        }
    }

    public sealed class PersonalityProfile
    {
        private readonly Dictionary<string, double> values = new Dictionary<string, double>();
        private readonly List<string> evolutionHistory = new List<string>();

        public string BeeId { get; }
        public string DefinitionId { get; }
        public string DominantTraitId { get; private set; }
        public double Stability { get; private set; }
        public IReadOnlyDictionary<string, double> Values => values;
        public IReadOnlyList<string> EvolutionHistory => evolutionHistory;

        public PersonalityProfile(string beeId, string definitionId)
        {
            BeeId = beeId ?? string.Empty;
            DefinitionId = definitionId ?? string.Empty;
            DominantTraitId = string.Empty;
            Stability = 1d;
        }

        public void SetTrait(string traitId, double value)
        {
            values[traitId] = value;
            RecalculateDominant();
        }

        public bool TryGetTrait(string traitId, out double value) => values.TryGetValue(traitId, out value);

        public void RecordEvolution(string traitId)
        {
            evolutionHistory.Add("trait:" + traitId);
            Stability = Math.Max(0d, Stability - 0.01d);
        }

        private void RecalculateDominant()
        {
            string dominant = string.Empty;
            double best = double.MinValue;
            foreach (KeyValuePair<string, double> value in values)
            {
                if (value.Value <= best) continue;
                best = value.Value;
                dominant = value.Key;
            }
            DominantTraitId = dominant;
        }
    }

    public readonly struct PersonalityContext
    {
        public string BeeId { get; }
        public string GenomeId { get; }
        public double EarlyEventFactor { get; }
        public double ExperienceFactor { get; }
        public double EnvironmentFactor { get; }

        public PersonalityContext(string beeId, string genomeId = "", double earlyEventFactor = 0.5d, double experienceFactor = 0.5d, double environmentFactor = 0.5d)
        {
            BeeId = beeId ?? string.Empty;
            GenomeId = genomeId ?? string.Empty;
            EarlyEventFactor = Clamp01(earlyEventFactor);
            ExperienceFactor = Clamp01(experienceFactor);
            EnvironmentFactor = Clamp01(environmentFactor);
        }

        private static double Clamp01(double value) => value < 0d ? 0d : value > 1d ? 1d : value;
    }

    public sealed class BeePersonalityEngine
    {
        public PersonalityProfile GeneratePersonality(PersonalityDefinition definition, PersonalityContext context, IReadOnlyDictionary<string, double> geneticTraits, int seed)
        {
            DeterministicRandom random = new DeterministicRandom(seed);
            PersonalityProfile profile = new PersonalityProfile(context.BeeId, definition.DefinitionId);
            for (int i = 0; i < definition.Traits.Count; i++)
            {
                PersonalityTrait trait = definition.Traits[i];
                geneticTraits.TryGetValue(trait.TraitId, out double genetic);
                double variability = random.NextDouble();
                double value =
                    genetic * trait.GeneticWeight +
                    context.ExperienceFactor * trait.ExperienceWeight +
                    context.EnvironmentFactor * trait.EnvironmentWeight +
                    context.EarlyEventFactor * 0.1d +
                    variability * 0.1d;
                profile.SetTrait(trait.TraitId, trait.Clamp(value));
            }
            return profile;
        }

        public void UpdatePersonality(PersonalityDefinition definition, PersonalityProfile profile, PersonalityContext context)
        {
            for (int i = 0; i < definition.Traits.Count; i++)
            {
                PersonalityTrait trait = definition.Traits[i];
                profile.TryGetTrait(trait.TraitId, out double current);
                double target = (context.ExperienceFactor + context.EnvironmentFactor) * 0.5d;
                double delta = Math.Max(-trait.EvolutionLimit, Math.Min(trait.EvolutionLimit, target - current));
                if (Math.Abs(delta) <= 0.0001d) continue;
                profile.SetTrait(trait.TraitId, trait.Clamp(current + delta));
                profile.RecordEvolution(trait.TraitId);
            }
        }

        public IReadOnlyDictionary<string, double> CalculateBehaviorModifiers(PersonalityProfile profile)
        {
            Dictionary<string, double> modifiers = new Dictionary<string, double>();
            foreach (KeyValuePair<string, double> value in profile.Values) modifiers.Add(value.Key, 1d + (value.Value - 0.5d) * 0.2d);
            return modifiers;
        }
    }

    public sealed class BeePersonalityDiagnostics
    {
        public int DefinitionsRegistered { get; private set; }
        public int ProfilesGenerated { get; private set; }
        public int Updates { get; private set; }
        public int TraitChanges { get; private set; }
        public int ModifierCalculations { get; private set; }
        public int Resets { get; private set; }

        public void RecordDefinitions(int count) => DefinitionsRegistered = count;
        public void RecordGenerated() => ProfilesGenerated++;
        public void RecordUpdate() => Updates++;
        public void RecordTraitChanges(int count) => TraitChanges += Math.Max(0, count);
        public void RecordModifierCalculation() => ModifierCalculations++;
        public void RecordReset() => Resets++;
    }

    public sealed class BeePersonalityManager
    {
        private readonly Dictionary<string, PersonalityDefinition> definitions = new Dictionary<string, PersonalityDefinition>();
        private readonly Dictionary<string, PersonalityProfile> profiles = new Dictionary<string, PersonalityProfile>();
        private readonly BeePersonalityEngine engine = new BeePersonalityEngine();
        private readonly GeneticsManager geneticsManager;
        private readonly IEventBus eventBus;

        public BeePersonalityDiagnostics Diagnostics { get; } = new BeePersonalityDiagnostics();

        public BeePersonalityManager(GeneticsManager geneticsManager = null, IEventBus eventBus = null)
        {
            this.geneticsManager = geneticsManager;
            this.eventBus = eventBus;
        }

        public bool RegisterTraitDefinition(PersonalityDefinition definition)
        {
            if (definition == null || definitions.ContainsKey(definition.DefinitionId)) return false;
            definitions.Add(definition.DefinitionId, definition);
            Diagnostics.RecordDefinitions(definitions.Count);
            return true;
        }

        public PersonalityProfile GeneratePersonality(string definitionId, PersonalityContext context, int seed)
        {
            if (!definitions.TryGetValue(definitionId, out PersonalityDefinition definition)) return null;
            IReadOnlyDictionary<string, double> geneticTraits = geneticsManager?.CalculateTraits(context.GenomeId) ?? new Dictionary<string, double>();
            PersonalityProfile profile = engine.GeneratePersonality(definition, context, geneticTraits, seed);
            profiles[context.BeeId] = profile;
            Diagnostics.RecordGenerated();
            eventBus?.Publish(new PersonalityGenerated(context.BeeId));
            eventBus?.Publish(new PersonalityProfileUpdated(context.BeeId));
            return profile;
        }

        public bool UpdatePersonality(PersonalityContext context)
        {
            PersonalityProfile profile = QueryPersonality(context.BeeId);
            if (profile == null || !definitions.TryGetValue(profile.DefinitionId, out PersonalityDefinition definition)) return false;
            int before = profile.EvolutionHistory.Count;
            engine.UpdatePersonality(definition, profile, context);
            int changes = profile.EvolutionHistory.Count - before;
            Diagnostics.RecordUpdate();
            Diagnostics.RecordTraitChanges(changes);
            if (changes > 0) eventBus?.Publish(new TraitChanged(context.BeeId, profile.DominantTraitId));
            eventBus?.Publish(new PersonalityUpdated(context.BeeId));
            eventBus?.Publish(new PersonalityProfileUpdated(context.BeeId));
            return true;
        }

        public PersonalityProfile QueryPersonality(string beeId) => profiles.TryGetValue(beeId ?? string.Empty, out PersonalityProfile profile) ? profile : null;

        public IReadOnlyDictionary<string, double> CalculateBehaviorModifiers(string beeId)
        {
            PersonalityProfile profile = QueryPersonality(beeId);
            if (profile == null) return new Dictionary<string, double>();
            Diagnostics.RecordModifierCalculation();
            eventBus?.Publish(new PersonalityInfluenced(beeId));
            return engine.CalculateBehaviorModifiers(profile);
        }

        public bool ResetPersonality(string beeId)
        {
            bool removed = profiles.Remove(beeId ?? string.Empty);
            if (removed)
            {
                Diagnostics.RecordReset();
                eventBus?.Publish(new PersonalityProfileUpdated(beeId));
            }
            return removed;
        }
    }

    public readonly struct PersonalityGenerated : IGameplayEvent, IBeeEvent { public string BeeId { get; } public PersonalityGenerated(string beeId) { BeeId = beeId; } }
    public readonly struct PersonalityUpdated : IGameplayEvent, IBeeEvent { public string BeeId { get; } public PersonalityUpdated(string beeId) { BeeId = beeId; } }
    public readonly struct TraitChanged : IGameplayEvent, IBeeEvent { public string BeeId { get; } public string TraitId { get; } public TraitChanged(string beeId, string traitId) { BeeId = beeId; TraitId = traitId; } }
    public readonly struct PersonalityInfluenced : IGameplayEvent, IBeeEvent { public string BeeId { get; } public PersonalityInfluenced(string beeId) { BeeId = beeId; } }
    public readonly struct PersonalityProfileUpdated : IGameplayEvent, IBeeEvent { public string BeeId { get; } public PersonalityProfileUpdated(string beeId) { BeeId = beeId; } }
}
