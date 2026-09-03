using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class HiveBuildingUpgradeClientTests
    {
        private static readonly Guid PlayerId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly Guid HiveId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly Guid OperationId = Guid.Parse("99999999-8888-7777-6666-555555555555");
        private const string Token = "upgrade-test-token";

        [Test]
        public void ClosedOfficialGateStopsBeforeCredentialsAndTransport()
        {
            var source = new FakeSessionSource(new GameAccountSession(PlayerId, Token));
            var transport = new ScriptedTransport(ValidSnapshot());
            var client = new HiveBuildingUpgradeClient(new MobileAccountSessionGate(), source, transport);

            HivePerimeterClientException error = Assert.ThrowsAsync<HivePerimeterClientException>(
                async () => await client.ReadAsync(HiveId));

            Assert.That(error.Error, Is.EqualTo(HivePerimeterClientError.NotConfigured));
            Assert.That(source.Calls, Is.Zero);
            Assert.That(transport.Requests, Is.Empty);
        }

        [Test]
        public async Task ValidatedReadUsesExactRouteAndProtectedGetCache()
        {
            RemoteBuildingUpgradeSnapshot snapshot = ValidSnapshot();
            var store = new MemoryCacheStore();
            var transport = new ScriptedTransport(snapshot);
            var client = NewClient(new FakeSessionSource(new GameAccountSession(PlayerId, Token)), transport,
                NewCache(store, snapshot.ServerTimeUtc));

            RemoteBuildingUpgradeSnapshot result = await client.ReadAsync(HiveId);

            Assert.That(result, Is.SameAs(snapshot));
            AuthenticatedGameRestRequest request = transport.Requests.Single();
            Assert.That(request.Method, Is.EqualTo("GET"));
            Assert.That(request.Path, Is.EqualTo("/game/v1/hives/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/building-upgrades"));
            Assert.That(request.Body, Is.Null);
            Assert.That(store.SaveCalls, Is.EqualTo(1));
            Assert.That(client.LastReadSource, Is.EqualTo(GameReadSource.Server));
        }

        [Test]
        public async Task StartSendsOnlyRevisionAndStableKeyThenAcceptsBoundReceipt()
        {
            RemoteBuildingUpgradeMutationResponse response = ValidStartResponse(3, "start-stable");
            var transport = new ScriptedTransport(response);
            var store = new MemoryCacheStore();
            var client = NewClient(new FakeSessionSource(new GameAccountSession(PlayerId, Token)), transport,
                NewCache(store, response.Snapshot.ServerTimeUtc));

            RemoteBuildingUpgradeMutationResponse result = await client.StartAsync(
                HiveId, "wax_workshop", 3, "start-stable");

            Assert.That(result, Is.SameAs(response));
            AuthenticatedGameRestRequest request = transport.Requests.Single();
            Assert.That(request.Method, Is.EqualTo("POST"));
            Assert.That(request.Path, Does.EndWith("/building-upgrades/wax_workshop/start"));
            var body = request.Body as BuildingUpgradeMutationRequest;
            Assert.That(body, Is.Not.Null);
            Assert.That(body.ExpectedRevision, Is.EqualTo(3));
            Assert.That(body.IdempotencyKey, Is.EqualTo("start-stable"));
            Assert.That(store.SaveCalls, Is.EqualTo(1), "A validated server mutation should refresh the protected read-only cache.");
            Assert.That(request.Body.GetType().GetProperties().Select(property => property.Name),
                Is.EquivalentTo(new[] { "ExpectedRevision", "IdempotencyKey" }));
        }

        [Test]
        public async Task CompleteSendsOperationRevisionAndStableKeyWithoutLocalResult()
        {
            RemoteBuildingUpgradeMutationResponse response = ValidCompleteResponse(4, "complete-stable");
            var transport = new ScriptedTransport(response);
            var client = NewClient(new FakeSessionSource(new GameAccountSession(PlayerId, Token)), transport);

            await client.CompleteAsync(HiveId, OperationId, 4, "complete-stable");

            AuthenticatedGameRestRequest request = transport.Requests.Single();
            Assert.That(request.Path, Does.EndWith("/building-upgrades/99999999-8888-7777-6666-555555555555/complete"));
            var body = request.Body as BuildingUpgradeMutationRequest;
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
            AssertInvalidRequest(() => client.StartAsync(HiveId, "wax_workshop", -1, "key"));
            AssertInvalidRequest(() => client.StartAsync(HiveId, "wax_workshop", 0, " key"));
            AssertInvalidRequest(() => client.CompleteAsync(HiveId, Guid.Empty, 0, "key"));
            AssertInvalidRequest(() => client.CompleteAsync(HiveId, OperationId, long.MaxValue, "key"));

            Assert.That(source.Calls, Is.Zero);
            Assert.That(transport.Requests, Is.Empty);
        }

        [Test]
        public async Task FreshAccountSnapshotWithGuardPostOfferAtLevelOneIsAccepted()
        {
            // M039-CL: reproduit exactement le compte neuf - guard_post est materialise au
            // niveau 1 des la creation du hive (voir CreateInitialHiveState cote serveur), donc
            // toute lecture reelle inclut une offre pour guard_post. Avant le correctif, cette
            // offre etait absente de SupportedBuildings et faisait rejeter TOUT le snapshot.
            RemoteBuildingUpgradeSnapshot snapshot = ValidSnapshot();
            snapshot.BuildingLevels["guard_post"] = 1;
            snapshot.Offers.Add(new RemoteBuildingUpgradeOffer
            {
                BuildingKey = "guard_post", FromLevel = 1, ToLevel = 2,
                Duration = TimeSpan.FromMinutes(3),
                Costs = new Dictionary<string, long> { ["honey"] = 972, ["wax"] = 251 }
            });
            snapshot.Balances["wax"] = new RemoteBuildingUpgradeBalance { Amount = 500, Capacity = 1000 };
            var client = NewClient(new FakeSessionSource(new GameAccountSession(PlayerId, Token)), new ScriptedTransport(snapshot));

            RemoteBuildingUpgradeSnapshot result = await client.ReadAsync(HiveId);

            Assert.That(result, Is.SameAs(snapshot));
        }

        [Test]
        public async Task EveryCatalogBuildingOfferIsAcceptedByClientValidation()
        {
            // M039-CL: garde-fou anti-regression - la liste ci-dessous doit rester le miroir
            // exact du catalogue serveur (appsettings.*.json, section BuildingUpgrades.Catalog).
            string[] catalogBuildings =
            {
                "honey_storage", "wax_workshop", "warehouse_cells", "nursery_cluster",
                "guard_post", "defense_growth", "genetics_garden", "research_node",
                "infirmary_grove", "academy_canopy", "hive_bank", "administration_core",
                "alliance_future_hall", "archives_honeyfall"
            };
            foreach (string buildingKey in catalogBuildings)
            {
                RemoteBuildingUpgradeSnapshot snapshot = ValidSnapshot();
                snapshot.BuildingLevels.Clear();
                snapshot.BuildingLevels[buildingKey] = 1;
                snapshot.Offers.Clear();
                snapshot.Offers.Add(new RemoteBuildingUpgradeOffer
                {
                    BuildingKey = buildingKey, FromLevel = 1, ToLevel = 2,
                    Duration = TimeSpan.FromMinutes(1),
                    Costs = new Dictionary<string, long> { ["honey"] = 10 }
                });
                var client = NewClient(new FakeSessionSource(new GameAccountSession(PlayerId, Token)), new ScriptedTransport(snapshot));

                RemoteBuildingUpgradeSnapshot result = await client.ReadAsync(HiveId);

                Assert.That(result, Is.SameAs(snapshot), "Building '" + buildingKey + "' must be accepted by client validation.");
            }
        }

        [Test]
        public void ForeignMalformedOrInconsistentSnapshotsAreRejected()
        {
            AssertInvalidSnapshot(snapshot => snapshot.PlayerId = Guid.NewGuid());
            AssertInvalidSnapshot(snapshot => snapshot.ContractVersion = "wrong-v1");
            AssertInvalidSnapshot(snapshot => snapshot.CatalogVersion = "Unsafe Catalog");
            AssertInvalidSnapshot(snapshot => snapshot.ServerTimeUtc = snapshot.ServerTimeUtc.ToOffset(TimeSpan.FromHours(-4)));
            AssertInvalidSnapshot(snapshot => snapshot.Balances["pollen"].Amount =
                snapshot.Balances["pollen"].Capacity + 1);
            AssertInvalidSnapshot(snapshot => snapshot.BuildingLevels["wax_workshop"] = 0);
            AssertInvalidSnapshot(snapshot => snapshot.Offers[0].ToLevel = 4);
            AssertInvalidSnapshot(snapshot => snapshot.Offers[0].Costs["pollen"] = 0);
            AssertInvalidSnapshot(snapshot => snapshot.Offers.Add(snapshot.Offers[0]));
            AssertInvalidSnapshot(snapshot => snapshot.ActiveOperation = ActiveOperation(snapshot.ServerTimeUtc, "running", snapshot.ServerTimeUtc));
        }

        [Test]
        public void DetachedOrAlteredMutationReceiptsAreRejected()
        {
            AssertInvalidStart(response => response.Receipt.PlayerId = Guid.NewGuid());
            AssertInvalidStart(response => response.Receipt.IdempotencyKey = "other");
            AssertInvalidStart(response => response.Receipt.Revision++);
            AssertInvalidStart(response => response.Receipt.Code = HiveBuildingUpgradeClient.CompletedCode);
            AssertInvalidStart(response => response.Snapshot.ActiveOperation.OperationId = Guid.NewGuid());
            AssertInvalidComplete(response => response.Snapshot.BuildingLevels["wax_workshop"] = 1);
            AssertInvalidComplete(response => response.Snapshot.ActiveOperation = ActiveOperation(response.Snapshot.ServerTimeUtc));
        }

        [Test]
        public async Task OriginalReceiptRemainsValidWhenReplayReturnsANewerAuthoritativeSnapshot()
        {
            RemoteBuildingUpgradeMutationResponse startReplay = ValidCompleteResponse(4, "start-replay");
            startReplay.Receipt = Receipt("start-replay", 4, HiveBuildingUpgradeClient.StartedCode,
                startReplay.Snapshot.ServerTimeUtc.AddMinutes(-1));
            startReplay.Snapshot.Revision = 5;
            var startClient = NewClient(new FakeSessionSource(new GameAccountSession(PlayerId, Token)),
                new ScriptedTransport(startReplay));
            await startClient.StartAsync(HiveId, "wax_workshop", 3, "start-replay");

            RemoteBuildingUpgradeMutationResponse completeReplay = ValidCompleteResponse(4, "complete-replay");
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

            await client.StartAsync(HiveId, "wax_workshop", 3, "stable-after-401");

            Assert.That(source.RefreshCalls, Is.EqualTo(1));
            Assert.That(transport.Requests.Count, Is.EqualTo(2));
            Assert.That(transport.Requests[0], Is.SameAs(transport.Requests[1]));
            Assert.That(transport.Tokens, Is.EqualTo(new[] { Token, "rotated-token" }));
        }

        [Test]
        public void NetworkFailureNeverRetriesOrCachesMutation()
        {
            var source = new RefreshableSessionSource(PlayerId, Token, "rotated-token");
            var transport = new ScriptedTransport(new AuthenticatedGameRestException(
                AuthenticatedGameRestError.NetworkFailure, "game.network_unavailable"));
            var store = new MemoryCacheStore();
            var client = NewClient(source, transport, NewCache(store, ValidSnapshot().ServerTimeUtc));

            HivePerimeterClientException error = Assert.ThrowsAsync<HivePerimeterClientException>(async () =>
                await client.StartAsync(HiveId, "wax_workshop", 3, "no-offline-start"));

            Assert.That(error.Error, Is.EqualTo(HivePerimeterClientError.TransportFailure));
            Assert.That(transport.Requests.Count, Is.EqualTo(1));
            Assert.That(source.RefreshCalls, Is.Zero);
            Assert.That(store.LoadCalls, Is.Zero);
            Assert.That(store.SaveCalls, Is.Zero);
        }

        [Test]
        public async Task NetworkReadFallsBackToSamePlayerProtectedSnapshotAsReadOnly()
        {
            RemoteBuildingUpgradeSnapshot snapshot = ValidSnapshot();
            var store = new MemoryCacheStore();
            var source = new RefreshableSessionSource(PlayerId, Token, "rotated-token");
            var transport = new ScriptedTransport(snapshot,
                new AuthenticatedGameRestException(AuthenticatedGameRestError.NetworkFailure, "game.network_unavailable"));
            var client = NewClient(source, transport, NewCache(store, snapshot.ServerTimeUtc));

            await client.ReadAsync(HiveId);
            RemoteBuildingUpgradeSnapshot offline = await client.ReadAsync(HiveId);

            Assert.That(offline.PlayerId, Is.EqualTo(PlayerId));
            Assert.That(client.LastReadSource, Is.EqualTo(GameReadSource.ProtectedCache));
            Assert.That(client.LastReadCachedAtUtc, Is.EqualTo(snapshot.ServerTimeUtc));
        }

        [Test]
        public void SystemTextCodecAcceptsExactServerEnvelope()
        {
            const string json = "{\"playerId\":\"11111111-2222-3333-4444-555555555555\"," +
                "\"hiveId\":\"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee\"," +
                "\"contractVersion\":\"living-hive-building-upgrade-v1\",\"catalogVersion\":\"test-v1\"," +
                "\"revision\":3,\"serverTimeUtc\":\"2026-07-22T12:00:00+00:00\"," +
                "\"balances\":{\"honey\":{\"amount\":100,\"capacity\":1000},\"pollen\":{\"amount\":100,\"capacity\":1000}}," +
                "\"buildingLevels\":{\"wax_workshop\":1}," +
                "\"offers\":[{\"buildingKey\":\"wax_workshop\",\"fromLevel\":1,\"toLevel\":2,\"duration\":\"00:10:00\",\"costs\":{\"honey\":10,\"pollen\":20}}]," +
                "\"activeOperation\":null}";

            RemoteBuildingUpgradeSnapshot value = new SystemTextGameJsonCodec().Deserialize<RemoteBuildingUpgradeSnapshot>(json);

            Assert.That(value.PlayerId, Is.EqualTo(PlayerId));
            Assert.That(value.Offers.Single().Duration, Is.EqualTo(TimeSpan.FromMinutes(10)));
            Assert.That(value.Offers.Single().Costs["pollen"], Is.EqualTo(20));
        }

        [Test]
        public void ProofRowsKeepMobileAndServerResponsibilitiesExplicitWithoutSecrets()
        {
            var client = NewClient(new FakeSessionSource(new GameAccountSession(PlayerId, Token)), new ScriptedTransport());
            string proof = string.Join("\n", client.ProofRows());

            Assert.That(proof, Does.Contain("building_upgrade_cost_authority:server"));
            Assert.That(proof, Does.Contain("building_upgrade_cache_read_only:true"));
            Assert.That(proof, Does.Contain("building_upgrade_local_completion:false"));
            Assert.That(proof, Does.Not.Contain(Token));
        }

        private static HiveBuildingUpgradeClient NewClient(
            IGameAccountSessionSource source,
            IAuthenticatedGameRestTransport transport,
            ProtectedGameReadCache cache = null)
        {
            var gate = new MobileAccountSessionGate();
            gate.ConfigureTransport(true);
            gate.Apply(AccountSessionReadinessSnapshot.FromServer(true, true, true, true, true));
            return new HiveBuildingUpgradeClient(gate, source, transport, cache);
        }

        private static ProtectedGameReadCache NewCache(MemoryCacheStore store, DateTimeOffset now)
        {
            return new ProtectedGameReadCache(store, new SystemTextGameJsonCodec(), new FixedClock(now));
        }

        private static RemoteBuildingUpgradeSnapshot ValidSnapshot()
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
                },
                ActiveOperation = null
            };
        }

        private static RemoteBuildingUpgradeOperation ActiveOperation(
            DateTimeOffset now,
            string status = HiveBuildingUpgradeClient.RunningStatus,
            DateTimeOffset? completes = null)
        {
            return new RemoteBuildingUpgradeOperation
            {
                OperationId = OperationId,
                BuildingKey = "wax_workshop",
                FromLevel = 1,
                ToLevel = 2,
                StartedAtUtc = now,
                CompletesAtUtc = completes ?? now.AddMinutes(10),
                Status = status
            };
        }

        private static RemoteBuildingUpgradeMutationResponse ValidStartResponse(long expectedRevision, string key)
        {
            RemoteBuildingUpgradeSnapshot snapshot = ValidSnapshot();
            snapshot.Revision = expectedRevision + 1;
            snapshot.ActiveOperation = ActiveOperation(snapshot.ServerTimeUtc);
            return new RemoteBuildingUpgradeMutationResponse
            {
                Receipt = Receipt(key, snapshot.Revision, HiveBuildingUpgradeClient.StartedCode, snapshot.ServerTimeUtc),
                Snapshot = snapshot
            };
        }

        private static RemoteBuildingUpgradeMutationResponse ValidCompleteResponse(long expectedRevision, string key)
        {
            RemoteBuildingUpgradeSnapshot snapshot = ValidSnapshot();
            snapshot.Revision = expectedRevision + 1;
            snapshot.BuildingLevels["wax_workshop"] = 2;
            snapshot.Offers.Clear();
            return new RemoteBuildingUpgradeMutationResponse
            {
                Receipt = Receipt(key, snapshot.Revision, HiveBuildingUpgradeClient.CompletedCode, snapshot.ServerTimeUtc),
                Snapshot = snapshot
            };
        }

        private static RemoteBuildingUpgradeReceipt Receipt(string key, long revision, string code, DateTimeOffset acceptedAt)
        {
            return new RemoteBuildingUpgradeReceipt
            {
                PlayerId = PlayerId, HiveId = HiveId, IdempotencyKey = key, OperationId = OperationId,
                BuildingKey = "wax_workshop", FromLevel = 1, ToLevel = 2, Revision = revision,
                AcceptedAtUtc = acceptedAt, Code = code
            };
        }

        private static void AssertInvalidSnapshot(Action<RemoteBuildingUpgradeSnapshot> mutate)
        {
            RemoteBuildingUpgradeSnapshot snapshot = ValidSnapshot();
            mutate(snapshot);
            var client = NewClient(new FakeSessionSource(new GameAccountSession(PlayerId, Token)), new ScriptedTransport(snapshot));
            HivePerimeterClientException error = Assert.ThrowsAsync<HivePerimeterClientException>(async () => await client.ReadAsync(HiveId));
            Assert.That(error.Error, Is.EqualTo(HivePerimeterClientError.InvalidResponse));
        }

        private static void AssertInvalidStart(Action<RemoteBuildingUpgradeMutationResponse> mutate)
        {
            RemoteBuildingUpgradeMutationResponse response = ValidStartResponse(3, "key");
            mutate(response);
            var client = NewClient(new FakeSessionSource(new GameAccountSession(PlayerId, Token)), new ScriptedTransport(response));
            HivePerimeterClientException error = Assert.ThrowsAsync<HivePerimeterClientException>(
                async () => await client.StartAsync(HiveId, "wax_workshop", 3, "key"));
            Assert.That(error.Error, Is.EqualTo(HivePerimeterClientError.InvalidResponse));
        }

        private static void AssertInvalidComplete(Action<RemoteBuildingUpgradeMutationResponse> mutate)
        {
            RemoteBuildingUpgradeMutationResponse response = ValidCompleteResponse(4, "key");
            mutate(response);
            var client = NewClient(new FakeSessionSource(new GameAccountSession(PlayerId, Token)), new ScriptedTransport(response));
            HivePerimeterClientException error = Assert.ThrowsAsync<HivePerimeterClientException>(
                async () => await client.CompleteAsync(HiveId, OperationId, 4, "key"));
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
                Requests.Add(request); Tokens.Add(bearerAccessToken);
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
