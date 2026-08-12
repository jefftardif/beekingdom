namespace BeeKingdom.Gateway.Diagnostics;

public sealed class GatewayDiagnostics
{
    public long ActiveConnections { get; private set; }
    public long NewConnections { get; private set; }
    public long Disconnections { get; private set; }
    public long BandwidthBytes { get; private set; }
    public long MessagesRouted { get; private set; }
    public long RoutingErrors { get; private set; }
    public double AverageLatency { get; private set; }

    public void RecordConnection(double latency)
    {
        NewConnections++;
        ActiveConnections++;
        AverageLatency = AverageLatency == 0 ? latency : (AverageLatency + latency) / 2d;
    }

    public void RecordDisconnection()
    {
        Disconnections++;
        if (ActiveConnections > 0)
        {
            ActiveConnections--;
        }
    }

    public void RecordMessage(long bytes)
    {
        MessagesRouted++;
        BandwidthBytes += bytes;
    }

    public void RecordRoutingError() => RoutingErrors++;
}
