using System.Security.Cryptography;
using System.Text;

namespace BeeKingdom.Core.Save
{
    public static class SaveChecksum
    {
        public static string Calculate(SaveSnapshot snapshot)
        {
            string content = snapshot.SaveVersion.ToString() + "|" +
                snapshot.GameVersion + "|" +
                snapshot.CreatedAtUtc.ToString("o") + "|" +
                snapshot.LastModifiedUtc.ToString("o") + "|" +
                snapshot.Payload;

            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(content);
                byte[] hash = sha.ComputeHash(bytes);
                StringBuilder builder = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }
    }
}
