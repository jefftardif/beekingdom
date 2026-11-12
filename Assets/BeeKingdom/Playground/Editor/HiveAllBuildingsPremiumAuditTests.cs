using System;
using System.IO;
using System.Linq;
using BeeKingdom.Buildings.Interaction;
using BeeKingdom.Localization;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeeKingdom.Playground.Editor
{
    public sealed class HiveAllBuildingsPremiumAuditTests
    {
        private static string Source(string relative) =>
            File.ReadAllText(Path.Combine(Application.dataPath,"BeeKingdom/Playground",relative));

        [Test]
        public void AllFourteenBuildingsHaveOwnExistingHdBanner()
        {
            Assert.That(BuildingCatalog.All.Count,Is.EqualTo(14));
            Assert.That(BuildingMappingTable.Count,Is.EqualTo(14));
            foreach (BuildingDefinition building in BuildingCatalog.All)
            {
                string expected="PremiumBeeReference/BuildingBanners/"+building.LegacyKey;
                string path=HiveMapPremiumBuildingWindow.BannerResourcePathForProof(building.LegacyKey);
                Assert.That(path,Is.EqualTo(expected),building.BuildingType);
                Texture2D banner=Resources.Load<Texture2D>(path);
                Assert.That(banner,Is.Not.Null,
                    building.BuildingType+" must not show another building's banner");
                Assert.That(banner.width,Is.GreaterThanOrEqualTo(256),building.BuildingType);
                Assert.That(banner.height,Is.GreaterThanOrEqualTo(256),building.BuildingType);
            }
        }

        [Test]
        public void UnknownBuildingCannotBorrowAllianceArt()
        {
            Assert.That(HiveMapPremiumBuildingWindow.BannerResourcePathForProof(
                "unknown_building"),Is.Empty);
        }

        [TestCase(BuildingTypes.HoneyReserve)]
        [TestCase(BuildingTypes.Warehouse)]
        [TestCase(BuildingTypes.Transformation)]
        public void ProductionInfoIsPremiumScrollableServerReadOnly(string buildingType)
        {
            GameObject go=new GameObject("Premium production audit",typeof(RectTransform));
            go.SetActive(false);
            var view=go.AddComponent<HiveMapPremiumBuildingWindow>();
            try
            {
                BuildingDefinition building=BuildingCatalog.GetByBuildingType(buildingType);
                view.ShowProductionInfo(building,building.DisplayName,
                    building.DisplayName,
                    new UnavailableHiveOfflineProductionPanelController(),
                    view.Hide,()=>{});
                Assert.That(view.ProductionInfoModeForProof,Is.True);
                Canvas canvas=go.GetComponentInChildren<Canvas>(true);
                Assert.That(canvas,Is.Not.Null);
                Assert.That(canvas.renderMode,Is.EqualTo(RenderMode.ScreenSpaceOverlay));
                Assert.That(canvas.GetComponent<GraphicRaycaster>(),Is.Not.Null);
                ScrollRect scroll=go.GetComponentInChildren<ScrollRect>(true);
                Assert.That(scroll,Is.Not.Null);
                Assert.That(scroll.verticalScrollbar,Is.Not.Null);
                Assert.That(scroll.horizontal,Is.False);
                Assert.That(go.GetComponentsInChildren<RawImage>(true).Any(i=>
                    i.texture==Resources.Load<Texture2D>(
                        HiveMapPremiumBuildingWindow.BannerResourcePathForProof(
                            building.LegacyKey))),Is.True,
                    "The exact building banner must appear in its header");
                TextMeshProUGUI[] labels=go.GetComponentsInChildren<TextMeshProUGUI>(true);
                Assert.That(labels.Any(l=>l.text.Contains("--")),Is.True,
                    "Unknown server amounts must never be fabricated");
                Button[] buttons=go.GetComponentsInChildren<Button>(true);
                Assert.That(buttons.Length,Is.GreaterThanOrEqualTo(2));
                Assert.That(buttons.Any(b=>b.GetComponentInChildren<TextMeshProUGUI>(true)
                    ?.text==BeeLocalization.Text("production.info.premium.action.refresh")
                    && !b.interactable),Is.True,
                    "Read-only session cannot refresh/collect as an official player");
                view.Hide();
                Assert.That(view.ProductionInfoModeForProof,Is.False);
            }
            finally
            {
                view.Hide();
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ProductionReadOnlyPopupUsesSharedModalAndNeverNativeLegacyGuiBox()
        {
            string source=Source("HiveMapProductionInfoBootstrap.cs");
            StringAssert.Contains("ShowProductionInfo(",source);
            StringAssert.Contains("IsAnotherOverlayBlockingProductionInfo()",source);
            StringAssert.Contains("CaptureClosingGesture()",source);
            StringAssert.DoesNotContain("GUILayout.BeginArea",source);
            StringAssert.DoesNotContain("GUI.Box(panel, string.Empty)",source);
            StringAssert.DoesNotContain("GUI.Label(target,",source);
            StringAssert.DoesNotContain("GUI.Button(target,",source);
            StringAssert.Contains("OfflineProductionControllerForHiveMap",source);
            string shared=Source("HiveMapOverlayInputGateBootstrap.cs");
            StringAssert.Contains("ignoreProductionInfo",shared);
            StringAssert.Contains("IsAnotherOverlayBlockingProductionInfo",shared);
        }

        [Test]
        public void ChampionHallUsesCanonicalArchivesHdArtwork()
        {
            string source=Source("HiveMapChampionHallBootstrap.cs");
            StringAssert.DoesNotContain("BuildingBanners/champion_hall",source);
            StringAssert.Contains("BuildingLegacyKeys.ArchivesHoneyfall",source);
            Assert.That(Resources.Load<Texture2D>(
                HiveMapPremiumBuildingWindow.BannerResourcePathForProof(
                    BuildingLegacyKeys.ArchivesHoneyfall)),Is.Not.Null);
        }

        [TestCase(390f,844f)]
        [TestCase(1080f,2400f)]
        [TestCase(1440f,900f)]
        [TestCase(1920f,1080f)]
        public void SharedHdBuildingViewportKeepsScrollbarOutsideText(float width,float height)
        {
            float scale=HiveMapPremiumBuildingWindow.PremiumCanvasScale(
                new Vector2(width,height));
            Rect safe=HiveMapPremiumBuildingWindow.PremiumSafeCanvasRect(
                new Vector2(width,height),new Rect(0,0,width,height),scale);
            Rect viewport=HiveMapPremiumBuildingWindow.ScrollViewportForProof(
                safe.width,safe.height);
            Rect track=HiveMapPremiumBuildingWindow.ScrollTrackForProof(
                safe.width,safe.height);
            Assert.That(viewport.width,Is.GreaterThan(100));
            Assert.That(viewport.height,Is.GreaterThan(100));
            Assert.That(track.xMin,Is.GreaterThanOrEqualTo(viewport.xMax));
            Assert.That(viewport.yMax,Is.LessThanOrEqualTo(safe.yMax+.01f));
        }

        [Test]
        public void AllProducerInfoLabelsExistInBothLanguages()
        {
            string old=BeeLocalization.CurrentLocale;
            try
            {
                foreach(string locale in new[]{"fr-CA","en-US"})
                {
                    BeeLocalization.SetLocale(locale);
                    foreach(string suffix in new[]{
                        "card.status","card.resources","card.management","subtitle",
                        "status.no_server","status.offline","status.error","status.loading",
                        "status.collecting","status.ready","resource","pending","hourly",
                        "reserve","capacity","note.no_server","note.offline","note.full",
                        "note.collect_on_building","action.refresh"})
                    {
                        string key="production.info.premium."+suffix;
                        string text=BeeLocalization.Text(key);
                        Assert.That(text,Is.Not.Empty,locale+"/"+key);
                        Assert.That(text,Is.Not.EqualTo(key),locale+"/"+key);
                        Assert.That(text.IndexOf((char)0xFFFD),Is.EqualTo(-1),locale+"/"+key);
                    }
                }
            }
            finally { BeeLocalization.SetLocale(old); }
        }
    }
}
