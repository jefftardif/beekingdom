namespace BeeKingdom.Alliance.Research;

// M052-CL: no SQL migration was needed for this mission - dbo.AllianceResearch already stores
// AllianceResearchState as an opaque JSON blob (StateJson), so the DOMAIN MODEL can evolve without
// a schema change, the same way PlayerHiveState evolves under HiveStateMigrator. What DOES need
// handling is that a row written by M051 (the old shape: Technologies/Contributions{3 fields}/
// ProcessedDonationIds only) deserializes into M052's new record type with several genuinely new,
// non-nullable collection fields (Funding/Completed/ProcessedLaunchIds/ProcessedSpeedUpIds) that
// have no matching JSON property in an old row - System.Text.Json's parameterized-constructor
// binding leaves those as null (not an empty collection) when absent, which would NullReferenceException
// the first time any code touches them. This normalizes that, called on every read/mutation
// (mirrors HiveStateMigrator.ToCurrent's own call-on-every-read convention).
//
// Old M051 funding "progress" (an abstract point count on a 9-technology catalog that no longer
// exists) is intentionally NOT carried forward - there is no resource-by-resource breakdown to
// migrate it into, and Alliance Test's real production row is confirmed pristine (0 progress, 0
// contributions - see M051B's report) at the time of this mission. Contributions themselves (which
// DO share a compatible shape - PlayerId/TotalPoints/DonationCount already existed, only
// AllianceCurrencyBalance is new and defaults to 0) survive the transition unchanged.
public static class AllianceResearchStateMigrator
{
    public static AllianceResearchState ToCurrent(AllianceResearchState state)
    {
        if (state.Funding != null! && state.Completed != null! && state.Contributions != null! &&
            state.ProcessedDonationIds != null! && state.ProcessedLaunchIds != null! && state.ProcessedSpeedUpIds != null! &&
            state.DonationAppliedAmounts != null! &&
            state.ModelVersion == AllianceResearchState.CurrentModelVersion)
            return state;

        return state with
        {
            ModelVersion = AllianceResearchState.CurrentModelVersion,
            Funding = state.Funding ?? new Dictionary<string, AllianceTechnologyFunding>(StringComparer.Ordinal),
            Completed = state.Completed ?? new Dictionary<string, AllianceCompletedTechnology>(StringComparer.Ordinal),
            Contributions = state.Contributions ?? new Dictionary<Guid, AllianceResearchContribution>(),
            ProcessedDonationIds = state.ProcessedDonationIds ?? new HashSet<string>(StringComparer.Ordinal),
            ProcessedLaunchIds = state.ProcessedLaunchIds ?? new HashSet<string>(StringComparer.Ordinal),
            ProcessedSpeedUpIds = state.ProcessedSpeedUpIds ?? new HashSet<string>(StringComparer.Ordinal),
            // M054A-CL: a row written before this mission has no per-donation applied-amount
            // breadcrumbs at all - an empty map is correct (not lossy): those historical donations
            // already finished crediting Royal Seals under the M054 formula at the time they ran; a
            // retry of one of those OLD ClientRequestIds is not a realistic scenario this needs to
            // reconstruct (idempotency for them is still fully honored by ProcessedDonationIds/
            // PlayerHiveState.Receipts, which are untouched).
            DonationAppliedAmounts = state.DonationAppliedAmounts ?? new Dictionary<string, long>(StringComparer.Ordinal),
        };
    }
}
