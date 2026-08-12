using System;
using System.Collections.Generic;
using System.Text;

namespace BeeKingdom.Core.Save
{
    public sealed class SaveDeserializer
    {
        private const string Header = "BEE_SAVE_V1";

        public bool TryDeserialize(string data, out SaveSnapshot snapshot)
        {
            snapshot = null;
            if (string.IsNullOrEmpty(data))
            {
                return false;
            }

            string[] lines = data.Replace("\r\n", "\n").Split('\n');
            if (lines.Length < 7 || lines[0] != Header)
            {
                return false;
            }

            Dictionary<string, string> values = new Dictionary<string, string>();
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                int separator = line.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                values[line.Substring(0, separator)] = line.Substring(separator + 1);
            }

            if (!values.TryGetValue("SaveVersion", out string saveVersionText) ||
                !values.TryGetValue("GameVersion", out string gameVersion) ||
                !values.TryGetValue("CreatedAt", out string createdAtText) ||
                !values.TryGetValue("LastModified", out string lastModifiedText) ||
                !values.TryGetValue("Checksum", out string checksum) ||
                !values.TryGetValue("Payload", out string payloadText))
            {
                return false;
            }

            if (!int.TryParse(saveVersionText, out int saveVersion) ||
                !DateTime.TryParse(createdAtText, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime createdAt) ||
                !DateTime.TryParse(lastModifiedText, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime lastModified))
            {
                return false;
            }

            try
            {
                string payload = Encoding.UTF8.GetString(Convert.FromBase64String(payloadText));
                snapshot = new SaveSnapshot(saveVersion, gameVersion, createdAt.ToUniversalTime(), lastModified.ToUniversalTime(), checksum, payload);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
