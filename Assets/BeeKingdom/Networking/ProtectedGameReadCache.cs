using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Networking
{
    public interface IProtectedGameReadCacheStore
    {
        bool IsProtectionAvailable { get; }
        Task<string> LoadAsync(CancellationToken cancellationToken);
        Task SaveAsync(string protectedPlaintext, CancellationToken cancellationToken);
        Task DeleteAsync(CancellationToken cancellationToken);
    }

    public sealed class ProtectedGameReadCacheHit<T>
    {
        public ProtectedGameReadCacheHit(T value, DateTimeOffset cachedAtUtc)
        {
            Value = value;
            CachedAtUtc = cachedAtUtc;
        }

        public T Value { get; }
        public DateTimeOffset CachedAtUtc { get; }
    }

    public sealed class ProtectedGameReadCache
    {
        public const int CurrentVersion = 1;
        public const int MaxEntries = 12;
        public const int MaxPayloadBytes = 512 * 1024;
        public const int MaxDocumentBytes = 1024 * 1024;
        public static readonly TimeSpan MaxRetention = TimeSpan.FromDays(7);

        private readonly IProtectedGameReadCacheStore store;
        private readonly IGameJsonCodec codec;
        private readonly IMobileAccountSessionClock clock;
        private readonly SemaphoreSlim lifecycle = new SemaphoreSlim(1, 1);

        public ProtectedGameReadCache(
            IProtectedGameReadCacheStore store,
            IGameJsonCodec codec,
            IMobileAccountSessionClock clock = null)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.codec = codec ?? throw new ArgumentNullException(nameof(codec));
            this.clock = clock ?? new SystemMobileAccountSessionClock();
        }

        public bool IsProtectionAvailable => store.IsProtectionAvailable;
        public bool LastLoadDetectedCorruption { get; private set; }

        public async Task SaveValidatedReadAsync<T>(
            Guid playerId,
            Guid hiveId,
            string contract,
            string path,
            T value,
            CancellationToken cancellationToken)
        {
            ValidatePartition(playerId, hiveId, contract, path);
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (!store.IsProtectionAvailable) return;

            string payload = codec.Serialize(value);
            if (Encoding.UTF8.GetByteCount(payload) > MaxPayloadBytes)
                throw new InvalidOperationException("The validated game read is too large to cache.");

            await lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                CacheDocument document = await LoadDocumentInsideLockAsync(cancellationToken).ConfigureAwait(false) ?? NewDocument();
                DateTimeOffset now = clock.UtcNow;
                document.entries.RemoveAll(entry => SamePartition(entry, playerId, hiveId, contract, path));
                document.entries.Add(new CacheEntry
                {
                    playerId = playerId,
                    hiveId = hiveId,
                    contract = contract,
                    path = path,
                    cachedAtUtc = now,
                    payloadJson = payload,
                    payloadSha256 = Hash(payload)
                });
                document.entries = document.entries
                    .Where(entry => IsStructurallyValid(entry, now))
                    .OrderByDescending(entry => entry.cachedAtUtc)
                    .Take(MaxEntries)
                    .ToList();

                string serialized = codec.Serialize(document);
                if (Encoding.UTF8.GetByteCount(serialized) > MaxDocumentBytes)
                {
                    while (document.entries.Count > 1 && Encoding.UTF8.GetByteCount(serialized) > MaxDocumentBytes)
                    {
                        document.entries.RemoveAt(document.entries.Count - 1);
                        serialized = codec.Serialize(document);
                    }
                }
                if (Encoding.UTF8.GetByteCount(serialized) > MaxDocumentBytes)
                    throw new InvalidOperationException("The protected game read cache is too large.");
                await store.SaveAsync(serialized, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                lifecycle.Release();
            }
        }

        public async Task<ProtectedGameReadCacheHit<T>> TryLoadAsync<T>(
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
                CacheDocument document = await LoadDocumentInsideLockAsync(cancellationToken).ConfigureAwait(false);
                if (document == null) return null;
                CacheEntry entry = document.entries.FirstOrDefault(candidate =>
                    SamePartition(candidate, playerId, hiveId, contract, path));
                if (entry == null || !IsStructurallyValid(entry, clock.UtcNow) ||
                    !string.Equals(entry.payloadSha256, Hash(entry.payloadJson), StringComparison.Ordinal))
                {
                    if (entry != null)
                    {
                        LastLoadDetectedCorruption = true;
                        await DeleteInsideLockBestEffortAsync().ConfigureAwait(false);
                    }
                    return null;
                }

                try
                {
                    return new ProtectedGameReadCacheHit<T>(codec.Deserialize<T>(entry.payloadJson), entry.cachedAtUtc);
                }
                catch
                {
                    LastLoadDetectedCorruption = true;
                    await DeleteInsideLockBestEffortAsync().ConfigureAwait(false);
                    return null;
                }
            }
            finally
            {
                lifecycle.Release();
            }
        }

        public async Task DeleteAsync(CancellationToken cancellationToken)
        {
            await lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await store.DeleteAsync(cancellationToken).ConfigureAwait(false);
                LastLoadDetectedCorruption = false;
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
                "game_read_cache_version:" + CurrentVersion,
                "game_read_cache_protected_store:" + store.IsProtectionAvailable.ToString().ToLowerInvariant(),
                "game_read_cache_partition:player+hive+contract+path",
                "game_read_cache_max_entries:" + MaxEntries,
                "game_read_cache_max_payload_bytes:" + MaxPayloadBytes,
                "game_read_cache_retention_days:" + MaxRetention.TotalDays.ToString("0", System.Globalization.CultureInfo.InvariantCulture),
                "game_read_cache_get_only:true",
                "game_read_cache_offline_mutations:false",
                "game_read_cache_rewards_authoritative:false"
            };
        }

        private async Task<CacheDocument> LoadDocumentInsideLockAsync(CancellationToken cancellationToken)
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
            {
                LastLoadDetectedCorruption = true;
                await DeleteInsideLockBestEffortAsync().ConfigureAwait(false);
                return null;
            }

            try
            {
                CacheDocument document = codec.Deserialize<CacheDocument>(serialized);
                if (document.version != CurrentVersion || document.entries == null || document.entries.Count > MaxEntries)
                    throw new InvalidOperationException("Unsupported protected game cache document.");
                return document;
            }
            catch
            {
                LastLoadDetectedCorruption = true;
                await DeleteInsideLockBestEffortAsync().ConfigureAwait(false);
                return null;
            }
        }

        private async Task DeleteInsideLockBestEffortAsync()
        {
            try
            {
                await store.DeleteAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
            }
        }

        private static CacheDocument NewDocument()
        {
            return new CacheDocument { version = CurrentVersion, entries = new List<CacheEntry>() };
        }

        private static bool SamePartition(CacheEntry entry, Guid playerId, Guid hiveId, string contract, string path)
        {
            return entry != null && entry.playerId == playerId && entry.hiveId == hiveId &&
                string.Equals(entry.contract, contract, StringComparison.Ordinal) &&
                string.Equals(entry.path, path, StringComparison.Ordinal);
        }

        private static bool IsStructurallyValid(CacheEntry entry, DateTimeOffset now)
        {
            return entry != null && entry.playerId != Guid.Empty && entry.hiveId != Guid.Empty &&
                !string.IsNullOrWhiteSpace(entry.contract) && entry.contract.Length <= 128 &&
                !string.IsNullOrWhiteSpace(entry.path) && entry.path.Length <= 512 &&
                entry.path.StartsWith("/game/v1/", StringComparison.Ordinal) &&
                entry.cachedAtUtc != default(DateTimeOffset) && entry.cachedAtUtc.Offset == TimeSpan.Zero &&
                entry.cachedAtUtc <= now.AddMinutes(5) && now - entry.cachedAtUtc <= MaxRetention &&
                !string.IsNullOrWhiteSpace(entry.payloadJson) &&
                Encoding.UTF8.GetByteCount(entry.payloadJson) <= MaxPayloadBytes &&
                !string.IsNullOrWhiteSpace(entry.payloadSha256) && entry.payloadSha256.Length == 64;
        }

        private static void ValidatePartition(Guid playerId, Guid hiveId, string contract, string path)
        {
            if (playerId == Guid.Empty) throw new ArgumentException("A player identifier is required.", nameof(playerId));
            if (hiveId == Guid.Empty) throw new ArgumentException("A hive identifier is required.", nameof(hiveId));
            if (string.IsNullOrWhiteSpace(contract) || contract.Length > 128)
                throw new ArgumentException("A bounded game contract is required.", nameof(contract));
            if (string.IsNullOrWhiteSpace(path) || path.Length > 512 || !path.StartsWith("/game/v1/", StringComparison.Ordinal))
                throw new ArgumentException("A bounded game read path is required.", nameof(path));
        }

        private static string Hash(string value)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                var builder = new StringBuilder(bytes.Length * 2);
                foreach (byte item in bytes) builder.Append(item.ToString("x2"));
                return builder.ToString();
            }
        }

        public sealed class CacheDocument
        {
            public int version { get; set; }
            public List<CacheEntry> entries { get; set; }
        }

        public sealed class CacheEntry
        {
            public Guid playerId { get; set; }
            public Guid hiveId { get; set; }
            public string contract { get; set; }
            public string path { get; set; }
            public DateTimeOffset cachedAtUtc { get; set; }
            public string payloadJson { get; set; }
            public string payloadSha256 { get; set; }
        }
    }
}
