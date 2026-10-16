using System;
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
    public sealed class InfirmaryEntryRecoveryTests
    {
        private const BindingFlags Instance = BindingFlags.NonPublic | BindingFlags.Instance;
        private static void Call(object host, string name, params object[] args) =>
            host.GetType().GetMethod(name, Instance).Invoke(host, args);
        private static string Text(GameObject root) => string.Join("|", root.GetComponentsInChildren<TextMeshProUGUI>(true).Select(x => x.text));

        [Test]
        public void ActualBuildingClick_OpensRetainedRecoveryWard_NotLegacyPlaceholder()
        {
            var root = new GameObject("Infirmary entry QA"); root.SetActive(false);
            try
            {
                var host = root.AddComponent<HiveMapUnsupportedBuildingBootstrap>();
                Call(host, "OnBuildingClicked", BuildingCatalog.GetByBuildingType(BuildingTypes.Infirmary));
                var view = root.GetComponentInChildren<HiveMapPremiumBuildingWindow>(true);
                Assert.That(view, Is.Not.Null);
                Assert.That(view.IsOpen && view.InfirmaryActiveForProof, Is.True);
                Assert.That(HiveMapUnsupportedBuildingBootstrap.OverlayOpenForExternalHost, Is.True);
                var controller = typeof(HiveMapPremiumBuildingWindow).GetField("infirmaryController", Instance).GetValue(view);
                Assert.That(controller, Is.SameAs(MobileAccountSessionRuntimeBootstrap.CombatPatrolControllerForHiveMap));
                Assert.That(root.GetComponentsInChildren<Canvas>(true).Length, Is.EqualTo(1));
                Assert.That(root.GetComponentInChildren<GraphicRaycaster>(true), Is.Not.Null);
                Assert.That(root.GetComponentInChildren<ScrollRect>(true).verticalScrollbar, Is.Not.Null);
                Assert.That(Text(root), Does.Not.Contain("Fonctionnalité en préparation"));
                Assert.That(HiveMapOverlayInputGateBootstrap.IsAnyOverlayBlocking(), Is.True);
                var visible = new List<Rect>(); HiveMapUiOcclusion.GetWorldPresentationVisibleRegions(visible);
                Assert.That(visible, Is.Empty);
                Call(host, "CloseInfirmaryWindow");
                Assert.That(view.IsOpen, Is.False);
                Assert.That(HiveMapUnsupportedBuildingBootstrap.OverlayOpenForExternalHost, Is.False);
                Call(host, "OnBuildingClicked", BuildingCatalog.GetByBuildingType(BuildingTypes.Infirmary));
                Assert.That(root.GetComponentsInChildren<HiveMapPremiumBuildingWindow>(true).Length, Is.EqualTo(1));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [TestCase(BuildingTypes.Genetics)]
        [TestCase(BuildingTypes.Academy)]
        [TestCase(BuildingTypes.Defense)]
        [TestCase(BuildingTypes.Bank)]
        public void OtherBuildingClicks_DoNotOpenTheInfirmary(string building)
        {
            var root = new GameObject("Other building route QA"); root.SetActive(false);
            try
            {
                var host = root.AddComponent<HiveMapUnsupportedBuildingBootstrap>();
                Call(host, "OnBuildingClicked", BuildingCatalog.GetByBuildingType(building));
                Assert.That(root.GetComponentInChildren<HiveMapPremiumBuildingWindow>(true), Is.Null);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void ReturnButton_ClosesWardAndReleasesItsOverlayFlag()
        {
            var root = new GameObject("Infirmary back QA"); root.SetActive(false);
            try
            {
                var host = root.AddComponent<HiveMapUnsupportedBuildingBootstrap>();
                Call(host, "OnBuildingClicked", BuildingCatalog.GetByBuildingType(BuildingTypes.Infirmary));
                var back = root.GetComponentsInChildren<Button>(true).Single(b => {
                    string label = b.GetComponentInChildren<TextMeshProUGUI>(true)?.text;
                    return label == "Retour" || label == "Back";
                });
                back.onClick.Invoke();
                Assert.That(HiveMapUnsupportedBuildingBootstrap.OverlayOpenForExternalHost, Is.False);
                Assert.That(root.GetComponentInChildren<HiveMapPremiumBuildingWindow>(true).IsOpen, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [TestCase(CombatPatrolScreenState.Loading)]
        [TestCase(CombatPatrolScreenState.NotConfigured)]
        [TestCase(CombatPatrolScreenState.Error)]
        public void NoSuccessfulRead_DoesNotClaimZeroWounded(CombatPatrolScreenState state)
        {
            var root = new GameObject("Infirmary unknown-state QA"); root.SetActive(false);
            try
            {
                var fake = new ReadOnlyPatrolFixture(); fake.Model.State = state;
                var view = root.AddComponent<HiveMapPremiumBuildingWindow>(); view.BindInfirmaryController(fake);
                view.Show(BuildingCatalog.GetByBuildingType(BuildingTypes.Infirmary), "Infirmerie", "Infirmary", "", "", view.Hide, () => {});
                string text = Text(root);
                Assert.That(text, Does.Not.Contain("0 blessées en soins"));
                Assert.That(text, Does.Not.Contain("0 recovering troops"));
                Assert.That(text, Does.Contain("--"));
                Assert.That(fake.Mutations, Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void ExpiredCountdown_DoesNotCreditTroops_OnlyAServerSnapshotCanClearTheWard()
        {
            var root = new GameObject("Infirmary persistence QA"); root.SetActive(false);
            try
            {
                var fake = new ReadOnlyPatrolFixture();
                fake.Model.State = CombatPatrolScreenState.ReadyToLaunch;
                var batches = new [] { new RemoteCombatPatrolRecoveringBatch { Family="guardians", Count=7, ReadyAtUtc=DateTimeOffset.UtcNow.AddSeconds(-1) } };
                fake.Model.Recovering = batches;
                var view = root.AddComponent<HiveMapPremiumBuildingWindow>(); view.BindInfirmaryController(fake);
                view.Show(BuildingCatalog.GetByBuildingType(BuildingTypes.Infirmary), "Infirmerie", "Infirmary", "", "", view.Hide, () => {});
                Assert.That(Text(root), Does.Contain("7"));
                Call(view,"RefreshInfirmaryLabels");
                Assert.That(fake.Model.Recovering, Is.SameAs(batches));
                Assert.That(fake.Model.Recovering[0].Count, Is.EqualTo(7));
                view.Hide();view.BindInfirmaryController(fake);
                view.Show(BuildingCatalog.GetByBuildingType(BuildingTypes.Infirmary), "Infirmerie", "Infirmary", "", "", view.Hide, () => {});
                Assert.That(Text(root), Does.Contain("7"));
                // Model update stands for a newly received authoritative snapshot, not a UI action.
                fake.Model.Recovering = Array.Empty<RemoteCombatPatrolRecoveringBatch>();
                Call(view,"RefreshInfirmaryLabels");
                Assert.That(Text(root).Contains("0 blessées en soins") || Text(root).Contains("0 recovering troops"), Is.True);
                Assert.That(fake.Mutations, Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void SessionRebind_ClearsPreviousPlayersVisibleCasualties()
        {
            var root = new GameObject("Infirmary session QA"); root.SetActive(false);
            try
            {
                var fake = new ReadOnlyPatrolFixture();fake.Model.State=CombatPatrolScreenState.ReadyToLaunch;
                fake.Model.Recovering=new[]{new RemoteCombatPatrolRecoveringBatch{Family="darters",Count=97,ReadyAtUtc=DateTimeOffset.UtcNow.AddHours(1)}};
                var view=root.AddComponent<HiveMapPremiumBuildingWindow>();view.BindInfirmaryController(fake);
                view.Show(BuildingCatalog.GetByBuildingType(BuildingTypes.Infirmary),"Infirmerie","Infirmary","","",view.Hide,()=>{});
                Assert.That(Text(root),Does.Contain("97"));
                view.BindInfirmaryController(null);
                Assert.That(Text(root),Does.Not.Contain("97"));
                Assert.That(Text(root),Does.Contain("--"));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void ProductionInitializerAndUpgradeRouteRemainConnected()
        {
            string initializer=File.ReadAllText(Path.Combine(Application.dataPath,"BeeKingdom/Playground/HiveMapRuntimeBootstrapInitializer.cs"));
            StringAssert.Contains("HiveMapUnsupportedBuildingBootstrap.InitializeForScene(scene)",initializer);
            string host=File.ReadAllText(Path.Combine(Application.dataPath,"BeeKingdom/Playground/HiveMapUnsupportedBuildingBootstrap.cs"));
            StringAssert.Contains("CloseInfirmaryWindow, UpgradeInfirmary",host);
            StringAssert.Contains("OpenConstructionOverlayForExternalHost(key)",host);
            StringAssert.Contains("KeyCode.Escape",host);
            Assert.That(host.IndexOf("if (IsInfirmarySelection)",host.IndexOf("private void OnGUI",StringComparison.Ordinal),StringComparison.Ordinal),
                Is.LessThan(host.IndexOf("GUI.Box(panel",StringComparison.Ordinal)));
        }

        private sealed class ReadOnlyPatrolFixture : ICombatPatrolPanelController
        {
            public CombatPatrolScreenModel Model {get;}=new CombatPatrolScreenModel();
            public bool IsConfigured=>true;public bool IsBusy=>false;public int Mutations;
            public void Refresh(){} public void SelectTier(int t){} public void AdjustDraft(string f,int d){}
            public void SelectEncounter(Guid id){} public void ClearSelection(){}
            public void Launch(){Mutations++;}public void Claim(){Mutations++;}public void Recall(){Mutations++;}
            public void PurchaseResourceSlot(){Mutations++;}public void GrantPremiumSlot(){Mutations++;}
            public void DismissDebrief(){}
            public bool TryGetRecentClaimReceipt(Guid id,out RemoteCombatPatrolClaimReceipt receipt){receipt=null;return false;}
            public IReadOnlyList<RemoteCombatPatrolClaimReceipt> RecentClaimReceipts=>Array.Empty<RemoteCombatPatrolClaimReceipt>();
        }
    }
}