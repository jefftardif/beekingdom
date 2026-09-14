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

        public static IReadOnlyDictionary<string, WorldMapMarch> AllMarches => _marches;

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
                if (ActiveMarchCount >= MaxActiveMarches) return false;
                if (_marches.ContainsKey(march.MarchId)) return false;

                if (!CanReserveTroops(march.OriginHiveId, march.CommittedTroops, availableRoster, march.MarchId)) return false;
                if (!CanReserveChampion(march.OriginHiveId, march.ChampionId, march.MarchId)) return false;

                ReserveTroops(march.OriginHiveId, march.CommittedTroops, march.MarchId);
                ReserveChampion(march.OriginHiveId, march.ChampionId, march.MarchId);

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

                ReleaseTroops(march.OriginHiveId, march.CommittedTroops, availableRoster, marchId);
                ReleaseChampion(march.OriginHiveId, march.ChampionId, marchId);

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

        private static bool CanReserveTroops(Guid hiveId, Dictionary<string, long> troops, IReadOnlyDictionary<string, long> availableRoster, string excludingMarchId)
        {
            if (troops == null || troops.Count == 0) return true;

            var hiveKey = hiveId.ToString("D");
            lock (_lock)
            {
                var reserved = _reservedTroopsByHive.GetValueOrDefault(hiveKey);
                if (reserved == null) return true;

                foreach (var kvp in troops)
                {
                    long available = availableRoster.GetValueOrDefault(kvp.Key) - reserved.GetValueOrDefault(kvp.Key);
                    if (available < kvp.Value) return false;
                }
                return true;
            }
        }

        private static bool CanReserveChampion(Guid hiveId, string championId, string excludingMarchId)
        {
            if (string.IsNullOrEmpty(championId)) return true;

            var hiveKey = hiveId.ToString("D");
            lock (_lock)
            {
                if (!_reservedChampionByHive.TryGetValue(hiveKey, out var reservedSet)) return true;
                return !reservedSet.Contains(championId);
            }
        }

        private static void ReserveTroops(Guid hiveId, Dictionary<string, long> troops, string marchId)
        {
            if (troops == null || troops.Count == 0) return;

            var hiveKey = hiveId.ToString("D");
            lock (_lock)
            {
                if (!_reservedTroopsByHive.TryGetValue(hiveKey, out var reserved))
                {
                    reserved = new Dictionary<string, long>();
                    _reservedTroopsByHive[hiveKey] = reserved;
                }

                foreach (var kvp in troops)
                {
                    reserved[kvp.Key] = reserved.GetValueOrDefault(kvp.Key) + kvp.Value;
                }
            }
        }

        private static void ReleaseTroops(Guid hiveId, Dictionary<string, long> troops, IReadOnlyDictionary<string, long> availableRoster, string marchId)
        {
            if (troops == null || troops.Count == 0) return;

            var hiveKey = hiveId.ToString("D");
            lock (_lock)
            {
                if (!_reservedTroopsByHive.TryGetValue(hiveKey, out var reserved)) return;

                foreach (var kvp in troops)
                {
                    if (reserved.TryGetValue(kvp.Key, out var current))
                    {
                        long remaining = Math.Max(0, current - kvp.Value);
                        if (remaining > 0)
                            reserved[kvp.Key] = remaining;
                        else
                            reserved.Remove(kvp.Key);
                    }
                }

                if (reserved.Count == 0)
                    _reservedTroopsByHive.Remove(hiveKey);
            }
        }

        private static void ReserveChampion(Guid hiveId, string championId, string marchId)
        {
            if (string.IsNullOrEmpty(championId)) return;

            var hiveKey = hiveId.ToString("D");
            lock (_lock)
            {
                if (!_reservedChampionByHive.TryGetValue(hiveKey, out var set))
                {
                    set = new HashSet<string>();
                    _reservedChampionByHive[hiveKey] = set;
                }
                set.Add(championId);
            }
        }

        private static void ReleaseChampion(Guid hiveId, string championId, string marchId)
        {
            if (string.IsNullOrEmpty(championId)) return;

            var hiveKey = hiveId.ToString("D");
            lock (_lock)
            {
                if (_reservedChampionByHive.TryGetValue(hiveKey, out var set))
                {
                    set.Remove(championId);
                    if (set.Count == 0)
                        _reservedChampionByHive.Remove(hiveKey);
                }
            }
        }

        public static void RebuildFromServerState(Guid hiveId, 
            IReadOnlyList<RemoteCombatPatrolActiveEncounter> combatEncounters,
            RemoteWorldResourceActiveFlight resourceFlight)
        {
            lock (_lock)
            {
                _marches.Clear();
                _reservedTroopsByHive.Clear();
                _reservedChampionByHive.Clear();

                if (combatEncounters != null)
                {
                    foreach (var encounter in combatEncounters)
                    {
                        var march = new WorldMapMarch
                        {
                            MarchId = $"combat_{encounter.EncounterId}",
                            Type = WorldMapMarchType.Combat,
                            State = MapCombatState(encounter),
                            OriginHiveId = hiveId,
                            TargetId = encounter.EncounterId.ToString(),
                            CommittedTroops = encounter.CommittedTroops ?? new Dictionary<string, long>(),
                            ChampionId = null,
                            StartedAtUtc = encounter.StartedAtUtc,
                            EndsAtUtc = encounter.EndsAtUtc,
                        };
                        _marches[march.MarchId] = march;
                        ReserveTroops(hiveId, march.CommittedTroops, march.MarchId);
                    }
                }

                if (resourceFlight != null)
                {
                    var march = new WorldMapMarch
                    {
                        MarchId = $"collection_{resourceFlight.FlightId}",
                        Type = WorldMapMarchType.Collection,
                        State = MapResourceState(resourceFlight),
                        OriginHiveId = hiveId,
                        TargetId = resourceFlight.NodeId,
                        CommittedTroops = resourceFlight.CommittedTroops ?? new Dictionary<string, long>(),
                        ChampionId = null,
                        StartedAtUtc = resourceFlight.StartedAtUtc,
                        EndsAtUtc = resourceFlight.EndsAtUtc,
                    };
                    _marches[march.MarchId] = march;
                    ReserveTroops(hiveId, march.CommittedTroops, march.MarchId);
                }
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