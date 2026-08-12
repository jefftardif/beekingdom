namespace BeeKingdom.HiveOperations;

// Seul point d'ecriture du Carnet du Bestiaire (demande de Jeff, 2026-08-01). Appele une seule fois,
// depuis CombatPatrolService.FinishAsync quand resolve == true, avec des donnees deja calculees pour
// le recu de reclamation existant (CombatPatrolClaimReceipt) - aucun calcul de combat duplique ici.
public static class BestiaryCodexAccounting
{
    // Choisi pour recompenser un investissement reel sans exiger un grind excessif - "Maitrisee"
    // doit rester atteignable en quelques sessions, pas seulement apres des mois.
    public const long MasteryEncounterThreshold = 10;

    public static BestiaryCodexState RecordEncounter(
        BestiaryCodexState? existing, int tier, string band, DateTimeOffset nowUtc,
        long honeyCredited, long pollenCredited, IReadOnlyList<string> contributingChampionBeeIds,
        string? strategicPathId, bool worldEventApplied, bool dailyFocusApplied)
    {
        var tiers = new Dictionary<int, BestiaryCodexTierState>(existing?.Tiers ?? new Dictionary<int, BestiaryCodexTierState>());
        tiers.TryGetValue(tier, out BestiaryCodexTierState? prior);

        long encounterCount = (prior?.EncounterCount ?? 0) + 1;
        string bestBand = HigherBand(prior?.BestBand, band);
        bool bestBandImproved = !string.Equals(bestBand, prior?.BestBand, StringComparison.Ordinal);
        bool mastered = (prior?.Mastered ?? false) || encounterCount >= MasteryEncounterThreshold;
        // Legendaire (demande de Jeff) : reutilise exactement le declencheur deja calcule pour la
        // banniere "menace en hausse" localisee sur ce palier au moment de la reclamation - aucune
        // nouvelle regle de jeu, juste sa persistance dans le carnet.
        bool legendary = (prior?.Legendary ?? false) || worldEventApplied;
        DateTimeOffset firstEncounteredAtUtc = prior?.FirstEncounteredAtUtc ?? nowUtc;
        long totalHoney = (prior?.TotalHoneyCredited ?? 0) + honeyCredited;
        long totalPollen = (prior?.TotalPollenCredited ?? 0) + pollenCredited;
        long dailyFocusEncounterCount = (prior?.DailyFocusEncounterCount ?? 0) + (dailyFocusApplied ? 1 : 0);
        // Souvenirs de combat (demande de Jeff, 2026-08-02) : en plus du cumul deja suivi, garder le
        // detail du DERNIER affrontement precis - "voici ce que ce combat precis a rapporte", une
        // memoire plus personnelle qu'un simple total.
        DateTimeOffset? bestBandAchievedAtUtc = bestBandImproved ? nowUtc : prior?.BestBandAchievedAtUtc;

        tiers[tier] = new BestiaryCodexTierState(
            tier, encounterCount, bestBand, mastered, legendary,
            firstEncounteredAtUtc, nowUtc, totalHoney, totalPollen, dailyFocusEncounterCount,
            contributingChampionBeeIds?.ToList() ?? new List<string>(), strategicPathId,
            honeyCredited, pollenCredited, bestBandAchievedAtUtc, band);

        return new BestiaryCodexState(tiers);
    }

    private static string HigherBand(string? current, string incoming)
    {
        if (string.IsNullOrEmpty(current)) return incoming;
        return Rank(incoming) > Rank(current) ? incoming : current;
    }

    private static int Rank(string band) => band switch
    {
        "DecisiveVictory" => 3,
        "Victory" => 2,
        "HardWon" => 1,
        _ => 0
    };
}
