using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class HiveDoctrineRecruitmentClientTests
    {
        private static readonly Guid PlayerId =
            Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly Guid HiveId =
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly Guid OperationId =
            Guid.Parse("99999999-8888-7777-6666-555555555555");
        private const string Token = "recruitment-test-token";

        [Test]
        public void ClosedOfficialGateStopsBeforeCredentialsAndTransport()
        {
            var source = new FakeSessionSource(
                new GameAccountSession(PlayerId, Token));
            var transport = new ScriptedTransport(ValidSnapshot());
            var client = new HiveDoctrineRecruitmentClient(
                new MobileAccountSessionGate(),
                source,
                transport);

            HivePerimeterClientException error =
                Assert.ThrowsAsync<HivePerimeterClientException>(
                    async () => await client.ReadAsync(HiveId));

            Assert.That(
                error.Error,
                Is.EqualTo(HivePerimeterClientError.NotConfigured));
            Assert.That(source.Calls, Is.Zero);
            Assert.That(transport.Requests, Is.Empty);
        }

        [Test]
        public async Task ValidatedReadUsesExactRouteAndProtectedGetCache()
        {
            RemoteDoctrineRecruitmentSnapshot snapshot = ValidSnapshot();
            var store = new MemoryCacheStore();
            var transport = new ScriptedTransport(snapshot);
            var client = NewClient(
                new FakeSessionSource(
                    new GameAccountSession(PlayerId, Token)),
                transport,
                NewCache(store, snapshot.ServerTimeUtc));

            RemoteDoctrineRecruitmentSnapshot result =
                await client.ReadAsync(HiveId);

            Assert.That(result, Is.SameAs(snapshot));
            AuthenticatedGameRestRequest request =
                transport.Requests.Single();
            Assert.That(request.Method, Is.EqualTo("GET"));
            Assert.That(
                request.Path,
                Is.EqualTo(
                    "/game/v1/hives/" +
                    "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/" +
                    "combat/recruitment"));
            Assert.That(request.Body, Is.Null);
            Assert.That(store.SaveCalls, Is.EqualTo(1));
            Assert.That(
                client.LastReadSource,
                Is.EqualTo(GameReadSource.Server));
        }

        [Test]
        public async Task StartSendsOnlyFamilyRevisionAndStableKey()
        {
            RemoteDoctrineRecruitmentResponse response =
                ValidStartResponse(3, "start-stable");
            var transport = new ScriptedTransport(response);
            var store = new MemoryCacheStore();
            var client = NewClient(
                new FakeSessionSource(
                    new GameAccountSession(PlayerId, Token)),
                transport,
                NewCache(store, response.Snapshot.ServerTimeUtc));

            RemoteDoctrineRecruitmentResponse result =
                await client.StartAsync(
                    HiveId,
                    "guardians",
                    3,
                    "start-stable");

            Assert.That(result, Is.SameAs(response));
            AuthenticatedGameRestRequest request =
                transport.Requests.Single();
            Assert.That(request.Method, Is.EqualTo("POST"));
            Assert.That(
                request.Path,
                Does.EndWith("/combat/recruitment/start"));
            var body = request.Body as DoctrineRecruitmentStartRequest;
            Assert.That(body, Is.Not.Null);
            Assert.That(body.Family, Is.EqualTo("guardians"));
            Assert.That(body.ExpectedRevision, Is.EqualTo(3));
            Assert.That(
                body.IdempotencyKey,
                Is.EqualTo("start-stable"));
            Assert.That(
                request.Body.GetType().GetProperties()
                    .Select(property => property.Name),
                Is.EquivalentTo(
                    new[]
                    {
                        "Family",
                        "ExpectedRevision",
                        "IdempotencyKey"
                    }));
            Assert.That(store.SaveCalls, Is.EqualTo(1));
        }

        [Test]
        public async Task ClaimSendsOnlyRevisionAndStableKey()
        {
            RemoteDoctrineRecruitmentResponse response =
                ValidClaimResponse(4, "claim-stable");
            var transport = new ScriptedTransport(response);
            var client = NewClient(
                new FakeSessionSource(
                    new GameAccountSession(PlayerId, Token)),
                transport);

            await client.ClaimAsync(
                HiveId,
                OperationId,
                "guardians",
                4,
                "claim-stable");

            AuthenticatedGameRestRequest request =
                transport.Requests.Single();
            Assert.That(
                request.Path,
                Does.EndWith(
                    "/combat/recruitment/" +
                    "99999999-8888-7777-6666-555555555555/claim"));
            var body = request.Body as DoctrineRecruitmentClaimRequest;
            Assert.That(body, Is.Not.Null);
            Assert.That(body.ExpectedRevision, Is.EqualTo(4));
            Assert.That(
                body.IdempotencyKey,
                Is.EqualTo("claim-stable"));
            Assert.That(
                request.Body.GetType().GetProperties()
                    .Select(property => property.Name),
                Is.EquivalentTo(
                    new[] { "ExpectedRevision", "IdempotencyKey" }));
        }

        [Test]
        public void InvalidLocalMutationsNeverReachSessionOrTransport()
        {
            var source = new FakeSessionSource(
                new GameAccountSession(PlayerId, Token));
            var transport = new ScriptedTransport();
            var client = NewClient(source, transport);

            AssertInvalidRequest(
                () => client.StartAsync(
                    HiveId,
                    "unknown",
                    0,
                    "key"));
            AssertInvalidRequest(
                () => client.StartAsync(
                    HiveId,
                    "guardians",
                    -1,
                    "key"));
            AssertInvalidRequest(
                () => client.StartAsync(
                    HiveId,
                    "guardians",
                    0,
                    " key"));
            AssertInvalidRequest(
                () => client.ClaimAsync(
                    HiveId,
                    Guid.Empty,
                    "guardians",
                    0,
                    "key"));
            AssertInvalidRequest(
                () => client.ClaimAsync(
                    HiveId,
                    OperationId,
                    "guardians",
                    long.MaxValue,
                    "key"));

            Assert.That(source.Calls, Is.Zero);
            Assert.That(transport.Requests, Is.Empty);
        }

        [Test]
        public void ForeignMalformedOrInconsistentSnapshotsAreRejected()
        {
            AssertInvalidSnapshot(
                snapshot => snapshot.PlayerId = Guid.NewGuid());
            AssertInvalidSnapshot(
                snapshot => snapshot.ContractVersion = "wrong-v1");
            AssertInvalidSnapshot(
                snapshot => snapshot.CatalogVersion = "wrong-catalog");
            AssertInvalidSnapshot(
                snapshot =>
                    snapshot.ServerTimeUtc =
                        snapshot.ServerTimeUtc.ToOffset(
                            TimeSpan.FromHours(-4)));
            AssertInvalidSnapshot(
                snapshot => snapshot.Offers[0].BatchSize++);
            AssertInvalidSnapshot(
                snapshot => snapshot.Offers.RemoveAt(0));
            AssertInvalidSnapshot(
                snapshot =>
                    snapshot.Balances["pollen"].Amount =
                        snapshot.Balances["pollen"].Capacity + 1);
            AssertInvalidSnapshot(
                snapshot => snapshot.Balances.Remove("honey"));
            AssertInvalidSnapshot(
                snapshot => snapshot.Counts["unknown"] = 1);
            AssertInvalidSnapshot(
                snapshot => snapshot.LegacyRoles[0] = "Guardians");
            AssertInvalidSnapshot(
                snapshot =>
                    snapshot.ActiveOperation =
                        RunningOperation(
                            snapshot.ServerTimeUtc,
                            HiveDoctrineRecruitmentClient
                                .AwaitingCompletionStatus));
        }

        [Test]
        public void DetachedAlteredOrImpossibleReceiptsAreRejected()
        {
            AssertInvalidStart(
                response => response.Receipt.PlayerId = Guid.NewGuid());
            AssertInvalidStart(
                response =>
                    response.Receipt.IdempotencyKey = "other");
            AssertInvalidStart(
                response => response.Receipt.RevisionAfter++);
            AssertInvalidStart(
                response =>
                    response.Receipt.Code =
                        HiveDoctrineRecruitmentClient.ClaimedCode);
            AssertInvalidStart(
                response =>
                    response.Snapshot.ActiveOperation.OperationId =
                        Guid.NewGuid());
            AssertInvalidClaim(
                response =>
                    response.Snapshot.ActiveOperation =
                        RunningOperation(
                            response.Snapshot.ServerTimeUtc));
        }

        [Test]
        public async Task OriginalReceiptsAcceptNewerAuthoritativeReplay()
        {
            RemoteDoctrineRecruitmentResponse startReplay =
                ValidClaimResponse(4, "start-replay");
            startReplay.Receipt =
                Receipt(
                    "start-replay",
                    3,
                    4,
                    HiveDoctrineRecruitmentClient.StartedCode,
                    startReplay.Snapshot.ServerTimeUtc.AddMinutes(-1));
            startReplay.Snapshot.Revision = 5;
            await NewClient(
                    new FakeSessionSource(
                        new GameAccountSession(PlayerId, Token)),
                    new ScriptedTransport(startReplay))
                .StartAsync(
                    HiveId,
                    "guardians",
                    3,
                    "start-replay");

            RemoteDoctrineRecruitmentResponse claimReplay =
                ValidClaimResponse(4, "claim-replay");
            claimReplay.Snapshot.Revision = 6;
            claimReplay.Snapshot.ActiveOperation =
                RunningOperation(
                    claimReplay.Snapshot.ServerTimeUtc,
                    operationId: Guid.NewGuid());
            await NewClient(
                    new FakeSessionSource(
                        new GameAccountSession(PlayerId, Token)),
                    new ScriptedTransport(claimReplay))
                .ClaimAsync(
                    HiveId,
                    OperationId,
                    "guardians",
                    4,
                    "claim-replay");
        }

        [Test]
        public async Task UnauthorizedRefreshesOnceAndReplaysSameRequest()
        {
            var source = new RefreshableSessionSource(
                PlayerId,
                Token,
                "rotated-token");
            var transport = new ScriptedTransport(
                new AuthenticatedGameRestException(
                    AuthenticatedGameRestError.Unauthorized,
                    "game.session_required",
                    401),
                ValidStartResponse(3, "stable-after-401"));
            var client = NewClient(source, transport);

            await client.StartAsync(
                HiveId,
                "guardians",
                3,
                "stable-after-401");

            Assert.That(source.RefreshCalls, Is.EqualTo(1));
            Assert.That(transport.Requests.Count, Is.EqualTo(2));
            Assert.That(
                transport.Requests[0],
                Is.SameAs(transport.Requests[1]));
            Assert.That(
                transport.Tokens,
                Is.EqualTo(new[] { Token, "rotated-token" }));
        }

        [Test]
        public void NetworkFailureNeverRetriesOrCachesMutation()
        {
            var source = new RefreshableSessionSource(
                PlayerId,
                Token,
                "rotated-token");
            var transport = new ScriptedTransport(
                new AuthenticatedGameRestException(
                    AuthenticatedGameRestError.NetworkFailure,
                    "game.network_unavailable"));
            var store = new MemoryCacheStore();
            var client = NewClient(
                source,
                transport,
                NewCache(store, ValidSnapshot().ServerTimeUtc));

            HivePerimeterClientException error =
                Assert.ThrowsAsync<HivePerimeterClientException>(
                    async () =>
                        await client.StartAsync(
                            HiveId,
                            "guardians",
                            3,
                            "no-offline-start"));

            Assert.That(
                error.Error,
                Is.EqualTo(HivePerimeterClientError.TransportFailure));
            Assert.That(transport.Requests.Count, Is.EqualTo(1));
            Assert.That(source.RefreshCalls, Is.Zero);
            Assert.That(store.LoadCalls, Is.Zero);
            Assert.That(store.SaveCalls, Is.Zero);
        }

        [Test]
        public async Task NetworkReadFallsBackToSamePlayerProtectedSnapshot()
        {
            RemoteDoctrineRecruitmentSnapshot snapshot = ValidSnapshot();
            var store = new MemoryCacheStore();
            var source = new RefreshableSessionSource(
                PlayerId,
                Token,
                "rotated-token");
            var transport = new ScriptedTransport(
                snapshot,
                new AuthenticatedGameRestException(
                    AuthenticatedGameRestError.NetworkFailure,
                    "game.network_unavailable"));
            var client = NewClient(
                source,
                transport,
                NewCache(store, snapshot.ServerTimeUtc));

            await client.ReadAsync(HiveId);
            RemoteDoctrineRecruitmentSnapshot offline =
                await client.ReadAsync(HiveId);

            Assert.That(offline.PlayerId, Is.EqualTo(PlayerId));
            Assert.That(
                client.LastReadSource,
                Is.EqualTo(GameReadSource.ProtectedCache));
            Assert.That(
                client.LastReadCachedAtUtc,
                Is.EqualTo(snapshot.ServerTimeUtc));
        }

        [Test]
        public void SystemTextCodecAcceptsExactPublicServerEnvelope()
        {
            const string json =
                "{\"playerId\":\"11111111-2222-3333-4444-555555555555\"," +
                "\"hiveId\":\"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee\"," +
                "\"contractVersion\":\"phase4-combat-recruitment-v1\"," +
                "\"catalogVersion\":\"phase4-combat-v1\"," +
                "\"revision\":3," +
                "\"serverTimeUtc\":\"2026-07-23T12:00:00+00:00\"," +
                "\"offers\":[" +
                "{\"family\":\"guardians\",\"batchSize\":4,\"honeyCost\":680," +
                "\"pollenCost\":180,\"duration\":\"00:00:14\"}," +
                "{\"family\":\"wingrunners\",\"batchSize\":6,\"honeyCost\":420," +
                "\"pollenCost\":260,\"duration\":\"00:00:14\"}," +
                "{\"family\":\"darters\",\"batchSize\":8,\"honeyCost\":500," +
                "\"pollenCost\":120,\"duration\":\"00:00:14\"}]," +
                "\"balances\":{\"honey\":{\"amount\":800,\"capacity\":1000}," +
                "\"pollen\":{\"amount\":700,\"capacity\":1000}}," +
                "\"counts\":{\"guardians\":4,\"wingrunners\":6,\"darters\":8}," +
                "\"legacyRoles\":[\"Soldats\",\"Gardiennes\",\"Eclaireuses\"]," +
                "\"activeOperation\":null}";

            RemoteDoctrineRecruitmentSnapshot value =
                new SystemTextGameJsonCodec()
                    .Deserialize<RemoteDoctrineRecruitmentSnapshot>(json);

            Assert.That(value.PlayerId, Is.EqualTo(PlayerId));
            Assert.That(value.Offers.Count, Is.EqualTo(3));
            Assert.That(
                value.Offers[0].Duration,
                Is.EqualTo(TimeSpan.FromSeconds(14)));
            Assert.That(value.Counts["darters"], Is.EqualTo(8));
        }

        [Test]
        public void ProofRowsExposeAuthorityWithoutLeakingCredentials()
        {
            var client = NewClient(
                new FakeSessionSource(
                    new GameAccountSession(PlayerId, Token)),
                new ScriptedTransport());
            string proof = string.Join("\n", client.ProofRows());

            Assert.That(
                proof,
                Does.Contain(
                    "doctrine_recruitment_roster_authority:server"));
            Assert.That(
                proof,
                Does.Contain(
                    "doctrine_recruitment_cache_read_only:true"));
            Assert.That(
                proof,
                Does.Contain(
                    "doctrine_recruitment_local_claim:false"));
            Assert.That(proof, Does.Not.Contain(Token));
        }

        private static HiveDoctrineRecruitmentClient NewClient(
            IGameAccountSessionSource source,
            IAuthenticatedGameRestTransport transport,
            ProtectedGameReadCache cache = null)
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
            return new HiveDoctrineRecruitmentClient(
                gate,
                source,
                transport,
                cache);
        }

        private static ProtectedGameReadCache NewCache(
            MemoryCacheStore store,
            DateTimeOffset now)
        {
            return new ProtectedGameReadCache(
                store,
                new SystemTextGameJsonCodec(),
                new FixedClock(now));
        }

        private static RemoteDoctrineRecruitmentSnapshot ValidSnapshot()
        {
            DateTimeOffset now =
                new DateTimeOffset(
                    2026,
                    7,
                    23,
                    12,
                    0,
                    0,
                    TimeSpan.Zero);
            return new RemoteDoctrineRecruitmentSnapshot
            {
                PlayerId = PlayerId,
                HiveId = HiveId,
                ContractVersion =
                    HiveDoctrineRecruitmentClient.ContractVersion,
                CatalogVersion =
                    HiveDoctrineRecruitmentClient.CatalogVersion,
                Revision = 3,
                ServerTimeUtc = now,
                Offers = new List<RemoteDoctrineRecruitmentOffer>
                {
                    new RemoteDoctrineRecruitmentOffer
                    {
                        Family = "guardians",
                        BatchSize = 4,
                        HoneyCost = 680,
                        PollenCost = 180,
                        Duration = TimeSpan.FromSeconds(14)
                    },
                    new RemoteDoctrineRecruitmentOffer
                    {
                        Family = "wingrunners",
                        BatchSize = 6,
                        HoneyCost = 420,
                        PollenCost = 260,
                        Duration = TimeSpan.FromSeconds(14)
                    },
                    new RemoteDoctrineRecruitmentOffer
                    {
                        Family = "darters",
                        BatchSize = 8,
                        HoneyCost = 500,
                        PollenCost = 120,
                        Duration = TimeSpan.FromSeconds(14)
                    }
                },
                Balances =
                    new Dictionary
                        <string, RemoteDoctrineRecruitmentBalance>
                    {
                        ["honey"] =
                            new RemoteDoctrineRecruitmentBalance
                            {
                                Amount = 900,
                                Capacity = 1000
                            },
                        ["pollen"] =
                            new RemoteDoctrineRecruitmentBalance
                            {
                                Amount = 800,
                                Capacity = 1000
                            }
                    },
                Counts = new Dictionary<string, long>
                {
                    ["guardians"] = 0,
                    ["wingrunners"] = 0,
                    ["darters"] = 0
                },
                LegacyRoles =
                    new List<string>
                    {
                        "Soldats",
                        "Gardiennes",
                        "Eclaireuses"
                    },
                ActiveOperation = null
            };
        }

        private static RemoteDoctrineRecruitmentOperation
            RunningOperation(
                DateTimeOffset now,
                string status =
                    HiveDoctrineRecruitmentClient.RunningStatus,
                Guid operationId = default(Guid))
        {
            DateTimeOffset started = now.AddSeconds(-4);
            return new RemoteDoctrineRecruitmentOperation
            {
                OperationId =
                    operationId == Guid.Empty
                        ? OperationId
                        : operationId,
                Family = "guardians",
                BatchSize = 4,
                StartedAtUtc = started,
                EndsAtUtc = started.AddSeconds(14),
                Status = status
            };
        }

        private static RemoteDoctrineRecruitmentResponse
            ValidStartResponse(
                long expectedRevision,
                string key)
        {
            RemoteDoctrineRecruitmentSnapshot snapshot =
                ValidSnapshot();
            snapshot.Revision = expectedRevision + 1;
            snapshot.ActiveOperation =
                RunningOperation(snapshot.ServerTimeUtc);
            return new RemoteDoctrineRecruitmentResponse
            {
                Receipt =
                    Receipt(
                        key,
                        expectedRevision,
                        expectedRevision + 1,
                        HiveDoctrineRecruitmentClient.StartedCode,
                        snapshot.ServerTimeUtc),
                Snapshot = snapshot
            };
        }

        private static RemoteDoctrineRecruitmentResponse
            ValidClaimResponse(
                long expectedRevision,
                string key)
        {
            RemoteDoctrineRecruitmentSnapshot snapshot =
                ValidSnapshot();
            snapshot.Revision = expectedRevision + 1;
            snapshot.Counts["guardians"] = 4;
            snapshot.ActiveOperation = null;
            return new RemoteDoctrineRecruitmentResponse
            {
                Receipt =
                    Receipt(
                        key,
                        expectedRevision,
                        expectedRevision + 1,
                        HiveDoctrineRecruitmentClient.ClaimedCode,
                        snapshot.ServerTimeUtc),
                Snapshot = snapshot
            };
        }

        private static RemoteDoctrineRecruitmentReceipt Receipt(
            string key,
            long revisionBefore,
            long revisionAfter,
            string code,
            DateTimeOffset acceptedAt)
        {
            return new RemoteDoctrineRecruitmentReceipt
            {
                PlayerId = PlayerId,
                HiveId = HiveId,
                IdempotencyKey = key,
                OperationId = OperationId,
                Family = "guardians",
                BatchSize = 4,
                RevisionBefore = revisionBefore,
                RevisionAfter = revisionAfter,
                AcceptedAtUtc = acceptedAt,
                Code = code
            };
        }

        private static void AssertInvalidSnapshot(
            Action<RemoteDoctrineRecruitmentSnapshot> mutate)
        {
            RemoteDoctrineRecruitmentSnapshot snapshot =
                ValidSnapshot();
            mutate(snapshot);
            var client = NewClient(
                new FakeSessionSource(
                    new GameAccountSession(PlayerId, Token)),
                new ScriptedTransport(snapshot));
            HivePerimeterClientException error =
                Assert.ThrowsAsync<HivePerimeterClientException>(
                    async () => await client.ReadAsync(HiveId));
            Assert.That(
                error.Error,
                Is.EqualTo(HivePerimeterClientError.InvalidResponse));
        }

        private static void AssertInvalidStart(
            Action<RemoteDoctrineRecruitmentResponse> mutate)
        {
            RemoteDoctrineRecruitmentResponse response =
                ValidStartResponse(3, "key");
            mutate(response);
            var client = NewClient(
                new FakeSessionSource(
                    new GameAccountSession(PlayerId, Token)),
                new ScriptedTransport(response));
            HivePerimeterClientException error =
                Assert.ThrowsAsync<HivePerimeterClientException>(
                    async () =>
                        await client.StartAsync(
                            HiveId,
                            "guardians",
                            3,
                            "key"));
            Assert.That(
                error.Error,
                Is.EqualTo(HivePerimeterClientError.InvalidResponse));
        }

        private static void AssertInvalidClaim(
            Action<RemoteDoctrineRecruitmentResponse> mutate)
        {
            RemoteDoctrineRecruitmentResponse response =
                ValidClaimResponse(4, "key");
            mutate(response);
            var client = NewClient(
                new FakeSessionSource(
                    new GameAccountSession(PlayerId, Token)),
                new ScriptedTransport(response));
            HivePerimeterClientException error =
                Assert.ThrowsAsync<HivePerimeterClientException>(
                    async () =>
                        await client.ClaimAsync(
                            HiveId,
                            OperationId,
                            "guardians",
                            4,
                            "key"));
            Assert.That(
                error.Error,
                Is.EqualTo(HivePerimeterClientError.InvalidResponse));
        }

        private static void AssertInvalidRequest(
            Func<Task> operation)
        {
            HivePerimeterClientException error =
                Assert.ThrowsAsync<HivePerimeterClientException>(
                    async () => await operation());
            Assert.That(
                error.Error,
                Is.EqualTo(HivePerimeterClientError.InvalidRequest));
        }

        private sealed class FakeSessionSource :
            IGameAccountSessionSource
        {
            private readonly GameAccountSession session;

            public FakeSessionSource(GameAccountSession session)
            {
                this.session = session;
            }

            public int Calls { get; private set; }

            public bool TryGetSession(out GameAccountSession value)
            {
                Calls++;
                value = session;
                return value != null;
            }
        }

        private sealed class RefreshableSessionSource :
            IRefreshableGameAccountSessionSource
        {
            private readonly Guid playerId;
            private readonly string replacementToken;
            private GameAccountSession session;

            public RefreshableSessionSource(
                Guid playerId,
                string token,
                string replacementToken)
            {
                this.playerId = playerId;
                this.replacementToken = replacementToken;
                session = new GameAccountSession(playerId, token);
            }

            public int RefreshCalls { get; private set; }

            public bool TryGetSession(out GameAccountSession value)
            {
                value = session;
                return value != null;
            }

            public bool TryGetKnownPlayerId(out Guid value)
            {
                value = playerId;
                return value != Guid.Empty;
            }

            public Task<GameAccountSession> GetFreshSessionAsync(
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(session);
            }

            public Task<GameAccountSession>
                RefreshAfterUnauthorizedAsync(
                    string rejectedAccessToken,
                    CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RefreshCalls++;
                session =
                    new GameAccountSession(
                        playerId,
                        replacementToken);
                return Task.FromResult(session);
            }

            public Task InvalidateUnauthorizedSessionAsync(
                string rejectedAccessToken,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                session = null;
                return Task.CompletedTask;
            }
        }

        private sealed class ScriptedTransport :
            IAuthenticatedGameRestTransport
        {
            private readonly Queue<object> steps;

            public ScriptedTransport(params object[] steps)
            {
                this.steps =
                    new Queue<object>(
                        steps ?? Array.Empty<object>());
            }

            public List<AuthenticatedGameRestRequest> Requests
            {
                get;
            } = new List<AuthenticatedGameRestRequest>();

            public List<string> Tokens
            {
                get;
            } = new List<string>();

            public Task<T> SendAsync<T>(
                AuthenticatedGameRestRequest request,
                string bearerAccessToken,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Requests.Add(request);
                Tokens.Add(bearerAccessToken);
                object step =
                    steps.Count == 0
                        ? null
                        : steps.Dequeue();
                if (step is Exception failure) throw failure;
                return Task.FromResult((T)step);
            }
        }

        private sealed class MemoryCacheStore :
            IProtectedGameReadCacheStore
        {
            public bool IsProtectionAvailable { get; set; } = true;
            public string Value { get; set; }
            public int LoadCalls { get; private set; }
            public int SaveCalls { get; private set; }

            public Task<string> LoadAsync(
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                LoadCalls++;
                return Task.FromResult(Value);
            }

            public Task SaveAsync(
                string value,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                SaveCalls++;
                Value = value;
                return Task.CompletedTask;
            }

            public Task DeleteAsync(
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Value = null;
                return Task.CompletedTask;
            }
        }

        private sealed class FixedClock :
            IMobileAccountSessionClock
        {
            public FixedClock(DateTimeOffset utcNow)
            {
                UtcNow = utcNow;
            }

            public DateTimeOffset UtcNow { get; }
        }
    }
}
