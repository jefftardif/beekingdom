using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Networking
{
    // PvE only: player squad vs. the world map bestiary. A separate PvP (player vs. player) combat
    // system is not designed or started yet, and must not reuse this loss/recovery model —
    // see Docs/Claude/Claude_Continuation.md for the distinction agreed with Jeff.
    public enum CombatPatrolClientError
    {
        NotConfigured = 0,
        AuthenticationRequired = 1,
        InvalidRequest = 2,
        InvalidResponse = 3,
        TransportFailure = 4
    }

    public sealed class CombatPatrolClientException : Exception
    {
        public CombatPatrolClientException(CombatPatrolClientError error, string message) : base(message) { Error = error; }
        public CombatPatrolClientError Error { get; }
    }

    public sealed class CombatPatrolPreviewRequest
    {
        public long Guardians { get; set; }
        public long Wingrunners { get; set; }
        public long Darters { get; set; }
    }

    public sealed class CombatPatrolLaunchRequest
    {
        public int Tier { get; set; }
        public long Guardians { get; set; }
        public long Wingrunners { get; set; }
        public long Darters { get; set; }
        public long ExpectedRevision { get; set; }
        public string IdempotencyKey { get; set; }
    }

    public sealed class CombatPatrolMutationRequest
    {
        public long ExpectedRevision { get; set; }
        public string IdempotencyKey { get; set; }
    }

    public sealed class RemoteCombatPatrolActiveEncounter
    {
        public Guid EncounterId { get; set; }
        public int Tier { get; set; }
        public Dictionary<string, long> CommittedTroops { get; set; }
        public DateTimeOffset StartedAtUtc { get; set; }
        public DateTimeOffset EndsAtUtc { get; set; }
    }

    public sealed class RemoteCombatPatrolRecoveringBatch
    {
        public string Family { get; set; }
        public long Count { get; set; }
        public DateTimeOffset ReadyAtUtc { get; set; }
    }

    public sealed class RemoteCombatPatrolClaimReceipt
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public Guid EncounterId { get; set; }
        public int Tier { get; set; }
        public string Band { get; set; }
        public DateTimeOffset ServerTimeUtc { get; set; }
        public Dictionary<string, long> PermanentLosses { get; set; }
        public Dictionary<string, long> WoundedLosses { get; set; }
        public Dictionary<string, long> CreditedByResource { get; set; }
        public Dictionary<string, RemoteHiveResourceBalance> ResultingBalances { get; set; }
        public List<string> ContributingChampionBeeIds { get; set; }
        public Dictionary<string, long> ChampionPowerBonusBpByFamily { get; set; }
        public Dictionary<string, int> TroopTierByFamily { get; set; }
        public Dictionary<string, long> TroopPowerBonusBpByFamily { get; set; }
        public long AvailablePower { get; set; }
        public long RequiredPower { get; set; }
        public long ReadinessBp { get; set; }
        public string StrategicPathId { get; set; }
        public Dictionary<string, long> StrategicPathPowerBonusBpByFamily { get; set; }
        public bool DailyFocusApplied { get; set; }
        public bool WorldEventApplied { get; set; }
        public string WorldEventKey { get; set; }
    }

    public sealed class RemoteCombatPatrolSlotCost
    {
        public long Honey { get; set; }
        public long Pollen { get; set; }
    }

    public sealed class RemoteCombatPatrolSnapshot
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public string ContractVersion { get; set; }
        public long Revision { get; set; }
        public DateTimeOffset ServerTimeUtc { get; set; }
        public List<RemoteCombatPatrolActiveEncounter> ActiveEncounters { get; set; }
        public Dictionary<int, DateTimeOffset> TierCooldownEndsAtUtc { get; set; }
        public List<RemoteCombatPatrolRecoveringBatch> Recovering { get; set; }
        public Dictionary<string, long> AvailableRoster { get; set; }
        public int Capacity { get; set; }
        public int TotalSlots { get; set; }
        public int ResourcePurchasedSlots { get; set; }
        public int PremiumPurchasedSlots { get; set; }
        public RemoteCombatPatrolSlotCost NextResourceSlotCost { get; set; }
        public RemoteCombatPatrolClaimReceipt ClaimReceipt { get; set; }
        public int FeaturedTier { get; set; }
        public RemoteActiveWorldEvent WorldEvent { get; set; }
        public int? WorldEventFeaturedTier { get; set; }
    }

    public sealed class RemoteCombatPatrolPreview
    {
        public int Tier { get; set; }
        public string EnemyName { get; set; }
        public string HazardFamily { get; set; }
        public bool CanLaunch { get; set; }
        public string BlockReason { get; set; }
        public long ReadinessBp { get; set; }
        public long AvailablePower { get; set; }
        public long RequiredPower { get; set; }
        public bool CooldownActive { get; set; }
        public DateTimeOffset? CooldownEndsAtUtc { get; set; }
        public bool IsDailyFocus { get; set; }
        public bool IsWorldEventBoosted { get; set; }
    }

    public sealed class RemoteCombatPatrolMutationResponse
    {
        public RemoteCombatPatrolSnapshot Snapshot { get; set; }
        public RemoteCombatPatrolClaimReceipt ClaimReceipt { get; set; }
    }

    public interface ICombatPatrolClient
    {
        Task<RemoteCombatPatrolSnapshot> ReadAsync(Guid hiveId, CancellationToken cancellationToken = default);
        Task<RemoteCombatPatrolPreview> PreviewAsync(Guid hiveId, int tier, long guardians, long wingrunners, long darters, CancellationToken cancellationToken = default);
        Task<RemoteCombatPatrolMutationResponse> LaunchAsync(Guid hiveId, int tier, long guardians, long wingrunners, long darters, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default);
        Task<RemoteCombatPatrolMutationResponse> ClaimAsync(Guid hiveId, Guid encounterId, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default);
        Task<RemoteCombatPatrolMutationResponse> RecallAsync(Guid hiveId, Guid encounterId, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default);
        Task<RemoteCombatPatrolMutationResponse> PurchaseResourceSlotAsync(Guid hiveId, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default);
        Task<RemoteCombatPatrolMutationResponse> GrantPremiumSlotAsync(Guid hiveId, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default);
    }

    public sealed class CombatPatrolClient : ICombatPatrolClient
    {
        public const string ContractVersion = "phase-combat-patrol-v2";
        private static readonly string[] Families = { "guardians", "wingrunners", "darters" };

        private readonly MobileAccountSessionGate sessionGate;
        private readonly IGameAccountSessionSource sessionSource;
        private readonly IAuthenticatedGameRestTransport transport;

        public CombatPatrolClient(MobileAccountSessionGate sessionGate, IGameAccountSessionSource sessionSource, IAuthenticatedGameRestTransport transport)
        {
            this.sessionGate = sessionGate ?? throw new ArgumentNullException(nameof(sessionGate));
            this.sessionSource = sessionSource ?? throw new ArgumentNullException(nameof(sessionSource));
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public Task<RemoteCombatPatrolSnapshot> ReadAsync(Guid hiveId, CancellationToken cancellationToken = default)
        {
            RequireHive(hiveId);
            return SendAsync<RemoteCombatPatrolSnapshot>(hiveId, new AuthenticatedGameRestRequest("GET", BoardPath(hiveId)), cancellationToken, ValidateSnapshot);
        }

        public Task<RemoteCombatPatrolPreview> PreviewAsync(Guid hiveId, int tier, long guardians, long wingrunners, long darters, CancellationToken cancellationToken = default)
        {
            RequireHive(hiveId);
            RequireTier(tier);
            var request = new AuthenticatedGameRestRequest("POST", PreviewPath(hiveId, tier), new CombatPatrolPreviewRequest
            {
                Guardians = Math.Max(0, guardians),
                Wingrunners = Math.Max(0, wingrunners),
                Darters = Math.Max(0, darters)
            });
            return SendAsync<RemoteCombatPatrolPreview>(hiveId, request, cancellationToken, ValidatePreview);
        }

        public Task<RemoteCombatPatrolMutationResponse> LaunchAsync(Guid hiveId, int tier, long guardians, long wingrunners, long darters, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default)
        {
            RequireHive(hiveId);
            RequireTier(tier);
            RequireRevision(expectedRevision);
            RequireKey(idempotencyKey, nameof(idempotencyKey));
            var request = new AuthenticatedGameRestRequest("POST", LaunchPath(hiveId), new CombatPatrolLaunchRequest
            {
                Tier = tier,
                Guardians = Math.Max(0, guardians),
                Wingrunners = Math.Max(0, wingrunners),
                Darters = Math.Max(0, darters),
                ExpectedRevision = expectedRevision,
                IdempotencyKey = idempotencyKey
            });
            return SendMutationAsync(hiveId, request, cancellationToken);
        }

        public Task<RemoteCombatPatrolMutationResponse> ClaimAsync(Guid hiveId, Guid encounterId, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default)
            => FinishAsync(hiveId, encounterId, expectedRevision, idempotencyKey, ClaimPath(hiveId, encounterId), cancellationToken);

        public Task<RemoteCombatPatrolMutationResponse> RecallAsync(Guid hiveId, Guid encounterId, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default)
            => FinishAsync(hiveId, encounterId, expectedRevision, idempotencyKey, RecallPath(hiveId, encounterId), cancellationToken);

        public Task<RemoteCombatPatrolMutationResponse> PurchaseResourceSlotAsync(Guid hiveId, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default)
        {
            RequireHive(hiveId);
            RequireRevision(expectedRevision);
            RequireKey(idempotencyKey, nameof(idempotencyKey));
            var request = new AuthenticatedGameRestRequest("POST", PurchaseResourceSlotPath(hiveId), new CombatPatrolMutationRequest { ExpectedRevision = expectedRevision, IdempotencyKey = idempotencyKey });
            return SendMutationAsync(hiveId, request, cancellationToken);
        }

        public Task<RemoteCombatPatrolMutationResponse> GrantPremiumSlotAsync(Guid hiveId, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default)
        {
            RequireHive(hiveId);
            RequireRevision(expectedRevision);
            RequireKey(idempotencyKey, nameof(idempotencyKey));
            var request = new AuthenticatedGameRestRequest("POST", GrantPremiumSlotPath(hiveId), new CombatPatrolMutationRequest { ExpectedRevision = expectedRevision, IdempotencyKey = idempotencyKey });
            return SendMutationAsync(hiveId, request, cancellationToken);
        }

        private Task<RemoteCombatPatrolMutationResponse> FinishAsync(Guid hiveId, Guid encounterId, long expectedRevision, string idempotencyKey, string path, CancellationToken cancellationToken)
        {
            RequireHive(hiveId);
            if (encounterId == Guid.Empty) throw InvalidRequest("An encounter identifier is required.");
            RequireRevision(expectedRevision);
            RequireKey(idempotencyKey, nameof(idempotencyKey));
            var request = new AuthenticatedGameRestRequest("POST", path, new CombatPatrolMutationRequest { ExpectedRevision = expectedRevision, IdempotencyKey = idempotencyKey });
            return SendMutationAsync(hiveId, request, cancellationToken);
        }

        private async Task<RemoteCombatPatrolMutationResponse> SendMutationAsync(Guid hiveId, AuthenticatedGameRestRequest request, CancellationToken cancellationToken)
        {
            RemoteCombatPatrolMutationResponse response = await SendAsync<RemoteCombatPatrolMutationResponse>(hiveId, request, cancellationToken, (value, playerId, hive) =>
            {
                if (value == null || value.Snapshot == null) throw InvalidResponse("The combat patrol mutation response is incomplete.");
                ValidateSnapshot(value.Snapshot, playerId, hive);
            }).ConfigureAwait(false);
            return response;
        }

        private async Task<T> SendAsync<T>(Guid hiveId, AuthenticatedGameRestRequest request, CancellationToken cancellationToken, Action<T, Guid, Guid> validate)
        {
            SessionContext context = await RequireSessionAsync(cancellationToken).ConfigureAwait(false);
            T response;
            try
            {
                response = await transport.SendAsync<T>(request, context.AccessToken, cancellationToken).ConfigureAwait(false);
            }
            catch (AuthenticatedGameRestException exception)
            {
                throw MapTransportFailure(exception);
            }
            validate(response, context.PlayerId, hiveId);
            return response;
        }

        private async Task<SessionContext> RequireSessionAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!sessionGate.CanSubmitLogin)
                throw new CombatPatrolClientException(CombatPatrolClientError.NotConfigured, "Official account session transport is not ready.");
            if (!sessionSource.TryGetSession(out GameAccountSession session) || session == null || session.PlayerId == Guid.Empty ||
                string.IsNullOrWhiteSpace(session.AccessToken) || session.AccessToken.Length > 8192)
                throw new CombatPatrolClientException(CombatPatrolClientError.AuthenticationRequired, "An official account session is required.");
            return new SessionContext(session.PlayerId, session.AccessToken);
        }

        private static void ValidateSnapshot(RemoteCombatPatrolSnapshot snapshot, Guid playerId, Guid hiveId)
        {
            if (snapshot == null) throw InvalidResponse("The combat patrol response is empty.");
            if (snapshot.PlayerId != playerId || snapshot.HiveId != hiveId) throw InvalidResponse("The combat patrol response belongs to another session or hive.");
            if (!string.Equals(snapshot.ContractVersion, ContractVersion, StringComparison.Ordinal)) throw InvalidResponse("The combat patrol contract version is unsupported.");
            if (snapshot.Revision < 0) throw InvalidResponse("The combat patrol revision is invalid.");
            if (snapshot.ActiveEncounters != null)
            {
                var seen = new HashSet<Guid>();
                foreach (RemoteCombatPatrolActiveEncounter active in snapshot.ActiveEncounters)
                {
                    if (active == null || active.EncounterId == Guid.Empty || active.Tier < 1 || active.Tier > 7 ||
                        active.CommittedTroops == null || active.EndsAtUtc <= active.StartedAtUtc || !seen.Add(active.EncounterId))
                        throw InvalidResponse("An active combat patrol encounter is inconsistent.");
                }
            }
            if (snapshot.Recovering != null)
            {
                foreach (RemoteCombatPatrolRecoveringBatch batch in snapshot.Recovering)
                    if (batch == null || !Families.Contains(batch.Family) || batch.Count <= 0)
                        throw InvalidResponse("A combat patrol recovery batch is invalid.");
            }
        }

        private static void ValidatePreview(RemoteCombatPatrolPreview preview, Guid playerId, Guid hiveId)
        {
            if (preview == null) throw InvalidResponse("The combat patrol preview is empty.");
            if (preview.Tier < 1 || preview.Tier > 7 || string.IsNullOrWhiteSpace(preview.HazardFamily) || !Families.Contains(preview.HazardFamily))
                throw InvalidResponse("The combat patrol preview tier is invalid.");
            if (preview.CanLaunch == (preview.BlockReason != null))
                throw InvalidResponse("The combat patrol preview launch gate is inconsistent with its block reason.");
        }

        private static CombatPatrolClientException MapTransportFailure(AuthenticatedGameRestException exception)
        {
            if (exception.Error == AuthenticatedGameRestError.NetworkFailure) return new CombatPatrolClientException(CombatPatrolClientError.TransportFailure, exception.SafeCode);
            if (exception.Error == AuthenticatedGameRestError.Unauthorized) return new CombatPatrolClientException(CombatPatrolClientError.AuthenticationRequired, exception.SafeCode);
            return new CombatPatrolClientException(CombatPatrolClientError.InvalidResponse, exception.SafeCode);
        }

        private static void RequireHive(Guid hiveId) { if (hiveId == Guid.Empty) throw InvalidRequest("A hive identifier is required."); }
        private static void RequireTier(int tier) { if (tier < 1 || tier > 7) throw InvalidRequest("Tier must be between 1 and 7."); }
        private static void RequireRevision(long revision) { if (revision < 0 || revision == long.MaxValue) throw InvalidRequest("The expected revision is outside the supported range."); }
        private static void RequireKey(string value, string name) { if (string.IsNullOrWhiteSpace(value) || value.Length > 256) throw InvalidRequest(name + " must contain between one and 256 characters."); }

        public static string BoardPath(Guid hiveId) => "/game/v1/hives/" + hiveId.ToString("D") + "/combat/patrol";
        public static string PreviewPath(Guid hiveId, int tier) => BoardPath(hiveId) + "/" + tier.ToString(System.Globalization.CultureInfo.InvariantCulture) + "/preview";
        public static string LaunchPath(Guid hiveId) => BoardPath(hiveId) + "/launch";
        public static string ClaimPath(Guid hiveId, Guid encounterId) => BoardPath(hiveId) + "/" + encounterId.ToString("D") + "/claim";
        public static string RecallPath(Guid hiveId, Guid encounterId) => BoardPath(hiveId) + "/" + encounterId.ToString("D") + "/recall";
        public static string PurchaseResourceSlotPath(Guid hiveId) => BoardPath(hiveId) + "/slots/purchase-resource";
        public static string GrantPremiumSlotPath(Guid hiveId) => BoardPath(hiveId) + "/slots/grant-premium";

        private static CombatPatrolClientException InvalidRequest(string message) => new CombatPatrolClientException(CombatPatrolClientError.InvalidRequest, message);
        private static CombatPatrolClientException InvalidResponse(string message) => new CombatPatrolClientException(CombatPatrolClientError.InvalidResponse, message);

        private sealed class SessionContext
        {
            public SessionContext(Guid playerId, string accessToken) { PlayerId = playerId; AccessToken = accessToken; }
            public Guid PlayerId { get; }
            public string AccessToken { get; }
        }
    }
}
