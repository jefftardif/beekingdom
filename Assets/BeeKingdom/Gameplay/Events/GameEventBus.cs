using System;
using System.Collections.Generic;

namespace BeeKingdom.Gameplay.Events
{
    public interface IGameEvent
    {
    }

    public readonly struct GameEventContext
    {
        public long Sequence { get; }
        public DateTime TimestampUtc { get; }
        public string Source { get; }

        public GameEventContext(long sequence, DateTime timestampUtc, string source)
        {
            Sequence = sequence;
            TimestampUtc = timestampUtc;
            Source = source ?? string.Empty;
        }
    }

    public sealed class GameEventSubscription : IDisposable
    {
        private readonly Action unsubscribe;
        private bool disposed;

        public bool IsDisposed => disposed;

        internal GameEventSubscription(Action unsubscribe)
        {
            this.unsubscribe = unsubscribe ?? throw new ArgumentNullException(nameof(unsubscribe));
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            unsubscribe();
        }
    }

    public sealed class GameEventBus
    {
        private readonly Dictionary<Type, IGameEventChannel> channels = new Dictionary<Type, IGameEventChannel>(64);
        private long nextSequence;

        public static GameEventBus Shared { get; } = new GameEventBus();

        public GameEventSubscription Subscribe<TEvent>(Action<TEvent, GameEventContext> handler)
            where TEvent : struct, IGameEvent
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            return GetChannel<TEvent>().Subscribe(handler, false);
        }

        public GameEventSubscription Subscribe<TEvent>(Action<TEvent> handler)
            where TEvent : struct, IGameEvent
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            return Subscribe<TEvent>((eventData, context) => handler(eventData));
        }

        public GameEventSubscription SubscribeOnce<TEvent>(Action<TEvent, GameEventContext> handler)
            where TEvent : struct, IGameEvent
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            return GetChannel<TEvent>().Subscribe(handler, true);
        }

        public GameEventContext Publish<TEvent>(TEvent eventData, string source = null)
            where TEvent : struct, IGameEvent
        {
            GameEventContext context = new GameEventContext(++nextSequence, DateTime.UtcNow, source);
            if (channels.TryGetValue(typeof(TEvent), out IGameEventChannel channel))
                ((GameEventChannel<TEvent>)channel).Publish(eventData, context);
            return context;
        }

        public bool HasSubscribers<TEvent>() where TEvent : struct, IGameEvent
        {
            return channels.TryGetValue(typeof(TEvent), out IGameEventChannel channel) &&
                ((GameEventChannel<TEvent>)channel).SubscriberCount > 0;
        }

        public void Clear()
        {
            foreach (IGameEventChannel channel in channels.Values) channel.Clear();
            channels.Clear();
            nextSequence = 0L;
        }

        private GameEventChannel<TEvent> GetChannel<TEvent>() where TEvent : struct, IGameEvent
        {
            Type type = typeof(TEvent);
            if (!channels.TryGetValue(type, out IGameEventChannel channel))
            {
                channel = new GameEventChannel<TEvent>();
                channels.Add(type, channel);
            }
            return (GameEventChannel<TEvent>)channel;
        }
    }

    internal interface IGameEventChannel
    {
        void Clear();
    }

    internal sealed class GameEventChannel<TEvent> : IGameEventChannel where TEvent : struct, IGameEvent
    {
        private readonly List<HandlerEntry> handlers = new List<HandlerEntry>(4);
        private bool publishing;
        private bool requiresCompaction;

        public int SubscriberCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < handlers.Count; i++)
                    if (!handlers[i].Removed) count++;
                return count;
            }
        }

        public GameEventSubscription Subscribe(Action<TEvent, GameEventContext> handler, bool once)
        {
            var entry = new HandlerEntry(handler, once);
            handlers.Add(entry);
            return new GameEventSubscription(() => Remove(entry));
        }

        public void Publish(TEvent eventData, GameEventContext context)
        {
            publishing = true;
            try
            {
                for (int i = 0; i < handlers.Count; i++)
                {
                    HandlerEntry entry = handlers[i];
                    if (entry.Removed) continue;
                    if (entry.Once) Remove(entry);
                    entry.Handler(eventData, context);
                }
            }
            finally
            {
                publishing = false;
                if (requiresCompaction) Compact();
            }
        }

        public void Clear()
        {
            handlers.Clear();
            requiresCompaction = false;
        }

        private void Remove(HandlerEntry entry)
        {
            if (entry.Removed) return;
            entry.Removed = true;
            if (publishing) requiresCompaction = true;
            else handlers.Remove(entry);
        }

        private void Compact()
        {
            for (int i = handlers.Count - 1; i >= 0; i--)
                if (handlers[i].Removed) handlers.RemoveAt(i);
            requiresCompaction = false;
        }

        private sealed class HandlerEntry
        {
            public readonly Action<TEvent, GameEventContext> Handler;
            public readonly bool Once;
            public bool Removed;

            public HandlerEntry(Action<TEvent, GameEventContext> handler, bool once)
            {
                Handler = handler;
                Once = once;
            }
        }
    }
}
