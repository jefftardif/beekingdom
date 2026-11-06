using System;
using System.Linq;
using BeeKingdom.Buildings.Interaction;
using BeeKingdom.Localization;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeeKingdom.Playground.Editor
{
    public sealed class BKStudioGeneticsPremiumWindowTests
    {
        [Test]
        public void GeneticsShowsBilingualPremiumWindowWithNoInventedGeneticActions()
        {
            string previousLocale = BeeLocalization.CurrentLocale;
            GameObject host = new GameObject("Genetics Bilingual Premium QA");
            host.SetActive(false);
            try
            {
                var view = host.AddComponent<HiveMapPremiumBuildingWindow>();
                BuildingDefinition building = BuildingCatalog.GetByBuildingType(
                    BuildingTypes.Genetics);
                Assert.That(building, Is.Not.Null);
                foreach (string locale in new[] { "fr-CA", "en-US" })
                {
                    Assert.That(BeeLocalization.SetLocale(locale), Is.True);
                    view.Show(building, "Génétique", "Genetics",
                        string.Empty, string.Empty, () => { }, () => { });
                    Assert.That(view.GeneticsActiveForProof, Is.True);
                    Assert.That(view.InfirmaryActiveForProof, Is.False);
                    Assert.That(view.NurseryActiveForProof, Is.False);
                    Assert.That(view.DefenseActiveForProof, Is.False);

                    TextMeshProUGUI[] labels =
                        host.GetComponentsInChildren<TextMeshProUGUI>(true);
                    string[] texts = labels.Select(x => x.text).ToArray();
                    string prefix = "genetics.premium.";
                    Assert.That(texts, Does.Contain(BeeLocalization.Text(
                        prefix + "card.crossing")));
                    Assert.That(texts, Does.Contain(BeeLocalization.Text(
                        prefix + "branch.foraging.title")));
                    Assert.That(texts, Does.Contain(BeeLocalization.Text(
                        prefix + "branch.resilience.title")));
                    Assert.That(texts, Does.Contain(BeeLocalization.Text(
                        prefix + "branch.combat.title")));
                    Assert.That(texts, Does.Contain(BeeLocalization.Text(
                        prefix + "preview_only")));
                    Assert.That(texts, Does.Contain(BeeLocalization.Text(
                        prefix + "no_experiments")));
                    Assert.That(texts, Does.Contain(BeeLocalization.Text(
                        prefix + "open_research")));
                    Assert.That(texts, Does.Contain(BeeLocalization.Text(
                        prefix + "card.progression")));
                    Assert.That(texts, Does.Contain(BeeLocalization.Text(
                        prefix + "upgrade.construction_link")));
                    Assert.That(host.GetComponentsInChildren<ScrollRect>(true),
                        Has.Length.EqualTo(1),
                        "Genetics must retain the native vertical scroll and scrollbar.");

                    // Exactly three actionable buttons: Back, building Upgrade,
                    // and navigate to genuine Research; NO mutation button.
                    Assert.That(host.GetComponentsInChildren<Button>(true),
                        Has.Length.EqualTo(3));
                    string upgrade = locale == "fr-CA" ? "Améliorer" : "Upgrade";
                    Assert.That(texts.Count(x => x == upgrade),
                        Is.EqualTo(1), "Only one building Upgrade button.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                BeeLocalization.SetLocale(previousLocale);
            }
        }

        [Test]
        public void OpenGeneticsWindowChangesFRAndENWithoutClosing()
        {
            string previous = BeeLocalization.CurrentLocale;
            GameObject host = new GameObject("Genetics Live Locale QA");
            host.SetActive(false);
            try
            {
                Assert.That(BeeLocalization.SetLocale("fr-CA"), Is.True);
                var view = host.AddComponent<HiveMapPremiumBuildingWindow>();
                view.Show(BuildingCatalog.GetByBuildingType(BuildingTypes.Genetics),
                    "Génétique", "Genetics", "", "", () => { }, () => { });
                var update = typeof(HiveMapPremiumBuildingWindow).GetMethod("Update",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);
                Assert.That(update, Is.Not.Null);
                Assert.That(view.GeneticsActiveForProof, Is.True);

                Assert.That(BeeLocalization.SetLocale("en-US"), Is.True);
                update.Invoke(view, null);
                string[] english = host.GetComponentsInChildren<TextMeshProUGUI>(true)
                    .Select(x => x.text).ToArray();
                Assert.That(english, Does.Contain("GENETICS"));
                Assert.That(english, Does.Contain("BREEDING AND SELECTION"));
                Assert.That(english, Does.Contain("DESIGN PREVIEW — NO BONUS APPLIED"));
                Assert.That(view.GeneticsActiveForProof, Is.True);

                Assert.That(BeeLocalization.SetLocale("fr-CA"), Is.True);
                update.Invoke(view, null);
                string[] french = host.GetComponentsInChildren<TextMeshProUGUI>(true)
                    .Select(x => x.text).ToArray();
                Assert.That(french, Does.Contain("GÉNÉTIQUE"));
                Assert.That(french, Does.Contain("CROISEMENTS ET SÉLECTION"));
                Assert.That(view.GeneticsActiveForProof, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                BeeLocalization.SetLocale(previous);
            }
        }
        [Test]
        public void PhysicalGeneticsBuildingOpensPremiumAndClosesWithoutLegacyPopup()
        {
            GameObject root = new GameObject("Genetics Entry Route QA");
            root.SetActive(false);
            HiveMapUnsupportedBuildingBootstrap host = null;
            try
            {
                host = root.AddComponent<HiveMapUnsupportedBuildingBootstrap>();
                var select = typeof(HiveMapUnsupportedBuildingBootstrap).GetMethod(
                    "OnBuildingClicked", System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);
                var close = typeof(HiveMapUnsupportedBuildingBootstrap).GetMethod(
                    "CloseGeneticsWindow", System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);
                Assert.That(select, Is.Not.Null);
                Assert.That(close, Is.Not.Null);

                select.Invoke(host, new object[] {
                    BuildingCatalog.GetByBuildingType(BuildingTypes.Genetics)
                });
                Assert.That(HiveMapUnsupportedBuildingBootstrap.OverlayOpenForExternalHost,
                    Is.True);
                HiveMapPremiumBuildingWindow view =
                    root.GetComponentInChildren<HiveMapPremiumBuildingWindow>(true);
                Assert.That(view, Is.Not.Null);
                Assert.That(view.GeneticsActiveForProof, Is.True);
                Assert.That(HiveMapOverlayInputGateBootstrap.IsAnyOverlayBlocking(),
                    Is.True, "The fullscreen Genetics window must block clicks into the hive.");
                Assert.That(root.GetComponentsInChildren<ScrollRect>(true).Length,
                    Is.EqualTo(1));

                Button back = root.GetComponentsInChildren<Button>(true)
                    .Single(button =>
                    {
                        string title = button.GetComponentInChildren<TextMeshProUGUI>(
                            true)?.text;
                        return title == "Retour" || title == "Back";
                    });
                back.onClick.Invoke();
                Assert.That(HiveMapUnsupportedBuildingBootstrap.OverlayOpenForExternalHost,
                    Is.False);
                Assert.That(view.GeneticsActiveForProof, Is.False);
            }
            finally
            {
                if (host != null)
                {
                    var close = typeof(HiveMapUnsupportedBuildingBootstrap).GetMethod(
                        "CloseGeneticsWindow", System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic);
                    close?.Invoke(host, null);
                }
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
        [Test]
        public void MovingBetweenDefenseGeneticsAndInfirmaryKeepsOnlyOneWindowOpen()
        {
            GameObject root = new GameObject("Premium Route Isolation QA");
            root.SetActive(false);
            var host = root.AddComponent<HiveMapUnsupportedBuildingBootstrap>();
            var select = typeof(HiveMapUnsupportedBuildingBootstrap).GetMethod(
                "OnBuildingClicked", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            Assert.That(select, Is.Not.Null);
            try
            {
                foreach (string buildingType in new[]
                {
                    BuildingTypes.Defense, BuildingTypes.Genetics,
                    BuildingTypes.Infirmary, BuildingTypes.Genetics
                })
                {
                    select.Invoke(host, new object[]
                    {
                        BuildingCatalog.GetByBuildingType(buildingType)
                    });
                    HiveMapPremiumBuildingWindow[] views =
                        root.GetComponentsInChildren<HiveMapPremiumBuildingWindow>(true);
                    Assert.That(views.Count(v => v.IsOpen), Is.EqualTo(1),
                        "Switching buildings must hide previously open fullscreen windows.");
                    var current = views.Single(v => v.IsOpen);
                    Assert.That(current.GeneticsActiveForProof,
                        Is.EqualTo(buildingType == BuildingTypes.Genetics));
                    Assert.That(current.DefenseActiveForProof,
                        Is.EqualTo(buildingType == BuildingTypes.Defense));
                    Assert.That(current.InfirmaryActiveForProof,
                        Is.EqualTo(buildingType == BuildingTypes.Infirmary));
                }
            }
            finally
            {
                // Disabled EditMode GameObjects do not reliably receive OnDestroy;
                // explicitly release the shared overlay gate before the next test.
                var close = typeof(HiveMapUnsupportedBuildingBootstrap).GetMethod(
                    "CloseInfirmaryWindow",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);
                close?.Invoke(host, null);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GeneticsBannerAndLocalizationCatalogRemainAvailable()
        {
            Texture2D banner = Resources.Load<Texture2D>(
                "PremiumBeeReference/BuildingBanners/genetics_garden");
            Assert.That(banner, Is.Not.Null,
                "Use Genetics building artwork, not the generic Alliance banner.");
            foreach (string locale in new[] { "fr-CA", "en-US" })
            {
                foreach (string key in new[]
                {
                    "subtitle", "level", "card.status", "card.vision",
                    "branch.foraging.title", "branch.resilience.title",
                    "branch.combat.title", "crossing_description", "no_experiments",
                    "no_genetics_server", "preview_only", "open_research",
                    "card.progression", "sync.disconnected", "sync.loading",
                    "sync.error", "sync.offline", "sync.ready",
                    "upgrade.next", "upgrade.cost_duration", "upgrade.no_cost",
                    "upgrade.running", "upgrade.complete",
                    "upgrade.construction_link", "upgrade.awaiting", "upgrade.max"
                })
                {
                    Assert.That(BeeLocalization.HasText(locale,
                        "genetics.premium." + key), Is.True,
                        "Missing Genetics translation: " + locale + " / " + key);
                }
            }
        }
    }
}