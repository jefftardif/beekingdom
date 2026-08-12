using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxLivingHiveOfficialBroodCareTests
    {
        private static readonly Guid PlayerId =
            Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly Guid HiveId =
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly Guid OperationId =
            Guid.Parse("99999999-8888-7777-6666-555555555555");
        private static readonly DateTimeOffset Now =
            new DateTimeOffset(2026, 7, 23, 10, 0, 0, TimeSpan.Zero);

        public static void RunAllAssertions()
        {
            var tests = new SandboxLivingHiveOfficialBroodCareTests();
            tests.NotConfiguredProofNeverClaimsLocalAuthority();
            tests.UninitializedSnapshotStaysHonestAndActionless();
            tests.ReadyAndOfflineModelsRespectTheServerBoundary();
            tests.ServerTimerUsesMonotonicElapsedTime();
            tests.PortraitAndLandscapeCareTargetsRemainFortyFourPixels();
            tests.OfficialBroodCareCopyExistsInBothCatalogs();
        }

        [Test]
        public void NotConfiguredProofNeverClaimsLocalAuthority()
        {
            try
            {
                HiveViewProductUiPresenter.UseBroodVitalityControllerForProof(
                    null);
                string[] rows =
                    HiveViewProductUiPresenter.OfficialBroodVitalityForProof();

                Assert.That(
                    rows,
                    Does.Contain("brood_vitality_authority:server"));
                Assert.That(
                    rows,
                    Does.Contain("brood_vitality_initialized:false"));
                Assert.That(
                    rows,
                    Does.Contain("brood_care_local_resource_debit:false"));
                Assert.That(
                    rows,
                    Does.Contain("brood_care_local_vitality_credit:false"));
                Assert.That(
                    rows,
                    Does.Contain("brood_care_auto_submit:false"));
            }
            finally
            {
                HiveViewProductUiPresenter.UseBroodVitalityControllerForProof(
                    null);
            }
        }

        [Test]
        public void UninitializedSnapshotStaysHonestAndActionless()
        {
            RemoteBroodVitalitySnapshot snapshot = Snapshot();
            snapshot.Vitality = null;
            HiveBroodVitalityScreenModel model =
                HiveBroodVitalityPresentation.Project(
                    HiveBroodVitalityScreenState.Ready,
                    snapshot,
                    TimeSpan.Zero,
                    true);

            Assert.That(model.Initialized, Is.False);
            Assert.That(model.Tier, Is.EqualTo("uninitialized"));
            Assert.That(
                model.CanStart(HiveBroodVitalityClient.FeedingType),
                Is.False);
            Assert.That(
                model.CanStart(
                    HiveBroodVitalityClient.StabilizationType),
                Is.False);
        }

        [Test]
        public void ReadyAndOfflineModelsRespectTheServerBoundary()
        {
            RemoteBroodVitalitySnapshot snapshot = Snapshot();
            snapshot.Vitality.ActiveOperation = null;
            HiveBroodVitalityScreenModel ready =
                HiveBroodVitalityPresentation.Project(
                    HiveBroodVitalityScreenState.Ready,
                    snapshot,
                    TimeSpan.Zero,
                    true);
            HiveBroodVitalityScreenModel offline =
                HiveBroodVitalityPresentation.Project(
                    HiveBroodVitalityScreenState.OfflineReadOnly,
                    snapshot,
                    TimeSpan.Zero,
                    true,
                    cachedAtUtc: Now.AddMinutes(1));

            Assert.That(ready.Nutrition, Is.EqualTo(72));
            Assert.That(ready.Stability, Is.EqualTo(81));
            Assert.That(
                ready.CanStart(HiveBroodVitalityClient.FeedingType),
                Is.True);
            Assert.That(offline.IsReadOnly, Is.True);
            Assert.That(
                offline.CanStart(HiveBroodVitalityClient.FeedingType),
                Is.False);
        }

        [Test]
        public void ServerTimerUsesMonotonicElapsedTime()
        {
            HiveBroodVitalityScreenModel model =
                HiveBroodVitalityPresentation.Project(
                    HiveBroodVitalityScreenState.Ready,
                    Snapshot(),
                    TimeSpan.FromSeconds(4),
                    true);

            Assert.That(
                model.Remaining(TimeSpan.FromSeconds(4)).TotalSeconds,
                Is.EqualTo(12).Within(0.001));
            Assert.That(
                model.Remaining(TimeSpan.FromSeconds(10)).TotalSeconds,
                Is.EqualTo(6).Within(0.001));
            Assert.That(
                model.CanComplete(TimeSpan.FromSeconds(16)),
                Is.True);
        }

        [Test]
        public async Task PreparedMutationIsStoredBeforeTransportAndDeletedAfterSuccess()
        {
            var store = new MemoryStore();
            var clock = new FakeClock();
            var fake = new FakeClient(store)
            {
                ReadResult = ReadySnapshot(),
                StartResult = StartResponse()
            };
            var controller = NewController(fake, store, clock);
            await controller.RefreshForProofAsync();

            await controller.StartForProofAsync(
                HiveBroodVitalityClient.FeedingType);

            Assert.That(fake.StoreWasPreparedBeforeStart, Is.True);
            Assert.That(fake.StartKeys.Count, Is.EqualTo(1));
            Assert.That(store.Value, Is.Null);
            Assert.That(
                controller.Model.ActiveOperation.OperationId,
                Is.EqualTo(OperationId));
            controller.Dispose();
        }

        [Test]
        public async Task NetworkRetryReusesTheExactProtectedKey()
        {
            var store = new MemoryStore();
            var clock = new FakeClock();
            var fake = new FakeClient(store)
            {
                ReadResult = ReadySnapshot(),
                StartResult = StartResponse(),
                FailFirstStart = true
            };
            var controller = NewController(fake, store, clock);
            await controller.RefreshForProofAsync();

            await controller.StartForProofAsync(
                HiveBroodVitalityClient.FeedingType);
            Assert.That(
                controller.Model.State,
                Is.EqualTo(
                    HiveBroodVitalityScreenState.PendingConfirmation));
            Assert.That(store.Value, Is.Not.Null);
            await controller.StartForProofAsync(
                HiveBroodVitalityClient.FeedingType,
                true);

            Assert.That(fake.StartKeys.Count, Is.EqualTo(2));
            Assert.That(fake.StartKeys[1], Is.EqualTo(fake.StartKeys[0]));
            Assert.That(store.Value, Is.Null);
            controller.Dispose();
        }

        [Test]
        public async Task CompletionIsPreparedBeforeTransportAndClearedAfterSuccess()
        {
            var store = new MemoryStore();
            var clock = new FakeClock
            {
                Elapsed = TimeSpan.FromSeconds(12)
            };
            var fake = new FakeClient(store)
            {
                ReadResult = ReadyToCompleteSnapshot(),
                CompleteResult = CompleteResponse()
            };
            var controller = NewController(fake, store, clock);
            await controller.RefreshForProofAsync();

            await controller.CompleteForProofAsync();

            Assert.That(fake.StoreWasPreparedBeforeComplete, Is.True);
            Assert.That(fake.CompleteKeys, Has.Count.EqualTo(1));
            Assert.That(store.Value, Is.Null);
            Assert.That(controller.Model.ActiveOperation, Is.Null);
            Assert.That(controller.Model.Nutrition, Is.EqualTo(94));
            controller.Dispose();
        }

        [Test]
        public void PortraitAndLandscapeCareTargetsRemainFortyFourPixels()
        {
            AssertRects(
                HiveViewProductUiPresenter
                    .OfficialBroodCareActionRectsForProof(
                        new Rect(10f, 574f, 370f, 170f),
                        true),
                new Rect(0f, 0f, 390f, 844f));
            AssertRects(
                HiveViewProductUiPresenter
                    .OfficialBroodCareActionRectsForProof(
                        new Rect(1280f, 118f, 302f, 264f),
                        false),
                new Rect(0f, 0f, 1600f, 900f));
        }

        [Test]
        public void OfficialBroodCareCopyExistsInBothCatalogs()
        {
            string[] keys =
            {
                "brood_care.action.feed",
                "brood_care.action.stabilize",
                "brood_care.action.complete",
                "brood_care.status.pending",
                "brood_care.disclosure",
                "brood_care.error.idempotency_conflict"
            };
            foreach (string locale in new[] { "fr-CA", "en-US" })
            {
                string path = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "Assets",
                    "_Project",
                    "Data",
                    "Localization",
                    "Resources",
                    "Localization",
                    "strings." + locale + ".json");
                using JsonDocument document =
                    JsonDocument.Parse(File.ReadAllText(path));
                Dictionary<string, string> entries = document.RootElement
                    .GetProperty("entries")
                    .EnumerateArray()
                    .ToDictionary(
                        entry => entry.GetProperty("key").GetString(),
                        entry => entry.GetProperty("value").GetString(),
                        StringComparer.Ordinal);
                foreach (string key in keys)
                {
                    string value;
                    Assert.That(
                        entries.TryGetValue(key, out value),
                        Is.True,
                        locale + " " + key);
                    Assert.That(value, Is.Not.Empty, locale + " " + key);
                    Assert.That(value, Is.Not.EqualTo(key));
                }
            }
        }

        private static HiveBroodVitalityPanelController NewController(
            FakeClient client,
            MemoryStore store,
            FakeClock clock)
        {
            return new HiveBroodVitalityPanelController(
                client,
                HiveId,
                new ProtectedGameMutationOutbox(
                    store,
                    new SystemTextGameJsonCodec(),
                    clock),
                new FixedKeySource(),
                clock);
        }

        private static RemoteBroodVitalitySnapshot ReadySnapshot()
        {
            RemoteBroodVitalitySnapshot snapshot = Snapshot();
            snapshot.Vitality.ActiveOperation = null;
            snapshot.GlobalRevision = 7;
            return snapshot;
        }

        private static RemoteBroodVitalitySnapshot Snapshot()
        {
            return new RemoteBroodVitalitySnapshot
            {
                PlayerId = PlayerId,
                HiveId = HiveId,
                ContractVersion = HiveBroodVitalityClient.ContractVersion,
                ServerTimeUtc = Now,
                GlobalRevision = 8,
                Vitality = new RemoteBroodVitalityState
                {
                    Nutrition = 72,
                    Stability = 81,
                    Revision = 5,
                    UpdatedAtUtc = Now.AddMinutes(-1),
                    ActiveOperation = new RemoteBroodVitalityOperation
                    {
                        OperationId = OperationId,
                        Type = HiveBroodVitalityClient.FeedingType,
                        StartedAtUtc = Now,
                        EndsAtUtc = Now.AddSeconds(12)
                    }
                }
            };
        }

        private static RemoteBroodVitalityCareResponse StartResponse()
        {
            return new RemoteBroodVitalityCareResponse
            {
                Receipt = new RemoteBroodVitalityCareReceipt
                {
                    PlayerId = PlayerId,
                    HiveId = HiveId,
                    IdempotencyKey = "brood-proof-key",
                    OperationId = OperationId,
                    Type = HiveBroodVitalityClient.FeedingType,
                    RevisionBefore = 7,
                    RevisionAfter = 8,
                    AcceptedAtUtc = Now,
                    Code = HiveBroodVitalityClient.StartedCode
                },
                Snapshot = Snapshot()
            };
        }

        private static RemoteBroodVitalitySnapshot ReadyToCompleteSnapshot()
        {
            RemoteBroodVitalitySnapshot snapshot = Snapshot();
            snapshot.ServerTimeUtc = Now.AddSeconds(12);
            return snapshot;
        }

        private static RemoteBroodVitalityCareResponse CompleteResponse()
        {
            RemoteBroodVitalitySnapshot snapshot = ReadyToCompleteSnapshot();
            snapshot.GlobalRevision = 9;
            snapshot.Vitality.Nutrition = 94;
            snapshot.Vitality.Revision = 6;
            snapshot.Vitality.UpdatedAtUtc = Now.AddSeconds(12);
            snapshot.Vitality.ActiveOperation = null;
            return new RemoteBroodVitalityCareResponse
            {
                Receipt = new RemoteBroodVitalityCareReceipt
                {
                    PlayerId = PlayerId,
                    HiveId = HiveId,
                    IdempotencyKey = "brood-proof-key",
                    OperationId = OperationId,
                    Type = HiveBroodVitalityClient.FeedingType,
                    RevisionBefore = 8,
                    RevisionAfter = 9,
                    AcceptedAtUtc = Now.AddSeconds(12),
                    Code = HiveBroodVitalityClient.CompletedCode
                },
                Snapshot = snapshot
            };
        }

        private static void AssertRects(
            IEnumerable<Rect> rects,
            Rect bounds)
        {
            foreach (Rect rect in rects)
            {
                Assert.That(rect.width, Is.GreaterThanOrEqualTo(44f));
                Assert.That(rect.height, Is.GreaterThanOrEqualTo(44f));
                Assert.That(bounds.Contains(rect.min), Is.True);
                Assert.That(bounds.Contains(rect.max), Is.True);
            }
        }

        private sealed class FixedKeySource :
            IHiveBroodVitalityMutationKeySource
        {
            public string Create(string operation)
            {
                return "brood-proof-key";
            }
        }

        private sealed class FakeClock :
            IHiveBroodVitalityMonotonicClock,
            IMobileAccountSessionClock
        {
            public TimeSpan Elapsed { get; set; }
            public DateTimeOffset UtcNow => Now.Add(Elapsed);
        }

        private sealed class MemoryStore : IProtectedGameMutationOutboxStore
        {
            public bool IsProtectionAvailable => true;
            public string Value { get; set; }

            public Task<string> LoadAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(Value);
            }

            public Task SaveAsync(
                string protectedPlaintext,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Value = protectedPlaintext;
                return Task.CompletedTask;
            }

            public Task DeleteAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Value = null;
                return Task.CompletedTask;
            }
        }

        private sealed class FakeClient : IHiveBroodVitalityClient
        {
            private readonly MemoryStore store;
            public FakeClient(MemoryStore store) { this.store = store; }
            public GameReadSource LastReadSource { get; set; } =
                GameReadSource.Server;
            public DateTimeOffset LastReadCachedAtUtc { get; set; }
            public RemoteBroodVitalitySnapshot ReadResult { get; set; }
            public RemoteBroodVitalityCareResponse StartResult { get; set; }
            public RemoteBroodVitalityCareResponse CompleteResult { get; set; }
            public bool FailFirstStart { get; set; }
            public bool StoreWasPreparedBeforeStart { get; private set; }
            public bool StoreWasPreparedBeforeComplete { get; private set; }
            public List<string> StartKeys { get; } = new List<string>();
            public List<string> CompleteKeys { get; } = new List<string>();

            public Task<RemoteBroodVitalitySnapshot> ReadAsync(
                Guid hiveId,
                CancellationToken cancellationToken =
                    default(CancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(ReadResult);
            }

            public Task<RemoteBroodVitalityCareResponse> StartCareAsync(
                Guid hiveId,
                string type,
                long expectedRevision,
                string idempotencyKey,
                CancellationToken cancellationToken =
                    default(CancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                StoreWasPreparedBeforeStart =
                    StoreWasPreparedBeforeStart ||
                    !string.IsNullOrWhiteSpace(store.Value);
                StartKeys.Add(idempotencyKey);
                if (FailFirstStart && StartKeys.Count == 1)
                    throw new HivePerimeterClientException(
                        HivePerimeterClientError.TransportFailure,
                        "network_unavailable");
                return Task.FromResult(StartResult);
            }

            public Task<RemoteBroodVitalityCareResponse> CompleteCareAsync(
                Guid hiveId,
                Guid operationId,
                long expectedRevision,
                string idempotencyKey,
                CancellationToken cancellationToken =
                    default(CancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                StoreWasPreparedBeforeComplete =
                    StoreWasPreparedBeforeComplete ||
                    !string.IsNullOrWhiteSpace(store.Value);
                CompleteKeys.Add(idempotencyKey);
                return Task.FromResult(CompleteResult);
            }
        }
    }
}
