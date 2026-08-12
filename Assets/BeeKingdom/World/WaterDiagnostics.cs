namespace BeeKingdom.World
{
    public sealed class WaterDiagnostics
    {
        public int SourceCount { get; private set; }
        public double AvailableWater { get; private set; }
        public double TransportedWater { get; private set; }
        public int DepletedSources { get; private set; }

        public void RecordSources(int sourceCount, double availableWater)
        {
            SourceCount = sourceCount;
            AvailableWater = availableWater;
        }

        public void RecordTransport(double amount)
        {
            TransportedWater += amount;
        }

        public void RecordDepleted()
        {
            DepletedSources++;
        }
    }
}
