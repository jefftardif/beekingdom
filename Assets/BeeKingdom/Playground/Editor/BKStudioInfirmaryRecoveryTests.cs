using System;
using System.Collections.Generic;
using System.Linq;
using BeeKingdom.Buildings.Interaction;
using BeeKingdom.Networking;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeeKingdom.Playground.Editor
{
    public sealed class BKStudioInfirmaryRecoveryTests
    {
        private sealed class FakePatrolController : ICombatPatrolPanelController
        {
            public CombatPatrolScreenModel Model { get; } = new CombatPatrolScreenModel { State = CombatPatrolScreenState.ReadyToLaunch };
            public bool IsConfigured { get; set; } = true;
            public bool IsBusy { get; set; }
            public int RefreshCalls { get; private set; }
            public void Refresh() { RefreshCalls++; }
            public void SelectTier(int tier) { }
            public void AdjustDraft(string family, int delta) { }
            public void SelectEncounter(Guid encounterId) { }
            public void ClearSelection() { }
            public void Launch() { }
            public void Claim() { }
            public void Recall() { }
            public void PurchaseResourceSlot() { }
            public void GrantPremiumSlot() { }
            public void DismissDebrief() { }
            public bool TryGetRecentClaimReceipt(Guid encounterId,
                out RemoteCombatPatrolClaimReceipt receipt) { receipt = null; return false; }
            public IReadOnlyList<RemoteCombatPatrolClaimReceipt> RecentClaimReceipts
                => Array.Empty<RemoteCombatPatrolClaimReceipt>();
        }

        [Test]
        public void RecoveryWardAggregatesActualServerBatchesAndKeepsFamilyOrder()
        {
            DateTimeOffset now = new DateTimeOffset(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);
            var batches = new List<RemoteCombatPatrolRecoveringBatch>
            {
                new RemoteCombatPatrolRecoveringBatch {
                    Family = "guardians", Count = 4, ReadyAtUtc = now.AddMinutes(12) },
                new RemoteCombatPatrolRecoveringBatch {
                    Family = "guardians", Count = 6, ReadyAtUtc = now.AddMinutes(5) },
                new RemoteCombatPatrolRecoveringBatch {
                    Family = "darters", Count = 3, ReadyAtUtc = now.AddMinutes(9) },
                new RemoteCombatPatrolRecoveringBatch {
                    Family = "unknown", Count = 999, ReadyAtUtc = now.AddMinutes(1) },
                new RemoteCombatPatrolRecoveringBatch {
                    Family = "wingrunners", Count = -100, ReadyAtUtc = now.AddMinutes(1) }
            };
            IReadOnlyList<InfirmaryRecoveryRow> rows = InfirmaryRecoveryOverview.Aggregate(batches);
            Assert.That(rows.Count, Is.EqualTo(3));
            Assert.That(rows[0].Family, Is.EqualTo("guardians"));
            Assert.That(rows[0].Wounded, Is.EqualTo(10L));
            Assert.That(rows[0].NextReadyAtUtc, Is.EqualTo(now.AddMinutes(5)));
            Assert.That(rows[0].LastReadyAtUtc, Is.EqualTo(now.AddMinutes(12)));
            Assert.That(rows[0].BatchCount, Is.EqualTo(2));
            Assert.That(rows[1].Family, Is.EqualTo("wingrunners"));
            Assert.That(rows[1].Wounded, Is.Zero);
            Assert.That(rows[2].Wounded, Is.EqualTo(3L));
            Assert.That(InfirmaryRecoveryOverview.Total(rows), Is.EqualTo(13L));
        }

        [Test]
        public void NoServerBatchesMeansZeroWithoutMakingUpWoundedTroops()
        {
            IReadOnlyList<InfirmaryRecoveryRow> rows = InfirmaryRecoveryOverview.Aggregate(null);
            Assert.That(rows.Count, Is.EqualTo(3));
            Assert.That(rows.All(r => r.Wounded == 0 && !r.NextReadyAtUtc.HasValue), Is.True);
            Assert.That(InfirmaryRecoveryOverview.Total(rows), Is.Zero);
        }

        [Test]
        public void CountdownNeverCreditsBeesBeforeServerConfirmsRecovery()
        {
            DateTimeOffset now = new DateTimeOffset(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);
            Assert.That(InfirmaryRecoveryOverview.Countdown(now.AddMinutes(5), now, true),
                Does.Contain("05:00"));
            Assert.That(InfirmaryRecoveryOverview.Countdown(now.AddHours(1),
                now, false), Does.Contain("1 h 00 min"));
            Assert.That(InfirmaryRecoveryOverview.Countdown(now.AddSeconds(-1),
                now, true), Does.Contain("actualiser"));
            Assert.That(InfirmaryRecoveryOverview.Countdown(null, now, false),
                Does.Contain("No wounded"));
        }

        [Test]
        public void InfirmaryUIUsesLiveSharedPatrolControllerAndRealRefresh()
        {
            GameObject host = new GameObject("Infirmary V1 QA");
            host.SetActive(false);
            try
            {
                var fake = new FakePatrolController();
                fake.Model.Recovering = new List<RemoteCombatPatrolRecoveringBatch>
                {
                    new RemoteCombatPatrolRecoveringBatch
                    {
                        Family = "guardians", Count = 7,
                        ReadyAtUtc = DateTimeOffset.UtcNow.AddMinutes(17)
                    }
                };
                HiveMapPremiumBuildingWindow view = host.AddComponent<HiveMapPremiumBuildingWindow>();
                view.BindInfirmaryController(fake);
                view.Show(BuildingCatalog.GetByBuildingType(BuildingTypes.Infirmary),
                    "Infirmerie", "Infirmary", "Placeholder fr", "Placeholder en",
                    () => view.Hide(), () => { });
                Assert.That(view.IsOpen, Is.True);
                Assert.That(view.InfirmaryActiveForProof, Is.True);
                Assert.That(fake.RefreshCalls, Is.EqualTo(1),
                    "Opening Infirmary must request the official server snapshot.");
                string joined = string.Join("|", host.GetComponentsInChildren<TextMeshProUGUI>(true)
                    .Select(t => t.text).ToArray());
                Assert.That(joined.Contains("7 blessées en soins") || joined.Contains("7 recovering troops"), Is.True);
                Assert.That(joined.IndexOf("Gardiennes", StringComparison.OrdinalIgnoreCase) >= 0 || joined.IndexOf("Guardians", StringComparison.OrdinalIgnoreCase) >= 0, Is.True);
                Assert.That(joined.Contains("Prochaine guérison") || joined.Contains("Next recovery"), Is.True);
                Assert.That(joined, Does.Not.Contain("Fonctionnalité en préparation"));
                ScrollRect scroll = host.GetComponentInChildren<ScrollRect>(true);
                Assert.That(scroll, Is.Not.Null);
                Assert.That(scroll.verticalScrollbar, Is.Not.Null);
                Button refresh = host.GetComponentsInChildren<Button>(true).FirstOrDefault(btn =>
                    (btn.GetComponentInChildren<TextMeshProUGUI>(true)?.text == "Actualiser" || btn.GetComponentInChildren<TextMeshProUGUI>(true)?.text == "Refresh"));
                Assert.That(refresh, Is.Not.Null);
                refresh.onClick.Invoke();
                Assert.That(fake.RefreshCalls, Is.EqualTo(2));
                view.Hide();
                Assert.That(view.InfirmaryActiveForProof, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }

        [Test]
        public void InfirmaryShowsConnectionRequiredWhenOfficialControllerUnavailable()
        {
            GameObject host = new GameObject("Offline Infirmary QA");
            host.SetActive(false);
            try
            {
                HiveMapPremiumBuildingWindow view = host.AddComponent<HiveMapPremiumBuildingWindow>();
                view.BindInfirmaryController(null);
                view.Show(BuildingCatalog.GetByBuildingType(BuildingTypes.Infirmary),
                    "Infirmerie", "Infirmary", "", "", () => view.Hide(), () => { });
                string all = string.Join("|", host.GetComponentsInChildren<TextMeshProUGUI>(true)
                    .Select(t => t.text).ToArray());
                Assert.That(all.Contains("Connexion requise") || all.Contains("Connection required"), Is.True);
                Assert.That(all.Contains("En attente du serveur") || all.Contains("Awaiting server"), Is.True);
                Assert.That(all, Does.Not.Contain("0 blessées en soins"));
                view.Hide();
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }

        [Test]
        public void InfirmaryIsNowActiveButOtherUndeliveredBuildingsRemainFuture()
        {
            Assert.That(BuildingCatalog.All.Count, Is.EqualTo(14));
            Assert.That(BuildingCatalog.GetByBuildingType(BuildingTypes.Infirmary).StateIsActive,
                Is.True);
            foreach (string type in new[] {
                BuildingTypes.Genetics, BuildingTypes.Defense, BuildingTypes.Academy })
                Assert.That(BuildingCatalog.GetByBuildingType(type).StateIsFuture, Is.True);
        }
    }
}
