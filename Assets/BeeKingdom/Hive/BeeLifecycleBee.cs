using System;

namespace BeeKingdom.Hive
{
    public sealed class BeeLifecycleBee
    {
        public string BeeId { get; }
        public string HiveId { get; }
        public BeeAgeProfile Age { get; private set; }
        public BeeLifecycleStage CurrentStage { get; private set; }
        public BeeLifecycleRole CurrentRole { get; private set; }
        public int Health { get; private set; }
        public int Energy { get; private set; }
        public int Experience { get; private set; }
        public string GeneticsId { get; }
        public bool Alive => CurrentStage != BeeLifecycleStage.Dead;

        public BeeLifecycleBee(string beeId, string hiveId, double birthTime, BeeLifecycleRole role, int health, int energy, string geneticsId)
        {
            BeeId = Require(beeId, nameof(beeId));
            HiveId = Require(hiveId, nameof(hiveId));
            Age = new BeeAgeProfile(birthTime, 0d, 0d);
            CurrentStage = BeeLifecycleStage.Egg;
            CurrentRole = role;
            Health = Clamp(health, 0, 100);
            Energy = Clamp(energy, 0, 100);
            GeneticsId = geneticsId ?? string.Empty;
        }

        public void AdvanceAge(double deltaSeconds, float biologicalMultiplier)
        {
            Age = Age.Advance(deltaSeconds, biologicalMultiplier);
        }

        public void ChangeStage(BeeLifecycleStage stage)
        {
            CurrentStage = stage;
        }

        public void ChangeRole(BeeLifecycleRole role)
        {
            CurrentRole = role;
        }

        public void Kill()
        {
            CurrentStage = BeeLifecycleStage.Dead;
            Health = 0;
        }

        public BeeLifecycleSnapshot ToSnapshot()
        {
            return new BeeLifecycleSnapshot
            {
                BeeId = BeeId,
                HiveId = HiveId,
                BirthTime = Age.BirthTime,
                AgeSeconds = Age.AgeSeconds,
                BiologicalAgeSeconds = Age.BiologicalAgeSeconds,
                CurrentStage = CurrentStage,
                CurrentRole = CurrentRole,
                Health = Health,
                Energy = Energy,
                Experience = Experience,
                GeneticsId = GeneticsId
            };
        }

        public static BeeLifecycleBee FromSnapshot(BeeLifecycleSnapshot snapshot)
        {
            BeeLifecycleBee bee = new BeeLifecycleBee(snapshot.BeeId, snapshot.HiveId, snapshot.BirthTime, snapshot.CurrentRole, snapshot.Health, snapshot.Energy, snapshot.GeneticsId);
            bee.Age = new BeeAgeProfile(snapshot.BirthTime, snapshot.AgeSeconds, snapshot.BiologicalAgeSeconds);
            bee.CurrentStage = snapshot.CurrentStage;
            bee.Experience = snapshot.Experience;
            return bee;
        }

        private static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value is required.", name);
            }

            return value;
        }

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}
