namespace BeeKingdom.Hive
{
    public readonly struct QueenStatistics
    {
        public int Level { get; }
        public int Experience { get; }
        public QueenState State { get; }
        public QueenHealth Health { get; }
        public int Energy { get; }
        public float Fertility { get; }
        public int EggsProduced { get; }

        public QueenStatistics(int level, int experience, QueenState state, QueenHealth health, int energy, float fertility, int eggsProduced)
        {
            Level = level;
            Experience = experience;
            State = state;
            Health = health;
            Energy = energy;
            Fertility = fertility;
            EggsProduced = eggsProduced;
        }
    }
}
