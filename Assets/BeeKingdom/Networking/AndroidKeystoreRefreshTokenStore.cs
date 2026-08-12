using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BeeKingdom.Networking
{
    public sealed class AndroidKeystoreRefreshTokenStore : IProtectedRefreshTokenStore
    {
        private const string KeyAlias = "bee_kingdom_mobile_refresh_v1";
        private const string PreferencesName = "bee_kingdom_protected_session_v1";
        private const string CiphertextKey = "refresh_ciphertext";
        private const string InitializationVectorKey = "refresh_iv";

        public bool IsProtectionAvailable
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return Application.platform == RuntimePlatform.Android;
#else
                return false;
#endif
            }
        }

        public Task SaveAsync(ProtectedRefreshTokenRecord record, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (record == null) throw new ArgumentNullException(nameof(record));
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (AndroidJavaObject key = GetOrCreateSecretKey())
                using (AndroidJavaClass cipherClass = new AndroidJavaClass("javax.crypto.Cipher"))
                using (AndroidJavaObject cipher = cipherClass.CallStatic<AndroidJavaObject>("getInstance", "AES/GCM/NoPadding"))
                {
                    cipher.Call("init", 1, key);
                    byte[] plaintext = Encoding.UTF8.GetBytes(Serialize(record));
                    byte[] encrypted = cipher.Call<byte[]>("doFinal", plaintext);
                    byte[] iv = cipher.Call<byte[]>("getIV");
                    WriteEncrypted(ConvertToBase64(encrypted), ConvertToBase64(iv));
                    Array.Clear(plaintext, 0, plaintext.Length);
                }
                return Task.CompletedTask;
            }
            catch (Exception exception)
            {
                throw ProtectedFailure("auth.android_keystore_write_failed", exception);
            }
#else
            throw new MobileAccountSessionException(
                MobileAccountSessionError.ProtectedStorageUnavailable,
                "auth.protected_storage_unavailable");
#endif
        }

        public Task<ProtectedRefreshTokenRecord> LoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                string ciphertext;
                string iv;
                if (!TryReadEncrypted(out ciphertext, out iv))
                    return Task.FromResult<ProtectedRefreshTokenRecord>(null);

                using (AndroidJavaObject key = GetExistingSecretKey())
                using (AndroidJavaClass cipherClass = new AndroidJavaClass("javax.crypto.Cipher"))
                using (AndroidJavaObject cipher = cipherClass.CallStatic<AndroidJavaObject>("getInstance", "AES/GCM/NoPadding"))
                using (AndroidJavaObject spec = new AndroidJavaObject("javax.crypto.spec.GCMParameterSpec", 128, ConvertFromBase64(iv)))
                {
                    if (key == null) throw new InvalidOperationException("Android Keystore alias is missing.");
                    cipher.Call("init", 2, key, spec);
                    byte[] plaintext = cipher.Call<byte[]>("doFinal", ConvertFromBase64(ciphertext));
                    string serialized = Encoding.UTF8.GetString(plaintext);
                    Array.Clear(plaintext, 0, plaintext.Length);
                    return Task.FromResult(Deserialize(serialized));
                }
            }
            catch (Exception exception)
            {
                throw ProtectedFailure("auth.android_keystore_read_failed", exception);
            }
#else
            return Task.FromResult<ProtectedRefreshTokenRecord>(null);
#endif
        }

        public Task DeleteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (AndroidJavaObject preferences = GetPreferences())
                using (AndroidJavaObject editor = preferences.Call<AndroidJavaObject>("edit"))
                {
                    editor.Call<AndroidJavaObject>("remove", CiphertextKey);
                    editor.Call<AndroidJavaObject>("remove", InitializationVectorKey);
                    editor.Call("apply");
                }
            }
            catch (Exception exception)
            {
                throw ProtectedFailure("auth.android_keystore_delete_failed", exception);
            }
#endif
            return Task.CompletedTask;
        }

        public static string[] ProofRows()
        {
            return new[]
            {
                "android_refresh_cipher:AES/GCM/NoPadding",
                "android_refresh_key_provider:AndroidKeyStore",
                "android_refresh_plaintext_preferences:false",
                "android_refresh_ciphertext_preferences:true",
                "android_refresh_key_exported:false",
                "editor_refresh_persistence:false"
            };
        }

#if UNITY_ANDROID && !UNITY_EDITOR
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

        private static AndroidJavaObject GetPreferences()
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                return activity.Call<AndroidJavaObject>("getSharedPreferences", PreferencesName, 0);
            }
        }

        private static void WriteEncrypted(string ciphertext, string iv)
        {
            using (AndroidJavaObject preferences = GetPreferences())
            using (AndroidJavaObject editor = preferences.Call<AndroidJavaObject>("edit"))
            {
                editor.Call<AndroidJavaObject>("putString", CiphertextKey, ciphertext);
                editor.Call<AndroidJavaObject>("putString", InitializationVectorKey, iv);
                editor.Call("apply");
            }
        }

        private static bool TryReadEncrypted(out string ciphertext, out string iv)
        {
            using (AndroidJavaObject preferences = GetPreferences())
            {
                ciphertext = preferences.Call<string>("getString", CiphertextKey, string.Empty);
                iv = preferences.Call<string>("getString", InitializationVectorKey, string.Empty);
                return !string.IsNullOrWhiteSpace(ciphertext) && !string.IsNullOrWhiteSpace(iv);
            }
        }

        private static string ConvertToBase64(byte[] value)
        {
            using (AndroidJavaClass base64 = new AndroidJavaClass("android.util.Base64"))
            {
                return base64.CallStatic<string>("encodeToString", value, 2);
            }
        }

        private static byte[] ConvertFromBase64(string value)
        {
            using (AndroidJavaClass base64 = new AndroidJavaClass("android.util.Base64"))
            {
                return base64.CallStatic<byte[]>("decode", value, 2);
            }
        }
#endif

        private static string Serialize(ProtectedRefreshTokenRecord record)
        {
            return record.PlayerId.ToString("D") + "\n" +
                record.AccountId.ToString("D") + "\n" +
                Convert.ToBase64String(Encoding.UTF8.GetBytes(record.SessionId)) + "\n" +
                Convert.ToBase64String(Encoding.UTF8.GetBytes(record.RefreshToken)) + "\n" +
                record.RefreshTokenExpiresUtc.UtcDateTime.Ticks.ToString(CultureInfo.InvariantCulture);
        }

        private static ProtectedRefreshTokenRecord Deserialize(string value)
        {
            string[] rows = (value ?? string.Empty).Split('\n');
            Guid playerId;
            Guid accountId;
            long utcTicks;
            if (rows.Length != 5 || !Guid.TryParseExact(rows[0], "D", out playerId) ||
                !Guid.TryParseExact(rows[1], "D", out accountId) ||
                !long.TryParse(rows[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out utcTicks))
                throw new FormatException("Protected session record is malformed.");

            string sessionId = Encoding.UTF8.GetString(Convert.FromBase64String(rows[2]));
            string refreshToken = Encoding.UTF8.GetString(Convert.FromBase64String(rows[3]));
            return new ProtectedRefreshTokenRecord(
                playerId,
                accountId,
                sessionId,
                refreshToken,
                new DateTimeOffset(utcTicks, TimeSpan.Zero));
        }

        private static MobileAccountSessionException ProtectedFailure(string code, Exception exception)
        {
            Debug.LogWarning("Bee Kingdom protected session storage failed: " + exception.GetType().Name);
            return new MobileAccountSessionException(MobileAccountSessionError.ProtectedStorageFailure, code);
        }
    }
}
