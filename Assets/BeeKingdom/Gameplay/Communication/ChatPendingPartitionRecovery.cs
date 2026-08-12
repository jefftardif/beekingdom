using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Gameplay.Communication
{
    public sealed class ChatPersistenceGate
    {
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        public async Task<IDisposable> EnterAsync(CancellationToken cancellationToken) { await semaphore.WaitAsync(cancellationToken); return new Lease(semaphore); }
        public IDisposable Enter() { semaphore.Wait(); return new Lease(semaphore); }
        private sealed class Lease : IDisposable
        {
            private SemaphoreSlim semaphore;
            public Lease(SemaphoreSlim semaphore) { this.semaphore = semaphore; }
            public void Dispose() { Interlocked.Exchange(ref semaphore, null)?.Release(); }
        }
    }

    public sealed class ChatPendingRecoveryReceipt
    {
        public string RecoveryId { get; set; }
        public int EntryFiles { get; set; }
        public bool SourceCleared { get; set; }
        public bool BackupRetained { get; set; }
    }

    public sealed class ChatPendingRecoveryException : Exception
    {
        public bool SourcePreserved { get; }
        public bool BackupRetained { get; }
        public ChatPendingRecoveryException(string message, bool sourcePreserved, bool backupRetained, Exception innerException = null) : base(message, innerException)
        { SourcePreserved = sourcePreserved; BackupRetained = backupRetained; }
    }

    public sealed class ChatPendingPartitionRecovery
    {
        private static readonly string[] Suffixes = { "PendingConversations.v1", "PendingSends.v1", "PendingReads.v1", "PendingReports.v1" };
        private readonly IChatStringStore rawStore;
        private readonly string partitionPrefix;
        private readonly ChatPersistenceGate persistenceGate;

        public ChatPendingPartitionRecovery(IChatStringStore rawStore, string partitionPrefix, ChatPersistenceGate persistenceGate = null)
        {
            this.rawStore = rawStore ?? throw new ArgumentNullException(nameof(rawStore));
            this.partitionPrefix = string.IsNullOrWhiteSpace(partitionPrefix) ? throw new ArgumentException("A partition prefix is required.", nameof(partitionPrefix)) : partitionPrefix.Trim();
            this.persistenceGate = persistenceGate ?? new ChatPersistenceGate();
        }

        public ChatPendingRecoveryReceipt QuarantineAndReset(string recoveryId)
        { using (persistenceGate.Enter()) return QuarantineAndResetCore(recoveryId); }

        private ChatPendingRecoveryReceipt QuarantineAndResetCore(string recoveryId)
        {
            string normalized = NormalizeRecoveryId(recoveryId);
            var values = ReadSources();
            var copied = new List<string>();
            try
            {
                foreach (KeyValuePair<string, string> item in values)
                {
                    string backupKey = BackupKey(normalized, item.Key);
                    rawStore.Write(backupKey, item.Value);
                    if (!string.Equals(rawStore.Read(backupKey), item.Value, StringComparison.Ordinal)) throw new InvalidOperationException("Quarantine verification failed.");
                    copied.Add(backupKey);
                }
            }
            catch (Exception exception)
            {
                foreach (string key in copied) { try { rawStore.Delete(key); } catch { } }
                throw new ChatPendingRecoveryException("Pending chat data could not be copied to quarantine; source data was preserved.", true, copied.Count > 0, exception);
            }

            try
            {
                foreach (string suffix in values.Keys) rawStore.Delete(SourceKey(suffix));
            }
            catch (Exception exception)
            {
                throw new ChatPendingRecoveryException("Pending chat quarantine was retained but the source could not be fully cleared.", false, true, exception);
            }
            return new ChatPendingRecoveryReceipt { RecoveryId = normalized, EntryFiles = values.Count, SourceCleared = true, BackupRetained = values.Count > 0 };
        }

        public ChatPendingRecoveryReceipt Restore(string recoveryId)
        { using (persistenceGate.Enter()) return RestoreCore(recoveryId); }

        private ChatPendingRecoveryReceipt RestoreCore(string recoveryId)
        {
            string normalized = NormalizeRecoveryId(recoveryId);
            var backups = ReadBackups(normalized);
            if (backups.Count == 0) throw new ChatPendingRecoveryException("No pending chat quarantine exists for this recovery id.", true, false);
            foreach (string suffix in backups.Keys)
                if (!string.IsNullOrEmpty(rawStore.Read(SourceKey(suffix)))) throw new ChatPendingRecoveryException("Pending chat data already exists; quarantine restoration refused to overwrite it.", true, true);
            try
            {
                foreach (KeyValuePair<string, string> item in backups)
                {
                    string sourceKey = SourceKey(item.Key);
                    rawStore.Write(sourceKey, item.Value);
                    if (!string.Equals(rawStore.Read(sourceKey), item.Value, StringComparison.Ordinal)) throw new InvalidOperationException("Restoration verification failed.");
                }
                foreach (string suffix in backups.Keys) rawStore.Delete(BackupKey(normalized, suffix));
            }
            catch (Exception exception)
            {
                throw new ChatPendingRecoveryException("Pending chat quarantine restoration was incomplete; backup data was retained where possible.", false, true, exception);
            }
            return new ChatPendingRecoveryReceipt { RecoveryId = normalized, EntryFiles = backups.Count, SourceCleared = false, BackupRetained = false };
        }

        private Dictionary<string, string> ReadSources()
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string suffix in Suffixes) { string value = rawStore.Read(SourceKey(suffix)); if (!string.IsNullOrEmpty(value)) result[suffix] = value; }
            return result;
        }

        private Dictionary<string, string> ReadBackups(string recoveryId)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string suffix in Suffixes) { string value = rawStore.Read(BackupKey(recoveryId, suffix)); if (!string.IsNullOrEmpty(value)) result[suffix] = value; }
            return result;
        }

        private string SourceKey(string suffix) => partitionPrefix + "." + suffix;
        private string BackupKey(string recoveryId, string suffix) => partitionPrefix + ".Recovery.v1." + recoveryId + "." + suffix;
        private static string NormalizeRecoveryId(string value)
        {
            if (!Guid.TryParseExact(value, "N", out Guid parsed)) throw new ArgumentException("Recovery id must be a GUID in N format.", nameof(value));
            return parsed.ToString("N");
        }
    }
}
