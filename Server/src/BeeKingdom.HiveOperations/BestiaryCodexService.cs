namespace BeeKingdom.HiveOperations;

public sealed record BestiaryCodexEntrySnapshot(
    int Tier, string EnemyName, string HazardFamily, long EncounterCount, string BestBand,
    bool Mastered, bool Legendary, DateTimeOffset? FirstEncounteredAtUtc, DateTimeOffset? LastEncounteredAtUtc,
    long TotalHoneyCredited, long TotalPollenCredited, long DailyFocusEncounterCount,
    IReadOnlyList<string> LastContributingChampionBeeIds, string? LastStrategicPathId,
    long LastHoneyCredited, long LastPollenCredited, DateTimeOffset? BestBandAchievedAtUtc, string LastBand);

public sealed record BestiaryCodexSnapshot(DateTimeOffset ServerTimeUtc, IReadOnlyList<BestiaryCodexEntrySnapshot> Tiers, int MasteredTierCount, int TotalTierCount, long MasteryEncounterThreshold);

// Carnet du Bestiaire (demande de Jeff, 2026-08-01) : lecture seule, sous-produit du flux Combat
// Patrol existant (voir BestiaryCodexAccounting + CombatPatrolService.FinishAsync) - meme forme que
// WorldPresenceService, pas de mutation possible depuis ce service.
public sealed class BestiaryCodexService(IHiveStateRepository repository, IServerClock clock)
{
    public const string ContractVersion = "living-bestiary-codex-v1";

    public async Task<BestiaryCodexSnapshot> ReadAsync(Guid playerId, Guid hiveId, CancellationToken ct = default)
    {
        PlayerHiveState state = await repository.ReadAsync(playerId, hiveId, ct) ?? throw new KeyNotFoundException();
        DateTimeOffset now = clock.UtcNow;
        var entries = new List<BestiaryCodexEntrySnapshot>();
        int masteredCount = 0;
        foreach (KeyValuePair<int, BestiaryTierDefinition> tier in CombatPatrolCatalog.Tiers.OrderBy(t => t.Key))
        {
            BestiaryCodexTierState? entry = null;
            state.BestiaryCodex?.Tiers.TryGetValue(tier.Key, out entry);
            if (entry is not null && entry.Mastered) masteredCount++;
            entries.Add(new BestiaryCodexEntrySnapshot(
                tier.Key, tier.Value.EnemyName, tier.Value.HazardFamily,
                entry?.EncounterCount ?? 0, entry?.BestBand ?? string.Empty,
                entry?.Mastered ?? false, entry?.Legendary ?? false,
                entry?.FirstEncounteredAtUtc, entry?.LastEncounteredAtUtc,
                entry?.TotalHoneyCredited ?? 0, entry?.TotalPollenCredited ?? 0, entry?.DailyFocusEncounterCount ?? 0,
                entry?.LastContributingChampionBeeIds ?? new List<string>(), entry?.LastStrategicPathId,
                entry?.LastHoneyCredited ?? 0, entry?.LastPollenCredited ?? 0, entry?.BestBandAchievedAtUtc,
                entry?.LastBand ?? string.Empty));
        }
        return new BestiaryCodexSnapshot(now, entries, masteredCount, CombatPatrolCatalog.Tiers.Count, BestiaryCodexAccounting.MasteryEncounterThreshold);
    }
}
