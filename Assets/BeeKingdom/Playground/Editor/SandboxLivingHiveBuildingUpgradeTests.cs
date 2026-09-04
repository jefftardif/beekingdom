using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;
using NUnit.Framework;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxLivingHiveBuildingUpgradeTests
    {
        private static readonly Guid PlayerId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly Guid HiveId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly Guid OperationId = Guid.Parse("99999999-8888-7777-6666-555555555555");

        public static void RunAllAssertions()
        {
            var tests = new SandboxLivingHiveBuildingUpgradeTests();
            tests.PresentationUsesOnlyServerOfferBalancesAndLevels();
            tests.MonotonicProjectionNeverAuthorizesCompletion();
            tests.OfflineReadOnlySnapshotBlocksEveryMutation();
            tests.MobileActionRowsAreBoundedAndKeepFortyFourPixelTargets();
            tests.PresenterQueueUsesServerAuthorityAndLocalizedCopy();
            tests.NetworkRetryReusesStartIdempotencyKey().GetAwaiter().GetResult();
            tests.ServerRefusalKeepsSpecificPlayerFacingCode().GetAwaiter().GetResult();
            tests.ControllerCompletesOnlyServerAwaitingOperation().GetAwaiter().GetResult();
            tests.ActiveUpgradeOutlinePulseUsesOnlyRunningServerOperation();
            tests.UpgradeOutlinePulseStaysVisibleAndKeepsExistingBlue();
        }

        [Test]
        public void PresentationUsesOnlyServerOfferBalancesAndLevels()
        {
            RemoteBuildingUpgradeSnapshot snapshot = Snapshot();
            HiveBuildingUpgradeScreenModel model = HiveBuildingUpgradePresentation.Ready(snapshot, TimeSpan.FromSeconds(5));

            Assert.That(model.LevelFor("wax_workshop"), Is.EqualTo(1));
            Assert.That(model.OfferFor("wax_workshop").Costs["honey"], Is.EqualTo(10));
            Assert.That(model.OfferFor("wax_workshop").Costs["pollen"], Is.EqualTo(20));
            Assert.That(model.CanStart("wax_workshop"), Is.True);

            snapshot.Balances["pollen"].Amount = 0;
            HiveBuildingUpgradeScreenModel insufficient = HiveBuildingUpgradePresentation.Ready(snapshot, TimeSpan.Zero);
            Assert.That(insufficient.CanStart("wax_workshop"), Is.False);
            Assert.That(insufficient.LevelFor("wax_workshop"), Is.EqualTo(1));
        }

        [Test]
        public void MonotonicProjectionNeverAuthorizesCompletion()
        {
            RemoteBuildingUpgradeSnapshot snapshot = SnapshotWithOperation(HiveBuildingUpgradeClient.RunningStatus);
            var model = HiveBuildingUpgradePresentation.Ready(snapshot, TimeSpan.FromSeconds(10));

            Assert.That(model.Remaining(TimeSpan.FromSeconds(10)), Is.EqualTo(TimeSpan.FromMinutes(10)));
            Assert.That(model.Remaining(TimeSpan.FromMinutes(15)), Is.Zero);
            Assert.That(model.Progress01(TimeSpan.FromMinutes(15)), Is.EqualTo(1d));
            Assert.That(model.CanComplete("wax_workshop"), Is.False,
                "The device clock may display zero, but only a server awaiting_completion state can authorize the button.");
        }

        [Test]
        public void OfflineReadOnlySnapshotBlocksEveryMutation()
        {
            RemoteBuildingUpgradeSnapshot idle = Snapshot();
            HiveBuildingUpgradeScreenModel idleOffline = HiveBuildingUpgradePresentation.OfflineReadOnly(
                idle, TimeSpan.Zero, idle.ServerTimeUtc);
            Assert.That(idleOffline.CanStart("wax_workshop"), Is.False);

            RemoteBuildingUpgradeSnapshot ready = SnapshotWithOperation(HiveBuildingUpgradeClient.AwaitingCompletionStatus);
            HiveBuildingUpgradeScreenModel readyOffline = HiveBuildingUpgradePresentation.OfflineReadOnly(
                ready, TimeSpan.Zero, ready.ServerTimeUtc);
            Assert.That(readyOffline.CanComplete("wax_workshop"), Is.False);
            Assert.That(readyOffline.IsReadOnly, Is.True);
        }

        [Test]
        public void MobileActionRowsAreBoundedAndKeepFortyFourPixelTargets()
        {
            RectAssert(HiveViewProductUiPresenter.OfficialBuildingUpgradeActionRectsForProof(
                true, 390f, 844f, true), 390f, 844f);
            RectAssert(HiveViewProductUiPresenter.OfficialBuildingUpgradeActionRectsForProof(
                false, 1600f, 900f, true), 1600f, 900f);
            RectAssert(HiveViewProductUiPresenter.OfficialBuildingUpgradeActionRectsForProof(
                true, 390f, 844f, false), 390f, 844f);
        }

        [Test]
        public void PresenterQueueUsesServerAuthorityAndLocalizedCopy()
        {
            RemoteBuildingUpgradeSnapshot snapshot = SnapshotWithOperation(HiveBuildingUpgradeClient.RunningStatus);
            try
            {
                HiveViewProductUiPresenter.UseBuildingUpgradeControllerForProof(new FakePanelController(
                    HiveBuildingUpgradePresentation.Ready(snapshot, TimeSpan.Zero), TimeSpan.FromMinutes(2)));
                string[] rows = HiveViewProductUiPresenter.QueueRailForProof();
                Assert.That(rows, Does.Contain("construction_authority:server"));
                Assert.That(rows, Does.Contain("construction_active:true"));
                Assert.That(rows, Does.Contain("construction_target:wax_workshop"));
                Assert.That(HiveViewProductUiPresenter.OfficialBuildingUpgradeForProof("wax_workshop"),
                    Does.Contain("building_upgrade_device_applies_level:false"));
            }
            finally
            {
                HiveViewProductUiPresenter.UseBuildingUpgradeControllerForProof(null);
            }

            string[] keys =
            {
                "building_upgrade.action.complete", "building_upgrade.status.running",
                "building_upgrade.status.offer", "building_upgrade.status.offline",
                "building_upgrade.error.network", "building_upgrade.disclosure",
                "building_upgrade.queue.ready"
            };
            foreach (string key in keys)
            {
                Assert.That(BeeKingdom.Localization.BeeLocalization.HasText("fr-CA", key), Is.True, key + " fr-CA");
                Assert.That(BeeKingdom.Localization.BeeLocalization.HasText("en-US", key), Is.True, key + " en-US");
            }
        }

        [Test]
        public async Task NetworkRetryReusesStartIdempotencyKey()
        {
            var client = new FakeClient(Snapshot());
            var keys = new FixedKeySource("stable-start-key");
            var clock = new FakeClock(TimeSpan.FromSeconds(2));
            using (var controller = new HiveBuildingUpgradePanelController(client, HiveId, keys, clock))
            {
                await controller.RefreshForProofAsync();
                client.StartSteps.Enqueue(new HivePerimeterClientException(
                    HivePerimeterClientError.TransportFailure, "game.network_unavailable"));
                client.StartSteps.Enqueue(StartResponse(3, "stable-start-key"));

                await controller.StartForProofAsync("wax_workshop");
                Assert.That(controller.Model.State, Is.EqualTo(HiveBuildingUpgradeScreenState.Error));
                Assert.That(controller.Model.CanStart("wax_workshop"), Is.True);
                await controller.StartForProofAsync("wax_workshop");

                Assert.That(client.StartKeys, Is.EqualTo(new[] { "stable-start-key", "stable-start-key" }));
                Assert.That(keys.Calls, Is.EqualTo(1));
                Assert.That(controller.Model.ActiveOperation.OperationId, Is.EqualTo(OperationId));
                Assert.That(controller.Model.State, Is.EqualTo(HiveBuildingUpgradeScreenState.Ready));
            }
        }

        [Test]
        public async Task ServerRefusalKeepsSpecificPlayerFacingCode()
        {
            var client = new FakeClient(Snapshot());
            client.StartSteps.Enqueue(new HivePerimeterClientException(
                HivePerimeterClientError.InvalidResponse, "game.insufficient_resources"));
            using (var controller = new HiveBuildingUpgradePanelController(
                client, HiveId, new FixedKeySource("refusal-key"), new FakeClock(TimeSpan.Zero)))
            {
                await controller.RefreshForProofAsync();
                await controller.StartForProofAsync("wax_workshop");
                Assert.That(controller.Model.State, Is.EqualTo(HiveBuildingUpgradeScreenState.Error));
                Assert.That(controller.Model.ErrorCode, Is.EqualTo("insufficient_resources"));
                Assert.That(controller.Model.CanStart("wax_workshop"), Is.False);
            }
        }

        [Test]
        public async Task ControllerCompletesOnlyServerAwaitingOperation()
        {
            RemoteBuildingUpgradeSnapshot awaiting = SnapshotWithOperation(
                HiveBuildingUpgradeClient.AwaitingCompletionStatus);
            var client = new FakeClient(awaiting);
            client.CompleteSteps.Enqueue(CompleteResponse(awaiting.Revision, "complete-key"));
            using (var controller = new HiveBuildingUpgradePanelController(
                client, HiveId, new FixedKeySource("complete-key"), new FakeClock(TimeSpan.Zero)))
            {
                await controller.RefreshForProofAsync();
                Assert.That(controller.Model.CanComplete("wax_workshop"), Is.True);
                await controller.CompleteForProofAsync();

                Assert.That(client.CompleteKeys.Single(), Is.EqualTo("complete-key"));
                Assert.That(controller.Model.ActiveOperation, Is.Null);
                Assert.That(controller.Model.LevelFor("wax_workshop"), Is.EqualTo(2));
                Assert.That(controller.Model.Offers, Is.Empty);
            }
        }

        [Test]
        public void ActiveUpgradeOutlinePulseUsesOnlyRunningServerOperation()
        {
            try
            {
                HiveViewProductUiPresenter.UseBuildingUpgradeControllerForProof(new FakePanelController(
                    HiveBuildingUpgradePresentation.Ready(
                        SnapshotWithOperation(HiveBuildingUpgradeClient.RunningStatus),
                        TimeSpan.Zero),
                    TimeSpan.FromMinutes(1)));
                Assert.That(HiveViewProductUiPresenter.ActiveOfficialUpgradeHotspotIdForExternalHost(),
                    Is.EqualTo("wax_workshop"));

                HiveViewProductUiPresenter.UseBuildingUpgradeControllerForProof(new FakePanelController(
                    HiveBuildingUpgradePresentation.Ready(
                        SnapshotWithOperation(HiveBuildingUpgradeClient.AwaitingCompletionStatus),
                        TimeSpan.Zero),
                    TimeSpan.FromMinutes(11)));
                Assert.That(HiveViewProductUiPresenter.ActiveOfficialUpgradeHotspotIdForExternalHost(),
                    Is.Null,
                    "Le pulse doit s'arreter en awaiting_completion pour préserver l'indicateur de validation.");

                HiveViewProductUiPresenter.UseBuildingUpgradeControllerForProof(new FakePanelController(
                    HiveBuildingUpgradePresentation.Ready(Snapshot(), TimeSpan.Zero),
                    TimeSpan.Zero));
                Assert.That(HiveViewProductUiPresenter.ActiveOfficialUpgradeHotspotIdForExternalHost(), Is.Null);
            }
            finally
            {
                HiveViewProductUiPresenter.UseBuildingUpgradeControllerForProof(null);
            }
        }

        [Test]
        public void UpgradeOutlinePulseStaysVisibleAndKeepsExistingBlue()
        {
            float alphaMin = float.MaxValue;
            float alphaMax = float.MinValue;
            float intensityMin = float.MaxValue;
            float intensityMax = float.MinValue;
            float widthMin = float.MaxValue;
            float widthMax = float.MinValue;
            for (int i = 0; i < 32; i++)
            {
                float alpha = HiveMapBuildingUpgradeVisualStateBootstrap.UpgradeOutlinePulseAlpha(i * 0.125f);
                float intensity = HiveMapBuildingUpgradeVisualStateBootstrap.UpgradeOutlinePulseIntensity(i * 0.125f);
                float width = HiveMapBuildingUpgradeVisualStateBootstrap.UpgradeOutlinePulseWidth(i * 0.125f);
                alphaMin = Math.Min(alphaMin, alpha);
                alphaMax = Math.Max(alphaMax, alpha);
                intensityMin = Math.Min(intensityMin, intensity);
                intensityMax = Math.Max(intensityMax, intensity);
                widthMin = Math.Min(widthMin, width);
                widthMax = Math.Max(widthMax, width);
            }

            Assert.That(alphaMin, Is.GreaterThanOrEqualTo(0.86f));
            Assert.That(alphaMax, Is.LessThanOrEqualTo(1f));
            Assert.That(widthMin, Is.GreaterThanOrEqualTo(4.5f));
            Assert.That(widthMax, Is.LessThanOrEqualTo(8.1f));
            Assert.That(widthMax - widthMin, Is.GreaterThan(3f));
            Assert.That(intensityMin, Is.GreaterThanOrEqualTo(1f));
            Assert.That(intensityMax, Is.LessThanOrEqualTo(1.5f));
            Assert.That(intensityMax - intensityMin, Is.GreaterThan(0.4f));

            UnityEngine.Color color = HiveMapBuildingUpgradeVisualStateBootstrap.UpgradeOutlinePulseColorForProof(0.5f);
            Assert.That(color.r, Is.EqualTo(0.35f).Within(0.001f));
            Assert.That(color.g, Is.EqualTo(0.75f).Within(0.001f));
            Assert.That(color.b, Is.EqualTo(1f).Within(0.001f));
        }

        private static RemoteBuildingUpgradeSnapshot Snapshot()
        {
            DateTimeOffset now = new DateTimeOffset(2026, 7, 22, 12, 0, 0, TimeSpan.Zero);
            return new RemoteBuildingUpgradeSnapshot
            {
                PlayerId = PlayerId,
                HiveId = HiveId,
                ContractVersion = HiveBuildingUpgradeClient.ContractVersion,
                CatalogVersion = "test-v1",
                Revision = 3,
                ServerTimeUtc = now,
                Balances = new Dictionary<string, RemoteBuildingUpgradeBalance>
                {
                    ["honey"] = new RemoteBuildingUpgradeBalance { Amount = 100, Capacity = 1000 },
                    ["pollen"] = new RemoteBuildingUpgradeBalance { Amount = 100, Capacity = 1000 }
                },
                BuildingLevels = new Dictionary<string, int> { ["wax_workshop"] = 1 },
                Offers = new List<RemoteBuildingUpgradeOffer>
                {
                    new RemoteBuildingUpgradeOffer
                    {
                        BuildingKey = "wax_workshop", FromLevel = 1, ToLevel = 2,
                        Duration = TimeSpan.FromMinutes(10),
                        Costs = new Dictionary<string, long> { ["honey"] = 10, ["pollen"] = 20 }
                    }
                }
            };
        }

        private static void RectAssert(UnityEngine.Rect[] rects, float width, float height)
        {
            Assert.That(rects, Is.Not.Empty);
            foreach (UnityEngine.Rect rect in rects)
            {
                Assert.That(rect.width, Is.GreaterThanOrEqualTo(44f));
                Assert.That(rect.height, Is.GreaterThanOrEqualTo(44f));
                Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(0f));
                Assert.That(rect.yMin, Is.GreaterThanOrEqualTo(0f));
                Assert.That(rect.xMax, Is.LessThanOrEqualTo(width));
                Assert.That(rect.yMax, Is.LessThanOrEqualTo(height));
            }
        }

        private static RemoteBuildingUpgradeSnapshot SnapshotWithOperation(string status)
        {
            RemoteBuildingUpgradeSnapshot snapshot = Snapshot();
            snapshot.Revision = 4;
            DateTimeOffset completes = snapshot.ServerTimeUtc.AddMinutes(10);
            if (status == HiveBuildingUpgradeClient.AwaitingCompletionStatus)
            {
                completes = snapshot.ServerTimeUtc.AddMinutes(-1);
            }
            snapshot.ActiveOperation = new RemoteBuildingUpgradeOperation
            {
                OperationId = OperationId,
                BuildingKey = "wax_workshop",
                FromLevel = 1,
                ToLevel = 2,
                StartedAtUtc = completes.AddMinutes(-10),
                CompletesAtUtc = completes,
                Status = status
            };
            return snapshot;
        }

        private static RemoteBuildingUpgradeMutationResponse StartResponse(long expectedRevision, string key)
        {
            RemoteBuildingUpgradeSnapshot snapshot = SnapshotWithOperation(HiveBuildingUpgradeClient.RunningStatus);
            snapshot.Revision = expectedRevision + 1;
            return Response(snapshot, key, HiveBuildingUpgradeClient.StartedCode);
        }

        private static RemoteBuildingUpgradeMutationResponse CompleteResponse(long expectedRevision, string key)
        {
            RemoteBuildingUpgradeSnapshot snapshot = Snapshot();
            snapshot.Revision = expectedRevision + 1;
            snapshot.BuildingLevels["wax_workshop"] = 2;
            snapshot.Offers.Clear();
            return Response(snapshot, key, HiveBuildingUpgradeClient.CompletedCode);
        }

        private static RemoteBuildingUpgradeMutationResponse Response(
            RemoteBuildingUpgradeSnapshot snapshot,
            string key,
            string code)
        {
            return new RemoteBuildingUpgradeMutationResponse
            {
                Snapshot = snapshot,
                Receipt = new RemoteBuildingUpgradeReceipt
                {
                    PlayerId = PlayerId, HiveId = HiveId, IdempotencyKey = key, OperationId = OperationId,
                    BuildingKey = "wax_workshop", FromLevel = 1, ToLevel = 2,
                    Revision = snapshot.Revision, AcceptedAtUtc = snapshot.ServerTimeUtc, Code = code
                }
            };
        }

        private sealed class FakeClient : IHiveBuildingUpgradeClient
        {
            private RemoteBuildingUpgradeSnapshot snapshot;
            public FakeClient(RemoteBuildingUpgradeSnapshot snapshot) { this.snapshot = snapshot; }
            public Queue<object> StartSteps { get; } = new Queue<object>();
            public Queue<object> CompleteSteps { get; } = new Queue<object>();
            public List<string> StartKeys { get; } = new List<string>();
            public List<string> CompleteKeys { get; } = new List<string>();
            public GameReadSource LastReadSource { get; set; } = GameReadSource.Server;
            public DateTimeOffset LastReadCachedAtUtc { get; set; }
            public Task<RemoteBuildingUpgradeSnapshot> ReadAsync(Guid hiveId, CancellationToken cancellationToken = default(CancellationToken))
            { cancellationToken.ThrowIfCancellationRequested(); return Task.FromResult(snapshot); }
            public Task<RemoteBuildingUpgradeMutationResponse> StartAsync(Guid hiveId, string buildingKey, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default(CancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested(); StartKeys.Add(idempotencyKey);
                return Step(StartSteps, value => snapshot = value.Snapshot);
            }
            public Task<RemoteBuildingUpgradeMutationResponse> CompleteAsync(Guid hiveId, Guid operationId, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default(CancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested(); CompleteKeys.Add(idempotencyKey);
                return Step(CompleteSteps, value => snapshot = value.Snapshot);
            }
            private static Task<RemoteBuildingUpgradeMutationResponse> Step(
                Queue<object> steps,
                Action<RemoteBuildingUpgradeMutationResponse> accepted)
            {
                object step = steps.Dequeue();
                if (step is Exception failure) throw failure;
                var response = (RemoteBuildingUpgradeMutationResponse)step;
                accepted(response);
                return Task.FromResult(response);
            }
        }

        private sealed class FixedKeySource : IHiveBuildingUpgradeMutationKeySource
        {
            private readonly string key;
            public FixedKeySource(string key) { this.key = key; }
            public int Calls { get; private set; }
            public string Create(string operation) { Calls++; return key; }
        }

        private sealed class FakePanelController : IHiveBuildingUpgradePanelController
        {
            public FakePanelController(HiveBuildingUpgradeScreenModel model, TimeSpan elapsed)
            { Model = model; Elapsed = elapsed; }
            public HiveBuildingUpgradeScreenModel Model { get; }
            public bool IsConfigured => true;
            public bool IsBusy => false;
            public TimeSpan Elapsed { get; }
            public void Refresh() { }
            public void Start(string buildingKey) { }
            public void Complete() { }
        }

        private sealed class FakeClock : IHiveBuildingUpgradeMonotonicClock
        {
            public FakeClock(TimeSpan elapsed) { Elapsed = elapsed; }
            public TimeSpan Elapsed { get; set; }
        }
    }
}
