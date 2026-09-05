using BeeKingdom.HiveOperations;

namespace BeeKingdom.Alliance.Research;

// M054-CL: one-time backfill moving legacy AllianceCurrencyBalance values (accrued before this
// mission, when Sceaux Royaux were mistakenly Alliance-scoped inside AllianceResearchState.
// Contributions) into the player's real, Alliance-independent wallet (PlayerHiveState.RoyalSeals -
// see RoyalSealsWallet). NOT wired to any live endpoint in M054 - the mission is implementation +
// preparation only. A future M054B would call MigrateAsync from an ops-protected endpoint mirroring
// the existing /ops/migrations/* convention, exactly once, with the CEO watching the returned
// summary.
//
// SAFETY: idempotent via the SAME PlayerHiveState.Receipts mechanism every other paid action in
// this codebase already uses - one receipt key per (AllianceId, PlayerId) pair migrated, so
// re-running this after a partial run (or after it has already fully completed) can never
// double-credit a player, even if this process crashes mid-run or is invoked twice concurrently.
//
// MULTI-ALLIANCE LEGACY EVIDENCE (see M054 report section 8 for the full writeup): a player CAN
// legitimately have a nonzero legacy AllianceCurrencyBalance recorded in more than one Alliance's
// own AllianceResearchState.Contributions dictionary (once while a member of Alliance A, again
// later while a member of Alliance B) - by direct code inspection, nothing in this codebase ever
// copies or carries forward a Contributions entry from one Alliance's state into another's; each
// Alliance's dictionary is mutated ONLY by DonateAsync operating on `membership.AllianceId.Value`
// (the player's CURRENT alliance at the time of that specific donation). Two nonzero legacy values
// for the same player therefore represent two INDEPENDENTLY EARNED amounts, never a duplicated
// snapshot of the same value - summing every (AllianceId, PlayerId) legacy balance exactly once is
// therefore the correct, non-inflating migration rule.
public sealed class RoyalSealsMigrationService
{
    private readonly IAllianceResearchRepository researchRepository;
    private readonly IHiveStateRepository hiveStateRepository;
    private readonly IServerClock clock;

    public RoyalSealsMigrationService(IAllianceResearchRepository researchRepository, IHiveStateRepository hiveStateRepository, IServerClock clock)
    {
        this.researchRepository = researchRepository;
        this.hiveStateRepository = hiveStateRepository;
        this.clock = clock;
    }

    public sealed record MigrationOutcome(
        int AllianceRowsScanned,
        int LegacyBalancesFound,
        int PlayersCredited,
        int AlreadyMigratedSkipped,
        int PlayersWithNoOwnedHive,
        long TotalRoyalSealsMigrated);

    // M054B-CL: `dryRun: true` performs the exact same scan and would-be-outcome computation but
    // never calls ExecuteAtomicallyAsync - a pure read-only preview (via ReadAsync instead) so the
    // CEO can see the real inventory (players affected, total amount) before authorizing the actual
    // apply. Both modes report identical counts for a given database state; only `dryRun: false`
    // actually writes.
    public async Task<MigrationOutcome> MigrateAsync(bool dryRun = false, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Guid> allianceIds = await researchRepository.ListAllAllianceIdsAsync(cancellationToken);
        int legacyFound = 0, credited = 0, alreadyMigrated = 0, noOwnedHive = 0;
        long totalMigrated = 0;

        foreach (Guid allianceId in allianceIds)
        {
            AllianceResearchState? state = await researchRepository.ReadAsync(allianceId, cancellationToken);
            if (state == null) continue;

            foreach ((Guid playerId, AllianceResearchContribution contribution) in state.Contributions)
            {
                if (contribution.AllianceCurrencyBalance <= 0) continue;
                legacyFound++;

                IReadOnlyList<Guid> ownedHiveIds = await hiveStateRepository.ListHiveIdsAsync(playerId, cancellationToken);
                if (ownedHiveIds.Count == 0) { noOwnedHive++; continue; }
                // A player with more than one hive (see PlayerHiveState.RoyalSeals's comment - not
                // observed in the live game today) would still be credited exactly once here: the
                // migration always targets the SAME first-listed hive for a given player, and the
                // idempotency key below is keyed by (AllianceId, PlayerId) alone, not by hive.
                Guid targetHiveId = ownedHiveIds[0];

                string idempotencyKey = "royal-seals-migration:" + allianceId.ToString("N") + ":" + playerId.ToString("N");
                bool wasAlreadyMigrated;
                if (dryRun)
                {
                    PlayerHiveState? current = await hiveStateRepository.ReadAsync(playerId, targetHiveId, cancellationToken);
                    wasAlreadyMigrated = current?.Receipts.ContainsKey(idempotencyKey) ?? false;
                }
                else
                {
                    bool wasAlreadyMigratedCapture = false;
                    await hiveStateRepository.ExecuteAtomicallyAsync(playerId, targetHiveId, hiveState =>
                    {
                        if (hiveState.Receipts.ContainsKey(idempotencyKey)) { wasAlreadyMigratedCapture = true; return hiveState; }
                        Dictionary<string, IdempotencyReceipt> receipts = new(hiveState.Receipts)
                        {
                            [idempotencyKey] = new IdempotencyReceipt("royal-seals-migration", true, "migrated", null, clock.UtcNow)
                        };
                        PlayerHiveState creditedState = RoyalSealsWallet.Credit(hiveState, contribution.AllianceCurrencyBalance);
                        return creditedState with { Revision = hiveState.Revision + 1, Receipts = receipts };
                    }, cancellationToken);
                    wasAlreadyMigrated = wasAlreadyMigratedCapture;
                }

                if (wasAlreadyMigrated) alreadyMigrated++;
                else { credited++; totalMigrated += contribution.AllianceCurrencyBalance; }
            }
        }

        return new MigrationOutcome(allianceIds.Count, legacyFound, credited, alreadyMigrated, noOwnedHive, totalMigrated);
    }
}
