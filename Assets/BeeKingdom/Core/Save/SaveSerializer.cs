using System;
using System.Text;

namespace BeeKingdom.Core.Save
{
    public sealed class SaveSerializer
    {
        private const string Header = "BEE_SAVE_V1";

        public string Serialize(SaveSnapshot snapshot)
        {
            SaveSnapshot normalized = EnsureChecksum(snapshot);
            string payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(normalized.Payload));

            StringBuilder builder = new StringBuilder(256 + payload.Length);
            builder.AppendLine(Header);
            builder.AppendLine("SaveVersion=" + normalized.SaveVersion);
            builder.AppendLine("GameVersion=" + normalized.GameVersion);
            builder.AppendLine("CreatedAt=" + normalized.CreatedAtUtc.ToString("o"));
            builder.AppendLine("LastModified=" + normalized.LastModifiedUtc.ToString("o"));
            builder.AppendLine("Checksum=" + normalized.Checksum);
            builder.Append("Payload=");
            builder.Append(payload);
            return builder.ToString();
        }

        public SaveSnapshot EnsureChecksum(SaveSnapshot snapshot)
        {
            if (!string.IsNullOrEmpty(snapshot.Checksum))
            {
                return snapshot;
            }

            return snapshot.WithChecksum(SaveChecksum.Calculate(snapshot));
        }
    }
}
