using System;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;
using UnityEngine;

namespace BeeKingdom.Playground
{
    // Candidate partial: read/diagnostic only, no scene access or coordinate assignment.
    public static partial class MobileAccountSessionRuntimeBootstrap
    {
        private static CanonicalHivePositionClient canonicalPositionClient;
        private static CancellationTokenSource canonicalPositionLifetime;
        private static long canonicalPositionGeneration;
        private static Guid canonicalPositionPlayer, canonicalPositionHive;
        private static string canonicalPositionWorld, canonicalPositionSurface, canonicalPositionUnit;

        // Call only after gameplayPlayerId/gameplayHiveId have been assigned by the
        // existing authenticated gameplay binding. No identity is derived from demo nodes.
        private static void ConfigureCanonicalPositionReadForSession(MobileAccountSessionGate gate,
            IGameAccountSessionSource sessions, IAuthenticatedGameRestTransport transport, bool authenticatedAuthority)
        {
            CloseCanonicalPositionReadForSession();
            var settings = Resources.Load<CanonicalHivePositionRuntimeSettings>("CanonicalHivePositionRuntime");
            if (!authenticatedAuthority || settings == null || !settings.Enabled || gameplayPlayerId == Guid.Empty
                || gameplayHiveId == Guid.Empty || string.IsNullOrWhiteSpace(settings.WorldId)
                || string.IsNullOrWhiteSpace(settings.SurfaceId) || string.IsNullOrWhiteSpace(settings.CoordinateUnit)) return;
            canonicalPositionPlayer = gameplayPlayerId;
            canonicalPositionHive = gameplayHiveId;
            canonicalPositionWorld = settings.WorldId;
            canonicalPositionSurface = settings.SurfaceId;
            canonicalPositionUnit = settings.CoordinateUnit;
            canonicalPositionLifetime = new CancellationTokenSource();
            canonicalPositionClient = new CanonicalHivePositionClient(gate, sessions, transport, () => canonicalPositionGeneration);
        }

        private static void CloseCanonicalPositionReadForSession()
        {
            canonicalPositionGeneration++;
            canonicalPositionLifetime?.Cancel();
            canonicalPositionLifetime?.Dispose();
            canonicalPositionLifetime = null;
            canonicalPositionClient = null;
            canonicalPositionPlayer = canonicalPositionHive = Guid.Empty;
            canonicalPositionWorld = canonicalPositionSurface = canonicalPositionUnit = null;
        }

        // On-demand diagnostic, not an automatic poller. Caller must discard returned
        // results on any subsequent session/binding change; never apply them to the map.
        public static async Task<HivePositionReadResult> InspectCanonicalHivePositionAsync(CancellationToken cancellationToken)
        {
            var reader = canonicalPositionClient;
            var lifetime = canonicalPositionLifetime;
            if (reader == null || lifetime == null) return HivePositionReadResult.AuthorityNotGranted();
            long generation = canonicalPositionGeneration;
            Guid player = canonicalPositionPlayer, hive = canonicalPositionHive;
            string world = canonicalPositionWorld, surface = canonicalPositionSurface, unit = canonicalPositionUnit;
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifetime.Token))
            {
                timeout.CancelAfter(TimeSpan.FromSeconds(10));
                var result = await reader.ReadAsync(true, player, hive, world, surface, unit, 0, timeout.Token);
                if (generation != canonicalPositionGeneration || player != gameplayPlayerId || hive != gameplayHiveId)
                    return HivePositionReadResult.SessionChanged();
                return result;
            }
        }
    }
}
