namespace BeeKingdom.HiveOperations;

public enum WorldEventKind { Weather, ThreatSurge }

// Un evenement du catalogue : Weather cible une ressource de Collecte mondiale (bonus/malus de
// rendement), ThreatSurge cible une famille de troupe deja utilisee par Combat Patrol (bonus de
// recompense uniquement quand le palier affronte est bien de cette famille de danger).
public sealed record WorldEventDefinition(string Key, WorldEventKind Kind, string TargetKey, long BonusBp);

public sealed record ActiveWorldEvent(string Key, WorldEventKind Kind, string TargetKey, long BonusBp, DateTimeOffset EndsAtUtc);

// Premier evenement mondial dynamique (demande de Jeff, 2026-08-01), rendu localise le meme jour
// (deuxieme demande) : contrairement a la Cible du jour (un palier/noeud fixe pour toute la
// journee), la meteo/menace du monde change plusieurs fois par jour (fenetres de 4h, 6 par jour)
// pour donner une raison de revenir plusieurs fois dans la meme journee. Toujours une fonction pure
// du temps (aucun etat persiste, aucun RNG) : le decalage change chaque jour civil pour eviter que
// la meme meteo tombe toujours aux memes heures.
//
// Localisation : la "saveur" active (ex. Invasion de fourmis) ne boost plus TOUS les paliers/noeuds
// de la famille/ressource visee a la fois - un seul, choisi parmi les regions eligibles par
// FeaturedRegionTier/FeaturedRegionNodeId, recoit reellement le bonus ce cycle. Le joueur doit donc
// choisir vers quelle region precise se deplacer plutot que profiter du bonus n'importe ou. Les
// evenements Weather modifient uniquement le rendement de collecte (jamais la puissance de combat) ;
// les evenements ThreatSurge modifient uniquement la recompense de Combat Patrol pour le palier
// localise (jamais la puissance/les seuils de resolution) - meme philosophie que DailyFocusCatalog.
public static class WorldEventCatalog
{
    private const int WindowHours = 4;

    private static readonly IReadOnlyList<WorldEventDefinition> Events =
    [
        new("blossom", WorldEventKind.Weather, "pollen", 2500),
        new("rain", WorldEventKind.Weather, "honey", 2500),
        new("drought", WorldEventKind.Weather, "wax", -2000),
        new("ant_invasion", WorldEventKind.ThreatSurge, "guardians", 2500),
        new("spider_surge", WorldEventKind.ThreatSurge, "darters", 2500),
        new("hornet_swarm", WorldEventKind.ThreatSurge, "wingrunners", 2500)
    ];

    public static ActiveWorldEvent Active(DateTimeOffset utcNow)
    {
        WorldEventDefinition definition = Events[(int)(Cycle(utcNow) % Events.Count)];
        return new ActiveWorldEvent(definition.Key, definition.Kind, definition.TargetKey, definition.BonusBp, NextChangeAtUtc(utcNow));
    }

    // Parmi les paliers de Combat Patrol qui partagent la famille de danger visee par l'evenement
    // actif (deja calculee par l'appelant), lequel est la region precise ciblee ce cycle. Change au
    // meme rythme que la saveur elle-meme (toutes les 4h) pour rester une seule fonction du temps.
    public static int? FeaturedRegionTier(DateTimeOffset utcNow, IReadOnlyList<int> eligibleTiers)
    {
        if (eligibleTiers is null || eligibleTiers.Count == 0) return null;
        return eligibleTiers[(int)(Cycle(utcNow) % eligibleTiers.Count)];
    }

    // Meme principe que FeaturedRegionTier, pour les noeuds de Collecte mondiale partageant la
    // ressource visee par l'evenement Weather actif.
    public static string? FeaturedRegionNodeId(DateTimeOffset utcNow, IReadOnlyList<string> eligibleNodeIds)
    {
        if (eligibleNodeIds is null || eligibleNodeIds.Count == 0) return null;
        return eligibleNodeIds[(int)(Cycle(utcNow) % eligibleNodeIds.Count)];
    }

    public static DateTimeOffset NextChangeAtUtc(DateTimeOffset utcNow)
    {
        DateTimeOffset dayStart = new(utcNow.UtcDateTime.Date, TimeSpan.Zero);
        int hourBucket = utcNow.UtcDateTime.Hour / WindowHours;
        return dayStart.AddHours((hourBucket + 1) * WindowHours);
    }

    public static long ApplyBonusBp(long amount, long bonusBp) => amount <= 0 ? amount : amount + checked(amount * bonusBp / 10000);

    private static long Cycle(DateTimeOffset utcNow)
    {
        long days = (long)(utcNow.UtcDateTime.Date - DateTime.UnixEpoch.Date).TotalDays;
        int hourBucket = utcNow.UtcDateTime.Hour / WindowHours;
        return days + hourBucket;
    }
}
