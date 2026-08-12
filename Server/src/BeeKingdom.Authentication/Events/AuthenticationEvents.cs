using BeeKingdom.Authentication.Models;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Authentication.Events;

public interface IAuthenticationEvent
{
    DateTimeOffset OccurredAtUtc { get; }
}

public sealed record PlayerAuthenticated(DateTimeOffset OccurredAtUtc, PlayerId PlayerId, Guid AccountId, string SessionId) : IAuthenticationEvent;
public sealed record AuthenticationFailed(DateTimeOffset OccurredAtUtc, string Email, string ErrorCode) : IAuthenticationEvent;
public sealed record SessionCreated(DateTimeOffset OccurredAtUtc, PlayerId PlayerId, Guid AccountId, string SessionId) : IAuthenticationEvent;
public sealed record SessionExpired(DateTimeOffset OccurredAtUtc, PlayerId PlayerId, Guid AccountId, string SessionId) : IAuthenticationEvent;
public sealed record SessionRevoked(DateTimeOffset OccurredAtUtc, PlayerId PlayerId, Guid AccountId, string SessionId) : IAuthenticationEvent;
public sealed record PlayerLoggedOut(DateTimeOffset OccurredAtUtc, PlayerId PlayerId, Guid AccountId, string SessionId) : IAuthenticationEvent;

public interface IAuthenticationEventSink
{
    void Publish(IAuthenticationEvent authenticationEvent);
}

public sealed class InMemoryAuthenticationEventSink : IAuthenticationEventSink
{
    private readonly List<IAuthenticationEvent> events = new();
    public IReadOnlyList<IAuthenticationEvent> Events => events;
    public void Publish(IAuthenticationEvent authenticationEvent) => events.Add(authenticationEvent);
}
