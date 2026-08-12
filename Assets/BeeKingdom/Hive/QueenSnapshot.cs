using System;

namespace BeeKingdom.Hive
{
    [Serializable]
    public sealed class QueenSnapshot
    {
        public string QueenId;
        public string HiveId;
        public double AgeSeconds;
        public QueenState State;
        public int Health;
        public int MaxHealth;
        public int Energy;
        public float Fertility;
        public int Level;
        public int Experience;
        public float BaseEggsPerMinute;
    }
}
