using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;
using NUnit.Framework;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxLivingHiveOfficialDoctrineRecruitmentTests
    {
        private static readonly Guid PlayerId =
            Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly Guid HiveId =
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly Guid OperationId =
            Guid.Parse("99999999-8888-7777-6666-555555555555");
        private static readonly DateTimeOffset Now =
            new DateTimeOffset(
                2026,
                7,
                23,
                12,
                0,
                0,
                TimeSpan.Zero);

        public static void RunAllAssertions()
        {
            var tests =
                new SandboxLivingHiveOfficialDoctrineRecruitmentTests();
            tests.NotConfiguredModelInventsNoServerRoster();
            tests.OfficialRosterMapsOnlyServerFamiliesAndKeepsLegacyRoles();
            tests.ReadyAndOfflineModelsRespectTheServerBoundary();
            tests.LocalCountdownNeverUnlocksAnUnconfirmedClaim();
            tests.OfficialRecruitmentCopyExistsInBothCatalogs();
        }

        [Test]
        public void NotConfiguredModelInventsNoServerRoster()
        {
            HiveDoctrineRecruitmentScreenModel model =
                HiveOfficialDoctrineRecruitmentPresentation
                    .NotConfigured();

            Assert.That(
                model.State,
                Is.EqualTo(
                    HiveDoctrineRecruitmentScreenState.NotConfigured));
            Assert.That(model.Offers, Is.Empty);
            Assert.That(model.Counts, Is.Empty);
            Assert.That(
                model.FormationRoster.ServerAuthoritative,
                Is.True);
            Assert.That(
                model.FormationRoster.Families
                    .All(
                        family =>
                            family.State ==
                            HiveFormationRosterState.Empty),
                Is.True);
        }

        [Test]
        public void OfficialRosterMapsOnlyServerFamiliesAndKeepsLegacyRoles()
        {
            RemoteDoctrineRecruitmentSnapshot snapshot = Snapshot();
            snapshot.Counts["guardians"] = 14;
            snapshot.Counts["wingrunners"] = 7;
            snapshot.Counts["darters"] = 2;

            HiveDoctrineRecruitmentScreenModel model =
                HiveOfficialDoctrineRecruitmentPresentation.Project(
                    HiveDoctrineRecruitmentScreenState.Ready,
                    snapshot,
                    TimeSpan.Zero,
                    true);

            Assert.That(
                model.FormationRoster.Find("guardians").EligibleCount,
                Is.EqualTo(14));
            Assert.That(
                model.FormationRoster.Find("wingrunners").EligibleCount,
                Is.EqualTo(7));
            Assert.That(
                model.FormationRoster.Find("darters").EligibleCount,
                Is.EqualTo(2));
            Assert.That(
                model.FormationRoster.UnclassifiedSoldiers,
                Is.Zero);
            Assert.That(
                model.FormationRoster.UnclassifiedScouts,
                Is.Zero);
            Assert.That(
                model.FormationRoster.UnclassifiedLegacyRoles,
                Is.EquivalentTo(
                    new[]
                    {
                        "Soldats",
                        "Gardiennes",
                        "Eclaireuses"
                    }));
        }

        [Test]
        public void ReadyAndOfflineModelsRespectTheServerBoundary()
        {
            RemoteDoctrineRecruitmentSnapshot snapshot = Snapshot();
            HiveDoctrineRecruitmentScreenModel ready =
                HiveOfficialDoctrineRecruitmentPresentation.Project(
                    HiveDoctrineRecruitmentScreenState.Ready,
                    snapshot,
                    TimeSpan.Zero,
                    true);
            HiveDoctrineRecruitmentScreenModel offline =
                HiveOfficialDoctrineRecruitmentPresentation.Project(
                    HiveDoctrineRecruitmentScreenState.OfflineReadOnly,
                    snapshot,
                    TimeSpan.Zero,
                    true,
                    cachedAtUtc: Now.AddMinutes(1));

            Assert.That(ready.CanStart("guardians"), Is.True);
            Assert.That(ready.Balance("honey"), Is.EqualTo(900));
            Assert.That(offline.IsReadOnly, Is.True);
            Assert.That(
                offline.CanStart("guardians"),
                Is.False);
            Assert.That(offline.CanClaim(), Is.False);
        }

        [Test]
        public void LocalCountdownNeverUnlocksAnUnconfirmedClaim()
        {
            RemoteDoctrineRecruitmentSnapshot running = Snapshot();
            running.Revision = 4;
            running.ActiveOperation =
                Operation(
                    HiveDoctrineRecruitmentClient.RunningStatus,
                    Now.AddSeconds(-14),
                    Now);
            HiveDoctrineRecruitmentScreenModel localZero =
                HiveOfficialDoctrineRecruitmentPresentation.Project(
                    HiveDoctrineRecruitmentScreenState.Ready,
                    running,
                    TimeSpan.Zero,
                    true);

            Assert.That(
                localZero.Remaining(TimeSpan.FromSeconds(30)),
                Is.EqualTo(TimeSpan.Zero));
            Assert.That(localZero.CanClaim(), Is.False);

            RemoteDoctrineRecruitmentSnapshot confirmed = Snapshot();
            confirmed.Revision = 4;
            confirmed.ActiveOperation =
                Operation(
                    HiveDoctrineRecruitmentClient
                        .AwaitingCompletionStatus,
                    Now.AddSeconds(-14),
                    Now);
            HiveDoctrineRecruitmentScreenModel awaiting =
                HiveOfficialDoctrineRecruitmentPresentation.Project(
                    HiveDoctrineRecruitmentScreenState.Ready,
                    confirmed,
                    TimeSpan.Zero,
                    true);

            Assert.That(awaiting.CanClaim(), Is.True);
        }

        [Test]
        public async Task PreparedStartIsStoredBeforeTransportAndCleared()
        {
            var store = new MemoryStore();
            var clock = new FakeClock();
            var fake = new FakeClient(store)
            {
                ReadResult = Snapshot(),
                StartResult = StartResponse()
            };
            var controller = NewController(fake, store, clock);
            await controller.RefreshForProofAsync();

            await controller.StartForProofAsync("guardians");

            Assert.That(
                fake.StoreWasPreparedBeforeStart,
                Is.True);
            Assert.That(fake.StartKeys, Has.Count.EqualTo(1));
            Assert.That(store.Value, Is.Null);
            Assert.That(
                controller.Model.ActiveOperation.OperationId,
                Is.EqualTo(OperationId));
            controller.Dispose();
        }

        [Test]
        public async Task NetworkRetryReusesExactProtectedStartKey()
        {
            var store = new MemoryStore();
            var clock = new FakeClock();
            var fake = new FakeClient(store)
            {
                ReadResult = Snapshot(),
                StartResult = StartResponse(),
                FailFirstStart = true
            };
            var controller = NewController(fake, store, clock);
            await controller.RefreshForProofAsync();

            await controller.StartForProofAsync("guardians");
            Assert.That(
                controller.Model.State,
                Is.EqualTo(
                    HiveDoctrineRecruitmentScreenState
                        .PendingConfirmation));
            Assert.That(store.Value, Is.Not.Null);
            await controller.StartForProofAsync(
                "guardians",
                true);

            Assert.That(fake.StartKeys, Has.Count.EqualTo(2));
            Assert.That(
                fake.StartKeys[1],
                Is.EqualTo(fake.StartKeys[0]));
            Assert.That(store.Value, Is.Null);
            controller.Dispose();
        }

        [Test]
        public async Task ClaimIsStoredBeforeTransportAndCleared()
        {
            var store = new MemoryStore();
            var clock = new FakeClock();
            var fake = new FakeClient(store)
            {
                ReadResult = AwaitingClaimSnapshot(),
                ClaimResult = ClaimResponse()
            };
            var controller = NewController(fake, store, clock);
            await controller.RefreshForProofAsync();

            await controller.ClaimForProofAsync();

            Assert.That(
                fake.StoreWasPreparedBeforeClaim,
                Is.True);
            Assert.That(fake.ClaimKeys, Has.Count.EqualTo(1));
            Assert.That(store.Value, Is.Null);
            Assert.That(controller.Model.ActiveOperation, Is.Null);
            Assert.That(
                controller.Model.Count("guardians"),
                Is.EqualTo(4));
            controller.Dispose();
        }

        [Test]
        public async Task AmbiguousClaimCanRetryAfterRefreshShowsNoOperation()
        {
            var store = new MemoryStore();
            var clock = new FakeClock();
            var fake = new FakeClient(store)
            {
                ReadResult = AwaitingClaimSnapshot(),
                ClaimResult = ClaimResponse(),
                FailFirstClaim = true
            };
            var controller = NewController(fake, store, clock);
            await controller.RefreshForProofAsync();

            await controller.ClaimForProofAsync();
            Assert.That(
                controller.Model.State,
                Is.EqualTo(
                    HiveDoctrineRecruitmentScreenState
                        .PendingConfirmation));
            string originalKey = fake.ClaimKeys.Single();

            fake.ReadResult = ClaimResponse().Snapshot;
            await controller.RefreshForProofAsync();
            Assert.That(
                controller.Model.ActiveOperation,
                Is.Null);
            Assert.That(
                controller.Model.CanRetry("guardians"),
                Is.True);

            await controller.ClaimForProofAsync(true);

            Assert.That(fake.ClaimKeys, Has.Count.EqualTo(2));
            Assert.That(
                fake.ClaimKeys[1],
                Is.EqualTo(originalKey));
            Assert.That(store.Value, Is.Null);
            controller.Dispose();
        }

        [Test]
        public async Task ProtectedCacheProjectionRemainsReadOnly()
        {
            var store = new MemoryStore();
            var clock = new FakeClock();
            var fake = new FakeClient(store)
            {
                LastReadSource = GameReadSource.ProtectedCache,
                LastReadCachedAtUtc = Now.AddMinutes(-2),
                ReadResult = AwaitingClaimSnapshot(),
                ClaimResult = ClaimResponse()
            };
            var controller = NewController(fake, store, clock);

            await controller.RefreshForProofAsync();
            await controller.ClaimForProofAsync();
            await controller.StartForProofAsync("guardians");

            Assert.That(
                controller.Model.State,
                Is.EqualTo(
                    HiveDoctrineRecruitmentScreenState
                        .OfflineReadOnly));
            Assert.That(fake.StartKeys, Is.Empty);
            Assert.That(fake.ClaimKeys, Is.Empty);
            Assert.That(store.Value, Is.Null);
            controller.Dispose();
        }

        [Test]
        public void OfficialRecruitmentCopyExistsInBothCatalogs()
        {
            string[] keys =
            {
                "formation_readiness.official.subtitle",
                "formation_readiness.official.server_disclosure",
                "formation_readiness.official.not_configured",
                "formation_readiness.official.offline",
                "formation_readiness.official.pending",
                "formation_readiness.official.verify",
                "formation_readiness.official.claim",
                "formation_readiness.official.queue.ready"
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
                Dictionary<string, string> entries =
                    document.RootElement
                        .GetProperty("entries")
                        .EnumerateArray()
                        .ToDictionary(
                            entry =>
                                entry.GetProperty("key").GetString(),
                            entry =>
                                entry.GetProperty("value").GetString(),
                            StringComparer.Ordinal);
                foreach (string key in keys)
                {
                    Assert.That(
                        entries.TryGetValue(
                            key,
                            out string value),
                        Is.True,
                        locale + " " + key);
                    Assert.That(
                        value,
                        Is.Not.Empty,
                        locale + " " + key);
                    Assert.That(value, Is.Not.EqualTo(key));
                }
            }
        }

        private static HiveDoctrineRecruitmentPanelController
            NewController(
                FakeClient client,
                MemoryStore store,
                FakeClock clock)
        {
            return new HiveDoctrineRecruitmentPanelController(
                client,
                HiveId,
                new ProtectedGameMutationOutbox(
                    store,
                    new SystemTextGameJsonCodec(),
                    clock),
                new FixedKeySource(),
                clock);
        }

        private static RemoteDoctrineRecruitmentSnapshot Snapshot()
        {
            return new RemoteDoctrineRecruitmentSnapshot
            {
                PlayerId = PlayerId,
                HiveId = HiveId,
                ContractVersion =
                    HiveDoctrineRecruitmentClient.ContractVersion,
                CatalogVersion =
                    HiveDoctrineRecruitmentClient.CatalogVersion,
                Revision = 3,
                ServerTimeUtc = Now,
                Offers =
                    new List<RemoteDoctrineRecruitmentOffer>
                    {
                        Offer("guardians", 4, 680, 180),
                        Offer("wingrunners", 6, 420, 260),
                        Offer("darters", 8, 500, 120)
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
                Counts =
                    new Dictionary<string, long>
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

        private static RemoteDoctrineRecruitmentOffer Offer(
            string family,
            int batchSize,
            long honey,
            long pollen)
        {
            return new RemoteDoctrineRecruitmentOffer
            {
                Family = family,
                BatchSize = batchSize,
                HoneyCost = honey,
                PollenCost = pollen,
                Duration = TimeSpan.FromSeconds(14)
            };
        }

        private static RemoteDoctrineRecruitmentOperation Operation(
            string status,
            DateTimeOffset started,
            DateTimeOffset ends)
        {
            return new RemoteDoctrineRecruitmentOperation
            {
                OperationId = OperationId,
                Family = "guardians",
                BatchSize = 4,
                StartedAtUtc = started,
                EndsAtUtc = ends,
                Status = status
            };
        }

        private static RemoteDoctrineRecruitmentSnapshot
            AwaitingClaimSnapshot()
        {
            RemoteDoctrineRecruitmentSnapshot snapshot = Snapshot();
            snapshot.Revision = 4;
            snapshot.ActiveOperation =
                Operation(
                    HiveDoctrineRecruitmentClient
                        .AwaitingCompletionStatus,
                    Now.AddSeconds(-14),
                    Now);
            return snapshot;
        }

        private static RemoteDoctrineRecruitmentResponse StartResponse()
        {
            RemoteDoctrineRecruitmentSnapshot snapshot = Snapshot();
            snapshot.Revision = 4;
            snapshot.Balances["honey"].Amount = 220;
            snapshot.Balances["pollen"].Amount = 620;
            snapshot.ActiveOperation =
                Operation(
                    HiveDoctrineRecruitmentClient.RunningStatus,
                    Now,
                    Now.AddSeconds(14));
            return new RemoteDoctrineRecruitmentResponse
            {
                Receipt =
                    Receipt(
                        "recruitment-proof-key",
                        3,
                        4,
                        HiveDoctrineRecruitmentClient.StartedCode,
                        Now),
                Snapshot = snapshot
            };
        }

        private static RemoteDoctrineRecruitmentResponse ClaimResponse()
        {
            RemoteDoctrineRecruitmentSnapshot snapshot = Snapshot();
            snapshot.Revision = 5;
            snapshot.Counts["guardians"] = 4;
            snapshot.ActiveOperation = null;
            return new RemoteDoctrineRecruitmentResponse
            {
                Receipt =
                    Receipt(
                        "recruitment-proof-key",
                        4,
                        5,
                        HiveDoctrineRecruitmentClient.ClaimedCode,
                        Now),
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

        private sealed class FixedKeySource :
            IHiveDoctrineRecruitmentKeySource
        {
            public string Create(string operation)
            {
                return "recruitment-proof-key";
            }
        }

        private sealed class FakeClock :
            IHiveDoctrineRecruitmentClock,
            IMobileAccountSessionClock
        {
            public TimeSpan Elapsed { get; set; }
            public DateTimeOffset UtcNow => Now.Add(Elapsed);
        }

        private sealed class MemoryStore :
            IProtectedGameMutationOutboxStore
        {
            public bool IsProtectionAvailable => true;
            public string Value { get; set; }

            public Task<string> LoadAsync(
                CancellationToken cancellationToken)
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

            public Task DeleteAsync(
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Value = null;
                return Task.CompletedTask;
            }
        }

        private sealed class FakeClient :
            IHiveDoctrineRecruitmentClient
        {
            private readonly MemoryStore store;

            public FakeClient(MemoryStore store)
            {
                this.store = store;
            }

            public GameReadSource LastReadSource { get; set; } =
                GameReadSource.Server;
            public DateTimeOffset LastReadCachedAtUtc { get; set; }
            public RemoteDoctrineRecruitmentSnapshot ReadResult
            {
                get;
                set;
            }
            public RemoteDoctrineRecruitmentResponse StartResult
            {
                get;
                set;
            }
            public RemoteDoctrineRecruitmentResponse ClaimResult
            {
                get;
                set;
            }
            public bool FailFirstStart { get; set; }
            public bool FailFirstClaim { get; set; }
            public bool StoreWasPreparedBeforeStart { get; private set; }
            public bool StoreWasPreparedBeforeClaim { get; private set; }
            public List<string> StartKeys { get; } =
                new List<string>();
            public List<string> ClaimKeys { get; } =
                new List<string>();

            public Task<RemoteDoctrineRecruitmentSnapshot> ReadAsync(
                Guid hiveId,
                CancellationToken cancellationToken =
                    default(CancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(ReadResult);
            }

            public Task<RemoteDoctrineRecruitmentResponse> StartAsync(
                Guid hiveId,
                string family,
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

            public Task<RemoteDoctrineRecruitmentResponse> ClaimAsync(
                Guid hiveId,
                Guid operationId,
                string expectedFamily,
                long expectedRevision,
                string idempotencyKey,
                CancellationToken cancellationToken =
                    default(CancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                StoreWasPreparedBeforeClaim =
                    StoreWasPreparedBeforeClaim ||
                    !string.IsNullOrWhiteSpace(store.Value);
                ClaimKeys.Add(idempotencyKey);
                if (FailFirstClaim && ClaimKeys.Count == 1)
                    throw new HivePerimeterClientException(
                        HivePerimeterClientError.TransportFailure,
                        "network_unavailable");
                return Task.FromResult(ClaimResult);
            }
        }
    }
}
