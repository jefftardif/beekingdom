using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BeeKingdom.Buildings.Interaction;
using BeeKingdom.Networking;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground.Editor
{
    // M059-CL, correctif de routage post-certification CEO.
    //
    // Le CEO a teste en Play Mode reel : Defense etait bien en cours d'amelioration (le Palais
    // Royal repondait correctement "Un autre batiment occupe la file de construction"), mais
    // AUCUNE barre de progression n'apparaissait sur Defense, et cliquer Defense ouvrait sa
    // fenetre ordinaire au lieu de la fenetre d'amelioration en cours.
    //
    // CAUSE UNIQUE, prouvee ici : HiveMapBuildingUpgradeProgressBootstrap n'etait pas cable dans
    // HiveMapRuntimeBootstrapInitializer. Son seul point d'entree etait son propre
    // [RuntimeInitializeOnLoadMethod(AfterSceneLoad)], qui ne se declenche qu'une fois sur la
    // scene active au demarrage du Play Mode (splash/login, jamais "Environment2D5D*"). Le
    // composant n'existait donc JAMAIS dans la scene reelle : pas d'OnGUI (aucune barre) et
    // surtout aucun RegisterCompletionPreemption (le clic retombait sur la fenetre ordinaire).
    // Exactement le meme defaut que M038B-CL et M049C-CL - troisieme recidive, d'ou le test
    // generique ci-dessous qui couvre TOUS les bootstraps d'un coup plutot que celui-ci seul.
    //
    // Le second groupe de tests prouve que la detection "ce batiment est en chantier" est
    // generique : elle marche pour les 14 batiments reels, sans aucune liste figee par nom.
    public sealed class HiveMapUpgradeProgressWiringTests
    {
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

        // ------------------------------------------------- 1. le cablage lui-meme (la cause)

        [Test]
        public void UpgradeProgressBootstrapIsWiredIntoTheProductionSceneInstaller()
        {
            string source = ReadInstallerSource();
            Assert.That(source, Does.Contain("HiveMapBuildingUpgradeProgressBootstrap.InitializeForScene(scene);"),
                "Sans cette ligne le bootstrap n'existe jamais dans la scene reelle : ni barre de " +
                "progression, ni routage de clic InProgress.");
        }

        // Le vrai filet : n'importe quel bootstrap HiveMap qui expose InitializeForScene(Scene)
        // et qui serait oublie dans l'installeur reproduirait le meme bug silencieux. On les
        // enumere par reflexion plutot que de les lister a la main, pour qu'un futur bootstrap
        // soit couvert automatiquement.
        [Test]
        public void EveryBootstrapExposingInitializeForSceneIsCalledByTheProductionInstaller()
        {
            string source = ReadInstallerSource();
            Assembly assembly = typeof(HiveMapRuntimeBootstrapInitializer).Assembly;

            List<string> missing = new List<string>();
            foreach (Type type in assembly.GetTypes())
            {
                if (!typeof(MonoBehaviour).IsAssignableFrom(type)) continue;
                if (type.Namespace != typeof(HiveMapRuntimeBootstrapInitializer).Namespace) continue;
                if (!type.Name.StartsWith("HiveMap", StringComparison.Ordinal)) continue;
                MethodInfo initializer = type.GetMethod(
                    "InitializeForScene",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(Scene) },
                    null);
                if (initializer == null) continue;
                if (!source.Contains(type.Name + ".InitializeForScene(scene)")) missing.Add(type.Name);
            }

            Assert.That(missing, Is.Empty,
                "Ces bootstraps ne seront jamais crees dans la scene reelle (leur AutoStart ne se " +
                "declenche que sur la scene active au demarrage du Play Mode) : " +
                string.Join(", ", missing.ToArray()));
        }

        // ------------------------------------ 2. la detection InProgress, pour TOUS les batiments

        [Test]
        public void RunningUpgradeIsDetectedForEveryRealBuilding()
        {
            try
            {
                foreach (string buildingKey in BuildingLegacyKeys.All)
                {
                    HiveViewProductUiPresenter.UseBuildingUpgradeControllerForProof(new FakePanelController(
                        HiveBuildingUpgradePresentation.Ready(
                            SnapshotWithOperation(buildingKey, HiveBuildingUpgradeClient.RunningStatus),
                            TimeSpan.Zero),
                        TimeSpan.FromMinutes(5)));

                    Assert.That(HiveViewProductUiPresenter.ActiveOfficialUpgradeHotspotIdForExternalHost(),
                        Is.EqualTo(buildingKey),
                        "La detection du chantier en cours doit etre generique, pas reservee a un " +
                        "sous-ensemble de batiments (" + buildingKey + ").");
                    Assert.That(HiveViewProductUiPresenter.OfficialUpgradeProgress01ForExternalHost(),
                        Is.EqualTo(0.5f).Within(0.02f),
                        "La barre monde doit avoir une valeur serveur pour " + buildingKey + ".");

                    Assert.That(HiveViewProductUiPresenter.TryOpenUpgradeProgressOverlayForExternalHost(buildingKey),
                        Is.True, "Le clic doit ouvrir la fenetre d'avancement pour " + buildingKey + ".");
                    HiveViewProductUiPresenter.CloseUpgradeProgressOverlayForExternalHost();

                    string otherKey = buildingKey == BuildingLegacyKeys.GuardPost
                        ? BuildingLegacyKeys.HoneyStorage
                        : BuildingLegacyKeys.GuardPost;
                    Assert.That(HiveViewProductUiPresenter.TryOpenUpgradeProgressOverlayForExternalHost(otherKey),
                        Is.False, "Tout autre batiment garde sa fenetre ordinaire (" + otherKey + ").");
                }
            }
            finally
            {
                HiveViewProductUiPresenter.CloseUpgradeProgressOverlayForExternalHost();
                HiveViewProductUiPresenter.UseBuildingUpgradeControllerForProof(null);
            }
        }

        // Le crochet de clic part d'un BuildingDefinition (donc d'un BuildingType) et doit
        // retrouver la meme cle que celle portee par l'operation serveur. Si cette conversion
        // perdait un batiment, ce batiment precis serait le seul a ne pas repondre au clic -
        // exactement le type de defaut partiel redoute. Aller-retour prouve dans les deux sens.
        [Test]
        public void ClickIdentityRoundTripsForEveryRealBuilding()
        {
            foreach (string buildingKey in BuildingLegacyKeys.All)
            {
                BuildingDefinition definition;
                Assert.That(BuildingCatalog.TryGetByLegacyKey(buildingKey, out definition), Is.True,
                    "Le catalogue doit connaitre " + buildingKey + " (sinon la barre monde ne trouve " +
                    "jamais le GameObject cible).");
                Assert.That(BuildingMappingTable.GetByBuildingType(definition.BuildingType).LegacyKey,
                    Is.EqualTo(buildingKey),
                    "Le clic sur " + definition.BuildingType + " doit resoudre exactement la cle serveur.");
            }
        }

        // ------------------------------------------------------------------------ fixtures

        private static string ReadInstallerSource()
        {
            string path = Path.Combine(
                Application.dataPath,
                "BeeKingdom/Playground/HiveMapRuntimeBootstrapInitializer.cs");
            Assert.That(File.Exists(path), Is.True, "Installeur introuvable : " + path);
            return File.ReadAllText(path);
        }

        private static RemoteBuildingUpgradeSnapshot SnapshotWithOperation(string buildingKey, string status)
        {
            DateTimeOffset now = new DateTimeOffset(2026, 9, 7, 12, 0, 0, TimeSpan.Zero);
            return new RemoteBuildingUpgradeSnapshot
            {
                PlayerId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                HiveId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                ContractVersion = HiveBuildingUpgradeClient.ContractVersion,
                CatalogVersion = "m059-routing-test",
                Revision = 1,
                ServerTimeUtc = now,
                Balances = new Dictionary<string, RemoteBuildingUpgradeBalance>
                {
                    ["honey"] = new RemoteBuildingUpgradeBalance { Amount = 100, Capacity = 1000 },
                    ["pollen"] = new RemoteBuildingUpgradeBalance { Amount = 100, Capacity = 1000 }
                },
                BuildingLevels = BuildingLegacyKeys.All.ToDictionary(key => key, key => 1, StringComparer.Ordinal),
                Offers = new List<RemoteBuildingUpgradeOffer>(),
                ActiveOperation = new RemoteBuildingUpgradeOperation
                {
                    OperationId = Guid.Parse("99999999-8888-7777-6666-555555555555"),
                    BuildingKey = buildingKey,
                    FromLevel = 1,
                    ToLevel = 2,
                    StartedAtUtc = now,
                    CompletesAtUtc = now.AddMinutes(10),
                    Status = status
                }
            };
        }
    }
}
