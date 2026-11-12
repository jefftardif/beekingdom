using System;
using System.IO;
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
    public sealed class HiveBuildingClickAndHeaderRegressionTests
    {
        private static readonly BindingFlags Private = BindingFlags.Instance |
            BindingFlags.NonPublic;

        [SetUp]
        public void ResetTransientUiGestureForIsolatedTests()
        {
            // EditMode test cases share one Time.frameCount. The normal
            // close-gesture barrier intentionally blocks another tap for
            // that frame; each NUnit case must start at a fresh input frame.
            MethodInfo reset=typeof(HiveMapOverlayInputGateBootstrap)
                .GetMethod("ResetGestureBarrier",
                    BindingFlags.Static|BindingFlags.NonPublic);
            Assert.That(reset,Is.Not.Null);
            reset.Invoke(null,null);
        }
        [TestCase(BuildingTypes.Nursery)]
        [TestCase(BuildingTypes.Infirmary)]
        [TestCase(BuildingTypes.Genetics)]
        [TestCase(BuildingTypes.Defense)]
        public void PremiumHeaderUsesBarracksGoldArrowRatherThanWrappedRetour(string type)
        {
            var root = new GameObject("QA Premium Header", typeof(RectTransform));
            root.SetActive(false);
            try
            {
                var view = root.AddComponent<HiveMapPremiumBuildingWindow>();
                BuildingDefinition building = BuildingCatalog.GetByBuildingType(type);
                view.Show(building,building.DisplayName,building.DisplayName,
                    string.Empty,string.Empty,()=>{},()=>{});
                var buttons = root.GetComponentsInChildren<Button>(true);
                Button back = buttons.FirstOrDefault(b => b.name == "PremiumBack");
                Assert.That(back,Is.Not.Null,type+" missing premium return arrow");
                Assert.That(back.targetGraphic,Is.TypeOf<Image>(),
                    type+" must use Image, not RawImage");
                Assert.That(back.GetComponentInChildren<RawImage>(true),Is.Not.Null,
                    "Gold back arrow glyph must be rendered");
                Assert.That(back.GetComponentsInChildren<TMP_Text>(true).Length,
                    Is.EqualTo(0),"Back arrow must not contain wrapped Retour text");
                Assert.That(back.GetComponent<RectTransform>().rect.width,
                    Is.GreaterThanOrEqualTo(52f));
                view.Hide();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [TestCase(BuildingTypes.HoneyReserve)]
        [TestCase(BuildingTypes.Warehouse)]
        [TestCase(BuildingTypes.Transformation)]
        public void ProducerClickRuleHarvestsOnceThenShowsDetails(string type)
        {
            BuildingDefinition building=BuildingCatalog.GetByBuildingType(type);
            Assert.That(building,Is.Not.Null);
            Assert.That(HiveMapProductionInfoBootstrap.ShouldHarvestOnFirstClickForProof(
                collectable:true,detailsPending:false),Is.True);
            Assert.That(HiveMapProductionInfoBootstrap.ShouldHarvestOnFirstClickForProof(
                collectable:true,detailsPending:true),Is.False,
                "The second click must show details even if the server still reports stock");
            Assert.That(HiveMapProductionInfoBootstrap.ShouldHarvestOnFirstClickForProof(
                collectable:false,detailsPending:false),Is.False);
        }
        [Test]
        public void NewInformationPanelCannotInventCreditsOrBreakExistingCollection()
        {
            string path=Path.Combine(Application.dataPath,
                "BeeKingdom/Playground/HiveMapProductionInfoBootstrap.cs");
            string bootstrap=File.ReadAllText(path);
            StringAssert.Contains("controller.Selection.BuildingClicked += OnBuildingClicked;",bootstrap);
            StringAssert.Contains("subscribedController.Selection.BuildingClicked -= OnBuildingClicked;",bootstrap);
            StringAssert.Contains("OpenPremiumInfo(type);",bootstrap);
            string original=File.ReadAllText(Path.Combine(Application.dataPath,
                "BeeKingdom/Playground/HiveMapProductionBootstrap.cs"));
            StringAssert.DoesNotContain("CollectManualProductionForExternalHost(",original);
            StringAssert.DoesNotContain("BuildingClicked += OnBuildingClicked;",original);
            StringAssert.Contains("CollectManualProductionForExternalHost(hotspotId);",bootstrap);
            StringAssert.Contains("showDetailsAfterSuccessfulHarvest.Add(type);",bootstrap);
            StringAssert.Contains("showDetailsAfterSuccessfulHarvest.Remove(type);",bootstrap);
            StringAssert.Contains("lastProducerClickFrame == Time.frameCount",bootstrap);
            StringAssert.DoesNotContain("GUI.Label(target,",bootstrap);
            string ui=File.ReadAllText(Path.Combine(Application.dataPath,
                "BeeKingdom/Playground/HiveMapPremiumBuildingWindow.ProductionInfo.cs"));
            StringAssert.DoesNotContain("AddResource(",ui);
            StringAssert.DoesNotContain("CollectManualProductionForExternalHost(",ui);
        }

        [TestCase("fr-CA")]
        [TestCase("en-US")]
        public void NurseryAndDefenseDoNotShowBackendCodesInPlayerStatus(string locale)
        {
            string old=BeeLocalization.CurrentLocale;
            try
            {
                BeeLocalization.SetLocale(locale);
                string nursery=BeeLocalization.Text("nursery.status.sync_failed");
                string hint=BeeLocalization.Text("nursery.status.retry_hint");
                string defense=BeeLocalization.Text("defense.premium.status.awaiting");
                string genetics=BeeLocalization.Text("genetics.premium.awaiting_level");
                foreach(string message in new[]{nursery,hint,defense,genetics})
                {
                    Assert.That(message,Is.Not.Empty);
                    Assert.That(message,Does.Not.Contain("server_unavailable"));
                    Assert.That(message,Does.Not.Contain("0x"));
                    Assert.That(message.IndexOf((char)0xFFFD),Is.EqualTo(-1));
                }
                string source=File.ReadAllText(Path.Combine(Application.dataPath,
                    "BeeKingdom/Playground/HiveMapPremiumBuildingWindow.cs"));
                StringAssert.DoesNotContain("model.ErrorCode",source,
                    "UI must not expose raw backend error codes");
                StringAssert.Contains("PremiumBackButton(header, closeAction);",source);
            }
            finally { BeeLocalization.SetLocale(old); }
        }
    }
}
