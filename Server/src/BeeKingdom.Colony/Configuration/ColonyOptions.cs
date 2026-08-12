namespace BeeKingdom.Colony.Configuration;

public sealed class ColonyOptions
{
    public const string SectionName = "Colony";

    public int MaxSnapshotBytes { get; set; } = 1024 * 1024;
    public TimeSpan AutoSaveInterval { get; set; } = TimeSpan.FromMinutes(5);
    public string CompressionPolicy { get; set; } = "None";
    public int RetentionDays { get; set; } = 30;
    public string VersioningStrategy { get; set; } = "Semantic";
}
