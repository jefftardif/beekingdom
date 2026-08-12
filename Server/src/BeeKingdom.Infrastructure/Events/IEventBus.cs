namespace BeeKingdom.Infrastructure.Events;

public interface IEventBus
{
    void Publish<TEvent>(TEvent eventData);
    IDisposable Subscribe<TEvent>(Action<TEvent> handler);
    bool HasSubscribers<TEvent>();
}
