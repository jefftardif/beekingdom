using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class HiveBroodVitalityClientTests
    {
        private static readonly Guid PlayerId =
            Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly Guid HiveId =
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly Guid OperationId =
            Guid.Parse("99999999-8888-7777-6666-555555555555");
        private static readonly DateTimeOffset ServerTime =
            new DateTimeOffset(2026, 7, 23, 10, 0, 0, TimeSpan.Zero);

        [Test]
        public void ExactServerEnvelopeDeserializesIntoTypedSnapshot()
        {
            const string json =
                "{\"playerId\":\"11111111-2222-3333-4444-555555555555\"," +
                "\"hiveId\":\"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee\"," +
                "\"contractVersion\":\"living-hive-brood-vitality-v1\"," +
                "\"serverTimeUtc\":\"2026-07-23T10:00:00+00:00\"," +
                "\"globalRevision\":8,\"vitality\":{\"nutrition\":72," +
                "\"stability\":81,\"revision\":5," +
                "\"updatedAtUtc\":\"2026-07-23T09:59:00+00:00\"," +
                "\"activeOperation\":{\"operationId\":" +
                "\"99999999-8888-7777-6666-555555555555\"," +
                "\"type\":\"feeding\"," +
                "\"startedAtUtc\":\"2026-07-23T10:00:00+00:00\"," +
                "\"endsAtUtc\":\"2026-07-23T10:00:12+00:00\"}}}";

            RemoteBroodVitalitySnapshot value =
                new SystemTextGameJsonCodec()
                    .Deserialize<RemoteBroodVitalitySnapshot>(json);

            Assert.That(value.PlayerId, Is.EqualTo(PlayerId));
            Assert.That(value.GlobalRevision, Is.EqualTo(8));
            Assert.That(value.Vitality.Nutrition, Is.EqualTo(72));
            Assert.That(
                value.Vitality.ActiveOperation.OperationId,
                Is.EqualTo(OperationId));
        }

        [Test]
        public async Task ReadAcceptsHonestUninitializedSnapshot()
        {
            RemoteBroodVitalitySnapshot snapshot = ValidSnapshot();
            snapshot.Vitality = null;
            var transport = new FakeTransport(snapshot);

            RemoteBroodVitalitySnapshot result =
                await NewClient(transport).ReadAsync(HiveId);

            Assert.That(result.Vitality, Is.Null);
            Assert.That(transport.Requests[0].Method, Is.EqualTo("GET"));
            Assert.That(
                transport.Requests[0].Path,
                Is.EqualTo(HiveBroodVitalityClient.Path(HiveId)));
        }

        [Test]
        public void ForeignPlayerSnapshotFailsClosed()
        {
            RemoteBroodVitalitySnapshot snapshot = ValidSnapshot();
            snapshot.PlayerId = Guid.NewGuid();

            HivePerimeterClientException error =
                Assert.ThrowsAsync<HivePerimeterClientException>(
                    async () => await NewClient(
                        new FakeTransport(snapshot)).ReadAsync(HiveId));

            Assert.That(
                error.Error,
                Is.EqualTo(HivePerimeterClientError.InvalidResponse));
        }

        [Test]
        public async Task StartSendsBoundTypeRevisionAndIdempotencyKey()
        {
            var transport = new FakeTransport(StartedResponse());

            await NewClient(transport).StartCareAsync(
                HiveId,
                HiveBroodVitalityClient.FeedingType,
                7,
                "brood-care-start-proof");

            Assert.That(transport.Requests.Count, Is.EqualTo(1));
            Assert.That(transport.Requests[0].Method, Is.EqualTo("POST"));
            Assert.That(
                transport.Requests[0].Path,
                Is.EqualTo(
                    HiveBroodVitalityClient.StartPath(
                        HiveId,
                        HiveBroodVitalityClient.FeedingType)));
            var body =
                (BroodVitalityCareMutationRequest)transport.Requests[0].Body;
            Assert.That(body.ExpectedRevision, Is.EqualTo(7));
            Assert.That(
                body.IdempotencyKey,
                Is.EqualTo("brood-care-start-proof"));
        }

        [Test]
        public async Task CompleteSendsOperationRevisionAndIdempotencyKey()
        {
            var transport = new FakeTransport(CompletedResponse());

            await NewClient(transport).CompleteCareAsync(
                HiveId,
                OperationId,
                8,
                "brood-care-complete-proof");

            Assert.That(
                transport.Requests[0].Path,
                Is.EqualTo(
                    HiveBroodVitalityClient.CompletePath(
                        HiveId,
                        OperationId)));
            var body =
                (BroodVitalityCareMutationRequest)transport.Requests[0].Body;
            Assert.That(body.ExpectedRevision, Is.EqualTo(8));
            Assert.That(
                body.IdempotencyKey,
                Is.EqualTo("brood-care-complete-proof"));
        }

        [Test]
        public void AlteredReceiptRevisionFailsClosed()
        {
            RemoteBroodVitalityCareResponse response = StartedResponse();
            response.Receipt.RevisionAfter++;

            HivePerimeterClientException error =
                Assert.ThrowsAsync<HivePerimeterClientException>(
                    async () => await NewClient(new FakeTransport(response))
                        .StartCareAsync(
                            HiveId,
                            HiveBroodVitalityClient.FeedingType,
                            7,
                            "brood-care-start-proof"));

            Assert.That(
                error.Error,
                Is.EqualTo(HivePerimeterClientError.InvalidResponse));
        }

        [Test]
        public void UnknownOperationTypeFailsBeforeSessionOrTransport()
        {
            var source = new FakeSessionSource();
            var transport = new FakeTransport();

            HivePerimeterClientException error =
                Assert.ThrowsAsync<HivePerimeterClientException>(
                    async () => await NewClient(transport, source)
                        .StartCareAsync(
                            HiveId,
                            "unknown",
                            7,
                            "brood-care-start-proof"));

            Assert.That(
                error.Error,
                Is.EqualTo(HivePerimeterClientError.InvalidRequest));
            Assert.That(source.Calls, Is.Zero);
            Assert.That(transport.Requests, Is.Empty);
        }

        [Test]
        public void MaximumRevisionFailsBeforeSessionOrTransport()
        {
            var source = new FakeSessionSource();
            var transport = new FakeTransport();

            HivePerimeterClientException error =
                Assert.ThrowsAsync<HivePerimeterClientException>(
                    async () => await NewClient(transport, source)
                        .CompleteCareAsync(
                            HiveId,
                            OperationId,
                            long.MaxValue,
                            "brood-care-complete-proof"));

            Assert.That(
                error.Error,
                Is.EqualTo(HivePerimeterClientError.InvalidRequest));
            Assert.That(source.Calls, Is.Zero);
            Assert.That(transport.Requests, Is.Empty);
        }

        [Test]
        public void AlteredServerDurationFailsClosed()
        {
            RemoteBroodVitalitySnapshot snapshot = ValidSnapshot();
            snapshot.Vitality.ActiveOperation.EndsAtUtc =
                snapshot.Vitality.ActiveOperation.EndsAtUtc.AddSeconds(1);

            HivePerimeterClientException error =
                Assert.ThrowsAsync<HivePerimeterClientException>(
                    async () => await NewClient(
                        new FakeTransport(snapshot)).ReadAsync(HiveId));

            Assert.That(
                error.Error,
                Is.EqualTo(HivePerimeterClientError.InvalidResponse));
        }

        private static HiveBroodVitalityClient NewClient(
            FakeTransport transport,
            FakeSessionSource source = null)
        {
            var gate = new MobileAccountSessionGate();
            gate.ConfigureTransport(true);
            gate.Apply(
                AccountSessionReadinessSnapshot.FromServer(
                    true,
                    true,
                    true,
                    true,
                    true));
            return new HiveBroodVitalityClient(
                gate,
                source ?? new FakeSessionSource(),
                transport);
        }

        private static RemoteBroodVitalitySnapshot ValidSnapshot()
        {
            return new RemoteBroodVitalitySnapshot
            {
                PlayerId = PlayerId,
                HiveId = HiveId,
                ContractVersion = HiveBroodVitalityClient.ContractVersion,
                ServerTimeUtc = ServerTime,
                GlobalRevision = 8,
                Vitality = new RemoteBroodVitalityState
                {
                    Nutrition = 72,
                    Stability = 81,
                    Revision = 5,
                    UpdatedAtUtc = ServerTime.AddMinutes(-1),
                    ActiveOperation = new RemoteBroodVitalityOperation
                    {
                        OperationId = OperationId,
                        Type = HiveBroodVitalityClient.FeedingType,
                        StartedAtUtc = ServerTime,
                        EndsAtUtc = ServerTime.AddSeconds(
                            HiveBroodVitalityClient.FeedingDurationSeconds)
                    }
                }
            };
        }

        private static RemoteBroodVitalityCareResponse StartedResponse()
        {
            return new RemoteBroodVitalityCareResponse
            {
                Receipt = new RemoteBroodVitalityCareReceipt
                {
                    PlayerId = PlayerId,
                    HiveId = HiveId,
                    IdempotencyKey = "brood-care-start-proof",
                    OperationId = OperationId,
                    Type = HiveBroodVitalityClient.FeedingType,
                    RevisionBefore = 7,
                    RevisionAfter = 8,
                    AcceptedAtUtc = ServerTime,
                    Code = HiveBroodVitalityClient.StartedCode
                },
                Snapshot = ValidSnapshot()
            };
        }

        private static RemoteBroodVitalityCareResponse CompletedResponse()
        {
            RemoteBroodVitalitySnapshot snapshot = ValidSnapshot();
            snapshot.ServerTimeUtc = ServerTime.AddSeconds(13);
            snapshot.GlobalRevision = 9;
            snapshot.Vitality.Nutrition = 94;
            snapshot.Vitality.Revision = 6;
            snapshot.Vitality.UpdatedAtUtc = ServerTime.AddSeconds(13);
            snapshot.Vitality.ActiveOperation = null;
            return new RemoteBroodVitalityCareResponse
            {
                Receipt = new RemoteBroodVitalityCareReceipt
                {
                    PlayerId = PlayerId,
                    HiveId = HiveId,
                    IdempotencyKey = "brood-care-complete-proof",
                    OperationId = OperationId,
                    Type = HiveBroodVitalityClient.FeedingType,
                    RevisionBefore = 8,
                    RevisionAfter = 9,
                    AcceptedAtUtc = snapshot.ServerTimeUtc,
                    Code = HiveBroodVitalityClient.CompletedCode
                },
                Snapshot = snapshot
            };
        }

        private sealed class FakeSessionSource :
            IRefreshableGameAccountSessionSource
        {
            public int Calls { get; private set; }

            public bool TryGetSession(out GameAccountSession session)
            {
                session = new GameAccountSession(
                    PlayerId,
                    "brood-care-token");
                return true;
            }

            public bool TryGetKnownPlayerId(out Guid playerId)
            {
                playerId = PlayerId;
                return true;
            }

            public Task<GameAccountSession> GetFreshSessionAsync(
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Calls++;
                return Task.FromResult(
                    new GameAccountSession(
                        PlayerId,
                        "brood-care-token"));
            }

            public Task<GameAccountSession> RefreshAfterUnauthorizedAsync(
                string rejectedAccessToken,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(
                    new GameAccountSession(
                        PlayerId,
                        "brood-care-rotated-token"));
            }

            public Task InvalidateUnauthorizedSessionAsync(
                string rejectedAccessToken,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            }
        }

        private sealed class FakeTransport : IAuthenticatedGameRestTransport
        {
            private readonly Queue<object> steps;

            public FakeTransport(params object[] steps)
            {
                this.steps =
                    new Queue<object>(steps ?? Array.Empty<object>());
            }

            public List<AuthenticatedGameRestRequest> Requests { get; } =
                new List<AuthenticatedGameRestRequest>();

            public Task<T> SendAsync<T>(
                AuthenticatedGameRestRequest request,
                string bearerAccessToken,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Requests.Add(request);
                object step = steps.Count == 0 ? null : steps.Dequeue();
                if (step is Exception error) throw error;
                return Task.FromResult((T)step);
            }
        }
    }
}
