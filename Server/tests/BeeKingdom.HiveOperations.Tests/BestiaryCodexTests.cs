using BeeKingdom.HiveOperations;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class BestiaryCodexTests
{
    [Fact]
    public void RecordEncounter_accumulates_counts_best_band_and_totals_across_repeated_encounters()
    {
        DateTimeOffset t0 = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);
        BestiaryCodexState? state = null;

        state = BestiaryCodexAccounting.RecordEncounter(state, 2, "HardWon", t0, honeyCredited: 30, pollenCredited: 10, contributingChampionBeeIds: [], strategicPathId: null, worldEventApplied: false, dailyFocusApplied: false);
        state = BestiaryCodexAccounting.RecordEncounter(state, 2, "DecisiveVictory", t0.AddMinutes(5), honeyCredited: 45, pollenCredited: 15, contributingChampionBeeIds: ["bee-1"], strategicPathId: "aggressive", worldEventApplied: false, dailyFocusApplied: true);

        BestiaryCodexTierState tier2 = state.Tiers[2];
        Assert.Equal(2, tier2.EncounterCount);
        Assert.Equal("DecisiveVictory", tier2.BestBand);
        Assert.False(tier2.Mastered);
        Assert.False(tier2.Legendary);
        Assert.Equal(t0, tier2.FirstEncounteredAtUtc);
        Assert.Equal(t0.AddMinutes(5), tier2.LastEncounteredAtUtc);
        Assert.Equal(75, tier2.TotalHoneyCredited);
        Assert.Equal(25, tier2.TotalPollenCredited);
        Assert.Equal(1, tier2.DailyFocusEncounterCount);
        Assert.Equal(new List<string> { "bee-1" }, tier2.LastContributingChampionBeeIds);
        Assert.Equal("aggressive", tier2.LastStrategicPathId);
        // Souvenir du dernier combat : distinct du cumul, reflete uniquement le 2e affrontement.
        Assert.Equal(45, tier2.LastHoneyCredited);
        Assert.Equal(15, tier2.LastPollenCredited);
        Assert.Equal(t0.AddMinutes(5), tier2.BestBandAchievedAtUtc);
        Assert.Equal("DecisiveVictory", tier2.LastBand);

        // A later, weaker outcome must never downgrade the best band already recorded.
        state = BestiaryCodexAccounting.RecordEncounter(state, 2, "HardWon", t0.AddMinutes(10), 5, 5, [], null, false, false);
        Assert.Equal("DecisiveVictory", state.Tiers[2].BestBand);
        Assert.Equal(3, state.Tiers[2].EncounterCount);
        // Le souvenir "dernier combat" avance meme quand ce n'est pas un record, mais la date du
        // meilleur combat ne bouge pas puisque HardWon < DecisiveVictory deja obtenu.
        Assert.Equal(5, state.Tiers[2].LastHoneyCredited);
        Assert.Equal(t0.AddMinutes(5), state.Tiers[2].BestBandAchievedAtUtc);
        Assert.Equal("HardWon", state.Tiers[2].LastBand);
    }

    [Fact]
    public void RecordEncounter_flags_legendary_immediately_and_mastered_only_after_threshold()
    {
        BestiaryCodexState? state = BestiaryCodexAccounting.RecordEncounter(null, 5, "Victory", DateTimeOffset.UtcNow, 10, 5, [], null, worldEventApplied: true, dailyFocusApplied: false);
        Assert.True(state.Tiers[5].Legendary);
        Assert.False(state.Tiers[5].Mastered);

        for (int i = 0; i < BestiaryCodexAccounting.MasteryEncounterThreshold - 1; i++)
            state = BestiaryCodexAccounting.RecordEncounter(state, 5, "Victory", DateTimeOffset.UtcNow, 10, 5, [], null, false, false);

        Assert.Equal(BestiaryCodexAccounting.MasteryEncounterThreshold, state.Tiers[5].EncounterCount);
        Assert.True(state.Tiers[5].Mastered);
        Assert.True(state.Tiers[5].Legendary); // never clears once earned
    }

    [Fact]
    public async Task Claim_updates_the_codex_for_the_fought_tier_and_leaves_other_tiers_untouched()
    {
        string root = Temp(); Guid p = Guid.NewGuid(); Guid h = Guid.NewGuid();
        var clock = new MutableClock(new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero));
        try
        {
            var repo = Repo(root, p, h, guardians: 0, wingrunners: 0, darters: 18, guardPostLevel: 2);
            var service = new CombatPatrolService(repo, clock);
            CombatPatrolResult launch = await service.LaunchAsync(new(p, h, 2, 0, 0, 18, 0, "launch"), default);
            Assert.True(launch.Succeeded, launch.Code);
            Guid encounterId = launch.Snapshot.ActiveEncounters[0].EncounterId;
            clock.Advance(CombatPatrolCatalog.Tiers[2].Duration);

            CombatPatrolResult claim = await service.ClaimAsync(new(p, h, encounterId, 1, "claim"), default);
            Assert.True(claim.Succeeded, claim.Code);

            PlayerHiveState after = await repo.ReadAsync(p, h, default) ?? throw new InvalidOperationException();
            Assert.NotNull(after.BestiaryCodex);
            BestiaryCodexTierState tier2Codex = after.BestiaryCodex!.Tiers[2];
            Assert.Equal(1, tier2Codex.EncounterCount);
            Assert.Equal(claim.ClaimReceipt!.Band, tier2Codex.BestBand);
            Assert.Equal(claim.ClaimReceipt.CreditedByResource["honey"], tier2Codex.TotalHoneyCredited);
            Assert.Equal(claim.ClaimReceipt.CreditedByResource["pollen"], tier2Codex.TotalPollenCredited);
            Assert.False(after.BestiaryCodex.Tiers.ContainsKey(1));

            BestiaryCodexSnapshot snapshot = await new BestiaryCodexService(repo, clock).ReadAsync(p, h, default);
            Assert.Equal(CombatPatrolCatalog.Tiers.Count, snapshot.TotalTierCount);
            BestiaryCodexEntrySnapshot entry2 = snapshot.Tiers.Single(t => t.Tier == 2);
            Assert.Equal(1, entry2.EncounterCount);
            Assert.Equal(CombatPatrolCatalog.Tiers[2].EnemyName, entry2.EnemyName);
            BestiaryCodexEntrySnapshot entry1 = snapshot.Tiers.Single(t => t.Tier == 1);
            Assert.Equal(0, entry1.EncounterCount);
            Assert.Equal(0, snapshot.MasteredTierCount);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static string Temp() => Path.Combine(Path.GetTempPath(), "bee-bestiary-codex-" + Guid.NewGuid().ToString("N"));

    private static DurableJsonHiveStateRepository Repo(string root, Guid p, Guid h, long guardians, long wingrunners, long darters, int guardPostLevel = 0, long honey = 100_000, long pollen = 100_000)
    {
        var repo = new DurableJsonHiveStateRepository(root, (_, _) => new PlayerHiveState(
            p, h, HiveStateMigrator.CurrentModelVersion, 0,
            new Dictionary<string, ResourceBalance> { ["honey"] = new(honey, 1_000_000), ["pollen"] = new(pollen, 1_000_000) },
            new Dictionary<string, int> { ["guard_post"] = guardPostLevel }, [], new(),
            DoctrineRoster: new DoctrineRosterState(0, new() { ["guardians"] = guardians, ["wingrunners"] = wingrunners, ["darters"] = darters }, null, new())));
        repo.ExecuteAtomicallyAsync(p, h, s => s).GetAwaiter().GetResult();
        return repo;
    }

    private sealed class MutableClock(DateTimeOffset now) : IServerClock { public DateTimeOffset UtcNow { get; private set; } = now; public void Advance(TimeSpan value) => UtcNow += value; }
}
