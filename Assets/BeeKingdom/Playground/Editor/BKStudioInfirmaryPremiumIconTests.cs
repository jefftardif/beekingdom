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
        public void InfirmaryUsesTheApprovedEmblemForItsTwoCareCards()
        {
            GameObject host = new GameObject("Infirmary Premium Icon QA");
            host.SetActive(false);
            try
            {
                var view = host.AddComponent<HiveMapPremiumBuildingWindow>();
                var building = BuildingCatalog.GetByBuildingType(BuildingTypes.Infirmary);
                view.Show(building, "Infirmerie", "Infirmary", "", "",
                    () => { }, () => { });

                Texture2D asset = Resources.Load<Texture2D>(
                    HiveMapPremiumBuildingWindow.InfirmaryPremiumIconResourcePath);
                Assert.That(asset, Is.Not.Null);
                RawImage[] all = host.GetComponentsInChildren<RawImage>(true);
                RawImage[] premium = all.Where(image => image.texture == asset).ToArray();
                Assert.That(premium.Length, Is.EqualTo(2),
                    "Only Centre de convalescence and Suivi des soins receive the new emblem.");
                foreach (var image in premium)
                {
                    Assert.That(image.raycastTarget, Is.False);
                    Assert.That(image.rectTransform.rect.width,
                        Is.GreaterThanOrEqualTo(48f));
                    Assert.That(image.rectTransform.rect.height,
                        Is.GreaterThanOrEqualTo(48f));
                }

                Assert.That(view.InfirmaryActiveForProof, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
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
