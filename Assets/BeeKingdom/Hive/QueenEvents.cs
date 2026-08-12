using BeeKingdom.Core.Events;

namespace BeeKingdom.Hive
{
    public readonly struct QueenCreated : IHiveEvent
    {
        public string QueenId { get; }
        public string HiveId { get; }
        public QueenCreated(string queenId, string hiveId) { QueenId = queenId; HiveId = hiveId; }
    }

    public readonly struct QueenLevelUp : IHiveEvent
    {
        public string QueenId { get; }
        public int Level { get; }
        public QueenLevelUp(string queenId, int level) { QueenId = queenId; Level = level; }
    }

    public readonly struct QueenStateChanged : IHiveEvent
    {
        public string QueenId { get; }
        public QueenState State { get; }
        public QueenStateChanged(string queenId, QueenState state) { QueenId = queenId; State = state; }
    }

    public readonly struct QueenEggProduced : IHiveEvent
    {
        public string QueenId { get; }
        public int Count { get; }
        public QueenEggProduced(string queenId, int count) { QueenId = queenId; Count = count; }
    }

    public readonly struct QueenBonusChanged : IHiveEvent
    {
        public string QueenId { get; }
        public QueenBonusType BonusType { get; }
        public float Value { get; }
        public QueenBonusChanged(string queenId, QueenBonusType bonusType, float value) { QueenId = queenId; BonusType = bonusType; Value = value; }
    }

    public readonly struct QueenDied : IHiveEvent
    {
        public string QueenId { get; }
        public QueenDied(string queenId) { QueenId = queenId; }
    }
}
