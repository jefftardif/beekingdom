using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Population
{
    public enum GeneticTraitKind { Fertility, Longevity, Productivity, Intelligence, Navigation, Strength, Resistance, Adaptability, Aggressiveness, LearningRate, Custom }
    public enum MutationKind { None, Beneficial, Neutral, Unfavorable, Exceptional }

    public sealed class GeneticTrait
    {
        public string TraitId { get; }
        public GeneticTraitKind Kind { get; }
        public double MinimumValue { get; }
        public double MaximumValue { get; }
        public double Dominance { get; }
        public double MutationChance { get; }
        public double MutationMagnitude { get; }

        public GeneticTrait(string traitId, GeneticTraitKind kind, double minimumValue, double maximumValue, double dominance, double mutationChance, double mutationMagnitude)
        {
            TraitId = string.IsNullOrWhiteSpace(traitId) ? throw new ArgumentException("Trait id is required.", nameof(traitId)) : traitId;
            Kind = kind;
            MinimumValue = minimumValue;
            MaximumValue = maximumValue < minimumValue ? minimumValue : maximumValue;
            Dominance = Clamp01(dominance);
            MutationChance = Clamp01(mutationChance);
            MutationMagnitude = Math.Max(0d, mutationMagnitude);
        }

        public double Clamp(double value)
        {
            if (value < MinimumValue) return MinimumValue;
            if (value > MaximumValue) return MaximumValue;
            return value;
        }

        private static double Clamp01(double value) => value < 0d ? 0d : value > 1d ? 1d : value;
    }

    public sealed class GenomeDefinition
    {
        public string DefinitionId { get; }
        public IReadOnlyList<GeneticTrait> Traits { get; }
        public double RecombinationRate { get; }
        public double RareMutationChance { get; }

        public GenomeDefinition(string definitionId, IReadOnlyList<GeneticTrait> traits, double recombinationRate, double rareMutationChance)
        {
            DefinitionId = string.IsNullOrWhiteSpace(definitionId) ? throw new ArgumentException("Definition id is required.", nameof(definitionId)) : definitionId;
            Traits = traits ?? Array.Empty<GeneticTrait>();
            RecombinationRate = Clamp01(recombinationRate);
            RareMutationChance = Clamp01(rareMutationChance);
        }

        private static double Clamp01(double value) => value < 0d ? 0d : value > 1d ? 1d : value;
    }

    public sealed class GenomeInstance
    {
        private readonly Dictionary<string, double> values = new Dictionary<string, double>();
        private readonly List<string> mutationHistory = new List<string>();

        public string GenomeId { get; }
        public string DefinitionId { get; }
        public int Generation { get; }
        public IReadOnlyDictionary<string, double> Values => values;
        public IReadOnlyList<string> MutationHistory => mutationHistory;

        public GenomeInstance(string genomeId, string definitionId, int generation)
        {
            GenomeId = string.IsNullOrWhiteSpace(genomeId) ? throw new ArgumentException("Genome id is required.", nameof(genomeId)) : genomeId;
            DefinitionId = definitionId ?? string.Empty;
            Generation = Math.Max(0, generation);
        }

        public void SetTraitValue(string traitId, double value) => values[traitId] = value;
        public bool TryGetTraitValue(string traitId, out double value) => values.TryGetValue(traitId, out value);
        public void RecordMutation(string mutationId) => mutationHistory.Add(mutationId ?? string.Empty);
    }

    public sealed class GeneticsStatistics
    {
        public int GenomeCount { get; }
        public int MutationCount { get; }
        public double Diversity { get; }
        public IReadOnlyDictionary<string, double> AverageTraits { get; }

        public GeneticsStatistics(int genomeCount, int mutationCount, double diversity, IReadOnlyDictionary<string, double> averageTraits)
        {
            GenomeCount = genomeCount;
            MutationCount = mutationCount;
            Diversity = diversity;
            AverageTraits = averageTraits;
        }
    }

    public sealed class GeneticsDiagnostics
    {
        public int DefinitionsRegistered { get; private set; }
        public int Generated { get; private set; }
        public int Inherited { get; private set; }
        public int Mutated { get; private set; }
        public int TraitCalculations { get; private set; }

        public void RecordDefinitions(int count) => DefinitionsRegistered = count;
        public void RecordGenerated() => Generated++;
        public void RecordInherited() => Inherited++;
        public void RecordMutated() => Mutated++;
        public void RecordTraitCalculation() => TraitCalculations++;
    }

    public sealed class GeneticsEngine
    {
        public GenomeInstance GenerateGenome(GenomeDefinition definition, string genomeId, int seed)
        {
            DeterministicRandom random = new DeterministicRandom(seed);
            GenomeInstance genome = new GenomeInstance(genomeId, definition.DefinitionId, 0);
            for (int i = 0; i < definition.Traits.Count; i++)
            {
                GeneticTrait trait = definition.Traits[i];
                double value = trait.MinimumValue + (trait.MaximumValue - trait.MinimumValue) * random.NextDouble();
                genome.SetTraitValue(trait.TraitId, trait.Clamp(value));
            }
            return genome;
        }

        public GenomeInstance InheritGenome(GenomeDefinition definition, string genomeId, GenomeInstance maternal, GenomeInstance paternal, int seed)
        {
            DeterministicRandom random = new DeterministicRandom(seed);
            int generation = Math.Max(maternal?.Generation ?? 0, paternal?.Generation ?? 0) + 1;
            GenomeInstance child = new GenomeInstance(genomeId, definition.DefinitionId, generation);

            for (int i = 0; i < definition.Traits.Count; i++)
            {
                GeneticTrait trait = definition.Traits[i];
                double maternalValue = ResolveTraitValue(trait, maternal, random);
                double paternalValue = ResolveTraitValue(trait, paternal, random);
                bool recombine = random.NextDouble() <= definition.RecombinationRate;
                double baseValue = recombine ? (maternalValue + paternalValue) * 0.5d : maternalValue;
                double inherited = baseValue * trait.Dominance + paternalValue * (1d - trait.Dominance);
                child.SetTraitValue(trait.TraitId, trait.Clamp(inherited));
            }

            return child;
        }

        public MutationKind MutateGenome(GenomeDefinition definition, GenomeInstance genome, int seed)
        {
            if (genome == null) return MutationKind.None;
            DeterministicRandom random = new DeterministicRandom(seed);
            MutationKind strongest = MutationKind.None;

            for (int i = 0; i < definition.Traits.Count; i++)
            {
                GeneticTrait trait = definition.Traits[i];
                if (!genome.TryGetTraitValue(trait.TraitId, out double current)) current = trait.MinimumValue;
                double roll = random.NextDouble();
                double chance = trait.MutationChance + definition.RareMutationChance;
                if (roll > chance) continue;

                MutationKind kind = ResolveMutationKind(random.NextDouble());
                double direction = kind == MutationKind.Unfavorable ? -1d : 1d;
                double magnitude = kind == MutationKind.Exceptional ? trait.MutationMagnitude * 2d : trait.MutationMagnitude;
                double mutated = trait.Clamp(current + direction * magnitude * random.NextDouble());
                genome.SetTraitValue(trait.TraitId, mutated);
                genome.RecordMutation(trait.TraitId + ":" + kind);
                strongest = Rank(kind) > Rank(strongest) ? kind : strongest;
            }

            return strongest;
        }

        public IReadOnlyDictionary<string, double> CalculateTraits(GenomeDefinition definition, GenomeInstance genome)
        {
            Dictionary<string, double> traits = new Dictionary<string, double>();
            if (genome == null) return traits;
            for (int i = 0; i < definition.Traits.Count; i++)
            {
                GeneticTrait trait = definition.Traits[i];
                if (genome.TryGetTraitValue(trait.TraitId, out double value)) traits.Add(trait.TraitId, trait.Clamp(value));
            }
            return traits;
        }

        private static double ResolveTraitValue(GeneticTrait trait, GenomeInstance genome, DeterministicRandom random)
        {
            if (genome != null && genome.TryGetTraitValue(trait.TraitId, out double value)) return trait.Clamp(value);
            return trait.MinimumValue + (trait.MaximumValue - trait.MinimumValue) * random.NextDouble();
        }

        private static MutationKind ResolveMutationKind(double roll)
        {
            if (roll < 0.15d) return MutationKind.Unfavorable;
            if (roll < 0.45d) return MutationKind.Neutral;
            if (roll < 0.9d) return MutationKind.Beneficial;
            return MutationKind.Exceptional;
        }

        private static int Rank(MutationKind kind)
        {
            switch (kind)
            {
                case MutationKind.Exceptional: return 4;
                case MutationKind.Beneficial: return 3;
                case MutationKind.Unfavorable: return 2;
                case MutationKind.Neutral: return 1;
                default: return 0;
            }
        }
    }

    public sealed class GeneticsManager
    {
        private readonly Dictionary<string, GenomeDefinition> definitions = new Dictionary<string, GenomeDefinition>();
        private readonly Dictionary<string, GenomeInstance> genomes = new Dictionary<string, GenomeInstance>();
        private readonly GeneticsEngine engine = new GeneticsEngine();
        private readonly IEventBus eventBus;

        public GeneticsDiagnostics Diagnostics { get; } = new GeneticsDiagnostics();

        public GeneticsManager(IEventBus eventBus = null)
        {
            this.eventBus = eventBus;
        }

        public bool RegisterDefinition(GenomeDefinition definition)
        {
            if (definition == null || definitions.ContainsKey(definition.DefinitionId)) return false;
            definitions.Add(definition.DefinitionId, definition);
            Diagnostics.RecordDefinitions(definitions.Count);
            return true;
        }

        public GenomeInstance GenerateGenome(string definitionId, string genomeId, int seed)
        {
            if (!definitions.TryGetValue(definitionId, out GenomeDefinition definition)) return null;
            GenomeInstance genome = engine.GenerateGenome(definition, genomeId, seed);
            genomes[genomeId] = genome;
            Diagnostics.RecordGenerated();
            eventBus?.Publish(new GenomeGenerated(genomeId));
            return genome;
        }

        public GenomeInstance InheritGenome(string definitionId, string genomeId, string maternalGenomeId, string paternalGenomeId, int seed)
        {
            if (!definitions.TryGetValue(definitionId, out GenomeDefinition definition)) return null;
            genomes.TryGetValue(maternalGenomeId ?? string.Empty, out GenomeInstance maternal);
            genomes.TryGetValue(paternalGenomeId ?? string.Empty, out GenomeInstance paternal);
            GenomeInstance child = engine.InheritGenome(definition, genomeId, maternal, paternal, seed);
            genomes[genomeId] = child;
            Diagnostics.RecordInherited();
            eventBus?.Publish(new GenomeGenerated(genomeId));
            return child;
        }

        public MutationKind MutateGenome(string genomeId, int seed)
        {
            if (!genomes.TryGetValue(genomeId, out GenomeInstance genome) || !definitions.TryGetValue(genome.DefinitionId, out GenomeDefinition definition)) return MutationKind.None;
            MutationKind kind = engine.MutateGenome(definition, genome, seed);
            if (kind != MutationKind.None)
            {
                Diagnostics.RecordMutated();
                eventBus?.Publish(new MutationOccurred(genomeId, kind));
                eventBus?.Publish(new GeneticsUpdated(genomeId));
            }
            return kind;
        }

        public IReadOnlyDictionary<string, double> CalculateTraits(string genomeId)
        {
            if (!genomes.TryGetValue(genomeId, out GenomeInstance genome) || !definitions.TryGetValue(genome.DefinitionId, out GenomeDefinition definition)) return new Dictionary<string, double>();
            Diagnostics.RecordTraitCalculation();
            eventBus?.Publish(new TraitsCalculated(genomeId));
            return engine.CalculateTraits(definition, genome);
        }

        public GenomeInstance QueryGenome(string genomeId)
        {
            return genomes.TryGetValue(genomeId, out GenomeInstance genome) ? genome : null;
        }

        public GeneticsStatistics QueryGeneticStatistics()
        {
            Dictionary<string, double> averages = new Dictionary<string, double>();
            int mutationCount = 0;
            foreach (GenomeInstance genome in genomes.Values)
            {
                mutationCount += genome.MutationHistory.Count;
                foreach (KeyValuePair<string, double> trait in genome.Values)
                {
                    averages.TryGetValue(trait.Key, out double total);
                    averages[trait.Key] = total + trait.Value;
                }
            }

            List<string> keys = new List<string>(averages.Keys);
            for (int i = 0; i < keys.Count; i++) averages[keys[i]] = genomes.Count == 0 ? 0d : averages[keys[i]] / genomes.Count;

            return new GeneticsStatistics(genomes.Count, mutationCount, CalculateDiversity(), averages);
        }

        private double CalculateDiversity()
        {
            if (genomes.Count <= 1) return 0d;
            HashSet<string> signatures = new HashSet<string>();
            foreach (GenomeInstance genome in genomes.Values)
            {
                List<string> parts = new List<string>();
                foreach (KeyValuePair<string, double> value in genome.Values) parts.Add(value.Key + ":" + value.Value.ToString("0.000"));
                parts.Sort(StringComparer.Ordinal);
                signatures.Add(string.Join("|", parts));
            }
            return (double)signatures.Count / genomes.Count;
        }
    }

    internal struct DeterministicRandom
    {
        private uint state;

        public DeterministicRandom(int seed)
        {
            state = seed == 0 ? 2166136261u : unchecked((uint)seed);
        }

        public double NextDouble()
        {
            state = unchecked(state * 1664525u + 1013904223u);
            return state / (double)uint.MaxValue;
        }
    }

    public readonly struct GenomeGenerated : IGameplayEvent, IBeeEvent { public string GenomeId { get; } public GenomeGenerated(string genomeId) { GenomeId = genomeId; } }
    public readonly struct MutationOccurred : IGameplayEvent, IBeeEvent { public string GenomeId { get; } public MutationKind Kind { get; } public MutationOccurred(string genomeId, MutationKind kind) { GenomeId = genomeId; Kind = kind; } }
    public readonly struct TraitsCalculated : IGameplayEvent, IBeeEvent { public string GenomeId { get; } public TraitsCalculated(string genomeId) { GenomeId = genomeId; } }
    public readonly struct GeneticsUpdated : IGameplayEvent, IBeeEvent { public string GenomeId { get; } public GeneticsUpdated(string genomeId) { GenomeId = genomeId; } }
}
