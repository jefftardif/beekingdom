using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BeeKingdom.Networking
{
    // Stockage de secours pour tester le flux de connexion officiel directement depuis
    // l'Editeur Unity (Windows/Mac), ou ni le Keychain iOS ni le Keystore Android ne sont
    // disponibles (AndroidKeystoreRefreshTokenStore.IsProtectionAvailable est toujours faux
    // hors d'un vrai appareil Android). AUCUNE protection materielle reelle : le jeton est
    // ecrit en clair dans PlayerPrefs. N'est JAMAIS actif hors de l'Editeur - les vrais
    // builds Android/iOS continuent d'utiliser AndroidKeystoreRefreshTokenStore/
    // IosKeychainRefreshTokenStore, jamais celui-ci.
    public sealed class EditorFallbackRefreshTokenStore : IProtectedRefreshTokenStore
    {
        // En memoire seulement (pas PlayerPrefs) : PlayerPrefs.SetString/GetString exigent le
        // thread principal Unity, or ce store est appele depuis des continuations async qui
        // s'executent hors thread principal (ConfigureAwait(false) en amont). Un champ statique
        // en memoire evite cette contrainte ; la persistance ne survit pas a un redemarrage de
        // l'Editeur, ce qui est suffisant pour tester le flux de connexion officiel en Play Mode.
        private static readonly object Sync = new object();
        private static string storedSerialized;

        public bool IsProtectionAvailable
        {
            get
            {
#if UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        public Task SaveAsync(ProtectedRefreshTokenRecord record, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (record == null) throw new ArgumentNullException(nameof(record));
#if UNITY_EDITOR
            lock (Sync)
            {
                storedSerialized = Serialize(record);
            }
            return Task.CompletedTask;
#else
            throw new MobileAccountSessionException(
                MobileAccountSessionError.ProtectedStorageUnavailable,
                "auth.protected_storage_unavailable");
#endif
        }

        public Task<ProtectedRefreshTokenRecord> LoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
#if UNITY_EDITOR
            string stored;
            lock (Sync)
            {
                stored = storedSerialized;
            }
            if (string.IsNullOrWhiteSpace(stored)) return Task.FromResult<ProtectedRefreshTokenRecord>(null);
            try
            {
                return Task.FromResult(Deserialize(stored));
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Bee Kingdom editor fallback session storage failed: " + exception.GetType().Name);
                return Task.FromResult<ProtectedRefreshTokenRecord>(null);
            }
#else
            return Task.FromResult<ProtectedRefreshTokenRecord>(null);
#endif
        }

        public Task DeleteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
#if UNITY_EDITOR
            lock (Sync)
            {
                storedSerialized = null;
            }
#endif
            return Task.CompletedTask;
        }

        public static string[] ProofRows()
        {
            return new[]
            {
                "editor_refresh_cipher:none",
                "editor_refresh_storage:in_memory_plaintext",
                "editor_refresh_persistence:false",
                "editor_refresh_production_use:false"
            };
        }

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
                throw new FormatException("Editor fallback session record is malformed.");

            string sessionId = Encoding.UTF8.GetString(Convert.FromBase64String(rows[2]));
            string refreshToken = Encoding.UTF8.GetString(Convert.FromBase64String(rows[3]));
            return new ProtectedRefreshTokenRecord(
                playerId,
                accountId,
                sessionId,
                refreshToken,
                new DateTimeOffset(utcTicks, TimeSpan.Zero));
        }
    }
}
