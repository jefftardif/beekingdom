using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class HiveResearchClientTests
    {
        private static readonly Guid PlayerId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly Guid HiveId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly Guid OperationId = Guid.Parse("99999999-8888-7777-6666-555555555555");
        private const string Token = "research-test-token";

        [Test]
        public void ClosedOfficialGateStopsBeforeCredentialsAndTransport()
        {
            var source = new FakeSessionSource(new GameAccountSession(PlayerId, Token));
            var transport = new ScriptedTransport(ValidSnapshot());
            var client = new HiveResearchClient(new MobileAccountSessionGate(), source, transport);

            HivePerimeterClientException error = Assert.ThrowsAsync<HivePerimeterClientException>(
                async () => await client.ReadAsync(HiveId));

            Assert.That(error.Error, Is.EqualTo(HivePerimeterClientError.NotConfigured));
            Assert.That(source.Calls, Is.Zero);
            Assert.That(transport.Requests, Is.Empty);
        }

        [Test]
        public async Task ValidatedReadUsesExactRouteAndProtectedGetCache()
        {
            RemoteHiveResearchSnapshot snapshot = ValidSnapshot();
            var store = new MemoryCacheStore();
            var transport = new ScriptedTransport(snapshot);
            var client = NewClient(new FakeSessionSource(new GameAccountSession(PlayerId, Token)), transport,
                NewCache(store, snapshot.ServerTimeUtc));

            RemoteHiveResearchSnapshot result = await client.ReadAsync(HiveId);

            Assert.That(result, Is.SameAs(snapshot));
            AuthenticatedGameRestRequest request = transport.Requests.Single();
            Assert.That(request.Method, Is.EqualTo("GET"));
            Assert.That(request.Path, Is.EqualTo("/game/v1/hives/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/research"));
            Assert.That(request.Body, Is.Null);
            Assert.That(store.SaveCalls, Is.EqualTo(1));
            Assert.That(client.LastReadSource, Is.EqualTo(GameReadSource.Server));
        }

        [Test]
        public async Task StartSendsOnlyRevisionAndStableKeyThenRefreshesCache()
        {
            RemoteHiveResearchMutationResponse response = ValidStartResponse(3, "start-stable");
            var store = new MemoryCacheStore();
            var transport = new ScriptedTransport(response);
            var client = NewClient(new FakeSessionSource(new GameAccountSession(PlayerId, Token)), transport,
                NewCache(store, response.Snapshot.ServerTimeUtc));

            await client.StartAsync(HiveId, HiveResearchClient.ForagingRoutesId, 3, "start-stable");

            AuthenticatedGameRestRequest request = transport.Requests.Single();
            Assert.That(request.Method, Is.EqualTo("POST"));
            Assert.That(request.Path, Does.EndWith("/research/foraging_routes_i/start"));
            var body = request.Body as HiveResearchMutationRequest;
            Assert.That(body.ExpectedRevision, Is.EqualTo(3));
            Assert.That(body.IdempotencyKey, Is.EqualTo("start-stable"));
            Assert.That(request.Body.GetType().GetProperties().Select(property => property.Name),
                Is.EquivalentTo(new[] { "ExpectedRevision", "IdempotencyKey" }));
            Assert.That(store.SaveCalls, Is.EqualTo(1));
        }

        [Test]
        public async Task CompleteSendsOperationRevisionAndNoLocalResult()
        {
            RemoteHiveResearchMutationResponse response = ValidCompleteResponse(4, "complete-stable");
            var transport = new ScriptedTransport(response);
            var client = NewClient(new FakeSessionSource(new GameAccountSession(PlayerId, Token)), transport);

            await client.CompleteAsync(HiveId, OperationId, 4, "complete-stable");

            AuthenticatedGameRestRequest request = transport.Requests.Single();
            Assert.That(request.Path, Does.EndWith("/research/99999999-8888-7777-6666-555555555555/complete"));
            var body = request.Body as HiveResearchMutationRequest;
            Assert.That(body.ExpectedRevision, Is.EqualTo(4));
            Assert.That(body.IdempotencyKey, Is.EqualTo("complete-stable"));
        }

        [Test]
        public void InvalidLocalMutationsNeverReachSessionOrTransport()
        {
            var source = new FakeSessionSource(new GameAccountSession(PlayerId, Token));
            var transport = new ScriptedTransport();
            var client = NewClient(source, transport);

            AssertInvalidRequest(() => client.StartAsync(HiveId, "unknown", 0, "key"));
            AssertInvalidRequest(() => client.StartAsync(HiveId, HiveResearchClient.ForagingRoutesId, -1, "key"));
            AssertInvalidRequest(() => client.StartAsync(HiveId, HiveResearchClient.ForagingRoutesId, 0, " key"));
            AssertInvalidRequest(() => client.CompleteAsync(HiveId, Guid.Empty, 0, "key"));
            AssertInvalidRequest(() => client.CompleteAsync(HiveId, OperationId, long.MaxValue, "key"));

            Assert.That(source.Calls, Is.Zero);
            Assert.That(transport.Requests, Is.Empty);
        }

        [Test]
        public void ForeignMalformedOrInconsistentSnapshotsAreRejected()
        {
            AssertInvalidSnapshot(snapshot => snapshot.PlayerId = Guid.NewGuid());
            AssertInvalidSnapshot(snapshot => snapshot.ContractVersion = "wrong-v1");
            AssertInvalidSnapshot(snapshot => snapshot.CatalogVersion = "Unsafe Catalog");
            AssertInvalidSnapshot(snapshot => snapshot.ServerTimeUtc = snapshot.ServerTimeUtc.ToOffset(TimeSpan.FromHours(-4)));
            AssertInvalidSnapshot(snapshot => snapshot.Balances["honey"].Amount = snapshot.Balances["honey"].Capacity + 1);
            AssertInvalidSnapshot(snapshot => snapshot.Offers[0].Duration = TimeSpan.FromDays(8));
            AssertInvalidSnapshot(snapshot => snapshot.Offers[0].Effects.WaxCapacityBonusBps = -1);
            AssertInvalidSnapshot(snapshot => snapshot.Offers[0].Effects.HoneyProductionBonusBps = 0);
            AssertInvalidSnapshot(snapshot => snapshot.Offers[0].Prerequisites = new List<string> { "unknown_research" });
            AssertInvalidSnapshot(snapshot => snapshot.Offers.Add(snapshot.Offers[0]));
            AssertInvalidSnapshot(snapshot => snapshot.Completed.Add(new RemoteHiveResearchCompletion
            {
                ResearchId = HiveResearchClient.ForagingRoutesId,
                CompletedAtUtc = snapshot.ServerTimeUtc,
                Effects = ForagingEffects()
            }));
            AssertInvalidSnapshot(snapshot => snapshot.ActiveOperation = ActiveOperation(
                snapshot.ServerTimeUtc, HiveResearchClient.RunningStatus, snapshot.ServerTimeUtc));
        }

        [Test]
        public async Task CompoundEffectsAndKnownPrerequisitesAreAccepted()
        {
            RemoteHiveResearchSnapshot snapshot = ValidSnapshot();
            snapshot.Offers[1].Effects = new RemoteHiveResearchEffects { WaxCapacityBonusBps = 800, WaxProductionBonusBps = 300 };
            snapshot.Offers[1].Prerequisites = new List<string> { HiveResearchClient.ForagingRoutesId };
            var client = NewClient(new FakeSessionSource(new GameAccountSession(PlayerId, Token)), new ScriptedTransport(snapshot));

            RemoteHiveResearchSnapshot result = await client.ReadAsync(HiveId);

            Assert.That(result.Offers[1].Effects.WaxProductionBonusBps, Is.EqualTo(300));
            Assert.That(result.Offers[1].Prerequisites, Is.EquivalentTo(new[] { HiveResearchClient.ForagingRoutesId }));
        }

        [Test]
        public void DetachedOrAlteredMutationReceiptsAreRejected()
        {
            AssertInvalidStart(response => response.Receipt.PlayerId = Guid.NewGuid());
            AssertInvalidStart(response => response.Receipt.IdempotencyKey = "other");
            AssertInvalidStart(response => response.Receipt.Revision++);
            AssertInvalidStart(response => response.Receipt.Code = HiveResearchClient.CompletedCode);
            AssertInvalidStart(response => response.Snapshot.ActiveOperation.OperationId = Guid.NewGuid());
            AssertInvalidComplete(response => response.Snapshot.Completed.Clear());
            AssertInvalidComplete(response => response.Snapshot.ActiveOperation = ActiveOperation(response.Snapshot.ServerTimeUtc));
        }

        [Test]
        public async Task OriginalReceiptRemainsValidWhenReplayReturnsNewerCompletedSnapshot()
        {
            RemoteHiveResearchMutationResponse startReplay = ValidCompleteResponse(3, "start-replay");
            startReplay.Receipt = Receipt("start-replay", 4, HiveResearchClient.StartedCode,
                startReplay.Snapshot.ServerTimeUtc.AddMinutes(-1));
            startReplay.Snapshot.Revision = 5;
            var startClient = NewClient(new FakeSessionSource(new GameAccountSession(PlayerId, Token)),
                new ScriptedTransport(startReplay));
            await startClient.StartAsync(HiveId, HiveResearchClient.ForagingRoutesId, 3, "start-replay");

            RemoteHiveResearchMutationResponse completeReplay = ValidCompleteResponse(4, "complete-replay");
            completeReplay.Snapshot.Revision = 6;
            var completeClient = NewClient(new FakeSessionSource(new GameAccountSession(PlayerId, Token)),
                new ScriptedTransport(completeReplay));
            await completeClient.CompleteAsync(HiveId, OperationId, 4, "complete-replay");
        }

        [Test]
        public async Task UnauthorizedRefreshesOnceAndReplaysIdenticalMutation()
        {
            var source = new RefreshableSessionSource(PlayerId, Token, "rotated-token");
            var transport = new ScriptedTransport(
                new AuthenticatedGameRestException(AuthenticatedGameRestError.Unauthorized, "game.session_required", 401),
                ValidStartResponse(3, "stable-after-401"));
            var client = NewClient(source, transport);

            await client.StartAsync(HiveId, HiveResearchClient.ForagingRoutesId, 3, "stable-after-401");

            Assert.That(source.RefreshCalls, Is.EqualTo(1));
            Assert.That(transport.Requests.Count, Is.EqualTo(2));
            Assert.That(transport.Requests[0], Is.SameAs(transport.Requests[1]));
            Assert.That(transport.Tokens, Is.EqualTo(new[] { Token, "rotated-token" }));
        }

        [Test]
        public void NetworkMutationNeverRetriesOrReadsOfflineCache()
        {
            var source = new RefreshableSessionSource(PlayerId, Token, "rotated-token");
            var transport = new ScriptedTransport(new AuthenticatedGameRestException(
                AuthenticatedGameRestError.NetworkFailure, "game.network_unavailable"));
            var store = new MemoryCacheStore();
            var client = NewClient(source, transport, NewCache(store, ValidSnapshot().ServerTimeUtc));

            HivePerimeterClientException error = Assert.ThrowsAsync<HivePerimeterClientException>(async () =>
                await client.StartAsync(HiveId, HiveResearchClient.ForagingRoutesId, 3, "no-offline-start"));

            Assert.That(error.Error, Is.EqualTo(HivePerimeterClientError.TransportFailure));
            Assert.That(transport.Requests.Count, Is.EqualTo(1));
            Assert.That(source.RefreshCalls, Is.Zero);
            Assert.That(store.LoadCalls, Is.Zero);
            Assert.That(store.SaveCalls, Is.Zero);
        }

        [Test]
        public async Task NetworkReadFallsBackToSamePlayerProtectedSnapshotAsReadOnly()
        {
            RemoteHiveResearchSnapshot snapshot = ValidSnapshot();
            var store = new MemoryCacheStore();
            var source = new RefreshableSessionSource(PlayerId, Token, "rotated-token");
            var transport = new ScriptedTransport(snapshot,
                new AuthenticatedGameRestException(AuthenticatedGameRestError.NetworkFailure, "game.network_unavailable"));
            var client = NewClient(source, transport, NewCache(store, snapshot.ServerTimeUtc));

            await client.ReadAsync(HiveId);
            RemoteHiveResearchSnapshot offline = await client.ReadAsync(HiveId);

            Assert.That(offline.PlayerId, Is.EqualTo(PlayerId));
            Assert.That(client.LastReadSource, Is.EqualTo(GameReadSource.ProtectedCache));
            Assert.That(client.LastReadCachedAtUtc, Is.EqualTo(snapshot.ServerTimeUtc));
        }

        [Test]
        public void SystemTextCodecAcceptsExactServerEnvelope()
        {
            const string json = "{\"playerId\":\"11111111-2222-3333-4444-555555555555\"," +
                "\"hiveId\":\"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee\",\"contractVersion\":\"living-hive-research-v1\"," +
                "\"catalogVersion\":\"test-v1\",\"revision\":3,\"serverTimeUtc\":\"2026-07-22T12:00:00+00:00\"," +
                "\"balances\":{\"honey\":{\"amount\":500,\"capacity\":1000},\"pollen\":{\"amount\":500,\"capacity\":1000}}," +
                "\"completed\":[],\"offers\":[{\"researchId\":\"foraging_routes_i\",\"duration\":\"00:04:00\"," +
                "\"costs\":{\"honey\":240,\"pollen\":90},\"effects\":{\"honeyProductionBonusBps\":200,\"waxCapacityBonusBps\":0}}]," +
                "\"activeOperation\":null}";

            RemoteHiveResearchSnapshot value = new SystemTextGameJsonCodec().Deserialize<RemoteHiveResearchSnapshot>(json);

            Assert.That(value.PlayerId, Is.EqualTo(PlayerId));
            Assert.That(value.Offers.Single().Duration, Is.EqualTo(TimeSpan.FromMinutes(4)));
            Assert.That(value.Offers.Single().Effects.HoneyProductionBonusBps, Is.EqualTo(200));
        }

        [Test]
        public void ProofRowsKeepMobileAndServerResponsibilitiesExplicitWithoutSecrets()
        {
            var client = NewClient(new FakeSessionSource(new GameAccountSession(PlayerId, Token)), new ScriptedTransport());
            string proof = string.Join("\n", client.ProofRows());

            Assert.That(proof, Does.Contain("research_cost_authority:server"));
            Assert.That(proof, Does.Contain("research_effect_authority:server"));
            Assert.That(proof, Does.Contain("research_cache_read_only:true"));
            Assert.That(proof, Does.Contain("research_local_completion:false"));
            Assert.That(proof, Does.Not.Contain(Token));
        }

        private static HiveResearchClient NewClient(
            IGameAccountSessionSource source,
            IAuthenticatedGameRestTransport transport,
            ProtectedGameReadCache cache = null)
        {
            var gate = new MobileAccountSessionGate();
            gate.ConfigureTransport(true);
            gate.Apply(AccountSessionReadinessSnapshot.FromServer(true, true, true, true, true));
            return new HiveResearchClient(gate, source, transport, cache);
        }

        private static ProtectedGameReadCache NewCache(MemoryCacheStore store, DateTimeOffset now)
        {
            return new ProtectedGameReadCache(store, new SystemTextGameJsonCodec(), new FixedClock(now));
        }

        private static RemoteHiveResearchSnapshot ValidSnapshot()
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
                        Effects = ForagingEffects()
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

        private static RemoteHiveResearchEffects ForagingEffects()
        {
            return new RemoteHiveResearchEffects { HoneyProductionBonusBps = 200 };
        }

        private static RemoteHiveResearchOperation ActiveOperation(
            DateTimeOffset now,
            string status = HiveResearchClient.RunningStatus,
            DateTimeOffset? completes = null)
        {
            return new RemoteHiveResearchOperation
            {
                OperationId = OperationId,
                ResearchId = HiveResearchClient.ForagingRoutesId,
                StartedAtUtc = now,
                CompletesAtUtc = completes ?? now.AddMinutes(4),
                Status = status
            };
        }

        private static RemoteHiveResearchMutationResponse ValidStartResponse(long expectedRevision, string key)
        {
            RemoteHiveResearchSnapshot snapshot = ValidSnapshot();
            snapshot.Revision = expectedRevision + 1;
            snapshot.ActiveOperation = ActiveOperation(snapshot.ServerTimeUtc);
            return new RemoteHiveResearchMutationResponse
            {
                Receipt = Receipt(key, snapshot.Revision, HiveResearchClient.StartedCode, snapshot.ServerTimeUtc),
                Snapshot = snapshot
            };
        }

        private static RemoteHiveResearchMutationResponse ValidCompleteResponse(long expectedRevision, string key)
        {
            RemoteHiveResearchSnapshot snapshot = ValidSnapshot();
            snapshot.Revision = expectedRevision + 1;
            snapshot.Completed.Add(new RemoteHiveResearchCompletion
            {
                ResearchId = HiveResearchClient.ForagingRoutesId,
                CompletedAtUtc = snapshot.ServerTimeUtc,
                Effects = ForagingEffects()
            });
            snapshot.Offers.RemoveAll(item => item.ResearchId == HiveResearchClient.ForagingRoutesId);
            return new RemoteHiveResearchMutationResponse
            {
                Receipt = Receipt(key, snapshot.Revision, HiveResearchClient.CompletedCode, snapshot.ServerTimeUtc),
                Snapshot = snapshot
            };
        }

        private static RemoteHiveResearchReceipt Receipt(string key, long revision, string code, DateTimeOffset acceptedAt)
        {
            return new RemoteHiveResearchReceipt
            {
                PlayerId = PlayerId,
                HiveId = HiveId,
                IdempotencyKey = key,
                OperationId = OperationId,
                ResearchId = HiveResearchClient.ForagingRoutesId,
                Revision = revision,
                AcceptedAtUtc = acceptedAt,
                Code = code
            };
        }

        private static void AssertInvalidSnapshot(Action<RemoteHiveResearchSnapshot> mutate)
        {
            RemoteHiveResearchSnapshot snapshot = ValidSnapshot();
            mutate(snapshot);
            var client = NewClient(new FakeSessionSource(new GameAccountSession(PlayerId, Token)), new ScriptedTransport(snapshot));
            HivePerimeterClientException error = Assert.ThrowsAsync<HivePerimeterClientException>(
                async () => await client.ReadAsync(HiveId));
            Assert.That(error.Error, Is.EqualTo(HivePerimeterClientError.InvalidResponse));
        }

        private static void AssertInvalidStart(Action<RemoteHiveResearchMutationResponse> mutate)
        {
            RemoteHiveResearchMutationResponse response = ValidStartResponse(3, "key");
            mutate(response);
            var client = NewClient(new FakeSessionSource(new GameAccountSession(PlayerId, Token)), new ScriptedTransport(response));
            HivePerimeterClientException error = Assert.ThrowsAsync<HivePerimeterClientException>(async () =>
                await client.StartAsync(HiveId, HiveResearchClient.ForagingRoutesId, 3, "key"));
            Assert.That(error.Error, Is.EqualTo(HivePerimeterClientError.InvalidResponse));
        }

        private static void AssertInvalidComplete(Action<RemoteHiveResearchMutationResponse> mutate)
        {
            RemoteHiveResearchMutationResponse response = ValidCompleteResponse(4, "key");
            mutate(response);
            var client = NewClient(new FakeSessionSource(new GameAccountSession(PlayerId, Token)), new ScriptedTransport(response));
            HivePerimeterClientException error = Assert.ThrowsAsync<HivePerimeterClientException>(async () =>
                await client.CompleteAsync(HiveId, OperationId, 4, "key"));
            Assert.That(error.Error, Is.EqualTo(HivePerimeterClientError.InvalidResponse));
        }

        private static void AssertInvalidRequest(Func<Task> operation)
        {
            HivePerimeterClientException error = Assert.ThrowsAsync<HivePerimeterClientException>(async () => await operation());
            Assert.That(error.Error, Is.EqualTo(HivePerimeterClientError.InvalidRequest));
        }

        private sealed class FakeSessionSource : IGameAccountSessionSource
        {
            private readonly GameAccountSession session;
            public FakeSessionSource(GameAccountSession session) { this.session = session; }
            public int Calls { get; private set; }
            public bool TryGetSession(out GameAccountSession value) { Calls++; value = session; return value != null; }
        }

        private sealed class RefreshableSessionSource : IRefreshableGameAccountSessionSource
        {
            private readonly Guid playerId;
            private readonly string replacementToken;
            private GameAccountSession session;
            public RefreshableSessionSource(Guid playerId, string token, string replacementToken)
            {
                this.playerId = playerId;
                this.replacementToken = replacementToken;
                session = new GameAccountSession(playerId, token);
            }
            public int RefreshCalls { get; private set; }
            public bool TryGetSession(out GameAccountSession value) { value = session; return value != null; }
            public bool TryGetKnownPlayerId(out Guid value) { value = playerId; return value != Guid.Empty; }
            public Task<GameAccountSession> GetFreshSessionAsync(CancellationToken cancellationToken)
            { cancellationToken.ThrowIfCancellationRequested(); return Task.FromResult(session); }
            public Task<GameAccountSession> RefreshAfterUnauthorizedAsync(string rejectedAccessToken, CancellationToken cancellationToken)
            { cancellationToken.ThrowIfCancellationRequested(); RefreshCalls++; session = new GameAccountSession(playerId, replacementToken); return Task.FromResult(session); }
            public Task InvalidateUnauthorizedSessionAsync(string rejectedAccessToken, CancellationToken cancellationToken)
            { cancellationToken.ThrowIfCancellationRequested(); session = null; return Task.CompletedTask; }
        }

        private sealed class ScriptedTransport : IAuthenticatedGameRestTransport
        {
            private readonly Queue<object> steps;
            public ScriptedTransport(params object[] steps) { this.steps = new Queue<object>(steps ?? Array.Empty<object>()); }
            public List<AuthenticatedGameRestRequest> Requests { get; } = new List<AuthenticatedGameRestRequest>();
            public List<string> Tokens { get; } = new List<string>();
            public Task<T> SendAsync<T>(AuthenticatedGameRestRequest request, string bearerAccessToken, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Requests.Add(request);
                Tokens.Add(bearerAccessToken);
                object step = steps.Count == 0 ? null : steps.Dequeue();
                if (step is Exception failure) throw failure;
                return Task.FromResult((T)step);
            }
        }

        private sealed class MemoryCacheStore : IProtectedGameReadCacheStore
        {
            public bool IsProtectionAvailable { get; set; } = true;
            public string Value { get; set; }
            public int LoadCalls { get; private set; }
            public int SaveCalls { get; private set; }
            public Task<string> LoadAsync(CancellationToken cancellationToken)
            { cancellationToken.ThrowIfCancellationRequested(); LoadCalls++; return Task.FromResult(Value); }
            public Task SaveAsync(string value, CancellationToken cancellationToken)
            { cancellationToken.ThrowIfCancellationRequested(); SaveCalls++; Value = value; return Task.CompletedTask; }
            public Task DeleteAsync(CancellationToken cancellationToken)
            { cancellationToken.ThrowIfCancellationRequested(); Value = null; return Task.CompletedTask; }
        }

        private sealed class FixedClock : IMobileAccountSessionClock
        {
            public FixedClock(DateTimeOffset utcNow) { UtcNow = utcNow; }
            public DateTimeOffset UtcNow { get; }
        }
    }
}
