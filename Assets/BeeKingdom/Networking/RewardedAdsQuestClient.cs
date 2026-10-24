using System;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Networking
{
    /// <summary>Authenticated and read-only. The client NEVER sends reward credit requests.</summary>
    public sealed class RewardedAdsQuestSnapshot
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public string ContractVersion { get; set; }
        public bool Enabled { get; set; }
        public DateTimeOffset DayUtc { get; set; }
        public DateTimeOffset NextResetUtc { get; set; }
        public DateTimeOffset ServerTimeUtc { get; set; }
        public int CompletedCount { get; set; }
        public int MaximumCount { get; set; }
        public long NextHoney { get; set; }
        public long NextPollen { get; set; }
        public long LifetimeHoneyCredited { get; set; }
        public long LifetimePollenCredited { get; set; }
    }

    public sealed class RewardedAdsQuestClient
    {
        public const string ContractVersion = "beekindom-rewarded-quest-v1";
        private readonly MobileAccountSessionGate gate;
        private readonly IGameAccountSessionSource sessions;
        private readonly IAuthenticatedGameRestTransport transport;

        public RewardedAdsQuestClient(
            MobileAccountSessionGate gate,
            IGameAccountSessionSource sessions,
            IAuthenticatedGameRestTransport transport)
        {
            this.gate = gate ?? throw new ArgumentNullException(nameof(gate));
            this.sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public async Task<RewardedAdsQuestSnapshot> ReadAsync(
            Guid hiveId, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (hiveId == Guid.Empty)
                throw new ArgumentException("A hive identifier is required.", nameof(hiveId));
            if (!gate.CanSubmitLogin ||
                !sessions.TryGetSession(out GameAccountSession session) ||
                session == null || session.PlayerId == Guid.Empty ||
                string.IsNullOrEmpty(session.AccessToken))
                throw new InvalidOperationException("rewarded_ads.session_unavailable");

            string path = "/game/v1/hives/" + hiveId.ToString("D") + "/rewarded-ads";
            RewardedAdsQuestSnapshot snapshot =
                await transport.SendAsync<RewardedAdsQuestSnapshot>(
                    new AuthenticatedGameRestRequest("GET", path),
                    session.AccessToken, cancellationToken);

            if (snapshot == null || snapshot.PlayerId != session.PlayerId ||
                snapshot.HiveId != hiveId ||
                !string.Equals(snapshot.ContractVersion, ContractVersion, StringComparison.Ordinal) ||
                snapshot.MaximumCount != 3 || snapshot.CompletedCount < 0 ||
                snapshot.CompletedCount > snapshot.MaximumCount ||
                snapshot.DayUtc.Offset != TimeSpan.Zero ||
                snapshot.NextResetUtc != snapshot.DayUtc.AddDays(1) ||
                snapshot.ServerTimeUtc.Offset != TimeSpan.Zero ||
                snapshot.NextHoney < 0 || snapshot.NextPollen < 0)
                throw new InvalidOperationException("rewarded_ads.invalid_snapshot");

            return snapshot;
        }
    }
}
