using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Networking
{
    public interface IProtectedGameMutationOutboxStore
    {
        bool IsProtectionAvailable { get; }
        Task<string> LoadAsync(CancellationToken cancellationToken);
        Task SaveAsync(string protectedPlaintext, CancellationToken cancellationToken);
        Task DeleteAsync(CancellationToken cancellationToken);
    }

    public sealed class PendingGameMutation
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public string Contract { get; set; }
        public string Path { get; set; }
        public string Method { get; set; }
        public string ExpectedDayUtc { get; set; }
        public string PayloadToken { get; set; }
        public long ExpectedRevision { get; set; }
        public string IdempotencyKey { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public string PayloadSha256 { get; set; }
    }

    public sealed class ProtectedGameMutationOutbox
    {
        public const int CurrentVersion = 1;
        public const int MaxEntries = 8;
        public const int MaxDocumentBytes = 64 * 1024;
        public static readonly TimeSpan MaxRetention = TimeSpan.FromDays(2);

        private readonly IProtectedGameMutationOutboxStore store;
        private readonly IGameJsonCodec codec;
        private readonly IMobileAccountSessionClock clock;
        private readonly SemaphoreSlim lifecycle = new SemaphoreSlim(1, 1);

        public ProtectedGameMutationOutbox(
            IProtectedGameMutationOutboxStore store,
            IGameJsonCodec codec,
            IMobileAccountSessionClock clock = null)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.codec = codec ?? throw new ArgumentNullException(nameof(codec));
            this.clock = clock ?? new SystemMobileAccountSessionClock();
        }

        public bool IsProtectionAvailable => store.IsProtectionAvailable;
        public bool LastLoadDetectedCorruption { get; private set; }

        public async Task SavePreparedAsync(
            PendingGameMutation mutation,
            CancellationToken cancellationToken)
        {
            if (!store.IsProtectionAvailable)
                throw new InvalidOperationException("game.mutation.protected_storage_unavailable");
            PendingGameMutation prepared = CopyAndValidate(mutation, clock.UtcNow, requireHash: false);
            prepared.PayloadSha256 = ComputePayloadHash(prepared);

            await lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                OutboxDocument document =
                    await LoadDocumentInsideLockAsync(cancellationToken).ConfigureAwait(false) ??
                    NewDocument();
                DateTimeOffset now = clock.UtcNow;
                document.Entries.RemoveAll(entry =>
                    SamePartition(entry, prepared.PlayerId, prepared.HiveId, prepared.Contract, prepared.Path));
                document.Entries.Add(prepared);
                document.Entries = document.Entries
                    .Where(entry => IsValid(entry, now))
                    .OrderByDescending(entry => entry.CreatedAtUtc)
                    .Take(MaxEntries)
                    .Select(Copy)
                    .ToList();
                await SaveDocumentInsideLockAsync(document, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                lifecycle.Release();
            }
        }

        public async Task<PendingGameMutation> TryLoadAsync(
            Guid playerId,
            Guid hiveId,
            string contract,
            string path,
            CancellationToken cancellationToken)
        {
            ValidatePartition(playerId, hiveId, contract, path);
            if (!store.IsProtectionAvailable) return null;

            await lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                OutboxDocument document =
                    await LoadDocumentInsideLockAsync(cancellationToken).ConfigureAwait(false);
                if (document == null) return null;
                PendingGameMutation match = document.Entries.FirstOrDefault(entry =>
                    SamePartition(entry, playerId, hiveId, contract, path));
                if (match == null) return null;
                if (!IsValid(match, clock.UtcNow))
                {
                    document.Entries.Remove(match);
                    await SaveOrDeleteInsideLockAsync(document, cancellationToken).ConfigureAwait(false);
                    return null;
                }
                return Copy(match);
            }
            finally
            {
                lifecycle.Release();
            }
        }

        public async Task<IReadOnlyList<PendingGameMutation>> ListAsync(
            Guid playerId,
            Guid hiveId,
            string contract,
            CancellationToken cancellationToken)
        {
            ValidateContractPartition(playerId, hiveId, contract);
            if (!store.IsProtectionAvailable)
                return Array.Empty<PendingGameMutation>();

            await lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                OutboxDocument document =
                    await LoadDocumentInsideLockAsync(cancellationToken).ConfigureAwait(false);
                if (document == null)
                    return Array.Empty<PendingGameMutation>();
                return document.Entries
                    .Where(entry =>
                        entry != null &&
                        entry.PlayerId == playerId &&
                        entry.HiveId == hiveId &&
                        string.Equals(
                            entry.Contract,
                            contract,
                            StringComparison.Ordinal))
                    .OrderBy(entry => entry.CreatedAtUtc)
                    .Select(Copy)
                    .ToArray();
            }
            finally
            {
                lifecycle.Release();
            }
        }

        public async Task DeletePartitionAsync(
            Guid playerId,
            Guid hiveId,
            string contract,
            string path,
            CancellationToken cancellationToken)
        {
            ValidatePartition(playerId, hiveId, contract, path);
            if (!store.IsProtectionAvailable) return;
            await lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                OutboxDocument document =
                    await LoadDocumentInsideLockAsync(cancellationToken).ConfigureAwait(false);
                if (document == null) return;
                document.Entries.RemoveAll(entry =>
                    SamePartition(entry, playerId, hiveId, contract, path));
                await SaveOrDeleteInsideLockAsync(document, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                lifecycle.Release();
            }
        }

        public async Task DeletePlayerAsync(Guid playerId, CancellationToken cancellationToken)
        {
            if (playerId == Guid.Empty) throw new ArgumentException("A player identifier is required.", nameof(playerId));
            if (!store.IsProtectionAvailable) return;
            await lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                OutboxDocument document =
                    await LoadDocumentInsideLockAsync(cancellationToken).ConfigureAwait(false);
                if (document == null) return;
                document.Entries.RemoveAll(entry => entry != null && entry.PlayerId == playerId);
                await SaveOrDeleteInsideLockAsync(document, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                lifecycle.Release();
            }
        }

        public async Task DeleteContractAsync(
            Guid playerId,
            Guid hiveId,
            string contract,
            CancellationToken cancellationToken)
        {
            ValidateContractPartition(playerId, hiveId, contract);
            if (!store.IsProtectionAvailable) return;
            await lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                OutboxDocument document =
                    await LoadDocumentInsideLockAsync(cancellationToken).ConfigureAwait(false);
                if (document == null) return;
                document.Entries.RemoveAll(entry =>
                    entry != null &&
                    entry.PlayerId == playerId &&
                    entry.HiveId == hiveId &&
                    string.Equals(
                        entry.Contract,
                        contract,
                        StringComparison.Ordinal));
                await SaveOrDeleteInsideLockAsync(
                    document,
                    cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                lifecycle.Release();
            }
        }

        public IReadOnlyList<string> ProofRows()
        {
            return new[]
            {
                "game_mutation_outbox_version:" + CurrentVersion,
                "game_mutation_outbox_protected_store:" +
                    store.IsProtectionAvailable.ToString().ToLowerInvariant(),
                "game_mutation_outbox_partition:player+hive+contract+path",
                "game_mutation_outbox_max_entries:" + MaxEntries,
                "game_mutation_outbox_retention_hours:" +
                    MaxRetention.TotalHours.ToString("0", CultureInfo.InvariantCulture),
                "game_mutation_outbox_access_token_stored:false",
                "game_mutation_outbox_auto_submit:false",
                "game_mutation_outbox_payload_binding:expected_day_or_payload_token"
            };
        }

        private async Task<OutboxDocument> LoadDocumentInsideLockAsync(
            CancellationToken cancellationToken)
        {
            LastLoadDetectedCorruption = false;
            string serialized;
            try
            {
                serialized = await store.LoadAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                LastLoadDetectedCorruption = true;
                return null;
            }
            if (string.IsNullOrWhiteSpace(serialized)) return null;
            if (Encoding.UTF8.GetByteCount(serialized) > MaxDocumentBytes)
                return await QuarantineByDeletionInsideLockAsync().ConfigureAwait(false);
            try
            {
                OutboxDocument document = codec.Deserialize<OutboxDocument>(serialized);
                if (document.Version != CurrentVersion || document.Entries == null ||
                    document.Entries.Count > MaxEntries ||
                    document.Entries.Any(entry => !IsValid(entry, clock.UtcNow)))
                    throw new InvalidOperationException("Unsupported protected mutation outbox.");
                return document;
            }
            catch
            {
                return await QuarantineByDeletionInsideLockAsync().ConfigureAwait(false);
            }
        }

        private async Task<OutboxDocument> QuarantineByDeletionInsideLockAsync()
        {
            LastLoadDetectedCorruption = true;
            try
            {
                await store.DeleteAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
            }
            return null;
        }

        private async Task SaveOrDeleteInsideLockAsync(
            OutboxDocument document,
            CancellationToken cancellationToken)
        {
            if (document == null || document.Entries == null || document.Entries.Count == 0)
            {
                await store.DeleteAsync(cancellationToken).ConfigureAwait(false);
                return;
            }
            await SaveDocumentInsideLockAsync(document, cancellationToken).ConfigureAwait(false);
        }

        private async Task SaveDocumentInsideLockAsync(
            OutboxDocument document,
            CancellationToken cancellationToken)
        {
            string serialized = codec.Serialize(document);
            if (Encoding.UTF8.GetByteCount(serialized) > MaxDocumentBytes)
                throw new InvalidOperationException("The protected mutation outbox is too large.");
            await store.SaveAsync(serialized, cancellationToken).ConfigureAwait(false);
        }

        private static OutboxDocument NewDocument()
        {
            return new OutboxDocument
            {
                Version = CurrentVersion,
                Entries = new List<PendingGameMutation>()
            };
        }

        private static PendingGameMutation CopyAndValidate(
            PendingGameMutation mutation,
            DateTimeOffset now,
            bool requireHash)
        {
            PendingGameMutation copy = Copy(mutation);
            if (!IsStructurallyValid(copy, now) ||
                requireHash && !string.Equals(
                    copy.PayloadSha256,
                    ComputePayloadHash(copy),
                    StringComparison.Ordinal))
                throw new ArgumentException("The pending game mutation is invalid.", nameof(mutation));
            return copy;
        }

        private static PendingGameMutation Copy(PendingGameMutation source)
        {
            if (source == null) return null;
            return new PendingGameMutation
            {
                PlayerId = source.PlayerId,
                HiveId = source.HiveId,
                Contract = source.Contract ?? string.Empty,
                Path = source.Path ?? string.Empty,
                Method = source.Method ?? string.Empty,
                ExpectedDayUtc = source.ExpectedDayUtc ?? string.Empty,
                PayloadToken = source.PayloadToken ?? string.Empty,
                ExpectedRevision = source.ExpectedRevision,
                IdempotencyKey = source.IdempotencyKey ?? string.Empty,
                CreatedAtUtc = source.CreatedAtUtc,
                PayloadSha256 = source.PayloadSha256 ?? string.Empty
            };
        }

        private static bool IsValid(PendingGameMutation mutation, DateTimeOffset now)
        {
            return IsStructurallyValid(mutation, now) &&
                string.Equals(
                    mutation.PayloadSha256,
                    ComputePayloadHash(mutation),
                    StringComparison.Ordinal);
        }

        private static bool IsStructurallyValid(PendingGameMutation mutation, DateTimeOffset now)
        {
            if (mutation == null) return false;
            DateTime parsedDay;
            bool hasCanonicalDay = DateTime.TryParseExact(
                mutation.ExpectedDayUtc,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out parsedDay);
            bool hasPayloadToken = IsSafeToken(mutation.PayloadToken, 256);
            return mutation.PlayerId != Guid.Empty &&
                mutation.HiveId != Guid.Empty &&
                IsSafeToken(mutation.Contract, 128) &&
                !string.IsNullOrWhiteSpace(mutation.Path) &&
                mutation.Path.Length <= 512 &&
                mutation.Path.StartsWith("/game/v1/", StringComparison.Ordinal) &&
                string.Equals(mutation.Method, "POST", StringComparison.Ordinal) &&
                (hasCanonicalDay || hasPayloadToken) &&
                (string.IsNullOrEmpty(mutation.ExpectedDayUtc) || hasCanonicalDay) &&
                (string.IsNullOrEmpty(mutation.PayloadToken) || hasPayloadToken) &&
                mutation.ExpectedRevision >= 0 &&
                IsSafeIdempotencyKey(mutation.IdempotencyKey) &&
                mutation.CreatedAtUtc != default(DateTimeOffset) &&
                mutation.CreatedAtUtc.Offset == TimeSpan.Zero &&
                mutation.CreatedAtUtc <= now.AddMinutes(5) &&
                now - mutation.CreatedAtUtc <= MaxRetention;
        }

        private static bool SamePartition(
            PendingGameMutation mutation,
            Guid playerId,
            Guid hiveId,
            string contract,
            string path)
        {
            return mutation != null &&
                mutation.PlayerId == playerId &&
                mutation.HiveId == hiveId &&
                string.Equals(mutation.Contract, contract, StringComparison.Ordinal) &&
                string.Equals(mutation.Path, path, StringComparison.Ordinal);
        }

        private static void ValidatePartition(
            Guid playerId,
            Guid hiveId,
            string contract,
            string path)
        {
            if (playerId == Guid.Empty || hiveId == Guid.Empty ||
                !IsSafeToken(contract, 128) ||
                string.IsNullOrWhiteSpace(path) ||
                path.Length > 512 ||
                !path.StartsWith("/game/v1/", StringComparison.Ordinal))
                throw new ArgumentException("The mutation outbox partition is invalid.");
        }

        private static void ValidateContractPartition(
            Guid playerId,
            Guid hiveId,
            string contract)
        {
            if (playerId == Guid.Empty ||
                hiveId == Guid.Empty ||
                !IsSafeToken(contract, 128))
                throw new ArgumentException(
                    "The mutation outbox contract partition is invalid.");
        }

        private static bool IsSafeToken(string value, int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                value.Trim() != value ||
                value.Length > maximumLength)
                return false;
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (!((character >= 'a' && character <= 'z') ||
                    (character >= '0' && character <= '9') ||
                    character == '.' ||
                    character == '_' ||
                    character == '-'))
                    return false;
            }
            return true;
        }

        private static bool IsSafeIdempotencyKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                value.Trim() != value ||
                value.Length > 256)
                return false;
            return value.All(character =>
                character >= 0x21 && character <= 0x7e && character != '\"' && character != '\\');
        }

        private static string ComputePayloadHash(PendingGameMutation mutation)
        {
            string canonical = string.Join(
                "|",
                mutation.PlayerId.ToString("D"),
                mutation.HiveId.ToString("D"),
                mutation.Contract ?? string.Empty,
                mutation.Path ?? string.Empty,
                mutation.Method ?? string.Empty,
                mutation.ExpectedDayUtc ?? string.Empty,
                mutation.ExpectedRevision.ToString(CultureInfo.InvariantCulture),
                mutation.IdempotencyKey ?? string.Empty);
            if (!string.IsNullOrEmpty(mutation.PayloadToken))
                canonical += "|" + mutation.PayloadToken;
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical));
                var builder = new StringBuilder(bytes.Length * 2);
                foreach (byte item in bytes) builder.Append(item.ToString("x2"));
                return builder.ToString();
            }
        }

        public sealed class OutboxDocument
        {
            public int Version { get; set; }
            public List<PendingGameMutation> Entries { get; set; }
        }
    }
}
