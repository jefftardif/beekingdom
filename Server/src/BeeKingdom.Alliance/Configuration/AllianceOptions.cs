namespace BeeKingdom.Alliance.Configuration;

// M041-CL: follows the same nested-section feature-flag convention as ChatOptions/
// LivingHiveResearchOptions ("Alliance": { "Enabled": false }), kept distinct from the
// pre-existing flat "LiveAllianceEnabled" readiness-gate flag in appsettings (that one guards
// world/ownership readiness for the whole alliance *concept*, this one gates the Alliance
// service/domain/endpoints themselves - see Docs/Alliance/ALLIANCE_PLATFORM_ARCHITECTURE.md).
public sealed class AllianceOptions
{
    public const string SectionName = "Alliance";

    public bool Enabled { get; init; }
    public bool DiplomacyEnabled { get; init; }
    public bool WarEnabled { get; init; }

    public int MaxMembers { get; init; } = 100;
    public int NameMinLength { get; init; } = 3;
    public int NameMaxLength { get; init; } = 32;
    public int TagMinLength { get; init; } = 2;
    public int TagMaxLength { get; init; } = 5;
    public int DescriptionMaxLength { get; init; } = 500;
    public int SearchPageMaxLimit { get; init; } = 50;
    public int ActivityPageMaxLimit { get; init; } = 100;
    public int IdempotencyReceiptRetentionDays { get; init; } = 30;

    // M042-CL: needed to create/look up the alliance's chat conversation
    // (BeeKingdom.Chat conversations are scoped by GameServerId+WorldId). Alliance itself has no
    // per-world concept (an AllianceEntity has no WorldId field - see
    // ALLIANCE_PLATFORM_ARCHITECTURE.md section 2), so this intentionally mirrors the same
    // well-known default GUIDs as ServerIdentityOptions (BeeKingdom.Server) rather than Alliance
    // depending on that project - kept here instead of hard-coded so it stays configurable if the
    // real per-world/multi-server story ever changes.
    public string GameServerId { get; init; } = "00000000-0000-0000-0000-000000000001";
    public string WorldId { get; init; } = "00000000-0000-0000-0000-000000000101";
}
