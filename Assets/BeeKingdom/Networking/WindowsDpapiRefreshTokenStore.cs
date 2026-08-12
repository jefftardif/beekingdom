using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BeeKingdom.Networking
{
    // Equivalent Windows du chiffrement materiel Android Keystore / iOS Keychain : DPAPI
    // (crypt32.dll, CryptProtectData/CryptUnprotectData) chiffre la donnee avec une cle
    // derivee du compte Windows courant - illisible pour tout autre compte Windows ou si le
    // fichier est copie sur une autre machine. Avant ce correctif,
    // MobileAccountSessionRuntimeBootstrap.CreateRefreshTokenStore() routait tout build
    // Windows Standalone vers AndroidKeystoreRefreshTokenStore, qui refuse toute sauvegarde
    // hors d'un vrai appareil Android (bug confirme, voir ALPHA_READINESS_REVIEW.md §2.4) - la
    // session ne survivait jamais a une fermeture du jeu sur Windows.
    public sealed class WindowsDpapiRefreshTokenStore : IProtectedRefreshTokenStore
    {
        private const string FileName = "bee_kingdom_protected_session_dpapi_v1.bin";
        private const int CryptProtectUiForbidden = 0x1;

        // Application.persistentDataPath n'est fiable que depuis le thread principal Unity ;
        // ce store est construit sur le thread principal (voir
        // MobileAccountSessionRuntimeBootstrap.CreateRefreshTokenStore), mais Save/Load/Delete
        // sont appeles depuis des continuations async hors thread principal. Capturer le chemin
        // ici, dans le constructeur, evite tout acces API Unity plus tard - PlayerPrefs, lui,
        // exige explicitement le thread principal et jetait un UnityException a chaque
        // tentative de sauvegarde (confirme en testant un vrai build Windows).
        private readonly string filePath;

        public WindowsDpapiRefreshTokenStore()
        {
            filePath = Path.Combine(Application.persistentDataPath, FileName);
        }

        public bool IsProtectionAvailable
        {
            get
            {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
                return Application.platform == RuntimePlatform.WindowsPlayer;
#else
                return false;
#endif
            }
        }

        public Task SaveAsync(ProtectedRefreshTokenRecord record, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (record == null) throw new ArgumentNullException(nameof(record));
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            try
            {
                byte[] plaintext = Encoding.UTF8.GetBytes(Serialize(record));
                byte[] encrypted = Protect(plaintext);
                Array.Clear(plaintext, 0, plaintext.Length);
                File.WriteAllBytes(filePath, encrypted);
                return Task.CompletedTask;
            }
            catch (Exception exception)
            {
                throw ProtectedFailure("auth.windows_dpapi_write_failed", exception);
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
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            try
            {
                if (!File.Exists(filePath)) return Task.FromResult<ProtectedRefreshTokenRecord>(null);
                byte[] encrypted = File.ReadAllBytes(filePath);
                byte[] plaintext = Unprotect(encrypted);
                string serialized = Encoding.UTF8.GetString(plaintext);
                Array.Clear(plaintext, 0, plaintext.Length);
                return Task.FromResult(Deserialize(serialized));
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Bee Kingdom Windows protected session storage failed: " + exception.GetType().Name);
                return Task.FromResult<ProtectedRefreshTokenRecord>(null);
            }
#else
            return Task.FromResult<ProtectedRefreshTokenRecord>(null);
#endif
        }

        public Task DeleteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            try { if (File.Exists(filePath)) File.Delete(filePath); }
            catch (Exception exception) { Debug.LogWarning("Bee Kingdom Windows protected session delete failed: " + exception.GetType().Name); }
#endif
            return Task.CompletedTask;
        }

        public static string[] ProofRows()
        {
            return new[]
            {
                "windows_refresh_cipher:dpapi_currentuser",
                "windows_refresh_key_provider:windows_data_protection_api",
                "windows_refresh_plaintext_on_disk:false",
                "windows_refresh_ciphertext_on_disk:true"
            };
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        [StructLayout(LayoutKind.Sequential)]
        private struct DataBlob
        {
            public int cbData;
            public IntPtr pbData;
        }

        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool CryptProtectData(
            ref DataBlob dataIn,
            string description,
            IntPtr entropy,
            IntPtr reserved,
            IntPtr promptStruct,
            int flags,
            out DataBlob dataOut);

        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool CryptUnprotectData(
            ref DataBlob dataIn,
            StringBuilder description,
            IntPtr entropy,
            IntPtr reserved,
            IntPtr promptStruct,
            int flags,
            out DataBlob dataOut);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LocalFree(IntPtr handle);

        private static byte[] Protect(byte[] plaintext)
        {
            DataBlob input = new DataBlob();
            DataBlob output = new DataBlob();
            GCHandle pinned = GCHandle.Alloc(plaintext, GCHandleType.Pinned);
            try
            {
                input.pbData = pinned.AddrOfPinnedObject();
                input.cbData = plaintext.Length;
                if (!CryptProtectData(ref input, string.Empty, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, CryptProtectUiForbidden, out output))
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                byte[] result = new byte[output.cbData];
                Marshal.Copy(output.pbData, result, 0, output.cbData);
                return result;
            }
            finally
            {
                pinned.Free();
                if (output.pbData != IntPtr.Zero) LocalFree(output.pbData);
            }
        }

        private static byte[] Unprotect(byte[] encrypted)
        {
            DataBlob input = new DataBlob();
            DataBlob output = new DataBlob();
            GCHandle pinned = GCHandle.Alloc(encrypted, GCHandleType.Pinned);
            try
            {
                input.pbData = pinned.AddrOfPinnedObject();
                input.cbData = encrypted.Length;
                if (!CryptUnprotectData(ref input, null, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, CryptProtectUiForbidden, out output))
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                byte[] result = new byte[output.cbData];
                Marshal.Copy(output.pbData, result, 0, output.cbData);
                return result;
            }
            finally
            {
                pinned.Free();
                if (output.pbData != IntPtr.Zero) LocalFree(output.pbData);
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
                throw new FormatException("Windows protected session record is malformed.");

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
            Debug.LogWarning("Bee Kingdom Windows protected session storage failed: " + exception.GetType().Name);
            return new MobileAccountSessionException(MobileAccountSessionError.ProtectedStorageFailure, code);
        }
    }
}
