using System;
using System.IO;
using System.Reflection;
using BeeKingdom.Buildings.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BeeKingdom.Playground.Editor
{
    public sealed class HiveProducerModalPersistenceAndBankReturnTests
    {
        const BindingFlags PI=BindingFlags.Instance|BindingFlags.NonPublic;
        const BindingFlags PS=BindingFlags.Static|BindingFlags.NonPublic;

        [Test]
        public void BankReturnUsesTheSharedGoldArrow()
        {
            string source=File.ReadAllText(Path.Combine(Application.dataPath,
                "BeeKingdom/Playground/HiveViewProductUiPresenter.Bank.cs"));
            StringAssert.Contains("WorldMapHudLayout.DrawChatReturnButton",source);
            StringAssert.DoesNotContain(
                "DrawActivitiesButton(new Rect(16f, 14f, 78f, 48f)",source);
            StringAssert.Contains("bankSelectedView, \"reserve\"",source);
            StringAssert.Contains("CloseBankOverlayForExternalHost()",source);
        }

        [TestCase(BuildingTypes.HoneyReserve)]
        [TestCase(BuildingTypes.Warehouse)]
        [TestCase(BuildingTypes.Transformation)]
        public void ProducerModalPersistsWhenOtherModalTemporarilyReportsOpen(
            string type)
        {
            Type klass=typeof(HiveMapProductionInfoBootstrap);
            PropertyInfo open=klass.GetProperty("OverlayOpenForExternalHost");
            FieldInfo field=klass.GetField("premiumWindow",PI);
            MethodInfo update=klass.GetMethod("Update",PI);
            FieldInfo bank=typeof(HiveViewProductUiPresenter)
                .GetField("bankOverlayOpenForExternalHost",PS);
            Assert.That(open,Is.Not.Null);
            Assert.That(field,Is.Not.Null);
            Assert.That(update,Is.Not.Null);
            Assert.That(bank,Is.Not.Null);
            bool originalOpen=(bool)open.GetValue(null);
            bool originalBank=(bool)bank.GetValue(null);
            GameObject host=new GameObject("Producer Persistence QA");
            host.SetActive(false);
            try
            {
                HiveViewProductUiPresenter.SetSplashAuthGateForProof("hive");
                var bootstrap=host.AddComponent<HiveMapProductionInfoBootstrap>();
                GameObject viewRoot=new GameObject("Producer Window QA");
                viewRoot.transform.SetParent(host.transform,false);
                var view=viewRoot.AddComponent<HiveMapPremiumBuildingWindow>();
                field.SetValue(bootstrap,view);
                view.ShowProductionInfo(BuildingCatalog.GetByBuildingType(type),
                    "Production","Production",
                    new UnavailableHiveOfflineProductionPanelController(),
                    ()=>{},()=>{});
                open.GetSetMethod(true).Invoke(null,new object[]{true});
                bank.SetValue(null,true);
                update.Invoke(bootstrap,null);
                Assert.That((bool)open.GetValue(null),Is.True);
                Assert.That(view.IsOpen,Is.True);
                Assert.That(view.ProductionInfoModeForProof,Is.True);
                Canvas canvas=viewRoot.GetComponentInChildren<Canvas>(true);
                Assert.That(canvas,Is.Not.Null);
                Assert.That(canvas.gameObject.activeSelf,Is.True);
                Assert.That(canvas.sortingOrder,Is.GreaterThanOrEqualTo(400));
            }
            finally
            {
                open.GetSetMethod(true).Invoke(null,new object[]{originalOpen});
                bank.SetValue(null,originalBank);
                HiveViewProductUiPresenter.SetSplashAuthGateForProof("splash");
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void ModalCannotAutoCloseDuringUpdateButExplicitBackStillWorks()
        {
            string text=File.ReadAllText(Path.Combine(Application.dataPath,
                "BeeKingdom/Playground/HiveMapProductionInfoBootstrap.cs"));
            int first=text.IndexOf("private void Update()",StringComparison.Ordinal);
            int last=text.IndexOf("private static bool IsProducerBuildingType",
                first,StringComparison.Ordinal);
            Assert.That(first,Is.GreaterThanOrEqualTo(0));
            Assert.That(last,Is.GreaterThan(first));
            StringAssert.DoesNotContain("ClosePremiumInfo();",
                text.Substring(first,last-first));
            StringAssert.Contains("premiumWindow.SetSuppressed(false);",text);
            StringAssert.Contains("private void ClosePremiumInfo()",text);
            StringAssert.Contains("premiumWindow.Hide()",text);
        }
    }
}
