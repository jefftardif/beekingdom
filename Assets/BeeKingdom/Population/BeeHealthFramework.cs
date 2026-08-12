using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Population
{
    public enum HealthState { Perfect, Healthy, Tired, Injured, Sick, Recovering, Critical, Dead }
    public enum InjuryKind { Minor, Severe, Permanent, Combat, Environmental }
    public enum DiseaseKind { Infection, Parasite, Poisoning, Seasonal, Rare }

    public sealed class HealthDefinition
    {
        public string DefinitionId { get; }
        public double MaximumHealth { get; }
        public double RecoveryRate { get; }
        public double CriticalThreshold { get; }
        public double TiredThreshold { get; }
        public double Resistance { get; }

        public HealthDefinition(string definitionId, double maximumHealth, double recoveryRate, double criticalThreshold, double tiredThreshold, double resistance)
        {
            DefinitionId = string.IsNullOrWhiteSpace(definitionId) ? throw new ArgumentException("Definition id is required.", nameof(definitionId)) : definitionId;
            MaximumHealth = maximumHealth <= 0d ? 1d : maximumHealth;
            RecoveryRate = Math.Max(0d, recoveryRate);
            CriticalThreshold = Clamp(criticalThreshold, 0d, MaximumHealth);
            TiredThreshold = Clamp(tiredThreshold, 0d, MaximumHealth);
            Resistance = Clamp01(resistance);
        }

        private static double Clamp(double value, double minimum, double maximum) => value < minimum ? minimum : value > maximum ? maximum : value;
        private static double Clamp01(double value) => value < 0d ? 0d : value > 1d ? 1d : value;
    }

    public sealed class InjuryRecord
    {
        public string InjuryId { get; }
        public InjuryKind Kind { get; }
        public double Severity { get; private set; }
        public double RemainingDurationDays { get; private set; }
        public double RecoveryRate { get; }
        public string EffectId { get; }

        public InjuryRecord(string injuryId, InjuryKind kind, double severity, double durationDays, double recoveryRate, string effectId = "")
        {
            InjuryId = string.IsNullOrWhiteSpace(injuryId) ? throw new ArgumentException("Injury id is required.", nameof(injuryId)) : injuryId;
            Kind = kind;
            Severity = Clamp01(severity);
            RemainingDurationDays = Math.Max(0d, durationDays);
            RecoveryRate = Math.Max(0d, recoveryRate);
            EffectId = effectId ?? string.Empty;
        }

        public void Recover(double days)
        {
            RemainingDurationDays = Math.Max(0d, RemainingDurationDays - Math.Max(0d, days) * RecoveryRate);
            if (RemainingDurationDays <= 0d) Severity = 0d;
        }

        private static double Clamp01(double value) => value < 0d ? 0d : value > 1d ? 1d : value;
    }

    public sealed class DiseaseRecord
    {
        public string DiseaseId { get; }
        public DiseaseKind Kind { get; }
        public double Severity { get; private set; }
        public double ResistancePenalty { get; }

        public DiseaseRecord(string diseaseId, DiseaseKind kind, double severity, double resistancePenalty)
        {
            DiseaseId = string.IsNullOrWhiteSpace(diseaseId) ? throw new ArgumentException("Disease id is required.", nameof(diseaseId)) : diseaseId;
            Kind = kind;
            Severity = Clamp01(severity);
            ResistancePenalty = Clamp01(resistancePenalty);
        }

        public void Cure() => Severity = 0d;
        private static double Clamp01(double value) => value < 0d ? 0d : value > 1d ? 1d : value;
    }

    public sealed class BeeHealthRecord
    {
        private readonly List<InjuryRecord> injuries = new List<InjuryRecord>();
        private readonly List<DiseaseRecord> diseases = new List<DiseaseRecord>();

        public string BeeId { get; }
        public string DefinitionId { get; }
        public double CurrentHealth { get; private set; }
        public HealthState State { get; private set; }
        public IReadOnlyList<InjuryRecord> Injuries => injuries;
        public IReadOnlyList<DiseaseRecord> Diseases => diseases;

        public BeeHealthRecord(string beeId, HealthDefinition definition)
        {
            BeeId = beeId ?? string.Empty;
            DefinitionId = definition.DefinitionId;
            CurrentHealth = definition.MaximumHealth;
            State = HealthState.Perfect;
        }

        public void ApplyDamage(double amount)
        {
            CurrentHealth = Math.Max(0d, CurrentHealth - Math.Max(0d, amount));
        }

        public void Heal(double amount, double maximumHealth)
        {
            CurrentHealth = Math.Min(maximumHealth, CurrentHealth + Math.Max(0d, amount));
        }

        public void AddInjury(InjuryRecord injury)
        {
            if (injury != null) injuries.Add(injury);
        }

        public void AddDisease(DiseaseRecord disease)
        {
            if (disease != null) diseases.Add(disease);
        }

        public bool CureDisease(string diseaseId)
        {
            for (int i = 0; i < diseases.Count; i++)
            {
                if (diseases[i].DiseaseId != diseaseId) continue;
                diseases[i].Cure();
                return true;
            }
            return false;
        }

        public void SetState(HealthState state) => State = state;
    }

    public readonly struct HealthEvaluationContext
    {
        public string BeeId { get; }
        public string GenomeId { get; }
        public double RestFactor { get; }
        public double NutritionFactor { get; }
        public double CareFactor { get; }
        public double EnvironmentFactor { get; }
        public double ColonyCapacityFactor { get; }

        public HealthEvaluationContext(string beeId, string genomeId = "", double restFactor = 1d, double nutritionFactor = 1d, double careFactor = 1d, double environmentFactor = 1d, double colonyCapacityFactor = 1d)
        {
            BeeId = beeId ?? string.Empty;
            GenomeId = genomeId ?? string.Empty;
            RestFactor = Clamp01(restFactor);
            NutritionFactor = Clamp01(nutritionFactor);
            CareFactor = Clamp01(careFactor);
            EnvironmentFactor = Clamp01(environmentFactor);
            ColonyCapacityFactor = Clamp01(colonyCapacityFactor);
        }

        private static double Clamp01(double value) => value < 0d ? 0d : value > 1d ? 1d : value;
    }

    public sealed class HealthEvaluator
    {
        public HealthState Evaluate(HealthDefinition definition, BeeHealthRecord record)
        {
            if (record.CurrentHealth <= 0d) return HealthState.Dead;
            if (record.CurrentHealth <= definition.CriticalThreshold) return HealthState.Critical;
            if (HasActiveDisease(record)) return HealthState.Sick;
            if (HasActiveInjury(record)) return HealthState.Injured;
            if (record.CurrentHealth < definition.TiredThreshold) return HealthState.Tired;
            if (record.CurrentHealth >= definition.MaximumHealth) return HealthState.Perfect;
            return HealthState.Healthy;
        }

        private static bool HasActiveInjury(BeeHealthRecord record)
        {
            for (int i = 0; i < record.Injuries.Count; i++) if (record.Injuries[i].Severity > 0d) return true;
            return false;
        }

        private static bool HasActiveDisease(BeeHealthRecord record)
        {
            for (int i = 0; i < record.Diseases.Count; i++) if (record.Diseases[i].Severity > 0d) return true;
            return false;
        }
    }

    public sealed class BeeHealthEngine
    {
        private readonly HealthEvaluator evaluator = new HealthEvaluator();

        public HealthState EvaluateHealth(HealthDefinition definition, BeeHealthRecord record)
        {
            return evaluator.Evaluate(definition, record);
        }

        public void Recover(HealthDefinition definition, BeeHealthRecord record, HealthEvaluationContext context, double days)
        {
            double recoveryFactor = (context.RestFactor + context.NutritionFactor + context.CareFactor + context.EnvironmentFactor + context.ColonyCapacityFactor) / 5d;
            record.Heal(definition.RecoveryRate * Math.Max(0d, days) * recoveryFactor, definition.MaximumHealth);
            for (int i = 0; i < record.Injuries.Count; i++) record.Injuries[i].Recover(days * recoveryFactor);
        }
    }

    public sealed class BeeHealthDiagnostics
    {
        public int DefinitionsRegistered { get; private set; }
        public int RecordsCreated { get; private set; }
        public int DamageApplications { get; private set; }
        public int HealApplications { get; private set; }
        public int DiseasesApplied { get; private set; }
        public int DiseasesCured { get; private set; }
        public int Evaluations { get; private set; }

        public void RecordDefinitions(int count) => DefinitionsRegistered = count;
        public void RecordCreated() => RecordsCreated++;
        public void RecordDamage() => DamageApplications++;
        public void RecordHeal() => HealApplications++;
        public void RecordDiseaseApplied() => DiseasesApplied++;
        public void RecordDiseaseCured() => DiseasesCured++;
        public void RecordEvaluation() => Evaluations++;
    }

    public sealed class BeeHealthManager
    {
        private readonly Dictionary<string, HealthDefinition> definitions = new Dictionary<string, HealthDefinition>();
        private readonly Dictionary<string, BeeHealthRecord> records = new Dictionary<string, BeeHealthRecord>();
        private readonly BeeHealthEngine engine = new BeeHealthEngine();
        private readonly IEventBus eventBus;

        public BeeHealthDiagnostics Diagnostics { get; } = new BeeHealthDiagnostics();

        public BeeHealthManager(IEventBus eventBus = null)
        {
            this.eventBus = eventBus;
        }

        public bool RegisterHealthDefinition(HealthDefinition definition)
        {
            if (definition == null || definitions.ContainsKey(definition.DefinitionId)) return false;
            definitions.Add(definition.DefinitionId, definition);
            Diagnostics.RecordDefinitions(definitions.Count);
            return true;
        }

        public BeeHealthRecord QueryHealth(string beeId)
        {
            return records.TryGetValue(beeId ?? string.Empty, out BeeHealthRecord record) ? record : null;
        }

        public BeeHealthRecord CreateHealthRecord(string beeId, string definitionId)
        {
            if (!definitions.TryGetValue(definitionId, out HealthDefinition definition)) return null;
            BeeHealthRecord record = new BeeHealthRecord(beeId, definition);
            records[beeId ?? string.Empty] = record;
            Diagnostics.RecordCreated();
            return record;
        }

        public HealthState EvaluateHealth(HealthEvaluationContext context)
        {
            BeeHealthRecord record = QueryHealth(context.BeeId);
            if (record == null || !definitions.TryGetValue(record.DefinitionId, out HealthDefinition definition)) return HealthState.Dead;
            HealthState previous = record.State;
            engine.Recover(definition, record, context, 0d);
            HealthState state = engine.EvaluateHealth(definition, record);
            record.SetState(state);
            Diagnostics.RecordEvaluation();
            eventBus?.Publish(new BeeHealthUpdated(context.BeeId));
            if (previous != state) eventBus?.Publish(new HealthStateChanged(context.BeeId, state));
            return state;
        }

        public bool ApplyDamage(string beeId, double amount, InjuryRecord injury = null)
        {
            BeeHealthRecord record = QueryHealth(beeId);
            if (record == null) return false;
            record.ApplyDamage(amount);
            record.AddInjury(injury);
            Diagnostics.RecordDamage();
            eventBus?.Publish(new BeeInjured(beeId));
            EvaluateHealth(new HealthEvaluationContext(beeId));
            return true;
        }

        public bool HealBee(string beeId, double amount)
        {
            BeeHealthRecord record = QueryHealth(beeId);
            if (record == null || !definitions.TryGetValue(record.DefinitionId, out HealthDefinition definition)) return false;
            record.Heal(amount, definition.MaximumHealth);
            Diagnostics.RecordHeal();
            eventBus?.Publish(new BeeRecovered(beeId));
            EvaluateHealth(new HealthEvaluationContext(beeId));
            return true;
        }

        public bool ApplyDisease(string beeId, DiseaseRecord disease)
        {
            BeeHealthRecord record = QueryHealth(beeId);
            if (record == null || disease == null) return false;
            record.AddDisease(disease);
            Diagnostics.RecordDiseaseApplied();
            eventBus?.Publish(new DiseaseApplied(beeId, disease.DiseaseId));
            EvaluateHealth(new HealthEvaluationContext(beeId));
            return true;
        }

        public bool CureDisease(string beeId, string diseaseId)
        {
            BeeHealthRecord record = QueryHealth(beeId);
            if (record == null || !record.CureDisease(diseaseId)) return false;
            Diagnostics.RecordDiseaseCured();
            eventBus?.Publish(new DiseaseCured(beeId, diseaseId));
            EvaluateHealth(new HealthEvaluationContext(beeId));
            return true;
        }
    }

    public readonly struct BeeInjured : IGameplayEvent, IBeeEvent { public string BeeId { get; } public BeeInjured(string beeId) { BeeId = beeId; } }
    public readonly struct BeeRecovered : IGameplayEvent, IBeeEvent { public string BeeId { get; } public BeeRecovered(string beeId) { BeeId = beeId; } }
    public readonly struct DiseaseApplied : IGameplayEvent, IBeeEvent { public string BeeId { get; } public string DiseaseId { get; } public DiseaseApplied(string beeId, string diseaseId) { BeeId = beeId; DiseaseId = diseaseId; } }
    public readonly struct DiseaseCured : IGameplayEvent, IBeeEvent { public string BeeId { get; } public string DiseaseId { get; } public DiseaseCured(string beeId, string diseaseId) { BeeId = beeId; DiseaseId = diseaseId; } }
    public readonly struct HealthStateChanged : IGameplayEvent, IBeeEvent { public string BeeId { get; } public HealthState State { get; } public HealthStateChanged(string beeId, HealthState state) { BeeId = beeId; State = state; } }
    public readonly struct BeeHealthUpdated : IGameplayEvent, IBeeEvent { public string BeeId { get; } public BeeHealthUpdated(string beeId) { BeeId = beeId; } }
}
