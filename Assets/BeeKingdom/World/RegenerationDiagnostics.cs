namespace BeeKingdom.World
{
    public sealed class RegenerationDiagnostics
    {
        public int NodeCount { get; private set; }
        public int AvailableNodes { get; private set; }
        public int DepletedNodes { get; private set; }
        public int RegeneratedEvents { get; private set; }

        public void Record(int nodeCount, int availableNodes, int depletedNodes)
        {
            NodeCount = nodeCount;
            AvailableNodes = availableNodes;
            DepletedNodes = depletedNodes;
        }

        public void RecordRegenerated()
        {
            RegeneratedEvents++;
        }
    }
}
