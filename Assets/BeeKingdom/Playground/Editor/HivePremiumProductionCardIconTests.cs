using System;
using System.Linq;
using BeeKingdom.Buildings.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;

namespace BeeKingdom.Playground.Editor
{
    public sealed class HivePremiumProductionCardIconTests
    {
        [Test]
        public void ProductionResourceAndLedgerIconsExistAsActualPremiumArt()
        {
            foreach (string name in new[] {
                "production-card-status",
                "production-card-capacity",
                "production-card-journal" })
            {
                Texture2D icon = Resources.Load<Texture2D>("PremiumBeeIcons/" + name);
                Assert.That(icon, Is.Not.Null, "Missing full-frame production icon " + name);
                Assert.That(icon.width, Is.EqualTo(384), name);
                Assert.That(icon.height, Is.EqualTo(384), name);
            }
        }

        [TestCase("production-card-status")]
        [TestCase("production-card-capacity")]
        [TestCase("production-card-journal")]
        public void OriginalArtworkIsFullyContainedAndNeverSliced(string name)
        {
            string asset = "Assets/BeeKingdom/Playground/Resources/PremiumBeeIcons/"
                + name + ".png";
            TextureImporter importer = AssetImporter.GetAtPath(asset) as TextureImporter;
            Assert.That(importer, Is.Not.Null, asset);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Default),
                "Never import premium cards as a sliced sprite atlas");

            byte[] png = File.ReadAllBytes(Path.Combine(
                Application.dataPath, "BeeKingdom/Playground/Resources/PremiumBeeIcons",
                name + ".png"));
            Texture2D readable = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(ImageConversion.LoadImage(readable, png), Is.True);
                Assert.That(readable.width, Is.EqualTo(384));
                Assert.That(readable.height, Is.EqualTo(384));
                Color32[] pixels = readable.GetPixels32();
                int size = readable.width;
                const int margin = 24;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        if (x >= margin && x < size - margin &&
                            y >= margin && y < size - margin) continue;
                        Assert.That(pixels[y * size + x].a, Is.EqualTo(0),
                            name + " artwork reaches the PNG border at " + x + "," + y);
                    }
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(readable); }
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

                foreach (string name in new[] {
                    "production-card-status",
                    "production-card-capacity",
                    "production-card-journal" })
                {
                    Texture2D expected = Resources.Load<Texture2D>("PremiumBeeIcons/" + name);
                    Assert.That(icons.Count(icon => icon.texture == expected),
                        Is.EqualTo(1), "Expected exactly one complete icon for " + name);
                }
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
