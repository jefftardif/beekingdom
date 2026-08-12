using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BeeKingdom.Networking
{
    public sealed class AndroidKeystoreGameMutationOutboxStore :
        IProtectedGameMutationOutboxStore
    {
        private const string KeyAlias = "bee_kingdom_game_mutation_outbox_v1";
        private const string PreferencesName = "bee_kingdom_protected_game_mutation_outbox_v1";
        private const string CiphertextKey = "outbox_ciphertext";
        private const string InitializationVectorKey = "outbox_iv";

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

        public Task SaveAsync(
            string protectedPlaintext,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(protectedPlaintext))
                throw new ArgumentException(
                    "A mutation outbox document is required.",
                    nameof(protectedPlaintext));
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (AndroidJavaObject key = GetOrCreateSecretKey())
                using (AndroidJavaClass cipherClass = new AndroidJavaClass("javax.crypto.Cipher"))
                using (AndroidJavaObject cipher =
                    cipherClass.CallStatic<AndroidJavaObject>("getInstance", "AES/GCM/NoPadding"))
                {
                    cipher.Call("init", 1, key);
                    byte[] plaintext = Encoding.UTF8.GetBytes(protectedPlaintext);
                    byte[] encrypted = cipher.Call<byte[]>("doFinal", plaintext);
                    byte[] iv = cipher.Call<byte[]>("getIV");
                    WriteEncrypted(ConvertToBase64(encrypted), ConvertToBase64(iv));
                    Array.Clear(plaintext, 0, plaintext.Length);
                }
                return Task.CompletedTask;
            }
            catch (Exception exception)
            {
                throw Failure("game.mutation.android_keystore_write_failed", exception);
            }
#else
            throw new InvalidOperationException("game.mutation.protected_storage_unavailable");
#endif
        }

        public Task<string> LoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                string ciphertext;
                string iv;
                if (!TryReadEncrypted(out ciphertext, out iv))
                    return Task.FromResult<string>(null);
                using (AndroidJavaObject key = GetExistingSecretKey())
                using (AndroidJavaClass cipherClass = new AndroidJavaClass("javax.crypto.Cipher"))
                using (AndroidJavaObject cipher =
                    cipherClass.CallStatic<AndroidJavaObject>("getInstance", "AES/GCM/NoPadding"))
                using (AndroidJavaObject spec =
                    new AndroidJavaObject(
                        "javax.crypto.spec.GCMParameterSpec",
                        128,
                        ConvertFromBase64(iv)))
                {
                    if (key == null)
                        throw new InvalidOperationException("Android Keystore alias is missing.");
                    cipher.Call("init", 2, key, spec);
                    byte[] plaintext = cipher.Call<byte[]>(
                        "doFinal",
                        ConvertFromBase64(ciphertext));
                    string serialized = Encoding.UTF8.GetString(plaintext);
                    Array.Clear(plaintext, 0, plaintext.Length);
                    return Task.FromResult(serialized);
                }
            }
            catch (Exception exception)
            {
                throw Failure("game.mutation.android_keystore_read_failed", exception);
            }
#else
            return Task.FromResult<string>(null);
#endif
        }

        public Task DeleteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (AndroidJavaObject preferences = GetPreferences())
                using (AndroidJavaObject editor =
                    preferences.Call<AndroidJavaObject>("edit"))
                {
                    editor.Call<AndroidJavaObject>("remove", CiphertextKey);
                    editor.Call<AndroidJavaObject>("remove", InitializationVectorKey);
                    editor.Call("apply");
                }
            }
            catch (Exception exception)
            {
                throw Failure("game.mutation.android_keystore_delete_failed", exception);
            }
#endif
            return Task.CompletedTask;
        }

        public static IReadOnlyList<string> ProofRows()
        {
            return new[]
            {
                "android_game_mutation_cipher:AES/GCM/NoPadding",
                "android_game_mutation_key_provider:AndroidKeyStore",
                "android_game_mutation_plaintext_preferences:false",
                "android_game_mutation_ciphertext_preferences:true",
                "android_game_mutation_key_exported:false",
                "android_game_mutation_alias_separate_from_read_cache:true",
                "editor_game_mutation_persistence:false"
            };
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject GetOrCreateSecretKey()
        {
            AndroidJavaObject existing = GetExistingSecretKey();
            if (existing != null) return existing;
            using (AndroidJavaClass keyGeneratorClass =
                new AndroidJavaClass("javax.crypto.KeyGenerator"))
            using (AndroidJavaObject generator =
                keyGeneratorClass.CallStatic<AndroidJavaObject>(
                    "getInstance",
                    "AES",
                    "AndroidKeyStore"))
            using (AndroidJavaObject builder =
                new AndroidJavaObject(
                    "android.security.keystore.KeyGenParameterSpec$Builder",
                    KeyAlias,
                    3))
            using (AndroidJavaObject blockModeBuilder =
                builder.Call<AndroidJavaObject>("setBlockModes", (object)new[] { "GCM" }))
            using (AndroidJavaObject paddingBuilder =
                blockModeBuilder.Call<AndroidJavaObject>(
                    "setEncryptionPaddings",
                    (object)new[] { "NoPadding" }))
            using (AndroidJavaObject randomizedBuilder =
                paddingBuilder.Call<AndroidJavaObject>(
                    "setRandomizedEncryptionRequired",
                    true))
            using (AndroidJavaObject specification =
                randomizedBuilder.Call<AndroidJavaObject>("build"))
            {
                generator.Call("init", specification);
                return generator.Call<AndroidJavaObject>("generateKey");
            }
        }

        private static AndroidJavaObject GetExistingSecretKey()
        {
            using (AndroidJavaClass keyStoreClass =
                new AndroidJavaClass("java.security.KeyStore"))
            using (AndroidJavaObject keyStore =
                keyStoreClass.CallStatic<AndroidJavaObject>(
                    "getInstance",
                    "AndroidKeyStore"))
            {
                keyStore.Call("load", (object)null);
                if (!keyStore.Call<bool>("containsAlias", KeyAlias)) return null;
                return keyStore.Call<AndroidJavaObject>("getKey", KeyAlias, null);
            }
        }

        private static AndroidJavaObject GetPreferences()
        {
            using (AndroidJavaClass unityPlayer =
                new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity =
                unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                return activity.Call<AndroidJavaObject>(
                    "getSharedPreferences",
                    PreferencesName,
                    0);
            }
        }

        private static void WriteEncrypted(string ciphertext, string iv)
        {
            using (AndroidJavaObject preferences = GetPreferences())
            using (AndroidJavaObject editor =
                preferences.Call<AndroidJavaObject>("edit"))
            {
                editor.Call<AndroidJavaObject>(
                    "putString",
                    CiphertextKey,
                    ciphertext);
                editor.Call<AndroidJavaObject>(
                    "putString",
                    InitializationVectorKey,
                    iv);
                editor.Call("apply");
            }
        }

        private static bool TryReadEncrypted(out string ciphertext, out string iv)
        {
            using (AndroidJavaObject preferences = GetPreferences())
            {
                ciphertext = preferences.Call<string>(
                    "getString",
                    CiphertextKey,
                    string.Empty);
                iv = preferences.Call<string>(
                    "getString",
                    InitializationVectorKey,
                    string.Empty);
                return !string.IsNullOrWhiteSpace(ciphertext) &&
                    !string.IsNullOrWhiteSpace(iv);
            }
        }

        private static string ConvertToBase64(byte[] value)
        {
            using (AndroidJavaClass base64 =
                new AndroidJavaClass("android.util.Base64"))
                return base64.CallStatic<string>("encodeToString", value, 2);
        }

        private static byte[] ConvertFromBase64(string value)
        {
            using (AndroidJavaClass base64 =
                new AndroidJavaClass("android.util.Base64"))
                return base64.CallStatic<byte[]>("decode", value, 2);
        }
#endif

        private static InvalidOperationException Failure(
            string code,
            Exception exception)
        {
            Debug.LogWarning(
                "Bee Kingdom protected game mutation outbox failed: " +
                exception.GetType().Name);
            return new InvalidOperationException(code);
        }
    }
}
