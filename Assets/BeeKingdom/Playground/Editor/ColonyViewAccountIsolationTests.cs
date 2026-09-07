using System;
using System.Collections.Generic;
using BeeKingdom.Networking;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    // M059-CL - Preuves du defaut rapporte par le premier testeur externe (Alex) : la VUE
    // COLONIE affichait "Niveau 22/24/25/27" sur une ruche neuve.
    //
    // Diagnostic couvert par ces tests :
    //  1. ces nombres ne venaient d'AUCUN joueur - ce sont les valeurs de repli codees en dur
    //     du bac a sable de demonstration, que la VUE COLONIE lisait directement au lieu de
    //     l'instantane serveur ;
    //  2. le cache d'apercu local etait partitionne par APPAREIL et non par COMPTE, donc deux
    //     comptes utilises sur la meme machine se le partageaient.
    public sealed class ColonyViewAccountIsolationTests
    {
        private const string AccountA = "aaaaaaaa-0000-0000-0000-000000000001";
        private const string AccountB = "bbbbbbbb-0000-0000-0000-000000000002";
        private const string ProfileId = "device-profile";

        private sealed class MemoryStore : ILocalPreviewHiveProgressStore
        {
            private string json;
            public MemoryStore(string initial = "") { json = initial ?? string.Empty; }
            public string Read() => json;
            public void Write(string value) { json = value ?? string.Empty; }
            public void Delete() { json = string.Empty; }
        }

        private sealed class FakePanelController : IHiveBuildingUpgradePanelController
        {
            public FakePanelController(HiveBuildingUpgradeScreenModel model, TimeSpan elapsed)
            { Model = model; Elapsed = elapsed; }
            public HiveBuildingUpgradeScreenModel Model { get; }
            public bool IsConfigured => true;
            public bool IsBusy => false;
            public TimeSpan Elapsed { get; }
            public void Refresh() { }
            public void Start(string buildingKey) { }
            public void Complete() { }
        }

        // ---------------------------------------------------------------- Part 1 : autorite

        [Test]
        public void ColonyViewShowsServerLevelNotTheHardCodedDemoLevel()
        {
            try
            {
                HiveViewProductUiPresenter.UseBuildingUpgradeControllerForProof(new FakePanelController(
                    HiveBuildingUpgradePresentation.Ready(SnapshotWithLevels(1), TimeSpan.Zero),
                    TimeSpan.Zero));

                string text = HiveViewProductUiPresenter.ColonyOverviewBuildingLevelTextForProof("honey_storage");

                Assert.That(text, Does.Contain("1"));
                // Les quatre valeurs inventees observees par le testeur ne doivent plus
                // pouvoir apparaitre des qu'une session officielle est branchee.
                Assert.That(text, Does.Not.Contain("25"));
                Assert.That(text, Does.Not.Contain("22"));
                Assert.That(text, Does.Not.Contain("24"));
                Assert.That(text, Does.Not.Contain("27"));
            }
            finally
            {
                HiveViewProductUiPresenter.UseBuildingUpgradeControllerForProof(null);
            }
        }

        [Test]
        public void ColonyViewShowsWaitingValueRatherThanAnInventedLevelWhenServerLevelMissing()
        {
            try
            {
                // Instantane sans niveau pour ce batiment : le serveur fait autorite mais n'a
                // rien livre. Il faut se taire, jamais retomber sur le bac a sable.
                HiveViewProductUiPresenter.UseBuildingUpgradeControllerForProof(new FakePanelController(
                    HiveBuildingUpgradePresentation.Ready(SnapshotWithLevels(1), TimeSpan.Zero),
                    TimeSpan.Zero));

                string text = HiveViewProductUiPresenter.ColonyOverviewBuildingLevelTextForProof("archives_honeyfall");

                Assert.That(text, Does.Contain("—"));
                Assert.That(text, Does.Not.Contain("22"));
            }
            finally
            {
                HiveViewProductUiPresenter.UseBuildingUpgradeControllerForProof(null);
            }
        }

        // ------------------------------------------------- Part 1 : isolation entre comptes

        [Test]
        public void ProgressWrittenByOneAccountIsNeverRestoredForAnother()
        {
            var store = new MemoryStore();

            LocalPreviewHiveProgress ownedByA = LocalPreviewHiveProgressCodec.CreateDefault(ProfileId);
            ownedByA.accountId = AccountA;
            LocalPreviewHiveProgressCodec.MergeBuildingLevel(ownedByA, "honey_storage", 17);
            LocalPreviewHiveProgressCodec.Write(store, ownedByA);

            LocalPreviewHiveProgressReadResult asA =
                LocalPreviewHiveProgressCodec.Read(store, ProfileId, AccountA);
            Assert.That(asA.Status, Is.EqualTo(LocalPreviewHiveProgressReadStatus.Restored));
            Assert.That(LocalPreviewHiveProgressCodec.TryGetBuildingLevel(asA.Progress, "honey_storage", out int levelA), Is.True);
            Assert.That(levelA, Is.EqualTo(17));

            LocalPreviewHiveProgressReadResult asB =
                LocalPreviewHiveProgressCodec.Read(store, ProfileId, AccountB);
            Assert.That(asB.Status, Is.EqualTo(LocalPreviewHiveProgressReadStatus.AccountMismatch));
            Assert.That(LocalPreviewHiveProgressCodec.TryGetBuildingLevel(asB.Progress, "honey_storage", out _), Is.False,
                "Le compte B ne doit jamais heriter du moindre niveau du compte A.");
            Assert.That(asB.Progress.buildings, Is.Empty);
        }

        [Test]
        public void LegacyProgressWithoutAccountIsAdoptedRatherThanDiscarded()
        {
            // Blob ecrit AVANT M059 : aucun accountId. Il appartient de fait au proprietaire
            // de la machine et ne doit pas etre efface par l'introduction de la partition.
            LocalPreviewHiveProgress legacy = LocalPreviewHiveProgressCodec.CreateDefault(ProfileId);
            LocalPreviewHiveProgressCodec.MergeBuildingLevel(legacy, "guard_post", 9);
            legacy.accountId = string.Empty;
            var store = new MemoryStore(JsonUtility.ToJson(legacy));

            LocalPreviewHiveProgressReadResult adopted =
                LocalPreviewHiveProgressCodec.Read(store, ProfileId, AccountA);

            Assert.That(adopted.Progress.accountId, Is.EqualTo(AccountA));
            Assert.That(LocalPreviewHiveProgressCodec.TryGetBuildingLevel(adopted.Progress, "guard_post", out int level), Is.True);
            Assert.That(level, Is.EqualTo(9));

            // Une fois adopte, il est refuse a tout autre compte.
            Assert.That(LocalPreviewHiveProgressCodec.Read(store, ProfileId, AccountB).Status,
                Is.EqualTo(LocalPreviewHiveProgressReadStatus.AccountMismatch));
        }

        [Test]
        public void NoAccountPartitionKeepsPreM059Behaviour()
        {
            LocalPreviewHiveProgress owned = LocalPreviewHiveProgressCodec.CreateDefault(ProfileId);
            owned.accountId = AccountA;
            LocalPreviewHiveProgressCodec.MergeBuildingLevel(owned, "wax_workshop", 4);
            var store = new MemoryStore(JsonUtility.ToJson(owned));

            // Aucune session officielle : la partition est vide, la lecture reste permissive.
            LocalPreviewHiveProgressReadResult result = LocalPreviewHiveProgressCodec.Read(store, ProfileId);

            Assert.That(result.Status, Is.Not.EqualTo(LocalPreviewHiveProgressReadStatus.AccountMismatch));
            Assert.That(LocalPreviewHiveProgressCodec.TryGetBuildingLevel(result.Progress, "wax_workshop", out int level), Is.True);
            Assert.That(level, Is.EqualTo(4));
        }

        [Test]
        public void SwitchingAccountPurgesInMemoryLevelsInsteadOfLeakingThem()
        {
            try
            {
                // Aucun controleur officiel ici : on observe volontairement la couche d'apercu
                // local, celle qui portait la fuite entre comptes sur une meme machine.
                HiveViewProductUiPresenter.UseBuildingUpgradeControllerForProof(null);
                var sharedDeviceStore = new MemoryStore();

                HiveViewProductUiPresenter.SetLocalPreviewAccountPartitionForRuntime(AccountA);
                HiveViewProductUiPresenter.UseLocalPreviewHiveProgressStoreForProof(sharedDeviceStore);
                HiveViewProductUiPresenter.SetLocalPreviewAccountPartitionForRuntime(AccountA);
                HiveViewProductUiPresenter.PersistLocalPreviewBuildingLevelForProof("honey_storage", 31);
                // Recharge depuis le magasin, comme au demarrage de l'application : c'est
                // l'etat REELLEMENT persiste du compte A que l'on veut observer ensuite.
                HiveViewProductUiPresenter.SimulateLocalPreviewHiveProgressRestartForProof();
                // Lire le texte peuple le cache memoire des niveaux : c'est exactement ce que
                // fait la VUE COLONIE hors ligne.
                string textForA = HiveViewProductUiPresenter.ColonyOverviewBuildingLevelTextForProof("honey_storage");
                Assert.That(textForA, Does.Contain("31"));
                Assert.That(HiveViewProductUiPresenter.LocalPreviewBuildingLevelCountForProof, Is.GreaterThan(0));

                HiveViewProductUiPresenter.SetLocalPreviewAccountPartitionForRuntime(AccountB);

                Assert.That(HiveViewProductUiPresenter.LocalPreviewAccountPartitionForProof, Is.EqualTo(AccountB));
                Assert.That(HiveViewProductUiPresenter.LocalPreviewBuildingLevelCountForProof, Is.Zero,
                    "Les niveaux du compte precedent ne doivent pas survivre en memoire au changement de compte.");

                // Et la relecture, sur le MEME magasin d'appareil, ne doit pas reconstituer
                // la progression du compte A.
                string textForB = HiveViewProductUiPresenter.ColonyOverviewBuildingLevelTextForProof("honey_storage");
                Assert.That(textForB, Does.Not.Contain("31"),
                    "Le compte B ne doit jamais recuperer le niveau ecrit par le compte A sur la meme machine.");
            }
            finally
            {
                HiveViewProductUiPresenter.SetLocalPreviewAccountPartitionForRuntime(string.Empty);
                HiveViewProductUiPresenter.UseLocalPreviewHiveProgressStoreForProof(null);
            }
        }

        // --------------------------------------------- Parts 2 et 3 : etat reel de chantier

        [Test]
        public void UpgradeProgressWindowOpensOnlyOnTheBuildingActuallyUnderConstruction()
        {
            try
            {
                HiveViewProductUiPresenter.UseBuildingUpgradeControllerForProof(new FakePanelController(
                    HiveBuildingUpgradePresentation.Ready(
                        SnapshotWithOperation(HiveBuildingUpgradeClient.RunningStatus),
                        TimeSpan.Zero),
                    TimeSpan.FromMinutes(1)));

                Assert.That(HiveViewProductUiPresenter.TryOpenUpgradeProgressOverlayForExternalHost("guard_post"), Is.False,
                    "Un batiment sans chantier doit continuer d'ouvrir sa fenetre ordinaire.");
                Assert.That(HiveViewProductUiPresenter.UpgradeProgressOverlayOpenForExternalHost, Is.False);

                Assert.That(HiveViewProductUiPresenter.TryOpenUpgradeProgressOverlayForExternalHost("wax_workshop"), Is.True);
                Assert.That(HiveViewProductUiPresenter.UpgradeProgressOverlayOpenForExternalHost, Is.True);
            }
            finally
            {
                HiveViewProductUiPresenter.CloseUpgradeProgressOverlayForExternalHost();
                HiveViewProductUiPresenter.UseBuildingUpgradeControllerForProof(null);
            }
        }

        [Test]
        public void AwaitingCompletionNeverOpensTheProgressWindow()
        {
            try
            {
                HiveViewProductUiPresenter.UseBuildingUpgradeControllerForProof(new FakePanelController(
                    HiveBuildingUpgradePresentation.Ready(
                        SnapshotWithOperation(HiveBuildingUpgradeClient.AwaitingCompletionStatus),
                        TimeSpan.Zero),
                    TimeSpan.FromMinutes(11)));

                Assert.That(HiveViewProductUiPresenter.TryOpenUpgradeProgressOverlayForExternalHost("wax_workshop"), Is.False,
                    "La validation au clic sur le batiment reste prioritaire et ne doit pas etre remplacee.");
                Assert.That(HiveViewProductUiPresenter.ReadyToCompleteOfficialUpgradeHotspotIdForExternalHost(),
                    Is.EqualTo("wax_workshop"));
            }
            finally
            {
                HiveViewProductUiPresenter.CloseUpgradeProgressOverlayForExternalHost();
                HiveViewProductUiPresenter.UseBuildingUpgradeControllerForProof(null);
            }
        }

        [Test]
        public void WorldProgressBarFollowsRealServerTimingAndDisappearsOutsideRunning()
        {
            try
            {
                HiveViewProductUiPresenter.UseBuildingUpgradeControllerForProof(new FakePanelController(
                    HiveBuildingUpgradePresentation.Ready(
                        SnapshotWithOperation(HiveBuildingUpgradeClient.RunningStatus),
                        TimeSpan.Zero),
                    TimeSpan.FromMinutes(5)));
                // 5 minutes ecoulees sur une operation de 10 minutes : la progression doit
                // venir de l'operation serveur, pas d'un minuteur client.
                Assert.That(HiveViewProductUiPresenter.OfficialUpgradeProgress01ForExternalHost(),
                    Is.EqualTo(0.5f).Within(0.02f));
                Assert.That(HiveViewProductUiPresenter.OfficialUpgradeRemainingTextForExternalHost(), Is.Not.Empty);

                HiveViewProductUiPresenter.UseBuildingUpgradeControllerForProof(new FakePanelController(
                    HiveBuildingUpgradePresentation.Ready(
                        SnapshotWithOperation(HiveBuildingUpgradeClient.AwaitingCompletionStatus),
                        TimeSpan.Zero),
                    TimeSpan.FromMinutes(11)));
                Assert.That(HiveViewProductUiPresenter.OfficialUpgradeProgress01ForExternalHost(), Is.Zero,
                    "En attente de validation, la barre laisse la place a l'indicateur d'achevement officiel.");
                Assert.That(HiveViewProductUiPresenter.OfficialUpgradeRemainingTextForExternalHost(), Is.Empty);

                HiveViewProductUiPresenter.UseBuildingUpgradeControllerForProof(new FakePanelController(
                    HiveBuildingUpgradePresentation.Ready(SnapshotWithLevels(1), TimeSpan.Zero),
                    TimeSpan.Zero));
                Assert.That(HiveViewProductUiPresenter.OfficialUpgradeProgress01ForExternalHost(), Is.Zero);
            }
            finally
            {
                HiveViewProductUiPresenter.UseBuildingUpgradeControllerForProof(null);
            }
        }

        [Test]
        public void ProgressWindowIsRegisteredAsAWorldInputBlocker()
        {
            try
            {
                HiveViewProductUiPresenter.UseBuildingUpgradeControllerForProof(new FakePanelController(
                    HiveBuildingUpgradePresentation.Ready(
                        SnapshotWithOperation(HiveBuildingUpgradeClient.RunningStatus),
                        TimeSpan.Zero),
                    TimeSpan.FromMinutes(1)));

                // Reference prise sur l'etat courant : d'autres tests du meme domaine peuvent
                // laisser des drapeaux d'overlay poses. Seul le DELTA nous interesse.
                bool baseline = HiveMapOverlayInputGateBootstrap.IsAnyOverlayBlocking();

                HiveViewProductUiPresenter.TryOpenUpgradeProgressOverlayForExternalHost("wax_workshop");
                Assert.That(HiveMapOverlayInputGateBootstrap.IsAnyOverlayBlocking(), Is.True,
                    "Sans ca, un clic destine a la fenetre atteindrait aussi le batiment derriere elle.");

                HiveViewProductUiPresenter.CloseUpgradeProgressOverlayForExternalHost();
                Assert.That(HiveMapOverlayInputGateBootstrap.IsAnyOverlayBlocking(), Is.EqualTo(baseline),
                    "Et la fermeture doit rendre la main au monde - pas de drapeau orphelin.");
            }
            finally
            {
                HiveViewProductUiPresenter.CloseUpgradeProgressOverlayForExternalHost();
                HiveViewProductUiPresenter.UseBuildingUpgradeControllerForProof(null);
            }
        }

        // ------------------------------------------------------------------------ fixtures

        private static RemoteBuildingUpgradeSnapshot SnapshotWithLevels(int waxWorkshopLevel)
        {
            DateTimeOffset now = new DateTimeOffset(2026, 9, 7, 12, 0, 0, TimeSpan.Zero);
            return new RemoteBuildingUpgradeSnapshot
            {
                PlayerId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                HiveId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                ContractVersion = HiveBuildingUpgradeClient.ContractVersion,
                CatalogVersion = "m059-test",
                Revision = 1,
                ServerTimeUtc = now,
                Balances = new Dictionary<string, RemoteBuildingUpgradeBalance>
                {
                    ["honey"] = new RemoteBuildingUpgradeBalance { Amount = 100, Capacity = 1000 },
                    ["pollen"] = new RemoteBuildingUpgradeBalance { Amount = 100, Capacity = 1000 }
                },
                BuildingLevels = new Dictionary<string, int>
                {
                    ["wax_workshop"] = waxWorkshopLevel,
                    ["honey_storage"] = 1,
                    ["guard_post"] = 1
                },
                Offers = new List<RemoteBuildingUpgradeOffer>
                {
                    new RemoteBuildingUpgradeOffer
                    {
                        BuildingKey = "wax_workshop", FromLevel = 1, ToLevel = 2,
                        Duration = TimeSpan.FromMinutes(10),
                        Costs = new Dictionary<string, long> { ["honey"] = 10, ["pollen"] = 20 }
                    }
                }
            };
        }

        private static RemoteBuildingUpgradeSnapshot SnapshotWithOperation(string status)
        {
            RemoteBuildingUpgradeSnapshot snapshot = SnapshotWithLevels(1);
            snapshot.ActiveOperation = new RemoteBuildingUpgradeOperation
            {
                OperationId = Guid.Parse("99999999-8888-7777-6666-555555555555"),
                BuildingKey = "wax_workshop",
                FromLevel = 1,
                ToLevel = 2,
                StartedAtUtc = snapshot.ServerTimeUtc,
                CompletesAtUtc = snapshot.ServerTimeUtc.AddMinutes(10),
                Status = status
            };
            return snapshot;
        }
    }
}
