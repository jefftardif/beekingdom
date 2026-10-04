namespace BeeKingdom.HiveOperations;

// Premiere brique de l'architecture de deploiement reutilisable (demande de Jeff, 2026-08-01) :
// toute mecanique qui engage reellement des troupes hors de la ruche pour une duree (Combat
// Patrol, et desormais la Collecte mondiale) doit soustraire sa propre reserve ICI, dans un seul
// endroit partage, pour qu'aucune abeille ne puisse jamais etre comptee deux fois entre deux
// systemes engages simultanement. Les futurs systemes de terrain (PvP, raids, renforts, occupation
// de points d'interet) n'auront qu'a ajouter leur propre source de troupes engagees dans
// SumAllCommitted plutot que de re-derouler ce calcul depuis zero.
public static class HiveTroopDeploymentAccounting
{
    public static readonly IReadOnlyList<string> Families = ["guardians", "wingrunners", "darters"];

    public static IReadOnlyDictionary<string, long> ComputeAvailableRoster(PlayerHiveState state)
    {
        IReadOnlyDictionary<string, long> roster = state.DoctrineRoster?.Counts ?? new Dictionary<string, long>();
        IReadOnlyDictionary<string, long> reserved = state.SquadReservation?.Reserved ?? new Dictionary<string, long>();
        Dictionary<string, long> committed = SumAllCommitted(state);
        return Families.ToDictionary(f => f, f => Math.Max(0L, roster.GetValueOrDefault(f) - reserved.GetValueOrDefault(f) - committed.GetValueOrDefault(f)), StringComparer.Ordinal);
    }

    private static Dictionary<string, long> SumAllCommitted(PlayerHiveState state)
    {
        Dictionary<string, long> sums = Families.ToDictionary(f => f, _ => 0L, StringComparer.Ordinal);

        // A combat patrol launched from the Caserne keeps the exact same prepared reservation
        // while it is active. ComputeAvailableRoster already subtracts that reservation, so the
        // matching active encounter must not debit the same bees a second time. Skip at most one
        // matching encounter: legacy/corrupt states containing duplicate active copies remain
        // conservatively accounted rather than accidentally freeing troops.
        SquadReservationState? reservation = state.SquadReservation;
        bool preparedEncounterAbsorbedByReservation = false;
        if (state.CombatPatrol?.ActiveEncounters != null)
        {
            foreach (CombatPatrolActiveEncounter encounter in state.CombatPatrol.ActiveEncounters)
            {
                if (!preparedEncounterAbsorbedByReservation &&
                    reservation?.ReservationId is not null &&
                    SameComposition(encounter.CommittedTroops, reservation.Reserved))
                {
                    preparedEncounterAbsorbedByReservation = true;
                    continue;
                }

                foreach (string family in Families)
                    sums[family] += encounter.CommittedTroops.GetValueOrDefault(family);
            }
        }

        IReadOnlyDictionary<string, long>? worldResourceCommitted = state.WorldResourceCollection?.Active?.CommittedTroops;
        if (worldResourceCommitted != null)
            foreach (string family in Families)
                sums[family] += worldResourceCommitted.GetValueOrDefault(family);
        return sums;
    }

    private static bool SameComposition(
        IReadOnlyDictionary<string, long>? left,
        IReadOnlyDictionary<string, long>? right)
    {
        if (left is null || right is null) return false;
        return Families.All(family =>
            left.GetValueOrDefault(family) == right.GetValueOrDefault(family)) &&
            Families.Sum(family => left.GetValueOrDefault(family)) > 0;
    }

    public static bool IsValidComposition(Dictionary<string, long> requested, int capacity)
    {
        if (requested is null || requested.Count != Families.Count || requested.Keys.Any(k => !Families.Contains(k)) || requested.Values.Any(v => v < 0))
            return false;
        long total;
        try { total = checked(requested.Values.Sum()); } catch (OverflowException) { return false; }
        return total > 0 && total <= capacity;
    }
}
