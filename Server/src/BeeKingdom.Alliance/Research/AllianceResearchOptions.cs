namespace BeeKingdom.Alliance.Research;

public sealed class AllianceResearchOptions
{
    public const string SectionName = "AllianceResearch";

    public bool Enabled { get; set; }

    public void Validate()
    {
        // Nothing to validate yet - the technology tree itself is a static Alpha catalog
        // (AllianceResearchCatalog), not configuration, mirroring the personal-Research convention.
    }
}
