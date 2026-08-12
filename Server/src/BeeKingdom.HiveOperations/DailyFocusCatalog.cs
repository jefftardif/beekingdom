namespace BeeKingdom.HiveOperations;

// Premiere boucle de retention pensee comme designer, pas comme systeme (demande de Jeff,
// 2026-07-31) : chaque jour civil UTC, UN palier de Combat Patrol et UN noeud de collecte
// mondiale sont "cible du jour" et recoivent un bonus de recompense reel a la validation. Aucun
// nouvel etat persiste - une pure fonction de la date, calculable identiquement cote serveur et
// cote client (miroir d'affichage). Ne touche ni la puissance de combat ni les seuils de
// resolution (CombatPatrolResolution reste inchange) : uniquement la recompense finale, pour que
// la decision reste "vers quoi est-ce que je reoriente mes troupes/collecte aujourd'hui",
// jamais un raccourci de puissance gratuite. Pas de minuteur : le joueur choisit toujours quand
// jouer, seule la cible la plus rentable change chaque jour.
public static class DailyFocusCatalog
{
    public const long RewardBonusBp = 5000; // +50% de recompense sur la cible du jour

    public static int FeaturedCombatTier(DateTimeOffset utcNow, int tierCount = 7)
    {
        if (tierCount <= 0) throw new ArgumentOutOfRangeException(nameof(tierCount));
        long days = DaysSinceEpoch(utcNow);
        return 1 + (int)(days % tierCount);
    }

    public static string? FeaturedWorldResourceNodeId(DateTimeOffset utcNow, IReadOnlyList<string> nodeIdsInCatalogOrder)
    {
        if (nodeIdsInCatalogOrder is null || nodeIdsInCatalogOrder.Count == 0) return null;
        long days = DaysSinceEpoch(utcNow);
        int index = (int)(days % nodeIdsInCatalogOrder.Count);
        return nodeIdsInCatalogOrder[index];
    }

    public static long ApplyRewardBonus(long amount) => amount <= 0 ? amount : amount + checked(amount * RewardBonusBp / 10000);

    private static long DaysSinceEpoch(DateTimeOffset utcNow) => (long)(utcNow.UtcDateTime.Date - DateTime.UnixEpoch.Date).TotalDays;
}
