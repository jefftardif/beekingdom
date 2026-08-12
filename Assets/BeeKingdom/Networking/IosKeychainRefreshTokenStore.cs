using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BeeKingdom.Networking
{
    public sealed class IosKeychainRefreshTokenStore : IProtectedRefreshTokenStore
    {
        private const string AccountKey = "bee_kingdom_mobile_refresh_v1";

        public bool IsProtectionAvailable => IosKeychainBridge.IsAvailable;

        public Task SaveAsync(ProtectedRefreshTokenRecord record, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (record == null) throw new ArgumentNullException(nameof(record));
            if (!IosKeychainBridge.IsAvailable)
                throw new MobileAccountSessionException(
                    MobileAccountSessionError.ProtectedStorageUnavailable,
                    "auth.protected_storage_unavailable");
            try
            {
                IosKeychainBridge.Set(AccountKey, Serialize(record));
                return Task.CompletedTask;
            }
            catch (Exception exception)
            {
                throw ProtectedFailure("auth.ios_keychain_write_failed", exception);
            }
        }

        public Task<ProtectedRefreshTokenRecord> LoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IosKeychainBridge.IsAvailable) return Task.FromResult<ProtectedRefreshTokenRecord>(null);
            try
            {
                string serialized = IosKeychainBridge.TryGet(AccountKey);
                if (string.IsNullOrWhiteSpace(serialized)) return Task.FromResult<ProtectedRefreshTokenRecord>(null);
                return Task.FromResult(Deserialize(serialized));
            }
            catch (Exception exception)
            {
                throw ProtectedFailure("auth.ios_keychain_read_failed", exception);
            }
        }

        public Task DeleteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IosKeychainBridge.IsAvailable) return Task.CompletedTask;
            try
            {
                IosKeychainBridge.Delete(AccountKey);
            }
            catch (Exception exception)
            {
                throw ProtectedFailure("auth.ios_keychain_delete_failed", exception);
            }
            return Task.CompletedTask;
        }

        public static string[] ProofRows()
        {
            return new[]
            {
                "ios_refresh_key_provider:Keychain",
                "ios_refresh_accessibility:after_first_unlock_this_device_only",
                "ios_refresh_icloud_synced:false",
                "editor_refresh_persistence:false"
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
