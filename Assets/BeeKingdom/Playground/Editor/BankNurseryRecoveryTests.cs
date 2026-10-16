using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BeeKingdom.Buildings.Interaction;
using BeeKingdom.Networking;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeeKingdom.Playground.Editor
{
    public sealed class BankNurseryRecoveryTests
    {
        private const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic;
        private static readonly Type Presenter = typeof(HiveViewProductUiPresenter);
        private static string Source(string relative) => File.ReadAllText(Path.Combine(Application.dataPath, relative));

        [Test]
        public void BankBuilding_HasDedicatedRouteAndIsNotAnUnsupportedPlaceholder()
        {
            MethodInfo method = typeof(HiveMapUnsupportedBuildingBootstrap).GetMethod("IsTracked", Static);
            Assert.That((bool)method.Invoke(null, new object[] { BuildingTypes.Bank }), Is.False);
            string host = Source("BeeKingdom/Playground/HiveMapBankBootstrap.cs");
            StringAssert.Contains("BuildingTypes.Bank", host);
            StringAssert.Contains("OpenBankOverlayForExternalHost()", host);
            StringAssert.Contains("DrawBankOverlayForExternalHost", host);
            StringAssert.Contains("HiveMapBankBootstrap.InitializeForScene(scene)", Source("BeeKingdom/Playground/HiveMapRuntimeBootstrapInitializer.cs"));
        }

        [Test]
        public void BankOverlay_BlocksWorldDrawingAndInputAndClearsOnClose()
        {
            FieldInfo open = Presenter.GetField("bankOverlayOpenForExternalHost", Static);
            bool previous = (bool)open.GetValue(null);
            try
            {
                open.SetValue(null, true);
                Assert.That(HiveMapOverlayInputGateBootstrap.IsAnyOverlayBlocking(), Is.True);
                Assert.That(HiveViewProductUiPresenter.AnyModalOpenForExternalHost, Is.True);
                var regions = new List<Rect>();
                HiveMapUiOcclusion.GetWorldPresentationVisibleRegions(regions);
                Assert.That(regions, Is.Empty);
                HiveViewProductUiPresenter.CloseBankOverlayForExternalHost();
                Assert.That(HiveViewProductUiPresenter.BankOverlayOpenForExternalHost, Is.False);
            }
            finally { open.SetValue(null, previous); }
        }

        [Test]
        public void BankClose_RemovesBalancesAndInvalidatesLateRequests()
        {
            FieldInfo snapshot = Presenter.GetField("bankRoyalReserveSnapshot", Static);
            FieldInfo version = Presenter.GetField("bankRequestVersion", Static);
            var previous = snapshot.GetValue(null);
            try
            {
                snapshot.SetValue(null, new RemoteRoyalReserveSnapshot());
                int before = (int)version.GetValue(null);
                HiveViewProductUiPresenter.CloseBankOverlayForExternalHost();
                Assert.That(snapshot.GetValue(null), Is.Null);
                Assert.That((int)version.GetValue(null), Is.Not.EqualTo(before));
            }
            finally { snapshot.SetValue(null, previous); }
        }

        [Test]
        public void BankRetainsApprovedElevenProgressionMilestones()
        {
            var milestones = (IEnumerable)Presenter.GetField("BankMilestones", Static).GetValue(null);
            var levels = milestones.Cast<object>().Select(m => (int)m.GetType().GetField("Level").GetValue(m)).ToArray();
            CollectionAssert.AreEqual(new[] { 1, 3, 5, 10, 12, 15, 20, 23, 25, 29, 30 }, levels);
        }

        [Test]
        public void BankOpenAndRead_DoNotInvokeTransfers()
        {
            string source = Source("BeeKingdom/Playground/HiveViewProductUiPresenter.Bank.cs");
            int start = source.IndexOf("public static void OpenBankOverlayForExternalHost()", StringComparison.Ordinal);
            int end = source.IndexOf("public static void CloseBankOverlayForExternalHost()", start, StringComparison.Ordinal);
            string open = source.Substring(start, end-start);
            StringAssert.DoesNotContain("DepositAsync", open);
            StringAssert.DoesNotContain("WithdrawAsync", open);
            StringAssert.Contains("BankRequestIsCurrent", source);
            StringAssert.DoesNotContain("Where(char.IsDigit)", source);
        }

        [TestCase(1920, 1080, 1.2f)]
        [TestCase(1600, 900, 1f)]
        [TestCase(1170, 2532, 3f)]
        public void NurseryDensity_MatchesApprovedActivitiesReference(int width, int height, float expected)
        {
            Assert.That(HiveMapPremiumBuildingWindow.PremiumCanvasScale(new Vector2(width,height)), Is.EqualTo(expected).Within(.001f));
        }

        [TestCase("panel-grain")]
        [TestCase("button-fill")]
        [TestCase("cave-background")]
        public void NurseryMaterials_AreCanonicalMaterialsNotStretchedCityArtwork(string id)
        {
            var method=typeof(HiveMapPremiumBuildingWindow).GetMethod("PremiumMaterial",Static);
            var texture=(Texture)method.Invoke(null,new object[]{id});
            Assert.That(texture, Is.SameAs(Presenter.GetMethod("BuildingWindowMaterial",Static).Invoke(null,new object[]{id})));
            Assert.That(texture, Is.Not.SameAs(Resources.Load<Texture2D>("PremiumBeeReference/bg_beehive")));
            Assert.That(texture, Is.Not.SameAs(Resources.Load<Texture2D>("PremiumBeeReference/background_hive")));
        }

        [Test]
        public void NurseryServerError_IsVisibleWithoutInventedFullGauges()
        {
            var host=new GameObject("Nursery recovery error QA",typeof(RectTransform));
            host.SetActive(false);
            try
            {
                var fake=new CareFixture { Model=HiveBroodVitalityPresentation.Project(
                    HiveBroodVitalityScreenState.Error,null,TimeSpan.Zero,true,"server_unavailable") };
                var window=host.AddComponent<HiveMapPremiumBuildingWindow>();
                window.BindNurseryController(fake);
                window.Show(BuildingCatalog.GetByBuildingType(BuildingTypes.Nursery),"Nurserie","Nursery","","",window.Hide,()=>{});
                string text=string.Join("|",host.GetComponentsInChildren<TextMeshProUGUI>(true).Select(x=>x.text));
                StringAssert.Contains("server_unavailable",text);
                StringAssert.DoesNotContain("Chargement...",text);
                StringAssert.DoesNotContain("Loading...",text);
                var gauges=host.GetComponentsInChildren<Image>(true).Where(x=>x.type==Image.Type.Filled).ToArray();
                Assert.That(gauges.Length,Is.EqualTo(3));
                foreach(var gauge in gauges) { Assert.That(gauge.sprite,Is.Not.Null);Assert.That(gauge.fillAmount,Is.Zero); }
                Assert.That(fake.StartCalls,Is.Zero);
                window.Hide();
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }

        private sealed class CareFixture : IHiveBroodVitalityPanelController
        {
            public HiveBroodVitalityScreenModel Model {get;set;}
            public bool IsConfigured=>true;
            public bool IsBusy=>false;
            public TimeSpan Elapsed=>TimeSpan.Zero;
            public int StartCalls;
            public void Refresh(){}
            public void Start(string type){StartCalls++;}
            public void Complete(){}
            public void Retry(){}
        }
    }
}