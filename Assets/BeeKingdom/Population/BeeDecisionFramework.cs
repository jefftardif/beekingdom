using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Population
{
    public enum BeeIntent { Idle, Rest, Eat, Drink, Build, Gather, Transport, Nurse, Clean, Defend, Patrol, Explore, Repair, Ventilate, ProduceWax, ProcessFood, FollowQueen, Escape, AssistBee, Custom }

    public sealed class DecisionCandidate
    {
        public BeeIntent Intent { get; }
        public double BaseWeight { get; }
        public bool IsEmergency { get; }
        public bool IsValid { get; }

        public DecisionCandidate(BeeIntent intent, double baseWeight, bool isEmergency = false, bool isValid = true)
        {
            Intent = intent;
            BaseWeight = Math.Max(0d, baseWeight);
            IsEmergency = isEmergency;
            IsValid = isValid;
        }
    }

    public readonly struct DecisionContext
    {
        public string BeeId { get; }
        public BeeCaste Caste { get; }
        public double NeedsWeight { get; }
        public double HealthWeight { get; }
        public double FatigueWeight { get; }
        public double PersonalityWeight { get; }
        public double ExperienceWeight { get; }
        public double MemoryWeight { get; }
        public double ColonyPriorityWeight { get; }
        public double QueenObjectiveWeight { get; }
        public double PlayerStrategyWeight { get; }

        public DecisionContext(string beeId, BeeCaste caste, double needsWeight = 1d, double healthWeight = 1d, double fatigueWeight = 1d, double personalityWeight = 1d, double experienceWeight = 1d, double memoryWeight = 1d, double colonyPriorityWeight = 1d, double queenObjectiveWeight = 1d, double playerStrategyWeight = 1d)
        {
            BeeId = beeId ?? string.Empty;
            Caste = caste;
            NeedsWeight = Clamp01(needsWeight);
            HealthWeight = Clamp01(healthWeight);
            FatigueWeight = Clamp01(fatigueWeight);
            PersonalityWeight = Clamp01(personalityWeight);
            ExperienceWeight = Clamp01(experienceWeight);
            MemoryWeight = Clamp01(memoryWeight);
            ColonyPriorityWeight = Clamp01(colonyPriorityWeight);
            QueenObjectiveWeight = Clamp01(queenObjectiveWeight);
            PlayerStrategyWeight = Clamp01(playerStrategyWeight);
        }

        private static double Clamp01(double value) => value < 0d ? 0d : value > 1d ? 1d : value;
    }

    public sealed class DecisionScore
    {
        public BeeIntent Intent { get; }
        public double Score { get; }
        public bool IsEmergency { get; }

        public DecisionScore(BeeIntent intent, double score, bool isEmergency)
        {
            Intent = intent;
            Score = score;
            IsEmergency = isEmergency;
        }
    }

    public sealed class BeeDecisionEngine
    {
        public IReadOnlyList<DecisionCandidate> GenerateIntentions(IReadOnlyList<DecisionCandidate> candidates)
        {
            List<DecisionCandidate> valid = new List<DecisionCandidate>();
            for (int i = 0; i < candidates.Count; i++) if (candidates[i].IsValid) valid.Add(candidates[i]);
            valid.Sort((left, right) => left.Intent.CompareTo(right.Intent));
            return valid;
        }

        public DecisionScore CalculateDecisionScore(DecisionCandidate candidate, DecisionContext context)
        {
            double influence = (
                context.NeedsWeight +
                context.HealthWeight +
                context.FatigueWeight +
                context.PersonalityWeight +
                context.ExperienceWeight +
                context.MemoryWeight +
                context.ColonyPriorityWeight +
                context.QueenObjectiveWeight +
                context.PlayerStrategyWeight) / 9d;
            double emergencyBoost = candidate.IsEmergency ? 10d : 0d;
            return new DecisionScore(candidate.Intent, candidate.BaseWeight * influence + emergencyBoost, candidate.IsEmergency);
        }

        public DecisionScore SelectBestDecision(IReadOnlyList<DecisionScore> scores)
        {
            DecisionScore best = null;
            for (int i = 0; i < scores.Count; i++)
            {
                if (best == null || scores[i].Score > best.Score || (Math.Abs(scores[i].Score - best.Score) < 0.0001d && scores[i].Intent < best.Intent)) best = scores[i];
            }
            return best ?? new DecisionScore(BeeIntent.Idle, 0d, false);
        }
    }

    public sealed class BeeDecisionDiagnostics
    {
        public int Evaluations { get; private set; }
        public int Generated { get; private set; }
        public int Changes { get; private set; }
        public int Interruptions { get; private set; }
        public int Emergencies { get; private set; }
        public void RecordEvaluation() => Evaluations++;
        public void RecordGenerated(int count) => Generated += Math.Max(0, count);
        public void RecordChange() => Changes++;
        public void RecordInterruption() => Interruptions++;
        public void RecordEmergency() => Emergencies++;
    }

    public sealed class BeeDecisionManager
    {
        private readonly Dictionary<string, DecisionScore> current = new Dictionary<string, DecisionScore>();
        private readonly BeeDecisionEngine engine = new BeeDecisionEngine();
        private readonly IEventBus eventBus;

        public BeeDecisionDiagnostics Diagnostics { get; } = new BeeDecisionDiagnostics();

        public BeeDecisionManager(IEventBus eventBus = null) { this.eventBus = eventBus; }

        public DecisionScore EvaluateDecision(DecisionContext context, IReadOnlyList<DecisionCandidate> candidates)
        {
            IReadOnlyList<DecisionCandidate> intentions = GenerateIntentions(candidates);
            List<DecisionScore> scores = new List<DecisionScore>();
            for (int i = 0; i < intentions.Count; i++) scores.Add(CalculateDecisionScore(intentions[i], context));
            DecisionScore selected = SelectBestDecision(scores);
            current.TryGetValue(context.BeeId, out DecisionScore previous);
            current[context.BeeId] = selected;
            Diagnostics.RecordEvaluation();
            eventBus?.Publish(new DecisionGenerated(context.BeeId, selected.Intent));
            if (previous == null || previous.Intent != selected.Intent)
            {
                Diagnostics.RecordChange();
                eventBus?.Publish(new DecisionChanged(context.BeeId, selected.Intent));
            }
            if (selected.IsEmergency)
            {
                Diagnostics.RecordEmergency();
                eventBus?.Publish(new EmergencyDecisionActivated(context.BeeId, selected.Intent));
            }
            return selected;
        }

        public IReadOnlyList<DecisionCandidate> GenerateIntentions(IReadOnlyList<DecisionCandidate> candidates)
        {
            IReadOnlyList<DecisionCandidate> generated = engine.GenerateIntentions(candidates ?? Array.Empty<DecisionCandidate>());
            Diagnostics.RecordGenerated(generated.Count);
            return generated;
        }

        public DecisionScore CalculateDecisionScore(DecisionCandidate candidate, DecisionContext context) => engine.CalculateDecisionScore(candidate, context);
        public DecisionScore SelectBestDecision(IReadOnlyList<DecisionScore> scores) => engine.SelectBestDecision(scores ?? Array.Empty<DecisionScore>());

        public bool InterruptDecision(string beeId, string reason)
        {
            if (!current.ContainsKey(beeId ?? string.Empty)) return false;
            Diagnostics.RecordInterruption();
            eventBus?.Publish(new DecisionInterrupted(beeId, reason ?? string.Empty));
            return true;
        }

        public bool CompleteDecision(string beeId)
        {
            bool removed = current.Remove(beeId ?? string.Empty);
            if (removed) eventBus?.Publish(new DecisionCompleted(beeId));
            return removed;
        }

        public DecisionScore QueryCurrentDecision(string beeId) => current.TryGetValue(beeId ?? string.Empty, out DecisionScore score) ? score : null;
    }

    public readonly struct DecisionGenerated : IGameplayEvent, IBeeEvent { public string BeeId { get; } public BeeIntent Intent { get; } public DecisionGenerated(string beeId, BeeIntent intent) { BeeId = beeId; Intent = intent; } }
    public readonly struct DecisionChanged : IGameplayEvent, IBeeEvent { public string BeeId { get; } public BeeIntent Intent { get; } public DecisionChanged(string beeId, BeeIntent intent) { BeeId = beeId; Intent = intent; } }
    public readonly struct DecisionInterrupted : IGameplayEvent, IBeeEvent { public string BeeId { get; } public string Reason { get; } public DecisionInterrupted(string beeId, string reason) { BeeId = beeId; Reason = reason; } }
    public readonly struct DecisionCompleted : IGameplayEvent, IBeeEvent { public string BeeId { get; } public DecisionCompleted(string beeId) { BeeId = beeId; } }
    public readonly struct EmergencyDecisionActivated : IGameplayEvent, IBeeEvent { public string BeeId { get; } public BeeIntent Intent { get; } public EmergencyDecisionActivated(string beeId, BeeIntent intent) { BeeId = beeId; Intent = intent; } }
}
