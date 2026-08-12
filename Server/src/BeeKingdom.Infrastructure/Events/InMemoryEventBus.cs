using BeeKingdom.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Infrastructure.Events;

public sealed class InMemoryEventBus : IEventBus
{
    private readonly Dictionary<Type, List<Delegate>> handlersByType = new();
    private readonly object sync = new();
    private readonly ILogger<InMemoryEventBus> logger;
    private readonly BeeKingdomServerOptions options;

    public InMemoryEventBus(ILogger<InMemoryEventBus> logger, IOptions<BeeKingdomServerOptions> options)
    {
        this.logger = logger;
        this.options = options.Value;
    }

    public void Publish<TEvent>(TEvent eventData)
    {
        Delegate[] handlers;
        lock (sync)
        {
            handlers = handlersByType.TryGetValue(typeof(TEvent), out List<Delegate>? registered)
                ? registered.ToArray()
                : Array.Empty<Delegate>();
        }

        foreach (Delegate handler in handlers)
        {
            ((Action<TEvent>)handler)(eventData);
        }

        if (options.EnableDiagnostics)
        {
            logger.LogDebug("Published server event {EventType} to {SubscriberCount} subscribers.", typeof(TEvent).Name, handlers.Length);
        }
    }

    public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        lock (sync)
        {
            if (!handlersByType.TryGetValue(typeof(TEvent), out List<Delegate>? handlers))
            {
                handlers = new List<Delegate>();
                handlersByType[typeof(TEvent)] = handlers;
            }

            handlers.Add(handler);
        }

        return new Subscription(() => Unsubscribe(handler));
    }

    public bool HasSubscribers<TEvent>()
    {
        lock (sync)
        {
            return handlersByType.TryGetValue(typeof(TEvent), out List<Delegate>? handlers) && handlers.Count > 0;
        }
    }

    private void Unsubscribe<TEvent>(Action<TEvent> handler)
    {
        lock (sync)
        {
            if (handlersByType.TryGetValue(typeof(TEvent), out List<Delegate>? handlers))
            {
                handlers.Remove(handler);
            }
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly Action dispose;
        private bool disposed;

        public Subscription(Action dispose)
        {
            this.dispose = dispose;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            dispose();
        }
    }
}
