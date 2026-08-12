using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class HiveDailyRoundClientTests
    {
        private static readonly Guid PlayerId =
            Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly Guid HiveId =
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly DateTimeOffset Day =
            new DateTimeOffset(2026, 7, 23, 0, 0, 0, TimeSpan.Zero);

        [Test]
        public void SystemTextCodecAcceptsTheExactTypedServerEnvelope()
        {
            const string json =
                "{\"receipt\":{\"playerId\":\"11111111-2222-3333-4444-555555555555\"," +
                "\"hiveId\":\"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee\"," +
                "\"idempotencyKey\":\"daily-round-proof-key\"," +
                "\"dayUtc\":\"2026-07-23T00:00:00+00:00\"," +
                "\"revisionBefore\":7,\"revisionAfter\":8," +
                "\"acceptedAtUtc\":\"2026-07-23T12:00:00+00:00\"," +
                "\"creditedHoney\":120,\"creditedPollen\":60," +
                "\"code\":\"game.daily_round_claimed\"}," +
                "\"snapshot\":{\"playerId\":\"11111111-2222-3333-4444-555555555555\"," +
                "\"hiveId\":\"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee\"," +
                "\"contractVersion\":\"living-hive-daily-round-v1\"," +
                "\"dayUtc\":\"2026-07-23T00:00:00+00:00\"," +
                "\"nextResetUtc\":\"2026-07-24T00:00:00+00:00\"," +
                "\"serverTimeUtc\":\"2026-07-23T12:03:00+00:00\"," +
                "\"revision\":11,\"facts\":{\"collection_received\":true," +
                "\"operation_launched\":true,\"snapshot_read\":true}," +
                "\"completedCount\":3,\"honeyReward\":120,\"pollenReward\":60," +
                "\"claimAvailable\":false," +
                "\"claimedAtUtc\":\"2026-07-23T12:00:00+00:00\"}}";

            RemoteHiveDailyRoundClaimResponse value =
                new SystemTextGameJsonCodec()
                    .Deserialize<RemoteHiveDailyRoundClaimResponse>(json);

            Assert.That(value.Receipt.DayUtc, Is.EqualTo(Day));
            Assert.That(value.Receipt.RevisionBefore, Is.EqualTo(7));
            Assert.That(value.Receipt.RevisionAfter, Is.EqualTo(8));
            Assert.That(value.Receipt.CreditedHoney, Is.EqualTo(120));
            Assert.That(value.Receipt.CreditedPollen, Is.EqualTo(60));
            Assert.That(value.Snapshot.Revision, Is.EqualTo(11));
        }

        [Test]
        public async Task ClaimSendsExpectedDayRevisionAndKey()
        {
            var transport = new FakeTransport(ValidResponse());
            HiveDailyRoundClient client = NewClient(transport);

            await client.ClaimAsync(
                HiveId,
                Day,
                7,
                "daily-round-proof-key");

            Assert.That(transport.Requests.Count, Is.EqualTo(1));
            AuthenticatedGameRestRequest request = transport.Requests[0];
            Assert.That(request.Method, Is.EqualTo("POST"));
            Assert.That(
                request.Path,
                Is.EqualTo(
                    "/game/v1/hives/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/daily-round/claim"));
            var body = (HiveDailyRoundClaimRequest)request.Body;
            Assert.That(body.ExpectedDayUtc, Is.EqualTo("2026-07-23"));
            Assert.That(body.ExpectedRevision, Is.EqualTo(7));
            Assert.That(body.IdempotencyKey, Is.EqualTo("daily-round-proof-key"));
        }

        [Test]
        public async Task OriginalReceiptIsAcceptedWithNewerCurrentSnapshot()
        {
            RemoteHiveDailyRoundClaimResponse response = ValidResponse();
            response.Snapshot.Revision = 11;
            response.Snapshot.ServerTimeUtc = Day.AddHours(12).AddMinutes(3);
            HiveDailyRoundClient client =
                NewClient(new FakeTransport(response));

            RemoteHiveDailyRoundClaimResponse result =
                await client.ClaimAsync(
                    HiveId,
                    Day,
                    7,
                    "daily-round-proof-key");

            Assert.That(result.Receipt.RevisionAfter, Is.EqualTo(8));
            Assert.That(result.Snapshot.Revision, Is.EqualTo(11));
        }

        [Test]
        public void AlteredRewardReceiptFailsClosed()
        {
            RemoteHiveDailyRoundClaimResponse response = ValidResponse();
            response.Receipt.CreditedHoney++;
            HiveDailyRoundClient client =
                NewClient(new FakeTransport(response));

            HivePerimeterClientException error =
                Assert.ThrowsAsync<HivePerimeterClientException>(
                    async () => await client.ClaimAsync(
                        HiveId,
                        Day,
                        7,
                        "daily-round-proof-key"));

            Assert.That(
                error.Error,
                Is.EqualTo(HivePerimeterClientError.InvalidResponse));
        }

        [Test]
        public void MaximumRevisionStopsBeforeCredentialsAndTransport()
        {
            var source = new FakeSessionSource();
            var transport = new FakeTransport();
            HiveDailyRoundClient client = NewClient(transport, source);

            HivePerimeterClientException error =
                Assert.ThrowsAsync<HivePerimeterClientException>(
                    async () => await client.ClaimAsync(
                        HiveId,
                        Day,
                        long.MaxValue,
                        "daily-round-proof-key"));

            Assert.That(
                error.Error,
                Is.EqualTo(HivePerimeterClientError.InvalidRequest));
            Assert.That(source.Calls, Is.Zero);
            Assert.That(transport.Requests, Is.Empty);
        }

        private static HiveDailyRoundClient NewClient(
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
            return new HiveDailyRoundClient(
                gate,
                source ?? new FakeSessionSource(),
                transport);
        }

        private static RemoteHiveDailyRoundClaimResponse ValidResponse()
        {
            DateTimeOffset acceptedAt = Day.AddHours(12);
            return new RemoteHiveDailyRoundClaimResponse
            {
                Receipt = new RemoteHiveDailyRoundReceipt
                {
                    PlayerId = PlayerId,
                    HiveId = HiveId,
                    IdempotencyKey = "daily-round-proof-key",
                    DayUtc = Day,
                    RevisionBefore = 7,
                    RevisionAfter = 8,
                    AcceptedAtUtc = acceptedAt,
                    CreditedHoney = HiveDailyRoundClient.HoneyReward,
                    CreditedPollen = HiveDailyRoundClient.PollenReward,
                    Code = HiveDailyRoundClient.ClaimedCode
                },
                Snapshot = new RemoteHiveDailyRoundSnapshot
                {
                    PlayerId = PlayerId,
                    HiveId = HiveId,
                    ContractVersion = HiveDailyRoundClient.ContractVersion,
                    DayUtc = Day,
                    NextResetUtc = Day.AddDays(1),
                    ServerTimeUtc = acceptedAt,
                    Revision = 8,
                    Facts = new Dictionary<string, bool>
                    {
                        [HiveDailyRoundClient.CollectionFact] = true,
                        [HiveDailyRoundClient.OperationFact] = true,
                        [HiveDailyRoundClient.SnapshotFact] = true
                    },
                    CompletedCount = 3,
                    HoneyReward = HiveDailyRoundClient.HoneyReward,
                    PollenReward = HiveDailyRoundClient.PollenReward,
                    ClaimAvailable = false,
                    ClaimedAtUtc = acceptedAt
                }
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
                    "daily-round-token");
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
                        "daily-round-token"));
            }

            public Task<GameAccountSession> RefreshAfterUnauthorizedAsync(
                string rejectedAccessToken,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(
                    new GameAccountSession(
                        PlayerId,
                        "daily-round-rotated-token"));
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
