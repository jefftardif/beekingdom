using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Gameplay.Communication
{
    public sealed class ChatSynchronizationPolicy
    {
        public TimeSpan PollInterval { get; }
        public int MaxRecoveryCycles { get; }
        public ChatSynchronizationPolicy(TimeSpan? pollInterval = null, int maxRecoveryCycles = 3)
        {
            if (maxRecoveryCycles < 0 || maxRecoveryCycles > 20) throw new ArgumentOutOfRangeException(nameof(maxRecoveryCycles));
            PollInterval = pollInterval ?? TimeSpan.FromSeconds(5);
            MaxRecoveryCycles = maxRecoveryCycles;
        }
    }

    public sealed class ChatConversationSynchronizer
    {
        private readonly ServerChatProvider provider;
        private readonly IChatDelay delay;
        private readonly ChatSynchronizationPolicy policy;

        public ChatConversationSynchronizer(ServerChatProvider provider, IChatDelay delay, ChatSynchronizationPolicy policy = null)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
            this.delay = delay ?? throw new ArgumentNullException(nameof(delay));
            this.policy = policy ?? new ChatSynchronizationPolicy();
        }

        public async Task RunAsync(string conversationId, Func<IReadOnlyList<RemoteChatMessage>, Task> onSnapshot, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(conversationId)) throw new ArgumentException("Conversation id is required.", nameof(conversationId));
            if (onSnapshot == null) throw new ArgumentNullException(nameof(onSnapshot));
            long lastSequence = 0;
            int recoveryCycles = 0;
            await provider.ConnectAsync(cancellationToken);
            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        IReadOnlyList<RemoteChatMessage> snapshot = await provider.PollWithRetryAsync(conversationId, lastSequence, cancellationToken);
                        if (snapshot.Count > 0) lastSequence = Math.Max(lastSequence, snapshot.Max(message => message.Sequence));
                        recoveryCycles = 0;
                        await onSnapshot(snapshot);
                    }
                    catch (RemoteChatTransportException exception) when (exception.Error == RemoteChatError.Unauthorized || exception.Error == RemoteChatError.Forbidden) { throw; }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception) when (recoveryCycles < policy.MaxRecoveryCycles) { recoveryCycles++; }
                    await delay.WaitAsync(policy.PollInterval, cancellationToken);
                }
            }
            finally
            {
                await provider.DisconnectAsync(CancellationToken.None);
            }
        }
    }
}
