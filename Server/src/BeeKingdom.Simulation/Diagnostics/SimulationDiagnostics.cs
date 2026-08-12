namespace BeeKingdom.Simulation.Diagnostics;

public sealed class SimulationDiagnostics
{
    public long TotalTicks { get; private set; }
    public long TotalTickTicks { get; private set; }
    public long ColoniesSimulated { get; private set; }
    public long ColoniesLoaded { get; private set; }
    public long SaveTicks { get; private set; }
    public long SnapshotsProduced { get; private set; }
    public double CpuUsagePercent { get; private set; }
    public long MemoryBytes { get; private set; }

    public double AverageTickMilliseconds => TotalTicks == 0 ? 0 : TimeSpan.FromTicks(TotalTickTicks / TotalTicks).TotalMilliseconds;
    public double TicksPerSecond => TotalTickTicks == 0 ? 0 : TotalTicks / TimeSpan.FromTicks(TotalTickTicks).TotalSeconds;

    public void RecordTick(long elapsedTicks, int coloniesSimulated)
    {
        TotalTicks++;
        TotalTickTicks += elapsedTicks;
        ColoniesSimulated += coloniesSimulated;
        MemoryBytes = GC.GetTotalMemory(false);
    }

    public void SetColoniesLoaded(long coloniesLoaded) => ColoniesLoaded = coloniesLoaded;

    public void RecordSave(long elapsedTicks)
    {
        SaveTicks += elapsedTicks;
        SnapshotsProduced++;
    }

    public void SetCpuUsage(double cpuUsagePercent) => CpuUsagePercent = cpuUsagePercent;
}
