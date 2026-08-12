using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BeeKingdom.Networking
{
    // The editor needs a session-scoped store for Play Mode, while Windows builds need
    // machine/user-bound protection instead of the Android-only Keystore implementation.
    public sealed class PlatformGameMutationOutboxStore : IProtectedGameMutationOutboxStore
    {
        private const string FileName = "bee_kingdom_game_mutation_outbox_dpapi_v1.bin";
        private static readonly object Sync = new object();
        private static string editorValue;
        private readonly string filePath;

        public PlatformGameMutationOutboxStore()
        {
            filePath = Path.Combine(Application.persistentDataPath, FileName);
        }

        public bool IsProtectionAvailable
        {
            get
            {
#if UNITY_EDITOR
                return true;
#elif UNITY_STANDALONE_WIN
                return Application.platform == RuntimePlatform.WindowsPlayer;
#else
                return false;
#endif
            }
        }

        public Task<string> LoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
#if UNITY_EDITOR
            lock (Sync) return Task.FromResult(editorValue);
#elif UNITY_STANDALONE_WIN
            try
            {
                if (!File.Exists(filePath)) return Task.FromResult<string>(null);
                return Task.FromResult(Encoding.UTF8.GetString(Unprotect(File.ReadAllBytes(filePath))));
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Bee Kingdom Windows mutation outbox load failed: " + exception.GetType().Name);
                return Task.FromResult<string>(null);
            }
#else
            return Task.FromResult<string>(null);
#endif
        }

        public Task SaveAsync(string protectedPlaintext, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(protectedPlaintext)) throw new ArgumentException("Mutation outbox content is required.", nameof(protectedPlaintext));
#if UNITY_EDITOR
            lock (Sync) editorValue = protectedPlaintext;
            return Task.CompletedTask;
#elif UNITY_STANDALONE_WIN
            try
            {
                File.WriteAllBytes(filePath, Protect(Encoding.UTF8.GetBytes(protectedPlaintext)));
                return Task.CompletedTask;
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("game.mutation.windows_dpapi_write_failed", exception);
            }
#else
            throw new InvalidOperationException("game.mutation.protected_storage_unavailable");
#endif
        }

        public Task DeleteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
#if UNITY_EDITOR
            lock (Sync) editorValue = null;
#elif UNITY_STANDALONE_WIN
            try { if (File.Exists(filePath)) File.Delete(filePath); }
            catch (Exception exception) { Debug.LogWarning("Bee Kingdom Windows mutation outbox delete failed: " + exception.GetType().Name); }
#endif
            return Task.CompletedTask;
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        [StructLayout(LayoutKind.Sequential)]
        private struct DataBlob
        {
            public int cbData;
            public IntPtr pbData;
        }

        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool CryptProtectData(ref DataBlob input, string description, IntPtr entropy, IntPtr reserved, IntPtr prompt, int flags, out DataBlob output);

        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool CryptUnprotectData(ref DataBlob input, StringBuilder description, IntPtr entropy, IntPtr reserved, IntPtr prompt, int flags, out DataBlob output);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LocalFree(IntPtr handle);

        private static byte[] Protect(byte[] plaintext)
        {
            DataBlob input = new DataBlob();
            DataBlob output = new DataBlob();
            GCHandle pinned = GCHandle.Alloc(plaintext, GCHandleType.Pinned);
            try
            {
                input.cbData = plaintext.Length;
                input.pbData = pinned.AddrOfPinnedObject();
                if (!CryptProtectData(ref input, string.Empty, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 1, out output))
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
                input.cbData = encrypted.Length;
                input.pbData = pinned.AddrOfPinnedObject();
                if (!CryptUnprotectData(ref input, null, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 1, out output))
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
    }
}
