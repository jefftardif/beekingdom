namespace BeeKingdom.Alliance.Research;

public sealed class AllianceResearchOptions
{
    public const string SectionName = "AllianceResearch";

    public bool Enabled { get; set; }

    // M052-CL: Bible section 11 - "Sceaux Royaux" foundation. A donation of N resource units
    // awards N Contribution points (Bible section 10, unchanged) and floor(N * this ratio)
    // Alliance Currency (a distinct, spendable balance - no spend path exists yet, that's the
    // future Alliance Shop, explicitly out of this mission's scope). Balance-data, not an
    // architectural constant - the Bible explicitly leaves the exact ratio to a later balancing
    // pass (section 26).
    public double AllianceCurrencyPerContributionPoint { get; set; } = 0.1;

    public void Validate()
    {
        // The technology tree itself is a static Alpha catalog (AllianceResearchCatalog), not
        // configuration, mirroring the personal-Research convention.
        if (AllianceCurrencyPerContributionPoint < 0) throw new InvalidDataException("AllianceResearch:AllianceCurrencyPerContributionPoint must not be negative.");
    }
}
