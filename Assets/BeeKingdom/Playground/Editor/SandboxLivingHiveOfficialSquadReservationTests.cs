using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxLivingHiveOfficialSquadReservationTests
    {
        private static readonly Guid PlayerId =
            Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly Guid HiveId =
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly DateTimeOffset Now =
            new DateTimeOffset(
                2026,
                7,
                23,
                9,
                0,
                0,
                TimeSpan.Zero);

        [Test]
        public void NotConfiguredModelInventsNoReservation()
        {
            HiveSquadReservationScreenModel model =
                HiveOfficialSquadReservationPresentation
                    .NotConfigured();

            Assert.That(
                model.State,
                Is.EqualTo(
                    HiveSquadReservationScreenState.NotConfigured));
            Assert.That(model.HasReservation, Is.False);
            Assert.That(model.ReservedTotal, Is.Zero);
            Assert.That(model.ReservationId, Is.Empty);
            Assert.That(model.CanRetry, Is.False);
        }

        [Test]
        public void ReadyAndOfflineModelsRespectServerBoundary()
        {
            RemoteSquadReservationSnapshot snapshot =
                EmptySnapshot();
            HiveSquadReservationScreenModel ready =
                HiveOfficialSquadReservationPresentation.Project(
                    HiveSquadReservationScreenState.Ready,
                    snapshot,
                    true);
            var composition =
                new HiveSquadCompositionSnapshot(12, 3, 6, 3);
            Assert.That(ready.CanCommit(composition), Is.True);

            HiveSquadReservationScreenModel offline =
                HiveOfficialSquadReservationPresentation.Project(
                    HiveSquadReservationScreenState.OfflineReadOnly,
                    snapshot,
                    true,
                    readOnlyOffline: true,
                    cachedAtUtc: Now.AddMinutes(-2));
            Assert.That(offline.ReadOnlyOffline, Is.True);
            Assert.That(offline.CanCommit(composition), Is.False);
            Assert.That(offline.CanRelease, Is.False);
        }

        [Test]
        public async Task CommitIsProtectedBeforeTransportAndCleared()
        {
            var store = new MemoryStore();
            var client = new FakeClient
            {
                ReadSnapshot = EmptySnapshot(),
                CommitResponse = CommitResponse()
            };
            bool protectedBeforeTransport = false;
            client.OnCommit = () =>
                protectedBeforeTransport =
                    !string.IsNullOrEmpty(store.Value);
            using HiveSquadReservationPanelController controller =
                NewController(client, store);

            await controller.RefreshForProofAsync();
            await controller.CommitForProofAsync(3, 6, 3);

            Assert.That(protectedBeforeTransport, Is.True);
            Assert.That(client.CommitCalls, Is.EqualTo(1));
            Assert.That(
                client.LastCommitQuantities,
                Is.EqualTo(new Dictionary<string, long>
                {
                    ["guardians"] = 3,
                    ["wingrunners"] = 6,
                    ["darters"] = 3
                }));
            Assert.That(store.Value, Is.Null);
            Assert.That(controller.Model.HasReservation, Is.True);
            Assert.That(
                controller.Model.SuccessCode,
                Is.EqualTo("game.squad_reserved"));
        }

        [Test]
        public async Task NetworkFailureNeverAutoSubmitsAndRetryUsesExactCommand()
        {
            var store = new MemoryStore();
            var client = new FakeClient
            {
                ReadSnapshot = EmptySnapshot(),
                CommitResponse = CommitResponse(),
                CommitFailure = new HivePerimeterClientException(
                    HivePerimeterClientError.TransportFailure,
                    "game.network_unavailable")
            };
            using HiveSquadReservationPanelController controller =
                NewController(client, store);

            await controller.RefreshForProofAsync();
            await controller.CommitForProofAsync(3, 6, 3);

            Assert.That(controller.Model.IsPending, Is.True);
            Assert.That(client.CommitCalls, Is.EqualTo(1));
            Assert.That(store.Value, Is.Not.Null);
            string key = client.LastCommitKey;

            await controller.RefreshForProofAsync();
            Assert.That(client.CommitCalls, Is.EqualTo(1));
            Assert.That(controller.Model.IsPending, Is.True);

            client.CommitFailure = null;
            await controller.RetryForProofAsync();

            Assert.That(client.CommitCalls, Is.EqualTo(2));
            Assert.That(client.LastCommitKey, Is.EqualTo(key));
            Assert.That(
                client.CommitKeys.Distinct().Count(),
                Is.EqualTo(1));
            Assert.That(store.Value, Is.Null);
            Assert.That(controller.Model.HasReservation, Is.True);
        }

        [Test]
        public async Task PendingCommandSurvivesControllerReconstructionWithoutAutoSubmit()
        {
            var store = new MemoryStore();
            var firstClient = new FakeClient
            {
                ReadSnapshot = EmptySnapshot(),
                CommitFailure = new HivePerimeterClientException(
                    HivePerimeterClientError.TransportFailure,
                    "game.network_unavailable")
            };
            string originalKey;
            using (HiveSquadReservationPanelController first =
                   NewController(firstClient, store))
            {
                await first.RefreshForProofAsync();
                await first.CommitForProofAsync(3, 6, 3);
                originalKey = firstClient.LastCommitKey;
                Assert.That(first.Model.IsPending, Is.True);
                Assert.That(store.Value, Is.Not.Null);
            }

            var replacementClient = new FakeClient
            {
                ReadSnapshot = EmptySnapshot(),
                CommitResponse = CommitResponse()
            };
            using HiveSquadReservationPanelController replacement =
                NewController(replacementClient, store);

            await replacement.RefreshForProofAsync();

            Assert.That(replacement.Model.IsPending, Is.True);
            Assert.That(replacementClient.CommitCalls, Is.Zero);
            Assert.That(store.Value, Is.Not.Null);

            await replacement.RetryForProofAsync();

            Assert.That(replacementClient.CommitCalls, Is.EqualTo(1));
            Assert.That(
                replacementClient.LastCommitKey,
                Is.EqualTo(originalKey));
            Assert.That(store.Value, Is.Null);
            Assert.That(replacement.Model.HasReservation, Is.True);
        }

        [Test]
        public async Task MalformedSuccessResponseRemainsPendingForExplicitRecovery()
        {
            var store = new MemoryStore();
            var client = new FakeClient
            {
                ReadSnapshot = EmptySnapshot(),
                CommitFailure = new HivePerimeterClientException(
                    HivePerimeterClientError.InvalidResponse,
                    "The receipt was malformed.")
            };
            using HiveSquadReservationPanelController controller =
                NewController(client, store);

            await controller.RefreshForProofAsync();
            await controller.CommitForProofAsync(3, 6, 3);

            Assert.That(controller.Model.IsPending, Is.True);
            Assert.That(store.Value, Is.Not.Null);
            Assert.That(client.CommitCalls, Is.EqualTo(1));
        }

        [Test]
        public async Task DefinitiveRevisionConflictClearsPendingCommand()
        {
            var store = new MemoryStore();
            var client = new FakeClient
            {
                ReadSnapshot = EmptySnapshot(),
                CommitFailure = new HivePerimeterClientException(
                    HivePerimeterClientError.InvalidResponse,
                    "game.revision_conflict")
            };
            using HiveSquadReservationPanelController controller =
                NewController(client, store);

            await controller.RefreshForProofAsync();
            await controller.CommitForProofAsync(3, 6, 3);

            Assert.That(
                controller.Model.State,
                Is.EqualTo(HiveSquadReservationScreenState.Error));
            Assert.That(
                controller.Model.ErrorCode,
                Is.EqualTo("revision_conflict"));
            Assert.That(store.Value, Is.Null);
        }

        [Test]
        public async Task ReleaseIsProtectedAndReturnsServerEmptyReservation()
        {
            var store = new MemoryStore();
            var client = new FakeClient
            {
                ReadSnapshot = ReservedSnapshot(),
                ReleaseResponse = ReleaseResponse()
            };
            bool protectedBeforeTransport = false;
            client.OnRelease = () =>
                protectedBeforeTransport =
                    !string.IsNullOrEmpty(store.Value);
            using HiveSquadReservationPanelController controller =
                NewController(client, store);

            await controller.RefreshForProofAsync();
            Assert.That(controller.Model.CanRelease, Is.True);
            await controller.ReleaseForProofAsync();

            Assert.That(protectedBeforeTransport, Is.True);
            Assert.That(client.ReleaseCalls, Is.EqualTo(1));
            Assert.That(controller.Model.HasReservation, Is.False);
            Assert.That(store.Value, Is.Null);
            Assert.That(
                controller.Model.SuccessCode,
                Is.EqualTo("game.squad_released"));
        }

        [Test]
        public void OfficialReservationLayoutKeepsAllActionsTouchable()
        {
            foreach (var size in new[]
            {
                new Vector3(390f, 844f, 1f),
                new Vector3(1600f, 900f, 0f)
            })
            {
                bool portrait = size.z > 0.5f;
                Rect primary =
                    HiveViewProductUiPresenter
                        .FormationReadinessCommitRectForProof(
                            portrait,
                            size.x,
                            size.y);
                Rect secondary =
                    HiveViewProductUiPresenter
                        .FormationReadinessSecondaryActionRectForProof(
                            portrait,
                            size.x,
                            size.y);
                Rect wide =
                    HiveViewProductUiPresenter
                        .FormationReadinessWideActionRectForProof(
                            portrait,
                            size.x,
                            size.y);
                Assert.That(primary.height, Is.GreaterThanOrEqualTo(44f));
                Assert.That(secondary.height, Is.GreaterThanOrEqualTo(44f));
                Assert.That(wide.height, Is.GreaterThanOrEqualTo(44f));
                Assert.That(primary.Overlaps(secondary), Is.False);
                Assert.That(
                    wide.width,
                    Is.EqualTo(
                        primary.width +
                        secondary.width +
                        8f).Within(0.01f));
            }
        }

        [Test]
        public void OfficialReservationCopyExistsInBothCatalogs()
        {
            string[] keys =
            {
                "formation_readiness.reservation.loading",
                "formation_readiness.reservation.not_configured",
                "formation_readiness.reservation.offline",
                "formation_readiness.reservation.pending",
                "formation_readiness.reservation.active",
                "formation_readiness.reservation.server_title",
                "formation_readiness.reservation.server_disclosure",
                "formation_readiness.reservation.verify",
                "formation_readiness.reservation.release",
                "formation_readiness.reservation.commit",
                "formation_readiness.reservation.back_to_composition"
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
                        entries.TryGetValue(key, out string value),
                        Is.True,
                        locale + " " + key);
                    Assert.That(value, Is.Not.Empty);
                    Assert.That(value, Is.Not.EqualTo(key));
                }
            }
        }

        private static HiveSquadReservationPanelController
            NewController(
                FakeClient client,
                MemoryStore store)
        {
            var clock = new FixedClock(Now);
            return new HiveSquadReservationPanelController(
                client,
                HiveId,
                new ProtectedGameMutationOutbox(
                    store,
                    new SystemTextGameJsonCodec(),
                    clock),
                new FixedKeySource(),
                clock);
        }

        private static RemoteSquadReservationSnapshot EmptySnapshot()
        {
            return Snapshot(
                0,
                0,
                0,
                0,
                null);
        }

        private static RemoteSquadReservationSnapshot ReservedSnapshot()
        {
            return Snapshot(
                1,
                3,
                6,
                3,
                new string('a', 32));
        }

        private static RemoteSquadReservationSnapshot Snapshot(
            long revision,
            long guardians,
            long wingrunners,
            long darters,
            string reservationId)
        {
            var roster = new Dictionary<string, long>
            {
                ["guardians"] = 8,
                ["wingrunners"] = 8,
                ["darters"] = 8
            };
            var reserved = new Dictionary<string, long>
            {
                ["guardians"] = guardians,
                ["wingrunners"] = wingrunners,
                ["darters"] = darters
            };
            return new RemoteSquadReservationSnapshot
            {
                PlayerId = PlayerId,
                HiveId = HiveId,
                ContractVersion =
                    HivePerimeterSortieClient
                        .ReservationContractVersion,
                CatalogVersion =
                    HivePerimeterSortieClient
                        .RecruitmentCatalogVersion,
                RosterRevision = 4,
                ReservationRevision = revision,
                Capacity = 12,
                Roster = roster,
                Available = roster.ToDictionary(
                    item => item.Key,
                    item => item.Value - reserved[item.Key],
                    StringComparer.Ordinal),
                Reserved = reserved,
                ReservationId = reservationId
            };
        }

        private static RemoteSquadReservationResponse CommitResponse()
        {
            RemoteSquadReservationSnapshot snapshot =
                ReservedSnapshot();
            return new RemoteSquadReservationResponse
            {
                Receipt = Receipt(
                    "fixed-reserve",
                    "commit",
                    snapshot.ReservationId,
                    0,
                    1,
                    snapshot.Reserved,
                    "game.squad_reserved"),
                Snapshot = snapshot
            };
        }

        private static RemoteSquadReservationResponse ReleaseResponse()
        {
            return new RemoteSquadReservationResponse
            {
                Receipt = Receipt(
                    "fixed-release",
                    "release",
                    null,
                    1,
                    2,
                    new Dictionary<string, long>
                    {
                        ["guardians"] = 0,
                        ["wingrunners"] = 0,
                        ["darters"] = 0
                    },
                    "game.squad_released"),
                Snapshot = Snapshot(2, 0, 0, 0, null)
            };
        }

        private static RemoteSquadReservationReceipt Receipt(
            string key,
            string action,
            string reservationId,
            long before,
            long after,
            IReadOnlyDictionary<string, long> quantities,
            string code)
        {
            return new RemoteSquadReservationReceipt
            {
                PlayerId = PlayerId,
                HiveId = HiveId,
                IdempotencyKey = key,
                Action = action,
                ReservationId = reservationId,
                Quantities = quantities.ToDictionary(
                    item => item.Key,
                    item => item.Value,
                    StringComparer.Ordinal),
                ReservationRevisionBefore = before,
                ReservationRevisionAfter = after,
                AcceptedAtUtc = Now,
                Code = code
            };
        }

        private sealed class FakeClient :
            IHiveSquadReservationClient
        {
            public RemoteSquadReservationSnapshot ReadSnapshot { get; set; }
            public RemoteSquadReservationResponse CommitResponse { get; set; }
            public RemoteSquadReservationResponse ReleaseResponse { get; set; }
            public HivePerimeterClientException CommitFailure { get; set; }
            public HivePerimeterClientException ReleaseFailure { get; set; }
            public Action OnCommit { get; set; }
            public Action OnRelease { get; set; }
            public int CommitCalls { get; private set; }
            public int ReleaseCalls { get; private set; }
            public string LastCommitKey { get; private set; }
            public Dictionary<string, long> LastCommitQuantities { get; private set; }
            public List<string> CommitKeys { get; } =
                new List<string>();
            public GameReadSource LastReadSource { get; set; } =
                GameReadSource.Server;
            public DateTimeOffset LastReadCachedAtUtc { get; set; }

            public Task<RemoteSquadReservationSnapshot>
                ReadReservationAsync(
                    Guid hiveId,
                    CancellationToken cancellationToken =
                        default(CancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(ReadSnapshot);
            }

            public Task<RemoteSquadReservationResponse>
                CommitReservationWithReceiptAsync(
                    Guid hiveId,
                    long expectedRevision,
                    IReadOnlyDictionary<string, long> quantities,
                    string idempotencyKey,
                    CancellationToken cancellationToken =
                        default(CancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                CommitCalls++;
                LastCommitKey = idempotencyKey;
                CommitKeys.Add(idempotencyKey);
                LastCommitQuantities = quantities.ToDictionary(
                    item => item.Key,
                    item => item.Value,
                    StringComparer.Ordinal);
                OnCommit?.Invoke();
                if (CommitFailure != null) throw CommitFailure;
                return Task.FromResult(CommitResponse);
            }

            public Task<RemoteSquadReservationResponse>
                ReleaseReservationWithReceiptAsync(
                    Guid hiveId,
                    long expectedRevision,
                    string idempotencyKey,
                    CancellationToken cancellationToken =
                        default(CancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                ReleaseCalls++;
                OnRelease?.Invoke();
                if (ReleaseFailure != null) throw ReleaseFailure;
                return Task.FromResult(ReleaseResponse);
            }
        }

        private sealed class FixedKeySource :
            IHiveSquadReservationKeySource
        {
            public string Create(string operation)
            {
                return "fixed-" + operation;
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

        private sealed class MemoryStore :
            IProtectedGameMutationOutboxStore
        {
            public bool IsProtectionAvailable { get; set; } = true;
            public string Value { get; private set; }

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
    }
}
