namespace BeeKingdom.Hive
{
    public sealed class QueenEvolution
    {
        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }
        public int ExperiencePerLevel { get; }

        public QueenEvolution(int experiencePerLevel = 100)
        {
            ExperiencePerLevel = experiencePerLevel <= 0 ? 100 : experiencePerLevel;
        }

        public bool AddExperience(int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            Experience += amount;
            bool leveled = false;
            while (Experience >= ExperiencePerLevel)
            {
                Experience -= ExperiencePerLevel;
                Level++;
                leveled = true;
            }

            return leveled;
        }

        public void Load(int level, int experience)
        {
            Level = level < 1 ? 1 : level;
            Experience = experience < 0 ? 0 : experience;
        }
    }
}
