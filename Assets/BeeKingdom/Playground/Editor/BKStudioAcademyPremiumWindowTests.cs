using System;
using System.Linq;
using System.Reflection;
using BeeKingdom.Buildings.Interaction;
using BeeKingdom.Localization;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeeKingdom.Playground.Editor
{
    public sealed class BKStudioAcademyPremiumWindowTests
    {
        private const BindingFlags Instance = BindingFlags.NonPublic | BindingFlags.Instance;

        private static void Invoke(object target, string method, params object[] args)
        {
            MethodInfo call = target.GetType().GetMethod(method, Instance);
            Assert.That(call, Is.Not.Null, method);
            call.Invoke(target, args);
        }

        private static string[] Labels(GameObject parent)
            => parent.GetComponentsInChildren<TextMeshProUGUI>(true)
                .Select(item => item.text).ToArray();

        private static Button FindButton(GameObject parent, string caption)
            => parent.GetComponentsInChildren<Button>(true)
                .Single(button => button.GetComponentInChildren<TextMeshProUGUI>(
                    true)?.text == caption);

        [Test]
        public void AcademyHasFourBilingualProgrammePreviewsAndOnlyRealNavigationActions()
        {
            string previous = BeeLocalization.CurrentLocale;
            GameObject root = new GameObject("Academy Premium Bilingual QA");
            root.SetActive(false);
            try
            {
                var view = root.AddComponent<HiveMapPremiumBuildingWindow>();
                BuildingDefinition academy = BuildingCatalog.GetByBuildingType(
                    BuildingTypes.Academy);
                Assert.That(academy, Is.Not.Null);
                foreach (string locale in new[] { "fr-CA", "en-US" })
                {
                    Assert.That(BeeLocalization.SetLocale(locale), Is.True);
                    view.Show(academy, "Acad\u00e9mie", "Academy",
                        string.Empty, string.Empty, view.Hide, () => { });
                    Assert.That(view.AcademyActiveForProof, Is.True);
                    Assert.That(view.InfirmaryActiveForProof, Is.False);
                    Assert.That(view.GeneticsActiveForProof, Is.False);
                    Assert.That(view.DefenseActiveForProof, Is.False);
                    Assert.That(view.NurseryActiveForProof, Is.False);

                    string prefix = "academy.premium.";
                    string[] labels = Labels(root);
                    foreach (string key in new[]
                    {
                        "card.state", "card.construction", "card.palace",
                        "card.mission", "card.training", "card.research",
                        "card.records", "course.tactics.title",
                        "course.logistics.title", "course.exploration.title",
                        "course.mentorship.title", "course.awaiting",
                        "records.no_academy_history",
                        "training.open", "research.open"
                    })
                    {
                        Assert.That(labels, Does.Contain(BeeLocalization.Text(prefix + key)),
                            "Missing Academy UI label: " + key + " (" + locale + ")");
                    }

                    Assert.That(root.GetComponentsInChildren<ScrollRect>(true),
                        Has.Length.EqualTo(1));
                    ScrollRect scroll =
                        root.GetComponentInChildren<ScrollRect>(true);
                    Assert.That(scroll.verticalScrollbar, Is.Not.Null);
                    Assert.That(scroll.horizontal, Is.False);
                    Assert.That(root.GetComponentInChildren<GraphicRaycaster>(true),
                        Is.Not.Null);
                    // Back, Construction, Barracks, Research, and exactly four
                    // actual Academy course controls (server-disabled if offline).
                    Assert.That(root.GetComponentsInChildren<Button>(true),
                        Has.Length.EqualTo(8));
                    Assert.That(root.GetComponentsInChildren<Button>(true)
                        .Count(b => b.GetComponentInChildren<TextMeshProUGUI>(true)
                            ?.text == BeeLocalization.Text(prefix + "course.start")),
                        Is.EqualTo(4));
                    string upgrade = locale == "fr-CA" ? "Am\u00e9liorer" : "Upgrade";
                    Assert.That(labels.Count(value => value == upgrade),
                        Is.EqualTo(1));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                BeeLocalization.SetLocale(previous);
            }
        }

        [Test]
        public void PhysicalAcademyRoutesToFullscreenAndBackClosesOverlay()
        {
            string previous = BeeLocalization.CurrentLocale;
            GameObject root = new GameObject("Academy Physical Entry QA");
            root.SetActive(false);
            var host = root.AddComponent<HiveMapUnsupportedBuildingBootstrap>();
            try
            {
                Assert.That(BeeLocalization.SetLocale("fr-CA"), Is.True);
                Invoke(host, "OnBuildingClicked",
                    BuildingCatalog.GetByBuildingType(BuildingTypes.Academy));
                var view = root.GetComponentInChildren<HiveMapPremiumBuildingWindow>(
                    true);
                Assert.That(view, Is.Not.Null);
                Assert.That(view.AcademyActiveForProof, Is.True);
                Assert.That(HiveMapUnsupportedBuildingBootstrap.OverlayOpenForExternalHost,
                    Is.True);
                Assert.That(HiveMapOverlayInputGateBootstrap.IsAnyOverlayBlocking(),
                    Is.True);
                Assert.That(root.GetComponentsInChildren<ScrollRect>(true),
                    Has.Length.EqualTo(1));
                Button goldBack = view.gameObject.GetComponentsInChildren<Button>(true)
                    .Single(button => button.name == "PremiumBack");
                Assert.That(goldBack.targetGraphic,Is.TypeOf<Image>());
                goldBack.onClick.Invoke();
                Assert.That(view.IsOpen, Is.False);
                Assert.That(HiveMapUnsupportedBuildingBootstrap.OverlayOpenForExternalHost,
                    Is.False);
            }
            finally
            {
                Invoke(host, "CloseAcademyWindow");
                UnityEngine.Object.DestroyImmediate(root);
                BeeLocalization.SetLocale(previous);
            }
        }

        [Test]
        public void SwitchingOtherPremiumBuildingsDoesNotLeaveAnAcademyCanvasOpen()
        {
            GameObject root = new GameObject("Academy Route Isolation QA");
            root.SetActive(false);
            var host = root.AddComponent<HiveMapUnsupportedBuildingBootstrap>();
            try
            {
                foreach (string building in new[]
                {
                    BuildingTypes.Defense, BuildingTypes.Academy,
                    BuildingTypes.Genetics, BuildingTypes.Academy,
                    BuildingTypes.Infirmary, BuildingTypes.Academy
                })
                {
                    Invoke(host, "OnBuildingClicked",
                        BuildingCatalog.GetByBuildingType(building));
                    var views = root.GetComponentsInChildren<
                        HiveMapPremiumBuildingWindow>(true);
                    Assert.That(views.Count(view => view.IsOpen), Is.EqualTo(1),
                        "Switching buildings must not leave overlapping fullscreen modals.");
                    Assert.That(views.Single(view => view.IsOpen).AcademyActiveForProof,
                        Is.EqualTo(building == BuildingTypes.Academy));
                }
                Invoke(host, "CloseAcademyWindow");
                Assert.That(HiveMapUnsupportedBuildingBootstrap.OverlayOpenForExternalHost,
                    Is.False);
            }
            finally
            {
                Invoke(host, "CloseAcademyWindow");
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AcademyNavigatesToRealBarracksAndResearchWithoutSimulatingCourses()
        {
            string previous = BeeLocalization.CurrentLocale;
            GameObject root = new GameObject("Academy Real Navigation QA");
            root.SetActive(false);
            var view = root.AddComponent<HiveMapPremiumBuildingWindow>();
            try
            {
                Assert.That(BeeLocalization.SetLocale("fr-CA"), Is.True);
                BuildingDefinition academy =
                    BuildingCatalog.GetByBuildingType(BuildingTypes.Academy);
                view.Show(academy, "Acad\u00e9mie", "Academy",
                    "", "", view.Hide, () => { });
                FindButton(view.gameObject,
                    BeeLocalization.Text("academy.premium.training.open"))
                    .onClick.Invoke();
                Assert.That(view.IsOpen, Is.False);
                Assert.That(HiveViewProductUiPresenter.BarrackOverlayOpenForExternalHost,
                    Is.True);
                HiveViewProductUiPresenter.CloseBarrackOverlayForExternalHost();

                view.Show(academy, "Acad\u00e9mie", "Academy",
                    "", "", view.Hide, () => { });
                FindButton(view.gameObject,
                    BeeLocalization.Text("academy.premium.research.open"))
                    .onClick.Invoke();
                Assert.That(view.IsOpen, Is.False);
                Assert.That(HiveViewProductUiPresenter.ResearchOverlayOpenForExternalHost,
                    Is.True);
            }
            finally
            {
                HiveViewProductUiPresenter.CloseBarrackOverlayForExternalHost();
                HiveViewProductUiPresenter.CloseResearchOverlayForExternalHost();
                UnityEngine.Object.DestroyImmediate(root);
                BeeLocalization.SetLocale(previous);
            }
        }

        [Test]
        public void AcademyLiveLocaleChangesWithoutClosingOrJumpingToTop()
        {
            string previous = BeeLocalization.CurrentLocale;
            GameObject root = new GameObject("Academy Locale Transition QA");
            root.SetActive(false);
            try
            {
                Assert.That(BeeLocalization.SetLocale("fr-CA"), Is.True);
                var view = root.AddComponent<HiveMapPremiumBuildingWindow>();
                view.Show(BuildingCatalog.GetByBuildingType(BuildingTypes.Academy),
                    "Acad\u00e9mie", "Academy", "", "", view.Hide, () => { });
                ScrollRect scroll = root.GetComponentInChildren<ScrollRect>(true);
                scroll.verticalNormalizedPosition = .37f;

                Assert.That(BeeLocalization.SetLocale("en-US"), Is.True);
                Invoke(view, "Update");
                Assert.That(Labels(root), Does.Contain("ACADEMY"));
                Assert.That(Labels(root), Does.Contain("ACADEMY STATUS"));
                Assert.That(view.AcademyActiveForProof, Is.True);
                scroll = root.GetComponentInChildren<ScrollRect>(true);
                Assert.That(scroll.verticalNormalizedPosition,
                    Is.EqualTo(.37f).Within(.08f));

                Assert.That(BeeLocalization.SetLocale("fr-CA"), Is.True);
                Invoke(view, "Update");
                Assert.That(Labels(root), Does.Contain("ACAD\u00c9MIE"));
                Assert.That(Labels(root), Does.Contain("\u00c9TAT DE L\u2019ACAD\u00c9MIE"));
                Assert.That(view.AcademyActiveForProof, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                BeeLocalization.SetLocale(previous);
            }
        }

        [Test]
        public void ResponsiveAcademyHasOneColumnOnPhonesAndTwoOnWideLandscape()
        {
            Assert.That(HiveMapPremiumBuildingWindow.AcademyProgramColumnsForProof(320f),
                Is.EqualTo(1));
            Assert.That(HiveMapPremiumBuildingWindow.AcademyProgramColumnsForProof(890f),
                Is.EqualTo(1));
            Assert.That(HiveMapPremiumBuildingWindow.AcademyProgramColumnsForProof(1000f),
                Is.EqualTo(2));
            Assert.That(HiveMapPremiumBuildingWindow.AcademyProgramHeightForProof(320f),
                Is.GreaterThan(
                    HiveMapPremiumBuildingWindow.AcademyProgramHeightForProof(1000f)));
        }

        [Test]
        public void AcademyBannerAndFullBilingualCatalogExist()
        {
            Assert.That(Resources.Load<Texture2D>(
                "PremiumBeeReference/BuildingBanners/academy_canopy"),
                Is.Not.Null);
            foreach (string locale in new[] { "fr-CA", "en-US" })
            {
                foreach (string key in new[]
                {
                    "subtitle", "card.state", "level.value", "sync.loading",
                    "sync.offline", "sync.disconnected", "sync.ready", "sync.error",
                    "card.construction", "upgrade.next", "upgrade.running",
                    "upgrade.complete", "upgrade.cost_duration", "upgrade.max",
                    "card.palace", "palace.requirement", "palace.satisfied",
                    "palace.unsatisfied", "palace.no_requirement",
                    "card.mission", "course.tactics.title",
                    "course.logistics.title", "course.exploration.title",
                    "course.mentorship.title", "course.start",
                    "course.complete", "course.completed_button",
                    "course.awaiting", "course.running", "course.ready",
                    "course.requirements", "records.summary",
                    "card.training", "training.active", "training.ready",
                    "training.idle", "training.open", "card.research",
                    "research.active", "research.ready", "research.completed",
                    "research.open", "card.records", "records.no_academy_history"
                })
                {
                    Assert.That(BeeLocalization.HasText(locale,
                        "academy.premium." + key), Is.True,
                        "Missing Academy translation " + locale + " / " + key);
                }
            }
        }
    }
}
