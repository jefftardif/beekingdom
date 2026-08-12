namespace BeeKingdom.HiveOperations;

public sealed class LivingHiveResearchOptions
{
    public const string SectionName = "LivingHiveResearch";
    public bool Enabled { get; set; }
    public string CatalogVersion { get; set; } = "";
    public List<string> Catalog { get; set; } = [];
}
