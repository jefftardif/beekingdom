namespace BeeKingdom.Hive
{
    public readonly struct BeeMortalityProfile
    {
        public double MaximumAgeSeconds { get; }

        public BeeMortalityProfile(double maximumAgeSeconds)
        {
            MaximumAgeSeconds = maximumAgeSeconds < 0d ? 0d : maximumAgeSeconds;
        }
    }
}
