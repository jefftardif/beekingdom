using System;

namespace BeeKingdom.Hive
{
    [Serializable]
    public sealed class BeeLifecycleSnapshot
    {
        public string BeeId;
        public string HiveId;
        public double BirthTime;
        public double AgeSeconds;
        public double BiologicalAgeSeconds;
        public BeeLifecycleStage CurrentStage;
        public BeeLifecycleRole CurrentRole;
        public int Health;
        public int Energy;
        public int Experience;
        public string GeneticsId;
    }
}
