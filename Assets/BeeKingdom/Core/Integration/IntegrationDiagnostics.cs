namespace BeeKingdom.Core.Integration
{
    public sealed class IntegrationDiagnostics
    {
        public int RegisteredBridges { get; private set; }
        public int RoutedMessages { get; private set; }
        public int MissingRoutes { get; private set; }

        public void RecordRegistered(int count) { RegisteredBridges = count; }
        public void RecordRouted() { RoutedMessages++; }
        public void RecordMissingRoute() { MissingRoutes++; }
    }
}
