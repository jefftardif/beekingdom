using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace BeeKingdom.Networking
{
    public sealed class RemoteCanonicalHivePosition
    {
        public Guid HiveId { get; set; }
        public Guid PlayerId { get; set; }
        public string WorldId { get; set; }
        public string SurfaceId { get; set; }
        public string CoordinateUnit { get; set; }
        public float? X { get; set; }
        public float? Y { get; set; }
        public long Revision { get; set; }
    }

    public sealed class VerifiedCanonicalHivePosition
    {
        public Guid HiveId { get; }
        public Guid PlayerId { get; }
        public string WorldId { get; }
        public string SurfaceId { get; }
        public string CoordinateUnit { get; }
        public float X { get; }
        public float Y { get; }
        public long Revision { get; }
        internal VerifiedCanonicalHivePosition(RemoteCanonicalHivePosition wire)
        {
            HiveId=wire.HiveId;PlayerId=wire.PlayerId;WorldId=wire.WorldId;SurfaceId=wire.SurfaceId;
            CoordinateUnit=wire.CoordinateUnit;X=wire.X.Value;Y=wire.Y.Value;Revision=wire.Revision;
        }
    }

    public sealed class HivePositionReadResult
    {
        public string Code { get; }
        public VerifiedCanonicalHivePosition Position { get; }
        internal HivePositionReadResult(string code, RemoteCanonicalHivePosition position = null)
        { Code = code; Position = position == null ? null : new VerifiedCanonicalHivePosition(position); }
        public static HivePositionReadResult AuthorityNotGranted() => new HivePositionReadResult("position_authority_not_granted");
        public static HivePositionReadResult SessionChanged() => new HivePositionReadResult("position_session_changed");
    }

    // Read/compare only. No cache, coordinate application, demo identity mapping or POST.
    public sealed class CanonicalHivePositionClient
    {
        private readonly MobileAccountSessionGate gate;
        private readonly IGameAccountSessionSource sessions;
        private readonly IAuthenticatedGameRestTransport transport;
        private readonly Func<long> bindingGeneration;

        // Host increments generation on logout, reconnect, hive/realm/base-URL change.
        public CanonicalHivePositionClient(MobileAccountSessionGate gate, IGameAccountSessionSource sessions,
            IAuthenticatedGameRestTransport transport, Func<long> bindingGeneration)
        {
            this.gate = gate ?? throw new ArgumentNullException(nameof(gate));
            this.sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            this.bindingGeneration = bindingGeneration ?? throw new ArgumentNullException(nameof(bindingGeneration));
        }

        public async Task<HivePositionReadResult> ReadAsync(bool serverAuthority, Guid player, Guid hive,
            string world, string surface, string unit, long minimumRevision, CancellationToken ct)
        {
            if (!serverAuthority) return new HivePositionReadResult("position_authority_not_granted");
            if (player == Guid.Empty || hive == Guid.Empty || string.IsNullOrWhiteSpace(world)
                || string.IsNullOrWhiteSpace(surface) || string.IsNullOrWhiteSpace(unit) || minimumRevision < 0)
                return new HivePositionReadResult("position_configuration_invalid");
            if (!gate.CanSubmitLogin) return new HivePositionReadResult("position_session_required");
            long generation = bindingGeneration();
            GameAccountSession session = null;
            try
            {
                ct.ThrowIfCancellationRequested();
                var refreshable = sessions as IRefreshableGameAccountSessionSource;
                if (refreshable != null) session = await refreshable.GetFreshSessionAsync(ct);
                else sessions.TryGetSession(out session);
                if (session == null || session.PlayerId != player || string.IsNullOrWhiteSpace(session.AccessToken))
                    return new HivePositionReadResult("position_session_required");
                if (!Current(session, generation)) return new HivePositionReadResult("position_session_changed");
                var response = await transport.SendAsync<RemoteCanonicalHivePosition>(new AuthenticatedGameRestRequest("GET",
                    "/game/v1/world/hives/mine/" + hive.ToString("D") + "?worldId=" + Uri.EscapeDataString(world)
                    + "&surfaceId=" + Uri.EscapeDataString(surface)), session.AccessToken, ct);
                ct.ThrowIfCancellationRequested();
                if (!Current(session, generation)) return new HivePositionReadResult("position_session_changed");
                if (response == null || response.PlayerId != player || response.HiveId != hive
                    || response.WorldId != world || response.SurfaceId != surface || response.CoordinateUnit != unit
                    || response.Revision <= 0 || response.Revision < minimumRevision || !response.X.HasValue || !response.Y.HasValue
                    || !Finite(response.X.Value) || !Finite(response.Y.Value))
                    return new HivePositionReadResult("position_response_invalid");
                return new HivePositionReadResult("server_position_verified", response);
            }
            catch (OperationCanceledException) { return new HivePositionReadResult("position_cancelled"); }
            catch (JsonException) { return new HivePositionReadResult("position_response_invalid"); }
            catch (AuthenticatedGameRestException error)
            {
                if (generation != bindingGeneration() || session != null && !Current(session, generation))
                    return new HivePositionReadResult("position_session_changed");
                // Safe fixed codes only. Never log transport messages or credentials.
                if (error.StatusCode == 401) return new HivePositionReadResult("position_session_required");
                if (error.StatusCode == 403) return new HivePositionReadResult("position_access_denied");
                if (error.StatusCode == 404) return new HivePositionReadResult("position_endpoint_unavailable");
                if (error.StatusCode == 409) return new HivePositionReadResult("position_not_reconciled");
                if (error.StatusCode == 503) return new HivePositionReadResult("position_authority_unavailable");
                return new HivePositionReadResult("position_transport_unavailable");
            }
        }

        private bool Current(GameAccountSession original, long generation)
        {
            GameAccountSession current;
            return generation == bindingGeneration() && gate.CanSubmitLogin
                && sessions.TryGetSession(out current) && current != null && current.PlayerId == original.PlayerId
                && string.Equals(current.AccessToken, original.AccessToken, StringComparison.Ordinal);
        }

        public static string CompareLoadedPlacement(HivePositionReadResult result, Guid verifiedLocalPlayer,
            Guid verifiedLocalHive, string loadedWorld, string loadedSurface, string loadedUnit, float loadedX, float loadedY)
        {
            if (result == null || result.Code != "server_position_verified" || result.Position == null) return "comparison_no_server_position";
            if (verifiedLocalPlayer == Guid.Empty || verifiedLocalHive == Guid.Empty) return "comparison_local_identity_unverified";
            var p = result.Position;
            if (verifiedLocalPlayer != p.PlayerId || verifiedLocalHive != p.HiveId) return "comparison_identity_conflict";
            if (loadedWorld != p.WorldId || loadedSurface != p.SurfaceId || loadedUnit != p.CoordinateUnit) return "comparison_coordinate_contract_conflict";
            if (!Finite(loadedX) || !Finite(loadedY)) return "comparison_local_position_invalid";
            // Exact float equality: no hidden tolerance authorizes a displacement.
            return loadedX == p.X && loadedY == p.Y ? "comparison_position_unchanged" : "comparison_position_conflict";
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
