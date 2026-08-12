using System;
using BeeKingdom.Core.Events;

namespace BeeKingdom.Core.Services
{
    public interface IEventBus : IGameService
    {
        EventDiagnostics Diagnostics { get; }
        void Publish<TEvent>(TEvent eventData);
        EventSubscription Subscribe<TEvent>(Action<TEvent> handler);
        EventSubscription SubscribeOnce<TEvent>(Action<TEvent> handler);
        void Unsubscribe<TEvent>(Action<TEvent> handler);
        bool HasSubscribers<TEvent>();
    }
}
