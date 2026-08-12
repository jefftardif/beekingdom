namespace BeeKingdom.Simulation.Configuration;

public sealed class SimulationOptions
{
    public const string SectionName = "Simulation";

    public TimeSpan FixedTickInterval { get; set; } = TimeSpan.FromSeconds(1);
    public int AutoSaveEveryTicks { get; set; } = 300;
    public TimeSpan InactiveUnloadAfter { get; set; } = TimeSpan.FromMinutes(15);
    public int MaxFastForwardTicks { get; set; } = 10000;
    public int MaxColoniesPerTickBatch { get; set; } = 1000;
    public DateTimeOffset SimulationEpochUtc { get; set; } = DateTimeOffset.UnixEpoch;
}
