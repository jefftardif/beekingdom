namespace BeeKingdom.Server;

public sealed class WorldRegistryReadinessOptions
{
    public const string SectionName = "WorldRegistryReadiness";

    public string ProductionTarget { get; set; } = "104.129.128.136";
    public string RegistryStatus { get; set; } = "PreparationOnly";
    public string DefaultWorldDisplayName { get; set; } = "Bee Kingdom 1";
    public string DefaultWorldStatus { get; set; } = "Preparing";
    public string DefaultWorldRegion { get; set; } = "Unassigned";
    public string DefaultWorldLocale { get; set; } = "und";
    public int MinAccountsPerWorld { get; set; } = 800;
    public int MaxAccountsPerWorld { get; set; } = 1500;
    public int MinActivePlayersPerWorld { get; set; } = 300;
    public int MaxActivePlayersPerWorld { get; set; } = 600;
    public int MinVeryActiveDailyPlayers { get; set; } = 100;
    public int MaxVeryActiveDailyPlayers { get; set; } = 300;
    public int MaxPlayersPerAlliance { get; set; } = 100;
    public int? CreatedAccounts { get; set; }
    public int? ActivePlayersEstimate { get; set; }
    public int? VeryActiveDailyPlayersEstimate { get; set; }
    public int? AllianceCount { get; set; }
    public bool ServerRecommended { get; set; }
    public bool ServerFull { get; set; }
    public bool ProductionRouteProven { get; set; }
    public bool WorldSelectionEnabled { get; set; }
    public bool WorldCreationEnabled { get; set; }
    public bool WorldTransferEnabled { get; set; }
    public bool WorldMergeEnabled { get; set; }
    public bool LivePopulationEnabled { get; set; }
    public List<WorldRegistryWorldOptions> Worlds { get; set; } = [];
}

public sealed class WorldRegistryWorldOptions
{
    public string? WorldId { get; set; }
    public string? GameServerId { get; set; }
    public string? DisplayName { get; set; }
    public string? Status { get; set; }
    public string? Region { get; set; }
    public string? Locale { get; set; }
    public int? CreatedAccounts { get; set; }
    public int? ActivePlayersEstimate { get; set; }
    public int? VeryActiveDailyPlayersEstimate { get; set; }
    public int? AllianceCount { get; set; }
    public bool ServerRecommended { get; set; }
    public bool ServerFull { get; set; }
}
