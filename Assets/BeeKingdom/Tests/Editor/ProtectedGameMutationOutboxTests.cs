using System;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class ProtectedGameMutationOutboxTests
    {
        private static readonly Guid PlayerId =
            Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly Guid HiveId =
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly DateTimeOffset Now =
            new DateTimeOffset(2026, 7, 23, 10, 0, 0, TimeSpan.Zero);

        [Test]
        public async Task ExistingDailyRoundShapeRemainsValid()
        {
            var store = new MemoryStore();
            var outbox = NewOutbox(store);
            PendingGameMutation mutation = DailyRoundMutation();

            await outbox.SavePreparedAsync(mutation, CancellationToken.None);
            PendingGameMutation restored = await outbox.TryLoadAsync(
                PlayerId,
                HiveId,
                mutation.Contract,
                mutation.Path,
                CancellationToken.None);

            Assert.That(restored, Is.Not.Null);
            Assert.That(restored.ExpectedDayUtc, Is.EqualTo("2026-07-23"));
            Assert.That(restored.PayloadToken, Is.Empty);
        }

        [Test]
        public async Task CareMutationBindsPayloadWithoutInventingADailyDay()
        {
            var store = new MemoryStore();
            var outbox = NewOutbox(store);
            PendingGameMutation mutation = CareMutation("feeding");

            await outbox.SavePreparedAsync(mutation, CancellationToken.None);
            PendingGameMutation restored = await outbox.TryLoadAsync(
                PlayerId,
                HiveId,
                mutation.Contract,
                mutation.Path,
                CancellationToken.None);

            Assert.That(restored, Is.Not.Null);
            Assert.That(restored.ExpectedDayUtc, Is.Empty);
            Assert.That(restored.PayloadToken, Is.EqualTo("feeding"));
            Assert.That(restored.PayloadSha256, Is.Not.Empty);
        }

        [Test]
        public void MutationWithoutDayOrPayloadFailsClosed()
        {
            PendingGameMutation mutation = CareMutation(string.Empty);

            Assert.ThrowsAsync<ArgumentException>(
                async () => await NewOutbox(new MemoryStore()).SavePreparedAsync(
                    mutation,
                    CancellationToken.None));
        }

        [Test]
        public async Task PayloadTamperingQuarantinesTheDocument()
        {
            var store = new MemoryStore();
            var outbox = NewOutbox(store);
            PendingGameMutation mutation = CareMutation("feeding");
            await outbox.SavePreparedAsync(mutation, CancellationToken.None);
            store.Value = store.Value.Replace(
                "\"payloadToken\":\"feeding\"",
                "\"payloadToken\":\"stabilization\"");

            PendingGameMutation restored = await outbox.TryLoadAsync(
                PlayerId,
                HiveId,
                mutation.Contract,
                mutation.Path,
                CancellationToken.None);

            Assert.That(restored, Is.Null);
            Assert.That(outbox.LastLoadDetectedCorruption, Is.True);
            Assert.That(store.Value, Is.Null);
        }

        [Test]
        public async Task ContractListingAndDeletionRemainPlayerAndHiveScoped()
        {
            var store = new MemoryStore();
            var outbox = NewOutbox(store);
            PendingGameMutation feeding = CareMutation("feeding");
            PendingGameMutation complete = CareMutation(
                "11111111111111111111111111111111");
            complete.Path = "/game/v1/hives/" + HiveId.ToString("D") +
                "/brood/vitality/care/11111111-1111-1111-1111-111111111111/complete";
            complete.IdempotencyKey = "brood-care-complete-proof-key";
            await outbox.SavePreparedAsync(feeding, CancellationToken.None);
            await outbox.SavePreparedAsync(complete, CancellationToken.None);

            var entries = await outbox.ListAsync(
                PlayerId,
                HiveId,
                feeding.Contract,
                CancellationToken.None);

            Assert.That(entries.Count, Is.EqualTo(2));
            Assert.That(entries[0].PayloadToken, Is.EqualTo("feeding"));
            Assert.That(
                entries[1].PayloadToken,
                Is.EqualTo("11111111111111111111111111111111"));

            await outbox.DeleteContractAsync(
                PlayerId,
                HiveId,
                feeding.Contract,
                CancellationToken.None);
            Assert.That(
                await outbox.ListAsync(
                    PlayerId,
                    HiveId,
                    feeding.Contract,
                    CancellationToken.None),
                Is.Empty);
        }

        private static ProtectedGameMutationOutbox NewOutbox(MemoryStore store)
        {
            return new ProtectedGameMutationOutbox(
                store,
                new SystemTextGameJsonCodec(),
                new FixedClock());
        }

        private static PendingGameMutation DailyRoundMutation()
        {
            return new PendingGameMutation
            {
                PlayerId = PlayerId,
                HiveId = HiveId,
                Contract = HiveDailyRoundClient.ContractVersion,
                Path = HiveDailyRoundClient.ClaimPath(HiveId),
                Method = "POST",
                ExpectedDayUtc = "2026-07-23",
                ExpectedRevision = 4,
                IdempotencyKey = "daily-round-existing-shape",
                CreatedAtUtc = Now
            };
        }

        private static PendingGameMutation CareMutation(string type)
        {
            return new PendingGameMutation
            {
                PlayerId = PlayerId,
                HiveId = HiveId,
                Contract = "living-hive-brood-care-v1",
                Path = "/game/v1/hives/" + HiveId.ToString("D") +
                    "/brood/vitality/care/start?type=" + type,
                Method = "POST",
                PayloadToken = type,
                ExpectedRevision = 8,
                IdempotencyKey = "brood-care-proof-key",
                CreatedAtUtc = Now
            };
        }

        private sealed class FixedClock : IMobileAccountSessionClock
        {
            public DateTimeOffset UtcNow => Now;
        }

        private sealed class MemoryStore : IProtectedGameMutationOutboxStore
        {
            public bool IsProtectionAvailable => true;
            public string Value { get; set; }

            public Task<string> LoadAsync(CancellationToken cancellationToken)
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

            public Task DeleteAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Value = null;
                return Task.CompletedTask;
            }
        }
    }
}
