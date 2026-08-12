using System;
using System.Collections.Generic;
using System.Linq;
using BeeKingdom.Core.Events;

namespace BeeKingdom.World
{
    public interface IRegionalEventPropagationEngine
    {
        RegionalEventInstance StartEvent(string definitionId, string sourceRegionId, double intensity, long tick);
        RegionalEventPropagationSnapshot Propagate(string eventId, long tick);
    }

    public interface IRegionalEventDefinitionRegistry
    {
        void RegisterDefinition(RegionalEventDefinition definition);
    }

    public interface IRegionalEventPropagationSnapshotProvider
    {
        RegionalEventPropagationSnapshot QueryPropagationSnapshot(string eventId);
    }

    public sealed class RegionalEventDefinition
    {
        public string DefinitionId { get; }
        public string Category { get; }
        public RegionalEventPropagationRule PropagationRule { get; }
        public long DurationTicks { get; }

        public RegionalEventDefinition(string definitionId, string category, RegionalEventPropagationRule propagationRule, long durationTicks)
        {
            DefinitionId = string.IsNullOrWhiteSpace(definitionId) ? throw new ArgumentException("DefinitionId is required.") : definitionId;
            Category = string.IsNullOrWhiteSpace(category) ? "regional" : category;
            PropagationRule = propagationRule ?? throw new ArgumentNullException(nameof(propagationRule));
            DurationTicks = durationTicks < 0 ? 0 : durationTicks;
        }
    }

    public sealed class RegionalEventPropagationRule
    {
        public int MaxDepth { get; }
        public double AttenuationPerDepth { get; }
        public double MinimumIntensity { get; }

        public RegionalEventPropagationRule(int maxDepth, double attenuationPerDepth, double minimumIntensity)
        {
            MaxDepth = Math.Max(0, maxDepth);
            AttenuationPerDepth = Clamp01(attenuationPerDepth);
            MinimumIntensity = Clamp01(minimumIntensity);
        }

        public double IntensityAtDepth(double sourceIntensity, int depth)
        {
            double intensity = Clamp01(sourceIntensity);
            for (int i = 0; i < depth; i++) intensity *= AttenuationPerDepth;
            return Clamp01(intensity);
        }

        private static double Clamp01(double value) { return value < 0d ? 0d : value > 1d ? 1d : value; }
    }

    public sealed class RegionalEventInstance
    {
        public string EventId { get; }
        public RegionalEventDefinition Definition { get; }
        public string SourceRegionId { get; }
        public double SourceIntensity { get; }
        public long StartedTick { get; }
        public bool Expired { get; private set; }

        public RegionalEventInstance(string eventId, RegionalEventDefinition definition, string sourceRegionId, double sourceIntensity, long startedTick)
        {
            EventId = eventId;
            Definition = definition;
            SourceRegionId = sourceRegionId;
            SourceIntensity = sourceIntensity < 0d ? 0d : sourceIntensity > 1d ? 1d : sourceIntensity;
            StartedTick = startedTick;
        }

        public bool UpdateExpiration(long tick)
        {
            Expired = Definition.DurationTicks > 0 && tick - StartedTick >= Definition.DurationTicks;
            return Expired;
        }
    }

    public sealed class RegionalEventPropagationEngine : IRegionalEventPropagationEngine, IRegionalEventDefinitionRegistry, IRegionalEventPropagationSnapshotProvider
    {
        private readonly Dictionary<string, RegionalEventDefinition> definitions = new Dictionary<string, RegionalEventDefinition>();
        private readonly Dictionary<string, RegionDefinition> regions = new Dictionary<string, RegionDefinition>();
        private readonly Dictionary<string, RegionalEventInstance> instances = new Dictionary<string, RegionalEventInstance>();
        private readonly Dictionary<string, RegionalEventPropagationSnapshot> snapshots = new Dictionary<string, RegionalEventPropagationSnapshot>();
        private readonly RegionalEventPropagationDiagnostics diagnostics;
        private int eventCounter;

        public RegionalEventPropagationEngine(RegionalEventPropagationDiagnostics diagnostics = null)
        {
            this.diagnostics = diagnostics ?? new RegionalEventPropagationDiagnostics();
        }

        public void RegisterDefinition(RegionalEventDefinition definition)
        {
            definitions[definition.DefinitionId] = definition;
        }

        public void RegisterRegion(RegionDefinition region)
        {
            regions[region.RegionId] = region;
        }

        public RegionalEventInstance StartEvent(string definitionId, string sourceRegionId, double intensity, long tick)
        {
            if (!definitions.TryGetValue(definitionId, out RegionalEventDefinition definition)) throw new KeyNotFoundException(definitionId);
            if (!regions.ContainsKey(sourceRegionId))
            {
                diagnostics.RecordBlocked(definitionId, sourceRegionId, "Unknown source region.");
                return null;
            }

            string eventId = definitionId + "-" + (++eventCounter).ToString("D4");
            RegionalEventInstance instance = new RegionalEventInstance(eventId, definition, sourceRegionId, intensity, tick);
            instances[eventId] = instance;
            diagnostics.RecordStarted(eventId);
            return instance;
        }

        public RegionalEventPropagationSnapshot Propagate(string eventId, long tick)
        {
            if (!instances.TryGetValue(eventId, out RegionalEventInstance instance)) throw new KeyNotFoundException(eventId);
            if (instance.UpdateExpiration(tick))
            {
                RegionalEventPropagationSnapshot expired = new RegionalEventPropagationSnapshot(eventId, instance.Definition.DefinitionId, instance.SourceRegionId, tick, true, Array.Empty<RegionalEventAffectedRegion>());
                snapshots[eventId] = expired;
                diagnostics.RecordExpired(eventId);
                return expired;
            }

            List<RegionalEventAffectedRegion> affected = new List<RegionalEventAffectedRegion>();
            Queue<Tuple<string, int>> queue = new Queue<Tuple<string, int>>();
            HashSet<string> visited = new HashSet<string>();
            queue.Enqueue(Tuple.Create(instance.SourceRegionId, 0));
            visited.Add(instance.SourceRegionId);
            while (queue.Count > 0)
            {
                Tuple<string, int> current = queue.Dequeue();
                double intensity = instance.Definition.PropagationRule.IntensityAtDepth(instance.SourceIntensity, current.Item2);
                if (intensity >= instance.Definition.PropagationRule.MinimumIntensity)
                {
                    affected.Add(new RegionalEventAffectedRegion(current.Item1, current.Item2, intensity));
                    diagnostics.RecordPropagated(eventId, current.Item1);
                }

                if (current.Item2 >= instance.Definition.PropagationRule.MaxDepth || !regions.TryGetValue(current.Item1, out RegionDefinition region))
                {
                    continue;
                }

                foreach (string neighbor in region.NeighborRegionIds.OrderBy(id => id, StringComparer.Ordinal))
                {
                    if (!regions.ContainsKey(neighbor))
                    {
                        diagnostics.RecordBlocked(eventId, neighbor, "Unknown neighbor region.");
                        continue;
                    }

                    if (visited.Add(neighbor))
                    {
                        queue.Enqueue(Tuple.Create(neighbor, current.Item2 + 1));
                    }
                }
            }

            RegionalEventPropagationSnapshot snapshot = new RegionalEventPropagationSnapshot(eventId, instance.Definition.DefinitionId, instance.SourceRegionId, tick, false, affected.OrderBy(item => item.RegionId, StringComparer.Ordinal).ToList());
            snapshots[eventId] = snapshot;
            return snapshot;
        }

        public RegionalEventPropagationSnapshot QueryPropagationSnapshot(string eventId)
        {
            return snapshots.TryGetValue(eventId, out RegionalEventPropagationSnapshot snapshot) ? snapshot : null;
        }
    }

    public sealed class RegionalEventAffectedRegion
    {
        public string RegionId { get; }
        public int Depth { get; }
        public double Intensity { get; }
        public RegionalEventAffectedRegion(string regionId, int depth, double intensity) { RegionId = regionId; Depth = depth; Intensity = intensity; }
    }

    public sealed class RegionalEventPropagationSnapshot
    {
        public string EventId { get; }
        public string DefinitionId { get; }
        public string SourceRegionId { get; }
        public long Tick { get; }
        public bool Expired { get; }
        public IReadOnlyList<RegionalEventAffectedRegion> AffectedRegions { get; }

        public RegionalEventPropagationSnapshot(string eventId, string definitionId, string sourceRegionId, long tick, bool expired, IReadOnlyList<RegionalEventAffectedRegion> affectedRegions)
        {
            EventId = eventId;
            DefinitionId = definitionId;
            SourceRegionId = sourceRegionId;
            Tick = tick;
            Expired = expired;
            AffectedRegions = new List<RegionalEventAffectedRegion>(affectedRegions ?? Array.Empty<RegionalEventAffectedRegion>()).AsReadOnly();
        }
    }

    public sealed class RegionalEventPropagationDiagnostics
    {
        private readonly List<string> messages = new List<string>();
        public int StartedCount { get; private set; }
        public int PropagatedCount { get; private set; }
        public int ExpiredCount { get; private set; }
        public int BlockedCount { get; private set; }
        public IReadOnlyList<string> Messages => messages.AsReadOnly();
        public void RecordStarted(string eventId) { StartedCount++; messages.Add("Started:" + eventId); }
        public void RecordPropagated(string eventId, string regionId) { PropagatedCount++; messages.Add("Propagated:" + eventId + ":" + regionId); }
        public void RecordExpired(string eventId) { ExpiredCount++; messages.Add("Expired:" + eventId); }
        public void RecordBlocked(string eventId, string regionId, string reason) { BlockedCount++; messages.Add("Blocked:" + eventId + ":" + regionId + ":" + reason); }
    }

    public readonly struct RegionalEventStarted : IGameplayEvent
    {
        public string EventId { get; }
        public string DefinitionId { get; }
        public string SourceRegionId { get; }
        public double Intensity { get; }
        public long Tick { get; }
        public RegionalEventStarted(string eventId, string definitionId, string sourceRegionId, double intensity, long tick) { EventId = eventId; DefinitionId = definitionId; SourceRegionId = sourceRegionId; Intensity = intensity; Tick = tick; }
    }

    public readonly struct RegionalEventPropagated : IGameplayEvent
    {
        public string EventId { get; }
        public string DefinitionId { get; }
        public string SourceRegionId { get; }
        public string TargetRegionId { get; }
        public double Intensity { get; }
        public long Tick { get; }
        public RegionalEventPropagated(string eventId, string definitionId, string sourceRegionId, string targetRegionId, double intensity, long tick) { EventId = eventId; DefinitionId = definitionId; SourceRegionId = sourceRegionId; TargetRegionId = targetRegionId; Intensity = intensity; Tick = tick; }
    }

    public readonly struct RegionalEventExpired : IGameplayEvent
    {
        public string EventId { get; }
        public string DefinitionId { get; }
        public string SourceRegionId { get; }
        public long Tick { get; }
        public RegionalEventExpired(string eventId, string definitionId, string sourceRegionId, long tick) { EventId = eventId; DefinitionId = definitionId; SourceRegionId = sourceRegionId; Tick = tick; }
    }

    public readonly struct RegionalEventPropagationBlocked : IGameplayEvent
    {
        public string EventId { get; }
        public string DefinitionId { get; }
        public string SourceRegionId { get; }
        public string TargetRegionId { get; }
        public double Intensity { get; }
        public long Tick { get; }
        public RegionalEventPropagationBlocked(string eventId, string definitionId, string sourceRegionId, string targetRegionId, double intensity, long tick) { EventId = eventId; DefinitionId = definitionId; SourceRegionId = sourceRegionId; TargetRegionId = targetRegionId; Intensity = intensity; Tick = tick; }
    }
}
