using System;
using System.Linq;
using BeeKingdom.Buildings.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BeeKingdom.Playground.Editor
{
    public sealed class BKStudioInfirmaryPremiumIconTests
    {
        [Test]
        public void ApprovedGoldHealingEmblemIsImportableWithAlphaAsTexture()
        {
            Texture2D emblem = Resources.Load<Texture2D>(
                HiveMapPremiumBuildingWindow.InfirmaryPremiumIconResourcePath);
            Assert.That(emblem, Is.Not.Null,
                "Founder-approved emblem must be present in the Unity Resources build.");
            Assert.That(emblem.width, Is.GreaterThanOrEqualTo(256));
            Assert.That(emblem.height, Is.EqualTo(emblem.width));
            Assert.That(emblem.format, Is.Not.EqualTo(TextureFormat.Alpha8));
        }

        [Test]
        public void InfirmaryFiveCareCardsUseCompletePremiumMedallionsWithoutClipping()
        {
            GameObject host = new GameObject("Infirmary Premium Icon QA");
            host.SetActive(false);
            try
            {
                var view = host.AddComponent<HiveMapPremiumBuildingWindow>();
                var building = BuildingCatalog.GetByBuildingType(BuildingTypes.Infirmary);
                view.Show(building, "Infirmerie", "Infirmary", "", "",
                    () => { }, () => { });
                string[] names = { "recovery", "guardians",
                    "wingrunners", "darters", "management" };
                RawImage[] premium = host.GetComponentsInChildren<RawImage>(true)
                    .Where(image => image.name == "PremiumCardIcon").ToArray();
                Assert.That(premium.Length, Is.EqualTo(names.Length));
                foreach (string section in names)
                {
                    Texture2D expected = Resources.Load<Texture2D>(
                        "PremiumBeeIcons/infirmary-card-" + section);
                    Assert.That(expected, Is.Not.Null, section);
                    Assert.That(expected.width, Is.EqualTo(384), section);
                    Assert.That(expected.height, Is.EqualTo(384), section);
                    RawImage icon = premium.Single(x => x.texture == expected);
                    Assert.That(icon.raycastTarget, Is.False);
                    Assert.That(icon.uvRect, Is.EqualTo(new Rect(0, 0, 1, 1)));
                    Rect rect = icon.rectTransform.rect;
                    Assert.That(rect.width, Is.InRange(99.99f, 100.01f), section);
                    Assert.That(rect.height, Is.InRange(99.99f, 100.01f), section);
                    float y = -icon.rectTransform.anchoredPosition.y;
                    Assert.That(y, Is.GreaterThanOrEqualTo(9f), section);
                    Assert.That(y + rect.height, Is.LessThan(126f), section);
                    RectTransform card = icon.transform.parent as RectTransform;
                    Assert.That(card, Is.Not.Null);
                    RectTransform separator = card.Find("Separator") as RectTransform;
                    Assert.That(separator, Is.Not.Null);
                    Assert.That(-separator.anchoredPosition.y,
                        Is.EqualTo(128f).Within(.01f), section);
                }
                Assert.That(view.InfirmaryActiveForProof, Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }

        [Test]
        public void OtherFutureBuildingsKeepTheirExistingIcons()
        {
            GameObject host = new GameObject("Other Building Icons QA");
            host.SetActive(false);
            try
            {
                var view = host.AddComponent<HiveMapPremiumBuildingWindow>();
                view.Show(BuildingCatalog.GetByBuildingType(BuildingTypes.Genetics),
                    "Génétique", "Genetics", "", "", () => { }, () => { });
                Texture2D emblem = Resources.Load<Texture2D>(
                    HiveMapPremiumBuildingWindow.InfirmaryPremiumIconResourcePath);
                Assert.That(emblem, Is.Not.Null);
                RawImage[] all = host.GetComponentsInChildren<RawImage>(true);
                Assert.That(all.Any(i => i.texture == emblem), Is.False,
                    "No global icon replacement outside the Infirmary.");
                Assert.That(view.InfirmaryActiveForProof, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }
    }
}
