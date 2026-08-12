using System;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Networking
{
    public sealed class RemoteVipSnapshot
    {
        public long LifetimePoints { get; set; }
        public int Level { get; set; }
        public long? NextThreshold { get; set; }
        public int CapacityBonusBps { get; set; }
        public long Revision { get; set; }
    }

    public sealed class GrantVipPointsRequestWire
    {
        public long Points { get; set; }
        public long ExpectedRevision { get; set; }
        public string IdempotencyKey { get; set; }
    }

    internal sealed class EnsureHiveResponseWire
    {
        public long Revision { get; set; }
    }

    public interface IHiveVipClient
    {
        Task<RemoteVipSnapshot> ReadAsync(Guid hiveId, CancellationToken cancellationToken = default(CancellationToken));
        Task<RemoteVipSnapshot> GrantTestPointsAsync(Guid hiveId, long points, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default(CancellationToken));
        Task EnsureHiveAsync(Guid hiveId, CancellationToken cancellationToken = default(CancellationToken));
    }

    public sealed class HiveVipClient : IHiveVipClient
    {
        private readonly MobileAccountSessionGate sessionGate;
        private readonly IGameAccountSessionSource sessionSource;
        private readonly IAuthenticatedGameRestTransport transport;

        public HiveVipClient(
            MobileAccountSessionGate sessionGate,
            IGameAccountSessionSource sessionSource,
            IAuthenticatedGameRestTransport transport)
        {
            this.sessionGate = sessionGate ?? throw new ArgumentNullException(nameof(sessionGate));
            this.sessionSource = sessionSource ?? throw new ArgumentNullException(nameof(sessionSource));
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public async Task<RemoteVipSnapshot> ReadAsync(Guid hiveId, CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireHive(hiveId);
            HiveGameClientSessionHelper.SessionContext context = await HiveGameClientSessionHelper.RequireSessionAsync(sessionGate, sessionSource, cancellationToken).ConfigureAwait(false);
            var request = new AuthenticatedGameRestRequest("GET", "/game/v1/hives/" + hiveId.ToString("D") + "/vip");
            RemoteVipSnapshot snapshot = await HiveGameClientSessionHelper
                .SendWithSingleAuthenticationRefreshAsync<RemoteVipSnapshot>(transport, sessionSource, request, context, cancellationToken)
                .ConfigureAwait(false);
            RequireSnapshot(snapshot);
            return snapshot;
        }

        // Source de test des points VIP en attendant l'integration des vrais achats (App
        // Store/Google Play). Le serveur refuse cette route hors environnement de developpement.
        public async Task<RemoteVipSnapshot> GrantTestPointsAsync(Guid hiveId, long points, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireHive(hiveId);
            HiveGameClientSessionHelper.RequireIdempotencyKey(idempotencyKey);
            HiveGameClientSessionHelper.SessionContext context = await HiveGameClientSessionHelper.RequireSessionAsync(sessionGate, sessionSource, cancellationToken).ConfigureAwait(false);
            var request = new AuthenticatedGameRestRequest(
                "POST",
                "/dev/hives/" + hiveId.ToString("D") + "/grant-vip-points",
                new GrantVipPointsRequestWire { Points = points, ExpectedRevision = expectedRevision, IdempotencyKey = idempotencyKey });
            RemoteVipSnapshot snapshot = await HiveGameClientSessionHelper
                .SendWithSingleAuthenticationRefreshAsync<RemoteVipSnapshot>(transport, sessionSource, request, context, cancellationToken)
                .ConfigureAwait(false);
            RequireSnapshot(snapshot);
            return snapshot;
        }

        // Materialise l'etat de ruche cote serveur pour un joueur reel de premiere connexion :
        // les endpoints de lecture (VIP, abeilles championnes, production hors ligne) ne
        // creent jamais l'etat initial eux-memes (game.hive_not_found tant que rien n'existe).
        public async Task EnsureHiveAsync(Guid hiveId, CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireHive(hiveId);
            HiveGameClientSessionHelper.SessionContext context = await HiveGameClientSessionHelper.RequireSessionAsync(sessionGate, sessionSource, cancellationToken).ConfigureAwait(false);
            var request = new AuthenticatedGameRestRequest("POST", "/game/v1/hives/" + hiveId.ToString("D") + "/ensure");
            await HiveGameClientSessionHelper
                .SendWithSingleAuthenticationRefreshAsync<EnsureHiveResponseWire>(transport, sessionSource, request, context, cancellationToken)
                .ConfigureAwait(false);
        }

        private static void RequireSnapshot(RemoteVipSnapshot snapshot)
        {
            if (snapshot == null || snapshot.LifetimePoints < 0 || snapshot.Level < 0 || snapshot.Revision < 0)
                throw new HivePerimeterClientException(HivePerimeterClientError.InvalidResponse, "The VIP snapshot is incomplete.");
        }

        private static void RequireHive(Guid hiveId)
        {
            if (hiveId == Guid.Empty) throw new HivePerimeterClientException(HivePerimeterClientError.InvalidRequest, "A hive identifier is required.");
        }
    }
}
