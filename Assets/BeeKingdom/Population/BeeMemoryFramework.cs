using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Population
{
    public enum MemoryType { LocationMemory, ResourceMemory, DangerMemory, RouteMemory, TaskMemory, SocialMemory, EnvironmentMemory, EventMemory, Custom }

    public sealed class MemoryDefinition
    {
        public string DefinitionId { get; }
        public MemoryType Type { get; }
        public double DefaultExpirationDays { get; }
        public double ForgetRate { get; }
        public double ReinforcementAmount { get; }
        public bool Permanent { get; }

        public MemoryDefinition(string definitionId, MemoryType type, double defaultExpirationDays, double forgetRate, double reinforcementAmount, bool permanent = false)
        {
            DefinitionId = string.IsNullOrWhiteSpace(definitionId) ? throw new ArgumentException("Definition id is required.", nameof(definitionId)) : definitionId;
            Type = type;
            DefaultExpirationDays = Math.Max(0d, defaultExpirationDays);
            ForgetRate = Math.Max(0d, forgetRate);
            ReinforcementAmount = Math.Max(0d, reinforcementAmount);
            Permanent = permanent;
        }
    }

    public sealed class MemoryEntry
    {
        public string MemoryId { get; }
        public string BeeId { get; }
        public MemoryType MemoryType { get; }
        public string TargetId { get; }
        public double Timestamp { get; }
        public double Importance { get; private set; }
        public double Confidence { get; private set; }
        public double Expiration { get; private set; }
        public double LastAccess { get; private set; }
        public bool Permanent { get; }

        public MemoryEntry(string memoryId, string beeId, MemoryType memoryType, string targetId, double timestamp, double importance, double confidence, double expiration, bool permanent)
        {
            MemoryId = string.IsNullOrWhiteSpace(memoryId) ? throw new ArgumentException("Memory id is required.", nameof(memoryId)) : memoryId;
            BeeId = beeId ?? string.Empty;
            MemoryType = memoryType;
            TargetId = targetId ?? string.Empty;
            Timestamp = Math.Max(0d, timestamp);
            Importance = Clamp01(importance);
            Confidence = Clamp01(confidence);
            Expiration = Math.Max(Timestamp, expiration);
            LastAccess = Timestamp;
            Permanent = permanent;
        }

        public void Access(double time) => LastAccess = Math.Max(LastAccess, time);
        public void Reinforce(double amount, double time)
        {
            Importance = Clamp01(Importance + amount);
            Confidence = Clamp01(Confidence + amount * 0.5d);
            Access(time);
        }

        public void Decay(double amount)
        {
            if (Permanent) return;
            Importance = Clamp01(Importance - amount);
            Confidence = Clamp01(Confidence - amount * 0.5d);
        }

        public bool IsExpired(double time) => !Permanent && (time >= Expiration || Importance <= 0d || Confidence <= 0d);
        private static double Clamp01(double value) => value < 0d ? 0d : value > 1d ? 1d : value;
    }

    public sealed class MemoryProfile
    {
        private readonly List<MemoryEntry> memories = new List<MemoryEntry>();

        public string BeeId { get; }
        public IReadOnlyList<MemoryEntry> Memories => memories;

        public MemoryProfile(string beeId) { BeeId = beeId ?? string.Empty; }
        public void Add(MemoryEntry entry) { if (entry != null) memories.Add(entry); }
        public bool Remove(string memoryId)
        {
            for (int i = 0; i < memories.Count; i++)
            {
                if (memories[i].MemoryId != memoryId) continue;
                memories.RemoveAt(i);
                return true;
            }
            return false;
        }
    }

    public sealed class BeeMemoryEngine
    {
        public void ForgetExpired(MemoryDefinition definition, MemoryProfile profile, double currentTime, List<MemoryEntry> expired)
        {
            for (int i = profile.Memories.Count - 1; i >= 0; i--)
            {
                MemoryEntry entry = profile.Memories[i];
                entry.Decay(definition.ForgetRate);
                if (entry.IsExpired(currentTime)) expired.Add(entry);
            }
        }

        public MemoryEntry GetBestMemory(IReadOnlyList<MemoryEntry> entries, MemoryType type)
        {
            MemoryEntry best = null;
            double bestScore = double.MinValue;
            for (int i = 0; i < entries.Count; i++)
            {
                MemoryEntry entry = entries[i];
                if (entry.MemoryType != type) continue;
                double score = entry.Importance * 0.7d + entry.Confidence * 0.3d;
                if (score > bestScore) { bestScore = score; best = entry; }
            }
            return best;
        }
    }

    public sealed class BeeMemoryDiagnostics
    {
        public int DefinitionsRegistered { get; private set; }
        public int Created { get; private set; }
        public int Updated { get; private set; }
        public int Forgotten { get; private set; }
        public int Reinforced { get; private set; }
        public int Expired { get; private set; }

        public void RecordDefinitions(int count) => DefinitionsRegistered = count;
        public void RecordCreated() => Created++;
        public void RecordUpdated() => Updated++;
        public void RecordForgotten() => Forgotten++;
        public void RecordReinforced() => Reinforced++;
        public void RecordExpired() => Expired++;
    }

    public sealed class BeeMemoryManager
    {
        private readonly Dictionary<string, MemoryDefinition> definitions = new Dictionary<string, MemoryDefinition>();
        private readonly Dictionary<string, MemoryProfile> profiles = new Dictionary<string, MemoryProfile>();
        private readonly BeeMemoryEngine engine = new BeeMemoryEngine();
        private readonly IEventBus eventBus;
        private int sequence;

        public BeeMemoryDiagnostics Diagnostics { get; } = new BeeMemoryDiagnostics();

        public BeeMemoryManager(IEventBus eventBus = null) { this.eventBus = eventBus; }

        public bool RegisterMemoryDefinition(MemoryDefinition definition)
        {
            if (definition == null || definitions.ContainsKey(definition.DefinitionId)) return false;
            definitions.Add(definition.DefinitionId, definition);
            Diagnostics.RecordDefinitions(definitions.Count);
            return true;
        }

        public MemoryEntry Remember(string beeId, string definitionId, string targetId, double timestamp, double importance, double confidence)
        {
            if (!definitions.TryGetValue(definitionId, out MemoryDefinition definition)) return null;
            MemoryProfile profile = EnsureProfile(beeId);
            string memoryId = beeId + "-memory-" + (++sequence).ToString("D6");
            MemoryEntry entry = new MemoryEntry(memoryId, beeId, definition.Type, targetId, timestamp, importance, confidence, timestamp + definition.DefaultExpirationDays, definition.Permanent);
            profile.Add(entry);
            Diagnostics.RecordCreated();
            eventBus?.Publish(new MemoryCreated(beeId, memoryId));
            return entry;
        }

        public bool Forget(string beeId, string memoryId)
        {
            MemoryProfile profile = EnsureProfile(beeId);
            bool removed = profile.Remove(memoryId);
            if (removed)
            {
                Diagnostics.RecordForgotten();
                eventBus?.Publish(new MemoryForgotten(beeId, memoryId));
            }
            return removed;
        }

        public bool ReinforceMemory(string beeId, string memoryId, double time)
        {
            MemoryEntry entry = FindMemory(beeId, memoryId);
            if (entry == null) return false;
            MemoryDefinition definition = FindDefinition(entry.MemoryType);
            entry.Reinforce(definition?.ReinforcementAmount ?? 0.1d, time);
            Diagnostics.RecordReinforced();
            eventBus?.Publish(new MemoryReinforced(beeId, memoryId));
            eventBus?.Publish(new MemoryUpdated(beeId, memoryId));
            return true;
        }

        public IReadOnlyList<MemoryEntry> QueryMemories(string beeId, MemoryType? type = null)
        {
            MemoryProfile profile = EnsureProfile(beeId);
            List<MemoryEntry> result = new List<MemoryEntry>();
            for (int i = 0; i < profile.Memories.Count; i++)
            {
                if (type.HasValue && profile.Memories[i].MemoryType != type.Value) continue;
                result.Add(profile.Memories[i]);
            }
            result.Sort((left, right) => string.CompareOrdinal(left.MemoryId, right.MemoryId));
            return result;
        }

        public MemoryEntry GetBestMemory(string beeId, MemoryType type)
        {
            return engine.GetBestMemory(QueryMemories(beeId), type);
        }

        public void UpdateMemory(string beeId, string definitionId, double currentTime)
        {
            if (!definitions.TryGetValue(definitionId, out MemoryDefinition definition)) return;
            MemoryProfile profile = EnsureProfile(beeId);
            List<MemoryEntry> expired = new List<MemoryEntry>();
            engine.ForgetExpired(definition, profile, currentTime, expired);
            for (int i = 0; i < expired.Count; i++)
            {
                profile.Remove(expired[i].MemoryId);
                Diagnostics.RecordExpired();
                eventBus?.Publish(new MemoryExpired(beeId, expired[i].MemoryId));
            }
            Diagnostics.RecordUpdated();
        }

        private MemoryProfile EnsureProfile(string beeId)
        {
            string key = beeId ?? string.Empty;
            if (!profiles.TryGetValue(key, out MemoryProfile profile)) { profile = new MemoryProfile(key); profiles[key] = profile; }
            return profile;
        }

        private MemoryEntry FindMemory(string beeId, string memoryId)
        {
            IReadOnlyList<MemoryEntry> entries = QueryMemories(beeId);
            for (int i = 0; i < entries.Count; i++) if (entries[i].MemoryId == memoryId) return entries[i];
            return null;
        }

        private MemoryDefinition FindDefinition(MemoryType type)
        {
            foreach (MemoryDefinition definition in definitions.Values) if (definition.Type == type) return definition;
            return null;
        }
    }

    public readonly struct MemoryCreated : IGameplayEvent, IBeeEvent { public string BeeId { get; } public string MemoryId { get; } public MemoryCreated(string beeId, string memoryId) { BeeId = beeId; MemoryId = memoryId; } }
    public readonly struct MemoryUpdated : IGameplayEvent, IBeeEvent { public string BeeId { get; } public string MemoryId { get; } public MemoryUpdated(string beeId, string memoryId) { BeeId = beeId; MemoryId = memoryId; } }
    public readonly struct MemoryForgotten : IGameplayEvent, IBeeEvent { public string BeeId { get; } public string MemoryId { get; } public MemoryForgotten(string beeId, string memoryId) { BeeId = beeId; MemoryId = memoryId; } }
    public readonly struct MemoryReinforced : IGameplayEvent, IBeeEvent { public string BeeId { get; } public string MemoryId { get; } public MemoryReinforced(string beeId, string memoryId) { BeeId = beeId; MemoryId = memoryId; } }
    public readonly struct MemoryExpired : IGameplayEvent, IBeeEvent { public string BeeId { get; } public string MemoryId { get; } public MemoryExpired(string beeId, string memoryId) { BeeId = beeId; MemoryId = memoryId; } }
}
