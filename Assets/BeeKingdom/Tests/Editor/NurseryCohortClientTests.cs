using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class NurseryCohortClientTests
    {
        private static readonly Guid Player =
            Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly Guid Hive =
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly Guid Cohort =
            Guid.Parse("88888888-9999-aaaa-bbbb-cccccccccccc");
        private static readonly DateTimeOffset Now =
            new DateTimeOffset(2026, 10, 14, 20, 0, 0, TimeSpan.Zero);

        [Test]
        public void ServerJsonContractDeserializesAndValidates()
        {
            var codec = new SystemTextGameJsonCodec();
            const string json =
                "{\"playerId\":\"11111111-2222-3333-4444-555555555555\"," +
                "\"hiveId\":\"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee\"," +
                "\"contractVersion\":\"bk-nursery-cohorts-v1-prototype\"," +
                "\"hiveRevision\":1,\"cohortRevision\":1," +
                "\"serverTimeUtc\":\"2026-10-14T20:00:00+00:00\"," +
                "\"recruitsInReserve\":0,\"reserveCapacity\":120," +
                "\"canStart\":false,\"status\":\"incubating\"," +
                "\"active\":{\"cohortId\":\"88888888-9999-aaaa-bbbb-cccccccccccc\"," +
                "\"startedAtUtc\":\"2026-10-14T20:00:00+00:00\"," +
                "\"readyAtUtc\":\"2026-10-14T21:00:00+00:00\"," +
                "\"expectedRecruits\":30,\"nutritionAtStart\":65," +
                "\"stabilityAtStart\":65},\"nextForecast\":{" +
                "\"contractVersion\":\"bk-nursery-growth-balance-v1-draft\"," +
                "\"globalTier\":\"Stable\",\"nutritionAtStart\":65," +
                "\"stabilityAtStart\":65,\"globalQuality\":65," +
                "\"nutritionSpeedBasisPoints\":10000," +
                "\"stabilityYieldBasisPoints\":10000," +
                "\"indicativeCombinedEfficiencyBasisPoints\":10000," +
                "\"baseRecruits\":30,\"expectedRecruits\":30," +
                "\"baseDurationSeconds\":3600,\"durationSeconds\":3600," +
                "\"startsAtUtc\":\"2026-10-14T20:00:00+00:00\"," +
                "\"endsAtUtc\":\"2026-10-14T21:00:00+00:00\"," +
                "\"recruitsAlreadyInReserve\":0,\"reserveCapacity\":120," +
                "\"canStartWithCurrentReserve\":true}}";
            var snapshot = codec.Deserialize<RemoteNurseryCohortSnapshot>(json);
            NurseryCohortClient.ValidateSnapshot(snapshot, Player, Hive);
            Assert.That(snapshot.Active.CohortId, Is.EqualTo(Cohort));
            Assert.That(snapshot.NextForecast.GlobalTier,
                Is.EqualTo(RemoteNurseryVitalityTier.Stable));
        }

        [Test]
        public void DisabledByDefaultCannotReadOrMutateAndNeverContactsServer()
        {
            var source = new FakeSessionSource();
            var transport = new FakeTransport();
            var client = NewClient(transport, source, enabled: false);
            Assert.That(client.Enabled, Is.False);
            HivePerimeterClientException a =
                Assert.ThrowsAsync<HivePerimeterClientException>(
                    async () => await client.ReadAsync(Hive));
            HivePerimeterClientException b =
                Assert.ThrowsAsync<HivePerimeterClientException>(
                    async () => await client.StartAsync(Hive, 0, "test"));
            Assert.That(a.Error, Is.EqualTo(HivePerimeterClientError.NotConfigured));
            Assert.That(b.Error, Is.EqualTo(HivePerimeterClientError.NotConfigured));
            Assert.That(source.FreshCalls, Is.Zero);
            Assert.That(transport.Requests, Is.Empty);
        }

        [Test]
        public async Task ReadUsesAuthenticatedGetAndAcceptsOnlyOwnServerSnapshot()
        {
            var transport = new FakeTransport(ValidSnapshot());
            var client = NewClient(transport);
            var result = await client.ReadAsync(Hive);
            Assert.That(result.Status, Is.EqualTo("incubating"));
            Assert.That(transport.Requests.Count, Is.EqualTo(1));
            Assert.That(transport.Requests[0].Method, Is.EqualTo("GET"));
            Assert.That(transport.Requests[0].Path,
                Is.EqualTo(NurseryCohortClient.ReadPath(Hive)));
            Assert.That(transport.Tokens[0], Is.EqualTo("original-token"));
        }

        [Test]
        public void AnotherPlayersSnapshotIsRejectedWithoutCachingOrCrediting()
        {
            var foreign = ValidSnapshot();
            foreign.PlayerId = Guid.NewGuid();
            var error = Assert.ThrowsAsync<HivePerimeterClientException>(
                async () => await NewClient(new FakeTransport(foreign)).ReadAsync(Hive));
            Assert.That(error.Error, Is.EqualTo(HivePerimeterClientError.InvalidResponse));
        }

        [Test]
        public void InvalidActiveCohortOrExcessiveReserveIsRejected()
        {
            var bad = ValidSnapshot();
            bad.RecruitsInReserve = 105;
            Assert.ThrowsAsync<HivePerimeterClientException>(
                async () => await NewClient(new FakeTransport(bad)).ReadAsync(Hive));
            bad = ValidSnapshot();
            bad.Active.ReadyAtUtc = bad.Active.StartedAtUtc;
            Assert.ThrowsAsync<HivePerimeterClientException>(
                async () => await NewClient(new FakeTransport(bad)).ReadAsync(Hive));
        }

        [Test]
        public async Task SuccessfulExplicitPostPreservesExpectedRevisionAndNonce()
        {
            var transport = new FakeTransport(Started());
            var client = NewClient(transport);
            var response = await client.StartAsync(Hive, 0, "client-new-cohort");
            Assert.That(response.Receipt.CohortId, Is.EqualTo(Cohort));
            Assert.That(transport.Requests.Count, Is.EqualTo(1));
            Assert.That(transport.Requests[0].Method, Is.EqualTo("POST"));
            Assert.That(transport.Requests[0].Path,
                Is.EqualTo(NurseryCohortClient.StartPath(Hive)));
            var body = (NurseryCohortStartRequest)transport.Requests[0].Body;
            Assert.That(body.ExpectedRevision, Is.Zero);
            Assert.That(body.IdempotencyKey, Is.EqualTo("client-new-cohort"));
        }

        [Test]
        public void InvalidKeysAndRevisionsFailBeforeSessionOrNetwork()
        {
            var source = new FakeSessionSource();
            var transport = new FakeTransport();
            var client = NewClient(transport, source);
            foreach (string key in new[] {"", "with spaces", "slash/", "quote\""})
            {
                Assert.ThrowsAsync<HivePerimeterClientException>(
                    async () => await client.StartAsync(Hive, 0, key));
            }
            Assert.ThrowsAsync<HivePerimeterClientException>(
                async () => await client.StartAsync(Hive, long.MaxValue, "valid-key"));
            Assert.ThrowsAsync<HivePerimeterClientException>(
                async () => await client.ReadAsync(Guid.Empty));
            Assert.That(source.FreshCalls, Is.Zero);
            Assert.That(transport.Requests, Is.Empty);
        }

        [Test]
        public void ForgedReceiptCannotAuthorizeClientAction()
        {
            var payload = Started();
            payload.Receipt.RevisionAfter = 8;
            HivePerimeterClientException error =
                Assert.ThrowsAsync<HivePerimeterClientException>(
                    async () => await NewClient(new FakeTransport(payload))
                        .StartAsync(Hive, 0, "client-new-cohort"));
            Assert.That(error.Error, Is.EqualTo(HivePerimeterClientError.InvalidResponse));
        }

        [Test]
        public async Task UnauthorizedRefreshOnlyOnceUsesSameIdempotentRequest()
        {
            var transport = new FakeTransport(
                new AuthenticatedGameRestException(
                    AuthenticatedGameRestError.Unauthorized, "expired", 401),
                Started());
            var session = new FakeSessionSource();
            await NewClient(transport, session).StartAsync(
                Hive, 0, "client-new-cohort");
            Assert.That(session.RefreshCalls, Is.EqualTo(1));
            Assert.That(transport.Requests.Count, Is.EqualTo(2));
            Assert.That(transport.Requests[0], Is.SameAs(transport.Requests[1]));
            Assert.That(transport.Tokens,
                Is.EqualTo(new[] {"original-token", "rotated-token"}));
            Assert.That(session.InvalidateCalls, Is.Zero);
        }

        [Test]
        public void RefreshChangingPlayerIdFailsClosedBeforeSecondRequest()
        {
            var transport = new FakeTransport(
                new AuthenticatedGameRestException(
                    AuthenticatedGameRestError.Unauthorized, "expired", 401));
            var session = new FakeSessionSource
            {
                RefreshPlayerId = Guid.NewGuid()
            };
            var error = Assert.ThrowsAsync<HivePerimeterClientException>(
                async () => await NewClient(transport, session).ReadAsync(Hive));
            Assert.That(error.Error, Is.EqualTo(HivePerimeterClientError.InvalidResponse));
            Assert.That(session.InvalidateCalls, Is.EqualTo(1));
            Assert.That(transport.Requests.Count, Is.EqualTo(1));
        }

        [Test]
        public void NetworkFailureDoesNotResubmitNonIdempotently()
        {
            var transport = new FakeTransport(
                new AuthenticatedGameRestException(
                    AuthenticatedGameRestError.NetworkFailure, "offline"));
            var error = Assert.ThrowsAsync<HivePerimeterClientException>(
                async () => await NewClient(transport).StartAsync(
                    Hive, 0, "uncertain-first"));
            Assert.That(error.Error,
                Is.EqualTo(HivePerimeterClientError.TransportFailure));
            Assert.That(transport.Requests.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task CancelledRequestDoesNotRetryOrContactTransport()
        {
            using (var token = new CancellationTokenSource())
            {
                token.Cancel();
                var transport = new FakeTransport();
                try
                {
                    await NewClient(transport).ReadAsync(Hive, token.Token);
                    Assert.Fail("Cancelled request unexpectedly succeeded.");
                }
                catch (OperationCanceledException)
                {
                    // TaskCanceledException derives from OperationCanceledException;
                    // NUnit 3 ThrowsAsync performs an exact-type check instead.
                }
                Assert.That(transport.Requests, Is.Empty);
            }
        }

        private static NurseryCohortClient NewClient(
            FakeTransport transport, FakeSessionSource source = null,
            bool enabled = true)
        {
            var gate = new MobileAccountSessionGate();
            gate.ConfigureTransport(true);
            gate.Apply(AccountSessionReadinessSnapshot.FromServer(
                true, true, true, true, true));
            return new NurseryCohortClient(gate,
                source ?? new FakeSessionSource(), transport, enabled);
        }

        private static RemoteNurseryCohortSnapshot ValidSnapshot()
        {
            return new RemoteNurseryCohortSnapshot
            {
                PlayerId = Player,
                HiveId = Hive,
                ContractVersion = NurseryCohortClient.ContractVersion,
                HiveRevision = 1,
                CohortRevision = 1,
                ServerTimeUtc = Now,
                RecruitsInReserve = 0,
                ReserveCapacity = 120,
                CanStart = false,
                Status = "incubating",
                Active = new RemoteNurseryCohort
                {
                    CohortId = Cohort,
                    StartedAtUtc = Now,
                    ReadyAtUtc = Now.AddHours(1),
                    ExpectedRecruits = 30,
                    NutritionAtStart = 65,
                    StabilityAtStart = 65
                },
                NextForecast = new RemoteNurseryGrowthForecast
                {
                    ContractVersion = NurseryCohortClient.ForecastContractVersion,
                    GlobalTier = RemoteNurseryVitalityTier.Stable,
                    NutritionAtStart = 65,
                    StabilityAtStart = 65,
                    GlobalQuality = 65,
                    NutritionSpeedBasisPoints = 10000,
                    StabilityYieldBasisPoints = 10000,
                    IndicativeCombinedEfficiencyBasisPoints = 10000,
                    BaseRecruits = 30,
                    ExpectedRecruits = 30,
                    BaseDurationSeconds = 3600,
                    DurationSeconds = 3600,
                    StartsAtUtc = Now,
                    EndsAtUtc = Now.AddHours(1),
                    RecruitsAlreadyInReserve = 0,
                    ReserveCapacity = 120,
                    CanStartWithCurrentReserve = true
                }
            };
        }

        private static RemoteNurseryCohortCommandResult Started()
        {
            return new RemoteNurseryCohortCommandResult
            {
                Succeeded = true,
                Code = NurseryCohortClient.StartedCode,
                Snapshot = ValidSnapshot(),
                Receipt = new RemoteNurseryCohortReceipt
                {
                    IdempotencyKey = "client-new-cohort",
                    CohortId = Cohort,
                    RevisionBefore = 0,
                    RevisionAfter = 1,
                    AcceptedAtUtc = Now,
                    Code = NurseryCohortClient.StartedCode
                }
            };
        }

        private sealed class FakeSessionSource : IRefreshableGameAccountSessionSource
        {
            public int FreshCalls { get; private set; }
            public int RefreshCalls { get; private set; }
            public int InvalidateCalls { get; private set; }
            public Guid RefreshPlayerId { get; set; } = Player;

            public bool TryGetSession(out GameAccountSession session)
            {
                session = new GameAccountSession(Player, "original-token");
                return true;
            }

            public bool TryGetKnownPlayerId(out Guid playerId)
            {
                playerId = Player;
                return true;
            }

            public Task<GameAccountSession> GetFreshSessionAsync(
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                FreshCalls++;
                return Task.FromResult(
                    new GameAccountSession(Player, "original-token"));
            }

            public Task<GameAccountSession> RefreshAfterUnauthorizedAsync(
                string rejectedAccessToken,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RefreshCalls++;
                return Task.FromResult(new GameAccountSession(
                    RefreshPlayerId, "rotated-token"));
            }

            public Task InvalidateUnauthorizedSessionAsync(
                string rejectedAccessToken,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                InvalidateCalls++;
                return Task.CompletedTask;
            }
        }

        private sealed class FakeTransport : IAuthenticatedGameRestTransport
        {
            private readonly Queue<object> steps;
            public FakeTransport(params object[] steps)
            {
                this.steps = new Queue<object>(steps ?? Array.Empty<object>());
            }
            public List<AuthenticatedGameRestRequest> Requests { get; } =
                new List<AuthenticatedGameRestRequest>();
            public List<string> Tokens { get; } = new List<string>();

            public Task<T> SendAsync<T>(AuthenticatedGameRestRequest request,
                string bearerAccessToken, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Requests.Add(request);
                Tokens.Add(bearerAccessToken);
                object step = steps.Count == 0 ? null : steps.Dequeue();
                if (step is Exception ex) throw ex;
                return Task.FromResult((T)step);
            }
        }
    }
}
