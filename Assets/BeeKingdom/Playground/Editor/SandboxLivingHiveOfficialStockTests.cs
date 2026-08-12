using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Localization;
using BeeKingdom.Networking;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxLivingHiveOfficialStockTests
    {
        private static readonly Guid PlayerId =
            Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly Guid HiveId =
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly Guid OperationId =
            Guid.Parse("99999999-8888-7777-6666-555555555555");

        public static void RunAllAssertions()
        {
            var tests = new SandboxLivingHiveOfficialStockTests();
            tests.PresentationCopiesOnlyServerSnapshotValues();
            tests.OfflineProjectionIsExplicitlyReadOnly();
            tests.ControllerSeparatesServerCacheAndFailureStates()
                .GetAwaiter().GetResult();
            tests.OfficialLedgerUsesServerAuthorityAndNoLocalFallback();
            tests.OfficialLedgerNavigationDoesNotMarkLocalDailyRound();
            tests.PortraitAndLandscapeActionsRemainBoundedAtFortyFourPixels();
            tests.OfficialStockCopyExistsInBothCatalogs();
        }

        [Test]
        public void PresentationCopiesOnlyServerSnapshotValues()
        {
            RemoteHiveStockSnapshot snapshot = Snapshot();
            HiveStockScreenModel model = HiveStockPresentation.Ready(snapshot);

            Assert.That(model.State, Is.EqualTo(HiveStockScreenState.Ready));
            Assert.That(model.FindResource("honey").Amount, Is.EqualTo(500));
            Assert.That(model.FindResource("wax").Capacity, Is.EqualTo(800));
            Assert.That(model.CompletedResearchIds, Is.EqualTo(new[] { "foraging_routes_i" }));
            Assert.That(model.ActiveEngagements.Single().OperationId, Is.EqualTo(OperationId));
            Assert.That(model.Population, Is.Null);
            Assert.That(model.PopulationCapacity, Is.Null);

            snapshot.Honey.Amount = 1;
            snapshot.CompletedResearchIds.Clear();
            snapshot.ActiveEngagements.Clear();
            Assert.That(model.FindResource("honey").Amount, Is.EqualTo(500));
            Assert.That(model.CompletedResearchIds.Count, Is.EqualTo(1));
            Assert.That(model.ActiveEngagements.Count, Is.EqualTo(1));
        }

        [Test]
        public void OfflineProjectionIsExplicitlyReadOnly()
        {
            RemoteHiveStockSnapshot snapshot = Snapshot();
            HiveStockScreenModel model =
                HiveStockPresentation.OfflineReadOnly(
                    snapshot, snapshot.ServerTimeUtc.AddMinutes(1));

            Assert.That(model.State, Is.EqualTo(HiveStockScreenState.OfflineReadOnly));
            Assert.That(model.IsReadOnly, Is.True);
            Assert.That(model.CachedAtUtc, Is.EqualTo(snapshot.ServerTimeUtc.AddMinutes(1)));
            Assert.That(model.FindResource("pollen").Amount, Is.EqualTo(400));
        }

        [Test]
        public async Task ControllerSeparatesServerCacheAndFailureStates()
        {
            RemoteHiveStockSnapshot snapshot = Snapshot();
            var client = new FakeClient(snapshot);
            using (var controller = new HiveStockPanelController(client, HiveId))
            {
                await controller.RefreshForProofAsync();
                Assert.That(controller.Model.State, Is.EqualTo(HiveStockScreenState.Ready));

                client.Source = GameReadSource.ProtectedCache;
                client.CachedAtUtc = snapshot.ServerTimeUtc.AddMinutes(2);
                await controller.RefreshForProofAsync();
                Assert.That(
                    controller.Model.State,
                    Is.EqualTo(HiveStockScreenState.OfflineReadOnly));
                Assert.That(
                    controller.Model.CachedAtUtc,
                    Is.EqualTo(client.CachedAtUtc));

                client.Steps.Enqueue(
                    new HivePerimeterClientException(
                        HivePerimeterClientError.TransportFailure,
                        "game.network_unavailable"));
                await controller.RefreshForProofAsync();
                Assert.That(controller.Model.State, Is.EqualTo(HiveStockScreenState.Error));
                Assert.That(controller.Model.ErrorCode, Is.EqualTo("network_unavailable"));
                Assert.That(controller.Model.HasSnapshot, Is.True);
            }
        }

        [Test]
        public void OfficialLedgerUsesServerAuthorityAndNoLocalFallback()
        {
            try
            {
                HiveViewProductUiPresenter.UseHiveStockControllerForProof(
                    new FakePanelController(HiveStockPresentation.Ready(Snapshot())));
                string[] rows = HiveViewProductUiPresenter.HiveLedgerForProof();

                Assert.That(rows, Does.Contain("ledger_authority:server"));
                Assert.That(rows, Does.Contain("ledger_official_honey:500"));
                Assert.That(rows, Does.Contain("ledger_official_population:unavailable"));
                Assert.That(rows, Does.Contain("ledger_official_local_resource_fallback:false"));
                Assert.That(rows, Does.Contain("ledger_official_local_engagement_fallback:false"));
                Assert.That(rows, Does.Contain("ledger_official_direct_collection:false"));
            }
            finally
            {
                HiveViewProductUiPresenter.UseHiveStockControllerForProof(null);
            }
        }

        [Test]
        public void OfficialLedgerNavigationDoesNotMarkLocalDailyRound()
        {
            var store = new MemoryDailyRoundStore();
            HiveViewProductUiPresenter.UseLocalPreviewDailyRoundStoreForProof(store);
            try
            {
                HiveViewProductUiPresenter.UseHiveStockControllerForProof(
                    new FakePanelController(HiveStockPresentation.Ready(Snapshot())));
                string before = DailyRoundRow("daily_round_tasks_mask:");

                HiveViewProductUiPresenter.OpenHiveLedgerResourceForProof("honey_storage");

                string after = DailyRoundRow("daily_round_tasks_mask:");
                Assert.That(after, Is.EqualTo(before));
                Assert.That(
                    HiveViewProductUiPresenter.OfficialHiveStockForProof(),
                    Does.Contain("ledger_official_daily_round_local_marker:false"));
            }
            finally
            {
                HiveViewProductUiPresenter.UseHiveStockControllerForProof(null);
                HiveViewProductUiPresenter.UseLocalPreviewDailyRoundStoreForProof(null);
            }
        }

        [Test]
        public void PortraitAndLandscapeActionsRemainBoundedAtFortyFourPixels()
        {
            RectAssert(
                HiveViewProductUiPresenter.OfficialHiveStockActionRectsForProof(
                    true, 390f, 844f),
                390f,
                844f);
            RectAssert(
                HiveViewProductUiPresenter.OfficialHiveStockActionRectsForProof(
                    false, 1600f, 900f),
                1600f,
                900f);
        }

        [Test]
        public void OfficialStockCopyExistsInBothCatalogs()
        {
            string[] keys =
            {
                "stock.official.subtitle",
                "stock.official.refresh",
                "stock.official.not_configured",
                "stock.official.loading",
                "stock.official.offline",
                "stock.official.error",
                "stock.official.ready",
                "stock.official.value.unavailable",
                "stock.official.capacity",
                "stock.official.no_device_value",
                "stock.official.resource.active",
                "stock.official.population.unavailable",
                "stock.official.engagements.debited"
            };

            foreach (string key in keys)
            {
                Assert.That(BeeLocalization.HasText("fr-CA", key), Is.True, key + " fr-CA");
                Assert.That(BeeLocalization.HasText("en-US", key), Is.True, key + " en-US");
            }
        }

        private static string DailyRoundRow(string prefix)
        {
            return HiveViewProductUiPresenter.LocalPreviewDailyRoundForProof()
                .Single(row => row.StartsWith(prefix, StringComparison.Ordinal));
        }

        private static void RectAssert(
            IReadOnlyList<Rect> rects,
            float width,
            float height)
        {
            Assert.That(rects.Count, Is.EqualTo(3));
            foreach (Rect rect in rects)
            {
                Assert.That(rect.width, Is.GreaterThanOrEqualTo(44f));
                Assert.That(rect.height, Is.GreaterThanOrEqualTo(44f));
                Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(0f));
                Assert.That(rect.yMin, Is.GreaterThanOrEqualTo(0f));
                Assert.That(rect.xMax, Is.LessThanOrEqualTo(width));
                Assert.That(rect.yMax, Is.LessThanOrEqualTo(height));
            }
        }

        private static RemoteHiveStockSnapshot Snapshot()
        {
            DateTimeOffset now =
                new DateTimeOffset(2026, 7, 22, 12, 0, 0, TimeSpan.Zero);
            return new RemoteHiveStockSnapshot
            {
                PlayerId = PlayerId,
                HiveId = HiveId,
                ContractVersion = HiveStockSnapshotClient.ContractVersion,
                CatalogVersion = "test-v1",
                Revision = 7,
                ServerTimeUtc = now,
                Honey = new RemoteHiveStockResource { Amount = 500, Capacity = 1000 },
                Wax = new RemoteHiveStockResource { Amount = 300, Capacity = 800 },
                Pollen = new RemoteHiveStockResource { Amount = 400, Capacity = 900 },
                CompletedResearchIds =
                    new List<string> { "foraging_routes_i" },
                ActiveEngagements =
                    new List<RemoteHiveStockEngagement>
                    {
                        new RemoteHiveStockEngagement
                        {
                            OperationId = OperationId,
                            Kind = "Research",
                            Key = "tempered_combs_i",
                            StartedAtUtc = now.AddMinutes(-1),
                            EndsAtUtc = now.AddMinutes(4)
                        }
                    }
            };
        }

        private sealed class FakeClient : IHiveStockSnapshotClient
        {
            private readonly RemoteHiveStockSnapshot fallback;

            public FakeClient(RemoteHiveStockSnapshot fallback)
            {
                this.fallback = fallback;
            }

            public Queue<object> Steps { get; } = new Queue<object>();
            public GameReadSource Source { get; set; } = GameReadSource.Server;
            public DateTimeOffset CachedAtUtc { get; set; }
            public GameReadSource LastReadSource => Source;
            public DateTimeOffset LastReadCachedAtUtc => CachedAtUtc;

            public Task<RemoteHiveStockSnapshot> ReadAsync(
                Guid hiveId,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                object step = Steps.Count == 0 ? fallback : Steps.Dequeue();
                if (step is Exception exception) throw exception;
                return Task.FromResult((RemoteHiveStockSnapshot)step);
            }
        }

        private sealed class FakePanelController : IHiveStockPanelController
        {
            public FakePanelController(HiveStockScreenModel model)
            {
                Model = model;
            }
            public HiveStockScreenModel Model { get; }
            public bool IsConfigured => true;
            public bool IsBusy => false;
            public void Refresh() { }
        }

        private sealed class MemoryDailyRoundStore : ILocalPreviewDailyRoundStore
        {
            private string value = string.Empty;
            public string Read() => value;
            public void Write(string next) => value = next ?? string.Empty;
            public void Delete() => value = string.Empty;
        }
    }
}
