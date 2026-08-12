using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Population
{
    public enum LifecycleStage { Egg, Larva, Pupa, YoungAdult, Adult, Experienced, Elder, Death }
    public enum BiologicalState { Healthy, Hungry, Exhausted, Injured, Sick, Recovering, Dying }
    public enum MortalityCause { OldAge, Disease, Famine, Combat, Disaster, SpecialEvent }

    public sealed class LifecycleTransition
    {
        public LifecycleStage From { get; }
        public LifecycleStage To { get; }
        public double RequiredBiologicalAgeDays { get; }

        public LifecycleTransition(LifecycleStage from, LifecycleStage to, double requiredBiologicalAgeDays)
        {
            From = from;
            To = to;
            RequiredBiologicalAgeDays = requiredBiologicalAgeDays < 0d ? 0d : requiredBiologicalAgeDays;
        }
    }

    public sealed class LifecycleDefinition
    {
        public string DefinitionId { get; }
        public IReadOnlyList<LifecycleTransition> Transitions { get; }
        public double LongevityDays { get; }

        public LifecycleDefinition(string definitionId, IReadOnlyList<LifecycleTransition> transitions, double longevityDays)
        {
            DefinitionId = string.IsNullOrWhiteSpace(definitionId) ? throw new ArgumentException("Definition id is required.", nameof(definitionId)) : definitionId;
            Transitions = transitions ?? Array.Empty<LifecycleTransition>();
            LongevityDays = longevityDays <= 0d ? 1d : longevityDays;
        }
    }

    public sealed class LifecycleStageRecord
    {
        public string BeeId { get; }
        public string DefinitionId { get; }
        public LifecycleStage Stage { get; private set; }
        public BiologicalState BiologicalState { get; private set; }
        public double ChronologicalAgeDays { get; private set; }
        public double BiologicalAgeDays { get; private set; }
        public bool IsDead => Stage == LifecycleStage.Death;

        public LifecycleStageRecord(string beeId, string definitionId)
        {
            BeeId = beeId ?? string.Empty;
            DefinitionId = definitionId ?? string.Empty;
            Stage = LifecycleStage.Egg;
            BiologicalState = BiologicalState.Healthy;
        }

        public void AdvanceAge(double days, double agingMultiplier)
        {
            ChronologicalAgeDays += Math.Max(0d, days);
            BiologicalAgeDays += Math.Max(0d, days) * Math.Max(0d, agingMultiplier);
        }

        public void ChangeStage(LifecycleStage stage) => Stage = stage;
        public void ChangeBiologicalState(BiologicalState state) => BiologicalState = state;
    }

    public sealed class LifecycleDiagnostics
    {
        public int DefinitionsRegistered { get; private set; }
        public int Updates { get; private set; }
        public int StageChanges { get; private set; }
        public int Deaths { get; private set; }
        public void RecordDefinitions(int count) => DefinitionsRegistered = count;
        public void RecordUpdate() => Updates++;
        public void RecordStageChange() => StageChanges++;
        public void RecordDeath() => Deaths++;
    }

    public sealed class LifecycleEngine
    {
        public double CalculateBiologicalAge(double chronologicalAgeDays, double agingMultiplier)
        {
            return Math.Max(0d, chronologicalAgeDays) * Math.Max(0d, agingMultiplier);
        }

        public LifecycleStage ResolveStage(LifecycleDefinition definition, LifecycleStage current, double biologicalAgeDays)
        {
            LifecycleStage resolved = current;
            for (int i = 0; i < definition.Transitions.Count; i++)
            {
                LifecycleTransition transition = definition.Transitions[i];
                if (transition.From == resolved && biologicalAgeDays >= transition.RequiredBiologicalAgeDays)
                {
                    resolved = transition.To;
                }
            }
            if (biologicalAgeDays >= definition.LongevityDays) resolved = LifecycleStage.Death;
            return resolved;
        }
    }

    public sealed class BeeLifecycleManager
    {
        private readonly Dictionary<string, LifecycleDefinition> definitions = new Dictionary<string, LifecycleDefinition>();
        private readonly Dictionary<string, LifecycleStageRecord> lifecycles = new Dictionary<string, LifecycleStageRecord>();
        private readonly LifecycleEngine engine = new LifecycleEngine();
        private readonly IEventBus eventBus;

        public LifecycleDiagnostics Diagnostics { get; } = new LifecycleDiagnostics();
        public BeeLifecycleManager(IEventBus eventBus = null) { this.eventBus = eventBus; }

        public bool RegisterLifecycleDefinition(LifecycleDefinition definition)
        {
            if (definition == null || definitions.ContainsKey(definition.DefinitionId)) return false;
            definitions.Add(definition.DefinitionId, definition);
            Diagnostics.RecordDefinitions(definitions.Count);
            return true;
        }

        public LifecycleStageRecord RegisterBee(string beeId, string definitionId)
        {
            LifecycleStageRecord record = new LifecycleStageRecord(beeId, definitionId);
            lifecycles[beeId] = record;
            eventBus?.Publish(new BeeBorn(beeId));
            return record;
        }

        public bool AdvanceLifecycle(string beeId, double days, double agingMultiplier = 1d)
        {
            if (!lifecycles.TryGetValue(beeId, out LifecycleStageRecord record) || !definitions.TryGetValue(record.DefinitionId, out LifecycleDefinition definition)) return false;
            LifecycleStage previous = record.Stage;
            record.AdvanceAge(days, agingMultiplier);
            LifecycleStage next = engine.ResolveStage(definition, previous, record.BiologicalAgeDays);
            if (next != previous) ChangeLifecycleStage(beeId, next);
            Diagnostics.RecordUpdate();
            eventBus?.Publish(new LifecycleUpdated(beeId));
            return true;
        }

        public bool ChangeLifecycleStage(string beeId, LifecycleStage stage)
        {
            if (!lifecycles.TryGetValue(beeId, out LifecycleStageRecord record)) return false;
            record.ChangeStage(stage);
            Diagnostics.RecordStageChange();
            eventBus?.Publish(new LifecycleStageChanged(beeId, stage));
            if (stage == LifecycleStage.Adult) eventBus?.Publish(new BeeAdult(beeId));
            if (stage == LifecycleStage.Elder) eventBus?.Publish(new BeeElder(beeId));
            if (stage == LifecycleStage.Death) { Diagnostics.RecordDeath(); eventBus?.Publish(new BeeDied(beeId, MortalityCause.OldAge)); }
            return true;
        }

        public double CalculateBiologicalAge(double chronologicalAgeDays, double agingMultiplier) => engine.CalculateBiologicalAge(chronologicalAgeDays, agingMultiplier);
        public LifecycleStageRecord QueryLifecycle(string beeId) => lifecycles.TryGetValue(beeId, out LifecycleStageRecord record) ? record : null;
        public LifecycleStage QueryStage(string beeId) => QueryLifecycle(beeId)?.Stage ?? LifecycleStage.Death;
    }

    public readonly struct BeeBorn : IGameplayEvent, IBeeEvent { public string BeeId { get; } public BeeBorn(string beeId) { BeeId = beeId; } }
    public readonly struct LifecycleStageChanged : IGameplayEvent, IBeeEvent { public string BeeId { get; } public LifecycleStage Stage { get; } public LifecycleStageChanged(string beeId, LifecycleStage stage) { BeeId = beeId; Stage = stage; } }
    public readonly struct BeeAdult : IGameplayEvent, IBeeEvent { public string BeeId { get; } public BeeAdult(string beeId) { BeeId = beeId; } }
    public readonly struct BeeElder : IGameplayEvent, IBeeEvent { public string BeeId { get; } public BeeElder(string beeId) { BeeId = beeId; } }
    public readonly struct BeeDied : IGameplayEvent, IBeeEvent { public string BeeId { get; } public MortalityCause Cause { get; } public BeeDied(string beeId, MortalityCause cause) { BeeId = beeId; Cause = cause; } }
    public readonly struct LifecycleUpdated : IGameplayEvent, IBeeEvent { public string BeeId { get; } public LifecycleUpdated(string beeId) { BeeId = beeId; } }
}
