namespace BeeKingdom.HiveOperations;

// M054-CL: the single canonical read/credit surface for a player's "Sceaux Royaux" (Royal Seals)
// balance - see PlayerHiveState.RoyalSeals's own comment for why it is stored there. This class is
// the seam a future Alliance Shop (explicitly NOT built in M054) can call directly, independently
// of Alliance membership or AllianceResearchState - a player who belongs to no Alliance still has a
// real balance here. BeeKingdom.Alliance.Research depends on BeeKingdom.HiveOperations already (see
// AllianceGameplayBonus.cs's own comment on the reverse-dependency-ban), so this lives on the
// correct side of that boundary for the Alliance module to call.
public static class RoyalSealsWallet
{
    // Defensive sum across every hive the player owns (see PlayerHiveState.RoyalSeals's comment) -
    // in the live game today a player has exactly one hive, so this is a single read in practice;
    // it stays correct without a schema change even if that ever stops being true.
    public static async Task<long> GetBalanceAsync(IHiveStateRepository repository, Guid playerId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Guid> hiveIds = await repository.ListHiveIdsAsync(playerId, cancellationToken);
        long total = 0;
        foreach (Guid hiveId in hiveIds)
        {
            PlayerHiveState? state = await repository.ReadAsync(playerId, hiveId, cancellationToken);
            if (state != null) total += state.RoyalSeals;
        }
        return total;
    }

    // Credits `amount` (must be >= 0) to the given hive's wallet inside a caller-supplied atomic
    // PlayerHiveState mutation delegate - a thin, reusable helper so every credit site (donation,
    // migration) applies the exact same "never negative" invariant the same way. Callers own their
    // own idempotency key/Receipts guard around the mutation - this helper only touches the balance
    // field itself.
    public static PlayerHiveState Credit(PlayerHiveState state, long amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Royal Seals credit amount must not be negative.");
        return state with { RoyalSeals = state.RoyalSeals + amount };
    }
}
