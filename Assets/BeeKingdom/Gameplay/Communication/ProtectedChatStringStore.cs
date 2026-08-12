using System;

namespace BeeKingdom.Gameplay.Communication
{
    public interface IChatDataProtector
    {
        string Protect(string purpose, string plaintext);
        string Unprotect(string purpose, string protectedValue);
    }

    public sealed class ChatProtectedStoreException : Exception
    {
        public ChatProtectedStoreException(string message, Exception innerException = null) : base(message, innerException) { }
    }

    public sealed class ProtectedChatStringStore : IChatStringStore
    {
        private readonly IChatStringStore inner;
        private readonly IChatDataProtector protector;
        private readonly string purposePrefix;

        public ProtectedChatStringStore(IChatStringStore inner, IChatDataProtector protector, string purposePrefix = "BeeKingdom.Chat.Storage.v1")
        {
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
            this.protector = protector ?? throw new ArgumentNullException(nameof(protector));
            this.purposePrefix = string.IsNullOrWhiteSpace(purposePrefix) ? throw new ArgumentException("Protection purpose is required.", nameof(purposePrefix)) : purposePrefix;
        }

        public string Read(string key)
        {
            string value = inner.Read(key);
            if (string.IsNullOrEmpty(value)) return value;
            try
            {
                string plaintext = protector.Unprotect(Purpose(key), value);
                if (plaintext == null) throw new InvalidOperationException("Protector returned no plaintext.");
                return plaintext;
            }
            catch (Exception exception) { throw new ChatProtectedStoreException("Protected chat data could not be authenticated or decrypted; the original value was preserved.", exception); }
        }

        public void Write(string key, string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            try
            {
                string protectedValue = protector.Protect(Purpose(key), value);
                if (string.IsNullOrWhiteSpace(protectedValue) || string.Equals(protectedValue, value, StringComparison.Ordinal)) throw new InvalidOperationException("Protector did not produce a protected envelope.");
                inner.Write(key, protectedValue);
            }
            catch (ChatProtectedStoreException) { throw; }
            catch (Exception exception) { throw new ChatProtectedStoreException("Chat data could not be protected and was not written.", exception); }
        }

        public void Delete(string key) => inner.Delete(key);
        private string Purpose(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Storage key is required.", nameof(key));
            return purposePrefix + ":" + key;
        }
    }
}
