using System;
using System.Collections.Generic;
using System.Linq;
using BeeKingdom.Core.Events;

namespace BeeKingdom.World
{
    public enum ExplorationVisibilityState
    {
        Unknown,
        Discovered,
        Visible,
        Observed,
        Stale
    }

    public interface IWorldExplorationVisibilityMap
    {
        ColonyWorldKnowledge QueryKnowledge(string colonyId);
    }

    public interface IWorldExplorationVisibilityUpdater
    {
        RegionVisibilityRecord MarkDiscovered(string colonyId, string worldId, string regionId, long tick);
        RegionVisibilityRecord MarkVisible(string colonyId, string worldId, string regionId, long tick, long durationTicks);
        void UpdateExpirations(long tick);
    }

    public interface IWorldExplorationVisibilitySnapshotProvider
    {
        WorldExplorationVisibilitySnapshot CreateSnapshot();
    }

    public sealed class WorldExplorationVisibilityMap : IWorldExplorationVisibilityMap, IWorldExplorationVisibilityUpdater, IWorldExplorationVisibilitySnapshotProvider
    {
        private readonly Dictionary<string, ColonyWorldKnowledge> knowledgeByColony = new Dictionary<string, ColonyWorldKnowledge>();
        public WorldExplorationVisibilityDiagnostics Diagnostics { get; } = new WorldExplorationVisibilityDiagnostics();

        public ColonyWorldKnowledge QueryKnowledge(string colonyId)
        {
            if (!knowledgeByColony.TryGetValue(colonyId, out ColonyWorldKnowledge knowledge))
            {
                knowledge = new ColonyWorldKnowledge(colonyId);
                knowledgeByColony[colonyId] = knowledge;
            }

            return knowledge;
        }

        public RegionVisibilityRecord MarkDiscovered(string colonyId, string worldId, string regionId, long tick)
        {
            RegionVisibilityRecord record = QueryKnowledge(colonyId).SetRegion(worldId, regionId, ExplorationVisibilityState.Discovered, tick, -1);
            Diagnostics.RecordDiscovered(colonyId, regionId);
            return record;
        }

        public RegionVisibilityRecord MarkVisible(string colonyId, string worldId, string regionId, long tick, long durationTicks)
        {
            RegionVisibilityRecord record = QueryKnowledge(colonyId).SetRegion(worldId, regionId, ExplorationVisibilityState.Visible, tick, tick + Math.Max(0, durationTicks));
            Diagnostics.RecordVisibilityChanged(colonyId, regionId);
            return record;
        }

        public RegionVisibilityRecord MarkObserved(string colonyId, string worldId, string regionId, long tick, long durationTicks)
        {
            RegionVisibilityRecord record = QueryKnowledge(colonyId).SetRegion(worldId, regionId, ExplorationVisibilityState.Observed, tick, tick + Math.Max(0, durationTicks));
            Diagnostics.RecordVisibilityChanged(colonyId, regionId);
            return record;
        }

        public void UpdateExpirations(long tick)
        {
            foreach (ColonyWorldKnowledge knowledge in knowledgeByColony.Values)
            {
                foreach (RegionVisibilityRecord record in knowledge.QueryRecords())
                {
                    if ((record.State == ExplorationVisibilityState.Visible || record.State == ExplorationVisibilityState.Observed) && record.ExpiresAtTick >= 0 && tick >= record.ExpiresAtTick)
                    {
                        knowledge.SetRegion(record.WorldId, record.RegionId, ExplorationVisibilityState.Stale, tick, -1);
                        Diagnostics.RecordExpired(knowledge.ColonyId, record.RegionId);
                    }
                }
            }
        }

        public WorldExplorationVisibilitySnapshot CreateSnapshot()
        {
            List<RegionVisibilityRecord> records = knowledgeByColony.Values
                .OrderBy(knowledge => knowledge.ColonyId, StringComparer.Ordinal)
                .SelectMany(knowledge => knowledge.QueryRecords().OrderBy(record => record.RegionId, StringComparer.Ordinal))
                .ToList();
            return new WorldExplorationVisibilitySnapshot(records);
        }
    }

    public sealed class ColonyWorldKnowledge
    {
        private readonly Dictionary<string, RegionVisibilityRecord> records = new Dictionary<string, RegionVisibilityRecord>();
        public string ColonyId { get; }

        public ColonyWorldKnowledge(string colonyId)
        {
            ColonyId = string.IsNullOrWhiteSpace(colonyId) ? throw new ArgumentException("ColonyId is required.") : colonyId;
        }

        public RegionVisibilityRecord QueryRegion(string regionId)
        {
            return records.TryGetValue(regionId, out RegionVisibilityRecord record) ? record : new RegionVisibilityRecord(ColonyId, string.Empty, regionId, ExplorationVisibilityState.Unknown, -1, -1);
        }

        public IReadOnlyList<RegionVisibilityRecord> QueryRecords()
        {
            return records.Values.ToList();
        }

        public RegionVisibilityRecord SetRegion(string worldId, string regionId, ExplorationVisibilityState state, long tick, long expiresAtTick)
        {
            RegionVisibilityRecord record = new RegionVisibilityRecord(ColonyId, worldId, regionId, state, tick, expiresAtTick);
            records[regionId] = record;
            return record;
        }
    }

    public sealed class RegionVisibilityRecord
    {
        public string ColonyId { get; }
        public string WorldId { get; }
        public string RegionId { get; }
        public ExplorationVisibilityState State { get; }
        public long UpdatedAtTick { get; }
        public long ExpiresAtTick { get; }

        public RegionVisibilityRecord(string colonyId, string worldId, string regionId, ExplorationVisibilityState state, long updatedAtTick, long expiresAtTick)
        {
            ColonyId = colonyId;
            WorldId = worldId ?? string.Empty;
            RegionId = regionId ?? string.Empty;
            State = state;
            UpdatedAtTick = updatedAtTick;
            ExpiresAtTick = expiresAtTick;
        }
    }

    public sealed class WorldExplorationVisibilitySnapshot
    {
        public IReadOnlyList<RegionVisibilityRecord> Records { get; }
        public WorldExplorationVisibilitySnapshot(IReadOnlyList<RegionVisibilityRecord> records) { Records = new List<RegionVisibilityRecord>(records ?? Array.Empty<RegionVisibilityRecord>()).AsReadOnly(); }
    }

    public sealed class WorldExplorationVisibilityDiagnostics
    {
        private readonly List<string> messages = new List<string>();
        public int DiscoveredCount { get; private set; }
        public int VisibilityChangeCount { get; private set; }
        public int ExpiredCount { get; private set; }
        public IReadOnlyList<string> Messages => messages.AsReadOnly();
        public void RecordDiscovered(string colonyId, string regionId) { DiscoveredCount++; messages.Add("Discovered:" + colonyId + ":" + regionId); }
        public void RecordVisibilityChanged(string colonyId, string regionId) { VisibilityChangeCount++; messages.Add("Visibility:" + colonyId + ":" + regionId); }
        public void RecordExpired(string colonyId, string regionId) { ExpiredCount++; messages.Add("Expired:" + colonyId + ":" + regionId); }
    }

    public readonly struct RegionDiscovered : IGameplayEvent
    {
        public string ColonyId { get; }
        public string WorldId { get; }
        public string RegionId { get; }
        public ExplorationVisibilityState PreviousState { get; }
        public ExplorationVisibilityState NewState { get; }
        public long Tick { get; }
        public RegionDiscovered(string colonyId, string worldId, string regionId, ExplorationVisibilityState previousState, ExplorationVisibilityState newState, long tick) { ColonyId = colonyId; WorldId = worldId; RegionId = regionId; PreviousState = previousState; NewState = newState; Tick = tick; }
    }

    public readonly struct RegionVisibilityChanged : IGameplayEvent
    {
        public string ColonyId { get; }
        public string WorldId { get; }
        public string RegionId { get; }
        public ExplorationVisibilityState PreviousState { get; }
        public ExplorationVisibilityState NewState { get; }
        public long Tick { get; }
        public RegionVisibilityChanged(string colonyId, string worldId, string regionId, ExplorationVisibilityState previousState, ExplorationVisibilityState newState, long tick) { ColonyId = colonyId; WorldId = worldId; RegionId = regionId; PreviousState = previousState; NewState = newState; Tick = tick; }
    }

    public readonly struct RegionObservationExpired : IGameplayEvent
    {
        public string ColonyId { get; }
        public string WorldId { get; }
        public string RegionId { get; }
        public ExplorationVisibilityState PreviousState { get; }
        public ExplorationVisibilityState NewState { get; }
        public long Tick { get; }
        public RegionObservationExpired(string colonyId, string worldId, string regionId, ExplorationVisibilityState previousState, ExplorationVisibilityState newState, long tick) { ColonyId = colonyId; WorldId = worldId; RegionId = regionId; PreviousState = previousState; NewState = newState; Tick = tick; }
    }
}
