using System;
using System.Collections.Generic;
using System.Diagnostics;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Services
{
    public sealed class EventBus : GameServiceBase, IEventBus
    {
        private readonly EventRegistry registry = new EventRegistry();

        public override int Priority => 40;
        public EventDiagnostics Diagnostics { get; } = new EventDiagnostics();

        public void Publish<TEvent>(TEvent eventData)
        {
            EventDispatcher<TEvent> dispatcher = registry.GetOrCreate<TEvent>();
            int subscriberCount = dispatcher.SubscriberCount;
            long start = Stopwatch.GetTimestamp();
            dispatcher.Publish(eventData);
            long elapsed = Stopwatch.GetTimestamp() - start;
            Diagnostics.RecordPublish(EventType<TEvent>.Id, EventType<TEvent>.Name, subscriberCount, elapsed);
        }

        public EventSubscription Subscribe<TEvent>(Action<TEvent> handler)
        {
            EventDispatcher<TEvent> dispatcher = registry.GetOrCreate<TEvent>();
            EventSubscription subscription = dispatcher.Subscribe(handler, false);
            Diagnostics.SetSubscriberCount(EventType<TEvent>.Id, EventType<TEvent>.Name, dispatcher.SubscriberCount);
            return subscription;
        }

        public EventSubscription SubscribeOnce<TEvent>(Action<TEvent> handler)
        {
            EventDispatcher<TEvent> dispatcher = registry.GetOrCreate<TEvent>();
            EventSubscription subscription = dispatcher.Subscribe(handler, true);
            Diagnostics.SetSubscriberCount(EventType<TEvent>.Id, EventType<TEvent>.Name, dispatcher.SubscriberCount);
            return subscription;
        }

        public void Unsubscribe<TEvent>(Action<TEvent> handler)
        {
            if (registry.TryGet(out EventDispatcher<TEvent> dispatcher))
            {
                dispatcher.Unsubscribe(handler);
                Diagnostics.SetSubscriberCount(EventType<TEvent>.Id, EventType<TEvent>.Name, dispatcher.SubscriberCount);
            }
        }

        public bool HasSubscribers<TEvent>()
        {
            return registry.TryGet(out EventDispatcher<TEvent> dispatcher) && dispatcher.SubscriberCount > 0;
        }

        protected override void OnShutdown()
        {
            registry.Clear();
            Diagnostics.Clear();
        }
    }

    internal sealed class EventRegistry
    {
        private readonly Dictionary<int, IEventDispatcher> dispatchersByEventId = new Dictionary<int, IEventDispatcher>();

        public EventDispatcher<TEvent> GetOrCreate<TEvent>()
        {
            int eventId = EventType<TEvent>.Id;
            if (!dispatchersByEventId.TryGetValue(eventId, out IEventDispatcher dispatcher))
            {
                dispatcher = new EventDispatcher<TEvent>();
                dispatchersByEventId[eventId] = dispatcher;
            }

            return (EventDispatcher<TEvent>)dispatcher;
        }

        public bool TryGet<TEvent>(out EventDispatcher<TEvent> dispatcher)
        {
            if (dispatchersByEventId.TryGetValue(EventType<TEvent>.Id, out IEventDispatcher value))
            {
                dispatcher = (EventDispatcher<TEvent>)value;
                return true;
            }

            dispatcher = null;
            return false;
        }

        public void Clear()
        {
            foreach (IEventDispatcher dispatcher in dispatchersByEventId.Values)
            {
                dispatcher.Clear();
            }

            dispatchersByEventId.Clear();
        }
    }

    internal interface IEventDispatcher
    {
        void Clear();
    }

    internal sealed class EventDispatcher<TEvent> : IEventDispatcher
    {
        private readonly List<EventHandlerEntry> handlers = new List<EventHandlerEntry>();
        private bool isPublishing;
        private bool requiresCompaction;

        public int SubscriberCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < handlers.Count; i++)
                {
                    if (!handlers[i].IsRemoved)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public EventSubscription Subscribe(Action<TEvent> handler, bool once)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            EventHandlerEntry entry = new EventHandlerEntry(handler, once);
            handlers.Add(entry);
            return new EventSubscription(() => RemoveEntry(entry));
        }

        public void Unsubscribe(Action<TEvent> handler)
        {
            for (int i = 0; i < handlers.Count; i++)
            {
                if (handlers[i].Handler == handler)
                {
                    RemoveEntry(handlers[i]);
                    return;
                }
            }
        }

        public void Publish(TEvent eventData)
        {
            isPublishing = true;

            for (int i = 0; i < handlers.Count; i++)
            {
                EventHandlerEntry entry = handlers[i];
                if (entry.IsRemoved)
                {
                    continue;
                }

                entry.Handler(eventData);
                if (entry.Once)
                {
                    RemoveEntry(entry);
                }
            }

            isPublishing = false;
            if (requiresCompaction)
            {
                handlers.RemoveAll(entry => entry.IsRemoved);
                requiresCompaction = false;
            }
        }

        public void Clear()
        {
            handlers.Clear();
            requiresCompaction = false;
            isPublishing = false;
        }

        private void RemoveEntry(EventHandlerEntry entry)
        {
            entry.IsRemoved = true;
            if (isPublishing)
            {
                requiresCompaction = true;
            }
            else
            {
                handlers.Remove(entry);
            }
        }

        private sealed class EventHandlerEntry
        {
            public Action<TEvent> Handler { get; }
            public bool Once { get; }
            public bool IsRemoved { get; set; }

            public EventHandlerEntry(Action<TEvent> handler, bool once)
            {
                Handler = handler;
                Once = once;
            }
        }
    }

    internal static class EventType<TEvent>
    {
        public static readonly int Id = EventTypeId.Next();
        public static readonly string Name = typeof(TEvent).Name;
    }

    internal static class EventTypeId
    {
        private static int nextId;

        public static int Next()
        {
            return ++nextId;
        }
    }
}
