using System.Collections.Generic;
using BeeKingdom.Hive;

namespace BeeKingdom.Gameplay
{
    public sealed class StarterHiveProfile
    {
        public string HiveId { get; }
        public string OwnerId { get; }
        public string QueenBeeId { get; }
        public HiveCapacity Capacity { get; }
        public int QueenLevel { get; }
        public float QueenBaseEggsPerMinute { get; }
        public double SimulationSpeed { get; }
        public IReadOnlyList<HiveChamberType> StartingChambers { get; }
        public IReadOnlyList<string> UnlockedTechnologyIds { get; }

        public StarterHiveProfile(
            string hiveId,
            string ownerId,
            string queenBeeId,
            HiveCapacity capacity,
            int queenLevel,
            float queenBaseEggsPerMinute,
            double simulationSpeed,
            IReadOnlyList<HiveChamberType> startingChambers,
            IReadOnlyList<string> unlockedTechnologyIds)
        {
            HiveId = string.IsNullOrWhiteSpace(hiveId) ? "starter-hive" : hiveId;
            OwnerId = string.IsNullOrWhiteSpace(ownerId) ? "player" : ownerId;
            QueenBeeId = string.IsNullOrWhiteSpace(queenBeeId) ? "queen-1" : queenBeeId;
            Capacity = capacity;
            QueenLevel = queenLevel < 1 ? 1 : queenLevel;
            QueenBaseEggsPerMinute = queenBaseEggsPerMinute < 0f ? 0f : queenBaseEggsPerMinute;
            SimulationSpeed = simulationSpeed <= 0d ? 1d : simulationSpeed;
            StartingChambers = startingChambers ?? new HiveChamberType[0];
            UnlockedTechnologyIds = unlockedTechnologyIds ?? new string[0];
        }

        public static StarterHiveProfile CreateDefault()
        {
            return new StarterHiveProfile(
                "starter-hive",
                "player",
                "queen-1",
                new HiveCapacity(256, 64, 64),
                1,
                0.25f,
                1d,
                new[] { HiveChamberType.Entrance, HiveChamberType.RoyalChamber, HiveChamberType.Nursery, HiveChamberType.HoneyStorage, HiveChamberType.PollenStorage },
                new[] { "starter-beekeeping" });
        }
    }
}
