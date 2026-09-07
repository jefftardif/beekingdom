using System;
using System.Collections.Generic;
using System.Linq;
using BeeKingdom.Networking;
using NUnit.Framework;

namespace BeeKingdom.Playground.Editor
{
    // M055-CL - Cote Unity : projection de la progression du Palais Royal et representation
    // des prerequis dans le modele que la fenetre HiveMap consomme.
    //
    // Ces tests couvrent la LOGIQUE de la fenetre (niveau courant, niveau suivant, prerequis
    // satisfaits/manquants, raison de blocage exploitable, borne de configuration,
    // compatibilite d'un compte deja au-dessus des seuils). Le rendu IMGUI lui-meme n'est
    // pas testable en EditMode : il reste a valider a l'oeil par le CEO.
    public sealed class RoyalPalaceProgressionPresentationTests
    {
        private const string Palace = HiveBuildingUpgradeClient.RoyalPalaceBuildingKey;

        [Test]
        public void PalaceKeyIsTheRealInternalIdentifier()
        {
            // Le Palais Royal cote Unity ("ROYAL_PALACE") EST administration_core cote serveur.
            Assert.That(Palace, Is.EqualTo("administration_core"));
            Assert.That(
                BeeKingdom.Buildings.Interaction.BuildingMappingTable.ToLegacyKey(
                    BeeKingdom.Buildings.Interaction.BuildingTypes.RoyalPalace),
                Is.EqualTo(Palace));
        }

        [Test]
        public void CurrentAndNextLevelsAreProjectedFromTheServerSnapshot()
        {
            HiveBuildingUpgradeScreenModel model = HiveBuildingUpgradePresentation.Ready(
                Snapshot(palaceLevel: 3, nurseryLevel: 1), TimeSpan.Zero);

            Assert.That(model.RoyalPalace, Is.Not.Null);
            Assert.That(model.RoyalPalaceLevel(), Is.EqualTo(3));
            Assert.That(model.RoyalPalace.NextLevel, Is.EqualTo(4));
            Assert.That(model.RoyalPalace.IsMaxConfiguredLevel, Is.False);
            Assert.That(model.RoyalPalace.IsAlphaBalance, Is.True);
            // Le niveau du Palais Royal est aussi le niveau du batiment : une seule source.
            Assert.That(model.LevelFor(Palace), Is.EqualTo(model.RoyalPalaceLevel()));
        }

        [Test]
        public void SatisfiedAndMissingRequirementsAreDistinguishable()
        {
            HiveBuildingUpgradeScreenModel model = HiveBuildingUpgradePresentation.Ready(
                Snapshot(palaceLevel: 3, nurseryLevel: 1), TimeSpan.Zero);

            IReadOnlyList<HiveRoyalPalaceRequirementModel> requirements = model.RoyalPalace.NextLevelRequirements;
            Assert.That(requirements.Count, Is.EqualTo(2));
            Assert.That(requirements.Single(x => x.BuildingKey == "guard_post").IsSatisfied, Is.True);
            Assert.That(requirements.Single(x => x.BuildingKey == "nursery_cluster").IsSatisfied, Is.False);

            // La raison de blocage est exploitable : quel batiment, quel niveau vise.
            HiveRoyalPalaceRequirementModel missing = model.RoyalPalace.FirstMissingRequirement();
            Assert.That(missing, Is.Not.Null);
            Assert.That(missing.BuildingKey, Is.EqualTo("nursery_cluster"));
            Assert.That(missing.MinimumLevel, Is.EqualTo(3));
            Assert.That(missing.CurrentLevel, Is.EqualTo(1));
        }

        [Test]
        public void AnUnmetPrerequisiteBlocksTheClientFromEvenSendingTheRequest()
        {
            HiveBuildingUpgradeScreenModel blocked = HiveBuildingUpgradePresentation.Ready(
                Snapshot(palaceLevel: 3, nurseryLevel: 1), TimeSpan.Zero);
            Assert.That(blocked.PrerequisitesSatisfied(Palace), Is.False);
            Assert.That(blocked.CanStart(Palace), Is.False);

            // Une fois la condition remplie, l'action redevient disponible.
            HiveBuildingUpgradeScreenModel ready = HiveBuildingUpgradePresentation.Ready(
                Snapshot(palaceLevel: 3, nurseryLevel: 3), TimeSpan.Zero);
            Assert.That(ready.PrerequisitesSatisfied(Palace), Is.True);
            Assert.That(ready.CanStart(Palace), Is.True);
        }

        [Test]
        public void OtherBuildingsAreNeverGatedByTheRoyalPalaceProgression()
        {
            HiveBuildingUpgradeScreenModel model = HiveBuildingUpgradePresentation.Ready(
                Snapshot(palaceLevel: 3, nurseryLevel: 1), TimeSpan.Zero);
            Assert.That(model.PrerequisitesSatisfied("nursery_cluster"), Is.True);
            Assert.That(model.CanStart("nursery_cluster"), Is.True);
        }

        [Test]
        public void NextLevelUnlocksDistinguishEnforcedRulesFromAnnouncements()
        {
            HiveBuildingUpgradeScreenModel model = HiveBuildingUpgradePresentation.Ready(
                Snapshot(palaceLevel: 3, nurseryLevel: 1), TimeSpan.Zero);

            IReadOnlyList<HiveRoyalPalaceUnlockModel> unlocks = model.RoyalPalace.NextLevelUnlocks;
            Assert.That(unlocks.Single(x => x.Key == "champion_bees.rare").Enforced, Is.True);
            Assert.That(unlocks.Single(x => x.Key == "alliance.help").Enforced, Is.False);
        }

        [Test]
        public void MaxConfiguredLevelIsRepresentedWithoutANextLevel()
        {
            RemoteBuildingUpgradeSnapshot snapshot = Snapshot(palaceLevel: 10, nurseryLevel: 6);
            snapshot.RoyalPalace.NextLevel = null;
            snapshot.RoyalPalace.IsMaxConfiguredLevel = true;
            snapshot.RoyalPalace.NextLevelRequirements = new List<RemoteRoyalPalaceRequirement>();
            snapshot.RoyalPalace.NextLevelUnlocks = new List<RemoteRoyalPalaceUnlock>();
            snapshot.RoyalPalace.RequirementsSatisfied = false;
            snapshot.RoyalPalace.BlockedReasonCode = "game.royal_palace_max_level";
            snapshot.Offers = new List<RemoteBuildingUpgradeOffer>();

            HiveBuildingUpgradeScreenModel model = HiveBuildingUpgradePresentation.Ready(snapshot, TimeSpan.Zero);
            Assert.That(model.RoyalPalaceLevel(), Is.EqualTo(10));
            Assert.That(model.RoyalPalace.NextLevel, Is.Null);
            Assert.That(model.RoyalPalace.IsMaxConfiguredLevel, Is.True);
            Assert.That(model.RoyalPalace.FirstMissingRequirement(), Is.Null);
        }

        [Test]
        public void AnAccountAlreadyPastTheAlphaThresholdsStaysCoherent()
        {
            // Compte de test existant, batiments tres au-dessus des seuils Alpha : tout est
            // satisfait, rien n'est bloque, aucun niveau n'est reduit.
            HiveBuildingUpgradeScreenModel model = HiveBuildingUpgradePresentation.Ready(
                Snapshot(palaceLevel: 3, nurseryLevel: 30), TimeSpan.Zero);

            Assert.That(model.RoyalPalace.NextLevelRequirements.All(x => x.IsSatisfied), Is.True);
            Assert.That(model.RoyalPalace.FirstMissingRequirement(), Is.Null);
            Assert.That(model.PrerequisitesSatisfied(Palace), Is.True);
            Assert.That(model.RoyalPalaceLevel(), Is.EqualTo(3));
        }

        [Test]
        public void AbsentProgressionKeepsThePreM055Behaviour()
        {
            // Serveur sans progression configuree (ou snapshot en cache d'avant M055) :
            // aucun prerequis, l'ecran se comporte exactement comme avant.
            RemoteBuildingUpgradeSnapshot snapshot = Snapshot(palaceLevel: 3, nurseryLevel: 1);
            snapshot.RoyalPalace = null;

            HiveBuildingUpgradeScreenModel model = HiveBuildingUpgradePresentation.Ready(snapshot, TimeSpan.Zero);
            Assert.That(model.RoyalPalace, Is.Null);
            Assert.That(model.PrerequisitesSatisfied(Palace), Is.True);
            Assert.That(model.CanStart(Palace), Is.True);
            // Le niveau reste lisible via le niveau de batiment : pas de second compteur.
            Assert.That(model.RoyalPalaceLevel(), Is.EqualTo(3));
        }

        [Test]
        public void RealCostAndDurationComeFromTheExistingServerOffer()
        {
            HiveBuildingUpgradeScreenModel model = HiveBuildingUpgradePresentation.Ready(
                Snapshot(palaceLevel: 3, nurseryLevel: 3), TimeSpan.Zero);

            HiveBuildingUpgradeOfferModel offer = model.OfferFor(Palace);
            Assert.That(offer, Is.Not.Null);
            Assert.That(offer.FromLevel, Is.EqualTo(3));
            Assert.That(offer.ToLevel, Is.EqualTo(4));
            Assert.That(offer.Costs["honey"], Is.EqualTo(2232));
            Assert.That(offer.Duration, Is.EqualTo(TimeSpan.FromMinutes(5)));
        }

        // ---------- Fixture ----------

        private static RemoteBuildingUpgradeSnapshot Snapshot(int palaceLevel, int nurseryLevel)
        {
            return new RemoteBuildingUpgradeSnapshot
            {
                PlayerId = Guid.NewGuid(),
                HiveId = Guid.NewGuid(),
                ContractVersion = HiveBuildingUpgradeClient.ContractVersion,
                CatalogVersion = "royal-palace-alpha-v1",
                Revision = 4,
                ServerTimeUtc = new DateTimeOffset(2026, 9, 6, 12, 0, 0, TimeSpan.Zero),
                Balances = new Dictionary<string, RemoteBuildingUpgradeBalance>
                {
                    { "honey", new RemoteBuildingUpgradeBalance { Amount = 500000, Capacity = 1000000 } },
                    { "wax", new RemoteBuildingUpgradeBalance { Amount = 500000, Capacity = 1000000 } }
                },
                BuildingLevels = new Dictionary<string, int>
                {
                    { Palace, palaceLevel },
                    { "nursery_cluster", nurseryLevel },
                    { "guard_post", 4 }
                },
                Offers = new List<RemoteBuildingUpgradeOffer>
                {
                    new RemoteBuildingUpgradeOffer
                    {
                        BuildingKey = Palace,
                        FromLevel = palaceLevel,
                        ToLevel = palaceLevel + 1,
                        Duration = TimeSpan.FromMinutes(5),
                        Costs = new Dictionary<string, long> { { "honey", 2232 }, { "wax", 626 } }
                    },
                    new RemoteBuildingUpgradeOffer
                    {
                        BuildingKey = "nursery_cluster",
                        FromLevel = nurseryLevel,
                        ToLevel = nurseryLevel + 1,
                        Duration = TimeSpan.FromMinutes(3),
                        Costs = new Dictionary<string, long> { { "honey", 972 } }
                    }
                },
                ActiveOperation = null,
                RoyalPalace = new RemoteRoyalPalaceProgression
                {
                    BuildingKey = Palace,
                    DefinitionVersion = "royal-palace-alpha-v1",
                    IsAlphaBalance = true,
                    CurrentLevel = palaceLevel,
                    NextLevel = palaceLevel + 1,
                    MaxConfiguredLevel = 10,
                    IsMaxConfiguredLevel = false,
                    RequirementsSatisfied = nurseryLevel >= 3,
                    BlockedReasonCode = nurseryLevel >= 3 ? string.Empty : HiveBuildingUpgradeClient.PrerequisitesNotMetCode,
                    BlockingBuildingKey = nurseryLevel >= 3 ? string.Empty : "nursery_cluster",
                    BlockingBuildingMinimumLevel = nurseryLevel >= 3 ? 0 : 3,
                    NextLevelDescription = "La colonie tient son perimetre.",
                    NextLevelRequirements = new List<RemoteRoyalPalaceRequirement>
                    {
                        new RemoteRoyalPalaceRequirement { BuildingKey = "guard_post", MinimumLevel = 2, CurrentLevel = 4, IsSatisfied = true },
                        new RemoteRoyalPalaceRequirement { BuildingKey = "nursery_cluster", MinimumLevel = 3, CurrentLevel = nurseryLevel, IsSatisfied = nurseryLevel >= 3 }
                    },
                    NextLevelUnlocks = new List<RemoteRoyalPalaceUnlock>
                    {
                        new RemoteRoyalPalaceUnlock { Key = "champion_bees.rare", Description = "Abeilles championnes rares", Enforced = true },
                        new RemoteRoyalPalaceUnlock { Key = "alliance.help", Description = "Aide d'alliance", Enforced = false }
                    },
                    UnlockedSoFar = new List<RemoteRoyalPalaceUnlock>
                    {
                        new RemoteRoyalPalaceUnlock { Key = "hive.building_upgrades", Description = "Ameliorations", Enforced = false }
                    }
                }
            };
        }
    }
}
