namespace BeeKingdom.HiveOperations;

// Systeme VIP v1 : uniquement du confort (capacite de stockage), jamais de la puissance
// militaire ou une progression reservee aux payants (voir CLAUDE.md). Les points VIP sont
// destines a etre credites par de vrais achats integres plus tard ; pour l'instant la seule
// source est un octroi manuel de developpement (voir /dev/grant-vip-points).
public static class VipCatalog
{
    public const int MaxLevel = 10;

    // Seuil cumulatif de points VIP pour atteindre chaque niveau (index = niveau).
    public static readonly long[] LevelThresholds =
    {
        0, 100, 300, 700, 1500, 3000, 6000, 12000, 25000, 50000, 100000
    };

    public static int LevelForPoints(long lifetimePoints)
    {
        int level = 0;
        for (int i = 0; i < LevelThresholds.Length; i++)
        {
            if (lifetimePoints < LevelThresholds[i]) break;
            level = i;
        }
        return level;
    }

    public static long? NextThreshold(int level)
    {
        return level + 1 < LevelThresholds.Length ? LevelThresholds[level + 1] : null;
    }

    // Bonus de capacite de stockage (miel/cire/pollen), en points de base (1% = 100 bps), par
    // niveau VIP. Purement du confort : plus de marge avant de devoir recolter, jamais plus de
    // production ni de puissance de combat.
    public const int CapacityBonusBpsPerLevel = 200;

    public static int CapacityBonusBps(int level) => level * CapacityBonusBpsPerLevel;
}
