namespace BeeKingdom.Services
{
    public sealed class SimulationWorld
    {
        public int Revision { get; private set; }

        public void AdvanceRevision()
        {
            Revision++;
        }

        public void Reset()
        {
            Revision = 0;
        }
    }
}
