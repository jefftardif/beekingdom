using BeeKingdom.Accounts.Models;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Accounts.Events;

public interface IAccountEvent
{
    DateTimeOffset OccurredAtUtc { get; }
    Guid AccountId { get; }
}

public sealed record AccountCreated(DateTimeOffset OccurredAtUtc, Guid AccountId, PlayerId PlayerId) : IAccountEvent;
public sealed record AccountUpdated(DateTimeOffset OccurredAtUtc, Guid AccountId, PlayerId PlayerId) : IAccountEvent;
public sealed record AccountSuspended(DateTimeOffset OccurredAtUtc, Guid AccountId, PlayerId PlayerId) : IAccountEvent;
public sealed record AccountReactivated(DateTimeOffset OccurredAtUtc, Guid AccountId, PlayerId PlayerId) : IAccountEvent;
public sealed record AccountDeleted(DateTimeOffset OccurredAtUtc, Guid AccountId, PlayerId PlayerId) : IAccountEvent;
public sealed record PreferencesChanged(DateTimeOffset OccurredAtUtc, Guid AccountId, PlayerId PlayerId) : IAccountEvent;

public interface IAccountEventSink
{
    void Publish(IAccountEvent accountEvent);
}

public sealed class InMemoryAccountEventSink : IAccountEventSink
{
    private readonly List<IAccountEvent> events = new();
    public IReadOnlyList<IAccountEvent> Events => events;
    public void Publish(IAccountEvent accountEvent) => events.Add(accountEvent);
}
