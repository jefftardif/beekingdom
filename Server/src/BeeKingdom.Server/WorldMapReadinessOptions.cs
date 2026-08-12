namespace BeeKingdom.Server;

public sealed class WorldMapReadinessOptions
{
    public const string SectionName = "WorldMapReadiness";

    public string ProductionTarget { get; set; } = "104.129.128.136";
    public string WorldMapStatus { get; set; } = "PreparationOnly";
    public string WorldMapBoundary { get; set; } = "ReadOnlyNonLiveFoundation";
    public bool ProductionRouteProven { get; set; }
    public bool MapGameplayEnabled { get; set; }
    public bool LiveTerritoryEnabled { get; set; }
    public bool LiveAllianceEnabled { get; set; }
    public bool LiveScoutingEnabled { get; set; }
    public bool LiveWarEnabled { get; set; }
    public bool LiveEconomyEnabled { get; set; }
    public bool RealTimeSynchronizationEnabled { get; set; }
    public bool OfficialProgressionEnabled { get; set; }
}
