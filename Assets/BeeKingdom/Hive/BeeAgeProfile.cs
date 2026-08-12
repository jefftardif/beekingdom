namespace BeeKingdom.Hive
{
    public readonly struct BeeAgeProfile
    {
        public double BirthTime { get; }
        public double AgeSeconds { get; }
        public double BiologicalAgeSeconds { get; }

        public BeeAgeProfile(double birthTime, double ageSeconds, double biologicalAgeSeconds)
        {
            BirthTime = birthTime;
            AgeSeconds = ageSeconds < 0d ? 0d : ageSeconds;
            BiologicalAgeSeconds = biologicalAgeSeconds < 0d ? 0d : biologicalAgeSeconds;
        }

        public BeeAgeProfile Advance(double deltaSeconds, float biologicalMultiplier)
        {
            if (deltaSeconds <= 0d)
            {
                return this;
            }

            return new BeeAgeProfile(BirthTime, AgeSeconds + deltaSeconds, BiologicalAgeSeconds + (deltaSeconds * biologicalMultiplier));
        }
    }
}
