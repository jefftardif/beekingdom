using System;
using System.Linq;
using BeeKingdom.Buildings.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BeeKingdom.Playground.Editor
{
    public sealed class HivePremiumProductionCardIconTests
    {
        [Test]
        public void ProductionResourceAndLedgerIconsExistAsActualPremiumArt()
        {
            Texture2D resources = Resources.Load<Texture2D>("PremiumBeeIcons/resources");
            Texture2D book = Resources.Load<Texture2D>("PremiumBeeIcons/book");
            Assert.That(resources, Is.Not.Null, "Missing dedicated resources emblem");
            Assert.That(book, Is.Not.Null, "Missing dedicated production ledger emblem");
            Assert.That(resources.width, Is.GreaterThanOrEqualTo(192));
            Assert.That(book.width, Is.GreaterThanOrEqualTo(192));
        }

        [TestCase(192f, 192f)]
        [TestCase(1254f, 1254f)]
        [TestCase(384f, 192f)]
        [TestCase(192f, 384f)]
        public void SharedPremiumIconContainFillsItsInsetWithoutCropping(
            float sourceWidth, float sourceHeight)
        {
            Rect frame = new Rect(12f, 6f, 72f, 72f);
            Rect fitted = HiveMapPremiumBuildingWindow
                .PremiumCardIconContainRectForProof(
                    frame, 6f, sourceWidth, sourceHeight);
            Assert.That(fitted.xMin, Is.GreaterThanOrEqualTo(frame.xMin + 5.99f));
            Assert.That(fitted.yMin, Is.GreaterThanOrEqualTo(frame.yMin + 5.99f));
            Assert.That(fitted.xMax, Is.LessThanOrEqualTo(frame.xMax - 5.99f));
            Assert.That(fitted.yMax, Is.LessThanOrEqualTo(frame.yMax - 5.99f));
            Assert.That(fitted.width / fitted.height,
                Is.EqualTo(sourceWidth / sourceHeight).Within(0.001f));
        }

        [TestCase(BuildingTypes.HoneyReserve)]
        [TestCase(BuildingTypes.Warehouse)]
        [TestCase(BuildingTypes.Transformation)]
        public void ProducerCardsHaveThreeFullFrameIconsAndRoomyHeaders(string type)
        {
            GameObject root = new GameObject("Production card QA", typeof(RectTransform));
            root.SetActive(false);
            try
            {
                var window = root.AddComponent<HiveMapPremiumBuildingWindow>();
                window.ShowProductionInfo(BuildingCatalog.GetByBuildingType(type),
                    "Production", "Production",
                    new UnavailableHiveOfflineProductionPanelController(),
                    window.Hide, () => {});

                RawImage[] icons = root.GetComponentsInChildren<RawImage>(true)
                    .Where(image => image.name == "PremiumCardIcon").ToArray();
                Assert.That(icons.Length, Is.EqualTo(3),
                    "All production cards must use the shared premium icon frame");

                Texture2D resources = Resources.Load<Texture2D>("PremiumBeeIcons/resources");
                Texture2D book = Resources.Load<Texture2D>("PremiumBeeIcons/book");
                Assert.That(icons.Any(icon => icon.texture == resources), Is.True);
                Assert.That(icons.Any(icon => icon.texture == book), Is.True);
                foreach (RawImage icon in icons)
                {
                    Rect rect = icon.rectTransform.rect;
                    Assert.That(rect.width, Is.GreaterThan(0f));
                    Assert.That(rect.height, Is.GreaterThan(0f));
                    Assert.That(rect.width, Is.LessThanOrEqualTo(60.01f));
                    Assert.That(rect.height, Is.LessThanOrEqualTo(60.01f));
                    Assert.That(icon.uvRect, Is.EqualTo(new Rect(0f, 0f, 1f, 1f)));
                    Assert.That(icon.raycastTarget, Is.False);
                    Assert.That(icon.texture, Is.Not.Null);
                }
                window.Hide();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
