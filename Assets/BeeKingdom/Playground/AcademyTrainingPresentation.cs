using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;

namespace BeeKingdom.Playground
{
    public enum AcademyTrainingScreenState
    {
        NotConfigured = 0,
        Loading = 1,
        Ready = 2,
        OfflineReadOnly = 3,
        Starting = 4,
        Completing = 5,
        Error = 6
    }

    // Account-scoped, server-authoritative Academic course panel controller.
    // Exactly one operation per hive. All paid mutations require a fresh server
    // snapshot; a failed request NEVER grants a certificate or debits locally.
    public sealed class AcademyTrainingPanelController : IDisposable
    {
        private readonly IAcademyTrainingClient client;
        private readonly Guid hive;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private bool disposed;
        private bool busy;
        private string pendingSignature;
        private string pendingKey;
        private string lastLoggedRefreshFailure;
        private int consecutiveReadFailures;
        private Guid deadlineProbeOperationId;
        private TimeSpan nextDeadlineProbeAt;
        private TimeSpan projectedAt;

        public AcademyTrainingPanelController(IAcademyTrainingClient client,Guid hive)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            if (hive == Guid.Empty) throw new ArgumentException("Hive id required.");
            this.hive = hive;
            State = AcademyTrainingScreenState.Loading;
        }

        public bool IsConfigured => !disposed;
        public bool IsBusy => busy;
        public AcademyTrainingScreenState State { get; private set; }
        public RemoteAcademySnapshot Model { get; private set; }
        public string LastCode { get; private set; } = string.Empty;
        public TimeSpan Elapsed => clock.Elapsed;

        // End of the countdown is projected locally. The signed-in server must
        // still confirm awaiting_completion before any diploma is claimed.
        public bool TrainingElapsed =>
            Model?.ActiveOperation != null && Remaining <= TimeSpan.Zero;

        public bool NeedsCompletionStatusRefresh =>
            Model?.ActiveOperation != null && TrainingElapsed &&
            !ReadyToComplete;

        public float RecommendedRefreshIntervalSeconds
        {
            get
            {
                if (disposed) return 30f;
                if (State == AcademyTrainingScreenState.Error)
                    return Math.Min(30f,3f*(1 << Math.Min(3,
                        Math.Max(0,consecutiveReadFailures-1))));
                if (State == AcademyTrainingScreenState.OfflineReadOnly) return 5f;
                if (NeedsCompletionStatusRefresh) return 2f;
                return Model?.ActiveOperation != null ? 12f : 30f;
            }
        }

        public void Refresh() { if (!disposed) RefreshFireAndForget(); }

        // Called from Unity Update on the exact frame when the server-clock
        // projection reaches the deadline. The GET is read-only. It does not
        // issue any diploma or alter stored resources, and never blocks frames.
        public void TickCompletionDeadline()
        {
            _ = ConfirmDeadlineAsync();
        }

        public Task ConfirmDeadlineForProofAsync() => ConfirmDeadlineAsync();

        private Task ConfirmDeadlineAsync()
        {
            if (disposed || busy)
                return Task.CompletedTask;
            // Also recover from a transient failed or cached read without any
            // user action, whether the course is underway or already due.
            bool readFailed = State == AcademyTrainingScreenState.Error ||
                State == AcademyTrainingScreenState.OfflineReadOnly;
            bool deadlineDue = Model?.ActiveOperation != null &&
                !ReadyToComplete && Remaining <= TimeSpan.Zero;
            if (!readFailed && !deadlineDue)
                return Task.CompletedTask;

            Guid operationId = Model?.ActiveOperation?.OperationId ?? Guid.Empty;
            if (deadlineProbeOperationId != operationId)
            {
                deadlineProbeOperationId = operationId;
                nextDeadlineProbeAt = TimeSpan.Zero;
            }
            if (clock.Elapsed < nextDeadlineProbeAt)
                return Task.CompletedTask;

            // Bound retries after failures, avoiding polling on every frame.
            double seconds = readFailed
                ? Math.Min(30d, 3d * Math.Pow(2d,
                    Math.Min(3, Math.Max(0, consecutiveReadFailures - 1))))
                : 2d;
            nextDeadlineProbeAt = clock.Elapsed +
                TimeSpan.FromSeconds(seconds);
            return RefreshCore();
        }

        public void Start(string course) { if (!disposed) StartFireAndForget(course); }
        public void Complete() { if (!disposed) CompleteFireAndForget(); }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            lifetime.Cancel();
            lifetime.Dispose();
            Model = null;
            deadlineProbeOperationId = Guid.Empty;
            nextDeadlineProbeAt = TimeSpan.Zero;
            pendingSignature = null;
            pendingKey = null;
            State = AcademyTrainingScreenState.NotConfigured;
        }

        public Task RefreshForProofAsync() => RefreshCore();
        public Task StartForProofAsync(string course) => StartCore(course);
        public Task CompleteForProofAsync() => CompleteCore();

        private async void RefreshFireAndForget() { await RefreshCore(); }
        private async void StartFireAndForget(string course) { await StartCore(course); }
        private async void CompleteFireAndForget() { await CompleteCore(); }

        private async Task RefreshCore()
        {
            if (busy || disposed) return;
            busy = true;
            bool had = Model != null;
            if (!had) State = AcademyTrainingScreenState.Loading;
            try
            {
                RemoteAcademySnapshot result = await client.ReadAsync(hive,lifetime.Token);
                if (disposed) return;
                if (!ReferenceEquals(result, Model) &&
                    Model != null && result.Revision < Model.Revision)
                    throw new InvalidOperationException("game.academy_stale_response");
                Model = result;
                projectedAt = clock.Elapsed;
                State = client.IsOfflineReadOnly
                    ? AcademyTrainingScreenState.OfflineReadOnly
                    : AcademyTrainingScreenState.Ready;
                LastCode = string.Empty;
                consecutiveReadFailures = 0;
                lastLoggedRefreshFailure = null;
            }
            catch (OperationCanceledException) { }
            catch (Exception error)
            {
                if (!disposed)
                {
                    // Retain last authenticated snapshot for a read-only fallback.
                    // Never permit spending or claiming during a failed refresh.
                    State = AcademyTrainingScreenState.Error;
                    LastCode = ErrorCode(error);
                    consecutiveReadFailures++;
                    string safe = LogSafeAcademyCode(LastCode);
                    if (!string.Equals(safe, lastLoggedRefreshFailure,
                        StringComparison.Ordinal))
                    {
                        lastLoggedRefreshFailure = safe;
                        UnityEngine.Debug.LogWarning(
                            "[BK Academy] Authenticated refresh failed: " + safe +
                            " (" + error.GetType().Name +
                            "). Existing course and credits were NOT changed.");
                    }
                }
            }
            finally { busy = false; }
        }

        private async Task StartCore(string course)
        {
            if (busy || disposed || Model == null ||
                State != AcademyTrainingScreenState.Ready) return;
            RemoteAcademyCourse offer = Model.Courses?.Find(x => x.CourseId == course);
            if (offer == null || !offer.CanStart || Model.ActiveOperation != null)
                return;
            busy = true;
            State = AcademyTrainingScreenState.Starting;
            long expected = Model.Revision;
            string key = MutationKey("start|"+course+"|"+expected);
            try
            {
                RemoteAcademyCommandResult reply = await client.StartAsync(
                    hive,course,expected,key,lifetime.Token);
                if (disposed) return;
                Model = reply.Snapshot;
                projectedAt = clock.Elapsed;
                pendingSignature = null;
                pendingKey = null;
                LastCode = reply.Code;
                State = AcademyTrainingScreenState.Ready;
            }
            catch (OperationCanceledException) { }
            catch (Exception error)
            {
                if (!disposed)
                {
                    LastCode = ErrorCode(error);
                    State = AcademyTrainingScreenState.Error;
                }
            }
            finally { busy = false; }
        }

        private async Task CompleteCore()
        {
            if (busy || disposed || Model == null ||
                State != AcademyTrainingScreenState.Ready ||
                Model.ActiveOperation == null || !ReadyToComplete)
                return;
            busy = true;
            State = AcademyTrainingScreenState.Completing;
            Guid operation = Model.ActiveOperation.OperationId;
            long expected = Model.Revision;
            string key = MutationKey("complete|"+operation.ToString("D")+"|"+expected);
            try
            {
                RemoteAcademyCommandResult reply = await client.CompleteAsync(
                    hive,operation,expected,key,lifetime.Token);
                if (disposed) return;
                Model = reply.Snapshot;
                projectedAt = clock.Elapsed;
                pendingSignature = null;
                pendingKey = null;
                LastCode = reply.Code;
                State = AcademyTrainingScreenState.Ready;
            }
            catch (OperationCanceledException) { }
            catch (Exception error)
            {
                if (!disposed)
                {
                    LastCode = ErrorCode(error);
                    State = AcademyTrainingScreenState.Error;
                }
            }
            finally { busy = false; }
        }

        public TimeSpan Remaining
        {
            get
            {
                if (Model?.ActiveOperation == null) return TimeSpan.Zero;
                TimeSpan delta = clock.Elapsed - projectedAt;
                DateTimeOffset now = Model.ServerTimeUtc.Add(delta);
                TimeSpan remaining = Model.ActiveOperation.CompletesAtUtc - now;
                return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
            }
        }

        public bool ReadyToComplete => Model?.ActiveOperation != null &&
            Remaining <= TimeSpan.Zero &&
            string.Equals(Model.ActiveOperation.Status,
                AcademyTrainingClient.AwaitingCompletionStatus,
                StringComparison.Ordinal);

        private string MutationKey(string signature)
        {
            if (signature != pendingSignature || string.IsNullOrEmpty(pendingKey))
            {
                pendingSignature = signature;
                pendingKey = "academy-" + Guid.NewGuid().ToString("N");
            }
            return pendingKey;
        }

        // Only print fixed-format diagnostic codes in the Editor log,
        // never URLs, bearer tokens or arbitrary exception messages.
        private static string LogSafeAcademyCode(string code)
        {
            if (string.IsNullOrEmpty(code) || code.Length > 96)
                return "game.academy_request_failed";
            foreach (char c in code)
                if (!char.IsLetterOrDigit(c) && c != '.' &&
                    c != '_' && c != '-')
                    return "game.academy_request_failed";
            return code;
        }

        private static string ErrorCode(Exception error)
        {
            if (error is AuthenticatedGameRestException rest)
                return rest.SafeCode;
            if (error is HivePerimeterClientException hiveError)
                return hiveError.Message;
            return error is InvalidOperationException
                ? error.Message
                : "game.academy_request_failed";
        }
    }
}
