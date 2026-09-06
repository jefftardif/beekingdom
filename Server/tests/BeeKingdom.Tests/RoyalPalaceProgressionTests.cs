using BeeKingdom.HiveOperations;

namespace BeeKingdom.Tests;

// M055-CL - Progression du Palais Royal (administration_core).
// Couvre : resolution du niveau, niveau suivant, bornes de configuration, prerequis
// (acceptation, refus, raison exploitable, impossibilite de contourner depuis le client),
// ressources (cout reel, refus, pas de double consommation au retry), persistance du
// niveau, et compatibilite des comptes existants deja au-dessus des seuils Alpha.
public sealed class RoyalPalaceProgressionTests
{
    private const string Palace = RoyalPalaceProgressionKeys.RoyalPalaceBuildingKey;

    // ---------- Progression : niveau courant / suivant / bornes ----------

    [Test]
    public void CurrentLevelUsesTheBuildingLevelAndDefaultsToOne()
    {
        Assert.That(RoyalPalaceProgression.CurrentLevel(new Dictionary<string, int> { { Palace, 4 } }), Is.EqualTo(4));
        // Un compte neuf n'a aucune entree pour le palais : niveau 1 implicite,
        // exactement la meme convention que BuildingUpgradeService.
        Assert.That(RoyalPalaceProgression.CurrentLevel(new Dictionary<string, int>()), Is.EqualTo(1));
        Assert.That(RoyalPalaceProgression.CurrentLevel(null), Is.EqualTo(1));
    }

    [Test]
    public void NextLevelIsResolvedWithItsRequirementsAndUnlocks()
    {
        RoyalPalaceProgressionView view = RoyalPalaceProgression.Evaluate(
            AlphaLikeOptions(), new Dictionary<string, int> { { Palace, 2 }, { "nursery_cluster", 1 } });

        Assert.That(view.CurrentLevel, Is.EqualTo(2));
        Assert.That(view.NextLevel, Is.EqualTo(3));
        Assert.That(view.MaxConfiguredLevel, Is.EqualTo(4));
        Assert.That(view.IsMaxConfiguredLevel, Is.False);
        Assert.That(view.NextLevelRequirements.Count, Is.EqualTo(1));
        Assert.That(view.NextLevelRequirements[0].BuildingKey, Is.EqualTo("nursery_cluster"));
        Assert.That(view.NextLevelRequirements[0].MinimumLevel, Is.EqualTo(2));
        Assert.That(view.NextLevelRequirements[0].CurrentLevel, Is.EqualTo(1));
        Assert.That(view.NextLevelRequirements[0].IsSatisfied, Is.False);
        Assert.That(view.NextLevelUnlocks.Select(x => x.Key), Does.Contain("champion_bees.rare"));
        Assert.That(view.BlockingBuildingKey, Is.EqualTo("nursery_cluster"));
        Assert.That(view.BlockingBuildingMinimumLevel, Is.EqualTo(2));
        Assert.That(view.RequirementsSatisfied, Is.False);
        Assert.That(view.BlockedReasonCode, Is.EqualTo(RoyalPalaceProgression.PrerequisitesNotMetCode));
        Assert.That(view.IsAlphaBalance, Is.True);
    }

    [Test]
    public void MaxConfiguredLevelIsReportedWithoutAnyNextLevel()
    {
        RoyalPalaceProgressionView view = RoyalPalaceProgression.Evaluate(
            AlphaLikeOptions(), new Dictionary<string, int> { { Palace, 4 } });

        Assert.That(view.CurrentLevel, Is.EqualTo(4));
        Assert.That(view.NextLevel, Is.Null);
        Assert.That(view.IsMaxConfiguredLevel, Is.True);
        Assert.That(view.BlockedReasonCode, Is.EqualTo(RoyalPalaceProgression.MaxLevelCode));
        Assert.That(view.NextLevelRequirements, Is.Empty);
        // Le cumul des deblocages atteints reste lisible au niveau maximum.
        Assert.That(view.UnlockedSoFar.Select(x => x.Key), Does.Contain("champion_bees.rare"));
    }

    [Test]
    public void DisabledProgressionStaysFailOpen()
    {
        var disabled = new RoyalPalaceProgressionOptions { Enabled = false };
        Assert.That(RoyalPalaceProgression.TryValidateUpgrade(disabled, new Dictionary<string, int>(), 1, out string code, out _, out _), Is.True);
        Assert.That(code, Is.Empty);
        Assert.That(RoyalPalaceProgression.TryValidateUpgrade(null, new Dictionary<string, int>(), 1, out _, out _, out _), Is.True);
        // Aucune vue de progression n'est fabriquee quand la fonctionnalite est eteinte.
        Assert.That(RoyalPalaceProgression.Evaluate(disabled, new Dictionary<string, int> { { Palace, 3 } }).NextLevel, Is.Null);
    }

    [Test]
    public void InvalidDefinitionsAreRejectedAtConfigurationTime()
    {
        // Le palais ne peut pas etre son propre prerequis (impasse permanente).
        var selfReferencing = new RoyalPalaceProgressionOptions
        {
            Enabled = true,
            DefinitionVersion = "test-v1",
            Levels = [new(1, [], [], "base"), new(2, [new(Palace, 2)], [], "boucle")]
        };
        Assert.Throws<InvalidDataException>(() => selfReferencing.Validate());

        // Une table trouee rendrait "niveau suivant" indefinissable.
        var nonContiguous = new RoyalPalaceProgressionOptions
        {
            Enabled = true,
            DefinitionVersion = "test-v1",
            Levels = [new(1, [], [], "base"), new(3, [], [], "trou")]
        };
        Assert.Throws<InvalidDataException>(() => nonContiguous.Validate());
    }

    // ---------- Prerequis imposes par le serveur ----------

    [Test]
    public async Task StartIsRefusedWithAnActionableReasonWhenARequiredBuildingIsTooLow()
    {
        var clock = new Clock(DateTimeOffset.UtcNow);
        var repo = new MemoryRepo();
        (Guid player, Guid hive) = Seed(repo, palaceLevel: 2, nurseryLevel: 1);
        var service = new BuildingUpgradeService(repo, clock, UpgradeOptions(), false, AlphaLikeOptions());

        BuildingUpgradeCommandResult result = await service.StartAsync(player, hive, Palace, new(0, "start-blocked"));

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Code, Is.EqualTo(RoyalPalaceProgression.PrerequisitesNotMetCode));
        // La raison est exploitable par l'UI : quel batiment, quel niveau.
        Assert.That(result.Snapshot.RoyalPalace, Is.Not.Null);
        Assert.That(result.Snapshot.RoyalPalace!.BlockingBuildingKey, Is.EqualTo("nursery_cluster"));
        Assert.That(result.Snapshot.RoyalPalace.BlockingBuildingMinimumLevel, Is.EqualTo(2));

        // Aucune mutation : ni debit, ni operation, ni revision.
        PlayerHiveState? state = await repo.ReadAsync(player, hive);
        Assert.That(state!.Revision, Is.EqualTo(0));
        Assert.That(state.Resources["honey"].Amount, Is.EqualTo(100000));
        Assert.That(state.Operations, Is.Empty);
        Assert.That(state.BuildingLevels[Palace], Is.EqualTo(2));
    }

    [Test]
    public async Task StartIsAcceptedOnceEveryRequirementIsSatisfied()
    {
        var clock = new Clock(DateTimeOffset.UtcNow);
        var repo = new MemoryRepo();
        (Guid player, Guid hive) = Seed(repo, palaceLevel: 2, nurseryLevel: 2);
        var service = new BuildingUpgradeService(repo, clock, UpgradeOptions(), false, AlphaLikeOptions());

        BuildingUpgradeCommandResult result = await service.StartAsync(player, hive, Palace, new(0, "start-ok"));

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.Response!.Receipt.FromLevel, Is.EqualTo(2));
        Assert.That(result.Response.Receipt.ToLevel, Is.EqualTo(3));
        // Cout reel du catalogue applique une seule fois.
        PlayerHiveState? state = await repo.ReadAsync(player, hive);
        Assert.That(state!.Resources["honey"].Amount, Is.EqualTo(100000 - 2088));
    }

    [Test]
    public async Task AClientCannotBypassPrerequisitesByCallingTheCommandDirectly()
    {
        // Le client n'a AUCUN autre chemin que StartAsync (les endpoints REST y appellent
        // directement) : refuser ici, c'est refuser toute tentative de contournement, quelles
        // que soient les regles que le client prive choisit d'afficher ou d'ignorer.
        var clock = new Clock(DateTimeOffset.UtcNow);
        var repo = new MemoryRepo();
        (Guid player, Guid hive) = Seed(repo, palaceLevel: 2, nurseryLevel: 1);
        var service = new BuildingUpgradeService(repo, clock, UpgradeOptions(), false, AlphaLikeOptions());

        foreach (string forgedKey in new[] { "bypass-1", "bypass-2", "bypass-3" })
            Assert.That((await service.StartAsync(player, hive, Palace, new(0, forgedKey))).Code,
                Is.EqualTo(RoyalPalaceProgression.PrerequisitesNotMetCode));

        Assert.That((await repo.ReadAsync(player, hive))!.Revision, Is.EqualTo(0));
    }

    [Test]
    public async Task OtherBuildingsAreNotGatedByTheRoyalPalaceProgression()
    {
        // M055 n'ajoute AUCUN prerequis aux autres batiments : un compte existant garde
        // exactement le comportement d'avant sur tout le reste du catalogue.
        var clock = new Clock(DateTimeOffset.UtcNow);
        var repo = new MemoryRepo();
        (Guid player, Guid hive) = Seed(repo, palaceLevel: 1, nurseryLevel: 1);
        var service = new BuildingUpgradeService(repo, clock, UpgradeOptions(), false, AlphaLikeOptions());

        Assert.That((await service.StartAsync(player, hive, "nursery_cluster", new(0, "nursery"))).Succeeded, Is.True);
    }

    // ---------- Ressources ----------

    [Test]
    public async Task InsufficientResourcesAreRefusedAndRetryNeverDoubleCharges()
    {
        var clock = new Clock(DateTimeOffset.UtcNow);
        var repo = new MemoryRepo();
        (Guid player, Guid hive) = Seed(repo, palaceLevel: 2, nurseryLevel: 2, honey: 10);
        var service = new BuildingUpgradeService(repo, clock, UpgradeOptions(), false, AlphaLikeOptions());

        Assert.That((await service.StartAsync(player, hive, Palace, new(0, "poor"))).Code, Is.EqualTo("game.insufficient_resources"));
        Assert.That((await repo.ReadAsync(player, hive))!.Resources["honey"].Amount, Is.EqualTo(10));

        // Meme cle d'idempotence rejouee apres un depot de ressources : un seul debit.
        (Guid richPlayer, Guid richHive) = Seed(repo, palaceLevel: 2, nurseryLevel: 2);
        BuildingUpgradeCommandResult first = await service.StartAsync(richPlayer, richHive, Palace, new(0, "once"));
        Assert.That(first.Succeeded, Is.True);
        BuildingUpgradeCommandResult replay = await service.StartAsync(richPlayer, richHive, Palace, new(0, "once"));
        Assert.That(replay.Code, Is.EqualTo("game.building_upgrade_started"));
        Assert.That(replay.Response!.Receipt, Is.EqualTo(first.Response!.Receipt));
        Assert.That((await repo.ReadAsync(richPlayer, richHive))!.Resources["honey"].Amount, Is.EqualTo(100000 - 2088));
    }

    // ---------- Persistance et compatibilite des comptes existants ----------

    [Test]
    public async Task CompletingTheUpgradePersistsTheNewRoyalPalaceLevel()
    {
        var clock = new Clock(DateTimeOffset.UtcNow);
        var repo = new MemoryRepo();
        (Guid player, Guid hive) = Seed(repo, palaceLevel: 2, nurseryLevel: 2);
        var service = new BuildingUpgradeService(repo, clock, UpgradeOptions(), false, AlphaLikeOptions());

        BuildingUpgradeCommandResult start = await service.StartAsync(player, hive, Palace, new(0, "start"));
        clock.Advance(TimeSpan.FromHours(2));
        BuildingUpgradeCommandResult done = await service.CompleteAsync(player, hive, start.Response!.Receipt.OperationId, new(1, "complete"));
        Assert.That(done.Succeeded, Is.True);

        // Le niveau est ecrit dans BuildingLevels - la seule source de verite. Une relecture
        // ulterieure (equivalent d'une reconnexion) le retrouve identique.
        Assert.That((await repo.ReadAsync(player, hive))!.BuildingLevels[Palace], Is.EqualTo(3));
        BuildingUpgradeReadSnapshot reread = await service.ReadAsync(player, hive);
        Assert.That(reread.BuildingLevels[Palace], Is.EqualTo(3));
        Assert.That(reread.RoyalPalace!.CurrentLevel, Is.EqualTo(3));
    }

    [Test]
    public async Task AnExistingAccountFarAboveTheAlphaThresholdsStaysCoherent()
    {
        // Compte de test reel deja tres avance : ses batiments depassent largement les
        // nouveaux seuils Alpha. Les prerequis etant des MINIMUMS, il passe trivialement -
        // il n'est ni bloque, ni retrograde, ni reset.
        var clock = new Clock(DateTimeOffset.UtcNow);
        var repo = new MemoryRepo();
        (Guid player, Guid hive) = Seed(repo, palaceLevel: 2, nurseryLevel: 25);
        var service = new BuildingUpgradeService(repo, clock, UpgradeOptions(), false, AlphaLikeOptions());

        RoyalPalaceProgressionView view = RoyalPalaceProgression.Evaluate(AlphaLikeOptions(), (await repo.ReadAsync(player, hive))!.BuildingLevels);
        Assert.That(view.RequirementsSatisfied, Is.True);
        Assert.That(view.BlockedReasonCode, Is.Empty);
        Assert.That((await service.StartAsync(player, hive, Palace, new(0, "veteran"))).Succeeded, Is.True);
    }

    [Test]
    public void AnAccountAlreadyAboveTheConfiguredMaximumIsNeverDowngraded()
    {
        // Le palais d'un compte existant peut deja depasser la table Alpha : on constate
        // simplement qu'il n'y a plus de palier devant lui, sans jamais reduire son niveau
        // ni le declarer invalide.
        RoyalPalaceProgressionView view = RoyalPalaceProgression.Evaluate(
            AlphaLikeOptions(), new Dictionary<string, int> { { Palace, 12 } });

        Assert.That(view.CurrentLevel, Is.EqualTo(12));
        Assert.That(view.NextLevel, Is.Null);
        Assert.That(view.IsMaxConfiguredLevel, Is.True);
        Assert.That(view.MaxConfiguredLevel, Is.EqualTo(4));
        // Au-dela de la table, aucun prerequis ne peut bloquer : c'est le catalogue de
        // couts qui borne reellement la progression.
        Assert.That(RoyalPalaceProgression.TryValidateUpgrade(AlphaLikeOptions(), new Dictionary<string, int> { { Palace, 12 } }, 12, out _, out _, out _), Is.True);
    }

    [Test]
    public void UnlockAndLevelQueriesAnswerTheFtueStyleQuestions()
    {
        var levels = new Dictionary<string, int> { { Palace, 3 } };
        Assert.That(RoyalPalaceProgression.IsAtLeast(levels, 3), Is.True);
        Assert.That(RoyalPalaceProgression.IsAtLeast(levels, 4), Is.False);
        Assert.That(RoyalPalaceProgression.IsUnlocked(AlphaLikeOptions(), levels, "champion_bees.rare"), Is.True);
        Assert.That(RoyalPalaceProgression.IsUnlocked(AlphaLikeOptions(), levels, "unknown.key"), Is.False);
    }

    // ---------- Fixtures ----------

    // Table reduite mais de MEME forme que la configuration Alpha reelle
    // (appsettings RoyalPalaceProgression) : niveau 3 exige nursery_cluster 2.
    private static RoyalPalaceProgressionOptions AlphaLikeOptions() => new()
    {
        Enabled = true,
        DefinitionVersion = "royal-palace-alpha-test-v1",
        IsAlphaBalance = true,
        Levels =
        [
            new(1, [], [], "Fondation"),
            new(2, [], [new RoyalPalaceUnlock("hive.building_upgrades", "Ameliorations", false)], "Cour royale"),
            new(3, [new RoyalPalaceLevelRequirement("nursery_cluster", 2)], [new RoyalPalaceUnlock("champion_bees.rare", "Championnes rares", true)], "Lignees remarquables"),
            new(4, [new RoyalPalaceLevelRequirement("guard_post", 2)], [], "Perimetre tenu")
        ]
    };

    private static BuildingUpgradeOptions UpgradeOptions() => new()
    {
        Enabled = true,
        CatalogVersion = "royal-palace-test-v1",
        Catalog =
        [
            new(Palace, 2, 3, TimeSpan.FromHours(1), new Dictionary<string, long> { { "honey", 2088 }, { "wax", 564 } }),
            new(Palace, 3, 4, TimeSpan.FromHours(1), new Dictionary<string, long> { { "honey", 2232 }, { "wax", 626 } }),
            new("nursery_cluster", 1, 2, TimeSpan.FromHours(1), new Dictionary<string, long> { { "honey", 972 } })
        ]
    };

    private static (Guid, Guid) Seed(MemoryRepo repo, int palaceLevel, int nurseryLevel, long honey = 100000)
    {
        var player = Guid.NewGuid();
        var hive = Guid.NewGuid();
        repo.State = new(player, hive, HiveStateMigrator.CurrentModelVersion, 0,
            new Dictionary<string, ResourceBalance> { { "honey", new(honey, 1000000) }, { "wax", new(100000, 1000000) }, { "pollen", new(100000, 1000000) } },
            new Dictionary<string, int> { { Palace, palaceLevel }, { "nursery_cluster", nurseryLevel } },
            new(), new());
        return (player, hive);
    }

    private sealed class Clock(DateTimeOffset now) : IServerClock
    {
        public DateTimeOffset UtcNow { get; private set; } = now;
        public void Advance(TimeSpan delta) => UtcNow += delta;
    }

    private sealed class MemoryRepo : IHiveStateRepository
    {
        public PlayerHiveState? State;
        public Task<PlayerHiveState?> ReadAsync(Guid p, Guid h, CancellationToken ct = default)
            => Task.FromResult(State?.PlayerId == p && State.HiveId == h ? State : null);
        public Task<PlayerHiveState> ExecuteAtomicallyAsync(Guid p, Guid h, Func<PlayerHiveState, PlayerHiveState> mutation, CancellationToken ct = default)
        {
            State ??= new(p, h, HiveStateMigrator.CurrentModelVersion, 0, new(), new(), new(), new());
            State = mutation(State);
            return Task.FromResult(State);
        }
        public Task<IReadOnlyList<Guid>> ListHiveIdsAsync(Guid p, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Guid>>(State != null && State.PlayerId == p ? new List<Guid> { State.HiveId } : new List<Guid>());
        public Task<IReadOnlyList<PlayerHiveState>> ListRecentlyActiveAsync(int limit, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<PlayerHiveState>>(State != null ? new List<PlayerHiveState> { State } : new List<PlayerHiveState>());
    }
}
