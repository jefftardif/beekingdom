using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BeeKingdom.Networking
{
    public sealed class IosKeychainGameMutationOutboxStore : IProtectedGameMutationOutboxStore
    {
        private const string AccountKey = "bee_kingdom_game_mutation_outbox_v1";

        public bool IsProtectionAvailable => IosKeychainBridge.IsAvailable;

        public Task SaveAsync(string protectedPlaintext, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(protectedPlaintext))
                throw new ArgumentException("A mutation outbox document is required.", nameof(protectedPlaintext));
            if (!IosKeychainBridge.IsAvailable) throw new InvalidOperationException("game.mutation.protected_storage_unavailable");
            try
            {
                IosKeychainBridge.Set(AccountKey, protectedPlaintext);
                return Task.CompletedTask;
            }
            catch (Exception exception)
            {
                throw Failure("game.mutation.ios_keychain_write_failed", exception);
            }
        }

        public Task<string> LoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IosKeychainBridge.IsAvailable) return Task.FromResult<string>(null);
            try
            {
                return Task.FromResult(IosKeychainBridge.TryGet(AccountKey));
            }
            catch (Exception exception)
            {
                throw Failure("game.mutation.ios_keychain_read_failed", exception);
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
                throw Failure("game.mutation.ios_keychain_delete_failed", exception);
            }
            return Task.CompletedTask;
        }

        public static IReadOnlyList<string> ProofRows()
        {
            return new[]
            {
                "ios_game_mutation_key_provider:Keychain",
                "ios_game_mutation_accessibility:after_first_unlock_this_device_only",
                "ios_game_mutation_icloud_synced:false",
                "editor_game_mutation_persistence:false"
            };
        }

        private static InvalidOperationException Failure(string code, Exception exception)
        {
            Debug.LogWarning("Bee Kingdom protected game mutation outbox failed: " + exception.GetType().Name);
            return new InvalidOperationException(code);
        }
    }
}
