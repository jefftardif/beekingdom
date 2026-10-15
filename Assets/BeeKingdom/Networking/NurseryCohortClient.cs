using System;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Networking
{
    // Matches the opt-in, server-authored /brood/cohorts contract.
    // Only basic DTO values are mirrored. The client must never compute or mint recruits.
    public sealed class RemoteNurseryCohort
    {
        public Guid CohortId { get; set; }
        public DateTimeOffset StartedAtUtc { get; set; }
        public DateTimeOffset ReadyAtUtc { get; set; }
        public int ExpectedRecruits { get; set; }
        public int NutritionAtStart { get; set; }
        public int StabilityAtStart { get; set; }
    }

    public enum RemoteNurseryVitalityTier
    {
        Fragile,
        Watch,
        Stable,
        Thriving
    }

    public sealed class RemoteNurseryGrowthForecast
    {
        public string ContractVersion { get; set; }
        public RemoteNurseryVitalityTier GlobalTier { get; set; }
        public int NutritionAtStart { get; set; }
        public int StabilityAtStart { get; set; }
        public int GlobalQuality { get; set; }
        public int NutritionSpeedBasisPoints { get; set; }
        public int StabilityYieldBasisPoints { get; set; }
        public int IndicativeCombinedEfficiencyBasisPoints { get; set; }
        public int BaseRecruits { get; set; }
        public int ExpectedRecruits { get; set; }
        public int BaseDurationSeconds { get; set; }
        public int DurationSeconds { get; set; }
        public DateTimeOffset StartsAtUtc { get; set; }
        public DateTimeOffset EndsAtUtc { get; set; }
        public long RecruitsAlreadyInReserve { get; set; }
        public long ReserveCapacity { get; set; }
        public bool CanStartWithCurrentReserve { get; set; }
    }

    public sealed class RemoteNurseryCohortSnapshot
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public string ContractVersion { get; set; }
        public long HiveRevision { get; set; }
        public long CohortRevision { get; set; }
        public DateTimeOffset ServerTimeUtc { get; set; }
        public long RecruitsInReserve { get; set; }
        public long ReserveCapacity { get; set; }
        public RemoteNurseryCohort Active { get; set; }
        public RemoteNurseryGrowthForecast NextForecast { get; set; }
        public bool CanStart { get; set; }
        public string Status { get; set; }
    }

    public sealed class NurseryCohortStartRequest
    {
        public long ExpectedRevision { get; set; }
        public string IdempotencyKey { get; set; }
    }

    public sealed class RemoteNurseryCohortReceipt
    {
        public string IdempotencyKey { get; set; }
        public Guid CohortId { get; set; }
        public long RevisionBefore { get; set; }
        public long RevisionAfter { get; set; }
        public DateTimeOffset AcceptedAtUtc { get; set; }
        public string Code { get; set; }
    }

    public sealed class RemoteNurseryCohortCommandResult
    {
        public bool Succeeded { get; set; }
        public string Code { get; set; }
        public RemoteNurseryCohortSnapshot Snapshot { get; set; }
        public RemoteNurseryCohortReceipt Receipt { get; set; }
    }

    public interface INurseryCohortClient
    {
        bool Enabled { get; }
        Task<RemoteNurseryCohortSnapshot> ReadAsync(
            Guid hiveId, CancellationToken cancellationToken = default(CancellationToken));
        Task<RemoteNurseryCohortCommandResult> StartAsync(
            Guid hiveId, long expectedRevision, string idempotencyKey,
            CancellationToken cancellationToken = default(CancellationToken));
    }

    /// <summary>
    /// Explicit-opt-in authenticated REST client for nursery cohorts.
    /// NO offline mutation, NO persistent cache, NO automatic POST, NO local troop
    /// grant. Requires identical replay key when retrying an uncertain command.
    /// The default constructor keeps the entire feature disabled.
    /// </summary>
    public sealed class NurseryCohortClient : INurseryCohortClient
    {
        public const string ContractVersion = "bk-nursery-cohorts-v1-prototype";
        public const string ForecastContractVersion = "bk-nursery-growth-balance-v1-draft";
        public const string StartedCode = "game.nursery_cohort_started";
        private const long MaximumReserveCapacity = 1000000000L;

        private readonly MobileAccountSessionGate gate;
        private readonly IGameAccountSessionSource sessionSource;
        private readonly IAuthenticatedGameRestTransport transport;

        public NurseryCohortClient(
            MobileAccountSessionGate gate,
            IGameAccountSessionSource sessionSource,
            IAuthenticatedGameRestTransport transport,
            bool enabled = false)
        {
            this.gate = gate ?? throw new ArgumentNullException(nameof(gate));
            this.sessionSource = sessionSource ??
                throw new ArgumentNullException(nameof(sessionSource));
            this.transport = transport ??
                throw new ArgumentNullException(nameof(transport));
            Enabled = enabled;
        }

        public bool Enabled { get; }

        public static string ReadPath(Guid hiveId)
        {
            RequireHive(hiveId);
            return "/game/v1/hives/" + hiveId.ToString("D") + "/brood/cohorts";
        }

        public static string StartPath(Guid hiveId) => ReadPath(hiveId) + "/start";

        public async Task<RemoteNurseryCohortSnapshot> ReadAsync(
            Guid hiveId, CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireEnabled();
            string path = ReadPath(hiveId);
            SessionIdentity context = await ResolveSessionAsync(cancellationToken)
                .ConfigureAwait(false);
            RemoteNurseryCohortSnapshot snapshot =
                await SendWithSingleAuthenticationRefreshAsync<RemoteNurseryCohortSnapshot>(
                    new AuthenticatedGameRestRequest("GET", path), context,
                    cancellationToken).ConfigureAwait(false);
            ValidateSnapshot(snapshot, context.PlayerId, hiveId);
            return snapshot;
        }

        public async Task<RemoteNurseryCohortCommandResult> StartAsync(
            Guid hiveId, long expectedRevision, string idempotencyKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireEnabled();
            string path = StartPath(hiveId);
            if (expectedRevision < 0 || expectedRevision == long.MaxValue)
                throw InvalidRequest("A bounded expected revision is required.");
            RequireIdempotencyKey(idempotencyKey);
            SessionIdentity context = await ResolveSessionAsync(cancellationToken)
                .ConfigureAwait(false);
            AuthenticatedGameRestRequest request = new AuthenticatedGameRestRequest(
                "POST", path, new NurseryCohortStartRequest
                {
                    ExpectedRevision = expectedRevision,
                    IdempotencyKey = idempotencyKey
                });
            RemoteNurseryCohortCommandResult result =
                await SendWithSingleAuthenticationRefreshAsync
                    <RemoteNurseryCohortCommandResult>(request, context,
                        cancellationToken).ConfigureAwait(false);
            if (result == null || !result.Succeeded ||
                !string.Equals(result.Code, StartedCode, StringComparison.Ordinal) ||
                result.Receipt == null || result.Snapshot == null)
                throw InvalidResponse("The cohort start response is incomplete.");
            ValidateSnapshot(result.Snapshot, context.PlayerId, hiveId);
            RemoteNurseryCohortReceipt receipt = result.Receipt;
            if (!string.Equals(receipt.IdempotencyKey, idempotencyKey,
                    StringComparison.Ordinal) || receipt.CohortId == Guid.Empty ||
                !string.Equals(receipt.Code, StartedCode, StringComparison.Ordinal) ||
                receipt.RevisionBefore != expectedRevision ||
                receipt.RevisionAfter != expectedRevision + 1L ||
                result.Snapshot.HiveRevision < receipt.RevisionAfter ||
                !IsUtc(receipt.AcceptedAtUtc) ||
                receipt.AcceptedAtUtc > result.Snapshot.ServerTimeUtc)
                throw InvalidResponse("The cohort start receipt is invalid.");
            // A replay after maturation has no Active cohort; a fresh response
            // at the receipt revision MUST describe the cohort just started.
            if (result.Snapshot.HiveRevision == receipt.RevisionAfter &&
                (result.Snapshot.Active == null ||
                 result.Snapshot.Active.CohortId != receipt.CohortId))
                throw InvalidResponse("The newly started cohort is missing.");
            return result;
        }

        public static void ValidateSnapshot(
            RemoteNurseryCohortSnapshot snapshot, Guid playerId, Guid hiveId)
        {
            if (snapshot == null || snapshot.PlayerId != playerId ||
                snapshot.HiveId != hiveId)
                throw InvalidResponse("The cohort belongs to another player or hive.");
            if (!string.Equals(snapshot.ContractVersion, ContractVersion,
                    StringComparison.Ordinal) ||
                snapshot.HiveRevision < 0 || snapshot.CohortRevision < 0 ||
                snapshot.CohortRevision > snapshot.HiveRevision ||
                !IsUtc(snapshot.ServerTimeUtc) ||
                snapshot.ReserveCapacity <= 0 ||
                snapshot.ReserveCapacity > MaximumReserveCapacity ||
                snapshot.RecruitsInReserve < 0 ||
                snapshot.RecruitsInReserve > snapshot.ReserveCapacity)
                throw InvalidResponse("Invalid cohort snapshot metadata.");

            if (snapshot.Status != "nursery_not_built" &&
                snapshot.Status != "vitality_not_initialized" &&
                snapshot.Status != "incubating" &&
                snapshot.Status != "ready_to_start" &&
                snapshot.Status != "reserve_full")
                throw InvalidResponse("Unknown cohort status.");

            if (snapshot.Active != null)
            {
                RemoteNurseryCohort active = snapshot.Active;
                if (active.CohortId == Guid.Empty ||
                    !IsUtc(active.StartedAtUtc) || !IsUtc(active.ReadyAtUtc) ||
                    active.StartedAtUtc > snapshot.ServerTimeUtc ||
                    active.ReadyAtUtc <= active.StartedAtUtc ||
                    active.ExpectedRecruits <= 0 ||
                    active.ExpectedRecruits > snapshot.ReserveCapacity ||
                    snapshot.RecruitsInReserve >
                        snapshot.ReserveCapacity - active.ExpectedRecruits ||
                    active.NutritionAtStart < 0 || active.NutritionAtStart > 100 ||
                    active.StabilityAtStart < 0 || active.StabilityAtStart > 100 ||
                    snapshot.CanStart || snapshot.Status != "incubating")
                    throw InvalidResponse("Invalid active cohort.");
            }
            else if (snapshot.Status == "incubating")
                throw InvalidResponse("Missing active cohort.");

            if (snapshot.CanStart != (snapshot.Status == "ready_to_start"))
                throw InvalidResponse("Start availability disagrees with cohort status.");
            if (snapshot.NextForecast != null)
            {
                RemoteNurseryGrowthForecast forecast = snapshot.NextForecast;
                if (!string.Equals(forecast.ContractVersion,
                        ForecastContractVersion, StringComparison.Ordinal) ||
                    forecast.NutritionAtStart < 0 ||
                    forecast.NutritionAtStart > 100 ||
                    forecast.StabilityAtStart < 0 ||
                    forecast.StabilityAtStart > 100 ||
                    forecast.GlobalQuality != Math.Min(
                        forecast.NutritionAtStart, forecast.StabilityAtStart) ||
                    forecast.BaseRecruits <= 0 ||
                    forecast.ExpectedRecruits <= 0 ||
                    forecast.DurationSeconds <= 0 ||
                    !IsUtc(forecast.StartsAtUtc) ||
                    !IsUtc(forecast.EndsAtUtc) ||
                    forecast.EndsAtUtc <= forecast.StartsAtUtc ||
                    forecast.RecruitsAlreadyInReserve != snapshot.RecruitsInReserve ||
                    forecast.ReserveCapacity != snapshot.ReserveCapacity)
                    throw InvalidResponse("Invalid cohort forecast.");
            }
            else if (snapshot.CanStart)
                throw InvalidResponse("Start is available without a forecast.");
        }

        private void RequireEnabled()
        {
            if (!Enabled)
                throw new HivePerimeterClientException(
                    HivePerimeterClientError.NotConfigured,
                    "Nursery cohorts are disabled until staging certification.");
        }

        private async Task<SessionIdentity> ResolveSessionAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!gate.CanSubmitLogin)
                throw new HivePerimeterClientException(
                    HivePerimeterClientError.NotConfigured,
                    "Official session transport is not ready.");
            IRefreshableGameAccountSessionSource refreshable =
                sessionSource as IRefreshableGameAccountSessionSource;
            GameAccountSession session;
            try
            {
                if (refreshable != null)
                    session = await refreshable.GetFreshSessionAsync(
                        cancellationToken).ConfigureAwait(false);
                else if (!sessionSource.TryGetSession(out session))
                    session = null;
            }
            catch (OperationCanceledException) { throw; }
            catch (MobileAccountSessionException ex)
            {
                throw MapSessionError(ex);
            }
            if (session == null || session.PlayerId == Guid.Empty ||
                string.IsNullOrWhiteSpace(session.AccessToken) ||
                session.AccessToken.Length > 8192)
                throw new HivePerimeterClientException(
                    HivePerimeterClientError.AuthenticationRequired,
                    "Official game session is required.");
            return new SessionIdentity(session.PlayerId, session.AccessToken);
        }

        private async Task<T> SendWithSingleAuthenticationRefreshAsync<T>(
            AuthenticatedGameRestRequest request, SessionIdentity identity,
            CancellationToken cancellationToken)
        {
            try
            {
                return await transport.SendAsync<T>(
                    request, identity.AccessToken, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (AuthenticatedGameRestException ex)
            {
                if (ex.Error != AuthenticatedGameRestError.Unauthorized)
                    throw MapTransportError(ex);
            }

            IRefreshableGameAccountSessionSource refreshable =
                sessionSource as IRefreshableGameAccountSessionSource;
            if (refreshable == null)
                throw new HivePerimeterClientException(
                    HivePerimeterClientError.AuthenticationRequired,
                    "The game session was rejected.");
            GameAccountSession replacement;
            try
            {
                replacement = await refreshable.RefreshAfterUnauthorizedAsync(
                    identity.AccessToken, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { throw; }
            catch (MobileAccountSessionException ex)
            {
                throw MapSessionError(ex);
            }

            if (replacement == null || replacement.PlayerId != identity.PlayerId ||
                string.IsNullOrWhiteSpace(replacement.AccessToken) ||
                replacement.AccessToken.Length > 8192)
            {
                await refreshable.InvalidateUnauthorizedSessionAsync(
                    identity.AccessToken, cancellationToken).ConfigureAwait(false);
                throw InvalidResponse("Session changed identity after refresh.");
            }
            try
            {
                return await transport.SendAsync<T>(
                    request, replacement.AccessToken, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (AuthenticatedGameRestException ex)
            {
                if (ex.Error == AuthenticatedGameRestError.Unauthorized)
                {
                    await refreshable.InvalidateUnauthorizedSessionAsync(
                        replacement.AccessToken, cancellationToken).ConfigureAwait(false);
                    throw new HivePerimeterClientException(
                        HivePerimeterClientError.AuthenticationRequired,
                        "The refreshed session was rejected.");
                }
                throw MapTransportError(ex);
            }
        }

        private static HivePerimeterClientException MapSessionError(
            MobileAccountSessionException ex) =>
            new HivePerimeterClientException(
                ex.Error == MobileAccountSessionError.TransportFailure
                    ? HivePerimeterClientError.TransportFailure
                    : HivePerimeterClientError.AuthenticationRequired, ex.SafeCode);

        private static HivePerimeterClientException MapTransportError(
            AuthenticatedGameRestException ex) =>
            new HivePerimeterClientException(
                ex.Error == AuthenticatedGameRestError.NetworkFailure
                    ? HivePerimeterClientError.TransportFailure
                    : ex.Error == AuthenticatedGameRestError.Unauthorized
                        ? HivePerimeterClientError.AuthenticationRequired
                        : HivePerimeterClientError.InvalidResponse,
                ex.SafeCode);

        private static void RequireHive(Guid hiveId)
        {
            if (hiveId == Guid.Empty)
                throw InvalidRequest("A hive identifier is required.");
        }

        private static void RequireIdempotencyKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Trim() != key ||
                key.Length > 256)
                throw InvalidRequest("Invalid cohort idempotency key.");
            foreach (char c in key)
            {
                if (!(c >= 'A' && c <= 'Z') &&
                    !(c >= 'a' && c <= 'z') &&
                    !(c >= '0' && c <= '9') && c != '-' && c != '_' && c != '.')
                    throw InvalidRequest("Unsafe cohort idempotency key.");
            }
        }

        private static bool IsUtc(DateTimeOffset time) =>
            time != default(DateTimeOffset) && time.Offset == TimeSpan.Zero;

        private static HivePerimeterClientException InvalidRequest(string message) =>
            new HivePerimeterClientException(
                HivePerimeterClientError.InvalidRequest, message);

        private static HivePerimeterClientException InvalidResponse(string message) =>
            new HivePerimeterClientException(
                HivePerimeterClientError.InvalidResponse, message);

        private sealed class SessionIdentity
        {
            public SessionIdentity(Guid playerId, string accessToken)
            {
                PlayerId = playerId;
                AccessToken = accessToken;
            }
            public Guid PlayerId { get; }
            public string AccessToken { get; }
        }
    }
}
