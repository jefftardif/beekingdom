using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using BeeKingdom.Buildings.Interaction;
using BeeKingdom.Networking;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeeKingdom.Playground.Editor
{
    public sealed class PremiumBuildingPolishTests
    {
        private static string Source(string name) => File.ReadAllText(
            Path.Combine(Application.dataPath, "BeeKingdom/Playground", name));
        private static string Method(string name)
        {
            string source = Source("HiveViewProductUiPresenter.cs");
            int start = source.IndexOf("void " + name + "(", StringComparison.Ordinal);
            if (start < 0) throw new InvalidOperationException(name);
            int next = source.IndexOf("\n        private static", start + name.Length + 9, StringComparison.Ordinal);
            return source.Substring(start, next < 0 ? source.Length-start : next-start);
        }

        [TestCase(320, true)]
        [TestCase(390, true)]
        [TestCase(844, false)]
        [TestCase(1600, false)]
        [TestCase(1920, false)]
        public void UpgradeAction_IsInUpperRightWithNoBackButtonOverlap(int screenWidth, bool compact)
        {
            Rect upgrade = HiveViewProductUiPresenter.PremiumBuildingUpgradeRectForProof(screenWidth, compact);
            Rect back = new Rect(16, 14, 78, 48);
            Assert.That(upgrade.Overlaps(back), Is.False);
            Assert.That(upgrade.xMax, Is.LessThanOrEqualTo(screenWidth));
            Assert.That(upgrade.y, Is.LessThanOrEqualTo(14));
            Assert.That(upgrade.height, Is.GreaterThanOrEqualTo(48));
        }

        [Test]
        public void AllianceUpgrade_UsesSharedHeaderAndNeverBottomBanner()
        {
            string source = Method("DrawAllianceUpgradeButtonForExternalHost");
            StringAssert.Contains("DrawPremiumBuildingUpgradeHeader",source);
            StringAssert.Contains("allianceMemberProfileOpen",source);
            StringAssert.DoesNotContain("Screen.height - 58f",source);
            StringAssert.DoesNotContain("DrawPreviewActionButton",source);
        }

        [Test]
        public void CaserneAndBank_UseSharedUpgradeWithNoManualHeaderRefresh()
        {
            string source = Method("DrawBarrackTopBar");
            StringAssert.Contains("DrawPremiumBuildingUpgradeHeader",source);
            StringAssert.DoesNotContain("\"Actualiser\"",source);
            string bank = Source("HiveViewProductUiPresenter.Bank.cs");
            StringAssert.Contains("DrawPremiumBuildingUpgradeHeader(\"hive_bank\"", bank);
            StringAssert.DoesNotContain("Rect refreshRect =", bank);
            StringAssert.Contains("TickBankOverlayForExternalHost",bank);
        }

        [Test]
        public void AllianceChat_PreservesServerMessagesWithReadableAvatarsAndComposer()
        {
            string source = Method("DrawAllianceChatRealBody");
            StringAssert.Contains("LivingHiveChatRuntime.Snapshot",source);
            StringAssert.Contains("message.VisibleBody",source);
            StringAssert.Contains("DrawAllianceMessageAvatar(avatar, sender)",source);
            StringAssert.Contains("compact ? 14 : 17",source);
            StringAssert.Contains("compact ? 15 : 18",source);
            StringAssert.Contains("DrawFlatRoundedRect(bubble",source);
            StringAssert.Contains("message.CreatedAt.ToLocalTime()",source);
            StringAssert.Contains("SendAllianceChatMessage(toSend)",source);
            StringAssert.DoesNotContain("compact ? 10 : 11",source);
        }

        [Test]
        public void AllFullscreenBuildingControllersRefreshWithoutHeaderButton()
        {
            string alliance = Source("HiveMapAllianceBootstrap.cs");
            string bank = Source("HiveMapBankBootstrap.cs");
            string window = Source("HiveMapPremiumBuildingWindow.cs");
            StringAssert.Contains("RefreshQuietly()",alliance);
            StringAssert.Contains("Time.unscaledTime + 18f",alliance);
            StringAssert.Contains("TickBankOverlayForExternalHost()",bank);
            StringAssert.Contains("Time.unscaledTime + 15f",window);
            StringAssert.Contains("RefreshInfirmary()",window);
            StringAssert.Contains("RefreshNursery()",window);
        }

        [Test]
        public void BankRoadmap_FontsAndSpacingAreLegible()
        {
            string text=Source("HiveViewProductUiPresenter.Bank.cs");
            StringAssert.Contains("float rowHeight = compact ? 104f : 100f",text);
            StringAssert.Contains("fontSize = compact ? 14 : 16",text);
            StringAssert.Contains("fontSize = compact ? 16 : 18",text);
            StringAssert.DoesNotContain("fontSize = compact ? 8 : 9",text);
            StringAssert.DoesNotContain("fontSize = compact ? 9 : 11",text);
        }

        [TestCase(BuildingTypes.Infirmary)]
        [TestCase(BuildingTypes.Nursery)]
        public void CareBuilding_UpgradeInHeaderNotBottomAndAutomaticStatus(string type)
        {
            GameObject root = new GameObject("Polish UI QA", typeof(RectTransform));
            root.SetActive(false);
            try
            {
                var view = root.AddComponent<HiveMapPremiumBuildingWindow>();
                view.Show(BuildingCatalog.GetByBuildingType(type),
                    type == BuildingTypes.Infirmary ? "Infirmerie" : "Nurserie",
                    "Building", "", "", view.Hide, () => {});
                var button = root.GetComponentsInChildren<Button>(true)
                    .Single(x => x.GetComponentInChildren<TextMeshProUGUI>(true)?.text == "Améliorer"
                        || x.GetComponentInChildren<TextMeshProUGUI>(true)?.text == "Upgrade");
                Assert.That(button.transform.parent.name, Is.EqualTo("Header"));
                var noManualRefresh=root.GetComponentsInChildren<Button>(true)
                    .Where(x=>x.GetComponentInChildren<TextMeshProUGUI>(true)?.text == "Actualiser"
                        || x.GetComponentInChildren<TextMeshProUGUI>(true)?.text == "Refresh");
                Assert.That(noManualRefresh,Is.Empty);
                var texts = root.GetComponentsInChildren<TextMeshProUGUI>(true)
                    .Select(x=>x.text).ToArray();
                Assert.That(texts.Any(x=>x.Contains("automatique")
                    || x.Contains("automatically")),Is.True);
                Assert.That(root.GetComponentInChildren<ScrollRect>(true).verticalScrollbar, Is.Not.Null);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [TestCase(BuildingTypes.Infirmary)]
        [TestCase(BuildingTypes.Nursery)]
        public void PremiumUpgradeHeader_UsesSameNativeGoldIconAsOtherBuildings(string buildingType)
        {
            var host = new GameObject("Shared upgrade emblem QA", typeof(RectTransform));
            host.SetActive(false);
            try
            {
                var view = host.AddComponent<HiveMapPremiumBuildingWindow>();
                view.Show(BuildingCatalog.GetByBuildingType(buildingType),
                    "Bâtiment", "Building", "", "", view.Hide, () => {});
                Texture2D sourceIcon = Resources.Load<Texture2D>("PremiumBeeIcons/upgrade");
                Assert.That(sourceIcon, Is.Not.Null);
                RawImage image = host.GetComponentsInChildren<RawImage>(true)
                    .Single(x => x.gameObject.name == "UpgradeIcon");
                Assert.That(image.texture, Is.SameAs(sourceIcon));
                Assert.That(image.color, Is.EqualTo(Color.white));
                Assert.That(image.raycastTarget, Is.False);
                RawImage socket = host.GetComponentsInChildren<RawImage>(true)
                    .Single(x => x.gameObject.name == "UpgradeIconSocket");
                Assert.That(socket.color.r, Is.LessThan(.12f));
                Assert.That(socket.raycastTarget, Is.False);
                string shared = Source("HiveViewProductUiPresenter.BuildingChrome.cs");
                StringAssert.Contains("PremiumBuildingUpgradeArtwork.Texture", shared);
                StringAssert.Contains("new Rect(iconSocket.x + 2f", shared);
                StringAssert.DoesNotContain("DrawGameIcon(new Rect(button.x + 9f", shared);
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }
        [Test]
        public void NurseryError_ActionsAreDisabledAndClearlyMuted()
        {
            GameObject root=new GameObject("Nursery disabled visual QA",typeof(RectTransform));
            root.SetActive(false);
            try
            {
                var fake=new CareFixture
                {
                    Model=HiveBroodVitalityPresentation.Project(
                        HiveBroodVitalityScreenState.Error,null,TimeSpan.Zero,true,
                        "server_unavailable")
                };
                var view=root.AddComponent<HiveMapPremiumBuildingWindow>();
                view.BindNurseryController(fake);
                view.Show(BuildingCatalog.GetByBuildingType(BuildingTypes.Nursery),
                    "Nurserie","Nursery","","",view.Hide,()=>{});
                var blocked=root.GetComponentsInChildren<Button>(true)
                    .Where(b=> { string s=b.GetComponentInChildren<TextMeshProUGUI>(true)?.text;
                        return s != null && (s.StartsWith("Nourrir (")||s.StartsWith("Stabiliser ("));})
                    .ToArray();
                Assert.That(blocked.Length,Is.EqualTo(2));
                foreach(var btn in blocked)
                {
                    Assert.That(btn.interactable,Is.False);
                    Assert.That(btn.targetGraphic.color.r,Is.LessThan(.30f));
                    Assert.That(btn.GetComponentInChildren<TextMeshProUGUI>(true).color.a,Is.GreaterThan(.95f));
                }
                Assert.That(fake.StartCalls,Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
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