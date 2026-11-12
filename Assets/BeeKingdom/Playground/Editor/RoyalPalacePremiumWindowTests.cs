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
    public sealed class RoyalPalacePremiumWindowTests
    {
        [TestCase("fr-CA")]
        [TestCase("en-US")]
        public void RefreshAndReopenKeepCanvasScrollAndLocalizedActions(string locale)
        {
            string previousLocale = BeeLocalization.CurrentLocale;
            GameObject host = new GameObject("Palace presentation test");
            host.SetActive(false);
            try
            {
                BeeLocalization.SetLocale(locale);
                var view = host.AddComponent<HiveMapRoyalPalacePremiumWindow>();
                view.Show(view.Hide);
                Canvas canvas = host.GetComponentInChildren<Canvas>(true);
                ScrollRect scroll = host.GetComponentInChildren<ScrollRect>(true);
                Transform[] identities = host.GetComponentsInChildren<Transform>(true);
                scroll.verticalNormalizedPosition = .43f;
                float retainedPosition = scroll.verticalNormalizedPosition;
                for (int i = 0; i < 10; i++)
                    typeof(HiveMapRoyalPalacePremiumWindow).GetMethod("RefreshLabels",
                        BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);
                Assert.That(host.GetComponentsInChildren<Transform>(true), Is.EqualTo(identities));
                Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(retainedPosition).Within(.01f));
                Assert.That(scroll.verticalScrollbar, Is.Not.Null);
                string[] labels = host.GetComponentsInChildren<TextMeshProUGUI>(true)
                    .Select(item => item.text).ToArray();
                foreach (string key in new[] { "common.back", "building_upgrade.action.refresh", "royal_palace.colony_view" })
                    Assert.That(labels, Does.Contain(BeeLocalization.Text(key)));
                Button back = host.GetComponentsInChildren<Button>(true).Single(item => item.name == "Back");
                back.onClick.Invoke();
                Assert.That(view.IsOpen, Is.False);
                Assert.That(canvas.gameObject.activeSelf, Is.False);
                view.Show(view.Hide);
                Assert.That(view.IsOpen, Is.True);
                Assert.That(host.GetComponentInChildren<Canvas>(true), Is.SameAs(canvas));
                Assert.That(host.GetComponentInChildren<ScrollRect>(true), Is.SameAs(scroll));
                view.SetSuppressed(true);
                Assert.That(canvas.gameObject.activeSelf, Is.False);
                view.SetSuppressed(false);
                Assert.That(canvas.gameObject.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(host);
                BeeLocalization.SetLocale(previousLocale);
            }
        }

        [Test]
        public void DisabledBootstrapCannotReopenTheModalFromSelectionEvents()
        {
            GameObject host = new GameObject("Palace disabled lifecycle test");
            host.SetActive(false);
            try
            {
                var bootstrap = host.AddComponent<HiveMapRoyalPalaceBootstrap>();
                typeof(HiveMapRoyalPalaceBootstrap).GetMethod("OnBuildingClicked",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(bootstrap, new object[]
                    { BuildingCatalog.GetByBuildingType(BuildingTypes.RoyalPalace) });
                Assert.That(HiveMapRoyalPalaceBootstrap.OverlayOpenForExternalHost, Is.False);
                Assert.That(host.GetComponent<HiveMapRoyalPalacePremiumWindow>(), Is.Null);
            }
            finally { Object.DestroyImmediate(host); }
        }
    }
}
