using BeeKingdom.Hive;

namespace BeeKingdom.Gameplay
{
    public sealed class StarterPopulationProfile
    {
        public int InitialWorkers { get; }
        public int InitialNurses { get; }
        public int InitialBuilders { get; }
        public int InitialScouts { get; }
        public int InitialSoldiers { get; }
        public int Health { get; }
        public int Energy { get; }
        public BeeLifecycleRules LifecycleRules { get; }

        public int TotalBees => 1 + InitialWorkers + InitialNurses + InitialBuilders + InitialScouts + InitialSoldiers;

        public StarterPopulationProfile(int initialWorkers, int initialNurses, int initialBuilders, int initialScouts, int initialSoldiers, int health, int energy, BeeLifecycleRules lifecycleRules)
        {
            InitialWorkers = initialWorkers < 0 ? 0 : initialWorkers;
            InitialNurses = initialNurses < 0 ? 0 : initialNurses;
            InitialBuilders = initialBuilders < 0 ? 0 : initialBuilders;
            InitialScouts = initialScouts < 0 ? 0 : initialScouts;
            InitialSoldiers = initialSoldiers < 0 ? 0 : initialSoldiers;
            Health = health < 0 ? 0 : health;
            Energy = energy < 0 ? 0 : energy;
            LifecycleRules = lifecycleRules ?? CreateDefaultRules();
        }

        public static StarterPopulationProfile CreateDefault()
        {
            return new StarterPopulationProfile(12, 4, 4, 2, 2, 100, 100, CreateDefaultRules());
        }

        private static BeeLifecycleRules CreateDefaultRules()
        {
            return new BeeLifecycleRules(
                new BeeDevelopmentProfile(3600d, 7200d, 10800d, 14400d, 86400d),
                new BeeMortalityProfile(604800d),
                1f);
        }
    }
}
