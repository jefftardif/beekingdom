using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class HiveOfflineProductionClientTests
    {
        private static readonly Guid PlayerId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly Guid HiveId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private const string Token = "production-test-token";

        [Test]
        public void ClosedOfficialGateStopsBeforeCredentialsAndTransport()
        {
            var source = new FakeSessionSource(new GameAccountSession(PlayerId, Token));
            var transport = new ScriptedTransport(ValidSnapshot());
            var client = new HiveOfflineProductionClient(new MobileAccountSessionGate(), source, transport);

            HivePerimeterClientException error = Assert.ThrowsAsync<HivePerimeterClientException>(
                async () => await client.ReadAsync(HiveId));

            Assert.That(error.Error, Is.EqualTo(HivePerimeterClientError.NotConfigured));
            Assert.That(source.Calls, Is.Zero);
            Assert.That(transport.Requests, Is.Empty);
        }

        [Test]
        public async Task ValidatedReadUsesExactRouteAndProtectedGetCache()
        {
            RemoteOfflineProductionSnapshot snapshot = ValidSnapshot();
            var store = new MemoryCacheStore();
            var cache = NewCache(store, snapshot.ServerTimeUtc);
            var transport = new ScriptedTransport(snapshot);
            var client = NewClient(new FakeSessionSource(new GameAccountSession(PlayerId, Token)), transport, cache);

            RemoteOfflineProductionSnapshot result = await client.ReadAsync(HiveId);

            Assert.That(result, Is.SameAs(snapshot));
            Assert.That(transport.Requests.Single().Method, Is.EqualTo("GET"));
            Assert.That(transport.Requests.Single().Path,
                Is.EqualTo("/game/v1/hives/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/offline-production"));
            Assert.That(transport.Requests.Single().Body, Is.Null);
            Assert.That(transport.Tokens.Single(), Is.EqualTo(Token));
            Assert.That(client.LastReadSource, Is.EqualTo(GameReadSource.Server));
            Assert.That(store.SaveCalls, Is.EqualTo(1));
        }

        [Test]
        public async Task CollectSendsOnlyRevisionAndStableKeyThenAcceptsBoundReceipt()
        {
            RemoteOfflineProductionCollectResponse response = ValidCollectResponse(3, "collect-stable");
            var transport = new ScriptedTransport(response);
            var client = NewClient(new FakeSessionSource(new GameAccountSession(PlayerId, Token)), transport);

            RemoteOfflineProductionCollectResponse result = await client.CollectAsync(
                HiveId,
                "honey_storage",
                3,
                "collect-stable");

            Assert.That(result, Is.SameAs(response));
            AuthenticatedGameRestRequest request = transport.Requests.Single();
            Assert.That(request.Method, Is.EqualTo("POST"));
            Assert.That(request.Path, Does.EndWith("/offline-production/honey_storage/collect"));
            var body = request.Body as OfflineProductionCollectRequest;
            Assert.That(body, Is.Not.Null);
            Assert.That(body.ExpectedProductionRevision, Is.EqualTo(3));
            Assert.That(body.IdempotencyKey, Is.EqualTo("collect-stable"));
        }

        [Test]
        public void InvalidLocalCollectionNeverReachesSessionOrTransport()
        {
            var source = new FakeSessionSource(new GameAccountSession(PlayerId, Token));
            var transport = new ScriptedTransport();
            var client = NewClient(source, transport);

            AssertInvalidRequest(() => client.CollectAsync(HiveId, "unknown", 0, "key"));
            AssertInvalidRequest(() => client.CollectAsync(HiveId, "honey_storage", -1, "key"));
            AssertInvalidRequest(() => client.CollectAsync(HiveId, "honey_storage", long.MaxValue, "key"));
            AssertInvalidRequest(() => client.CollectAsync(HiveId, "honey_storage", 0, ""));

            Assert.That(source.Calls, Is.Zero);
            Assert.That(transport.Requests, Is.Empty);
        }

        [Test]
        public void ForeignMalformedOrArithmeticallyInconsistentSnapshotsAreRejected()
        {
            AssertInvalidSnapshot(snapshot => snapshot.PlayerId = Guid.NewGuid());
            AssertInvalidSnapshot(snapshot => snapshot.ContractVersion = "wrong-v1");
            AssertInvalidSnapshot(snapshot => snapshot.CatalogVersion = "Unsafe Catalog");
            AssertInvalidSnapshot(snapshot => snapshot.ServerTimeUtc = snapshot.ServerTimeUtc.ToOffset(TimeSpan.FromHours(-4)));
            AssertInvalidSnapshot(snapshot => snapshot.MaxRecognizedDuration = TimeSpan.FromDays(8));
            AssertInvalidSnapshot(snapshot => snapshot.Lines[0].ResourceKey = "wax");
            AssertInvalidSnapshot(snapshot => snapshot.Lines[0].PendingAmount = snapshot.Lines[0].Capacity + 1m);
            AssertInvalidSnapshot(snapshot => snapshot.Lines[0].CollectableWholeUnits++);
            AssertInvalidSnapshot(snapshot => snapshot.Balances["honey"].Amount = 101);
            AssertInvalidSnapshot(snapshot => snapshot.Lines[1].BuildingKey = "honey_storage");
        }

        [Test]
        public void DetachedOrAlteredCollectionReceiptIsRejected()
        {
            AssertInvalidReceipt(response => response.Receipt.PlayerId = Guid.NewGuid());
            AssertInvalidReceipt(response => response.Receipt.IdempotencyKey = "other");
            AssertInvalidReceipt(response => response.Receipt.BuildingKey = "wax_workshop");
            AssertInvalidReceipt(response => response.Receipt.CreditedAmount = 0);
            AssertInvalidReceipt(response => response.Receipt.RemainingPending = 1.5m);
            AssertInvalidReceipt(response => response.Receipt.ProductionRevision++);
            AssertInvalidReceipt(response => response.Receipt.ResultingBalance.Amount++);
        }

        [Test]
        public async Task UnauthorizedRefreshesOnceAndReplaysTheIdenticalMutation()
        {
            var source = new RefreshableSessionSource(PlayerId, Token, "rotated-token");
            RemoteOfflineProductionCollectResponse response = ValidCollectResponse(3, "stable-after-401");
            var transport = new ScriptedTransport(
                new AuthenticatedGameRestException(
                    AuthenticatedGameRestError.Unauthorized,
                    "game.session_required",
                    401),
                response);
            var client = NewClient(source, transport);

            await client.CollectAsync(HiveId, "honey_storage", 3, "stable-after-401");

            Assert.That(source.RefreshCalls, Is.EqualTo(1));
            Assert.That(transport.Requests.Count, Is.EqualTo(2));
            Assert.That(transport.Requests[0], Is.SameAs(transport.Requests[1]));
            Assert.That(transport.Tokens, Is.EqualTo(new[] { Token, "rotated-token" }));
        }

        [Test]
        public void NetworkFailureNeverRetriesOrCachesCollection()
        {
            var source = new RefreshableSessionSource(PlayerId, Token, "rotated-token");
            var transport = new ScriptedTransport(new AuthenticatedGameRestException(
                AuthenticatedGameRestError.NetworkFailure,
                "game.network_unavailable"));
            var store = new MemoryCacheStore();
            var client = NewClient(source, transport, NewCache(store, ValidSnapshot().ServerTimeUtc));

            HivePerimeterClientException error = Assert.ThrowsAsync<HivePerimeterClientException>(async () =>
                await client.CollectAsync(HiveId, "honey_storage", 3, "no-offline-credit"));

            Assert.That(error.Error, Is.EqualTo(HivePerimeterClientError.TransportFailure));
            Assert.That(transport.Requests.Count, Is.EqualTo(1));
            Assert.That(source.RefreshCalls, Is.Zero);
            Assert.That(store.LoadCalls, Is.Zero);
            Assert.That(store.SaveCalls, Is.Zero);
        }

        [Test]
        public async Task NetworkReadFallsBackToSamePlayerProtectedSnapshotAsReadOnly()
        {
            RemoteOfflineProductionSnapshot snapshot = ValidSnapshot();
            var source = new RefreshableSessionSource(PlayerId, Token, "rotated-token");
            var transport = new ScriptedTransport(
                snapshot,
                new AuthenticatedGameRestException(
                    AuthenticatedGameRestError.NetworkFailure,
                    "game.network_unavailable"));
            var store = new MemoryCacheStore();
            var client = NewClient(source, transport, NewCache(store, snapshot.ServerTimeUtc));

            await client.ReadAsync(HiveId);
            RemoteOfflineProductionSnapshot offline = await client.ReadAsync(HiveId);

            Assert.That(offline.PlayerId, Is.EqualTo(PlayerId));
            Assert.That(client.LastReadSource, Is.EqualTo(GameReadSource.ProtectedCache));
            Assert.That(client.LastReadCachedAtUtc, Is.EqualTo(snapshot.ServerTimeUtc));
            Assert.That(transport.Requests.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task ProtectedCacheNeverCrossesKnownPlayerPartition()
        {
            RemoteOfflineProductionSnapshot snapshot = ValidSnapshot();
            var store = new MemoryCacheStore();
            ProtectedGameReadCache cache = NewCache(store, snapshot.ServerTimeUtc);
            var first = NewClient(
                new RefreshableSessionSource(PlayerId, Token, "rotated-a"),
                new ScriptedTransport(snapshot),
                cache);
            await first.ReadAsync(HiveId);

            Guid other = Guid.Parse("22222222-3333-4444-5555-666666666666");
            var second = NewClient(
                new RefreshableSessionSource(other, "other", "rotated-b"),
                new ScriptedTransport(new AuthenticatedGameRestException(
                    AuthenticatedGameRestError.NetworkFailure,
                    "game.network_unavailable")),
                cache);

            HivePerimeterClientException error = Assert.ThrowsAsync<HivePerimeterClientException>(
                async () => await second.ReadAsync(HiveId));

            Assert.That(error.Error, Is.EqualTo(HivePerimeterClientError.TransportFailure));
            Assert.That(second.LastReadSource, Is.EqualTo(GameReadSource.None));
        }

        [Test]
        public void SystemTextCodecAcceptsExactServerEnvelopeIncludingTimeSpanAndDecimals()
        {
            const string json = "{\"playerId\":\"11111111-2222-3333-4444-555555555555\"," +
                "\"hiveId\":\"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee\"," +
                "\"contractVersion\":\"living-hive-offline-production-v1\",\"catalogVersion\":\"test-v1\"," +
                "\"productionRevision\":4,\"serverTimeUtc\":\"2026-07-22T12:00:00+00:00\"," +
                "\"productionAsOfUtc\":\"2026-07-22T12:00:00+00:00\",\"maxRecognizedDuration\":\"02:00:00\"," +
                "\"lines\":[{\"buildingKey\":\"honey_storage\",\"resourceKey\":\"honey\",\"pendingAmount\":0.5,\"hourlyRate\":10,\"capacity\":1000000000,\"collectableWholeUnits\":0}]," +
                "\"balances\":{\"honey\":{\"amount\":13,\"capacity\":100}}}";

            var codec = new SystemTextGameJsonCodec();
            RemoteOfflineProductionSnapshot value = codec.Deserialize<RemoteOfflineProductionSnapshot>(json);

            Assert.That(value.PlayerId, Is.EqualTo(PlayerId));
            Assert.That(value.MaxRecognizedDuration, Is.EqualTo(TimeSpan.FromHours(2)));
            Assert.That(value.Lines.Single().PendingAmount, Is.EqualTo(.5m));
        }

        [Test]
        public void ProofRowsKeepMobileAndServerResponsibilitiesExplicitWithoutSecrets()
        {
            var client = NewClient(
                new FakeSessionSource(new GameAccountSession(PlayerId, Token)),
                new ScriptedTransport());
            string proof = string.Join("\n", client.ProofRows());

            Assert.That(proof, Does.Contain("production_time_authority:server"));
            Assert.That(proof, Does.Contain("production_cache_read_only:true"));
            Assert.That(proof, Does.Contain("production_local_credit:false"));
            Assert.That(proof, Does.Not.Contain(Token));
        }

        private static HiveOfflineProductionClient NewClient(
            IGameAccountSessionSource source,
            IAuthenticatedGameRestTransport transport,
            ProtectedGameReadCache cache = null)
        {
            var gate = new MobileAccountSessionGate();
            gate.ConfigureTransport(true);
            gate.Apply(AccountSessionReadinessSnapshot.FromServer(true, true, true, true, true));
            return new HiveOfflineProductionClient(gate, source, transport, cache);
        }

        private static ProtectedGameReadCache NewCache(MemoryCacheStore store, DateTimeOffset now)
        {
            return new ProtectedGameReadCache(store, new SystemTextGameJsonCodec(), new FixedClock(now));
        }

        private static RemoteOfflineProductionSnapshot ValidSnapshot()
        {
            DateTimeOffset now = new DateTimeOffset(2026, 7, 22, 12, 0, 0, TimeSpan.Zero);
            return new RemoteOfflineProductionSnapshot
            {
                PlayerId = PlayerId,
                HiveId = HiveId,
                ContractVersion = HiveOfflineProductionClient.ContractVersion,
                CatalogVersion = "test-v1",
                ProductionRevision = 3,
                ServerTimeUtc = now,
                ProductionAsOfUtc = now,
                MaxRecognizedDuration = TimeSpan.FromHours(2),
                Lines = new List<RemoteOfflineProductionLine>
                {
                    new RemoteOfflineProductionLine { BuildingKey = "honey_storage", ResourceKey = "honey", PendingAmount = 20m, HourlyRate = 10m, Capacity = 1000000000, CollectableWholeUnits = 20 },
                    new RemoteOfflineProductionLine { BuildingKey = "wax_workshop", ResourceKey = "wax", PendingAmount = 10m, HourlyRate = 5m, Capacity = 1000000000, CollectableWholeUnits = 10 },
                    new RemoteOfflineProductionLine { BuildingKey = "warehouse_cells", ResourceKey = "pollen", PendingAmount = 16m, HourlyRate = 8m, Capacity = 1000000000, CollectableWholeUnits = 16 }
                },
                Balances = new Dictionary<string, RemoteOfflineProductionBalance>
                {
                    ["honey"] = new RemoteOfflineProductionBalance { Amount = 11, Capacity = 100 },
                    ["wax"] = new RemoteOfflineProductionBalance { Amount = 12, Capacity = 100 },
                    ["pollen"] = new RemoteOfflineProductionBalance { Amount = 13, Capacity = 100 }
                }
            };
        }

        private static RemoteOfflineProductionCollectResponse ValidCollectResponse(long expectedRevision, string key)
        {
            RemoteOfflineProductionSnapshot snapshot = ValidSnapshot();
            snapshot.ProductionRevision = expectedRevision + 1;
            snapshot.Lines[0].PendingAmount = .5m;
            snapshot.Lines[0].CollectableWholeUnits = 0;
            snapshot.Balances["honey"].Amount = 13;
            return new RemoteOfflineProductionCollectResponse
            {
                Receipt = new RemoteOfflineProductionReceipt
                {
                    PlayerId = PlayerId,
                    HiveId = HiveId,
                    IdempotencyKey = key,
                    BuildingKey = "honey_storage",
                    ResourceKey = "honey",
                    CreditedAmount = 2,
                    RemainingPending = .5m,
                    ProductionRevision = expectedRevision + 1,
                    ServerTimeUtc = snapshot.ServerTimeUtc,
                    ResultingBalance = new RemoteOfflineProductionBalance { Amount = 13, Capacity = 100 }
                },
                Snapshot = snapshot
            };
        }

        private static void AssertInvalidSnapshot(Action<RemoteOfflineProductionSnapshot> mutate)
        {
            RemoteOfflineProductionSnapshot snapshot = ValidSnapshot();
            mutate(snapshot);
            var client = NewClient(
                new FakeSessionSource(new GameAccountSession(PlayerId, Token)),
                new ScriptedTransport(snapshot));
            HivePerimeterClientException error = Assert.ThrowsAsync<HivePerimeterClientException>(
                async () => await client.ReadAsync(HiveId));
            Assert.That(error.Error, Is.EqualTo(HivePerimeterClientError.InvalidResponse));
        }

        private static void AssertInvalidReceipt(Action<RemoteOfflineProductionCollectResponse> mutate)
        {
            RemoteOfflineProductionCollectResponse response = ValidCollectResponse(3, "collect-stable");
            mutate(response);
            var client = NewClient(
                new FakeSessionSource(new GameAccountSession(PlayerId, Token)),
                new ScriptedTransport(response));
            HivePerimeterClientException error = Assert.ThrowsAsync<HivePerimeterClientException>(async () =>
                await client.CollectAsync(HiveId, "honey_storage", 3, "collect-stable"));
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
            public bool TryGetSession(out GameAccountSession value)
            {
                Calls++;
                value = session;
                return value != null;
            }
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
            public int InvalidateCalls { get; private set; }
            public bool TryGetSession(out GameAccountSession value) { value = session; return value != null; }
            public bool TryGetKnownPlayerId(out Guid value) { value = playerId; return value != Guid.Empty; }
            public Task<GameAccountSession> GetFreshSessionAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(session);
            }
            public Task<GameAccountSession> RefreshAfterUnauthorizedAsync(string rejectedAccessToken, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RefreshCalls++;
                session = new GameAccountSession(playerId, replacementToken);
                return Task.FromResult(session);
            }
            public Task InvalidateUnauthorizedSessionAsync(string rejectedAccessToken, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                InvalidateCalls++;
                session = null;
                return Task.CompletedTask;
            }
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
                Exception failure = step as Exception;
                if (failure != null) throw failure;
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
            {
                cancellationToken.ThrowIfCancellationRequested();
                LoadCalls++;
                return Task.FromResult(Value);
            }
            public Task SaveAsync(string value, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                SaveCalls++;
                Value = value;
                return Task.CompletedTask;
            }
            public Task DeleteAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Value = null;
                return Task.CompletedTask;
            }
        }

        private sealed class FixedClock : IMobileAccountSessionClock
        {
            public FixedClock(DateTimeOffset utcNow) { UtcNow = utcNow; }
            public DateTimeOffset UtcNow { get; }
        }
    }
}
