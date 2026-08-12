using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Gameplay.Communication;
using BeeKingdom.Networking;
using UnityEngine;

namespace BeeKingdom.Playground
{
    // Adapts the mobile account session (Networking) to the chat client's session contract
    // (Gameplay.Communication), following the same GameAccountSession pattern already used by
    // every other Hive*Client in MobileAccountSessionRuntimeBootstrap.
    public sealed class MobileAccountChatSessionSource : IRefreshableChatSessionSource
    {
        private readonly MobileAccountSessionClient client;

        public MobileAccountChatSessionSource(MobileAccountSessionClient client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public Task<ChatSession> GetSessionAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(client.TryGetSession(out GameAccountSession session) ? Map(session) : null);
        }

        public async Task<ChatSession> RefreshSessionAsync(CancellationToken cancellationToken)
        {
            GameAccountSession refreshed = await client.GetFreshSessionAsync(cancellationToken);
            return Map(refreshed);
        }

        private static ChatSession Map(GameAccountSession session) =>
            session == null ? null : new ChatSession(session.PlayerId.ToString("D"), session.AccessToken);
    }

    public sealed class PlayerPrefsChatStringStore : IChatStringStore
    {
        public string Read(string key) => PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;
        public void Write(string key, string value) { PlayerPrefs.SetString(key, value); PlayerPrefs.Save(); }
        public void Delete(string key) { PlayerPrefs.DeleteKey(key); PlayerPrefs.Save(); }
    }

    // Hardware-backed on Android (AES/GCM via the Android Keystore, mirroring
    // AndroidKeystoreRefreshTokenStore.cs). No OS-level secure storage is wired into this project
    // for other platforms yet, so Editor/Windows/desktop falls back to a locally generated AES key
    // persisted in PlayerPrefs: software obfuscation, not hardware-backed protection. That matches
    // this build's actual security posture today on non-Android targets — harden before release.
    public sealed class LivingHiveChatDataProtector : IChatDataProtector
    {
        private const string KeyAlias = "bee_kingdom_chat_protector_v1";
        private const string FallbackKeyPref = "BeeKingdom.Chat.Protector.SoftwareKey.v1";

        public string Protect(string purpose, string plaintext)
        {
            if (plaintext == null) throw new ArgumentNullException(nameof(plaintext));
#if UNITY_ANDROID && !UNITY_EDITOR
            return ProtectAndroid(plaintext);
#else
            return ProtectSoftware(plaintext);
#endif
        }

        public string Unprotect(string purpose, string protectedValue)
        {
            if (string.IsNullOrEmpty(protectedValue)) throw new ArgumentException("A protected value is required.", nameof(protectedValue));
#if UNITY_ANDROID && !UNITY_EDITOR
            return UnprotectAndroid(protectedValue);
#else
            return UnprotectSoftware(protectedValue);
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static string ProtectAndroid(string plaintext)
        {
            using (AndroidJavaObject key = GetOrCreateSecretKey())
            using (AndroidJavaClass cipherClass = new AndroidJavaClass("javax.crypto.Cipher"))
            using (AndroidJavaObject cipher = cipherClass.CallStatic<AndroidJavaObject>("getInstance", "AES/GCM/NoPadding"))
            {
                cipher.Call("init", 1, key);
                byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
                byte[] encrypted = cipher.Call<byte[]>("doFinal", plaintextBytes);
                byte[] iv = cipher.Call<byte[]>("getIV");
                Array.Clear(plaintextBytes, 0, plaintextBytes.Length);
                return Convert.ToBase64String(iv) + "." + Convert.ToBase64String(encrypted);
            }
        }

        private static string UnprotectAndroid(string protectedValue)
        {
            string[] parts = protectedValue.Split('.');
            if (parts.Length != 2) throw new FormatException("Malformed protected chat value.");
            byte[] iv = Convert.FromBase64String(parts[0]);
            byte[] cipherBytes = Convert.FromBase64String(parts[1]);
            using (AndroidJavaObject key = GetOrCreateSecretKey())
            using (AndroidJavaClass cipherClass = new AndroidJavaClass("javax.crypto.Cipher"))
            using (AndroidJavaObject cipher = cipherClass.CallStatic<AndroidJavaObject>("getInstance", "AES/GCM/NoPadding"))
            using (AndroidJavaObject spec = new AndroidJavaObject("javax.crypto.spec.GCMParameterSpec", 128, iv))
            {
                cipher.Call("init", 2, key, spec);
                byte[] plaintextBytes = cipher.Call<byte[]>("doFinal", cipherBytes);
                string plaintext = Encoding.UTF8.GetString(plaintextBytes);
                Array.Clear(plaintextBytes, 0, plaintextBytes.Length);
                return plaintext;
            }
        }

        private static AndroidJavaObject GetOrCreateSecretKey()
        {
            AndroidJavaObject existing = GetExistingSecretKey();
            if (existing != null) return existing;

            using (AndroidJavaClass keyGeneratorClass = new AndroidJavaClass("javax.crypto.KeyGenerator"))
            using (AndroidJavaObject generator = keyGeneratorClass.CallStatic<AndroidJavaObject>("getInstance", "AES", "AndroidKeyStore"))
            using (AndroidJavaObject builder = new AndroidJavaObject("android.security.keystore.KeyGenParameterSpec$Builder", KeyAlias, 3))
            using (AndroidJavaObject blockModeBuilder = builder.Call<AndroidJavaObject>("setBlockModes", (object)new[] { "GCM" }))
            using (AndroidJavaObject paddingBuilder = blockModeBuilder.Call<AndroidJavaObject>("setEncryptionPaddings", (object)new[] { "NoPadding" }))
            using (AndroidJavaObject randomizedBuilder = paddingBuilder.Call<AndroidJavaObject>("setRandomizedEncryptionRequired", true))
            using (AndroidJavaObject specification = randomizedBuilder.Call<AndroidJavaObject>("build"))
            {
                generator.Call("init", specification);
                return generator.Call<AndroidJavaObject>("generateKey");
            }
        }

        private static AndroidJavaObject GetExistingSecretKey()
        {
            using (AndroidJavaClass keyStoreClass = new AndroidJavaClass("java.security.KeyStore"))
            using (AndroidJavaObject keyStore = keyStoreClass.CallStatic<AndroidJavaObject>("getInstance", "AndroidKeyStore"))
            {
                keyStore.Call("load", (object)null);
                if (!keyStore.Call<bool>("containsAlias", KeyAlias)) return null;
                return keyStore.Call<AndroidJavaObject>("getKey", KeyAlias, null);
            }
        }
#endif

        private static string ProtectSoftware(string plaintext)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = GetOrCreateSoftwareKey();
                aes.GenerateIV();
                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
                    byte[] encrypted = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);
                    return Convert.ToBase64String(aes.IV) + "." + Convert.ToBase64String(encrypted);
                }
            }
        }

        private static string UnprotectSoftware(string protectedValue)
        {
            string[] parts = protectedValue.Split('.');
            if (parts.Length != 2) throw new FormatException("Malformed protected chat value.");
            using (Aes aes = Aes.Create())
            {
                aes.Key = GetOrCreateSoftwareKey();
                aes.IV = Convert.FromBase64String(parts[0]);
                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    byte[] cipherBytes = Convert.FromBase64String(parts[1]);
                    byte[] plaintextBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                    return Encoding.UTF8.GetString(plaintextBytes);
                }
            }
        }

        private static byte[] GetOrCreateSoftwareKey()
        {
            string stored = PlayerPrefs.GetString(FallbackKeyPref, string.Empty);
            if (!string.IsNullOrEmpty(stored))
            {
                try { return Convert.FromBase64String(stored); } catch (FormatException) { }
            }

            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.GenerateKey();
                PlayerPrefs.SetString(FallbackKeyPref, Convert.ToBase64String(aes.Key));
                PlayerPrefs.Save();
                return aes.Key;
            }
        }
    }
}
