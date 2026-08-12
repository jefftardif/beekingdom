namespace BeeKingdom.World
{
    public sealed class EcologicalBalance
    {
        public double PollinationFactor { get; private set; }
        public double ClimateFactor { get; private set; }
        public double BiomeFactor { get; private set; }
        public double CombinedFactor => PollinationFactor * ClimateFactor * BiomeFactor;

        public EcologicalBalance(double pollinationFactor = 1d, double climateFactor = 1d, double biomeFactor = 1d)
        {
            PollinationFactor = Sanitize(pollinationFactor);
            ClimateFactor = Sanitize(climateFactor);
            BiomeFactor = Sanitize(biomeFactor);
        }

        public void SetPollination(double value)
        {
            PollinationFactor = Sanitize(value);
        }

        public void SetClimate(double value)
        {
            ClimateFactor = Sanitize(value);
        }

        public void SetBiome(double value)
        {
            BiomeFactor = Sanitize(value);
        }

        private static double Sanitize(double value)
        {
            return value < 0d ? 0d : value;
        }
    }
}
