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
    public sealed class AllHiveBuildingsPremiumCoverageTests
    {
        private static readonly (string Type,string Legacy)[] Expected =
        {
            (BuildingTypes.Nursery,"nursery_cluster"),
            (BuildingTypes.HoneyReserve,"honey_storage"),
            (BuildingTypes.Barrack,"guard_post"),
            (BuildingTypes.Defense,"defense_growth"),
            (BuildingTypes.Genetics,"genetics_garden"),
            (BuildingTypes.Research,"research_node"),
            (BuildingTypes.Warehouse,"warehouse_cells"),
            (BuildingTypes.Transformation,"wax_workshop"),
            (BuildingTypes.Infirmary,"infirmary_grove"),
            (BuildingTypes.Academy,"academy_canopy"),
            (BuildingTypes.Bank,"hive_bank"),
            (BuildingTypes.RoyalPalace,"administration_core"),
            (BuildingTypes.AllianceCenter,"alliance_future_hall"),
            (BuildingTypes.ChampionHall,"archives_honeyfall")
        };
        private static string Source(string filename) =>
            File.ReadAllText(Path.Combine(Application.dataPath,
                "BeeKingdom/Playground",filename));

        [Test]
        public void AllFourteenBuildingsUseTheirOwnCanonicalHdBanner()
        {
            Assert.That(BuildingTypes.All.Length,Is.EqualTo(14));
            Assert.That(BuildingCatalog.All.Count,Is.EqualTo(14));
            Assert.That(Expected.Select(x=>x.Legacy).Distinct().Count(),
                Is.EqualTo(14));
            foreach(var expected in Expected)
            {
                Assert.That(BuildingMappingTable.ToLegacyKey(expected.Type),
                    Is.EqualTo(expected.Legacy),expected.Type);
                string path=HiveMapPremiumBuildingWindow.BannerResourcePathForProof(
                    expected.Legacy);
                Assert.That(path,Is.EqualTo(
                    "PremiumBeeReference/BuildingBanners/"+expected.Legacy));
                Assert.That(Resources.Load<Texture2D>(path),Is.Not.Null,
                    "Missing HD banner for "+expected.Type);
            }
        }

        [Test]
        public void UnknownAndMalformedBuildingBannerNamesCannotBeAccepted()
        {
            foreach(string invalid in new[]{null,"","future_unknown",
                        "alliance_future_hall/../bad","hive_bank.png"})
                Assert.That(
                    HiveMapPremiumBuildingWindow.BannerResourcePathForProof(invalid),
                    Is.Empty,invalid);
        }

        [TestCase(BuildingTypes.HoneyReserve)]
        [TestCase(BuildingTypes.Warehouse)]
        [TestCase(BuildingTypes.Transformation)]
        public void ProductionInformationIsFullscreenBilingualAndReadOnly(string type)
        {
            string oldLocale=BeeLocalization.CurrentLocale;
            foreach(string language in new[]{"fr-CA","en-US"})
            {
                GameObject host=new GameObject("Building Production Premium QA",
                    typeof(RectTransform));
                host.SetActive(false);
                try
                {
                    BeeLocalization.SetLocale(language);
                    var view=host.AddComponent<HiveMapPremiumBuildingWindow>();
                    BuildingDefinition building=BuildingCatalog.GetByBuildingType(type);
                    view.ShowProductionInfo(building,"Production","Production",
                        new UnavailableHiveOfflineProductionPanelController(),
                        view.Hide,()=>{});
                    Assert.That(view.ProductionInfoModeForProof,Is.True,
                        type+" "+language);
                    Canvas canvas=host.GetComponentInChildren<Canvas>(true);
                    Assert.That(canvas,Is.Not.Null,type+" "+language);
                    Assert.That(canvas.renderMode,Is.EqualTo(RenderMode.ScreenSpaceOverlay));
                    Assert.That(canvas.GetComponent<GraphicRaycaster>(),
                        Is.Not.Null);
                    ScrollRect scroll=host.GetComponentInChildren<ScrollRect>(true);
                    Assert.That(scroll,Is.Not.Null);
                    Assert.That(scroll.horizontal,Is.False);
                    Assert.That(scroll.verticalScrollbar,Is.Not.Null);
                    string[] captions=host.GetComponentsInChildren<TextMeshProUGUI>(true)
                        .Select(t=>t.text).ToArray();
                    string expectedStatus=language=="fr-CA"
                        ?"Session officielle requise":"Official session required";
                    Assert.That(captions.Any(x=>x.Contains(expectedStatus)),
                        Is.True,"No honest server status in "+type+" "+language);
                    Assert.That(captions.Any(x=>x.Contains("--")),
                        Is.True,"Unavailable stock must not be fabricated");
                    var controls=host.GetComponentsInChildren<Button>(true);
                    Assert.That(controls.Length,Is.GreaterThanOrEqualTo(2));
                    Assert.That(controls.Any(c=>!c.interactable),
                        Is.True,"Server refresh must be disabled while offline");
                    view.Hide();
                    Assert.That(view.ProductionInfoModeForProof,Is.False);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(host);
                }
            }
            BeeLocalization.SetLocale(oldLocale);
        }

        [Test]
        public void ProductionInformationNeverMutatesStockOrClaimsRewards()
        {
            string detail=Source("HiveMapPremiumBuildingWindow.ProductionInfo.cs");
            StringAssert.Contains("RenderProductionInfoCards(",detail);
            StringAssert.Contains("productionInfoController.Refresh()",detail);
            StringAssert.DoesNotContain("productionInfoController.Collect(",detail);
            StringAssert.DoesNotContain("StartPreview",detail);
            StringAssert.DoesNotContain("AddResource(",detail);

            string original=Source("HiveMapProductionBootstrap.cs");
            StringAssert.DoesNotContain("CollectManualProductionForExternalHost(",original);

            string info=Source("HiveMapProductionInfoBootstrap.cs");
            StringAssert.Contains("ShowProductionInfo(",info);
            StringAssert.Contains("CollectManualProductionForExternalHost(hotspotId);",info);
            StringAssert.Contains("ShouldHarvestOnFirstClickForProof",info);
            StringAssert.Contains("showDetailsAfterSuccessfulHarvest",info);
            StringAssert.Contains("CaptureClosingGesture(",info);
        }

        [Test]
        public void FullscreenInformationDoesNotBlockItselfAndKeepsOtherModalsProtected()
        {
            string gate=Source("HiveMapOverlayInputGateBootstrap.cs");
            StringAssert.Contains("IsAnotherOverlayBlockingProductionInfo",gate);
            StringAssert.Contains("ignoreProductionInfo: true",gate);
            StringAssert.Contains(
                "!ignoreProductionInfo && HiveMapProductionInfoBootstrap.OverlayOpenForExternalHost",
                gate);
            string info=Source("HiveMapProductionInfoBootstrap.cs");
            StringAssert.Contains("IsAnyOverlayBlocking()",info);
            StringAssert.Contains("IsAnotherOverlayBlockingProductionInfo()",info);
        }

        [TestCase("fr-CA")]
        [TestCase("en-US")]
        public void ProductionScreenHasTranslatedLabelsAndStates(string language)
        {
            string previous=BeeLocalization.CurrentLocale;
            try
            {
                BeeLocalization.SetLocale(language);
                string[] keys=
                {
                    "card.status","card.resources","card.management",
                    "subtitle","status.no_server","status.offline",
                    "status.error","status.loading","status.collecting",
                    "status.ready","resource","pending","hourly","reserve",
                    "capacity","note.no_server","note.offline",
                    "note.full","note.collect_on_building","action.refresh"
                };
                foreach(string key in keys)
                {
                    string full="production.info.premium."+key;
                    string text=BeeLocalization.Text(full);
                    Assert.That(text,Is.Not.Empty,full);
                    Assert.That(text,Is.Not.EqualTo(full),full);
                    Assert.That(text.IndexOf((char)0xFFFD),Is.EqualTo(-1),full);
                }
            }
            finally { BeeLocalization.SetLocale(previous); }
        }

        [Test]
        public void ExistingSpecializedFullscreenScreensAreStillRouted()
        {
            string standard=Source("HiveMapUnsupportedBuildingBootstrap.cs");
            foreach(string type in new[]{"Infirmary","Defense","Genetics","Academy"})
                StringAssert.Contains("BuildingTypes."+type,standard);
            StringAssert.Contains("HiveMapPremiumBuildingWindow",standard);
            foreach(string file in new[]{"HiveMapNurseryBootstrap.cs",
                 "HiveMapBankBootstrap.cs","HiveMapRoyalPalaceBootstrap.cs",
                 "HiveMapChampionHallBootstrap.cs",
                 "HiveMapAllianceBootstrap.cs","HiveMapBarrackBootstrap.cs",
                 "HiveMapResearchBootstrap.cs"})
                Assert.That(Source(file),Is.Not.Empty,file);
        }
    }
}
