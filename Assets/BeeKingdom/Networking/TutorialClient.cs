using System;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Networking
{
    public sealed class TutorialProgressSnapshot
    {
        public string ChapterKey { get; set; }
        public string SafeResumeStepKey { get; set; }
        public string LastObservedStepKey { get; set; }
        public DateTimeOffset UpdatedAtUtc { get; set; }
        public long Revision { get; set; }
    }

    public interface ITutorialClient
    {
        Task<TutorialProgressSnapshot> LoadAsync(Guid hiveId, CancellationToken ct = default);
        Task<TutorialProgressSnapshot> SaveAsync(Guid hiveId, string chapterKey, string safeResume, string lastObserved, long expectedRevision, string idempotencyKey, CancellationToken ct = default);
    }

    public sealed class TutorialClient : ITutorialClient
    {
        private readonly MobileAccountSessionGate gate;
        private readonly IGameAccountSessionSource source;
        private readonly IAuthenticatedGameRestTransport transport;

        public TutorialClient(MobileAccountSessionGate gate, IGameAccountSessionSource source, IAuthenticatedGameRestTransport transport)
        {
            this.gate = gate ?? throw new ArgumentNullException(nameof(gate));
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public async Task<TutorialProgressSnapshot> LoadAsync(Guid hiveId, CancellationToken ct = default)
        {
            if (hiveId == Guid.Empty) throw new ArgumentException("hiveId");
            var ctx = await RequireSessionAsync(ct).ConfigureAwait(false);
            var req = new AuthenticatedGameRestRequest("GET", $"/game/v1/hives/{hiveId:D}/tutorial");
            var dto = await SendWithRefreshAsync<TutorialProgressDto>(req, ctx, ct).ConfigureAwait(false);
            return new TutorialProgressSnapshot
            {
                ChapterKey = dto.chapterKey ?? string.Empty,
                SafeResumeStepKey = dto.safeResumeStepKey ?? string.Empty,
                LastObservedStepKey = dto.lastObservedStepKey ?? string.Empty,
                UpdatedAtUtc = string.IsNullOrEmpty(dto.updatedAtUtc) ? DateTimeOffset.MinValue : DateTimeOffset.Parse(dto.updatedAtUtc),
                Revision = dto.revision
            };
        }

        public async Task<TutorialProgressSnapshot> SaveAsync(Guid hiveId, string chapterKey, string safeResume, string lastObserved, long expectedRevision, string idempotencyKey, CancellationToken ct = default)
        {
            if (hiveId == Guid.Empty) throw new ArgumentException("hiveId");
            var ctx = await RequireSessionAsync(ct).ConfigureAwait(false);
            var req = new AuthenticatedGameRestRequest("POST", $"/game/v1/hives/{hiveId:D}/tutorial/progress", new SaveTutorialProgressBody
            {
                ChapterKey = chapterKey ?? string.Empty,
                SafeResumeStepKey = safeResume ?? string.Empty,
                LastObservedStepKey = lastObserved ?? string.Empty,
                ExpectedRevision = expectedRevision,
                IdempotencyKey = idempotencyKey ?? Guid.NewGuid().ToString()
            });
            var dto = await SendWithRefreshAsync<TutorialProgressDto>(req, ctx, ct).ConfigureAwait(false);
            return new TutorialProgressSnapshot
            {
                ChapterKey = dto.chapterKey ?? string.Empty,
                SafeResumeStepKey = dto.safeResumeStepKey ?? string.Empty,
                LastObservedStepKey = dto.lastObservedStepKey ?? string.Empty,
                UpdatedAtUtc = string.IsNullOrEmpty(dto.updatedAtUtc) ? DateTimeOffset.MinValue : DateTimeOffset.Parse(dto.updatedAtUtc),
                Revision = dto.revision
            };
        }

        private async Task<SessionContext> RequireSessionAsync(CancellationToken ct)
        {
            if (!gate.CanSubmitLogin) throw new HivePerimeterClientException(HivePerimeterClientError.NotConfigured, "Official account session transport is not ready.");
            var refreshable = source as IRefreshableGameAccountSessionSource;
            if (refreshable != null)
            {
                try { return RequireUsable(await refreshable.GetFreshSessionAsync(ct).ConfigureAwait(false)); }
                catch (OperationCanceledException) { throw; }
                catch (MobileAccountSessionException ex) { throw new HivePerimeterClientException(HivePerimeterClientError.AuthenticationRequired, ex.SafeCode); }
            }
            if (!source.TryGetSession(out var s) || s == null) throw new HivePerimeterClientException(HivePerimeterClientError.AuthenticationRequired, "An official account session is required.");
            return RequireUsable(s);
        }

        private static SessionContext RequireUsable(GameAccountSession s)
        {
            if (s == null || s.PlayerId == Guid.Empty || string.IsNullOrWhiteSpace(s.AccessToken) || s.AccessToken.Length > 8192) throw new HivePerimeterClientException(HivePerimeterClientError.AuthenticationRequired, "An official account session is required.");
            return new SessionContext(s.PlayerId, s.AccessToken);
        }

        private async Task<T> SendWithRefreshAsync<T>(AuthenticatedGameRestRequest req, SessionContext ctx, CancellationToken ct)
        {
            try { return await transport.SendAsync<T>(req, ctx.AccessToken, ct).ConfigureAwait(false); }
            catch (AuthenticatedGameRestException ex) when (ex.Error == AuthenticatedGameRestError.Unauthorized)
            {
                var refreshable = source as IRefreshableGameAccountSessionSource;
                if (refreshable == null) throw new HivePerimeterClientException(HivePerimeterClientError.AuthenticationRequired, "The game session was rejected.");
                GameAccountSession repl;
                try { repl = await refreshable.RefreshAfterUnauthorizedAsync(ctx.AccessToken, ct).ConfigureAwait(false); }
                catch (MobileAccountSessionException mex) { throw new HivePerimeterClientException(HivePerimeterClientError.AuthenticationRequired, mex.SafeCode); }
                if (repl == null || repl.PlayerId != ctx.PlayerId || string.IsNullOrWhiteSpace(repl.AccessToken)) throw new HivePerimeterClientException(HivePerimeterClientError.AuthenticationRequired, "The refreshed game session changed identity.");
                try { return await transport.SendAsync<T>(req, repl.AccessToken, ct).ConfigureAwait(false); }
                catch (AuthenticatedGameRestException ex2) when (ex2.Error == AuthenticatedGameRestError.Unauthorized) { await refreshable.InvalidateUnauthorizedSessionAsync(repl.AccessToken, ct).ConfigureAwait(false); throw new HivePerimeterClientException(HivePerimeterClientError.AuthenticationRequired, "The refreshed game session was rejected."); }
            }
        }

        private sealed class SessionContext { public SessionContext(Guid pid, string tok) { PlayerId = pid; AccessToken = tok; } public Guid PlayerId { get; } public string AccessToken { get; } }

        private sealed class TutorialProgressDto
        {
            public string chapterKey;
            public string safeResumeStepKey;
            public string lastObservedStepKey;
            public string updatedAtUtc;
            public long revision;
        }
        private sealed class SaveTutorialProgressBody
        {
            public string ChapterKey;
            public string SafeResumeStepKey;
            public string LastObservedStepKey;
            public long ExpectedRevision;
            public string IdempotencyKey;
        }
    }
}
