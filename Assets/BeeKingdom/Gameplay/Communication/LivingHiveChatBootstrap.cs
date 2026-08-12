using System;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Gameplay.Communication
{
    public interface ILivingHiveChatBootstrap
    {
        Task ActivateAsync(RemoteChatClientOptions options, IChatSessionSource sessions, IChatStringStore storage, IChatDataProtector protector, IChatRealtimeTransport realtime = null, IChatDiagnosticsSink diagnostics = null, CancellationToken ct = default);
        Task LogoutAsync(CancellationToken ct = default);
    }

    public sealed class LivingHiveChatBootstrap : ILivingHiveChatBootstrap
    {
        private readonly SemaphoreSlim lifecycle = new SemaphoreSlim(1, 1);

        public async Task ActivateAsync(RemoteChatClientOptions options, IChatSessionSource sessions, IChatStringStore storage, IChatDataProtector protector, IChatRealtimeTransport realtime = null, IChatDiagnosticsSink diagnostics = null, CancellationToken ct = default)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (sessions == null) throw new ArgumentNullException(nameof(sessions));
            await lifecycle.WaitAsync(ct);
            try
            {
                ChatSession current = await sessions.GetSessionAsync(ct);
                if (current == null || !ChatSessionSecurity.IsValidPlayerId(current.PlayerId) || !string.Equals(current.PlayerId, options.StoragePartitionId?.Trim(), StringComparison.Ordinal))
                    throw new RemoteChatTransportException(RemoteChatError.LocalAccountMismatch, "Authenticated player does not match the requested chat storage partition.", 0, "local_account_mismatch");
                ct.ThrowIfCancellationRequested();
                RemoteChatClientComponents components = RemoteChatClientFactory.Create(options, sessions, storage, protector, realtime, diagnostics);
                ct.ThrowIfCancellationRequested();
                await LivingHiveChatRuntime.ReconfigureAsync(new LivingHiveChatController(components.Provider, recentCache: components.RecentCache));
            }
            finally { lifecycle.Release(); }
        }

        public async Task LogoutAsync(CancellationToken ct = default)
        {
            await lifecycle.WaitAsync(ct);
            try { await LivingHiveChatRuntime.ResetAsync(); }
            finally { lifecycle.Release(); }
        }
    }

    public sealed class LivingHiveChatSessionBinding
    {
        public RemoteChatClientOptions Options { get; }
        public IChatSessionSource Sessions { get; }
        public IChatStringStore Storage { get; }
        public IChatDataProtector Protector { get; }
        public IChatRealtimeTransport Realtime { get; }
        public IChatDiagnosticsSink Diagnostics { get; }

        public LivingHiveChatSessionBinding(RemoteChatClientOptions options, IChatSessionSource sessions, IChatStringStore storage, IChatDataProtector protector, IChatRealtimeTransport realtime = null, IChatDiagnosticsSink diagnostics = null)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            Sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
            Storage = storage ?? throw new ArgumentNullException(nameof(storage));
            Protector = protector ?? throw new ArgumentNullException(nameof(protector));
            if (!ChatSessionSecurity.IsValidPlayerId(options.StoragePartitionId)) throw new ArgumentException("A valid player storage partition is required.", nameof(options));
            Realtime = realtime;
            Diagnostics = diagnostics;
        }
    }

    public interface IChatAccountSessionReadiness
    {
        bool CanSubmitLogin { get; }
    }

    public sealed class DelegateChatAccountSessionReadiness : IChatAccountSessionReadiness
    {
        private readonly Func<bool> canSubmitLogin;
        public DelegateChatAccountSessionReadiness(Func<bool> canSubmitLogin) { this.canSubmitLogin = canSubmitLogin ?? throw new ArgumentNullException(nameof(canSubmitLogin)); }
        public bool CanSubmitLogin => canSubmitLogin();
    }

    public sealed class LivingHiveChatSessionCoordinator : IDisposable
    {
        private readonly ILivingHiveChatBootstrap bootstrap;
        private readonly SemaphoreSlim lifecycle = new SemaphoreSlim(1, 1);
        private readonly object gate = new object();
        private CancellationTokenSource transition;
        private string activePlayerId;
        private LivingHiveChatSessionBinding activeBinding;
        private bool disposed;

        public LivingHiveChatSessionCoordinator(ILivingHiveChatBootstrap bootstrap = null)
        {
            this.bootstrap = bootstrap ?? new LivingHiveChatBootstrap();
        }

        public async Task SessionAvailableAsync(IChatAccountSessionReadiness readiness, LivingHiveChatSessionBinding binding, CancellationToken ct = default)
        {
            if (readiness == null) throw new ArgumentNullException(nameof(readiness));
            if (binding == null) throw new ArgumentNullException(nameof(binding));
            CancellationTokenSource operation = BeginTransition(ct);
            try
            {
                await lifecycle.WaitAsync(operation.Token);
                try
                {
                    operation.Token.ThrowIfCancellationRequested();
                    if (!readiness.CanSubmitLogin)
                        throw new RemoteChatTransportException(RemoteChatError.Disabled, "Official account session is not ready for chat activation.", 0, "account_session_not_ready");

                    string playerId = binding.Options.StoragePartitionId;
                    if (string.Equals(activePlayerId, playerId, StringComparison.Ordinal) && ReferenceEquals(activeBinding, binding)) return;
                    if (activePlayerId != null) await bootstrap.LogoutAsync(CancellationToken.None);
                    activePlayerId = null;
                    activeBinding = null;
                    operation.Token.ThrowIfCancellationRequested();
                    await bootstrap.ActivateAsync(binding.Options, binding.Sessions, binding.Storage, binding.Protector, binding.Realtime, binding.Diagnostics, operation.Token);
                    operation.Token.ThrowIfCancellationRequested();
                    activePlayerId = playerId;
                    activeBinding = binding;
                }
                catch
                {
                    activePlayerId = null;
                    activeBinding = null;
                    await bootstrap.LogoutAsync(CancellationToken.None);
                    throw;
                }
                finally { lifecycle.Release(); }
            }
            finally { EndTransition(operation); }
        }

        public async Task SessionEndedAsync()
        {
            CancellationTokenSource operation = BeginTransition(CancellationToken.None);
            try
            {
                await lifecycle.WaitAsync(CancellationToken.None);
                try
                {
                    if (activePlayerId != null) await bootstrap.LogoutAsync(CancellationToken.None);
                    activePlayerId = null;
                    activeBinding = null;
                }
                finally { lifecycle.Release(); }
            }
            finally { EndTransition(operation); }
        }

        private CancellationTokenSource BeginTransition(CancellationToken external)
        {
            lock (gate)
            {
                if (disposed) throw new ObjectDisposedException(nameof(LivingHiveChatSessionCoordinator));
                transition?.Cancel();
                transition = CancellationTokenSource.CreateLinkedTokenSource(external);
                return transition;
            }
        }

        private void EndTransition(CancellationTokenSource operation)
        {
            lock (gate)
            {
                if (ReferenceEquals(transition, operation)) transition = null;
            }
            operation.Dispose();
        }

        public void Dispose()
        {
            lock (gate)
            {
                if (disposed) return;
                disposed = true;
                transition?.Cancel();
            }
        }
    }
}
