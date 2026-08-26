using BeeKingdom.HiveOperations;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class CombatPatrolServiceTests
{
    [Fact]
    public async Task Launch_is_blocked_when_underpowered_with_zero_mutation()
    {
        string root = Temp(); Guid p = Guid.NewGuid(); Guid h = Guid.NewGuid(); var clock = new MutableClock(new(2026, 7, 25, 9, 0, 0, TimeSpan.Zero));
        try
        {
            var repo = Repo(root, p, h, guardians: 1, wingrunners: 0, darters: 0);
            var service = new CombatPatrolService(repo, clock);
            CombatPatrolSnapshot before = await service.ReadAsync(p, h, default);

            CombatPatrolResult result = await service.LaunchAsync(new(p, h, 3, 1, 0, 0, 0, "launch-1"), default);

            Assert.False(result.Succeeded);
            Assert.Equal("game.patrol_underpowered", result.Code);
            Assert.Equal(before.Revision, result.Snapshot.Revision);
            Assert.Empty(result.Snapshot.ActiveEncounters);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Launch_then_claim_applies_real_losses_and_credits_reward()
    {
        string root = Temp(); Guid p = Guid.NewGuid(); Guid h = Guid.NewGuid(); var clock = new MutableClock(new(2026, 7, 25, 9, 0, 0, TimeSpan.Zero));
        try
        {
            // Tier 2 (guardians hazard, required=90); darters are disadvantaged against it -> HardWon band with real losses.
            var repo = Repo(root, p, h, guardians: 0, wingrunners: 0, darters: 18, guardPostLevel: 2);
            var service = new CombatPatrolService(repo, clock);

            CombatPatrolResult launch = await service.LaunchAsync(new(p, h, 2, 0, 0, 18, 0, "launch"), default);
            Assert.True(launch.Succeeded, launch.Code);
            Assert.Single(launch.Snapshot.ActiveEncounters);
            Guid encounterId = launch.Snapshot.ActiveEncounters[0].EncounterId;

            clock.Advance(CombatPatrolCatalog.Tiers[2].Duration);
            CombatPatrolResult claim = await service.ClaimAsync(new(p, h, encounterId, 1, "claim"), default);

            Assert.True(claim.Succeeded, claim.Code);
            Assert.Equal("game.patrol_hard_won", claim.Code);
            Assert.NotNull(claim.ClaimReceipt);
            long permanent = claim.ClaimReceipt!.PermanentLosses["darters"];
            long wounded = claim.ClaimReceipt.WoundedLosses["darters"];
            Assert.True(wounded > 0);
            Assert.True(permanent < wounded); // most losses are wounded/recoverable, only a small share is permanent
            Assert.True(claim.ClaimReceipt.CreditedByResource["honey"] > 0);
            Assert.Empty(claim.Snapshot.ActiveEncounters);
            Assert.Single(claim.Snapshot.Recovering);
            Assert.Equal(wounded, claim.Snapshot.Recovering[0].Count);

            PlayerHiveState after = await repo.ReadAsync(p, h, default) ?? throw new InvalidOperationException();
            Assert.Equal(18 - permanent - wounded, after.DoctrineRoster!.Counts["darters"]);

            // Once the recovery window elapses, a later read matures the wounded batch back into the roster.
            clock.Advance(CombatPatrolResolution.ComputeRecoveryDuration(CombatPatrolCatalog.Tiers[2]));
            CombatPatrolSnapshot recovered = await service.ReadAsync(p, h, default);
            Assert.Empty(recovered.Recovering);
            PlayerHiveState healed = await repo.ReadAsync(p, h, default) ?? throw new InvalidOperationException();
            Assert.Equal(18 - permanent, healed.DoctrineRoster!.Counts["darters"]);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Claim_sets_tier_cooldown_that_blocks_immediate_relaunch()
    {
        string root = Temp(); Guid p = Guid.NewGuid(); Guid h = Guid.NewGuid(); var clock = new MutableClock(new(2026, 7, 25, 9, 0, 0, TimeSpan.Zero));
        try
        {
            var repo = Repo(root, p, h, guardians: 18, wingrunners: 0, darters: 0, guardPostLevel: 2);
            var service = new CombatPatrolService(repo, clock);
            CombatPatrolResult launch = await service.LaunchAsync(new(p, h, 2, 18, 0, 0, 0, "launch"), default);
            Guid encounterId = launch.Snapshot.ActiveEncounters[0].EncounterId;
            clock.Advance(CombatPatrolCatalog.Tiers[2].Duration);
            CombatPatrolResult claim = await service.ClaimAsync(new(p, h, encounterId, 1, "claim"), default);
            Assert.True(claim.Succeeded, claim.Code);

            CombatPatrolResult relaunch = await service.LaunchAsync(new(p, h, 2, 18, 0, 0, claim.Snapshot.Revision, "relaunch"), default);

            Assert.False(relaunch.Succeeded);
            Assert.Equal("game.patrol_cooldown_active", relaunch.Code);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Recall_returns_squad_without_loss_reward_or_cooldown()
    {
        string root = Temp(); Guid p = Guid.NewGuid(); Guid h = Guid.NewGuid(); var clock = new MutableClock(new(2026, 7, 25, 9, 0, 0, TimeSpan.Zero));
        try
        {
            var repo = Repo(root, p, h, guardians: 18, wingrunners: 0, darters: 0, guardPostLevel: 2);
            var service = new CombatPatrolService(repo, clock);
            CombatPatrolResult launch = await service.LaunchAsync(new(p, h, 2, 18, 0, 0, 0, "launch"), default);
            Guid encounterId = launch.Snapshot.ActiveEncounters[0].EncounterId;
            await repo.ExecuteAtomicallyAsync(p, h, s => s with { SpeedUps = new Dictionary<string, int> { [CombatPatrolService.RecallItemId] = 1 } });

            CombatPatrolResult recall = await service.RecallAsync(new(p, h, encounterId, 1, "recall"), default);

            Assert.True(recall.Succeeded, recall.Code);
            Assert.Equal("game.patrol_recalled", recall.Code);
            Assert.Null(recall.ClaimReceipt);
            Assert.Empty(recall.Snapshot.TierCooldownEndsAtUtc);
            PlayerHiveState after = await repo.ReadAsync(p, h, default) ?? throw new InvalidOperationException();
            Assert.Equal(18, after.DoctrineRoster!.Counts["guardians"]);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Launch_replay_is_idempotent_and_squad_release_is_blocked_while_active()
    {
        string root = Temp(); Guid p = Guid.NewGuid(); Guid h = Guid.NewGuid(); var clock = new MutableClock(new(2026, 7, 25, 9, 0, 0, TimeSpan.Zero));
        try
        {
            var repo = Repo(root, p, h, guardians: 18, wingrunners: 0, darters: 0, guardPostLevel: 2, reservedGuardians: 5);
            var service = new CombatPatrolService(repo, clock);

            CombatPatrolResult launch = await service.LaunchAsync(new(p, h, 2, 13, 0, 0, 0, "launch-1"), default);
            Assert.True(launch.Succeeded, launch.Code);
            CombatPatrolResult replay = await new CombatPatrolService(repo, clock).LaunchAsync(new(p, h, 2, 13, 0, 0, 0, "launch-1"), default);
            Assert.True(replay.Succeeded);
            Assert.Equal(launch.Snapshot.ActiveEncounters[0].EncounterId, replay.Snapshot.ActiveEncounters[0].EncounterId);

            var release = await new CombatSquadReservationService(repo).ReleaseAsync(new(p, h, 0, "release"), default);
            Assert.Equal("game.squad_in_use", release.Code);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Two_concurrent_patrols_on_different_tiers_do_not_interfere()
    {
        string root = Temp(); Guid p = Guid.NewGuid(); Guid h = Guid.NewGuid(); var clock = new MutableClock(new(2026, 7, 25, 9, 0, 0, TimeSpan.Zero));
        try
        {
            // Base slot count is 1 — buy one resource slot so a second concurrent patrol is allowed.
            var repo = Repo(root, p, h, guardians: 20, wingrunners: 20, darters: 0, guardPostLevel: 4);
            var service = new CombatPatrolService(repo, clock);
            CombatPatrolResult purchase = await service.PurchaseResourceSlotAsync(new(p, h, 0, "buy-slot"), default);
            Assert.True(purchase.Succeeded, purchase.Code);

            CombatPatrolResult first = await service.LaunchAsync(new(p, h, 1, 0, 20, 0, purchase.Snapshot.Revision, "launch-a"), default);
            Assert.True(first.Succeeded, first.Code);
            CombatPatrolResult second = await service.LaunchAsync(new(p, h, 2, 20, 0, 0, first.Snapshot.Revision, "launch-b"), default);
            Assert.True(second.Succeeded, second.Code);

            Assert.Equal(2, second.Snapshot.ActiveEncounters.Count);

            clock.Advance(CombatPatrolCatalog.Tiers[1].Duration);
            CombatPatrolResult claimFirst = await service.ClaimAsync(new(p, h, first.Snapshot.ActiveEncounters[0].EncounterId, second.Snapshot.Revision, "claim-a"), default);
            Assert.True(claimFirst.Succeeded, claimFirst.Code);
            Assert.Single(claimFirst.Snapshot.ActiveEncounters);
            Assert.Equal(second.Snapshot.ActiveEncounters[1].EncounterId, claimFirst.Snapshot.ActiveEncounters[0].EncounterId);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Third_concurrent_patrol_is_blocked_without_a_purchased_slot()
    {
        string root = Temp(); Guid p = Guid.NewGuid(); Guid h = Guid.NewGuid(); var clock = new MutableClock(new(2026, 7, 25, 9, 0, 0, TimeSpan.Zero));
        try
        {
            // Tier 1 hazard is wingrunners; guardians are disadvantaged against it but 10 is still
            // enough to clear the launch gate (readiness ~9250bp, HardWon band).
            var repo = Repo(root, p, h, guardians: 40, wingrunners: 0, darters: 0, guardPostLevel: 0);
            var service = new CombatPatrolService(repo, clock);

            CombatPatrolResult first = await service.LaunchAsync(new(p, h, 1, 10, 0, 0, 0, "launch-a"), default);
            Assert.True(first.Succeeded, first.Code);

            CombatPatrolResult second = await service.LaunchAsync(new(p, h, 1, 10, 0, 0, first.Snapshot.Revision, "launch-b"), default);

            Assert.False(second.Succeeded);
            Assert.Equal("game.patrol_no_slot_available", second.Code);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Committed_troops_are_unavailable_for_a_second_patrol_until_claimed()
    {
        string root = Temp(); Guid p = Guid.NewGuid(); Guid h = Guid.NewGuid(); var clock = new MutableClock(new(2026, 7, 25, 9, 0, 0, TimeSpan.Zero));
        try
        {
            var repo = Repo(root, p, h, guardians: 10, wingrunners: 0, darters: 0, guardPostLevel: 2);
            var service = new CombatPatrolService(repo, clock);
            CombatPatrolResult purchase = await service.PurchaseResourceSlotAsync(new(p, h, 0, "buy-slot"), default);
            Assert.True(purchase.Succeeded, purchase.Code);

            CombatPatrolResult first = await service.LaunchAsync(new(p, h, 1, 10, 0, 0, purchase.Snapshot.Revision, "launch-a"), default);
            Assert.True(first.Succeeded, first.Code);
            Assert.Equal(0, first.Snapshot.AvailableRoster["guardians"]);

            CombatPatrolResult second = await service.LaunchAsync(new(p, h, 1, 5, 0, 0, first.Snapshot.Revision, "launch-b"), default);
            Assert.False(second.Succeeded);
            Assert.Equal("game.patrol_insufficient_troops", second.Code);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Purchase_resource_slot_debits_honey_and_pollen_and_refuses_beyond_two()
    {
        string root = Temp(); Guid p = Guid.NewGuid(); Guid h = Guid.NewGuid(); var clock = new MutableClock(new(2026, 7, 25, 9, 0, 0, TimeSpan.Zero));
        try
        {
            var repo = Repo(root, p, h, guardians: 0, wingrunners: 0, darters: 0, honey: 10_000, pollen: 10_000);
            var service = new CombatPatrolService(repo, clock);

            CombatPatrolResult first = await service.PurchaseResourceSlotAsync(new(p, h, 0, "buy-1"), default);
            Assert.True(first.Succeeded, first.Code);
            Assert.Equal(1, first.Snapshot.ResourcePurchasedSlots);
            Assert.Equal(2, first.Snapshot.TotalSlots);

            CombatPatrolResult second = await service.PurchaseResourceSlotAsync(new(p, h, first.Snapshot.Revision, "buy-2"), default);
            Assert.True(second.Succeeded, second.Code);
            Assert.Equal(2, second.Snapshot.ResourcePurchasedSlots);
            Assert.Equal(3, second.Snapshot.TotalSlots);
            Assert.Null(second.Snapshot.NextResourceSlotCost);

            PlayerHiveState after = await repo.ReadAsync(p, h, default) ?? throw new InvalidOperationException();
            Assert.Equal(10_000 - 800 - 2200, after.Resources["honey"].Amount);
            Assert.Equal(10_000 - 500 - 1400, after.Resources["pollen"].Amount);

            CombatPatrolResult third = await service.PurchaseResourceSlotAsync(new(p, h, second.Snapshot.Revision, "buy-3"), default);
            Assert.False(third.Succeeded);
            Assert.Equal("game.patrol_slot_limit_reached", third.Code);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Purchase_resource_slot_refuses_when_insufficient_resources()
    {
        string root = Temp(); Guid p = Guid.NewGuid(); Guid h = Guid.NewGuid(); var clock = new MutableClock(new(2026, 7, 25, 9, 0, 0, TimeSpan.Zero));
        try
        {
            var repo = Repo(root, p, h, guardians: 0, wingrunners: 0, darters: 0, honey: 10, pollen: 10);
            var service = new CombatPatrolService(repo, clock);

            CombatPatrolResult result = await service.PurchaseResourceSlotAsync(new(p, h, 0, "buy-1"), default);

            Assert.False(result.Succeeded);
            Assert.Equal("game.insufficient_resources", result.Code);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Grant_premium_slot_refuses_beyond_two()
    {
        string root = Temp(); Guid p = Guid.NewGuid(); Guid h = Guid.NewGuid(); var clock = new MutableClock(new(2026, 7, 25, 9, 0, 0, TimeSpan.Zero));
        try
        {
            var repo = Repo(root, p, h, guardians: 0, wingrunners: 0, darters: 0);
            var service = new CombatPatrolService(repo, clock);

            CombatPatrolResult first = await service.GrantPremiumSlotAsync(new(p, h, 0, "grant-1"), default);
            Assert.True(first.Succeeded, first.Code);
            CombatPatrolResult second = await service.GrantPremiumSlotAsync(new(p, h, first.Snapshot.Revision, "grant-2"), default);
            Assert.True(second.Succeeded, second.Code);
            Assert.Equal(3, second.Snapshot.TotalSlots);

            CombatPatrolResult third = await service.GrantPremiumSlotAsync(new(p, h, second.Snapshot.Revision, "grant-3"), default);
            Assert.False(third.Succeeded);
            Assert.Equal("game.patrol_slot_limit_reached", third.Code);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Isolation_is_scoped_per_player_and_hive()
    {
        string root = Temp(); Guid p = Guid.NewGuid(); Guid h = Guid.NewGuid(); var clock = new MutableClock(new(2026, 7, 25, 9, 0, 0, TimeSpan.Zero));
        try
        {
            var repo = Repo(root, p, h, guardians: 18, wingrunners: 0, darters: 0);
            var service = new CombatPatrolService(repo, clock);
            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ReadAsync(Guid.NewGuid(), h, default));
            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ReadAsync(p, Guid.NewGuid(), default));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Claim_applies_daily_focus_bonus_only_to_the_featured_tier()
    {
        string root = Temp(); Guid p = Guid.NewGuid(); Guid h = Guid.NewGuid(); var clock = new MutableClock(new(2026, 7, 25, 9, 0, 0, TimeSpan.Zero));
        try
        {
            int featuredTier = DailyFocusCatalog.FeaturedCombatTier(clock.UtcNow);
            int otherTier = featuredTier == 1 ? 2 : 1;
            // Large, balanced squad so readiness clears comfortably regardless of which family
            // the featured/other tier's hazard disadvantages - keeps the test independent of
            // which tier happens to be featured on the fixed clock date above.
            var repo = Repo(root, p, h, guardians: 300, wingrunners: 300, darters: 300, guardPostLevel: 100, honey: 0, pollen: 0);
            var service = new CombatPatrolService(repo, clock);
            Dictionary<string, long> squad = new() { ["guardians"] = 100, ["wingrunners"] = 100, ["darters"] = 100 };

            CombatPatrolResolutionResult expectedFeatured = CombatPatrolResolution.Resolve(squad, CombatPatrolCatalog.Tiers[featuredTier]);
            CombatPatrolResult launch = await service.LaunchAsync(new(p, h, featuredTier, 100, 100, 100, 0, "launch-featured"), default);
            Assert.True(launch.Succeeded, launch.Code);
            clock.Advance(CombatPatrolCatalog.Tiers[featuredTier].Duration);
            CombatPatrolResult claim = await service.ClaimAsync(new(p, h, launch.Snapshot.ActiveEncounters[0].EncounterId, launch.Snapshot.Revision, "claim-featured"), default);
            Assert.True(claim.Succeeded, claim.Code);
            Assert.True(claim.ClaimReceipt!.DailyFocusApplied);
            Assert.Equal(DailyFocusCatalog.ApplyRewardBonus(expectedFeatured.HoneyCredited), claim.ClaimReceipt.CreditedByResource["honey"]);
            Assert.Equal(DailyFocusCatalog.ApplyRewardBonus(expectedFeatured.PollenCredited), claim.ClaimReceipt.CreditedByResource["pollen"]);
            Assert.True(claim.Snapshot.FeaturedTier == featuredTier);

            CombatPatrolResolutionResult expectedOther = CombatPatrolResolution.Resolve(squad, CombatPatrolCatalog.Tiers[otherTier]);
            CombatPatrolResult launchOther = await service.LaunchAsync(new(p, h, otherTier, 100, 100, 100, claim.Snapshot.Revision, "launch-other"), default);
            Assert.True(launchOther.Succeeded, launchOther.Code);
            clock.Advance(CombatPatrolCatalog.Tiers[otherTier].Duration);
            CombatPatrolResult claimOther = await service.ClaimAsync(new(p, h, launchOther.Snapshot.ActiveEncounters[0].EncounterId, launchOther.Snapshot.Revision, "claim-other"), default);
            Assert.True(claimOther.Succeeded, claimOther.Code);
            Assert.False(claimOther.ClaimReceipt!.DailyFocusApplied);
            Assert.Equal(expectedOther.HoneyCredited, claimOther.ClaimReceipt.CreditedByResource["honey"]);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Preview_flags_the_featured_tier_as_daily_focus()
    {
        string root = Temp(); Guid p = Guid.NewGuid(); Guid h = Guid.NewGuid(); var clock = new MutableClock(new(2026, 7, 25, 9, 0, 0, TimeSpan.Zero));
        try
        {
            int featuredTier = DailyFocusCatalog.FeaturedCombatTier(clock.UtcNow);
            int otherTier = featuredTier == 1 ? 2 : 1;
            var repo = Repo(root, p, h, guardians: 0, wingrunners: 0, darters: 0);
            var service = new CombatPatrolService(repo, clock);

            CombatPatrolPreview featuredPreview = await service.PreviewAsync(new(p, h, featuredTier, 0, 0, 0), default);
            CombatPatrolPreview otherPreview = await service.PreviewAsync(new(p, h, otherTier, 0, 0, 0), default);

            Assert.True(featuredPreview.IsDailyFocus);
            Assert.False(otherPreview.IsDailyFocus);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Claim_applies_world_event_reward_bonus_only_to_the_localized_tier()
    {
        string root = Temp(); Guid p = Guid.NewGuid(); Guid h = Guid.NewGuid();
        // L'evenement mondial change toutes les 4h (contrairement a la Cible du jour, fixe pour la
        // journee) - on avance jusqu'a tomber sur une fenetre "menace en hausse" (3 des 6 creneaux)
        // pour tester le cas qui modifie reellement la recompense de combat. Localise (demande de
        // Jeff, 2026-08-01) : un seul palier precis parmi ceux de la famille visee est reellement
        // cible ce cycle - pas tous les paliers de cette famille en meme temps.
        DateTimeOffset t = new(2026, 7, 25, 9, 0, 0, TimeSpan.Zero);
        while (WorldEventCatalog.Active(t).Kind != WorldEventKind.ThreatSurge) t = t.AddHours(4);
        ActiveWorldEvent activeEvent = WorldEventCatalog.Active(t);
        List<int> eligibleTiers = CombatPatrolCatalog.Tiers.Values
            .Where(x => x.HazardFamily == activeEvent.TargetKey).Select(x => x.Tier).OrderBy(x => x).ToList();
        int matchingTier = WorldEventCatalog.FeaturedRegionTier(t, eligibleTiers)!.Value;
        // Prefere un palier NON cible de la MEME famille de danger quand il en existe un (au moins
        // une famille en a 2+) : c'est ca qui prouve la localisation, pas juste "autre famille".
        int mismatchedTier = eligibleTiers.FirstOrDefault(x => x != matchingTier, 0);
        if (mismatchedTier == 0) mismatchedTier = CombatPatrolCatalog.Tiers.Values.First(x => x.Tier != matchingTier).Tier;
        var clock = new MutableClock(t);
        try
        {
            var repo = Repo(root, p, h, guardians: 300, wingrunners: 300, darters: 300, guardPostLevel: 100, honey: 0, pollen: 0);
            var service = new CombatPatrolService(repo, clock);
            Dictionary<string, long> squad = new() { ["guardians"] = 100, ["wingrunners"] = 100, ["darters"] = 100 };

            CombatPatrolResolutionResult expectedMatching = CombatPatrolResolution.Resolve(squad, CombatPatrolCatalog.Tiers[matchingTier]);
            bool matchingIsDailyFocus = matchingTier == DailyFocusCatalog.FeaturedCombatTier(clock.UtcNow);
            long expectedHoney = matchingIsDailyFocus ? DailyFocusCatalog.ApplyRewardBonus(expectedMatching.HoneyCredited) : expectedMatching.HoneyCredited;
            long expectedPollen = matchingIsDailyFocus ? DailyFocusCatalog.ApplyRewardBonus(expectedMatching.PollenCredited) : expectedMatching.PollenCredited;
            expectedHoney = WorldEventCatalog.ApplyBonusBp(expectedHoney, activeEvent.BonusBp);
            expectedPollen = WorldEventCatalog.ApplyBonusBp(expectedPollen, activeEvent.BonusBp);

            CombatPatrolResult launch = await service.LaunchAsync(new(p, h, matchingTier, 100, 100, 100, 0, "launch-matching"), default);
            Assert.True(launch.Succeeded, launch.Code);
            clock.Advance(CombatPatrolCatalog.Tiers[matchingTier].Duration);
            CombatPatrolResult claim = await service.ClaimAsync(new(p, h, launch.Snapshot.ActiveEncounters[0].EncounterId, launch.Snapshot.Revision, "claim-matching"), default);
            Assert.True(claim.Succeeded, claim.Code);
            Assert.True(claim.ClaimReceipt!.WorldEventApplied);
            Assert.Equal(activeEvent.Key, claim.ClaimReceipt.WorldEventKey);
            Assert.Equal(expectedHoney, claim.ClaimReceipt.CreditedByResource["honey"]);
            Assert.Equal(expectedPollen, claim.ClaimReceipt.CreditedByResource["pollen"]);

            CombatPatrolResolutionResult expectedMismatched = CombatPatrolResolution.Resolve(squad, CombatPatrolCatalog.Tiers[mismatchedTier]);
            bool mismatchedIsDailyFocus = mismatchedTier == DailyFocusCatalog.FeaturedCombatTier(clock.UtcNow);
            long expectedOtherHoney = mismatchedIsDailyFocus ? DailyFocusCatalog.ApplyRewardBonus(expectedMismatched.HoneyCredited) : expectedMismatched.HoneyCredited;
            CombatPatrolResult launchOther = await service.LaunchAsync(new(p, h, mismatchedTier, 100, 100, 100, claim.Snapshot.Revision, "launch-mismatched"), default);
            Assert.True(launchOther.Succeeded, launchOther.Code);
            clock.Advance(CombatPatrolCatalog.Tiers[mismatchedTier].Duration);
            CombatPatrolResult claimOther = await service.ClaimAsync(new(p, h, launchOther.Snapshot.ActiveEncounters[0].EncounterId, launchOther.Snapshot.Revision, "claim-mismatched"), default);
            Assert.True(claimOther.Succeeded, claimOther.Code);
            Assert.False(claimOther.ClaimReceipt!.WorldEventApplied);
            Assert.Equal(expectedOtherHoney, claimOther.ClaimReceipt.CreditedByResource["honey"]);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static string Temp() => Path.Combine(Path.GetTempPath(), "bee-combat-patrol-" + Guid.NewGuid().ToString("N"));

    private static DurableJsonHiveStateRepository Repo(string root, Guid p, Guid h, long guardians, long wingrunners, long darters, int guardPostLevel = 0, long honey = 100_000, long pollen = 100_000, long reservedGuardians = 0, long reservedWingrunners = 0, long reservedDarters = 0)
    {
        var reservedCounts = new Dictionary<string, long> { ["guardians"] = reservedGuardians, ["wingrunners"] = reservedWingrunners, ["darters"] = reservedDarters };
        bool anyReserved = reservedGuardians + reservedWingrunners + reservedDarters > 0;
        var repo = new DurableJsonHiveStateRepository(root, (_, _) => new PlayerHiveState(
            p, h, HiveStateMigrator.CurrentModelVersion, 0,
            new Dictionary<string, ResourceBalance> { ["honey"] = new(honey, 1_000_000), ["pollen"] = new(pollen, 1_000_000) },
            new Dictionary<string, int> { ["guard_post"] = guardPostLevel }, [], new(),
            DoctrineRoster: new DoctrineRosterState(0, new() { ["guardians"] = guardians, ["wingrunners"] = wingrunners, ["darters"] = darters }, null, new()),
            // The stored Capacity here is just the migrator's internal consistency bound (sum(reserved) <= Capacity);
            // CombatSquadReservationService recomputes the *authoritative* capacity from BuildingLevels at read/commit
            // time regardless of this stored value (see CombatSquadReservationService.ComputeCapacity).
            SquadReservation: new SquadReservationState(0, 1000, reservedCounts, anyReserved ? "reservation" : null, new())));
        repo.ExecuteAtomicallyAsync(p, h, s => s).GetAwaiter().GetResult();
        return repo;
    }

    private sealed class MutableClock(DateTimeOffset now) : IServerClock { public DateTimeOffset UtcNow { get; private set; } = now; public void Advance(TimeSpan value) => UtcNow += value; }
}
