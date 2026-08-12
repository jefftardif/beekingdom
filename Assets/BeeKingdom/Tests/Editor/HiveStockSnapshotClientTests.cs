using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class HiveStockSnapshotClientTests
    {
        private static readonly Guid PlayerId =
            Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly Guid HiveId =
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly Guid OperationId =
            Guid.Parse("99999999-8888-7777-6666-555555555555");
        private const string Token = "stock-test-token";

        [Test]
        public void ClosedOfficialGateStopsBeforeCredentialsAndTransport()
        {
            var source = new FakeSessionSource(new GameAccountSession(PlayerId, Token));
            var transport = new ScriptedTransport(ValidSnapshot());
            var client = new HiveStockSnapshotClient(
                new MobileAccountSessionGate(), source, transport);

            HivePerimeterClientException error =
                Assert.ThrowsAsync<HivePerimeterClientException>(
                    async () => await client.ReadAsync(HiveId));

            Assert.That(error.Error, Is.EqualTo(HivePerimeterClientError.NotConfigured));
            Assert.That(source.Calls, Is.Zero);
            Assert.That(transport.Requests, Is.Empty);
        }

        [Test]
        public async Task ValidatedReadUsesExactGetAndProtectedCache()
        {
            RemoteHiveStockSnapshot snapshot = ValidSnapshot();
            var store = new MemoryCacheStore();
            var transport = new ScriptedTransport(snapshot);
            var client = NewClient(
                new FakeSessionSource(new GameAccountSession(PlayerId, Token)),
                transport,
                NewCache(store, snapshot.ServerTimeUtc));

            RemoteHiveStockSnapshot result = await client.ReadAsync(HiveId);

            Assert.That(result, Is.SameAs(snapshot));
            AuthenticatedGameRestRequest request = transport.Requests.Single();
            Assert.That(request.Method, Is.EqualTo("GET"));
            Assert.That(
                request.Path,
                Is.EqualTo(
                    "/game/v1/hives/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/hive-stock"));
            Assert.That(request.Body, Is.Null);
            Assert.That(store.SaveCalls, Is.EqualTo(1));
            Assert.That(client.LastReadSource, Is.EqualTo(GameReadSource.Server));
        }

        [Test]
        public void EmptyHiveNeverReachesSessionOrTransport()
        {
            var source = new FakeSessionSource(new GameAccountSession(PlayerId, Token));
            var transport = new ScriptedTransport();
            var client = NewClient(source, transport);

            HivePerimeterClientException error =
                Assert.ThrowsAsync<HivePerimeterClientException>(
                    async () => await client.ReadAsync(Guid.Empty));

            Assert.That(error.Error, Is.EqualTo(HivePerimeterClientError.InvalidRequest));
            Assert.That(source.Calls, Is.Zero);
            Assert.That(transport.Requests, Is.Empty);
        }

        [Test]
        public void ForeignMalformedOrUnboundedSnapshotsAreRejected()
        {
            AssertInvalid(snapshot => snapshot.PlayerId = Guid.NewGuid());
            AssertInvalid(snapshot => snapshot.ContractVersion = "wrong-v1");
            AssertInvalid(snapshot => snapshot.CatalogVersion = "Unsafe Catalog");
            AssertInvalid(snapshot =>
                snapshot.ServerTimeUtc =
                    snapshot.ServerTimeUtc.ToOffset(TimeSpan.FromHours(-4)));
            AssertInvalid(snapshot =>
                snapshot.Honey.Amount = snapshot.Honey.Capacity + 1);
            AssertInvalid(snapshot => snapshot.Wax = null);
            AssertInvalid(snapshot => snapshot.Population = 1);
            AssertInvalid(snapshot =>
            {
                snapshot.Population = 11;
                snapshot.PopulationCapacity = 10;
            });
            AssertInvalid(snapshot =>
                snapshot.CompletedResearchIds.Add(
                    snapshot.CompletedResearchIds.Single()));
            AssertInvalid(snapshot =>
                snapshot.ActiveEngagements.Add(
                    new RemoteHiveStockEngagement
                    {
                        OperationId = OperationId,
                        Kind = "Research",
                        Key = "tempered_combs_i",
                        StartedAtUtc = snapshot.ServerTimeUtc.AddMinutes(-2),
                        EndsAtUtc = snapshot.ServerTimeUtc.AddMinutes(2)
                    }));
            AssertInvalid(snapshot =>
                snapshot.ActiveEngagements[0].Kind = "Unknown");
            AssertInvalid(snapshot =>
                snapshot.ActiveEngagements[0].Key = "Unsafe Key");
            AssertInvalid(snapshot =>
                snapshot.ActiveEngagements[0].StartedAtUtc =
                    snapshot.ServerTimeUtc.AddMinutes(1));
            AssertInvalid(snapshot =>
                snapshot.ActiveEngagements[0].EndsAtUtc =
                    snapshot.ActiveEngagements[0].StartedAtUtc);
            AssertInvalid(snapshot =>
                snapshot.ActiveEngagements[0].EndsAtUtc =
                    snapshot.ActiveEngagements[0].StartedAtUtc.AddDays(31));
            AssertInvalid(snapshot =>
            {
                snapshot.ActiveEngagements.Clear();
                for (int index = 0; index < 65; index++)
                    snapshot.ActiveEngagements.Add(
                        new RemoteHiveStockEngagement
                        {
                            OperationId = Guid.NewGuid(),
                            Kind = "Production",
                            Key = "honey_storage",
                            StartedAtUtc = snapshot.ServerTimeUtc.AddMinutes(-1),
                            EndsAtUtc = snapshot.ServerTimeUtc.AddMinutes(1)
                        });
            });
        }

        [Test]
        public async Task PopulationPairIsAcceptedOnlyWhenAuthoritative()
        {
            RemoteHiveStockSnapshot snapshot = ValidSnapshot();
            snapshot.Population = 250;
            snapshot.PopulationCapacity = 500;
            var client = NewClient(
                new FakeSessionSource(new GameAccountSession(PlayerId, Token)),
                new ScriptedTransport(snapshot));

            RemoteHiveStockSnapshot result = await client.ReadAsync(HiveId);

            Assert.That(result.Population, Is.EqualTo(250));
            Assert.That(result.PopulationCapacity, Is.EqualTo(500));
        }

        [Test]
        public async Task UnauthorizedRefreshesOnceAndReplaysIdenticalGet()
        {
            var source = new RefreshableSessionSource(
                PlayerId, Token, "rotated-stock-token");
            var transport = new ScriptedTransport(
                new AuthenticatedGameRestException(
                    AuthenticatedGameRestError.Unauthorized,
                    "game.session_required",
                    401),
                ValidSnapshot());
            var client = NewClient(source, transport);

            await client.ReadAsync(HiveId);

            Assert.That(source.RefreshCalls, Is.EqualTo(1));
            Assert.That(transport.Requests.Count, Is.EqualTo(2));
            Assert.That(transport.Requests[0], Is.SameAs(transport.Requests[1]));
            Assert.That(
                transport.Tokens,
                Is.EqualTo(new[] { Token, "rotated-stock-token" }));
        }

        [Test]
        public async Task NetworkReadFallsBackToSamePlayerProtectedReadOnlySnapshot()
        {
            RemoteHiveStockSnapshot snapshot = ValidSnapshot();
            var store = new MemoryCacheStore();
            var source = new RefreshableSessionSource(
                PlayerId, Token, "rotated-stock-token");
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
            RemoteHiveStockSnapshot offline = await client.ReadAsync(HiveId);

            Assert.That(offline.PlayerId, Is.EqualTo(PlayerId));
            Assert.That(client.LastReadSource, Is.EqualTo(GameReadSource.ProtectedCache));
            Assert.That(client.LastReadCachedAtUtc, Is.EqualTo(snapshot.ServerTimeUtc));
            Assert.That(store.LoadCalls, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void NetworkReadNeverUsesCacheForAnotherKnownPlayer()
        {
            RemoteHiveStockSnapshot snapshot = ValidSnapshot();
            var store = new MemoryCacheStore();
            var first = NewClient(
                new RefreshableSessionSource(PlayerId, Token, "rotated"),
                new ScriptedTransport(snapshot),
                NewCache(store, snapshot.ServerTimeUtc));
            Assert.DoesNotThrowAsync(async () => await first.ReadAsync(HiveId));

            Guid otherPlayer = Guid.Parse(
                "22222222-3333-4444-5555-666666666666");
            var second = NewClient(
                new RefreshableSessionSource(otherPlayer, "other", "other-2"),
                new ScriptedTransport(
                    new AuthenticatedGameRestException(
                        AuthenticatedGameRestError.NetworkFailure,
                        "game.network_unavailable")),
                NewCache(store, snapshot.ServerTimeUtc));

            HivePerimeterClientException error =
                Assert.ThrowsAsync<HivePerimeterClientException>(
                    async () => await second.ReadAsync(HiveId));

            Assert.That(error.Error, Is.EqualTo(HivePerimeterClientError.TransportFailure));
            Assert.That(second.LastReadSource, Is.Not.EqualTo(GameReadSource.ProtectedCache));
        }

        [Test]
        public void SystemTextCodecAcceptsExactServerEnvelope()
        {
            const string json =
                "{\"playerId\":\"11111111-2222-3333-4444-555555555555\"," +
                "\"hiveId\":\"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee\"," +
                "\"contractVersion\":\"living-hive-stock-v1\"," +
                "\"catalogVersion\":\"test-v1\",\"revision\":7," +
                "\"serverTimeUtc\":\"2026-07-22T12:00:00+00:00\"," +
                "\"honey\":{\"amount\":500,\"capacity\":1000}," +
                "\"wax\":{\"amount\":300,\"capacity\":800}," +
                "\"pollen\":{\"amount\":400,\"capacity\":900}," +
                "\"population\":null,\"populationCapacity\":null," +
                "\"completedResearchIds\":[\"foraging_routes_i\"]," +
                "\"activeEngagements\":[{\"operationId\":" +
                "\"99999999-8888-7777-6666-555555555555\"," +
                "\"kind\":\"Research\",\"key\":\"tempered_combs_i\"," +
                "\"startedAtUtc\":\"2026-07-22T11:59:00+00:00\"," +
                "\"endsAtUtc\":\"2026-07-22T12:04:00+00:00\"}]}";

            RemoteHiveStockSnapshot value =
                new SystemTextGameJsonCodec()
                    .Deserialize<RemoteHiveStockSnapshot>(json);

            Assert.That(value.PlayerId, Is.EqualTo(PlayerId));
            Assert.That(value.Honey.Amount, Is.EqualTo(500));
            Assert.That(value.ActiveEngagements.Single().Kind, Is.EqualTo("Research"));
        }

        [Test]
        public void ProofRowsKeepDeviceAndServerResponsibilitiesExplicit()
        {
            var client = NewClient(
                new FakeSessionSource(new GameAccountSession(PlayerId, Token)),
                new ScriptedTransport());
            string proof = string.Join("\n", client.ProofRows());

            Assert.That(proof, Does.Contain("hive_stock_resource_authority:server"));
            Assert.That(proof, Does.Contain("hive_stock_cache_read_only:true"));
            Assert.That(proof, Does.Contain("hive_stock_local_resource_fallback:false"));
            Assert.That(proof, Does.Contain("hive_stock_mutation:false"));
            Assert.That(proof, Does.Not.Contain(Token));
        }

        private static HiveStockSnapshotClient NewClient(
            IGameAccountSessionSource source,
            IAuthenticatedGameRestTransport transport,
            ProtectedGameReadCache cache = null)
        {
            var gate = new MobileAccountSessionGate();
            gate.ConfigureTransport(true);
            gate.Apply(
                AccountSessionReadinessSnapshot.FromServer(
                    true, true, true, true, true));
            return new HiveStockSnapshotClient(gate, source, transport, cache);
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

        private static RemoteHiveStockSnapshot ValidSnapshot()
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

        private static void AssertInvalid(Action<RemoteHiveStockSnapshot> mutate)
        {
            RemoteHiveStockSnapshot snapshot = ValidSnapshot();
            mutate(snapshot);
            var client = NewClient(
                new FakeSessionSource(new GameAccountSession(PlayerId, Token)),
                new ScriptedTransport(snapshot));
            HivePerimeterClientException error =
                Assert.ThrowsAsync<HivePerimeterClientException>(
                    async () => await client.ReadAsync(HiveId));
            Assert.That(error.Error, Is.EqualTo(HivePerimeterClientError.InvalidResponse));
        }

        private sealed class FakeSessionSource : IGameAccountSessionSource
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
            public Task<GameAccountSession> RefreshAfterUnauthorizedAsync(
                string rejectedAccessToken,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RefreshCalls++;
                session = new GameAccountSession(playerId, replacementToken);
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

        private sealed class ScriptedTransport : IAuthenticatedGameRestTransport
        {
            private readonly Queue<object> steps;
            public ScriptedTransport(params object[] steps)
            {
                this.steps =
                    new Queue<object>(steps ?? Array.Empty<object>());
            }
            public List<AuthenticatedGameRestRequest> Requests { get; } =
                new List<AuthenticatedGameRestRequest>();
            public List<string> Tokens { get; } = new List<string>();

            public Task<T> SendAsync<T>(
                AuthenticatedGameRestRequest request,
                string bearerAccessToken,
                CancellationToken cancellationToken)
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
            public Task DeleteAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Value = null;
                return Task.CompletedTask;
            }
        }

        private sealed class FixedClock : IMobileAccountSessionClock
        {
            public FixedClock(DateTimeOffset utcNow)
            {
                UtcNow = utcNow;
            }
            public DateTimeOffset UtcNow { get; }
        }
    }
}
