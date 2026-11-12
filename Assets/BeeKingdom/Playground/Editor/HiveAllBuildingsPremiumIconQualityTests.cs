using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BeeKingdom.Buildings.Interaction;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BeeKingdom.Playground.Editor
{
    public sealed class HiveAllBuildingsPremiumIconQualityTests
    {
        private static readonly string[] RemasteredLegacy = {
            "guard", "guard-bee", "worker", "worker-bee", "queen-bee",
            "inventory", "production", "royal-jelly", "quests",
            "trophy", "propolis", "drone", "shop", "settings"
        };

        private static readonly string[] AcademySections = {
            "state", "construction", "palace", "mission",
            "tactics", "logistics", "exploration", "mentorship",
            "training", "research", "records"
        };

        [Test]
        public void EveryOfficialBuildingHasARealPremiumIconAndCorrectBanner()
        {
            Assert.That(BuildingCatalog.All.Count, Is.EqualTo(14));
            foreach (BuildingDefinition building in BuildingCatalog.All)
            {
                var icon = Resources.Load<Texture2D>(
                    "PremiumBeeIcons/" + building.IconId);
                Assert.That(icon, Is.Not.Null,
                    building.BuildingType + " missing original icon " + building.IconId);
                Assert.That(icon.width, Is.GreaterThanOrEqualTo(192), building.BuildingType);
                Assert.That(icon.height, Is.GreaterThanOrEqualTo(192), building.BuildingType);
                var banner = Resources.Load<Texture2D>(
                    HiveMapPremiumBuildingWindow.BannerResourcePathForProof(
                        building.LegacyKey));
                Assert.That(banner, Is.Not.Null, building.BuildingType);
            }
        }

        [Test]
        public void AcademyElevenCardsHaveCompleteUnclippedPremiumHeaders()
        {
            var root = new GameObject("Academy premium icon visual QA",
                typeof(RectTransform));
            root.SetActive(false);
            try
            {
                var view = root.AddComponent<HiveMapPremiumBuildingWindow>();
                view.Show(BuildingCatalog.GetByBuildingType(BuildingTypes.Academy),
                    "Académie", "Academy", "", "", view.Hide, () => { });
                RawImage[] icons = root.GetComponentsInChildren<RawImage>(true)
                    .Where(x => x.name == "PremiumCardIcon").ToArray();
                Assert.That(icons.Length, Is.EqualTo(AcademySections.Length));
                foreach (string section in AcademySections)
                {
                    string name = "academy-card-" + section;
                    Texture2D expected = Resources.Load<Texture2D>(
                        "PremiumBeeIcons/" + name);
                    Assert.That(expected, Is.Not.Null, name);
                    RawImage icon = icons.Single(x => x.texture == expected);
                    Assert.That(icon.uvRect, Is.EqualTo(new Rect(0, 0, 1, 1)), name);
                    Assert.That(icon.raycastTarget, Is.False, name);
                    Rect rect = icon.rectTransform.rect;
                    Assert.That(rect.width, Is.InRange(99.99f, 100.01f), name);
                    Assert.That(rect.height, Is.InRange(99.99f, 100.01f), name);
                    float top = -icon.rectTransform.anchoredPosition.y;
                    Assert.That(top, Is.GreaterThanOrEqualTo(9f), name);
                    Assert.That(top + rect.height, Is.LessThan(126f), name);
                    RectTransform card = icon.transform.parent as RectTransform;
                    Assert.That(card, Is.Not.Null, name);
                    RectTransform separator = card.Find("Separator") as RectTransform;
                    Assert.That(separator, Is.Not.Null, name);
                    Assert.That(-separator.anchoredPosition.y,
                        Is.EqualTo(128f).Within(.01f), name);
                }
                // The Academy still has Back, Construction, two navigation actions
                // and four real server-gated course controls.
                Assert.That(root.GetComponentsInChildren<Button>(true).Length,
                    Is.EqualTo(8));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void CorrectedArtworkHasSafeTransparentMarginsAndStableUniqueImports()
        {
            var keys = RemasteredLegacy.Concat(new[] {
                "production-card-status", "production-card-capacity",
                "production-card-journal"
            }).Concat(AcademySections.Select(x => "academy-card-" + x)).ToArray();
            var guids = new HashSet<string>();
            foreach (string key in keys)
            {
                string assetPath =
                    "Assets/BeeKingdom/Playground/Resources/PremiumBeeIcons/"
                    + key + ".png";
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                Assert.That(texture, Is.Not.Null, key);
                Assert.That(texture.width, Is.EqualTo(384), key);
                Assert.That(texture.height, Is.EqualTo(384), key);
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                Assert.That(importer, Is.Not.Null, key);
                var expectedType = key.StartsWith("academy-card-", StringComparison.Ordinal)
                    || key.StartsWith("production-card-", StringComparison.Ordinal)
                    ? TextureImporterType.Default : TextureImporterType.Sprite;
                Assert.That(importer.textureType,
                    Is.EqualTo(expectedType), key);
                Assert.That(guids.Add(AssetDatabase.AssetPathToGUID(assetPath)),
                    Is.True, "Duplicate Unity asset GUID " + key);

                byte[] bytes = File.ReadAllBytes(Path.Combine(
                    Application.dataPath,
                    "BeeKingdom/Playground/Resources/PremiumBeeIcons",
                    key + ".png"));
                var readable = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    Assert.That(ImageConversion.LoadImage(readable, bytes),
                        Is.True, key);
                    Assert.That(readable.width, Is.EqualTo(384), key);
                    Assert.That(readable.height, Is.EqualTo(384), key);
                    Color32[] pixels = readable.GetPixels32();
                    for (int y = 0; y < 384; y++)
                        for (int x = 0; x < 384; x++)
                            if (x < 24 || x >= 360 || y < 24 || y >= 360)
                                Assert.That(pixels[y * 384 + x].a,
                                    Is.EqualTo(0),
                                    key + " artwork reaches outer edge at "
                                    + x + "," + y);
                }
                finally { UnityEngine.Object.DestroyImmediate(readable); }
            }
        }
    }
}
