using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;
using NUnit.Framework;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxLivingHiveOfficialResearchTests
    {
        private static readonly Guid PlayerId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly Guid HiveId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly Guid OperationId = Guid.Parse("99999999-8888-7777-6666-555555555555");

        public static void RunAllAssertions()
        {
            var tests = new SandboxLivingHiveOfficialResearchTests();
            tests.PresentationUsesOnlyServerCostsBalancesAndEffects();
            tests.MonotonicProjectionNeverAuthorizesCompletion();
            tests.OfflineReadOnlySnapshotBlocksEveryMutation();
            tests.MutatingOneOfferNeverBlanksTheOthers();
            tests.MobileResearchRowsAreBoundedAndKeepFortyFourPixelTargets();
            tests.PresenterQueueUsesServerAuthorityAndLocalizedCopy();
            tests.NetworkRetryReusesStartIdempotencyKey().GetAwaiter().GetResult();
            tests.ServerRefusalKeepsSpecificPlayerFacingCode().GetAwaiter().GetResult();
            tests.ControllerCompletesOnlyServerAwaitingOperation().GetAwaiter().GetResult();
            tests.RevisionConflictOnStartAutoRefreshesAndRetriesTransparently().GetAwaiter().GetResult();
            tests.RevisionConflictOnStartTwiceInARowSurfacesAsError().GetAwaiter().GetResult();
            tests.RevisionConflictOnCompleteAutoRefreshesAndRetriesTransparently().GetAwaiter().GetResult();
            tests.QuietRefreshNeverShowsLoadingState().GetAwaiter().GetResult();
        }

        [Test]
        public void PresentationUsesOnlyServerCostsBalancesAndEffects()
        {
            RemoteHiveResearchSnapshot snapshot = Snapshot();
            HiveResearchScreenModel model = HiveResearchPresentation.Ready(snapshot, TimeSpan.FromSeconds(5));

            Assert.That(model.OfferFor(HiveResearchClient.ForagingRoutesId).Costs["honey"], Is.EqualTo(240));
            Assert.That(model.OfferFor(HiveResearchClient.ForagingRoutesId).HoneyProductionBonusBps, Is.EqualTo(200));
            Assert.That(model.CanStart(HiveResearchClient.ForagingRoutesId), Is.True);

            snapshot.Balances["pollen"].Amount = 0;
            HiveResearchScreenModel insufficient = HiveResearchPresentation.Ready(snapshot, TimeSpan.Zero);
            Assert.That(insufficient.CanStart(HiveResearchClient.ForagingRoutesId), Is.False);
            Assert.That(insufficient.ShortageResource(HiveResearchClient.ForagingRoutesId), Is.EqualTo("pollen"));
            Assert.That(insufficient.MissingAmount(HiveResearchClient.ForagingRoutesId, "pollen"), Is.EqualTo(90));
        }

        [Test]
        public void MonotonicProjectionNeverAuthorizesCompletion()
        {
            RemoteHiveResearchSnapshot snapshot = SnapshotWithOperation(HiveResearchClient.RunningStatus);
            var model = HiveResearchPresentation.Ready(snapshot, TimeSpan.FromSeconds(10));

            Assert.That(model.Remaining(TimeSpan.FromSeconds(10)), Is.EqualTo(TimeSpan.FromMinutes(4)));
            Assert.That(model.Remaining(TimeSpan.FromMinutes(10)), Is.EqualTo(TimeSpan.Zero));
            Assert.That(model.Progress01(TimeSpan.FromMinutes(10)), Is.EqualTo(1d));
            Assert.That(model.CanComplete(HiveResearchClient.ForagingRoutesId), Is.False,
                "The device projection may reach zero, but only server awaiting_completion authorizes completion.");
        }

        [Test]
        public void MutatingOneOfferNeverBlanksTheOthers()
        {
            RemoteHiveResearchSnapshot snapshot = Snapshot();
            HiveResearchScreenModel starting = HiveResearchPresentation.Mutating(
                HiveResearchScreenState.Starting, snapshot, TimeSpan.Zero, HiveResearchClient.ForagingRoutesId);

            Assert.That(starting.IsMutating(HiveResearchClient.ForagingRoutesId), Is.True);
            Assert.That(starting.IsMutating(HiveResearchClient.TemperedCombsId), Is.False,
                "A different offer must not report itself as mutating while another study starts.");
            Assert.That(starting.OfferFor(HiveResearchClient.TemperedCombsId).Costs["honey"], Is.EqualTo(180),
                "The untouched offer must keep its real cost data available for display while another study starts.");

            HiveResearchScreenModel completing = HiveResearchPresentation.Mutating(
                HiveResearchScreenState.Completing, snapshot, TimeSpan.Zero, HiveResearchClient.ForagingRoutesId);
            Assert.That(completing.IsMutating(HiveResearchClient.ForagingRoutesId), Is.True);
            Assert.That(completing.IsMutating(HiveResearchClient.TemperedCombsId), Is.False);
        }

        [Test]
        public void OfflineReadOnlySnapshotBlocksEveryMutation()
        {
            RemoteHiveResearchSnapshot idle = Snapshot();
            HiveResearchScreenModel idleOffline = HiveResearchPresentation.OfflineReadOnly(
                idle, TimeSpan.Zero, idle.ServerTimeUtc);
            Assert.That(idleOffline.CanStart(HiveResearchClient.ForagingRoutesId), Is.False);

            RemoteHiveResearchSnapshot ready = SnapshotWithOperation(HiveResearchClient.AwaitingCompletionStatus);
            HiveResearchScreenModel readyOffline = HiveResearchPresentation.OfflineReadOnly(
                ready, TimeSpan.Zero, ready.ServerTimeUtc);
            Assert.That(readyOffline.CanComplete(HiveResearchClient.ForagingRoutesId), Is.False);
            Assert.That(readyOffline.IsReadOnly, Is.True);
        }

        [Test]
        public void MobileResearchRowsAreBoundedAndKeepFortyFourPixelTargets()
        {
            RectAssert(HiveViewProductUiPresenter.OfficialResearchActionRectsForProof(true, 390f, 844f), 390f, 844f);
            RectAssert(HiveViewProductUiPresenter.OfficialResearchActionRectsForProof(false, 1600f, 900f), 1600f, 900f);
        }

        [Test]
        public void PresenterQueueUsesServerAuthorityAndLocalizedCopy()
        {
            RemoteHiveResearchSnapshot snapshot = SnapshotWithOperation(HiveResearchClient.RunningStatus);
            try
            {
                HiveViewProductUiPresenter.UseResearchControllerForProof(new FakePanelController(
                    HiveResearchPresentation.Ready(snapshot, TimeSpan.Zero), TimeSpan.FromMinutes(1)));
                string[] rows = HiveViewProductUiPresenter.QueueRailForProof();
                Assert.That(rows, Does.Contain("research_authority:server"));
                Assert.That(rows, Does.Contain("research_active:true"));
                Assert.That(HiveViewProductUiPresenter.OfficialResearchForProof(HiveResearchClient.ForagingRoutesId),
                    Does.Contain("research_official_local_effect:false"));
            }
            finally
            {
                HiveViewProductUiPresenter.UseResearchControllerForProof(null);
            }

            string[] keys =
            {
                "research.official.action.complete", "research.official.status.running",
                "research.official.status.offer", "research.official.status.offline",
                "research.official.error.network", "research.official.disclosure",
                "research.official.queue.ready"
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
            using (var controller = new HiveResearchPanelController(client, HiveId, keys, new FakeClock(TimeSpan.FromSeconds(2))))
            {
                await controller.RefreshForProofAsync();
                client.StartSteps.Enqueue(new HivePerimeterClientException(
                    HivePerimeterClientError.TransportFailure, "game.network_unavailable"));
                client.StartSteps.Enqueue(StartResponse(3, "stable-start-key"));

                await controller.StartForProofAsync(HiveResearchClient.ForagingRoutesId);
                Assert.That(controller.Model.State, Is.EqualTo(HiveResearchScreenState.Error));
                Assert.That(controller.Model.CanStart(HiveResearchClient.ForagingRoutesId), Is.True);
                await controller.StartForProofAsync(HiveResearchClient.ForagingRoutesId);

                Assert.That(client.StartKeys, Is.EqualTo(new[] { "stable-start-key", "stable-start-key" }));
                Assert.That(keys.Calls, Is.EqualTo(1));
                Assert.That(controller.Model.ActiveOperation.OperationId, Is.EqualTo(OperationId));
            }
        }

        [Test]
        public async Task ServerRefusalKeepsSpecificPlayerFacingCode()
        {
            var client = new FakeClient(Snapshot());
            client.StartSteps.Enqueue(new HivePerimeterClientException(
                HivePerimeterClientError.InvalidResponse, "game.insufficient_resources"));
            using (var controller = new HiveResearchPanelController(
                client, HiveId, new FixedKeySource("refusal-key"), new FakeClock(TimeSpan.Zero)))
            {
                await controller.RefreshForProofAsync();
                await controller.StartForProofAsync(HiveResearchClient.ForagingRoutesId);
                Assert.That(controller.Model.State, Is.EqualTo(HiveResearchScreenState.Error));
                Assert.That(controller.Model.ErrorCode, Is.EqualTo("insufficient_resources"));
                Assert.That(controller.Model.CanStart(HiveResearchClient.ForagingRoutesId), Is.False);
            }
        }

        [Test]
        public async Task ControllerCompletesOnlyServerAwaitingOperation()
        {
            RemoteHiveResearchSnapshot awaiting = SnapshotWithOperation(HiveResearchClient.AwaitingCompletionStatus);
            var client = new FakeClient(awaiting);
            client.CompleteSteps.Enqueue(CompleteResponse(awaiting.Revision, "complete-key"));
            using (var controller = new HiveResearchPanelController(
                client, HiveId, new FixedKeySource("complete-key"), new FakeClock(TimeSpan.Zero)))
            {
                await controller.RefreshForProofAsync();
                Assert.That(controller.Model.CanComplete(HiveResearchClient.ForagingRoutesId), Is.True);
                await controller.CompleteForProofAsync();

                Assert.That(client.CompleteKeys.Single(), Is.EqualTo("complete-key"));
                Assert.That(controller.Model.ActiveOperation, Is.Null);
                Assert.That(controller.Model.IsCompleted(HiveResearchClient.ForagingRoutesId), Is.True);
                Assert.That(controller.Model.OfferFor(HiveResearchClient.ForagingRoutesId), Is.Null);
            }
        }

        // M040-CL: revision_conflict is almost never a real double-action conflict - Revision
        // is global to the hive (bumped by unrelated production ticks, upgrades, etc.), so the
        // client's cached copy is very often stale by the time the player actually clicks. The
        // player must never see this as an error: a silent refresh + one transparent retry
        // should just make their original click succeed.
        [Test]
        public async Task RevisionConflictOnStartAutoRefreshesAndRetriesTransparently()
        {
            var client = new FakeClient(Snapshot()); // Revision = 3
            using (var controller = new HiveResearchPanelController(
                client, HiveId, new FixedKeySource("start-key"), new FakeClock(TimeSpan.Zero)))
            {
                await controller.RefreshForProofAsync();
                Assert.That(controller.Model.Revision, Is.EqualTo(3));

                client.StartSteps.Enqueue(new HivePerimeterClientException(
                    HivePerimeterClientError.InvalidResponse, "game.revision_conflict"));
                // Simulates an unrelated server-side mutation (a production tick) bumping the
                // hive's real revision between the player's last fetch and their click.
                client.BumpSnapshotRevision(4);
                client.StartSteps.Enqueue(StartResponse(4, "start-key"));

                await controller.StartForProofAsync(HiveResearchClient.ForagingRoutesId);

                Assert.That(controller.Model.State, Is.EqualTo(HiveResearchScreenState.Ready),
                    "The player must never see an Error state from a plain revision_conflict.");
                Assert.That(controller.Model.ErrorCode, Is.Empty);
                Assert.That(controller.Model.ActiveOperation, Is.Not.Null,
                    "The retried Start must have actually gone through server-side.");
                Assert.That(controller.Model.ActiveOperation.ResearchId, Is.EqualTo(HiveResearchClient.ForagingRoutesId));
                Assert.That(client.StartSteps, Is.Empty, "Both the failing and the retried Start call must have been consumed.");
            }
        }

        [Test]
        public async Task RevisionConflictOnStartTwiceInARowSurfacesAsError()
        {
            var client = new FakeClient(Snapshot());
            using (var controller = new HiveResearchPanelController(
                client, HiveId, new FixedKeySource("start-key"), new FakeClock(TimeSpan.Zero)))
            {
                await controller.RefreshForProofAsync();

                // A second, genuine conflict on the retry itself (not just a stale cache) -
                // the single automatic retry must not loop forever; it should surface normally.
                client.StartSteps.Enqueue(new HivePerimeterClientException(
                    HivePerimeterClientError.InvalidResponse, "game.revision_conflict"));
                client.StartSteps.Enqueue(new HivePerimeterClientException(
                    HivePerimeterClientError.InvalidResponse, "game.revision_conflict"));

                await controller.StartForProofAsync(HiveResearchClient.ForagingRoutesId);

                Assert.That(controller.Model.State, Is.EqualTo(HiveResearchScreenState.Error));
                Assert.That(controller.Model.ErrorCode, Is.EqualTo("revision_conflict"));
                Assert.That(client.StartSteps, Is.Empty,
                    "Exactly one retry must be attempted, not an unbounded loop.");
            }
        }

        [Test]
        public async Task RevisionConflictOnCompleteAutoRefreshesAndRetriesTransparently()
        {
            RemoteHiveResearchSnapshot awaiting = SnapshotWithOperation(HiveResearchClient.AwaitingCompletionStatus);
            var client = new FakeClient(awaiting);
            using (var controller = new HiveResearchPanelController(
                client, HiveId, new FixedKeySource("complete-key"), new FakeClock(TimeSpan.Zero)))
            {
                await controller.RefreshForProofAsync();
                Assert.That(controller.Model.CanComplete(HiveResearchClient.ForagingRoutesId), Is.True);

                client.CompleteSteps.Enqueue(new HivePerimeterClientException(
                    HivePerimeterClientError.InvalidResponse, "game.revision_conflict"));
                client.BumpSnapshotRevision(awaiting.Revision + 1);
                client.CompleteSteps.Enqueue(CompleteResponse(awaiting.Revision + 1, "complete-key"));

                await controller.CompleteForProofAsync();

                Assert.That(controller.Model.State, Is.EqualTo(HiveResearchScreenState.Ready));
                Assert.That(controller.Model.ErrorCode, Is.Empty);
                Assert.That(controller.Model.IsCompleted(HiveResearchClient.ForagingRoutesId), Is.True,
                    "The retried Complete must have actually gone through server-side.");
            }
        }

        [Test]
        public async Task QuietRefreshNeverShowsLoadingState()
        {
            var client = new FakeClient(Snapshot());
            using (var controller = new HiveResearchPanelController(
                client, HiveId, new FixedKeySource("k"), new FakeClock(TimeSpan.Zero)))
            {
                await controller.RefreshForProofAsync();
                Assert.That(controller.Model.State, Is.EqualTo(HiveResearchScreenState.Ready));

                var statesObserved = new List<HiveResearchScreenState>();
                client.OnReadAsync = () => statesObserved.Add(controller.Model.State);
                await controller.RefreshQuietlyForProofAsync();

                Assert.That(statesObserved.Contains(HiveResearchScreenState.Loading), Is.False,
                    "A background poll the player never asked for must never flash the whole window to Loading.");
                Assert.That(controller.Model.State, Is.EqualTo(HiveResearchScreenState.Ready));
            }
        }

        private static RemoteHiveResearchSnapshot Snapshot()
        {
            DateTimeOffset now = new DateTimeOffset(2026, 7, 22, 12, 0, 0, TimeSpan.Zero);
            return new RemoteHiveResearchSnapshot
            {
                PlayerId = PlayerId,
                HiveId = HiveId,
                ContractVersion = HiveResearchClient.ContractVersion,
                CatalogVersion = "test-v1",
                Revision = 3,
                ServerTimeUtc = now,
                Balances = new Dictionary<string, RemoteHiveResearchBalance>
                {
                    ["honey"] = new RemoteHiveResearchBalance { Amount = 500, Capacity = 1000 },
                    ["pollen"] = new RemoteHiveResearchBalance { Amount = 500, Capacity = 1000 }
                },
                Completed = new List<RemoteHiveResearchCompletion>(),
                Offers = new List<RemoteHiveResearchOffer>
                {
                    new RemoteHiveResearchOffer
                    {
                        ResearchId = HiveResearchClient.ForagingRoutesId,
                        Duration = TimeSpan.FromMinutes(4),
                        Costs = new Dictionary<string, long> { ["honey"] = 240, ["pollen"] = 90 },
                        Effects = new RemoteHiveResearchEffects { HoneyProductionBonusBps = 200 }
                    },
                    new RemoteHiveResearchOffer
                    {
                        ResearchId = HiveResearchClient.TemperedCombsId,
                        Duration = TimeSpan.FromMinutes(3),
                        Costs = new Dictionary<string, long> { ["honey"] = 180, ["pollen"] = 120 },
                        Effects = new RemoteHiveResearchEffects { WaxCapacityBonusBps = 500 }
                    }
                }
            };
        }

        private static RemoteHiveResearchSnapshot SnapshotWithOperation(string status)
        {
            RemoteHiveResearchSnapshot snapshot = Snapshot();
            snapshot.Revision = 4;
            DateTimeOffset completes = snapshot.ServerTimeUtc.AddMinutes(4);
            if (status == HiveResearchClient.AwaitingCompletionStatus) completes = snapshot.ServerTimeUtc.AddMinutes(-1);
            snapshot.ActiveOperation = new RemoteHiveResearchOperation
            {
                OperationId = OperationId,
                ResearchId = HiveResearchClient.ForagingRoutesId,
                StartedAtUtc = completes.AddMinutes(-4),
                CompletesAtUtc = completes,
                Status = status
            };
            return snapshot;
        }

        private static RemoteHiveResearchMutationResponse StartResponse(long expectedRevision, string key)
        {
            RemoteHiveResearchSnapshot snapshot = SnapshotWithOperation(HiveResearchClient.RunningStatus);
            snapshot.Revision = expectedRevision + 1;
            return Response(snapshot, key, HiveResearchClient.StartedCode);
        }

        private static RemoteHiveResearchMutationResponse CompleteResponse(long expectedRevision, string key)
        {
            RemoteHiveResearchSnapshot snapshot = Snapshot();
            snapshot.Revision = expectedRevision + 1;
            snapshot.Completed.Add(new RemoteHiveResearchCompletion
            {
                ResearchId = HiveResearchClient.ForagingRoutesId,
                CompletedAtUtc = snapshot.ServerTimeUtc,
                Effects = new RemoteHiveResearchEffects { HoneyProductionBonusBps = 200 }
            });
            snapshot.Offers.RemoveAll(item => item.ResearchId == HiveResearchClient.ForagingRoutesId);
            return Response(snapshot, key, HiveResearchClient.CompletedCode);
        }

        private static RemoteHiveResearchMutationResponse Response(RemoteHiveResearchSnapshot snapshot, string key, string code)
        {
            return new RemoteHiveResearchMutationResponse
            {
                Snapshot = snapshot,
                Receipt = new RemoteHiveResearchReceipt
                {
                    PlayerId = PlayerId,
                    HiveId = HiveId,
                    IdempotencyKey = key,
                    OperationId = OperationId,
                    ResearchId = HiveResearchClient.ForagingRoutesId,
                    Revision = snapshot.Revision,
                    AcceptedAtUtc = snapshot.ServerTimeUtc,
                    Code = code
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

        private sealed class FakeClient : IHiveResearchClient
        {
            private RemoteHiveResearchSnapshot snapshot;
            public FakeClient(RemoteHiveResearchSnapshot snapshot) { this.snapshot = snapshot; }
            public Queue<object> StartSteps { get; } = new Queue<object>();
            public Queue<object> CompleteSteps { get; } = new Queue<object>();
            public List<string> StartKeys { get; } = new List<string>();
            public List<string> CompleteKeys { get; } = new List<string>();
            public GameReadSource LastReadSource { get; set; } = GameReadSource.Server;
            public DateTimeOffset LastReadCachedAtUtc { get; set; }
            public Action OnReadAsync { get; set; }
            public void BumpSnapshotRevision(long newRevision) { snapshot.Revision = newRevision; }
            public Task<RemoteHiveResearchSnapshot> ReadAsync(Guid hiveId, CancellationToken cancellationToken = default(CancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                OnReadAsync?.Invoke();
                return Task.FromResult(snapshot);
            }
            public Task<RemoteHiveResearchMutationResponse> StartAsync(Guid hiveId, string researchId, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default(CancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                StartKeys.Add(idempotencyKey);
                return Step(StartSteps, value => snapshot = value.Snapshot);
            }
            public Task<RemoteHiveResearchMutationResponse> CompleteAsync(Guid hiveId, Guid operationId, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default(CancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                CompleteKeys.Add(idempotencyKey);
                return Step(CompleteSteps, value => snapshot = value.Snapshot);
            }
            private static Task<RemoteHiveResearchMutationResponse> Step(
                Queue<object> steps, Action<RemoteHiveResearchMutationResponse> accepted)
            {
                object step = steps.Dequeue();
                if (step is Exception failure) throw failure;
                var response = (RemoteHiveResearchMutationResponse)step;
                accepted(response);
                return Task.FromResult(response);
            }
        }

        private sealed class FixedKeySource : IHiveResearchMutationKeySource
        {
            private readonly string key;
            public FixedKeySource(string key) { this.key = key; }
            public int Calls { get; private set; }
            public string Create(string operation) { Calls++; return key; }
        }

        private sealed class FakePanelController : IHiveResearchPanelController
        {
            public FakePanelController(HiveResearchScreenModel model, TimeSpan elapsed)
            { Model = model; Elapsed = elapsed; }
            public HiveResearchScreenModel Model { get; }
            public bool IsConfigured => true;
            public bool IsBusy => false;
            public TimeSpan Elapsed { get; }
            public void Refresh() { }
            public void RefreshQuietly() { }
            public void Start(string researchId) { }
            public void Complete() { }
        }

        private sealed class FakeClock : IHiveResearchMonotonicClock
        {
            public FakeClock(TimeSpan elapsed) { Elapsed = elapsed; }
            public TimeSpan Elapsed { get; set; }
        }
    }
}
