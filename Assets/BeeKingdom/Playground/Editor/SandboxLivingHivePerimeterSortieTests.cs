using System;
using System.Collections.Generic;
using System.Linq;
using BeeKingdom.Localization;
using BeeKingdom.Networking;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxLivingHivePerimeterSortieTests
    {
        public static void RunAllAssertions()
        {
            var tests = new SandboxLivingHivePerimeterSortieTests();
            tests.NotConfiguredAndLoadingNeverInventActions();
            tests.EmptyReservationLeadsToReservationWithoutLaunch();
            tests.ReservationEnablesOnlyServerLaunchableSignals();
            tests.ActiveCountdownUsesServerRelativeDurationAndMonotonicElapsed();
            tests.ServerTimeAloneUnlocksClaimAndRecallRemainsAvailable();
            tests.CompletedCycleOffersNoMutation();
            tests.OfflineSnapshotSuppressesEveryServerMutation();
            tests.ClaimReceiptProjectsExactCapacityAwareDebrief();
            tests.PresenterDefaultsToNotConfiguredAndKeepsDraftOnBack();
            tests.PresenterRoutesOnlyServerAuthorizedActionsToInjectedController();
            tests.PresenterDismissesDebriefLocallyWithoutAnotherServerMutation();
            tests.PortraitAndLandscapeSortieLayoutsStayInsideArmyPanel();
            tests.AllPerimeterSortieCopyExistsInBothCatalogs();
        }

        [Test]
        public void NotConfiguredAndLoadingNeverInventActions()
        {
            HivePerimeterSortieScreenModel closed = HivePerimeterSortiePresentation.NotConfigured();
            HivePerimeterSortieScreenModel loading = HivePerimeterSortiePresentation.Loading();

            Assert.That(closed.State, Is.EqualTo(HivePerimeterSortieScreenState.NotConfigured));
            Assert.That(closed.PrimaryAction, Is.EqualTo(HivePerimeterSortieAction.None));
            Assert.That(closed.Signals, Is.Empty);
            Assert.That(loading.State, Is.EqualTo(HivePerimeterSortieScreenState.Loading));
            Assert.That(loading.ActiveSortieId, Is.EqualTo(Guid.Empty));
        }

        [Test]
        public void EmptyReservationLeadsToReservationWithoutLaunch()
        {
            RemoteHivePerimeterSnapshot snapshot = Snapshot(false, false);
            HivePerimeterSortieScreenModel model = HivePerimeterSortiePresentation.FromSnapshot(snapshot);

            Assert.That(model.State, Is.EqualTo(HivePerimeterSortieScreenState.NeedsReservation));
            Assert.That(model.PrimaryAction, Is.EqualTo(HivePerimeterSortieAction.ReserveSquad));
            Assert.That(model.ReservedTotal, Is.Zero);
            Assert.That(model.CanLaunch("foraging_scout"), Is.False);
        }

        [Test]
        public void ReservationEnablesOnlyServerLaunchableSignals()
        {
            RemoteHivePerimeterSnapshot snapshot = Snapshot(true, false);
            snapshot.Signals[0].Completed = true;
            snapshot.Signals[0].CanLaunch = false;
            HivePerimeterSortieScreenModel model = HivePerimeterSortiePresentation.FromSnapshot(snapshot);

            Assert.That(model.State, Is.EqualTo(HivePerimeterSortieScreenState.ReadyToLaunch));
            Assert.That(model.PrimaryAction, Is.EqualTo(HivePerimeterSortieAction.LaunchSignal));
            Assert.That(model.CanLaunch("foraging_scout"), Is.False);
            Assert.That(model.CanLaunch("brood_watch"), Is.True);
            Assert.That(model.ReservedTotal, Is.EqualTo(3));
        }

        [Test]
        public void ActiveCountdownUsesServerRelativeDurationAndMonotonicElapsed()
        {
            RemoteHivePerimeterSnapshot snapshot = Snapshot(true, true);
            snapshot.Active.EndsAtUtc = snapshot.ServerTimeUtc.AddSeconds(11);
            HivePerimeterSortieScreenModel model = HivePerimeterSortiePresentation.FromSnapshot(snapshot);

            Assert.That(model.State, Is.EqualTo(HivePerimeterSortieScreenState.Active));
            Assert.That(model.PrimaryAction, Is.EqualTo(HivePerimeterSortieAction.None));
            Assert.That(model.SecondaryAction, Is.EqualTo(HivePerimeterSortieAction.Recall));
            Assert.That(model.RemainingAtReceipt, Is.EqualTo(TimeSpan.FromSeconds(11)));
            Assert.That(model.EstimateRemaining(TimeSpan.FromSeconds(4)), Is.EqualTo(TimeSpan.FromSeconds(7)));
            Assert.That(model.EstimateRemaining(TimeSpan.FromSeconds(99)), Is.EqualTo(TimeSpan.Zero));
            Assert.That(model.EstimateRemaining(TimeSpan.FromSeconds(-5)), Is.EqualTo(TimeSpan.FromSeconds(11)));
        }

        [Test]
        public void ServerTimeAloneUnlocksClaimAndRecallRemainsAvailable()
        {
            RemoteHivePerimeterSnapshot snapshot = Snapshot(true, true);
            snapshot.ServerTimeUtc = snapshot.Active.EndsAtUtc;
            HivePerimeterSortieScreenModel model = HivePerimeterSortiePresentation.FromSnapshot(snapshot);

            Assert.That(model.State, Is.EqualTo(HivePerimeterSortieScreenState.ClaimReady));
            Assert.That(model.PrimaryAction, Is.EqualTo(HivePerimeterSortieAction.Claim));
            Assert.That(model.SecondaryAction, Is.EqualTo(HivePerimeterSortieAction.Recall));
            Assert.That(model.RemainingAtReceipt, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void CompletedCycleOffersNoMutation()
        {
            RemoteHivePerimeterSnapshot snapshot = Snapshot(false, false);
            foreach (RemoteHivePerimeterSignal signal in snapshot.Signals)
            {
                signal.Completed = true;
                signal.CanLaunch = false;
            }
            HivePerimeterSortieScreenModel model = HivePerimeterSortiePresentation.FromSnapshot(snapshot);

            Assert.That(model.State, Is.EqualTo(HivePerimeterSortieScreenState.CycleComplete));
            Assert.That(model.PrimaryAction, Is.EqualTo(HivePerimeterSortieAction.None));
            Assert.That(model.SecondaryAction, Is.EqualTo(HivePerimeterSortieAction.None));
        }

        [Test]
        public void OfflineSnapshotSuppressesEveryServerMutation()
        {
            RemoteHivePerimeterSnapshot snapshot = Snapshot(true, false);
            HivePerimeterSortieScreenModel model = HivePerimeterSortiePresentation.FromSnapshot(
                snapshot,
                includeClaimReceipt: false,
                readOnlyOffline: true,
                cachedAtUtc: snapshot.ServerTimeUtc);

            Assert.That(model.ReadOnlyOffline, Is.True);
            Assert.That(model.PrimaryAction, Is.EqualTo(HivePerimeterSortieAction.None));
            Assert.That(model.SecondaryAction, Is.EqualTo(HivePerimeterSortieAction.None));
            Assert.That(model.CanLaunch("foraging_scout"), Is.False);
            AssertRow(
                HiveViewProductUiPresenter.PerimeterSortieForProof(model, true, 390f, 844f, 0f),
                "perimeter_sortie_read_only_offline:true");
        }

        [Test]
        public void ClaimReceiptProjectsExactCapacityAwareDebrief()
        {
            HivePerimeterSortieScreenModel model = HivePerimeterSortiePresentation.FromSnapshot(ClaimSnapshot());

            Assert.That(model.State, Is.EqualTo(HivePerimeterSortieScreenState.ReturnDebrief));
            Assert.That(model.PrimaryAction, Is.EqualTo(HivePerimeterSortieAction.DismissDebrief));
            Assert.That(model.SecondaryAction, Is.EqualTo(HivePerimeterSortieAction.None));
            Assert.That(model.ReturnDebrief.HoneyCredited, Is.EqualTo(10));
            Assert.That(model.ReturnDebrief.PollenCredited, Is.EqualTo(20));
            Assert.That(model.ReturnDebrief.HoneyBalance, Is.EqualTo(130));
            Assert.That(model.ReturnDebrief.HoneyCapacityLimited, Is.True);
            Assert.That(model.ReturnDebrief.PollenCapacityLimited, Is.False);
        }

        [Test]
        public void PresenterDefaultsToNotConfiguredAndKeepsDraftOnBack()
        {
            try
            {
                HiveViewProductUiPresenter.ResetPerimeterSortieControllerForProof();
                HiveViewProductUiPresenter.PrepareSquadCompositionCaptureForProof(true);
                int guardians = HiveViewProductUiPresenter.FormationDraftGuardiansForProof;
                int wingrunners = HiveViewProductUiPresenter.FormationDraftWingrunnersForProof;
                int darters = HiveViewProductUiPresenter.FormationDraftDartersForProof;
                HiveViewProductUiPresenter.OpenPerimeterSortieForProof();

                Assert.That(HiveViewProductUiPresenter.PerimeterSortieOpenForProof, Is.True);
                AssertRow(
                    HiveViewProductUiPresenter.PerimeterSortieForProof(HivePerimeterSortiePresentation.NotConfigured(), true, 390f, 844f, 0f),
                    "perimeter_sortie_state:not_configured");
                HiveViewProductUiPresenter.InvokePerimeterSortiePrimaryForProof();
                HiveViewProductUiPresenter.InvokePerimeterSortieSecondaryForProof();
                HiveViewProductUiPresenter.ClosePerimeterSortieForProof();

                Assert.That(HiveViewProductUiPresenter.PerimeterSortieOpenForProof, Is.False);
                Assert.That(HiveViewProductUiPresenter.FormationReadinessOpenForProof, Is.True);
                Assert.That(HiveViewProductUiPresenter.FormationDraftGuardiansForProof, Is.EqualTo(guardians));
                Assert.That(HiveViewProductUiPresenter.FormationDraftWingrunnersForProof, Is.EqualTo(wingrunners));
                Assert.That(HiveViewProductUiPresenter.FormationDraftDartersForProof, Is.EqualTo(darters));
            }
            finally
            {
                HiveViewProductUiPresenter.ResetPerimeterSortieControllerForProof();
                HiveViewProductUiPresenter.CloseFormationReadinessForProof();
            }
        }

        [Test]
        public void PresenterRoutesOnlyServerAuthorizedActionsToInjectedController()
        {
            var controller = new MemoryController(HivePerimeterSortiePresentation.FromSnapshot(Snapshot(false, false)));
            try
            {
                HiveViewProductUiPresenter.PrepareSquadCompositionCaptureForProof(true);
                HiveViewProductUiPresenter.UsePerimeterSortieControllerForProof(controller, true);
                HiveViewProductUiPresenter.OpenPerimeterSortieForProof();
                Assert.That(controller.RefreshCount, Is.EqualTo(1));

                HiveViewProductUiPresenter.InvokePerimeterSortiePrimaryForProof();
                Assert.That(controller.ReserveCount, Is.Zero);
                Assert.That(
                    HiveViewProductUiPresenter.PerimeterSortieOpenForProof,
                    Is.False);

                controller.Model = HivePerimeterSortiePresentation.FromSnapshot(Snapshot(true, false));
                HiveViewProductUiPresenter.OpenPerimeterSortieForProof();
                HiveViewProductUiPresenter.SelectPerimeterSortieSignalForProof(1);
                HiveViewProductUiPresenter.InvokePerimeterSortiePrimaryForProof();
                Assert.That(controller.LaunchCount, Is.EqualTo(1));
                Assert.That(controller.LastSignal, Is.EqualTo("brood_watch"));

                RemoteHivePerimeterSnapshot claimReady = Snapshot(true, true);
                claimReady.ServerTimeUtc = claimReady.Active.EndsAtUtc;
                controller.Model = HivePerimeterSortiePresentation.FromSnapshot(claimReady);
                HiveViewProductUiPresenter.InvokePerimeterSortiePrimaryForProof();
                Assert.That(controller.ClaimCount, Is.EqualTo(1));

                controller.Model = HivePerimeterSortiePresentation.FromSnapshot(Snapshot(true, true));
                HiveViewProductUiPresenter.InvokePerimeterSortieSecondaryForProof();
                Assert.That(controller.RecallCount, Is.EqualTo(1));
            }
            finally
            {
                HiveViewProductUiPresenter.ResetPerimeterSortieControllerForProof();
                HiveViewProductUiPresenter.CloseFormationReadinessForProof();
            }
        }

        [Test]
        public void PresenterDismissesDebriefLocallyWithoutAnotherServerMutation()
        {
            var controller = new MemoryController(HivePerimeterSortiePresentation.FromSnapshot(ClaimSnapshot()));
            try
            {
                HiveViewProductUiPresenter.UsePerimeterSortieControllerForProof(controller, true);
                HiveViewProductUiPresenter.OpenPerimeterSortieForProof();

                HiveViewProductUiPresenter.InvokePerimeterSortiePrimaryForProof();

                Assert.That(controller.DismissDebriefCount, Is.EqualTo(1));
                Assert.That(controller.ClaimCount, Is.Zero);
                Assert.That(controller.RecallCount, Is.Zero);
                string[] proof = HiveViewProductUiPresenter.PerimeterSortieForProof(controller.Model, true, 390f, 844f, 0f);
                AssertRow(proof, "perimeter_sortie_state:return_debrief");
                AssertRow(proof, "perimeter_sortie_primary_action:dismiss_debrief");
                AssertRow(proof, "perimeter_sortie_debrief_honey_credited:10");
                AssertRow(proof, "perimeter_sortie_debrief_capacity_limited:true");
                AssertRow(proof, "perimeter_sortie_debrief_receipt_bound:true");
                AssertRow(proof, "perimeter_sortie_debrief_device_lifetime:panel_session_only");
            }
            finally
            {
                HiveViewProductUiPresenter.ResetPerimeterSortieControllerForProof();
                HiveViewProductUiPresenter.CloseFormationReadinessForProof();
            }
        }

        [Test]
        public void PortraitAndLandscapeSortieLayoutsStayInsideArmyPanel()
        {
            AssertLayout(true, 390f, 844f);
            AssertLayout(false, 1600f, 900f);
        }

        [Test]
        public void AllPerimeterSortieCopyExistsInBothCatalogs()
        {
            string[] keys =
            {
                "perimeter_sortie.entry", "perimeter_sortie.opened", "perimeter_sortie.closed",
                "perimeter_sortie.title", "perimeter_sortie.subtitle", "perimeter_sortie.subtitle.compact",
                "perimeter_sortie.back",
                "perimeter_sortie.proof_disclosure", "perimeter_sortie.reservation.title",
                "perimeter_sortie.reservation.ready", "perimeter_sortie.reservation.empty",
                "perimeter_sortie.signals.label", "perimeter_sortie.signal.waiting",
                "perimeter_sortie.signal.completed", "perimeter_sortie.signal.available",
                "perimeter_sortie.signal.locked", "perimeter_sortie.signal.hazard",
                "perimeter_sortie.signal.requirement", "perimeter_sortie.signal.reward",
                "perimeter_sortie.signal.foraging_scout.name", "perimeter_sortie.signal.foraging_scout.body",
                "perimeter_sortie.signal.brood_watch.name", "perimeter_sortie.signal.brood_watch.body",
                "perimeter_sortie.signal_selected", "perimeter_sortie.active.title",
                "perimeter_sortie.active.ready", "perimeter_sortie.active.remaining",
                "perimeter_sortie.active.disclosure", "perimeter_sortie.detail.title",
                "perimeter_sortie.detail.body", "perimeter_sortie.server_disclosure",
                "perimeter_sortie.action.syncing", "perimeter_sortie.action.compose",
                "perimeter_sortie.action.reserve", "perimeter_sortie.action.launch",
                "perimeter_sortie.action.claim", "perimeter_sortie.action.recall",
                "perimeter_sortie.action.no_recall", "perimeter_sortie.action.complete",
                "perimeter_sortie.action.session_required", "perimeter_sortie.action.wait",
                "perimeter_sortie.status.loading.title", "perimeter_sortie.status.loading.body",
                "perimeter_sortie.status.reserve.title", "perimeter_sortie.status.reserve.body",
                "perimeter_sortie.status.ready.title", "perimeter_sortie.status.ready.body",
                "perimeter_sortie.status.active.title", "perimeter_sortie.status.active.body",
                "perimeter_sortie.status.claim.title", "perimeter_sortie.status.claim.body",
                "perimeter_sortie.status.complete.title", "perimeter_sortie.status.complete.body",
                "perimeter_sortie.status.error.title", "perimeter_sortie.status.error.body",
                "perimeter_sortie.status.not_configured.title", "perimeter_sortie.status.not_configured.body",
                "perimeter_sortie.offline.title", "perimeter_sortie.offline.body",
                "perimeter_sortie.debrief.title", "perimeter_sortie.debrief.subtitle",
                "perimeter_sortie.debrief.receipt", "perimeter_sortie.debrief.revision",
                "perimeter_sortie.debrief.resources", "perimeter_sortie.debrief.resource.honey",
                "perimeter_sortie.debrief.resource.pollen", "perimeter_sortie.debrief.stock",
                "perimeter_sortie.debrief.limited", "perimeter_sortie.debrief.complete",
                "perimeter_sortie.debrief.result", "perimeter_sortie.debrief.capacity_limited",
                "perimeter_sortie.debrief.full_credit", "perimeter_sortie.debrief.disclosure",
                "perimeter_sortie.debrief.continue", "perimeter_sortie.debrief.verified"
            };
            foreach (string key in keys)
            {
                Assert.That(BeeLocalization.HasText("fr-CA", key), Is.True, "Missing fr-CA " + key);
                Assert.That(BeeLocalization.HasText("en-US", key), Is.True, "Missing en-US " + key);
            }
        }

        private static void AssertLayout(bool portrait, float width, float height)
        {
            Rect panel = HiveViewProductUiPresenter.ArmyMenuPanelRectForProof(portrait, width, height, true);
            Rect back = HiveViewProductUiPresenter.PerimeterSortieBackRectForProof(portrait, width, height);
            Rect status = HiveViewProductUiPresenter.PerimeterSortieStatusRectForProof(portrait, width, height);
            Rect reservation = HiveViewProductUiPresenter.PerimeterSortieReservationRectForProof(portrait, width, height);
            Rect[] signals = HiveViewProductUiPresenter.PerimeterSortieSignalRectsForProof(portrait, width, height);
            Rect detail = HiveViewProductUiPresenter.PerimeterSortieDetailRectForProof(portrait, width, height);
            Rect primary = HiveViewProductUiPresenter.PerimeterSortiePrimaryRectForProof(portrait, width, height);
            Rect secondary = HiveViewProductUiPresenter.PerimeterSortieSecondaryRectForProof(portrait, width, height);
            foreach (Rect rect in signals.Concat(new[] { back, status, reservation, detail, primary, secondary })) AssertInside(panel, rect);
            foreach (Rect control in signals.Concat(new[] { back, primary, secondary }))
            {
                Assert.That(control.width, Is.GreaterThanOrEqualTo(44f));
                Assert.That(control.height, Is.GreaterThanOrEqualTo(44f));
            }
            Assert.That(status.yMax, Is.LessThanOrEqualTo(reservation.y));
            Assert.That(reservation.yMax, Is.LessThanOrEqualTo(signals[0].y));
            Assert.That(signals.Max(rect => rect.yMax), Is.LessThanOrEqualTo(detail.y));
            Assert.That(detail.yMax, Is.LessThanOrEqualTo(primary.y));
            string[] proof = HiveViewProductUiPresenter.PerimeterSortieForProof(
                HivePerimeterSortiePresentation.FromSnapshot(Snapshot(true, true)), portrait, width, height, 4f);
            AssertRow(proof, "perimeter_sortie_min_touch:44");
            AssertRow(proof, "perimeter_sortie_panel_contains_status:true");
            AssertRow(proof, "perimeter_sortie_panel_contains_reservation:true");
            AssertRow(proof, "perimeter_sortie_panel_contains_signals:true");
            AssertRow(proof, "perimeter_sortie_panel_contains_detail:true");
            AssertRow(proof, "perimeter_sortie_server_time_authoritative:true");
            AssertRow(proof, "perimeter_sortie_offline_reward_mutation:false");

            string[] debriefProof = HiveViewProductUiPresenter.PerimeterSortieForProof(
                HivePerimeterSortiePresentation.FromSnapshot(ClaimSnapshot()), portrait, width, height, 0f);
            AssertRow(debriefProof, "perimeter_sortie_state:return_debrief");
            AssertRow(debriefProof, "perimeter_sortie_debrief_visible:true");
            AssertRow(debriefProof, "perimeter_sortie_debrief_honey_credited:10");
        }

        private static RemoteHivePerimeterSnapshot ClaimSnapshot()
        {
            RemoteHivePerimeterSnapshot snapshot = Snapshot(false, false);
            snapshot.Revision = 2;
            snapshot.Signals[0].Completed = true;
            snapshot.Signals[0].CanLaunch = false;
            snapshot.ClaimReceipt = new RemoteHivePerimeterClaimReceipt
            {
                SortieId = Guid.Parse("87654321-4321-4321-4321-cba987654321"),
                SignalKey = snapshot.Signals[0].SignalKey,
                SignalInstanceId = snapshot.Signals[0].SignalInstanceId,
                Revision = 2,
                ServerTimeUtc = snapshot.ServerTimeUtc,
                CreditedByResource = new Dictionary<string, long> { ["honey"] = 10, ["pollen"] = 20 },
                ResultingBalances = new Dictionary<string, RemoteHiveResourceBalance>
                {
                    ["honey"] = new RemoteHiveResourceBalance { Amount = 130, Capacity = 130 },
                    ["pollen"] = new RemoteHiveResourceBalance { Amount = 120, Capacity = 1000 }
                }
            };
            return snapshot;
        }

        private static RemoteHivePerimeterSnapshot Snapshot(bool reserved, bool active)
        {
            DateTimeOffset cycle = new DateTimeOffset(2026, 7, 22, 8, 0, 0, TimeSpan.Zero);
            var reservation = new RemoteSquadReservationSnapshot
            {
                ReservationId = reserved ? new string('a', 32) : string.Empty,
                Reserved = new Dictionary<string, long>
                {
                    ["guardians"] = reserved ? 1 : 0,
                    ["wingrunners"] = reserved ? 2 : 0,
                    ["darters"] = 0
                }
            };
            var signals = new List<RemoteHivePerimeterSignal>
            {
                new RemoteHivePerimeterSignal { SignalKey = "foraging_scout", SignalInstanceId = new string('b', 32), HazardDoctrine = "wingrunners", Duration = TimeSpan.FromSeconds(16), MinimumSquad = 1, HoneyReward = 40, PollenReward = 20, CanLaunch = !active },
                new RemoteHivePerimeterSignal { SignalKey = "brood_watch", SignalInstanceId = new string('c', 32), HazardDoctrine = "guardians", Duration = TimeSpan.FromSeconds(20), MinimumSquad = 2, HoneyReward = 25, PollenReward = 35, CanLaunch = !active }
            };
            var snapshot = new RemoteHivePerimeterSnapshot
            {
                Revision = active ? 1 : 0,
                ServerTimeUtc = cycle.AddMinutes(3),
                CycleStartedAtUtc = cycle,
                CycleEndsAtUtc = cycle.AddHours(8),
                Reservation = reservation,
                Signals = signals
            };
            if (active)
            {
                snapshot.Active = new RemoteHivePerimeterActiveSortie
                {
                    SortieId = Guid.Parse("12345678-1234-1234-1234-123456789abc"),
                    SignalKey = signals[0].SignalKey,
                    SignalInstanceId = signals[0].SignalInstanceId,
                    ReservationId = reservation.ReservationId,
                    StartedAtUtc = snapshot.ServerTimeUtc,
                    EndsAtUtc = snapshot.ServerTimeUtc.Add(signals[0].Duration),
                    Revision = 1
                };
            }
            return snapshot;
        }

        private static void AssertInside(Rect outer, Rect inner)
        {
            Assert.That(outer.Contains(inner.min), Is.True);
            Assert.That(outer.Contains(new Vector2(inner.xMax - 0.01f, inner.yMax - 0.01f)), Is.True);
        }

        private static void AssertRow(IEnumerable<string> rows, string expected)
        {
            Assert.That(rows, Does.Contain(expected));
        }

        private sealed class MemoryController : IHivePerimeterSortiePanelController
        {
            public MemoryController(HivePerimeterSortieScreenModel model) { Model = model; }
            public HivePerimeterSortieScreenModel Model { get; set; }
            public bool IsConfigured => true;
            public bool IsBusy { get; set; }
            public int RefreshCount { get; private set; }
            public int ReserveCount { get; private set; }
            public int LaunchCount { get; private set; }
            public int ClaimCount { get; private set; }
            public int RecallCount { get; private set; }
            public int RetryCount { get; private set; }
            public int DismissDebriefCount { get; private set; }
            public int[] LastSquad { get; private set; }
            public string LastSignal { get; private set; }
            public void Refresh() { RefreshCount++; }
            public void ReserveSquad(int guardians, int wingrunners, int darters) { ReserveCount++; LastSquad = new[] { guardians, wingrunners, darters }; }
            public void Launch(string signalKey) { LaunchCount++; LastSignal = signalKey; }
            public void Claim() { ClaimCount++; }
            public void Recall() { RecallCount++; }
            public void Retry() { RetryCount++; }
            public void DismissDebrief() { DismissDebriefCount++; }
        }
    }
}
