using System.Collections.Generic;

namespace BeeKingdom.Hive
{
    public readonly struct BeeTaskCandidate
    {
        public string BeeId { get; }
        public BeeLifecycleRole Role { get; }
        public double AgeSeconds { get; }
        public int Energy { get; }
        public int Experience { get; }
        public int Health { get; }
        public bool IsAvailable { get; }

        public BeeTaskCandidate(string beeId, BeeLifecycleRole role, double ageSeconds, int energy, int experience, int health, bool isAvailable)
        {
            BeeId = beeId;
            Role = role;
            AgeSeconds = ageSeconds;
            Energy = energy;
            Experience = experience;
            Health = health;
            IsAvailable = isAvailable;
        }
    }

    public sealed class TaskAllocator
    {
        public bool TrySelectBee(TaskInstance task, IReadOnlyList<BeeTaskCandidate> candidates, out string beeId)
        {
            beeId = null;
            int bestScore = int.MinValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                BeeTaskCandidate candidate = candidates[i];
                if (!candidate.IsAvailable || candidate.Health <= 0 || candidate.Energy <= 0)
                {
                    continue;
                }

                int score = candidate.Energy + candidate.Health + candidate.Experience;
                if (candidate.Role == task.Definition.PreferredRole)
                {
                    score += 100;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    beeId = candidate.BeeId;
                }
            }

            return beeId != null;
        }
    }
}
