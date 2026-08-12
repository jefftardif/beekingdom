namespace BeeKingdom.Hive
{
    public readonly struct BeeDevelopmentProfile
    {
        public double LarvaAtSeconds { get; }
        public double PupaAtSeconds { get; }
        public double YoungWorkerAtSeconds { get; }
        public double AdultWorkerAtSeconds { get; }
        public double SeniorWorkerAtSeconds { get; }

        public BeeDevelopmentProfile(double larvaAtSeconds, double pupaAtSeconds, double youngWorkerAtSeconds, double adultWorkerAtSeconds, double seniorWorkerAtSeconds)
        {
            LarvaAtSeconds = larvaAtSeconds;
            PupaAtSeconds = pupaAtSeconds;
            YoungWorkerAtSeconds = youngWorkerAtSeconds;
            AdultWorkerAtSeconds = adultWorkerAtSeconds;
            SeniorWorkerAtSeconds = seniorWorkerAtSeconds;
        }
    }
}
