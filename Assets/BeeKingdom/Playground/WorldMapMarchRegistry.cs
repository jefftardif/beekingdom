using System;
using System.Collections.Generic;
using System.Linq;
using BeeKingdom.Networking;

namespace BeeKingdom.Playground
{
    public enum WorldMapMarchType
    {
        Combat = 0,
        Collection = 1,
    }

    public enum WorldMapMarchState
    {
        Preparing = 0,
        Outbound = 1,
        Operating = 2,
        Returning = 3,
        Completed = 4,
    }

    public sealed class WorldMapMarch
    {
        public string MarchId { get; set; }
        public WorldMapMarchType Type { get; set; }
        public WorldMapMarchState State { get; set; }
        public Guid OriginHiveId { get; set; }
        public string TargetId { get; set; }
        public Dictionary<string, long> CommittedTroops { get; set; } = new Dictionary<string, long>();
        public string ChampionId { get; set; }
        public DateTimeOffset StartedAtUtc { get; set; }
        public DateTimeOffset? EndsAtUtc { get; set; }
        public object SourceData { get; set; }

        public long TotalTroops => CommittedTroops?.Values.Sum() ?? 0;
        public bool IsActive => State != WorldMapMarchState.Completed;
    }

    public static class WorldMapMarchRegistry
    {
        private const int MaxActiveMarches = 3;
        private static readonly Dictionary<string, WorldMapMarch> _marches = new Dictionary<string, WorldMapMarch>();
        private static readonly object _lock = new object();

        public static IReadOnlyDictionary<string, WorldMapMarch> AllMarches
        {
            get { lock (_lock) { return new Dictionary<string, WorldMapMarch>(_marches); } }
        }

        public static int ActiveMarchCount
        {
            get
            {
                lock (_lock)
                {
                    return _marches.Values.Count(m => m.IsActive);
                }
            }
        }

        public static int AvailableSlots => Math.Max(0, MaxActiveMarches - ActiveMarchCount);

        public static bool TryRegisterMarch(WorldMapMarch march, IReadOnlyDictionary<string, long> availableRoster)
        {
            if (march == null) return false;
            lock (_lock)
            {
                // Use snapshot of count without re-lock
                int activeCount = _marches.Values.Count(m => m.IsActive);
                if (activeCount >= MaxActiveMarches) return false;
                if (_marches.ContainsKey(march.MarchId)) return false;

                if (!CanReserveTroopsInternal(march.OriginHiveId, march.CommittedTroops, availableRoster)) return false;
                if (!CanReserveChampionInternal(march.OriginHiveId, march.ChampionId)) return false;

                ReserveTroopsInternal(march.OriginHiveId, march.CommittedTroops);
                ReserveChampionInternal(march.OriginHiveId, march.ChampionId);

                _marches[march.MarchId] = march;
                return true;
            }
        }

        public static bool UnregisterMarch(string marchId, IReadOnlyDictionary<string, long> availableRoster)
        {
            if (string.IsNullOrEmpty(marchId)) return false;
            lock (_lock)
            {
                if (!_marches.TryGetValue(marchId, out var march)) return false;

                ReleaseTroopsInternal(march.OriginHiveId, march.CommittedTroops);
                ReleaseChampionInternal(march.OriginHiveId, march.ChampionId);

                _marches.Remove(marchId);
                return true;
            }
        }

        public static bool CompleteMarch(string marchId)
        {
            if (string.IsNullOrEmpty(marchId)) return false;
            lock (_lock)
            {
                if (!_marches.TryGetValue(marchId, out var march)) return false;
                march.State = WorldMapMarchState.Completed;
                return true;
            }
        }

        public static WorldMapMarch GetMarch(string marchId)
        {
            lock (_lock)
            {
                _marches.TryGetValue(marchId, out var march);
                return march;
            }
        }

        public static IReadOnlyList<WorldMapMarch> GetActiveMarches()
        {
            lock (_lock)
            {
                return _marches.Values.Where(m => m.IsActive).ToList();
            }
        }

        public static IReadOnlyList<WorldMapMarch> GetMarchesByType(WorldMapMarchType type)
        {
            lock (_lock)
            {
                return _marches.Values.Where(m => m.Type == type && m.IsActive).ToList();
            }
        }

        public static bool CanLaunchNewMarch()
        {
            return ActiveMarchCount < MaxActiveMarches;
        }

        public static string GetMarchLimitText()
        {
            return $"Marches {ActiveMarchCount}/{MaxActiveMarches}";
        }

        private static readonly Dictionary<string, Dictionary<string, long>> _reservedTroopsByHive = new Dictionary<string, Dictionary<string, long>>();
        private static readonly Dictionary<string, HashSet<string>> _reservedChampionByHive = new Dictionary<string, HashSet<string>>();

        // Internal helpers assume _lock already held
        private static bool CanReserveTroopsInternal(Guid hiveId, Dictionary<string, long> troops, IReadOnlyDictionary<string, long> availableRoster)
        {
            if (troops == null || troops.Count == 0) return true;
            // Server AvailableRoster is already authoritative post-deduction.
            // Only check requested <= available (no double subtract). Keep local reserved for optimistic second march in same frame before Rebuild.
            foreach (var kvp in troops)
            {
                long available = availableRoster != null ? availableRoster.GetValueOrDefault(kvp.Key) : 0;
                if (available < kvp.Value) return false;
            }
            return true;
        }

        private static bool CanReserveChampionInternal(Guid hiveId, string championId)
        {
            if (string.IsNullOrEmpty(championId)) return true;
            var hiveKey = hiveId.ToString("D");
            if (!_reservedChampionByHive.TryGetValue(hiveKey, out var reservedSet)) return true;
            return !reservedSet.Contains(championId);
        }

        private static void ReserveTroopsInternal(Guid hiveId, Dictionary<string, long> troops)
        {
            if (troops == null || troops.Count == 0) return;
            var hiveKey = hiveId.ToString("D");
            if (!_reservedTroopsByHive.TryGetValue(hiveKey, out var reserved))
            {
                reserved = new Dictionary<string, long>();
                _reservedTroopsByHive[hiveKey] = reserved;
            }
            foreach (var kvp in troops)
                reserved[kvp.Key] = reserved.GetValueOrDefault(kvp.Key) + kvp.Value;
        }

        private static void ReleaseTroopsInternal(Guid hiveId, Dictionary<string, long> troops)
        {
            if (troops == null || troops.Count == 0) return;
            var hiveKey = hiveId.ToString("D");
            if (!_reservedTroopsByHive.TryGetValue(hiveKey, out var reserved)) return;
            foreach (var kvp in troops)
            {
                if (reserved.TryGetValue(kvp.Key, out var current))
                {
                    long remaining = Math.Max(0, current - kvp.Value);
                    if (remaining > 0) reserved[kvp.Key] = remaining;
                    else reserved.Remove(kvp.Key);
                }
            }
            if (reserved.Count == 0) _reservedTroopsByHive.Remove(hiveKey);
        }

        private static void ReserveChampionInternal(Guid hiveId, string championId)
        {
            if (string.IsNullOrEmpty(championId)) return;
            var hiveKey = hiveId.ToString("D");
            if (!_reservedChampionByHive.TryGetValue(hiveKey, out var set))
            {
                set = new HashSet<string>();
                _reservedChampionByHive[hiveKey] = set;
            }
            set.Add(championId);
        }

        private static void ReleaseChampionInternal(Guid hiveId, string championId)
        {
            if (string.IsNullOrEmpty(championId)) return;
            var hiveKey = hiveId.ToString("D");
            if (_reservedChampionByHive.TryGetValue(hiveKey, out var set))
            {
                set.Remove(championId);
                if (set.Count == 0) _reservedChampionByHive.Remove(hiveKey);
            }
        }

        // Incremental per-type sync helpers
        public static void UpdateCombatMarches(Guid hiveId, IReadOnlyList<RemoteCombatPatrolActiveEncounter> combatEncounters)
        {
            if (hiveId == Guid.Empty) return;
            lock (_lock)
            {
                // combatEncounters == null => not configured, skip sync
                if (combatEncounters == null) return;
                var keep = new HashSet<string>();
                foreach (var enc in combatEncounters) keep.Add($"combat_{enc.EncounterId}");
                var toRemove = _marches.Values.Where(m => m.Type == WorldMapMarchType.Combat && !keep.Contains(m.MarchId)).Select(m => m.MarchId).ToList();
                foreach (var id in toRemove)
                {
                    if (_marches.TryGetValue(id, out var old)) ReleaseTroopsInternal(old.OriginHiveId, old.CommittedTroops);
                    _marches.Remove(id);
                }
                foreach (var enc in combatEncounters)
                {
                    string mid = $"combat_{enc.EncounterId}";
                    if (!_marches.ContainsKey(mid))
                    {
                        var march = new WorldMapMarch { MarchId = mid, Type = WorldMapMarchType.Combat, State = MapCombatState(enc), OriginHiveId = hiveId, TargetId = enc.EncounterId.ToString(), CommittedTroops = enc.CommittedTroops ?? new Dictionary<string, long>(), ChampionId = null, StartedAtUtc = enc.StartedAtUtc, EndsAtUtc = enc.EndsAtUtc };
                        _marches[mid] = march;
                        ReserveTroopsInternal(hiveId, march.CommittedTroops);
                    }
                    else
                    {
                        var ex = _marches[mid];
                        ex.State = MapCombatState(enc);
                        ex.StartedAtUtc = enc.StartedAtUtc;
                        ex.EndsAtUtc = enc.EndsAtUtc;
                        ex.CommittedTroops = enc.CommittedTroops ?? new Dictionary<string, long>();
                    }
                }
            }
        }

        public static void UpdateCollectionMarch(Guid hiveId, RemoteWorldResourceActiveFlight resourceFlight)
        {
            if (hiveId == Guid.Empty) return;
            lock (_lock)
            {
                if (resourceFlight == null)
                {
                    var toRemove = _marches.Values.Where(m => m.Type == WorldMapMarchType.Collection).Select(m => m.MarchId).ToList();
                    foreach (var id in toRemove)
                    {
                        if (_marches.TryGetValue(id, out var old)) ReleaseTroopsInternal(old.OriginHiveId, old.CommittedTroops);
                        _marches.Remove(id);
                    }
                    return;
                }
                string mid = $"collection_{resourceFlight.FlightId}";
                var toRemoveCol = _marches.Values.Where(m => m.Type == WorldMapMarchType.Collection && m.MarchId != mid).Select(m => m.MarchId).ToList();
                foreach (var id in toRemoveCol)
                {
                    if (_marches.TryGetValue(id, out var old)) ReleaseTroopsInternal(old.OriginHiveId, old.CommittedTroops);
                    _marches.Remove(id);
                }
                if (!_marches.ContainsKey(mid))
                {
                    var march = new WorldMapMarch { MarchId = mid, Type = WorldMapMarchType.Collection, State = MapResourceState(resourceFlight), OriginHiveId = hiveId, TargetId = resourceFlight.NodeId, CommittedTroops = resourceFlight.CommittedTroops ?? new Dictionary<string, long>(), ChampionId = null, StartedAtUtc = resourceFlight.StartedAtUtc, EndsAtUtc = resourceFlight.EndsAtUtc };
                    _marches[mid] = march;
                    ReserveTroopsInternal(hiveId, march.CommittedTroops);
                }
                else
                {
                    var ex = _marches[mid];
                    ex.State = MapResourceState(resourceFlight);
                    ex.StartedAtUtc = resourceFlight.StartedAtUtc;
                    ex.EndsAtUtc = resourceFlight.EndsAtUtc;
                    ex.CommittedTroops = resourceFlight.CommittedTroops ?? new Dictionary<string, long>();
                }
            }
        }

        public static void RebuildFromServerState(Guid hiveId,
            IReadOnlyList<RemoteCombatPatrolActiveEncounter> combatEncounters,
            RemoteWorldResourceActiveFlight resourceFlight)
        {
            // Full rebuild: delegate to per-type incremental but handle null as delete only if caller had data
            // If one side is null because not configured yet, don't delete that type.
            bool hasCombat = combatEncounters != null;
            bool hasCollection = resourceFlight != null;
            // If both sides provided (even if empty/null after load), do incremental per-type
            if (hasCombat) UpdateCombatMarches(hiveId, combatEncounters);
            // For collection, we need to know if collection model exists. If resourceFlight==null but collection configured, that means no active flight -> delete. If not configured (model null), resourceFlight will be null but we shouldn't delete.
            // Caller passes resourceFlight = model?.Active, so we can't distinguish. Use heuristic: if hasCombat==false and hasCollection==false, it's initial full rebuild call with both null -> don't delete either (keep empty). Otherwise if hasCollection==false but caller is collection side, we should delete.
            // Simplified: Rebuild is now only used for initial load where both may be null -> handled by per-type above. For full rebuild we treat null collection as delete only if hiveId valid and we have combat data.
            if (hasCollection) UpdateCollectionMarch(hiveId, resourceFlight);
            else if (!hasCombat && !hasCollection) { /* initial empty, nothing to do */ }
            else if (hasCombat && !hasCollection)
            {
                // Called from Combat with collection not loaded yet -> skip collection delete
            }
        }

        private static WorldMapMarchState MapCombatState(RemoteCombatPatrolActiveEncounter encounter)
        {
            var now = DateTimeOffset.UtcNow;
            if (now < encounter.StartedAtUtc) return WorldMapMarchState.Preparing;
            if (now < encounter.EndsAtUtc) return WorldMapMarchState.Operating;
            return WorldMapMarchState.Returning;
        }

        private static WorldMapMarchState MapResourceState(RemoteWorldResourceActiveFlight flight)
        {
            var now = DateTimeOffset.UtcNow;
            if (now < flight.StartedAtUtc) return WorldMapMarchState.Preparing;
            if (now < flight.EndsAtUtc) return WorldMapMarchState.Operating;
            return WorldMapMarchState.Returning;
        }

        public static void Clear()
        {
            lock (_lock)
            {
                _marches.Clear();
                _reservedTroopsByHive.Clear();
                _reservedChampionByHive.Clear();
            }
        }
    }
}