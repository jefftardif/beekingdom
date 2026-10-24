using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Networking
{
    public sealed class RemoteAcademyCourse
    {
        public string CourseId { get; set; }
        public int RequiredAcademyLevel { get; set; }
        public TimeSpan Duration { get; set; }
        public Dictionary<string, long> Costs { get; set; }
        public List<string> Prerequisites { get; set; }
        public int Credits { get; set; }
        public string State { get; set; }
        public bool CanStart { get; set; }
    }

    public sealed class RemoteAcademyCompletion
    {
        public string CourseId { get; set; }
        public DateTimeOffset CompletedAtUtc { get; set; }
        public int Credits { get; set; }
    }

    public sealed class RemoteAcademyOperation
    {
        public Guid OperationId { get; set; }
        public string CourseId { get; set; }
        public DateTimeOffset StartedAtUtc { get; set; }
        public DateTimeOffset CompletesAtUtc { get; set; }
        public string Status { get; set; }
    }

    public sealed class RemoteAcademyBalance
    {
        public long Amount { get; set; }
        public long Capacity { get; set; }
    }

    public sealed class RemoteAcademySnapshot
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public string ContractVersion { get; set; }
        public string CatalogVersion { get; set; }
        public long Revision { get; set; }
        public long AcademyRevision { get; set; }
        public DateTimeOffset ServerTimeUtc { get; set; }
        public int AcademyBuildingLevel { get; set; }
        public Dictionary<string, RemoteAcademyBalance> Balances { get; set; }
        public List<RemoteAcademyCourse> Courses { get; set; }
        public List<RemoteAcademyCompletion> Completed { get; set; }
        public RemoteAcademyOperation ActiveOperation { get; set; }
        public int EarnedCredits { get; set; }
    }

    public sealed class RemoteAcademyMutationRequest
    {
        public long ExpectedRevision { get; set; }
        public string IdempotencyKey { get; set; }
    }

    public sealed class RemoteAcademyCommandReceipt
    {
        public string IdempotencyKey { get; set; }
        public string CourseId { get; set; }
        public Guid OperationId { get; set; }
        public string Code { get; set; }
        public long RevisionBefore { get; set; }
        public long RevisionAfter { get; set; }
        public DateTimeOffset AcceptedAtUtc { get; set; }
    }

    public sealed class RemoteAcademyCommandResult
    {
        public bool Succeeded { get; set; }
        public string Code { get; set; }
        public RemoteAcademySnapshot Snapshot { get; set; }
        public RemoteAcademyCommandReceipt Receipt { get; set; }
    }

    public interface IAcademyTrainingClient
    {
        bool IsOfflineReadOnly { get; }
        Task<RemoteAcademySnapshot> ReadAsync(Guid hiveId,
            CancellationToken cancellationToken = default(CancellationToken));
        Task<RemoteAcademyCommandResult> StartAsync(Guid hiveId, string courseId,
            long expectedRevision, string idempotencyKey,
            CancellationToken cancellationToken = default(CancellationToken));
        Task<RemoteAcademyCommandResult> CompleteAsync(Guid hiveId, Guid operationId,
            long expectedRevision, string idempotencyKey,
            CancellationToken cancellationToken = default(CancellationToken));
    }

    // Same authenticated REST transport and account scope as Research.
    // No local resource debit, classes, bonuses or offline command replay.
    public sealed class AcademyTrainingClient : IAcademyTrainingClient
    {
        public const string ContractVersion = "bk-academy-training-v1";
        public const string CatalogVersion = "academy-curriculum-2026-10-v1";
        public const string AwaitingCompletionStatus = "awaiting_completion";
        public const string RunningStatus = "running";

        private static readonly HashSet<string> KnownCourses =
            new HashSet<string>(StringComparer.Ordinal)
            { "tactics", "logistics", "exploration", "mentorship" };
        private readonly MobileAccountSessionGate gate;
        private readonly IGameAccountSessionSource sessions;
        private readonly IAuthenticatedGameRestTransport transport;
        private readonly ProtectedGameReadCache cache;

        public AcademyTrainingClient(MobileAccountSessionGate gate,
            IGameAccountSessionSource sessions,
            IAuthenticatedGameRestTransport transport,
            ProtectedGameReadCache cache = null)
        {
            this.gate = gate ?? throw new ArgumentNullException(nameof(gate));
            this.sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            this.cache = cache;
        }

        public bool IsOfflineReadOnly { get; private set; }

        public async Task<RemoteAcademySnapshot> ReadAsync(
            Guid hiveId, CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireHive(hiveId);
            string path = Path(hiveId);
            try
            {
                SessionContext session = await RequireSession(cancellationToken);
                RemoteAcademySnapshot response = await Send<RemoteAcademySnapshot>(
                    new AuthenticatedGameRestRequest("GET",path),session,cancellationToken);
                ValidateSnapshot(response,session.PlayerId,hiveId);
                IsOfflineReadOnly = false;
                await StoreCache(session.PlayerId,hiveId,path,response);
                return response;
            }
            catch (Exception error) when (CacheEligible(error))
            {
                RemoteAcademySnapshot saved = await ReadCache(hiveId,path,cancellationToken);
                if (saved != null) { IsOfflineReadOnly = true; return saved; }
                throw;
            }
        }

        public async Task<RemoteAcademyCommandResult> StartAsync(Guid hiveId,
            string courseId,long expectedRevision,string idempotencyKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireHive(hiveId);
            RequireCourse(courseId);
            RequireMutation(expectedRevision,idempotencyKey);
            SessionContext session = await RequireSession(cancellationToken);
            var payload = new RemoteAcademyMutationRequest
            { ExpectedRevision = expectedRevision, IdempotencyKey = idempotencyKey };
            RemoteAcademyCommandResult response = await Send<RemoteAcademyCommandResult>(
                new AuthenticatedGameRestRequest("POST",
                    Path(hiveId)+"/"+courseId+"/start",payload),session,cancellationToken);
            ValidateMutation(response,session.PlayerId,hiveId,idempotencyKey,expectedRevision,
                courseId,Guid.Empty,"game.academy_started");
            IsOfflineReadOnly = false;
            await StoreCache(session.PlayerId,hiveId,Path(hiveId),response.Snapshot);
            return response;
        }

        public async Task<RemoteAcademyCommandResult> CompleteAsync(Guid hiveId,
            Guid operationId,long expectedRevision,string idempotencyKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireHive(hiveId);
            if (operationId == Guid.Empty)
                throw new ArgumentException("Academy operation identifier required.");
            RequireMutation(expectedRevision,idempotencyKey);
            SessionContext session = await RequireSession(cancellationToken);
            var payload = new RemoteAcademyMutationRequest
            { ExpectedRevision = expectedRevision, IdempotencyKey = idempotencyKey };
            RemoteAcademyCommandResult response = await Send<RemoteAcademyCommandResult>(
                new AuthenticatedGameRestRequest("POST",
                    Path(hiveId)+"/"+operationId.ToString("D")+"/complete",payload),
                session,cancellationToken);
            ValidateMutation(response,session.PlayerId,hiveId,idempotencyKey,expectedRevision,
                null,operationId,"game.academy_completed");
            IsOfflineReadOnly = false;
            await StoreCache(session.PlayerId,hiveId,Path(hiveId),response.Snapshot);
            return response;
        }

        private async Task<SessionContext> RequireSession(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (!gate.CanSubmitLogin)
                throw new InvalidOperationException("game.academy_session_unavailable");
            if (sessions is IRefreshableGameAccountSessionSource refreshable)
            {
                GameAccountSession fresh = await refreshable.GetFreshSessionAsync(ct);
                return RequireUsable(fresh);
            }
            if (!sessions.TryGetSession(out GameAccountSession current))
                throw new InvalidOperationException("game.academy_session_required");
            return RequireUsable(current);
        }

        private static SessionContext RequireUsable(GameAccountSession current)
        {
            if (current == null || current.PlayerId == Guid.Empty ||
                string.IsNullOrWhiteSpace(current.AccessToken) ||
                current.AccessToken.Length > 8192)
                throw new InvalidOperationException("game.academy_session_required");
            return new SessionContext(current.PlayerId,current.AccessToken);
        }

        private async Task<T> Send<T>(AuthenticatedGameRestRequest request,
            SessionContext session,CancellationToken ct)
        {
            try
            {
                return await transport.SendAsync<T>(request,session.Token,ct);
            }
            catch (AuthenticatedGameRestException e)
            {
                if (e.Error != AuthenticatedGameRestError.Unauthorized ||
                    !(sessions is IRefreshableGameAccountSessionSource refreshable))
                    throw;
                // One refresh only; identity changes are rejected before retry.
                GameAccountSession fresh =
                    await refreshable.RefreshAfterUnauthorizedAsync(session.Token,ct);
                SessionContext replacement = RequireUsable(fresh);
                if (replacement.PlayerId != session.PlayerId)
                    throw new InvalidOperationException("game.academy_identity_changed");
                try
                {
                    return await transport.SendAsync<T>(request,replacement.Token,ct);
                }
                catch (AuthenticatedGameRestException second)
                {
                    if (second.Error == AuthenticatedGameRestError.Unauthorized)
                    {
                        await refreshable.InvalidateUnauthorizedSessionAsync(
                            replacement.Token,ct);
                        throw new InvalidOperationException("game.academy_session_required");
                    }
                    throw;
                }
            }
        }

        private static bool CacheEligible(Exception error)
        {
            // Only transient transport failures can fall back to the encrypted,
            // player/hive-partitioned read cache. An explicit 401 or rejected
            // credential MUST NOT reveal data from another session.
            if (error is AuthenticatedGameRestException rest)
                return rest.Error == AuthenticatedGameRestError.NetworkFailure;
            if (error is MobileAccountSessionException session)
                return session.Error == MobileAccountSessionError.TransportFailure ||
                    session.Error == MobileAccountSessionError.NotConfigured;
            return error is HivePerimeterClientException hive &&
                hive.Error == HivePerimeterClientError.TransportFailure;
        }

        private async Task StoreCache(Guid player,Guid hive,string path,
            RemoteAcademySnapshot snapshot)
        {
            if (cache == null || !cache.IsProtectionAvailable) return;
            try
            {
                await cache.SaveValidatedReadAsync(
                    player,hive,ContractVersion,path,snapshot,CancellationToken.None);
            }
            catch { /* Cache failures never cancel validated server mutations. */ }
        }

        private async Task<RemoteAcademySnapshot> ReadCache(
            Guid hive,string path,CancellationToken ct)
        {
            if (cache == null || !cache.IsProtectionAvailable) return null;
            Guid player = Guid.Empty;
            if (sessions is IRefreshableGameAccountSessionSource refreshable)
                refreshable.TryGetKnownPlayerId(out player);
            else if (sessions.TryGetSession(out GameAccountSession known) &&
                     known != null) player = known.PlayerId;
            if (player == Guid.Empty) return null;
            ProtectedGameReadCacheHit<RemoteAcademySnapshot> hit =
                await cache.TryLoadAsync<RemoteAcademySnapshot>(
                    player,hive,ContractVersion,path,ct);
            if (hit == null) return null;
            ValidateSnapshot(hit.Value,player,hive);
            return hit.Value;
        }

        public static void ValidateSnapshot(RemoteAcademySnapshot snapshot,
            Guid playerId,Guid hiveId)
        {
            if (snapshot == null ||
                snapshot.PlayerId != playerId || snapshot.HiveId != hiveId ||
                snapshot.ContractVersion != ContractVersion ||
                snapshot.CatalogVersion != CatalogVersion ||
                snapshot.Revision < 0 || snapshot.AcademyRevision < 0 ||
                snapshot.AcademyRevision > snapshot.Revision ||
                snapshot.AcademyBuildingLevel < 1 ||
                snapshot.ServerTimeUtc == default(DateTimeOffset) ||
                snapshot.ServerTimeUtc.Offset != TimeSpan.Zero ||
                snapshot.Balances == null || snapshot.Courses == null ||
                snapshot.Completed == null ||
                snapshot.Courses.Count != KnownCourses.Count ||
                snapshot.Completed.Count > KnownCourses.Count ||
                snapshot.Balances.Count > 16 ||
                snapshot.EarnedCredits < 0 || snapshot.EarnedCredits > 4)
                throw new InvalidOperationException("game.academy_invalid_snapshot");

            if (snapshot.Courses.Select(c=>c==null ? "" : c.CourseId)
                .Distinct(StringComparer.Ordinal).Count()!=KnownCourses.Count ||
                snapshot.Completed.Select(c=>c==null ? "" : c.CourseId)
                .Distinct(StringComparer.Ordinal).Count()!=snapshot.Completed.Count)
                throw new InvalidOperationException("game.academy_duplicate_courses");
            foreach (RemoteAcademyCourse course in snapshot.Courses)
            {
                if (course == null || !KnownCourses.Contains(course.CourseId) ||
                    course.RequiredAcademyLevel is < 1 or > 10 ||
                    course.Duration <= TimeSpan.Zero ||
                    course.Duration > TimeSpan.FromDays(7) ||
                    course.Costs == null || course.Costs.Count is < 1 or > 5 ||
                    course.Costs.Any(x=> x.Value <= 0 || x.Value > 1000000000 ||
                        !snapshot.Balances.ContainsKey(x.Key)) ||
                    course.Prerequisites == null || course.Prerequisites.Count > 4 ||
                    course.Prerequisites.Any(p=> !KnownCourses.Contains(p) ||
                        p == course.CourseId) ||
                    course.Credits != 1 ||
                    course.State != "available" && course.State != "locked" &&
                        course.State != "completed")
                    throw new InvalidOperationException("game.academy_invalid_course");
            }
            foreach (RemoteAcademyCompletion completion in snapshot.Completed)
                if (completion == null || !KnownCourses.Contains(completion.CourseId) ||
                    completion.Credits != 1 ||
                    completion.CompletedAtUtc == default(DateTimeOffset) ||
                    completion.CompletedAtUtc.Offset != TimeSpan.Zero ||
                    completion.CompletedAtUtc > snapshot.ServerTimeUtc)
                    throw new InvalidOperationException("game.academy_invalid_certificate");
            if (snapshot.EarnedCredits != snapshot.Completed.Sum(x=>x.Credits))
                throw new InvalidOperationException("game.academy_invalid_credits");
            var certified = new HashSet<string>(
                snapshot.Completed.Select(x => x.CourseId), StringComparer.Ordinal);
            foreach (RemoteAcademyCourse course in snapshot.Courses)
            {
                bool completed = certified.Contains(course.CourseId);
                bool prerequisites = course.RequiredAcademyLevel <=
                    snapshot.AcademyBuildingLevel &&
                    course.Prerequisites.All(certified.Contains);
                string expectedState = completed ? "completed" :
                    prerequisites ? "available" : "locked";
                if (course.State != expectedState ||
                    course.CanStart && (!prerequisites || completed ||
                        snapshot.ActiveOperation != null))
                    throw new InvalidOperationException("game.academy_inconsistent_progression");
            }
            foreach (RemoteAcademyBalance balance in snapshot.Balances.Values)
                if (balance == null || balance.Amount < 0 ||
                    balance.Capacity < balance.Amount)
                    throw new InvalidOperationException("game.academy_invalid_balance");

            RemoteAcademyOperation active = snapshot.ActiveOperation;
            if (active != null &&
                (active.OperationId == Guid.Empty ||
                 !KnownCourses.Contains(active.CourseId) ||
                 active.StartedAtUtc == default(DateTimeOffset) ||
                 active.CompletesAtUtc == default(DateTimeOffset) ||
                 active.StartedAtUtc.Offset != TimeSpan.Zero ||
                 active.CompletesAtUtc.Offset != TimeSpan.Zero ||
                 active.CompletesAtUtc <= active.StartedAtUtc ||
                 active.CompletesAtUtc - active.StartedAtUtc > TimeSpan.FromDays(7) ||
                 snapshot.Completed.Any(x=>x.CourseId == active.CourseId) ||
                 (active.Status != RunningStatus &&
                  active.Status != AwaitingCompletionStatus) ||
                 (active.Status == RunningStatus &&
                  active.CompletesAtUtc <= snapshot.ServerTimeUtc) ||
                 (active.Status == AwaitingCompletionStatus &&
                  active.CompletesAtUtc > snapshot.ServerTimeUtc)))
                throw new InvalidOperationException("game.academy_invalid_operation");
        }

        private static void ValidateMutation(RemoteAcademyCommandResult result,
            Guid player,Guid hive,string key,long expectedRevision,
            string course,Guid operation,string expectedCode)
        {
            if (result == null || !result.Succeeded ||
                result.Code != expectedCode || result.Receipt == null ||
                result.Snapshot == null)
                throw new InvalidOperationException("game.academy_invalid_response");
            ValidateSnapshot(result.Snapshot,player,hive);
            RemoteAcademyCommandReceipt receipt = result.Receipt;
            if (receipt.IdempotencyKey != key ||
                receipt.Code != expectedCode ||
                receipt.OperationId == Guid.Empty ||
                !KnownCourses.Contains(receipt.CourseId) ||
                (course != null && receipt.CourseId != course) ||
                (operation != Guid.Empty && receipt.OperationId != operation) ||
                receipt.RevisionBefore != expectedRevision ||
                receipt.RevisionAfter != expectedRevision + 1 ||
                receipt.RevisionAfter > result.Snapshot.Revision ||
                receipt.AcceptedAtUtc.Offset != TimeSpan.Zero ||
                receipt.AcceptedAtUtc > result.Snapshot.ServerTimeUtc)
                throw new InvalidOperationException("game.academy_invalid_receipt");
            // A signed receipt alone is insufficient: the resulting durable
            // queue or certificate MUST be visible in the server snapshot.
            bool hasCertificate = result.Snapshot.Completed.Exists(
                x => x.CourseId == receipt.CourseId);
            RemoteAcademyOperation active = result.Snapshot.ActiveOperation;
            if (expectedCode == "game.academy_started" &&
                !hasCertificate &&
                (active == null || active.OperationId != receipt.OperationId ||
                 active.CourseId != receipt.CourseId))
                throw new InvalidOperationException("game.academy_missing_started_course");
            if (expectedCode == "game.academy_completed" &&
                (!hasCertificate ||
                 active != null && active.OperationId == receipt.OperationId))
                throw new InvalidOperationException("game.academy_missing_certificate");
        }

        private static void RequireHive(Guid hive)
        {
            if (hive == Guid.Empty) throw new ArgumentException("Missing hive.");
        }

        private static void RequireCourse(string course)
        {
            if (!KnownCourses.Contains(course ?? ""))
                throw new ArgumentException("Unsupported Academy course.");
        }

        private static void RequireMutation(long revision,string key)
        {
            if (revision < 0 || revision == long.MaxValue ||
                string.IsNullOrWhiteSpace(key) || key.Length < 8 || key.Length > 128 ||
                key.Trim() != key ||
                key.Any(c => !((c >= 'a' && c <= 'z') ||
                    (c >= 'A' && c <= 'Z') ||
                    (c >= '0' && c <= '9') ||
                    c == '-' || c == '_' || c == '.')))
                throw new ArgumentException("Invalid Academy mutation request.");
        }

        public static string Path(Guid hive)
            => "/game/v1/hives/" + hive.ToString("D") + "/academy";

        private sealed class SessionContext
        {
            public SessionContext(Guid player,string token)
            { PlayerId=player; Token=token; }
            public Guid PlayerId { get; }
            public string Token { get; }
        }
    }
}
