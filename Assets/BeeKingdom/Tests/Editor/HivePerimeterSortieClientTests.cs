using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class HivePerimeterSortieClientTests
    {
        private static readonly Guid PlayerId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly Guid HiveId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private const string Token = "sensitive-test-token";

        [Test]
        public void AccountGateBlocksBeforeSessionAndTransport()
        {
            var source = new FakeSessionSource(new GameAccountSession(PlayerId, Token));
            var transport = new FakeTransport { Response = ValidBoard() };
            var client = new HivePerimeterSortieClient(new MobileAccountSessionGate(), source, transport);

            HivePerimeterClientException error = Assert.ThrowsAsync<HivePerimeterClientException>(
                async () => await client.ReadSortieBoardAsync(HiveId));

            Assert.That(error.Error, Is.EqualTo(HivePerimeterClientError.NotConfigured));
            Assert.That(source.CallCount, Is.Zero);
            Assert.That(transport.CallCount, Is.Zero);
        }

        [Test]
        public void MissingOfficialSessionBlocksBeforeTransport()
        {
            var source = new FakeSessionSource(null);
            var transport = new FakeTransport { Response = ValidBoard() };
            var client = NewClient(source, transport);

            HivePerimeterClientException error = Assert.ThrowsAsync<HivePerimeterClientException>(
                async () => await client.ReadSortieBoardAsync(HiveId));

            Assert.That(error.Error, Is.EqualTo(HivePerimeterClientError.AuthenticationRequired));
            Assert.That(source.CallCount, Is.EqualTo(1));
            Assert.That(transport.CallCount, Is.Zero);
        }

        [Test]
        public void CancellationStopsBeforeSessionAndTransport()
        {
            var source = new FakeSessionSource(new GameAccountSession(PlayerId, Token));
            var transport = new FakeTransport { Response = ValidBoard() };
            var client = NewClient(source, transport);
            var cancellation = new CancellationToken(true);

            Assert.ThrowsAsync<TaskCanceledException>(async () => await client.ReadSortieBoardAsync(HiveId, cancellation));
            Assert.That(source.CallCount, Is.Zero);
            Assert.That(transport.CallCount, Is.Zero);
        }

        [Test]
        public async Task BoardReadUsesExactAuthenticatedRouteAndAcceptsBoundSnapshot()
        {
            var source = new FakeSessionSource(new GameAccountSession(PlayerId, Token));
            var board = ValidBoard();
            var transport = new FakeTransport { Response = board };
            var client = NewClient(source, transport);

            RemoteHivePerimeterSnapshot result = await client.ReadSortieBoardAsync(HiveId);

            Assert.That(result, Is.SameAs(board));
            Assert.That(transport.LastRequest.Method, Is.EqualTo("GET"));
            Assert.That(transport.LastRequest.Path, Is.EqualTo("/game/v1/hives/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/perimeter-sortie"));
            Assert.That(transport.LastRequest.Body, Is.Null);
            Assert.That(transport.LastAccessToken, Is.EqualTo(Token));
            Assert.That(transport.LastRequest.ToString(), Does.Not.Contain(Token));
        }

        [Test]
        public async Task ReservationCommitCopiesCanonicalQuantitiesAndUsesStableKey()
        {
            var quantities = new Dictionary<string, long>
            {
                ["guardians"] = 2,
                ["wingrunners"] = 3,
                ["darters"] = 1
            };
            var transport = new FakeTransport
            {
                Response = ValidCommitResponse(
                    7,
                    "reserve-7",
                    quantities)
            };
            var client = NewClient(new FakeSessionSource(new GameAccountSession(PlayerId, Token)), transport);

            await client.CommitReservationAsync(HiveId, 7, quantities, "reserve-7");
            quantities["guardians"] = 99;

            Assert.That(transport.LastRequest.Method, Is.EqualTo("POST"));
            Assert.That(transport.LastRequest.Path, Does.EndWith("/combat/squad-reservation/commit"));
            var body = transport.LastRequest.Body as SquadReservationMutationRequest;
            Assert.That(body, Is.Not.Null);
            Assert.That(body.ExpectedRevision, Is.EqualTo(7));
            Assert.That(body.IdempotencyKey, Is.EqualTo("reserve-7"));
            Assert.That(body.Quantities, Is.EqualTo(new Dictionary<string, long>
            {
                ["guardians"] = 2,
                ["wingrunners"] = 3,
                ["darters"] = 1
            }));
        }

        [Test]
        public async Task ReservationReadAndReleaseUseExactRoutes()
        {
            var transport = new FakeTransport { Response = ValidReservation() };
            var client = NewClient(new FakeSessionSource(new GameAccountSession(PlayerId, Token)), transport);

            await client.ReadReservationAsync(HiveId);
            Assert.That(transport.LastRequest.Method, Is.EqualTo("GET"));
            Assert.That(transport.LastRequest.Path, Does.EndWith("/combat/squad-reservation"));

            transport.Response =
                ValidReleaseResponse(8, "release-8");
            await client.ReleaseReservationAsync(HiveId, 8, "release-8");
            Assert.That(transport.LastRequest.Method, Is.EqualTo("POST"));
            Assert.That(transport.LastRequest.Path, Does.EndWith("/combat/squad-reservation/release"));
            var body = transport.LastRequest.Body as SquadReservationReleaseRequest;
            Assert.That(body.ExpectedRevision, Is.EqualTo(8));
            Assert.That(body.IdempotencyKey, Is.EqualTo("release-8"));
        }

        [Test]
        public async Task ReservationMutationReturnsValidatedPublicReceipt()
        {
            IReadOnlyDictionary<string, long> quantities =
                CanonicalQuantities();
            RemoteSquadReservationResponse response =
                ValidCommitResponse(
                    7,
                    "stable-reserve",
                    quantities);
            var transport = new FakeTransport { Response = response };
            var client = NewClient(
                new FakeSessionSource(
                    new GameAccountSession(PlayerId, Token)),
                transport);

            RemoteSquadReservationResponse result =
                await client.CommitReservationWithReceiptAsync(
                    HiveId,
                    7,
                    quantities,
                    "stable-reserve");

            Assert.That(result.Receipt.PlayerId, Is.EqualTo(PlayerId));
            Assert.That(result.Receipt.HiveId, Is.EqualTo(HiveId));
            Assert.That(result.Receipt.Action, Is.EqualTo("commit"));
            Assert.That(
                result.Receipt.ReservationRevisionBefore,
                Is.EqualTo(7));
            Assert.That(
                result.Receipt.ReservationRevisionAfter,
                Is.EqualTo(8));
            Assert.That(
                typeof(RemoteSquadReservationReceipt)
                    .GetProperty("PayloadHash"),
                Is.Null);
        }

        [Test]
        public void AlteredDetachedOrIncompleteReservationReceiptsAreRejected()
        {
            IReadOnlyDictionary<string, long> quantities =
                CanonicalQuantities();
            var transport = new FakeTransport();
            var client = NewClient(
                new FakeSessionSource(
                    new GameAccountSession(PlayerId, Token)),
                transport);

            RemoteSquadReservationResponse response =
                ValidCommitResponse(7, "receipt", quantities);
            response.Receipt.PlayerId = Guid.NewGuid();
            transport.Response = response;
            AssertInvalidResponse(async () =>
                await client.CommitReservationWithReceiptAsync(
                    HiveId, 7, quantities, "receipt"));

            response = ValidCommitResponse(7, "receipt", quantities);
            response.Receipt.Quantities["guardians"]++;
            transport.Response = response;
            AssertInvalidResponse(async () =>
                await client.CommitReservationWithReceiptAsync(
                    HiveId, 7, quantities, "receipt"));

            response = ValidCommitResponse(7, "receipt", quantities);
            response.Receipt.AcceptedAtUtc =
                response.Receipt.AcceptedAtUtc.ToOffset(
                    TimeSpan.FromHours(1));
            transport.Response = response;
            AssertInvalidResponse(async () =>
                await client.CommitReservationWithReceiptAsync(
                    HiveId, 7, quantities, "receipt"));

            response = ValidReleaseResponse(8, "release");
            response.Receipt.ReservationRevisionAfter = 11;
            transport.Response = response;
            AssertInvalidResponse(async () =>
                await client.ReleaseReservationWithReceiptAsync(
                    HiveId, 8, "release"));
        }

        [Test]
        public async Task OriginalCommitReceiptAcceptsNewerAuthoritativeSnapshot()
        {
            IReadOnlyDictionary<string, long> quantities =
                CanonicalQuantities();
            RemoteSquadReservationResponse response =
                ValidCommitResponse(7, "replay", quantities);
            response.Snapshot =
                ValidReleaseResponse(8, "later-release").Snapshot;
            var transport = new FakeTransport { Response = response };
            var client = NewClient(
                new FakeSessionSource(
                    new GameAccountSession(PlayerId, Token)),
                transport);

            RemoteSquadReservationResponse replay =
                await client.CommitReservationWithReceiptAsync(
                    HiveId,
                    7,
                    quantities,
                    "replay");

            Assert.That(
                replay.Receipt.ReservationRevisionAfter,
                Is.EqualTo(8));
            Assert.That(
                replay.Snapshot.ReservationRevision,
                Is.EqualTo(9));
            Assert.That(replay.Snapshot.ReservationId, Is.Null);
        }

        [Test]
        public async Task LaunchClaimAndRecallPreserveCallerIdempotencyAndRevision()
        {
            var board = ValidBoard(true);
            var transport = new FakeTransport { Response = board };
            var client = NewClient(new FakeSessionSource(new GameAccountSession(PlayerId, Token)), transport);
            string instance = board.Signals.Single(signal => signal.SignalKey == "foraging_scout").SignalInstanceId;
            string reservation = board.Reservation.ReservationId;

            await client.LaunchAsync(HiveId, "foraging_scout", instance, reservation, 0, "launch-1");
            var launch = transport.LastRequest.Body as HivePerimeterLaunchRequest;
            Assert.That(transport.LastRequest.Path, Does.EndWith("/perimeter-sortie/launch"));
            Assert.That(launch.SignalInstanceId, Is.EqualTo(instance));
            Assert.That(launch.ReservationId, Is.EqualTo(reservation));
            Assert.That(launch.ExpectedRevision, Is.Zero);
            Assert.That(launch.IdempotencyKey, Is.EqualTo("launch-1"));

            Guid sortieId = board.Active.SortieId;
            await client.ClaimAsync(HiveId, sortieId, 1, "claim-1");
            var claim = transport.LastRequest.Body as HivePerimeterMutationRequest;
            Assert.That(transport.LastRequest.Path, Does.EndWith("/" + sortieId.ToString("D") + "/claim"));
            Assert.That(claim.ExpectedRevision, Is.EqualTo(1));
            Assert.That(claim.IdempotencyKey, Is.EqualTo("claim-1"));

            await client.RecallAsync(HiveId, sortieId, 1, "recall-1");
            var recall = transport.LastRequest.Body as HivePerimeterMutationRequest;
            Assert.That(transport.LastRequest.Path, Does.EndWith("/" + sortieId.ToString("D") + "/recall"));
            Assert.That(recall.ExpectedRevision, Is.EqualTo(1));
            Assert.That(recall.IdempotencyKey, Is.EqualTo("recall-1"));
        }

        [Test]
        public async Task ClaimAcceptsBoundServerReceiptWithPartialCapacityCredit()
        {
            RemoteHivePerimeterSnapshot board = ValidClaimBoard(10, 20, 130, 130, 120, 1000);
            var transport = new FakeTransport { Response = board };
            var client = NewClient(new FakeSessionSource(new GameAccountSession(PlayerId, Token)), transport);

            RemoteHivePerimeterSnapshot result = await client.ClaimAsync(
                HiveId,
                board.ClaimReceipt.SortieId,
                1,
                "claim-partial");

            Assert.That(result.ClaimReceipt.CreditedByResource["honey"], Is.EqualTo(10));
            Assert.That(result.ClaimReceipt.CreditedByResource["pollen"], Is.EqualTo(20));
            Assert.That(result.ClaimReceipt.ResultingBalances["honey"].Amount, Is.EqualTo(130));
            Assert.That(result.ClaimReceipt.ResultingBalances["honey"].Capacity, Is.EqualTo(130));
            Assert.That(transport.LastRequest.Path, Does.EndWith("/" + board.ClaimReceipt.SortieId.ToString("D") + "/claim"));
        }

        [Test]
        public void ForeignDetachedOrUnexplainedClaimReceiptsAreRejected()
        {
            var transport = new FakeTransport();
            var client = NewClient(new FakeSessionSource(new GameAccountSession(PlayerId, Token)), transport);

            RemoteHivePerimeterSnapshot board = ValidClaimBoard(40, 20, 140, 1000, 120, 1000);
            board.ClaimReceipt.PlayerId = Guid.NewGuid();
            transport.Response = board;
            AssertInvalidResponse(async () => await client.ClaimAsync(HiveId, Guid.NewGuid(), 1, "foreign"));

            board = ValidClaimBoard(40, 20, 140, 1000, 120, 1000);
            board.ClaimReceipt.SignalInstanceId = new string('f', 32);
            transport.Response = board;
            AssertInvalidResponse(async () => await client.ClaimAsync(HiveId, Guid.NewGuid(), 1, "detached"));

            board = ValidClaimBoard(10, 20, 110, 130, 120, 1000);
            transport.Response = board;
            AssertInvalidResponse(async () => await client.ClaimAsync(HiveId, Guid.NewGuid(), 1, "unexplained"));

            board = ValidClaimBoard(40, 20, 140, 1000, 120, 1000);
            board.ClaimReceipt.ResultingBalances.Remove("pollen");
            transport.Response = board;
            AssertInvalidResponse(async () => await client.ClaimAsync(HiveId, Guid.NewGuid(), 1, "missing-resource"));
        }

        [Test]
        public async Task SecondSignalUsesExposedCycleRevisionAfterFirstClaim()
        {
            RemoteHivePerimeterSnapshot afterClaim = ValidBoard();
            afterClaim.Revision = 2;
            afterClaim.Signals[0].Completed = true;
            afterClaim.Signals[0].CanLaunch = false;
            RemoteHivePerimeterSignal second = afterClaim.Signals.Single(signal => signal.SignalKey == "brood_watch");

            RemoteHivePerimeterSnapshot launched = ValidBoard(true);
            launched.Revision = 3;
            launched.Active.Revision = 3;
            launched.Active.SignalKey = second.SignalKey;
            launched.Active.SignalInstanceId = second.SignalInstanceId;
            launched.Active.EndsAtUtc = launched.Active.StartedAtUtc.Add(second.Duration);
            var transport = new FakeTransport { Response = launched };
            var client = NewClient(new FakeSessionSource(new GameAccountSession(PlayerId, Token)), transport);

            RemoteHivePerimeterSnapshot result = await client.LaunchAsync(
                HiveId,
                second.SignalKey,
                second.SignalInstanceId,
                afterClaim.Reservation.ReservationId,
                afterClaim.Revision,
                "launch-second");

            var body = transport.LastRequest.Body as HivePerimeterLaunchRequest;
            Assert.That(body.ExpectedRevision, Is.EqualTo(2));
            Assert.That(body.SignalKey, Is.EqualTo("brood_watch"));
            Assert.That(result.Revision, Is.EqualTo(3));
            Assert.That(result.Active.SignalInstanceId, Is.EqualTo(second.SignalInstanceId));
            Assert.That(result.Active.ReservationId, Is.EqualTo(afterClaim.Reservation.ReservationId));
        }

        [Test]
        public void InvalidLocalCommandsNeverReachSessionOrTransport()
        {
            var source = new FakeSessionSource(new GameAccountSession(PlayerId, Token));
            var transport = new FakeTransport { Response = ValidBoard() };
            var client = NewClient(source, transport);

            AssertInvalidRequest(() => client.ReadSortieBoardAsync(Guid.Empty));
            AssertInvalidRequest(() => client.CommitReservationAsync(HiveId, -1, CanonicalQuantities(), "key"));
            AssertInvalidRequest(() => client.CommitReservationAsync(HiveId, long.MaxValue, CanonicalQuantities(), "key"));
            AssertInvalidRequest(() => client.CommitReservationAsync(HiveId, 0, new Dictionary<string, long> { ["guardians"] = 1 }, "key"));
            AssertInvalidRequest(() => client.CommitReservationAsync(HiveId, 0, new Dictionary<string, long> { ["guardians"] = 5, ["wingrunners"] = 5, ["darters"] = 5 }, "key"));
            AssertInvalidRequest(() => client.LaunchAsync(HiveId, "unknown", new string('a', 32), new string('b', 32), 0, "key"));
            AssertInvalidRequest(() => client.ClaimAsync(HiveId, Guid.Empty, 0, "key"));
            AssertInvalidRequest(() => client.ReleaseReservationAsync(HiveId, 0, ""));

            Assert.That(source.CallCount, Is.Zero);
            Assert.That(transport.CallCount, Is.Zero);
        }

        [Test]
        public void ForeignOrUnsupportedResponsesAreRejected()
        {
            var transport = new FakeTransport();
            var client = NewClient(new FakeSessionSource(new GameAccountSession(PlayerId, Token)), transport);
            RemoteHivePerimeterSnapshot board = ValidBoard();
            board.PlayerId = Guid.NewGuid();
            transport.Response = board;
            AssertInvalidResponse(async () => await client.ReadSortieBoardAsync(HiveId));

            board = ValidBoard();
            board.ContractVersion = "phase6-invented";
            transport.Response = board;
            AssertInvalidResponse(async () => await client.ReadSortieBoardAsync(HiveId));

            RemoteSquadReservationSnapshot reservation = ValidReservation();
            reservation.CatalogVersion = "invented";
            transport.Response = reservation;
            AssertInvalidResponse(async () => await client.ReadReservationAsync(HiveId));
        }

        [Test]
        public void MalformedCatalogReservationAndActiveLinkAreRejected()
        {
            var transport = new FakeTransport();
            var client = NewClient(new FakeSessionSource(new GameAccountSession(PlayerId, Token)), transport);
            RemoteHivePerimeterSnapshot board = ValidBoard();
            board.Signals[0].HoneyReward++;
            transport.Response = board;
            AssertInvalidResponse(async () => await client.ReadSortieBoardAsync(HiveId));

            board = ValidBoard();
            board.Reservation.Available["guardians"]++;
            transport.Response = board;
            AssertInvalidResponse(async () => await client.ReadSortieBoardAsync(HiveId));

            board = ValidBoard(true);
            board.Active.ReservationId = new string('c', 32);
            transport.Response = board;
            AssertInvalidResponse(async () => await client.ReadSortieBoardAsync(HiveId));

            board = ValidBoard();
            board.Signals[0].SignalInstanceId = new string('0', 32);
            transport.Response = board;
            AssertInvalidResponse(async () => await client.ReadSortieBoardAsync(HiveId));

            board = ValidBoard();
            board.ServerTimeUtc = board.ServerTimeUtc.ToOffset(TimeSpan.FromHours(1));
            transport.Response = board;
            AssertInvalidResponse(async () => await client.ReadSortieBoardAsync(HiveId));
        }

        [Test]
        public void ProofRowsKeepDeviceAndServerAuthorityExplicitWithoutSecrets()
        {
            var client = NewClient(
                new FakeSessionSource(new GameAccountSession(PlayerId, Token)),
                new FakeTransport { Response = ValidBoard() });
            string proof = string.Join("\n", client.ProofRows());

            Assert.That(proof, Does.Contain("server_time_authoritative:true"));
            Assert.That(proof, Does.Contain("server_rewards_authoritative:true"));
            Assert.That(proof, Does.Contain("mobile_reservation_receipt_validated:true"));
            Assert.That(proof, Does.Contain("device_snapshot_memory_only:true"));
            Assert.That(proof, Does.Contain("access_token_persisted:false"));
            Assert.That(proof, Does.Not.Contain(Token));
        }

        [Test]
        public async Task UnauthorizedResponseRotatesOnceAndReplaysSameMutation()
        {
            var source = new FakeRefreshableSessionSource(PlayerId, Token, "rotated-token");
            var transport = new ScriptedTransport(
                new AuthenticatedGameRestException(AuthenticatedGameRestError.Unauthorized, "game.session_required", 401),
                ValidBoard());
            var client = NewClient(source, transport);

            await client.ClaimAsync(
                HiveId,
                Guid.Parse("99999999-8888-7777-6666-555555555555"),
                4,
                "stable-claim-key");

            Assert.That(source.RefreshCalls, Is.EqualTo(1));
            Assert.That(source.InvalidateCalls, Is.Zero);
            Assert.That(transport.Requests.Count, Is.EqualTo(2));
            Assert.That(transport.Requests[1], Is.SameAs(transport.Requests[0]));
            Assert.That(transport.Tokens, Is.EqualTo(new[] { Token, "rotated-token" }));
            Assert.That(((HivePerimeterMutationRequest)transport.Requests[1].Body).IdempotencyKey, Is.EqualTo("stable-claim-key"));
        }

        [Test]
        public void SecondUnauthorizedPurgesSessionAndNeverAttemptsThirdRequest()
        {
            var source = new FakeRefreshableSessionSource(PlayerId, Token, "rotated-token");
            var transport = new ScriptedTransport(
                new AuthenticatedGameRestException(AuthenticatedGameRestError.Unauthorized, "game.session_required", 401),
                new AuthenticatedGameRestException(AuthenticatedGameRestError.Unauthorized, "game.session_required", 401));
            var client = NewClient(source, transport);

            HivePerimeterClientException error = Assert.ThrowsAsync<HivePerimeterClientException>(
                async () => await client.ReadSortieBoardAsync(HiveId));

            Assert.That(error.Error, Is.EqualTo(HivePerimeterClientError.AuthenticationRequired));
            Assert.That(source.RefreshCalls, Is.EqualTo(1));
            Assert.That(source.InvalidateCalls, Is.EqualTo(1));
            Assert.That(transport.Requests.Count, Is.EqualTo(2));
        }

        [Test]
        public void NetworkFailureNeverRetriesMutationOrReadsCache()
        {
            var source = new FakeRefreshableSessionSource(PlayerId, Token, "rotated-token");
            var transport = new ScriptedTransport(
                new AuthenticatedGameRestException(AuthenticatedGameRestError.NetworkFailure, "game.network_unavailable"));
            var store = new MemoryGameReadCacheStore();
            var cache = new ProtectedGameReadCache(store, new SystemTextGameJsonCodec(), new FixedClock(ValidBoard().ServerTimeUtc));
            var client = NewClient(source, transport, cache);

            HivePerimeterClientException error = Assert.ThrowsAsync<HivePerimeterClientException>(
                async () => await client.ClaimAsync(HiveId, Guid.NewGuid(), 1, "no-network-retry"));

            Assert.That(error.Error, Is.EqualTo(HivePerimeterClientError.TransportFailure));
            Assert.That(transport.Requests.Count, Is.EqualTo(1));
            Assert.That(source.RefreshCalls, Is.Zero);
            Assert.That(store.LoadCalls, Is.Zero);
            Assert.That(store.SaveCalls, Is.Zero);
        }

        [Test]
        public async Task ValidatedGetFallsBackToProtectedPartitionedCacheOnNetworkFailure()
        {
            RemoteHivePerimeterSnapshot board = ValidBoard();
            var source = new FakeRefreshableSessionSource(PlayerId, Token, "rotated-token");
            var transport = new ScriptedTransport(
                board,
                new AuthenticatedGameRestException(AuthenticatedGameRestError.NetworkFailure, "game.network_unavailable"));
            var store = new MemoryGameReadCacheStore();
            var cache = new ProtectedGameReadCache(store, new SystemTextGameJsonCodec(), new FixedClock(board.ServerTimeUtc));
            var client = NewClient(source, transport, cache);

            RemoteHivePerimeterSnapshot online = await client.ReadSortieBoardAsync(HiveId);
            RemoteHivePerimeterSnapshot offline = await client.ReadSortieBoardAsync(HiveId);

            Assert.That(online.PlayerId, Is.EqualTo(PlayerId));
            Assert.That(offline.PlayerId, Is.EqualTo(PlayerId));
            Assert.That(client.LastReadSource, Is.EqualTo(GameReadSource.ProtectedCache));
            Assert.That(client.LastReadCachedAtUtc, Is.EqualTo(board.ServerTimeUtc));
            Assert.That(store.SaveCalls, Is.EqualTo(1));
            Assert.That(transport.Requests.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task ProtectedCacheNeverCrossesPlayerPartition()
        {
            RemoteHivePerimeterSnapshot board = ValidBoard();
            var store = new MemoryGameReadCacheStore();
            var cache = new ProtectedGameReadCache(store, new SystemTextGameJsonCodec(), new FixedClock(board.ServerTimeUtc));
            var first = NewClient(
                new FakeRefreshableSessionSource(PlayerId, Token, "rotated-a"),
                new ScriptedTransport(board),
                cache);
            await first.ReadSortieBoardAsync(HiveId);

            Guid otherPlayer = Guid.Parse("22222222-3333-4444-5555-666666666666");
            var second = NewClient(
                new FakeRefreshableSessionSource(otherPlayer, "other-token", "rotated-b"),
                new ScriptedTransport(new AuthenticatedGameRestException(
                    AuthenticatedGameRestError.NetworkFailure,
                    "game.network_unavailable")),
                cache);

            HivePerimeterClientException error = Assert.ThrowsAsync<HivePerimeterClientException>(
                async () => await second.ReadSortieBoardAsync(HiveId));

            Assert.That(error.Error, Is.EqualTo(HivePerimeterClientError.TransportFailure));
            Assert.That(second.LastReadSource, Is.EqualTo(GameReadSource.None));
        }

        [Test]
        public void CorruptedProtectedCacheIsPurgedWithoutInventingOfflineState()
        {
            RemoteHivePerimeterSnapshot board = ValidBoard();
            var store = new MemoryGameReadCacheStore { Value = "{corrupted" };
            var cache = new ProtectedGameReadCache(store, new SystemTextGameJsonCodec(), new FixedClock(board.ServerTimeUtc));
            var client = NewClient(
                new FakeRefreshableSessionSource(PlayerId, Token, "rotated-token"),
                new ScriptedTransport(new AuthenticatedGameRestException(
                    AuthenticatedGameRestError.NetworkFailure,
                    "game.network_unavailable")),
                cache);

            Assert.ThrowsAsync<HivePerimeterClientException>(async () => await client.ReadSortieBoardAsync(HiveId));

            Assert.That(cache.LastLoadDetectedCorruption, Is.True);
            Assert.That(store.DeleteCalls, Is.EqualTo(1));
            Assert.That(store.Value, Is.Null);
            Assert.That(client.LastReadSource, Is.EqualTo(GameReadSource.None));
        }

        [Test]
        public void SystemTextCodecAcceptsServerPlayerIdObjectAndDictionaryPayload()
        {
            var codec = new SystemTextGameJsonCodec();
            string json = "{\"playerId\":{\"value\":\"11111111-2222-3333-4444-555555555555\"}," +
                "\"hiveId\":\"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee\",\"contractVersion\":\"phase4-combat-squad-reservation-v1\"," +
                "\"catalogVersion\":\"phase4-combat-v1\",\"rosterRevision\":1,\"reservationRevision\":2,\"capacity\":12," +
                "\"roster\":{\"guardians\":4},\"available\":{\"guardians\":3},\"reserved\":{\"guardians\":1},\"reservationId\":\"r-1\"}";

            RemoteSquadReservationSnapshot value = codec.Deserialize<RemoteSquadReservationSnapshot>(json);

            Assert.That(value.PlayerId, Is.EqualTo(PlayerId));
            Assert.That(value.HiveId, Is.EqualTo(HiveId));
            Assert.That(value.Reserved["guardians"], Is.EqualTo(1));
        }

        private static HivePerimeterSortieClient NewClient(
            IGameAccountSessionSource source,
            IAuthenticatedGameRestTransport transport,
            ProtectedGameReadCache cache = null)
        {
            var gate = new MobileAccountSessionGate();
            gate.ConfigureTransport(true);
            gate.Apply(AccountSessionReadinessSnapshot.FromServer(true, true, true, true, true));
            return new HivePerimeterSortieClient(gate, source, transport, cache);
        }

        private static RemoteHivePerimeterSnapshot ValidBoard(bool active = false)
        {
            DateTimeOffset cycle = new DateTimeOffset(2026, 7, 21, 8, 0, 0, TimeSpan.Zero);
            var board = new RemoteHivePerimeterSnapshot
            {
                PlayerId = PlayerId,
                HiveId = HiveId,
                ContractVersion = HivePerimeterSortieClient.SortieContractVersion,
                Revision = active ? 1 : 0,
                ServerTimeUtc = cycle.AddMinutes(5),
                CycleStartedAtUtc = cycle,
                CycleEndsAtUtc = cycle.AddHours(8),
                Reservation = ValidReservation(),
                Signals = new List<RemoteHivePerimeterSignal>
                {
                    Signal("foraging_scout", "wingrunners", 16, 1, 40, 20, cycle, active),
                    Signal("brood_watch", "guardians", 20, 2, 25, 35, cycle, active)
                }
            };
            if (active)
            {
                RemoteHivePerimeterSignal signal = board.Signals[0];
                board.Active = new RemoteHivePerimeterActiveSortie
                {
                    SortieId = Guid.Parse("12345678-1234-1234-1234-123456789abc"),
                    SignalKey = signal.SignalKey,
                    SignalInstanceId = signal.SignalInstanceId,
                    ReservationId = board.Reservation.ReservationId,
                    StartedAtUtc = cycle.AddMinutes(5),
                    EndsAtUtc = cycle.AddMinutes(5).Add(signal.Duration),
                    Revision = 1
                };
            }
            return board;
        }

        private static RemoteHivePerimeterSnapshot ValidClaimBoard(
            long honeyCredited,
            long pollenCredited,
            long honeyAmount,
            long honeyCapacity,
            long pollenAmount,
            long pollenCapacity)
        {
            RemoteHivePerimeterSnapshot board = ValidBoard();
            board.Revision = 2;
            board.Signals[0].Completed = true;
            board.Signals[0].CanLaunch = false;
            board.Reservation.ReservationRevision++;
            board.Reservation.Available = new Dictionary<string, long>(board.Reservation.Roster);
            board.Reservation.Reserved = new Dictionary<string, long>
            {
                ["guardians"] = 0,
                ["wingrunners"] = 0,
                ["darters"] = 0
            };
            board.Reservation.ReservationId = null;
            Guid sortieId = Guid.Parse("87654321-4321-4321-4321-cba987654321");
            board.ClaimReceipt = new RemoteHivePerimeterClaimReceipt
            {
                PlayerId = PlayerId,
                HiveId = HiveId,
                SortieId = sortieId,
                SignalKey = board.Signals[0].SignalKey,
                SignalInstanceId = board.Signals[0].SignalInstanceId,
                CycleStartedAtUtc = board.CycleStartedAtUtc,
                CycleEndsAtUtc = board.CycleEndsAtUtc,
                Revision = board.Revision,
                ServerTimeUtc = board.ServerTimeUtc,
                CreditedByResource = new Dictionary<string, long>
                {
                    ["honey"] = honeyCredited,
                    ["pollen"] = pollenCredited
                },
                ResultingBalances = new Dictionary<string, RemoteHiveResourceBalance>
                {
                    ["honey"] = new RemoteHiveResourceBalance { Amount = honeyAmount, Capacity = honeyCapacity },
                    ["pollen"] = new RemoteHiveResourceBalance { Amount = pollenAmount, Capacity = pollenCapacity }
                }
            };
            return board;
        }

        private static RemoteHivePerimeterSignal Signal(
            string key,
            string hazard,
            int seconds,
            int minimum,
            long honey,
            long pollen,
            DateTimeOffset cycle,
            bool active)
        {
            return new RemoteHivePerimeterSignal
            {
                SignalKey = key,
                SignalInstanceId = SignalInstance(PlayerId, HiveId, cycle, key),
                HazardDoctrine = hazard,
                Duration = TimeSpan.FromSeconds(seconds),
                MinimumSquad = minimum,
                HoneyReward = honey,
                PollenReward = pollen,
                Completed = false,
                CanLaunch = !active
            };
        }

        private static RemoteSquadReservationSnapshot ValidReservation()
        {
            return new RemoteSquadReservationSnapshot
            {
                PlayerId = PlayerId,
                HiveId = HiveId,
                ContractVersion = HivePerimeterSortieClient.ReservationContractVersion,
                CatalogVersion = HivePerimeterSortieClient.RecruitmentCatalogVersion,
                RosterRevision = 5,
                ReservationRevision = 8,
                Capacity = 12,
                Roster = new Dictionary<string, long> { ["guardians"] = 4, ["wingrunners"] = 6, ["darters"] = 4 },
                Available = new Dictionary<string, long> { ["guardians"] = 2, ["wingrunners"] = 3, ["darters"] = 3 },
                Reserved = new Dictionary<string, long> { ["guardians"] = 2, ["wingrunners"] = 3, ["darters"] = 1 },
                ReservationId = new string('a', 32)
            };
        }

        private static RemoteSquadReservationResponse ValidCommitResponse(
            long expectedRevision,
            string idempotencyKey,
            IReadOnlyDictionary<string, long> quantities)
        {
            RemoteSquadReservationSnapshot snapshot =
                ValidReservation();
            snapshot.ReservationRevision = expectedRevision + 1;
            snapshot.Reserved = quantities.ToDictionary(
                item => item.Key,
                item => item.Value,
                StringComparer.Ordinal);
            snapshot.Available = snapshot.Roster.ToDictionary(
                item => item.Key,
                item => item.Value -
                    snapshot.Reserved[item.Key],
                StringComparer.Ordinal);
            return new RemoteSquadReservationResponse
            {
                Receipt = new RemoteSquadReservationReceipt
                {
                    PlayerId = PlayerId,
                    HiveId = HiveId,
                    IdempotencyKey = idempotencyKey,
                    Action = "commit",
                    ReservationId = snapshot.ReservationId,
                    Quantities = quantities.ToDictionary(
                        item => item.Key,
                        item => item.Value,
                        StringComparer.Ordinal),
                    ReservationRevisionBefore = expectedRevision,
                    ReservationRevisionAfter =
                        expectedRevision + 1,
                    AcceptedAtUtc = new DateTimeOffset(
                        2026,
                        7,
                        23,
                        8,
                        0,
                        0,
                        TimeSpan.Zero),
                    Code = "game.squad_reserved"
                },
                Snapshot = snapshot
            };
        }

        private static RemoteSquadReservationResponse ValidReleaseResponse(
            long expectedRevision,
            string idempotencyKey)
        {
            RemoteSquadReservationSnapshot snapshot =
                ValidReservation();
            snapshot.ReservationRevision = expectedRevision + 1;
            snapshot.Available = new Dictionary<string, long>(
                snapshot.Roster,
                StringComparer.Ordinal);
            snapshot.Reserved = new Dictionary<string, long>
            {
                ["guardians"] = 0,
                ["wingrunners"] = 0,
                ["darters"] = 0
            };
            snapshot.ReservationId = null;
            return new RemoteSquadReservationResponse
            {
                Receipt = new RemoteSquadReservationReceipt
                {
                    PlayerId = PlayerId,
                    HiveId = HiveId,
                    IdempotencyKey = idempotencyKey,
                    Action = "release",
                    ReservationId = null,
                    Quantities = new Dictionary<string, long>
                    {
                        ["guardians"] = 0,
                        ["wingrunners"] = 0,
                        ["darters"] = 0
                    },
                    ReservationRevisionBefore = expectedRevision,
                    ReservationRevisionAfter =
                        expectedRevision + 1,
                    AcceptedAtUtc = new DateTimeOffset(
                        2026,
                        7,
                        23,
                        8,
                        1,
                        0,
                        TimeSpan.Zero),
                    Code = "game.squad_released"
                },
                Snapshot = snapshot
            };
        }

        private static IReadOnlyDictionary<string, long> CanonicalQuantities()
        {
            return new Dictionary<string, long> { ["guardians"] = 1, ["wingrunners"] = 1, ["darters"] = 1 };
        }

        private static string SignalInstance(Guid player, Guid hive, DateTimeOffset cycle, string key)
        {
            string payload = "instance|" + player.ToString("N") + "|" + hive.ToString("N") + "|" +
                cycle.UtcDateTime.ToString("O", CultureInfo.InvariantCulture) + "|" + key;
            byte[] hash;
            using (SHA256 sha = SHA256.Create()) hash = sha.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var builder = new StringBuilder();
            foreach (byte value in hash) builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
            return builder.ToString(0, 32);
        }

        private static void AssertInvalidRequest(Func<Task> operation)
        {
            HivePerimeterClientException error = Assert.ThrowsAsync<HivePerimeterClientException>(async () => await operation());
            Assert.That(error.Error, Is.EqualTo(HivePerimeterClientError.InvalidRequest));
        }

        private static void AssertInvalidResponse(AsyncTestDelegate operation)
        {
            HivePerimeterClientException error = Assert.ThrowsAsync<HivePerimeterClientException>(operation);
            Assert.That(error.Error, Is.EqualTo(HivePerimeterClientError.InvalidResponse));
        }

        private sealed class FakeSessionSource : IGameAccountSessionSource
        {
            private readonly GameAccountSession session;
            public FakeSessionSource(GameAccountSession session) { this.session = session; }
            public int CallCount { get; private set; }
            public bool TryGetSession(out GameAccountSession value)
            {
                CallCount++;
                value = session;
                return session != null;
            }
        }

        private sealed class FakeTransport : IAuthenticatedGameRestTransport
        {
            public object Response { get; set; }
            public int CallCount { get; private set; }
            public AuthenticatedGameRestRequest LastRequest { get; private set; }
            public string LastAccessToken { get; private set; }

            public Task<T> SendAsync<T>(AuthenticatedGameRestRequest request, string bearerAccessToken, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                CallCount++;
                LastRequest = request;
                LastAccessToken = bearerAccessToken;
                return Task.FromResult((T)Response);
            }
        }

        private sealed class FakeRefreshableSessionSource : IRefreshableGameAccountSessionSource
        {
            private readonly Guid playerId;
            private readonly string replacementToken;
            private GameAccountSession session;

            public FakeRefreshableSessionSource(Guid playerId, string token, string replacementToken)
            {
                this.playerId = playerId;
                this.replacementToken = replacementToken;
                session = new GameAccountSession(playerId, token);
            }

            public int RefreshCalls { get; private set; }
            public int InvalidateCalls { get; private set; }

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

            public ScriptedTransport(params object[] steps)
            {
                this.steps = new Queue<object>(steps ?? Array.Empty<object>());
            }

            public List<AuthenticatedGameRestRequest> Requests { get; } = new List<AuthenticatedGameRestRequest>();
            public List<string> Tokens { get; } = new List<string>();

            public Task<T> SendAsync<T>(AuthenticatedGameRestRequest request, string bearerAccessToken, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Requests.Add(request);
                Tokens.Add(bearerAccessToken);
                object step = steps.Count == 0 ? null : steps.Dequeue();
                Exception exception = step as Exception;
                if (exception != null) throw exception;
                return Task.FromResult((T)step);
            }
        }

        private sealed class MemoryGameReadCacheStore : IProtectedGameReadCacheStore
        {
            public bool IsProtectionAvailable { get; set; } = true;
            public string Value { get; set; }
            public int LoadCalls { get; private set; }
            public int SaveCalls { get; private set; }
            public int DeleteCalls { get; private set; }

            public Task<string> LoadAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                LoadCalls++;
                return Task.FromResult(Value);
            }

            public Task SaveAsync(string protectedPlaintext, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                SaveCalls++;
                Value = protectedPlaintext;
                return Task.CompletedTask;
            }

            public Task DeleteAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                DeleteCalls++;
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
