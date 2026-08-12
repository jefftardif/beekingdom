using System;

namespace BeeKingdom.Core.Save
{
    public sealed class SaveSnapshot
    {
        public int SaveVersion { get; }
        public string GameVersion { get; }
        public DateTime CreatedAtUtc { get; }
        public DateTime LastModifiedUtc { get; }
        public string Checksum { get; }
        public string Payload { get; }

        public SaveSnapshot(
            int saveVersion,
            string gameVersion,
            DateTime createdAtUtc,
            DateTime lastModifiedUtc,
            string checksum,
            string payload)
        {
            SaveVersion = saveVersion;
            GameVersion = gameVersion ?? string.Empty;
            CreatedAtUtc = createdAtUtc;
            LastModifiedUtc = lastModifiedUtc;
            Checksum = checksum ?? string.Empty;
            Payload = payload ?? string.Empty;
        }

        public SaveSnapshot WithChecksum(string checksum)
        {
            return new SaveSnapshot(SaveVersion, GameVersion, CreatedAtUtc, LastModifiedUtc, checksum, Payload);
        }

        public SaveSnapshot WithVersion(int saveVersion)
        {
            return new SaveSnapshot(saveVersion, GameVersion, CreatedAtUtc, DateTime.UtcNow, string.Empty, Payload);
        }

        public SaveSnapshot Touch()
        {
            return new SaveSnapshot(SaveVersion, GameVersion, CreatedAtUtc, DateTime.UtcNow, string.Empty, Payload);
        }
    }
}
